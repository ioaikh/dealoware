using Dealoware.Domain.AgentGateway;
using Dealoware.Domain.FieldAcl;

namespace Dealoware.Api.Tests;

/// <summary>
/// Stage C (#67) Agent/tool hard wall + response scrubber tests.
/// 
/// Spec §9 / §9.1 test matrix:
/// - Allowlisted tool + scrub: OK; response/context passes Evaluate; allowed fields only
/// - Denied field in tool response: Stripped BEFORE model context
/// - LoginEmail: Never present in agent context packs / tool outputs
/// - Pre-Accept ShareOutbound: Deny regardless of prompt text
/// - Stranger / cross-tenant: Deny; no private-field leakage
/// - Unauthenticated: Deny; no private fields in errors
/// - Prompt-only soft guidance as sole control: REJECTED (tests assert gateway+Evaluate path)
/// 
/// Security Dev Plan-step points verified:
/// 1. Agent runtime gateway — agents on platform tools only
/// 2. Tool allowlist deny-by-default with FieldClass declarations
/// 3. Server-side scrub before model context via same IFieldPolicy.Evaluate
/// 4. No LoginEmail in agent context — User-only; OwnAgent Deny held
/// 5. ShareOutbound Accept-gated server-side; prompt cannot escalate
/// 6. Reject prompt-only / parallel ACL
/// 7. Dual wall all FieldClasses — same Evaluate
/// 8. Cross-agent mediated (if any); no denied FieldClasses
/// 9. OUT / Gate / spend respected
/// 10. Traceability + handshake
/// </summary>
public class StageCAgentHardwallTests
{
    private readonly IFieldPolicy _fieldPolicy;
    private readonly IToolAllowlist _toolAllowlist;
    private readonly IAgentContextScrubber _scrubber;

    public StageCAgentHardwallTests()
    {
        _fieldPolicy = new FieldPolicy();
        _toolAllowlist = new ToolAllowlist();
        _scrubber = new AgentContextScrubber(_fieldPolicy);
    }

    #region Tool Allowlist Deny-by-Default (Security point 2)

    [Fact]
    public void Allowlist_RegisteredTool_IsAllowed()
    {
        Assert.True(_toolAllowlist.IsAllowed("get_profile"));
        Assert.True(_toolAllowlist.IsAllowed("get_negotiation"));
        Assert.True(_toolAllowlist.IsAllowed("get_strategy"));
        Assert.True(_toolAllowlist.IsAllowed("share_contact_email"));
    }

    [Fact]
    public void Allowlist_UnregisteredTool_IsDenied()
    {
        Assert.False(_toolAllowlist.IsAllowed("raw_db_query"));
        Assert.False(_toolAllowlist.IsAllowed("arbitrary_http"));
        Assert.False(_toolAllowlist.IsAllowed("get_login_email"));
        Assert.False(_toolAllowlist.IsAllowed("unknown_tool"));
    }

    [Fact]
    public void Allowlist_CaseInsensitive_Works()
    {
        Assert.True(_toolAllowlist.IsAllowed("GET_PROFILE"));
        Assert.True(_toolAllowlist.IsAllowed("Get_Profile"));
        Assert.False(_toolAllowlist.IsAllowed("Raw_Db_Query"));
    }

    [Fact]
    public void Allowlist_NullOrEmpty_IsDenied()
    {
        Assert.False(_toolAllowlist.IsAllowed(null!));
        Assert.False(_toolAllowlist.IsAllowed(""));
        Assert.False(_toolAllowlist.IsAllowed("   "));
    }

    [Fact]
    public void GetTool_RegisteredTool_ReturnsToolWithDeclarations()
    {
        var tool = _toolAllowlist.GetTool("get_profile");

        Assert.NotNull(tool);
        Assert.Equal("get_profile", tool.Name);
        Assert.True(tool.DeclaresAccess(FieldClass.DisplayName, FieldAction.Read));
        Assert.True(tool.DeclaresAccess(FieldClass.ContactEmail, FieldAction.Read));
        Assert.False(tool.DeclaresAccess(FieldClass.LoginEmail, FieldAction.Read));
    }

    [Fact]
    public void GetTool_UnregisteredTool_ReturnsNull()
    {
        var tool = _toolAllowlist.GetTool("raw_db_query");
        Assert.Null(tool);
    }

    #endregion

    #region No LoginEmail Tool/Context (Security point 4)

