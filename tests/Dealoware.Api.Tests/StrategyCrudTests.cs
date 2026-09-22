using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Strategies.Dtos;
using Dealoware.Domain.FieldAcl;

namespace Dealoware.Api.Tests;

/// <summary>
/// Tests for MVP Stage B Strategy CRUD (#41).
/// 
/// Security SD checklist points verified:
/// 1. Owner-scoped CRUD on query plane (OwnerParticipantId == sub)
/// 2. StrategyBody FieldClass: User R/W; OwnAgent R/W; Counterparty Deny
/// 3. IDOR fail-closed (404 not 403)
/// 4. Unauthenticated deny (401)
/// 5. Wrong principal deny (404 - no info leak)
/// 6. Uniform deny bodies with no private field leakage
/// 7. Negotiation DTOs never expose StrategyBody
/// </summary>
[Collection("WebAppTests")]
public class StrategyCrudTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StrategyCrudTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Test Helpers

    private async Task<(HttpClient Client, string Sub, string ApiKey)> CreateAuthenticatedClientAsync(string suffix)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"strategy-test-{suffix}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");
        return (client, participant.Sub, participant.ApiKey);
    }

    private static CreateStrategyRequest CreateStrategyRequest(string? name = null, string? body = null) => new()
    {
        Name = name ?? "Test Strategy",
        StrategyBody = body ?? "Accept offers above $100, decline below $50, counter otherwise"
    };

    #endregion

    #region Owner CRUD OK Tests (Security point 1)

    [Fact]
    public async Task Strategy_Create_OwnerOK_Returns201()
    {
        var (client, sub, _) = await CreateAuthenticatedClientAsync($"create-ok-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/strategies", CreateStrategyRequest());
        var strategy = await response.Content.ReadFromJsonAsync<StrategyResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(strategy);
        Assert.NotEqual(Guid.Empty, strategy.Id);
        Assert.Equal(sub, strategy.OwnerParticipantId);
        Assert.Equal("Test Strategy", strategy.Name);
        Assert.True(strategy.IncludesStrategyBody);
        Assert.NotNull(strategy.StrategyBody);
    }

    [Fact]
    public async Task Strategy_Get_OwnerOK_Returns200()
    {
        var (client, sub, _) = await CreateAuthenticatedClientAsync($"get-ok-{Guid.NewGuid()}");

        var createResponse = await client.PostAsJsonAsync("/strategies", CreateStrategyRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        var response = await client.GetAsync($"/strategies/{created!.Id}");
        var strategy = await response.Content.ReadFromJsonAsync<StrategyResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(strategy);
        Assert.Equal(created.Id, strategy.Id);
        Assert.Equal(sub, strategy.OwnerParticipantId);
        Assert.True(strategy.IncludesStrategyBody);
    }

    [Fact]
    public async Task Strategy_List_OwnerOK_ReturnsOnlyOwnStrategies()
    {
        var (ownerClient, ownerSub, _) = await CreateAuthenticatedClientAsync($"list-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"list-stranger-{Guid.NewGuid()}");

        await ownerClient.PostAsJsonAsync("/strategies", CreateStrategyRequest("Strategy 1"));
        await ownerClient.PostAsJsonAsync("/strategies", CreateStrategyRequest("Strategy 2"));
        await strangerClient.PostAsJsonAsync("/strategies", CreateStrategyRequest("Stranger Strategy"));

        var response = await ownerClient.GetAsync("/strategies");
        var strategies = await response.Content.ReadFromJsonAsync<List<StrategyResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(strategies);
        Assert.Equal(2, strategies.Count);
        Assert.All(strategies, s => Assert.Equal(ownerSub, s.OwnerParticipantId));
    }

    [Fact]
    public async Task Strategy_Update_OwnerOK_Returns200()
    {
        var (client, sub, _) = await CreateAuthenticatedClientAsync($"update-ok-{Guid.NewGuid()}");

        var createResponse = await client.PostAsJsonAsync("/strategies", CreateStrategyRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        var updateRequest = new UpdateStrategyRequest
        {
            Name = "Updated Strategy",
            StrategyBody = "New strategy body"
        };
        var response = await client.PutAsJsonAsync($"/strategies/{created!.Id}", updateRequest);
        var strategy = await response.Content.ReadFromJsonAsync<StrategyResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(strategy);
        Assert.Equal("Updated Strategy", strategy.Name);
        Assert.Equal("New strategy body", strategy.StrategyBody);
    }

    [Fact]
    public async Task Strategy_Patch_OwnerOK_Returns200()
    {
        var (client, sub, _) = await CreateAuthenticatedClientAsync($"patch-ok-{Guid.NewGuid()}");

        var createResponse = await client.PostAsJsonAsync("/strategies", 
            CreateStrategyRequest("Original Name", "Original Body"));
        var created = await createResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        var patchRequest = new UpdateStrategyRequest { Name = "Patched Name" };
        var response = await client.PatchAsJsonAsync($"/strategies/{created!.Id}", patchRequest);
        var strategy = await response.Content.ReadFromJsonAsync<StrategyResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(strategy);
        Assert.Equal("Patched Name", strategy.Name);
        Assert.Equal("Original Body", strategy.StrategyBody);
    }

    [Fact]
    public async Task Strategy_Delete_OwnerOK_Returns204()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"delete-ok-{Guid.NewGuid()}");

        var createResponse = await client.PostAsJsonAsync("/strategies", CreateStrategyRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        var deleteResponse = await client.DeleteAsync($"/strategies/{created!.Id}");
        var getResponse = await client.GetAsync($"/strategies/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    #endregion

    #region Stranger/IDOR Deny Tests (Security points 3, 5)

    [Fact]
    public async Task Strategy_Get_StrangerDeny_Returns404NoPrivateFields()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"stranger-get-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"stranger-get-{Guid.NewGuid()}");

        var createResponse = await ownerClient.PostAsJsonAsync("/strategies", CreateStrategyRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        var response = await strangerClient.GetAsync($"/strategies/{created!.Id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("StrategyBody", body);
        Assert.DoesNotContain("OwnerParticipantId", body);
        Assert.DoesNotContain("participant:", body);
        Assert.DoesNotContain("Accept offers", body);
    }

    [Fact]
    public async Task Strategy_Update_StrangerDeny_Returns404()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"stranger-update-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"stranger-update-{Guid.NewGuid()}");

        var createResponse = await ownerClient.PostAsJsonAsync("/strategies", CreateStrategyRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        var updateRequest = new UpdateStrategyRequest { Name = "Hacked!" };
        var response = await strangerClient.PutAsJsonAsync($"/strategies/{created!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_Delete_StrangerDeny_Returns404()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"stranger-delete-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"stranger-delete-{Guid.NewGuid()}");

        var createResponse = await ownerClient.PostAsJsonAsync("/strategies", CreateStrategyRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        var response = await strangerClient.DeleteAsync($"/strategies/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_Get_IDORDeny_Returns404()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"idor-{Guid.NewGuid()}");

        var response = await client.GetAsync($"/strategies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_List_StrangerDeny_ReturnsEmptyListNotForeignRows()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"list-empty-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"list-empty-stranger-{Guid.NewGuid()}");

        await ownerClient.PostAsJsonAsync("/strategies", CreateStrategyRequest("Owner Strategy 1"));
        await ownerClient.PostAsJsonAsync("/strategies", CreateStrategyRequest("Owner Strategy 2"));

        var response = await strangerClient.GetAsync("/strategies");
        var strategies = await response.Content.ReadFromJsonAsync<List<StrategyResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(strategies);
        Assert.Empty(strategies);
    }

    #endregion

    #region Unauthenticated Deny Tests (Security point 4)

    [Fact]
    public async Task Strategy_Create_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/strategies", CreateStrategyRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_Get_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/strategies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_List_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/strategies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_Update_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/strategies/{Guid.NewGuid()}", new UpdateStrategyRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_Delete_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/strategies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_Get_InvalidAuth_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "ApiKey dlw_invalid_keythatdoesnotexist");

        var response = await client.GetAsync($"/strategies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_Get_InvalidJwt_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.jwt.token");

        var response = await client.GetAsync($"/strategies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Uniform Deny Body Tests - No Private Field Leakage (Security point 6)

    [Fact]
    public async Task Strategy_DenyError_NoPrivateFields()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "ApiKey dlw_invalid_keythatdoesnotexist");

        var response = await client.GetAsync($"/strategies/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("StrategyBody", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OwnerParticipantId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("participant:", body, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region StrategyBody ACL Tests (Security point 2)

    [Fact]
    public void FieldPolicy_StrategyBody_UserOK_ReadWrite()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context);
        var canWrite = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Write, context);
        var canList = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.List, context);

        Assert.True(canRead, "User should be allowed StrategyBody Read");
        Assert.True(canWrite, "User should be allowed StrategyBody Write");
        Assert.True(canList, "User should be allowed StrategyBody List");
    }

    [Fact]
    public void FieldPolicy_StrategyBody_OwnAgentOK_ReadWrite()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context);
        var canWrite = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Write, context);
        var canList = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.List, context);

        Assert.True(canRead, "OwnAgent should be allowed StrategyBody Read");
        Assert.True(canWrite, "OwnAgent should be allowed StrategyBody Write");
        Assert.True(canList, "OwnAgent should be allowed StrategyBody List");
    }

    [Fact]
    public void FieldPolicy_StrategyBody_CounterpartyDeny()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("counterparty-sub");
        var context = FieldResourceContext.ForNegotiation("owner-sub", "counterparty-sub");

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context);
        var canWrite = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Write, context);

        Assert.False(canRead, "Counterparty should be denied StrategyBody Read");
        Assert.False(canWrite, "Counterparty should be denied StrategyBody Write");
    }

    [Fact]
    public void FieldPolicy_StrategyBody_StrangerDeny()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("stranger-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context);
        var canWrite = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Write, context);

        Assert.False(canRead, "Stranger should be denied StrategyBody Read");
        Assert.False(canWrite, "Stranger should be denied StrategyBody Write");
    }

    [Fact]
    public void FieldPolicy_StrategyBody_UnauthDeny()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.Unauthenticated();
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context);
        var canWrite = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Write, context);

        Assert.False(canRead, "Unauthenticated should be denied StrategyBody Read");
        Assert.False(canWrite, "Unauthenticated should be denied StrategyBody Write");
    }

    [Fact]
    public void FieldPolicy_StrategyBody_ShareOutboundDeny()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canShare = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.ShareOutbound, context);

        Assert.False(canShare, "ShareOutbound should be denied for StrategyBody");
    }

    [Theory]
    [InlineData("User", true)]
    [InlineData("OwnAgent", true)]
    [InlineData("Counterparty", false)]
    [InlineData("Stranger", false)]
    public void FieldPolicy_StrategyBodyMatrix(string principalType, bool expectedAllow)
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

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context);

        Assert.Equal(expectedAllow, canRead);
    }

    #endregion

    #region Query-Plane Verification Tests

    [Fact]
    public async Task Strategy_GetByIdForOwner_QueryPlaneFilter_NotFetchThenFilter()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"qp-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"qp-stranger-{Guid.NewGuid()}");

        var createResponse = await ownerClient.PostAsJsonAsync("/strategies", CreateStrategyRequest());
        var strategy = await createResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        var response = await strangerClient.GetAsync($"/strategies/{strategy!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Health Endpoint Unchanged

    [Fact]
    public async Task Health_StillNoAuthRequired()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Negotiation DTO Never Exposes StrategyBody (Security point 7)

    [Fact]
    public async Task Negotiation_Response_NeverExposesStrategyBody()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"neg-strat-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"neg-strat-b-{Guid.NewGuid()}");

        await clientA.PostAsJsonAsync("/strategies", CreateStrategyRequest("My Secret Strategy"));

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", new
        {
            Entities = new[] { new { Name = "Test Item", Description = "For negotiation" } },
            Intent = "sell"
        });
        var artifactJson = await artifactResponse.Content.ReadAsStringAsync();
        using var artifactDoc = JsonDocument.Parse(artifactJson);
        var artifactId = Guid.Parse(artifactDoc.RootElement.GetProperty("id").GetString()!);

        var negotiationResponse = await clientA.PostAsJsonAsync("/negotiations", new
        {
            ArtifactId = artifactId,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiationJson = await negotiationResponse.Content.ReadAsStringAsync();
        using var negotiationDoc = JsonDocument.Parse(negotiationJson);
        var negotiationId = Guid.Parse(negotiationDoc.RootElement.GetProperty("id").GetString()!);

        var getResponse = await clientA.GetAsync($"/negotiations/{negotiationId}");
        var body = await getResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("StrategyBody", body);
        Assert.DoesNotContain("My Secret Strategy", body);
        Assert.DoesNotContain("Accept offers", body);
    }

    [Fact]
    public async Task NegotiationList_Response_NeverExposesStrategyBody()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"neg-list-strat-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"neg-list-strat-b-{Guid.NewGuid()}");

        await clientA.PostAsJsonAsync("/strategies", CreateStrategyRequest("Secret Negotiation Strategy"));

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", new
        {
            Entities = new[] { new { Name = "Test Item", Description = "For listing" } },
            Intent = "sell"
        });
        var artifactJson = await artifactResponse.Content.ReadAsStringAsync();
        using var artifactDoc = JsonDocument.Parse(artifactJson);
        var artifactId = Guid.Parse(artifactDoc.RootElement.GetProperty("id").GetString()!);

        await clientA.PostAsJsonAsync("/negotiations", new
        {
            ArtifactId = artifactId,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });

        var listResponse = await clientA.GetAsync("/negotiations");
        var body = await listResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("StrategyBody", body);
        Assert.DoesNotContain("Secret Negotiation Strategy", body);
    }

    #endregion
}
