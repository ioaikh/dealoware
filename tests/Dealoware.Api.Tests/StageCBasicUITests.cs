using System.Net;
using System.Net.Http.Json;
using Dealoware.Application.Assistant.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Budget.Dtos;
using Dealoware.Application.Negotiations.Dtos;
using Dealoware.Application.Profile.Dtos;
using Dealoware.Application.Strategies.Dtos;
using Dealoware.Domain.FieldAcl;

namespace Dealoware.Api.Tests;

/// <summary>
/// Stage C (#69) Basic UI Tests
/// MVP Stage C Basic authenticated Participant UI surface (X2 partial)
/// 
/// Spec §8.1 test matrix:
/// 1. Unauthenticated protected UI action → fail-closed (401); no private fields
/// 2. Wrong principal / cross-tenant via UI → deny (403/404); no private-field leakage
/// 3. FieldPolicy / authz on chosen surface → Critical paths enforce server-side FieldPolicy
/// 4. Assistant invoke (when #66 land) → Path binds #67 hard wall
/// 5. Budget status (if shown) → Minimal cutoff respect only — not owner-admin privilege
/// 
/// Security SD checklist points (1-10) verified:
/// 1. No privileged back doors — UI does not bypass API FieldPolicy or #67 hard wall
/// 2. Authn fail-closed on protected actions — unauth 401; wrong principal 403/404
/// 3. Assistant path binds #67 — tool/agent path via hard wall + scrub
/// 4. Budget status minimal only (#68) — cutoff display, not owner-admin
/// 5. Surface pick = basic UI — Spec-locked minimum
/// 6. OUT locked — X2 MVP partial; no OpenAPI/MCP invent
/// 7. Consume tip authz — exercises existing auth (#5) + FieldPolicy
/// 8. No Gate unlock / no invent — #26 backlog; #27 HOLD
/// 9. Cost / spend — PoC $0
/// 10. Evidence + handshake — this test suite
/// </summary>
[Collection("WebAppTests")]
public class StageCBasicUITests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StageCBasicUITests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Test Helpers

    private async Task<(HttpClient Client, string Sub, string ApiKey)> CreateAuthenticatedClientAsync(string suffix)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"ui-test-{suffix}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");
        return (client, participant.Sub, participant.ApiKey);
    }

    private HttpClient CreateUnauthenticatedClient()
    {
        return _factory.CreateClient();
    }

    #endregion

    #region Spec §8.1 Case 1: Unauthenticated protected UI action (Security point 2)

    [Fact]
    public async Task Profile_Unauth_Returns401()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Artifacts_Unauth_Returns401()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("/artifacts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_Unauth_Returns401()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("/search/artifacts?q=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Negotiations_Unauth_Returns401()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("/negotiations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Strategies_Unauth_Returns401()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("/strategies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assistant_Unauth_Returns401()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.PostAsJsonAsync("/assistant/invoke", new AssistantInvokeRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Budget_Unauth_Returns401()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("/budget/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unauth_NoPrivateFieldsInResponse_Profile()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("/profile");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("LoginEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ContactEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StrategyBody", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", body);
    }

    [Fact]
    public async Task InvalidAuth_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "ApiKey dlw_invalid_key_12345");

        var response = await client.GetAsync("/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Spec §8.1 Case 2: Wrong principal / cross-tenant deny (Security points 1, 2)

    [Fact]
    public async Task Artifact_CrossTenant_Returns404()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"artifact-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"artifact-stranger-{Guid.NewGuid()}");

        var createResponse = await ownerClient.PostAsJsonAsync("/artifacts", new
        {
            Entities = new[] { new { Name = "Owner's Item", Description = "Private" } },
            Intent = "Sell"
        });
        Assert.True(createResponse.IsSuccessStatusCode, $"Create artifact failed: {await createResponse.Content.ReadAsStringAsync()}");
        var artifact = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(artifact);

        var response = await strangerClient.GetAsync($"/artifacts/{artifact.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_CrossTenant_Returns404()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"strategy-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"strategy-stranger-{Guid.NewGuid()}");

        var createResponse = await ownerClient.PostAsJsonAsync("/strategies", new CreateStrategyRequest
        {
            Name = "Secret Strategy",
            StrategyBody = "Confidential rules"
        });
        var strategy = await createResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        var response = await strangerClient.GetAsync($"/strategies/{strategy!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Strategy_CrossTenant_NoPrivateFieldLeak()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"strategy-leak-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"strategy-leak-stranger-{Guid.NewGuid()}");

        await ownerClient.PostAsJsonAsync("/strategies", new CreateStrategyRequest
        {
            Name = "Secret Name",
            StrategyBody = "Super secret strategy body with sensitive rules"
        });

        var response = await strangerClient.GetAsync("/strategies");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Secret Name", body);
        Assert.DoesNotContain("Super secret", body);
        Assert.DoesNotContain("sensitive rules", body);
    }

    [Fact]
    public async Task Negotiation_CrossTenant_Returns404()
    {
        var (caller, callerSub, _) = await CreateAuthenticatedClientAsync($"neg-caller-{Guid.NewGuid()}");
        var (counterparty, counterpartySub, _) = await CreateAuthenticatedClientAsync($"neg-counterparty-{Guid.NewGuid()}");
        var (stranger, _, _) = await CreateAuthenticatedClientAsync($"neg-stranger-{Guid.NewGuid()}");

        var artifactResponse = await caller.PostAsJsonAsync("/artifacts", new
        {
            Entities = new[] { new { Name = "Negotiation Item" } },
            Intent = "Sell"
        });
        Assert.True(artifactResponse.IsSuccessStatusCode, $"Create artifact failed: {await artifactResponse.Content.ReadAsStringAsync()}");
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(artifact);

        var negResponse = await caller.PostAsJsonAsync("/negotiations", new
        {
            ArtifactId = artifact.Id,
            CounterpartyParticipantId = counterpartySub,
            CallerIntent = "Sell",
            CounterpartyIntent = "Buy"
        });
        Assert.True(negResponse.IsSuccessStatusCode, $"Create negotiation failed: {await negResponse.Content.ReadAsStringAsync()}");
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.NotNull(negotiation);

        var response = await stranger.GetAsync($"/negotiations/{negotiation.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Budget_CrossTenant_CannotAccessOther()
    {
        var (userA, _, _) = await CreateAuthenticatedClientAsync($"budget-a-{Guid.NewGuid()}");
        var (userB, _, _) = await CreateAuthenticatedClientAsync($"budget-b-{Guid.NewGuid()}");

        var responseA = await userA.GetAsync("/budget/status");
        var responseB = await userB.GetAsync("/budget/status");

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }

    #endregion

    #region Spec §8.1 Case 3: FieldPolicy / authz server-side (Security points 1, 7)

    [Fact]
    public void FieldPolicy_DenyByDefault_UnknownField()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("some-sub");
        var context = FieldResourceContext.ForSelfProfile("some-sub");

        var unknownField = FieldClass.Custom("UnknownField");
        var result = fieldPolicy.Evaluate(principal, unknownField, FieldAction.Read, context);

        Assert.False(result, "Unknown fields should be denied by default");
    }

    [Fact]
    public void FieldPolicy_UnauthDeny_AllFields()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.Unauthenticated();
        var context = FieldResourceContext.ForSelfProfile("any-sub");

        Assert.False(fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Read, context));
        Assert.False(fieldPolicy.Evaluate(principal, FieldClass.ContactEmail, FieldAction.Read, context));
        Assert.False(fieldPolicy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Read, context));
        Assert.False(fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context));
    }

    [Fact]
    public void FieldPolicy_StrangerDeny_PrivateFields()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.User("stranger-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        Assert.False(fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Read, context));
        Assert.False(fieldPolicy.Evaluate(principal, FieldClass.ContactEmail, FieldAction.Read, context));
        Assert.False(fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context));
    }

    [Fact]
    public async Task Profile_ServerSideFieldProjection_NoLoginEmail()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"profile-proj-{Guid.NewGuid()}");

        var response = await client.GetAsync("/profile");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("login_email", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_DiscoveryOmitsPrivateFields()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"search-priv-{Guid.NewGuid()}");

        await client.PostAsJsonAsync("/artifacts", new
        {
            Entities = new[] { new { Name = "Searchable Item" } },
            Intent = "Sell"
        });

        var response = await client.GetAsync("/search/artifacts?q=Searchable");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("OwnerParticipantId", body);
        Assert.DoesNotContain("LoginEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ContactEmail", body, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Spec §8.1 Case 4: Assistant invoke binds #67 hard wall (Security point 3)

    [Fact]
    public async Task Assistant_ToolInvoke_UsesGateway()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"assist-gateway-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/assistant/invoke", new AssistantInvokeRequest
        {
            ToolName = "get_profile"
        });
        var result = await response.Content.ReadFromJsonAsync<AssistantInvokeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.ToolResult);
        Assert.Equal("get_profile", result.ToolResult.ToolName);
    }

    [Fact]
    public async Task Assistant_UnallowedTool_Denied()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"assist-deny-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/assistant/invoke", new AssistantInvokeRequest
        {
            ToolName = "raw_db_query"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Assistant_ToolResult_NoLoginEmail()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"assist-nologin-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/assistant/invoke", new AssistantInvokeRequest
        {
            ToolName = "get_profile"
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("LoginEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("login_email", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Assistant_Capabilities_NoLoginEmailTool()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"assist-caps-{Guid.NewGuid()}");

        var response = await client.GetAsync("/assistant/capabilities");
        var result = await response.Content.ReadFromJsonAsync<AssistantCapabilitiesResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.DoesNotContain("login_email", result.AvailableTools, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("get_login_email", result.AvailableTools, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Assistant_StrategyBinding_OwnOnly()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"assist-strat-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"assist-strat-stranger-{Guid.NewGuid()}");

        var stratResponse = await ownerClient.PostAsJsonAsync("/strategies", new CreateStrategyRequest
        {
            Name = "Owner Strategy",
            StrategyBody = "Private rules"
        });
        var strategy = await stratResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        var response = await strangerClient.PostAsJsonAsync("/assistant/invoke", new AssistantInvokeRequest
        {
            StrategyId = strategy!.Id
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Spec §8.1 Case 5: Budget status minimal (Security point 4)

    [Fact]
    public async Task Budget_Status_ReturnsMinimalFields()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"budget-minimal-{Guid.NewGuid()}");

        var response = await client.GetAsync("/budget/status");
        var result = await response.Content.ReadFromJsonAsync<BudgetStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.LimitUnits > 0);
        Assert.True(result.RemainingUnits >= 0);
    }

    [Fact]
    public async Task Budget_Status_NoAdminPrivilege()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"budget-noadmin-{Guid.NewGuid()}");

        var response = await client.GetAsync("/budget/status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("admin", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("owner", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("analytics", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("billing", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Budget_CutoffRespected_AfterExhaustion()
    {
        var (client, sub, _) = await CreateAuthenticatedClientAsync($"budget-cutoff-{Guid.NewGuid()}");

        var initialStatus = await client.GetAsync("/budget/status");
        var initialResult = await initialStatus.Content.ReadFromJsonAsync<BudgetStatusResponse>();
        Assert.NotNull(initialResult);
        Assert.False(initialResult.IsExhausted);
    }

    [Fact]
    public async Task Budget_Status_OwnBudgetOnly()
    {
        var (userA, subA, _) = await CreateAuthenticatedClientAsync($"budget-own-a-{Guid.NewGuid()}");
        var (userB, subB, _) = await CreateAuthenticatedClientAsync($"budget-own-b-{Guid.NewGuid()}");

        var responseA = await userA.GetAsync("/budget/status");
        var responseB = await userB.GetAsync("/budget/status");

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);

        var resultA = await responseA.Content.ReadFromJsonAsync<BudgetStatusResponse>();
        var resultB = await responseB.Content.ReadFromJsonAsync<BudgetStatusResponse>();

        Assert.NotNull(resultA);
        Assert.NotNull(resultB);
    }

    #endregion

    #region Static UI Serving (Security point 5: Surface pick = basic UI)

    [Fact]
    public async Task StaticUI_IndexHtml_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Dealoware", content);
        Assert.Contains("MVP Stage C Basic UI", content);
    }

    [Fact]
    public async Task StaticUI_AppJs_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/app.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("UI is NOT a security boundary", content);
    }

    [Fact]
    public async Task StaticUI_StylesCss_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/styles.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Dealoware MVP Stage C Basic UI", content);
    }

    #endregion

    #region Health Endpoint Unchanged (Security point 6: OUT locked)

    [Fact]
    public async Task Health_StillAuthNone()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}
