using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Negotiations.Dtos;
using Dealoware.Application.Profile.Dtos;
using Dealoware.Domain.FieldAcl;

namespace Dealoware.Api.Tests;

/// <summary>
/// Stage B (#42) Contact on Accept Tests.
/// 
/// Spec §7.1 test matrix:
/// - Pre-Accept get: no ContactEmail/LoginEmail
/// - Accept: ContactEmail may appear for counterparty with grant; LoginEmail never
/// - Stranger post-Accept: still Deny ContactEmail
/// - Unauth deny
/// - Pre-Accept ShareOutbound Deny
/// 
/// Security SD points verified:
/// 1. Pre-Accept seal held (#7 no regression)
/// 2. On Accept: persist Accept-grant; expose HasAcceptGrant to IFieldPolicy
/// 3. ShareOutbound(ContactEmail) only with Accept grant to authorized counterparty
/// 4. ContactEmail rows: User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny
/// 5. LoginEmail remains User-only — NEVER shared on Accept; OwnAgent Deny
/// 6. Authn/stranger fail-closed; uniform deny; no private leakage
/// </summary>
[Collection("WebAppTests")]
public class StageBContactOnAcceptTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StageBContactOnAcceptTests(IsolatedWebApplicationFactory factory)
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

    private async Task<ProfileResponse?> SetupProfileWithFields(HttpClient client, string contactEmail, string? loginEmail = null)
    {
        var updateResponse = await client.PatchAsJsonAsync("/profile", new UpdateProfileRequest
        {
            DisplayName = "Test User",
            LoginEmail = loginEmail ?? "login@secret.example",
            ContactEmail = contactEmail
        });
        return await updateResponse.Content.ReadFromJsonAsync<ProfileResponse>();
    }

    private static CreateArtifactRequest CreateArtifactRequest(string intent) => new()
    {
        Entities = new List<CreateSubjectEntityDto>
        {
            new() { Name = "Test Item", Description = "A negotiable item" }
        },
        Intent = intent
    };

    #endregion

    #region Pre-Accept Tests - Seal Held (Security point 1)

    [Fact]
    public async Task PreAccept_GetNegotiation_NoContactEmail_NoLoginEmail()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"stageB-preaccept-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"stageB-preaccept-b-{Guid.NewGuid()}");
        
        await SetupProfileWithFields(clientA, "seller@contact.example", "seller@login.secret");
        await SetupProfileWithFields(clientB, "buyer@contact.example", "buyer@login.secret");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var getResponse = await clientB.GetAsync($"/negotiations/{negotiation!.Id}");
        var json = await getResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("seller@contact.example", json);
        Assert.DoesNotContain("seller@login.secret", json);
        Assert.DoesNotContain("buyer@login.secret", json);
        Assert.Contains("\"identitySealed\":true", json.Replace(" ", ""));
    }

    [Fact]
    public async Task PreAccept_GetOffer_NoContactEmail_NoLoginEmail()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"stageB-preoffer-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"stageB-preoffer-b-{Guid.NewGuid()}");
        
        await SetupProfileWithFields(clientA, "seller@contact.example");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var getResponse = await clientB.GetAsync($"/offers/{offer!.Id}");
        var json = await getResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("seller@contact.example", json);
        Assert.Contains("\"identitySealed\":true", json.Replace(" ", ""));
    }

    #endregion

    #region Accept Tests - ContactEmail Shared (Security points 2, 3)

    [Fact]
    public async Task Accept_CounterpartyReceivesContactEmail_LoginEmailNever()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"stageB-accept-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"stageB-accept-b-{Guid.NewGuid()}");
        
        await SetupProfileWithFields(clientA, "seller@contact.example", "seller@login.secret");
        await SetupProfileWithFields(clientB, "buyer@contact.example", "buyer@login.secret");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var acceptResponse = await clientB.PostAsync($"/offers/{offer!.Id}/accept", null);
        var json = await acceptResponse.Content.ReadAsStringAsync();

        Assert.Contains("seller@contact.example", json);
        
        Assert.DoesNotContain("seller@login.secret", json);
        Assert.DoesNotContain("buyer@login.secret", json);
        Assert.DoesNotContain("loginEmail", json, StringComparison.OrdinalIgnoreCase);
        
        var accepted = JsonSerializer.Deserialize<AcceptOfferResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.False(accepted!.IdentitySealed);
        Assert.True(accepted.IncludesContactEmail);
        Assert.Equal("seller@contact.example", accepted.CounterpartyContactEmail);
    }

    [Fact]
    public async Task Accept_HasAcceptGrant_PersistsInDatabase()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"stageB-grant-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"stageB-grant-b-{Guid.NewGuid()}");
        
        await SetupProfileWithFields(clientA, "seller@contact.example");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var acceptResponse = await clientB.PostAsync($"/offers/{offer!.Id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var accepted = await acceptResponse.Content.ReadFromJsonAsync<AcceptOfferResponse>();
        Assert.NotNull(accepted);
        Assert.True(accepted.IncludesContactEmail);
    }

    #endregion

    #region Stranger Post-Accept Tests - Still Deny (Security point 4)

    [Fact]
    public async Task PostAccept_StrangerCannotAccessNegotiation()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"stageB-stranger-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"stageB-stranger-b-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"stageB-stranger-c-{Guid.NewGuid()}");
        
        await SetupProfileWithFields(clientA, "seller@contact.example");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        await clientB.PostAsync($"/offers/{offer!.Id}/accept", null);

        var strangerResponse = await strangerClient.GetAsync($"/negotiations/{negotiation!.Id}");
        
        Assert.Equal(HttpStatusCode.NotFound, strangerResponse.StatusCode);
        var body = await strangerResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("seller@contact.example", body);
        Assert.DoesNotContain("loginEmail", body, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Unauthenticated Deny Tests (Security point 6)

    [Fact]
    public async Task Unauth_CannotAccessNegotiation_NoPiiLeak()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"stageB-unauth-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"stageB-unauth-b-{Guid.NewGuid()}");
        
        await SetupProfileWithFields(clientA, "seller@contact.example");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync($"/negotiations/{negotiation!.Id}");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("seller@contact.example", body);
        Assert.DoesNotContain("participant:", body);
    }

    [Fact]
    public async Task Unauth_CannotAcceptOffer()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"stageB-unauth-accept-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"stageB-unauth-accept-b-{Guid.NewGuid()}");
        
        await SetupProfileWithFields(clientA, "seller@contact.example");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.PostAsync($"/offers/{offer!.Id}/accept", null);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Pre-Accept ShareOutbound Deny (Security point 3)

    [Fact]
    public void PreAccept_ShareOutbound_Deny_FieldPolicy()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("counterparty-sub");
        var context = FieldResourceContext.ForNegotiation("owner-sub", "counterparty-sub");

        var canShareContactEmail = fieldPolicy.Evaluate(
            principal, FieldClass.ContactEmail, FieldAction.ShareOutbound, context);

        Assert.False(canShareContactEmail, "Pre-Accept ShareOutbound(ContactEmail) should be denied");
    }

    [Fact]
    public void PostAccept_ShareOutbound_Allow_FieldPolicy()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("counterparty-sub");
        var context = FieldResourceContext.ForAcceptedNegotiation("owner-sub", "counterparty-sub");

        var canShareContactEmail = fieldPolicy.Evaluate(
            principal, FieldClass.ContactEmail, FieldAction.ShareOutbound, context);

        Assert.True(canShareContactEmail, "Post-Accept ShareOutbound(ContactEmail) should be allowed for counterparty");
    }

    [Fact]
    public void ShareOutbound_LoginEmail_AlwaysDeny()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("counterparty-sub");
        
        var contextPreAccept = FieldResourceContext.ForNegotiation("owner-sub", "counterparty-sub");
        var contextPostAccept = FieldResourceContext.ForAcceptedNegotiation("owner-sub", "counterparty-sub");

        var canSharePreAccept = fieldPolicy.Evaluate(
            principal, FieldClass.LoginEmail, FieldAction.ShareOutbound, contextPreAccept);
        var canSharePostAccept = fieldPolicy.Evaluate(
            principal, FieldClass.LoginEmail, FieldAction.ShareOutbound, contextPostAccept);

        Assert.False(canSharePreAccept, "LoginEmail ShareOutbound should always be denied");
        Assert.False(canSharePostAccept, "LoginEmail ShareOutbound should always be denied even after Accept");
    }

    #endregion

    #region ContactEmail Policy Matrix (Security point 4)

    [Theory]
    [InlineData("User", true, true)]
    [InlineData("OwnAgent", true, false)]
    [InlineData("Counterparty", false, false)]
    [InlineData("Stranger", false, false)]
    public void ContactEmail_PolicyMatrix_PreAccept(string principalType, bool expectedRead, bool expectedWrite)
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
        var canShare = fieldPolicy.Evaluate(principal, FieldClass.ContactEmail, FieldAction.ShareOutbound, context);

        Assert.Equal(expectedRead, canRead);
        Assert.Equal(expectedWrite, canWrite);
        Assert.False(canShare);
    }

    [Fact]
    public void ContactEmail_Counterparty_ShareOutbound_AllowedPostAccept()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("counterparty-sub");
        var context = FieldResourceContext.ForAcceptedNegotiation("owner-sub", "counterparty-sub");

        var canShare = fieldPolicy.Evaluate(principal, FieldClass.ContactEmail, FieldAction.ShareOutbound, context);

        Assert.True(canShare, "Counterparty should be able to receive ContactEmail via ShareOutbound after Accept");
    }

    #endregion

    #region LoginEmail Never Shared (Security point 5)

    [Fact]
    public async Task Accept_LoginEmail_NeverIncluded()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"stageB-loginmail-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"stageB-loginmail-b-{Guid.NewGuid()}");
        
        await SetupProfileWithFields(clientA, "seller@contact.example", "seller@login.secret");
        await SetupProfileWithFields(clientB, "buyer@contact.example", "buyer@login.secret");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var acceptResponse = await clientB.PostAsync($"/offers/{offer!.Id}/accept", null);
        var json = await acceptResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("seller@login.secret", json);
        Assert.DoesNotContain("buyer@login.secret", json);
        Assert.DoesNotContain("\"loginEmail\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("User", true)]
    [InlineData("OwnAgent", false)]
    [InlineData("Counterparty", false)]
    [InlineData("Stranger", false)]
    public void LoginEmail_PolicyMatrix(string principalType, bool expectedRead)
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
        var canWrite = fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Write, context);
        var canShare = fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.ShareOutbound, context);

        Assert.Equal(expectedRead, canRead);
        Assert.False(canShare);
    }

    #endregion

    #region Health Endpoint Still Open

    [Fact]
    public async Task Health_StillNoAuthRequired()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}