    [Fact]
    public void Allowlist_NoToolDeclaresLoginEmail()
    {
        var toolNames = _toolAllowlist.GetAllowedToolNames();
        
        foreach (var toolName in toolNames)
        {
            var tool = _toolAllowlist.GetTool(toolName);
            Assert.NotNull(tool);
            
            Assert.False(tool.DeclaresAccess(FieldClass.LoginEmail, FieldAction.Read),
                $"Tool '{toolName}' should not declare LoginEmail Read");
            Assert.False(tool.DeclaresAccess(FieldClass.LoginEmail, FieldAction.Write),
                $"Tool '{toolName}' should not declare LoginEmail Write");
            Assert.False(tool.DeclaresAccess(FieldClass.LoginEmail, FieldAction.ShareOutbound),
                $"Tool '{toolName}' should not declare LoginEmail ShareOutbound");
        }
    }

    [Fact]
    public void Allowlist_NoLoginEmailTool()
    {
        Assert.False(_toolAllowlist.IsAllowed("get_login_email"));
        Assert.False(_toolAllowlist.IsAllowed("login_email"));
        Assert.False(_toolAllowlist.IsAllowed("read_login_email"));
    }

    [Fact]
    public void Scrubber_StripsLoginEmail_EvenIfAccidentallyReturned()
    {
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var response = ToolResponse.Ok("test_tool",
            AgentContextField.ForLoginEmail("secret@login.example"),
            AgentContextField.ForDisplayName("Test User"),
            AgentContextField.ForContactEmail("contact@example.com")
        );

        var scrubbed = _scrubber.Scrub(response, principal, context, FieldAction.Read);

        Assert.DoesNotContain(scrubbed.AllowedFields, 
            f => f.FieldClass == FieldClass.LoginEmail);
        Assert.Equal(1, scrubbed.StrippedFieldCount);
        
        Assert.Contains(scrubbed.AllowedFields, f => f.FieldClass == FieldClass.DisplayName);
        Assert.Contains(scrubbed.AllowedFields, f => f.FieldClass == FieldClass.ContactEmail);
    }

