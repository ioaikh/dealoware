using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Application.Assistant.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Strategies.Dtos;
using Dealoware.Domain.AgentGateway;
using Dealoware.Domain.Assistant;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Strategies;

namespace Dealoware.Api.Tests;

/// <summary>
/// Stage C (#66) Thin OwnAgent-only Strategy-driven AI Assistant runtime tests.
/// 
/// Spec §8.1 test matrix:
/// 1. Owner OwnAgent OK — Can use own Strategy (StrategyBody via FieldPolicy OwnAgent R/W)
/// 2. Stranger / cross-tenant — Deny; no private Strategy leakage
/// 3. Unauthenticated — Deny (401); no private fields in errors
/// 4. LoginEmail — Never in agent context packs / tool outputs
/// 5. Gateway bind — Runtime path uses platform tools / #67 gateway
/// 
/// Security SD checklist points verified:
/// 1. OwnAgent-only 1:1 — acts only for owning Participant
/// 2. StrategyBody via FieldPolicy — OwnAgent R/W for owner only
/// 3. Mandatory bind #67 — platform tools/gateway only
/// 4. No LoginEmail — never in context packs/tool outputs
/// 5. Authn/IDOR fail-closed — unauth 401; wrong principal 404
/// 6. Soft #41 OUT closed by #66+#67 delivery path
/// 7. X1 thin OUT locked
/// 8. Sibling/Gate HOLDs (#68/#69/#26/#27)
/// 9. Cost $0; no LLM provision
/// 10. Evidence + handshake
/// </summary>
[Collection("WebAppTests")]
public class StageCThinAssistantTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StageCThinAssistantTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Test Helpers

    private async Task<(HttpClient Client, string Sub, string ApiKey)> CreateAuthenticatedClientAsync(string suffix)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"assistant-test-{suffix}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");
        return (client, participant.Sub, participant.ApiKey);
    }

    private static AssistantInvokeRequest CreateInvokeRequest(
        Guid? strategyId = null, 
        string? input = null,
        string? toolName = null) => new()
    {
        StrategyId = strategyId,
        Input = input,
        ToolName = toolName
    };

    #endregion

    #region Spec §8.1 Case 1: Owner OwnAgent OK (Security points 1, 2)

    [Fact]
    public async Task Assistant_Invoke_OwnerOK_Returns200()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"owner-ok-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/assistant/invoke", CreateInvokeRequest());
        var result = await response.Content.ReadFromJsonAsync<AssistantInvokeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Assistant_Invoke_WithOwnStrategy_Returns200WithStrategyContext()
    {
        var (client, sub, _) = await CreateAuthenticatedClientAsync($"owner-strategy-{Guid.NewGuid()}");

        // Create a strategy first
        var strategyResponse = await client.PostAsJsonAsync("/strategies", new CreateStrategyRequest
        {
            Name = "Test Strategy",
            StrategyBody = "Accept offers above $100"
        });
        var strategy = await strategyResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        // Invoke assistant with strategy
        var request = CreateInvokeRequest(strategyId: strategy!.Id);
        var response = await client.PostAsJsonAsync("/assistant/invoke", request);
        var result = await response.Content.ReadFromJsonAsync<AssistantInvokeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.StrategyUsed);
        Assert.Equal(strategy.Id, result.StrategyUsed.StrategyId);
        Assert.Equal("Test Strategy", result.StrategyUsed.StrategyName);
    }

    [Fact]
    public async Task Assistant_Invoke_WithTool_Returns200WithToolResult()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"owner-tool-{Guid.NewGuid()}");

        var request = CreateInvokeRequest(toolName: "get_profile");
        var response = await client.PostAsJsonAsync("/assistant/invoke", request);
        var result = await response.Content.ReadFromJsonAsync<AssistantInvokeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.ToolResult);
        Assert.Equal("get_profile", result.ToolResult.ToolName);
    }

    [Fact]
    public async Task Assistant_Capabilities_OwnerOK_Returns200()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"caps-ok-{Guid.NewGuid()}");

        var response = await client.GetAsync("/assistant/capabilities");
        var result = await response.Content.ReadFromJsonAsync<AssistantCapabilitiesResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.NotEmpty(result.AvailableTools);
    }

    [Fact]
    public async Task Assistant_Capabilities_WithStrategies_ReturnsStrategyCount()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"caps-strat-{Guid.NewGuid()}");

        // Create a strategy
        await client.PostAsJsonAsync("/strategies", new CreateStrategyRequest
        {
            Name = "Cap Test Strategy",
            StrategyBody = "Test body"
        });

        var response = await client.GetAsync("/assistant/capabilities");
        var result = await response.Content.ReadFromJsonAsync<AssistantCapabilitiesResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.HasStrategies);
        Assert.Equal(1, result.StrategyCount);
    }

    #endregion

    #region Spec §8.1 Case 2: Stranger / Cross-Tenant Deny (Security points 1, 2, 5)

    [Fact]
    public async Task Assistant_Invoke_StrangerStrategyDeny_Returns404NoPrivateLeak()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"stranger-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"stranger-{Guid.NewGuid()}");

        // Owner creates a strategy
        var strategyResponse = await ownerClient.PostAsJsonAsync("/strategies", new CreateStrategyRequest
        {
            Name = "Owner's Secret Strategy",
            StrategyBody = "Accept offers above $100, decline below $50"
        });
        var strategy = await strategyResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        // Stranger tries to use owner's strategy
        var request = CreateInvokeRequest(strategyId: strategy!.Id);
        var response = await strangerClient.PostAsJsonAsync("/assistant/invoke", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Owner's Secret Strategy", body);
        Assert.DoesNotContain("Accept offers", body);
        Assert.DoesNotContain("$100", body);
        Assert.DoesNotContain("StrategyBody", body);
    }

    [Fact]
    public async Task Assistant_Invoke_CrossTenantDeny_NoPrivateLeak()
    {
        var (tenantAClient, _, _) = await CreateAuthenticatedClientAsync($"tenant-a-{Guid.NewGuid()}");
        var (tenantBClient, _, _) = await CreateAuthenticatedClientAsync($"tenant-b-{Guid.NewGuid()}");

        // Tenant A creates a strategy
        var strategyResponse = await tenantAClient.PostAsJsonAsync("/strategies", new CreateStrategyRequest
        {
            Name = "Tenant A Strategy",
            StrategyBody = "Confidential negotiation rules"
        });
        var strategy = await strategyResponse.Content.ReadFromJsonAsync<StrategyResponse>();

        // Tenant B tries to access Tenant A's strategy
        var request = CreateInvokeRequest(strategyId: strategy!.Id);
        var response = await tenantBClient.PostAsJsonAsync("/assistant/invoke", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Tenant A Strategy", body);
        Assert.DoesNotContain("Confidential", body);
    }

    [Fact]
    public async Task Assistant_Capabilities_OwnStrategiesOnly_NoOtherUserStrategies()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"caps-own-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"caps-stranger-{Guid.NewGuid()}");

        // Owner creates strategies
        await ownerClient.PostAsJsonAsync("/strategies", new CreateStrategyRequest
        {
            Name = "Owner Strategy 1", StrategyBody = "Body 1"
        });
        await ownerClient.PostAsJsonAsync("/strategies", new CreateStrategyRequest
        {
            Name = "Owner Strategy 2", StrategyBody = "Body 2"
        });

        // Stranger should see 0 strategies (only their own)
        var strangerResponse = await strangerClient.GetAsync("/assistant/capabilities");
        var strangerCaps = await strangerResponse.Content.ReadFromJsonAsync<AssistantCapabilitiesResponse>();

        Assert.Equal(HttpStatusCode.OK, strangerResponse.StatusCode);
        Assert.NotNull(strangerCaps);
        Assert.False(strangerCaps.HasStrategies);
        Assert.Equal(0, strangerCaps.StrategyCount);
    }

    #endregion

    #region Spec §8.1 Case 3: Unauthenticated Deny (Security point 5)

    [Fact]
    public async Task Assistant_Invoke_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/assistant/invoke", CreateInvokeRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assistant_Capabilities_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/assistant/capabilities");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assistant_Invoke_InvalidAuth_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "ApiKey dlw_invalid_keythatdoesnotexist");

        var response = await client.PostAsJsonAsync("/assistant/invoke", CreateInvokeRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assistant_Invoke_InvalidJwt_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.jwt.token");

        var response = await client.PostAsJsonAsync("/assistant/invoke", CreateInvokeRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assistant_UnauthError_NoPrivateFieldsInBody()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/assistant/invoke", CreateInvokeRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("LoginEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StrategyBody", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", body);
    }

    #endregion

    #region Spec §8.1 Case 4: LoginEmail Never in Context (Security point 4)

    [Fact]
    public void FieldPolicy_OwnAgent_DenyLoginEmail()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Read, context);
        var canWrite = fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Write, context);

        Assert.False(canRead, "OwnAgent should be denied LoginEmail Read");
        Assert.False(canWrite, "OwnAgent should be denied LoginEmail Write");
    }

    [Fact]
    public void ToolAllowlist_NoLoginEmailTool()
    {
        var toolAllowlist = new ToolAllowlist();

        Assert.False(toolAllowlist.IsAllowed("get_login_email"));
        Assert.False(toolAllowlist.IsAllowed("login_email"));
        Assert.False(toolAllowlist.IsAllowed("read_login_email"));
    }

    [Fact]
    public void ToolAllowlist_NoToolDeclaresLoginEmail()
    {
        var toolAllowlist = new ToolAllowlist();
        var toolNames = toolAllowlist.GetAllowedToolNames();

        foreach (var toolName in toolNames)
        {
            var tool = toolAllowlist.GetTool(toolName);
            Assert.NotNull(tool);
            Assert.False(tool.DeclaresAccess(FieldClass.LoginEmail, FieldAction.Read),
                $"Tool '{toolName}' should not declare LoginEmail Read");
            Assert.False(tool.DeclaresAccess(FieldClass.LoginEmail, FieldAction.ShareOutbound),
                $"Tool '{toolName}' should not declare LoginEmail ShareOutbound");
        }
    }

    [Fact]
    public async Task Assistant_ToolResult_NoLoginEmail()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"no-login-{Guid.NewGuid()}");

        var request = CreateInvokeRequest(toolName: "get_profile");
        var response = await client.PostAsJsonAsync("/assistant/invoke", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("LoginEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("login_email", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Assistant_Capabilities_NoLoginEmailTool()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"caps-no-login-{Guid.NewGuid()}");

        var response = await client.GetAsync("/assistant/capabilities");
        var result = await response.Content.ReadFromJsonAsync<AssistantCapabilitiesResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.DoesNotContain("login_email", result.AvailableTools, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("get_login_email", result.AvailableTools, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Spec §8.1 Case 5: Gateway Bind (Security point 3)

    [Fact]
    public void AssistantService_RequiresGateway()
    {
        var gateway = new TestAgentGateway();
        var strategyRepo = new TestStrategyRepository();
        var fieldPolicy = new FieldPolicy();

        var service = new AssistantService(gateway, strategyRepo, fieldPolicy);
        Assert.NotNull(service);
    }

    [Fact]
    public async Task AssistantService_UsesGatewayForToolInvocation()
    {
        var gateway = new TestAgentGateway();
        var strategyRepo = new TestStrategyRepository();
        var fieldPolicy = new FieldPolicy();
        var service = new AssistantService(gateway, strategyRepo, fieldPolicy);

        var request = AssistantInvocationRequest.Create(toolName: "get_profile");
        await service.InvokeAsync("owner-sub", request);

        Assert.True(gateway.ToolInvoked);
        Assert.Equal("get_profile", gateway.LastToolName);
    }

    [Fact]
    public async Task AssistantService_RejectsUnallowedTool()
    {
        var gateway = new TestAgentGateway { ToolAvailable = false };
        var strategyRepo = new TestStrategyRepository();
        var fieldPolicy = new FieldPolicy();
        var service = new AssistantService(gateway, strategyRepo, fieldPolicy);

        var request = AssistantInvocationRequest.Create(toolName: "raw_db_query");
        var result = await service.InvokeAsync("owner-sub", request);

        Assert.False(result.Success);
        Assert.Equal("TOOL_NOT_ALLOWED", result.ErrorCode);
        Assert.False(gateway.ToolInvoked);
    }

    [Fact]
    public async Task Assistant_Invoke_UnallowedTool_ReturnsBadRequest()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"unallowed-tool-{Guid.NewGuid()}");

        var request = CreateInvokeRequest(toolName: "raw_db_query");
        var response = await client.PostAsJsonAsync("/assistant/invoke", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Gateway_DenyByDefault_UnknownTool()
    {
        var toolAllowlist = new ToolAllowlist();
        var fieldPolicy = new FieldPolicy();
        var scrubber = new AgentContextScrubber(fieldPolicy);
        var executor = new StubExecutor();
        var gateway = new AgentGateway(toolAllowlist, scrubber, executor);

        Assert.False(gateway.IsToolAvailable("raw_db_query"));
        Assert.False(gateway.IsToolAvailable("arbitrary_http"));
        Assert.False(gateway.IsToolAvailable("direct_db_access"));
    }

    [Fact]
    public void Gateway_AllowlistToolsOnly()
    {
        var toolAllowlist = new ToolAllowlist();
        var fieldPolicy = new FieldPolicy();
        var scrubber = new AgentContextScrubber(fieldPolicy);
        var executor = new StubExecutor();
        var gateway = new AgentGateway(toolAllowlist, scrubber, executor);

        Assert.True(gateway.IsToolAvailable("get_profile"));
        Assert.True(gateway.IsToolAvailable("get_strategy"));
        Assert.True(gateway.IsToolAvailable("get_negotiation"));
    }

    private class StubExecutor : IToolExecutor
    {
        public Task<ToolResponse> ExecuteAsync(AgentTool tool, ToolInvocationRequest request)
        {
            return Task.FromResult(ToolResponse.Ok(tool.Name,
                AgentContextField.ForDisplayName("Test")));
        }
    }

    #endregion

    #region OwnAgent-only 1:1 Tests (Security point 1)

    [Fact]
    public void FieldPrincipal_Agent_IsOwnAgentType()
    {
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var effectiveType = principal.GetTypeForContext(context);

        Assert.True(principal.IsAgent);
        Assert.Equal(PrincipalType.OwnAgent, effectiveType);
    }

    [Fact]
    public void FieldPolicy_OwnAgent_AllowStrategyBody_ForOwner()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context);
        var canWrite = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Write, context);

        Assert.True(canRead, "OwnAgent should be allowed StrategyBody Read for owner");
        Assert.True(canWrite, "OwnAgent should be allowed StrategyBody Write for owner");
    }

    [Fact]
    public void FieldPolicy_OwnAgent_DenyStrategyBody_ForStranger()
    {
        var fieldPolicy = new FieldPolicy();
        var principal = FieldPrincipal.Agent("stranger-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canRead = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context);
        var canWrite = fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Write, context);

        Assert.False(canRead, "Stranger agent should be denied StrategyBody Read");
        Assert.False(canWrite, "Stranger agent should be denied StrategyBody Write");
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

    #region Test Helpers (Stubs)

    private class TestAgentGateway : IAgentGateway
    {
        public bool ToolInvoked { get; private set; }
        public string? LastToolName { get; private set; }
        public bool ToolAvailable { get; set; } = true;

        public Task<GatewayResult> InvokeToolAsync(ToolInvocationRequest request)
        {
            ToolInvoked = true;
            LastToolName = request.ToolName;

            var response = ScrubbedToolResponse.Create(
                request.ToolName,
                true,
                null,
                new List<AgentContextField> { AgentContextField.ForDisplayName("Test") }.AsReadOnly(),
                0);

            return Task.FromResult(GatewayResult.Ok(response));
        }

        public bool IsToolAvailable(string toolName) => ToolAvailable;

        public IEnumerable<string> GetAvailableToolNames()
        {
            return new[] { "get_profile", "get_strategy", "get_negotiation" };
        }
    }

    private class TestStrategyRepository : IStrategyRepository
    {
        private readonly List<Strategy> _strategies = new();

        public Task<Strategy?> GetByIdForOwnerAsync(Guid id, string ownerParticipantId, CancellationToken cancellationToken = default)
        {
            var strategy = _strategies.FirstOrDefault(s => s.Id == id && s.OwnerParticipantId == ownerParticipantId);
            return Task.FromResult(strategy);
        }

        public Task<IReadOnlyList<Strategy>> GetByOwnerAsync(string ownerParticipantId, CancellationToken cancellationToken = default)
        {
            var strategies = _strategies.Where(s => s.OwnerParticipantId == ownerParticipantId).ToList();
            return Task.FromResult<IReadOnlyList<Strategy>>(strategies.AsReadOnly());
        }

        public Task AddAsync(Strategy strategy, CancellationToken cancellationToken = default)
        {
            _strategies.Add(strategy);
            return Task.CompletedTask;
        }

        public void Update(Strategy strategy) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
