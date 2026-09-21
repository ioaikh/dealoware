using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Profile.Dtos;
using Dealoware.Domain.FieldAcl;

namespace Dealoware.Api.Tests;

/// <summary>
/// Tests for MVP Stage A Field ACL registry (#31).
/// 
/// Spec §7.1 test matrix:
/// - Owner/User allowed OK
/// - OwnAgent denied LoginEmail
/// - Stranger/counterparty denied LoginEmail+ContactEmail
/// - Unauthenticated deny no private fields
/// - Unknown FieldClass deny
/// 
/// Security SD points verified:
/// 1. API/DB scope only
/// 2. Open-ended FieldClass registry
/// 3. Deny-by-default unknown FieldClass
/// 4. LoginEmail User-only
/// 5. ContactEmail OwnAgent Read / counterparty Deny
/// 6. Authn fail-closed on projection
/// 7. No Stage B/C inventing
/// 8. Cross-story non-merge
/// 9. PoC $0
/// 10. Evidence + handshake
/// </summary>
[Collection("WebAppTests")]
public class FieldAclTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public FieldAclTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Test Helpers

    private async Task<(HttpClient Client, string Sub, string ApiKey)> CreateAuthenticatedClientAsync(string suffix)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"participant-{suffix}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");
        return (client, participant.Sub, participant.ApiKey);
    }

    private async Task<ProfileResponse?> SetupProfileWithFields(HttpClient client)
    {
        var updateResponse = await client.PatchAsJsonAsync("/profile", new UpdateProfileRequest
        {
            DisplayName = "Test User",
            LoginEmail = "login@test.example",
            ContactEmail = "contact@test.example"
        });
        return await updateResponse.Content.ReadFromJsonAsync<ProfileResponse>();
    }

    #endregion

    #region Owner/User Tests - Allowed OK (Security point 4)

    [Fact]
    public async Task Profile_Get_UserOK_ReturnsAllFields()
    {
        var (client, sub, _) = await CreateAuthenticatedClientAsync($"acl-user-{Guid.NewGuid()}");
        await SetupProfileWithFields(client);

        var response = await client.GetAsync("/profile");
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(profile);
        Assert.Equal(sub, profile.Sub);
        Assert.Equal("Test User", profile.DisplayName);
        Assert.Equal("login@test.example", profile.LoginEmail);
        Assert.Equal("contact@test.example", profile.ContactEmail);
        Assert.True(profile.IncludesLoginEmail);
        Assert.True(profile.IncludesContactEmail);
    }

    [Fact]
    public async Task Profile_Patch_UserOK_CanUpdateAllFields()
    {
        var (client, sub, _) = await CreateAuthenticatedClientAsync($"acl-user-patch-{Guid.NewGuid()}");

        var response = await client.PatchAsJsonAsync("/profile", new UpdateProfileRequest
        {
            DisplayName = "Updated Name",
            LoginEmail = "updated-login@test.example",
            ContactEmail = "updated-contact@test.example"
        });
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(profile);
        Assert.Equal("Updated Name", profile.DisplayName);
        Assert.Equal("updated-login@test.example", profile.LoginEmail);
        Assert.Equal("updated-contact@test.example", profile.ContactEmail);
    }

    #endregion

    #region OwnAgent Tests - LoginEmail Denied (Security points 4, 5)

    [Fact]
    public void FieldPolicy_OwnAgent_DeniedLoginEmail()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canReadLoginEmail = fieldPolicy.Evaluate(
            principal, FieldClass.LoginEmail, FieldAction.Read, context);
        var canWriteLoginEmail = fieldPolicy.Evaluate(
            principal, FieldClass.LoginEmail, FieldAction.Write, context);

        Assert.False(canReadLoginEmail, "OwnAgent should be denied LoginEmail Read");
        Assert.False(canWriteLoginEmail, "OwnAgent should be denied LoginEmail Write");
    }

    [Fact]
    public void FieldPolicy_OwnAgent_AllowedContactEmailRead()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canReadContactEmail = fieldPolicy.Evaluate(
            principal, FieldClass.ContactEmail, FieldAction.Read, context);
        var canWriteContactEmail = fieldPolicy.Evaluate(
            principal, FieldClass.ContactEmail, FieldAction.Write, context);

        Assert.True(canReadContactEmail, "OwnAgent should be allowed ContactEmail Read");
        Assert.False(canWriteContactEmail, "OwnAgent should be denied ContactEmail Write");
    }

    #endregion

    #region Stranger/Counterparty Tests - Denied (Security point 5)

    [Fact]
    public void FieldPolicy_Stranger_DeniedAllProtectedFields()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("stranger-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canReadLoginEmail = fieldPolicy.Evaluate(
            principal, FieldClass.LoginEmail, FieldAction.Read, context);
        var canReadContactEmail = fieldPolicy.Evaluate(
            principal, FieldClass.ContactEmail, FieldAction.Read, context);
        var canReadDisplayName = fieldPolicy.Evaluate(
            principal, FieldClass.DisplayName, FieldAction.Read, context);

        Assert.False(canReadLoginEmail, "Stranger should be denied LoginEmail");
        Assert.False(canReadContactEmail, "Stranger should be denied ContactEmail");
        Assert.False(canReadDisplayName, "Stranger should be denied DisplayName");
    }

    [Fact]
    public void FieldPolicy_Counterparty_DeniedProtectedFields()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("counterparty-sub");
        var context = FieldResourceContext.ForNegotiation("owner-sub", "counterparty-sub");

        var canReadLoginEmail = fieldPolicy.Evaluate(
            principal, FieldClass.LoginEmail, FieldAction.Read, context);
        var canReadContactEmail = fieldPolicy.Evaluate(
            principal, FieldClass.ContactEmail, FieldAction.Read, context);

        Assert.False(canReadLoginEmail, "Counterparty should be denied LoginEmail");
        Assert.False(canReadContactEmail, "Counterparty should be denied ContactEmail");
    }

    [Fact]
    public void FieldPolicy_Counterparty_AllowedDisplayName()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("counterparty-sub");
        var context = FieldResourceContext.ForNegotiation("owner-sub", "counterparty-sub");

        var canReadDisplayName = fieldPolicy.Evaluate(
            principal, FieldClass.DisplayName, FieldAction.Read, context);

        Assert.True(canReadDisplayName, "Counterparty should be allowed DisplayName Read");
    }

    #endregion

    #region Unauthenticated Tests - Deny No Private Fields (Security point 6)

    [Fact]
    public async Task Profile_Get_Unauth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_Get_Unauth_NoPrivateFieldsInBody()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/profile");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("LoginEmail", body);
        Assert.DoesNotContain("ContactEmail", body);
        Assert.DoesNotContain("login@", body);
        Assert.DoesNotContain("contact@", body);
        Assert.DoesNotContain("participant:", body);
    }

    [Fact]
    public async Task Profile_Patch_Unauth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync("/profile", new UpdateProfileRequest
        {
            LoginEmail = "malicious@test.example"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_Get_InvalidAuth_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.jwt.token");

        var response = await client.GetAsync("/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_Get_InvalidApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "ApiKey dlw_invalid_notarealapikey");

        var response = await client.GetAsync("/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Deny-by-Default Tests - Unknown FieldClass (Security point 3)

    [Fact]
    public void FieldPolicy_UnknownFieldClass_DeniedByDefault()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("user-sub");
        var context = FieldResourceContext.ForSelfProfile("user-sub");

        var unknownField = FieldClass.Custom("UnknownSecretField");

        var canRead = fieldPolicy.Evaluate(principal, unknownField, FieldAction.Read, context);
        var canWrite = fieldPolicy.Evaluate(principal, unknownField, FieldAction.Write, context);
        var canList = fieldPolicy.Evaluate(principal, unknownField, FieldAction.List, context);

        Assert.False(canRead, "Unknown FieldClass should be denied Read");
        Assert.False(canWrite, "Unknown FieldClass should be denied Write");
        Assert.False(canList, "Unknown FieldClass should be denied List");
    }

    [Fact]
    public void FieldPolicy_IsRegistered_ReturnsFalseForUnknown()
    {
        var fieldPolicy = new FieldPolicy();

        var unknownField = FieldClass.Custom("NotRegistered");

        Assert.False(fieldPolicy.IsRegistered(unknownField));
    }

    [Fact]
    public void FieldPolicy_IsRegistered_ReturnsTrueForKnown()
    {
        var fieldPolicy = new FieldPolicy();

        Assert.True(fieldPolicy.IsRegistered(FieldClass.LoginEmail));
        Assert.True(fieldPolicy.IsRegistered(FieldClass.ContactEmail));
        Assert.True(fieldPolicy.IsRegistered(FieldClass.DisplayName));
    }

    #endregion

    #region ShareOutbound Tests - Deny in Stage A (Security point 5)

    [Fact]
    public void FieldPolicy_ShareOutbound_DeniedForAll()
    {
        var fieldPolicy = new FieldPolicy();
        var userPrincipal = FieldPrincipal.User("user-sub");
        var context = FieldResourceContext.ForSelfProfile("user-sub");

        var canShareLoginEmail = fieldPolicy.Evaluate(
            userPrincipal, FieldClass.LoginEmail, FieldAction.ShareOutbound, context);
        var canShareContactEmail = fieldPolicy.Evaluate(
            userPrincipal, FieldClass.ContactEmail, FieldAction.ShareOutbound, context);
        var canShareDisplayName = fieldPolicy.Evaluate(
            userPrincipal, FieldClass.DisplayName, FieldAction.ShareOutbound, context);

        Assert.False(canShareLoginEmail, "ShareOutbound should be denied for LoginEmail");
        Assert.False(canShareContactEmail, "ShareOutbound should be denied for ContactEmail (Stage B HOLD)");
        Assert.False(canShareDisplayName, "ShareOutbound should be denied for DisplayName");
    }

    #endregion

    #region Open-Ended Registry Tests (Security point 2)

    [Fact]
    public void FieldClass_IsExtensible()
    {
        var customField1 = FieldClass.Custom("CustomSecret1");
        var customField2 = FieldClass.Custom("CustomSecret2");

        Assert.Equal("CustomSecret1", customField1.Name);
        Assert.Equal("CustomSecret2", customField2.Name);
        Assert.NotEqual(customField1, customField2);
    }

    [Fact]
    public void FieldClass_EqualsWorks()
    {
        var field1 = FieldClass.Custom("Test");
        var field2 = FieldClass.Custom("Test");

        Assert.Equal(field1, field2);
        Assert.True(field1 == field2);
    }

    #endregion

    #region Policy Matrix Tests (Comprehensive)

    [Theory]
    [InlineData("User", true)]
    [InlineData("OwnAgent", false)]
    [InlineData("Counterparty", false)]
    [InlineData("Stranger", false)]
    public void FieldPolicy_LoginEmailMatrix(string principalType, bool expectedRead)
    {
        var fieldPolicy = new FieldPolicy();
        var context = FieldResourceContext.ForNegotiation("owner-sub", "counterparty-sub");
        
        FieldPrincipal principal = principalType switch
        {
            "User" => FieldPrincipal.User("owner-sub"),
            "OwnAgent" => FieldPrincipal.Agent("owner-sub"),
            "Counterparty" => FieldPrincipal.User("counterparty-sub"),
            "Stranger" => FieldPrincipal.User("stranger-sub"),
            _ => throw new ArgumentException($"Unknown principal type: {principalType}")
        };

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Read, context);

        Assert.Equal(expectedRead, canRead);
    }

    [Theory]
    [InlineData("User", true, true)]
    [InlineData("OwnAgent", true, false)]
    [InlineData("Counterparty", false, false)]
    [InlineData("Stranger", false, false)]
    public void FieldPolicy_ContactEmailMatrix(string principalType, bool expectedRead, bool expectedWrite)
    {
        var fieldPolicy = new FieldPolicy();
        var context = FieldResourceContext.ForNegotiation("owner-sub", "counterparty-sub");
        
        FieldPrincipal principal = principalType switch
        {
            "User" => FieldPrincipal.User("owner-sub"),
            "OwnAgent" => FieldPrincipal.Agent("owner-sub"),
            "Counterparty" => FieldPrincipal.User("counterparty-sub"),
            "Stranger" => FieldPrincipal.User("stranger-sub"),
            _ => throw new ArgumentException($"Unknown principal type: {principalType}")
        };

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.ContactEmail, FieldAction.Read, context);
        var canWrite = fieldPolicy.Evaluate(principal, FieldClass.ContactEmail, FieldAction.Write, context);

        Assert.Equal(expectedRead, canRead);
        Assert.Equal(expectedWrite, canWrite);
    }

    #endregion

    #region Projection Omit Tests

    [Fact]
    public void ProfileMapper_OmitsDeniedFields()
    {
        var participant = CreateTestParticipant();
        var agentPrincipal = FieldPrincipal.Agent(participant.Sub);
        var fieldPolicy = new FieldPolicy();

        var response = Dealoware.Application.Profile.Mapping.ProfileMapper.ToResponse(
            participant, agentPrincipal, fieldPolicy);

        Assert.Null(response.LoginEmail);
        Assert.False(response.IncludesLoginEmail);
        Assert.NotNull(response.ContactEmail);
        Assert.True(response.IncludesContactEmail);
    }

    private static Dealoware.Domain.Participants.Participant CreateTestParticipant()
    {
        var participant = Dealoware.Domain.Participants.Participant.Create("Test");
        participant.UpdateLoginEmail("login@test.example");
        participant.UpdateContactEmail("contact@test.example");
        return participant;
    }

    #endregion

    #region Error Hygiene Tests

    [Fact]
    public async Task Profile_DenyError_NoPrivateFields()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "ApiKey dlw_invalid_keythatdoesnotexist");

        var response = await client.GetAsync("/profile");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("LoginEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ContactEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("login@", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contact@", body, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Health Endpoint Unchanged (Security point 1)

    [Fact]
    public async Task Health_StillNoAuthRequired()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}