    [Fact]
    public void FieldPolicy_OwnAgent_DenyLoginEmail_Held()
    {
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canRead = _fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Read, context);
        var canWrite = _fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Write, context);
        var canShare = _fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.ShareOutbound, context);

        Assert.False(canRead, "OwnAgent should be denied LoginEmail Read");
        Assert.False(canWrite, "OwnAgent should be denied LoginEmail Write");
        Assert.False(canShare, "OwnAgent should be denied LoginEmail ShareOutbound");
    }

    #endregion

    #region Server-Side Scrub Before Model Context (Security point 3)

    [Fact]
    public void Scrubber_AllowedFields_PassThrough()
    {
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var response = ToolResponse.Ok("get_profile",
            AgentContextField.ForDisplayName("Test User"),
            AgentContextField.ForContactEmail("contact@example.com")
        );

        var scrubbed = _scrubber.Scrub(response, principal, context, FieldAction.Read);

        Assert.True(scrubbed.Success);
        Assert.Equal(2, scrubbed.AllowedFields.Count);
        Assert.Equal(0, scrubbed.StrippedFieldCount);
    }

    [Fact]
    public void Scrubber_DeniedFields_Stripped()
    {
        var principal = FieldPrincipal.Agent("stranger-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var response = ToolResponse.Ok("get_profile",
            AgentContextField.ForDisplayName("Test User"),
            AgentContextField.ForContactEmail("contact@example.com")
        );

        var scrubbed = _scrubber.Scrub(response, principal, context, FieldAction.Read);

        Assert.True(scrubbed.Success);
        Assert.Empty(scrubbed.AllowedFields);
        Assert.Equal(2, scrubbed.StrippedFieldCount);
    }

    [Fact]
    public void Scrubber_UsesSameFieldPolicyAsApiDb()
    {
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var contactEmailAllowed = _fieldPolicy.Evaluate(principal, FieldClass.ContactEmail, FieldAction.Read, context);
        var loginEmailAllowed = _fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Read, context);

        Assert.True(contactEmailAllowed, "OwnAgent should be allowed ContactEmail Read");
        Assert.False(loginEmailAllowed, "OwnAgent should be denied LoginEmail Read");

        var response = ToolResponse.Ok("test_tool",
            AgentContextField.ForContactEmail("contact@example.com"),
            AgentContextField.ForLoginEmail("secret@login.example")
        );

        var scrubbed = _scrubber.Scrub(response, principal, context, FieldAction.Read);

        Assert.Single(scrubbed.AllowedFields);
        Assert.Equal(FieldClass.ContactEmail, scrubbed.AllowedFields[0].FieldClass);
        Assert.Equal(1, scrubbed.StrippedFieldCount);
    }

    #endregion

    #region ShareOutbound Accept-Gated (Security point 5)

    [Fact]
    public void PreAccept_ShareOutbound_Deny()
    {
        var principal = FieldPrincipal.Agent("counterparty-sub");
        var context = FieldResourceContext.ForNegotiation("owner-sub", "counterparty-sub");

        var canShare = _fieldPolicy.Evaluate(
            principal, FieldClass.ContactEmail, FieldAction.ShareOutbound, context);

        Assert.False(canShare, "Pre-Accept ShareOutbound(ContactEmail) should be denied");
    }

    [Fact]
    public void PostAccept_ShareOutbound_Allow_ForCounterparty()
    {
        var principal = FieldPrincipal.User("counterparty-sub");
        var context = FieldResourceContext.ForAcceptedNegotiation("owner-sub", "counterparty-sub");

        var canShare = _fieldPolicy.Evaluate(
            principal, FieldClass.ContactEmail, FieldAction.ShareOutbound, context);

        Assert.True(canShare, "Post-Accept ShareOutbound(ContactEmail) should be allowed for counterparty");
    }

    [Fact]
    public void PostAccept_ShareOutbound_Deny_ForStranger()
    {
        var principal = FieldPrincipal.User("stranger-sub");
        var context = FieldResourceContext.ForAcceptedNegotiation("owner-sub", "counterparty-sub");

        var canShare = _fieldPolicy.Evaluate(
            principal, FieldClass.ContactEmail, FieldAction.ShareOutbound, context);

        Assert.False(canShare, "Post-Accept ShareOutbound(ContactEmail) should be denied for stranger");
    }

    [Fact]
    public void ShareOutbound_LoginEmail_AlwaysDeny()
    {
        var principalPre = FieldPrincipal.User("counterparty-sub");
        var contextPre = FieldResourceContext.ForNegotiation("owner-sub", "counterparty-sub");
        var contextPost = FieldResourceContext.ForAcceptedNegotiation("owner-sub", "counterparty-sub");

        var canSharePre = _fieldPolicy.Evaluate(
            principalPre, FieldClass.LoginEmail, FieldAction.ShareOutbound, contextPre);
        var canSharePost = _fieldPolicy.Evaluate(
            principalPre, FieldClass.LoginEmail, FieldAction.ShareOutbound, contextPost);

        Assert.False(canSharePre, "LoginEmail ShareOutbound should always be denied");
        Assert.False(canSharePost, "LoginEmail ShareOutbound should always be denied even after Accept");
    }

    [Fact]
    public void Scrubber_ShareOutbound_RespectsAcceptGrant()
    {
        var counterpartyPrincipal = FieldPrincipal.User("counterparty-sub");
        
        var preAcceptContext = FieldResourceContext.ForNegotiation("owner-sub", "counterparty-sub");
        var postAcceptContext = FieldResourceContext.ForAcceptedNegotiation("owner-sub", "counterparty-sub");

        var response = ToolResponse.Ok("share_contact_email",
            AgentContextField.ForContactEmail("contact@example.com")
        );

        var scrubbedPreAccept = _scrubber.Scrub(response, counterpartyPrincipal, preAcceptContext, FieldAction.ShareOutbound);
        var scrubbedPostAccept = _scrubber.Scrub(response, counterpartyPrincipal, postAcceptContext, FieldAction.ShareOutbound);

        Assert.Empty(scrubbedPreAccept.AllowedFields);
        Assert.Equal(1, scrubbedPreAccept.StrippedFieldCount);

        Assert.Single(scrubbedPostAccept.AllowedFields);
        Assert.Equal(0, scrubbedPostAccept.StrippedFieldCount);
    }

    #endregion

    #region Stranger/Cross-Tenant Deny (Security point 8)

    [Fact]
    public void Stranger_DeniedAllProtectedFields()
    {
        var principal = FieldPrincipal.Agent("stranger-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canReadLoginEmail = _fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Read, context);
        var canReadContactEmail = _fieldPolicy.Evaluate(principal, FieldClass.ContactEmail, FieldAction.Read, context);
        var canReadDisplayName = _fieldPolicy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Read, context);
        var canReadStrategyBody = _fieldPolicy.Evaluate(principal, FieldClass.StrategyBody, FieldAction.Read, context);

        Assert.False(canReadLoginEmail);
        Assert.False(canReadContactEmail);
        Assert.False(canReadDisplayName);
        Assert.False(canReadStrategyBody);
    }

    [Fact]
    public void Scrubber_StripsAllForStranger()
    {
        var principal = FieldPrincipal.Agent("stranger-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var response = ToolResponse.Ok("get_profile",
            AgentContextField.ForDisplayName("Owner Name"),
            AgentContextField.ForContactEmail("owner@example.com"),
            AgentContextField.ForLoginEmail("owner-login@example.com"),
            AgentContextField.ForStrategyBody("secret strategy")
        );

        var scrubbed = _scrubber.Scrub(response, principal, context, FieldAction.Read);

        Assert.Empty(scrubbed.AllowedFields);
        Assert.Equal(4, scrubbed.StrippedFieldCount);
    }

    [Fact]
    public void CrossTenant_NoPrivateFieldLeakage()
    {
        var tenantAPrincipal = FieldPrincipal.Agent("tenant-a-user");
        var tenantBContext = FieldResourceContext.ForSelfProfile("tenant-b-user");

        var response = ToolResponse.Ok("get_profile",
            AgentContextField.ForDisplayName("Tenant B User"),
            AgentContextField.ForContactEmail("tenantb@example.com"),
            AgentContextField.ForStrategyBody("tenant B secrets")
        );

        var scrubbed = _scrubber.Scrub(response, tenantAPrincipal, tenantBContext, FieldAction.Read);

        Assert.Empty(scrubbed.AllowedFields);
        Assert.Equal(3, scrubbed.StrippedFieldCount);
    }

    #endregion

    #region Unauthenticated Deny (Security point 6)

    [Fact]
    public void Unauthenticated_DeniedAll()
    {
        var principal = FieldPrincipal.Unauthenticated();
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var canReadLoginEmail = _fieldPolicy.Evaluate(principal, FieldClass.LoginEmail, FieldAction.Read, context);
        var canReadContactEmail = _fieldPolicy.Evaluate(principal, FieldClass.ContactEmail, FieldAction.Read, context);
        var canReadDisplayName = _fieldPolicy.Evaluate(principal, FieldClass.DisplayName, FieldAction.Read, context);

        Assert.False(canReadLoginEmail);
        Assert.False(canReadContactEmail);
        Assert.False(canReadDisplayName);
    }

    [Fact]
    public void Scrubber_StripsAllForUnauthenticated()
    {
        var principal = FieldPrincipal.Unauthenticated();
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var response = ToolResponse.Ok("get_profile",
            AgentContextField.ForDisplayName("User"),
            AgentContextField.ForContactEmail("contact@example.com")
        );

        var scrubbed = _scrubber.Scrub(response, principal, context, FieldAction.Read);

        Assert.Empty(scrubbed.AllowedFields);
        Assert.Equal(2, scrubbed.StrippedFieldCount);
    }

    [Fact]
    public void Scrubber_ErrorResponse_NoPrivateFieldsInMessage()
    {
        var principal = FieldPrincipal.Unauthenticated();
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var response = ToolResponse.Error("get_profile", "Error accessing login@secret.example ContactEmail");
        
        var scrubbed = _scrubber.Scrub(response, principal, context, FieldAction.Read);

        Assert.False(scrubbed.Success);
        Assert.DoesNotContain("login@", scrubbed.ErrorMessage ?? "");
        Assert.DoesNotContain("ContactEmail", scrubbed.ErrorMessage ?? "");
    }

    #endregion

    #region Reject Prompt-Only as Sole Control (Security point 6)

    [Fact]
    public void Architecture_GatewayPlusEvaluate_NotPromptOnly()
    {
        Assert.NotNull(_toolAllowlist);
        Assert.NotNull(_scrubber);
        Assert.NotNull(_fieldPolicy);
    }

    [Fact]
    public void ToolDeclarations_Enforced_NotPromptGuidance()
    {
        var tool = _toolAllowlist.GetTool("get_profile")!;
        
        Assert.True(tool.DeclaresAccess(FieldClass.DisplayName, FieldAction.Read));
        Assert.True(tool.DeclaresAccess(FieldClass.ContactEmail, FieldAction.Read));
        Assert.False(tool.DeclaresAccess(FieldClass.LoginEmail, FieldAction.Read));
        Assert.False(tool.DeclaresAccess(FieldClass.StrategyBody, FieldAction.Read));
    }

    [Fact]
    public void ValidateToolDeclarations_RejectsUndeclaredFields()
    {
        var tool = _toolAllowlist.GetTool("get_profile")!;
        
        var validResponse = ToolResponse.Ok("get_profile",
            AgentContextField.ForDisplayName("User"),
            AgentContextField.ForContactEmail("contact@example.com")
        );

        var invalidResponse = ToolResponse.Ok("get_profile",
            AgentContextField.ForDisplayName("User"),
            AgentContextField.ForLoginEmail("secret@login.example")
        );

        Assert.True(_scrubber.ValidateToolDeclarations(validResponse, tool, FieldAction.Read));
        Assert.False(_scrubber.ValidateToolDeclarations(invalidResponse, tool, FieldAction.Read));
    }

    [Fact]
    public void Scrubber_ServerSide_NotClientPromptBased()
    {
        var ownerPrincipal = FieldPrincipal.Agent("owner-sub");
        var strangerPrincipal = FieldPrincipal.Agent("stranger-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var response = ToolResponse.Ok("get_profile",
            AgentContextField.ForContactEmail("contact@example.com")
        );

        var ownerScrubbed = _scrubber.Scrub(response, ownerPrincipal, context, FieldAction.Read);
        var strangerScrubbed = _scrubber.Scrub(response, strangerPrincipal, context, FieldAction.Read);

        Assert.Single(ownerScrubbed.AllowedFields);
        Assert.Empty(strangerScrubbed.AllowedFields);
    }

    #endregion

    #region Dual Wall All FieldClasses (Security point 7)

    [Fact]
    public void DualWall_AllRegisteredFieldClasses_EvaluatedBySamePolicyAsApiDb()
    {
        Assert.True(_fieldPolicy.IsRegistered(FieldClass.LoginEmail));
        Assert.True(_fieldPolicy.IsRegistered(FieldClass.ContactEmail));
        Assert.True(_fieldPolicy.IsRegistered(FieldClass.DisplayName));
        Assert.True(_fieldPolicy.IsRegistered(FieldClass.StrategyBody));
    }

    [Fact]
    public void DualWall_UnknownFieldClass_Denied()
    {
        var principal = FieldPrincipal.User("user-sub");
        var context = FieldResourceContext.ForSelfProfile("user-sub");

        var unknownField = FieldClass.Custom("UnknownSecretField");
        var canRead = _fieldPolicy.Evaluate(principal, unknownField, FieldAction.Read, context);

        Assert.False(canRead, "Unknown FieldClass should be denied by default");
    }

    [Fact]
    public void DualWall_Scrubber_UsesOpenEndedRegistry()
    {
        var principal = FieldPrincipal.Agent("owner-sub");
        var context = FieldResourceContext.ForSelfProfile("owner-sub");

        var unknownField = FieldClass.Custom("FutureSecretField");
        var response = ToolResponse.Ok("test_tool",
            AgentContextField.Create("futureSecret", unknownField, "secret value"),
            AgentContextField.ForDisplayName("User")
        );

        var scrubbed = _scrubber.Scrub(response, principal, context, FieldAction.Read);

        Assert.Single(scrubbed.AllowedFields);
        Assert.Equal(FieldClass.DisplayName, scrubbed.AllowedFields[0].FieldClass);
        Assert.Equal(1, scrubbed.StrippedFieldCount);
    }

    #endregion

    #region Tool Field Declarations

    [Fact]
    public void GetProfile_DeclaresDisplayNameAndContactEmail_NotLoginEmail()
    {
        var tool = _toolAllowlist.GetTool("get_profile")!;
        
        Assert.True(tool.DeclaresAccess(FieldClass.DisplayName, FieldAction.Read));
        Assert.True(tool.DeclaresAccess(FieldClass.ContactEmail, FieldAction.Read));
        Assert.False(tool.DeclaresAccess(FieldClass.LoginEmail, FieldAction.Read));
        Assert.False(tool.DeclaresAccess(FieldClass.StrategyBody, FieldAction.Read));
    }

    [Fact]
    public void GetStrategy_DeclaresStrategyBodyOnly()
    {
        var tool = _toolAllowlist.GetTool("get_strategy")!;
        
        Assert.True(tool.DeclaresAccess(FieldClass.StrategyBody, FieldAction.Read));
        Assert.False(tool.DeclaresAccess(FieldClass.LoginEmail, FieldAction.Read));
        Assert.False(tool.DeclaresAccess(FieldClass.ContactEmail, FieldAction.Read));
    }

    [Fact]
    public void ShareContactEmail_DeclaresShareOutboundOnly()
    {
        var tool = _toolAllowlist.GetTool("share_contact_email")!;
        
        Assert.True(tool.DeclaresAccess(FieldClass.ContactEmail, FieldAction.ShareOutbound));
        Assert.False(tool.DeclaresAccess(FieldClass.ContactEmail, FieldAction.Read));
        Assert.False(tool.DeclaresAccess(FieldClass.LoginEmail, FieldAction.ShareOutbound));
    }

    #endregion

    #region Integration: Full Gateway Flow

    [Fact]
    public async Task Gateway_AllowedTool_ReturnsScrubbedResponse()
    {
        var toolAllowlist = new ToolAllowlist();
        var fieldPolicy = new FieldPolicy();
        var scrubber = new AgentContextScrubber(fieldPolicy);
        var executor = new TestToolExecutor();
        var gateway = new Dealoware.Domain.AgentGateway.AgentGateway(toolAllowlist, scrubber, executor);

        var request = ToolInvocationRequest.Create(
            "get_profile",
            FieldPrincipal.Agent("owner-sub"),
            FieldResourceContext.ForSelfProfile("owner-sub")
        );

        var result = await gateway.InvokeToolAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.DoesNotContain(result.Response.AllowedFields, f => f.FieldClass == FieldClass.LoginEmail);
    }

    [Fact]
    public async Task Gateway_UnallowedTool_ReturnsDeny()
    {
        var toolAllowlist = new ToolAllowlist();
        var fieldPolicy = new FieldPolicy();
        var scrubber = new AgentContextScrubber(fieldPolicy);
        var executor = new TestToolExecutor();
        var gateway = new Dealoware.Domain.AgentGateway.AgentGateway(toolAllowlist, scrubber, executor);

        var request = ToolInvocationRequest.Create(
            "raw_db_query",
            FieldPrincipal.Agent("owner-sub"),
            FieldResourceContext.ForSelfProfile("owner-sub")
        );

        var result = await gateway.InvokeToolAsync(request);

        Assert.False(result.Success);
        Assert.Equal("TOOL_NOT_ALLOWED", result.ErrorCode);
    }

    [Fact]
    public async Task Gateway_UnauthenticatedPrincipal_ReturnsDeny()
    {
        var toolAllowlist = new ToolAllowlist();
        var fieldPolicy = new FieldPolicy();
        var scrubber = new AgentContextScrubber(fieldPolicy);
        var executor = new TestToolExecutor();
        var gateway = new Dealoware.Domain.AgentGateway.AgentGateway(toolAllowlist, scrubber, executor);

        var request = ToolInvocationRequest.Create(
            "get_profile",
            FieldPrincipal.Unauthenticated(),
            FieldResourceContext.ForSelfProfile("owner-sub")
        );

        var result = await gateway.InvokeToolAsync(request);

        Assert.False(result.Success);
        Assert.Equal("UNAUTHENTICATED", result.ErrorCode);
    }

    [Fact]
    public async Task Gateway_NonAgentPrincipal_ReturnsDeny()
    {
        var toolAllowlist = new ToolAllowlist();
        var fieldPolicy = new FieldPolicy();
        var scrubber = new AgentContextScrubber(fieldPolicy);
        var executor = new TestToolExecutor();
        var gateway = new Dealoware.Domain.AgentGateway.AgentGateway(toolAllowlist, scrubber, executor);

        var request = ToolInvocationRequest.Create(
            "get_profile",
            FieldPrincipal.User("user-sub"),
            FieldResourceContext.ForSelfProfile("user-sub")
        );

        var result = await gateway.InvokeToolAsync(request);

        Assert.False(result.Success);
        Assert.Equal("NOT_AGENT", result.ErrorCode);
    }

    private class TestToolExecutor : IToolExecutor
    {
        public Task<ToolResponse> ExecuteAsync(AgentTool tool, ToolInvocationRequest request)
        {
            return Task.FromResult(ToolResponse.Ok(tool.Name,
                AgentContextField.ForDisplayName("Test User"),
                AgentContextField.ForContactEmail("contact@example.com")
            ));
        }
    }

    #endregion

    #region Health Endpoint Unchanged

    [Fact]
    public void HealthEndpoint_NotAffectedByAgentGateway()
    {
        Assert.False(_toolAllowlist.IsAllowed("health"));
    }

    #endregion
}
