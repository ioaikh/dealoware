namespace Dealoware.Domain.AgentGateway;

using Dealoware.Domain.FieldAcl;

/// <summary>
/// Separate agent runtime gateway implementation.
/// Stage C #67: Agents invoke platform tools only via this gateway.
/// 
/// Architecture enforcement:
/// - Only allowlisted tools can be invoked
/// - No raw DB access (tools are platform abstractions)
/// - No arbitrary HTTP to internal APIs
/// - No privileged back doors that skip Evaluate
/// - Prompt text cannot escalate rights — gateway is server-side control
/// 
/// Control flow:
/// 1. Validate principal is authenticated agent
/// 2. Check tool is on allowlist (deny-by-default)
/// 3. Invoke tool (platform abstraction)
/// 4. Validate tool response doesn't contain undeclared FieldClasses
/// 5. Scrub response via IFieldPolicy.Evaluate
/// 6. Return scrubbed response safe for model context
/// </summary>
public sealed class AgentGateway : IAgentGateway
{
    private readonly IToolAllowlist _toolAllowlist;
    private readonly IAgentContextScrubber _scrubber;
    private readonly IToolExecutor _toolExecutor;

    public AgentGateway(
        IToolAllowlist toolAllowlist,
        IAgentContextScrubber scrubber,
        IToolExecutor toolExecutor)
    {
        _toolAllowlist = toolAllowlist ?? throw new ArgumentNullException(nameof(toolAllowlist));
        _scrubber = scrubber ?? throw new ArgumentNullException(nameof(scrubber));
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
    }

    public async Task<GatewayResult> InvokeToolAsync(ToolInvocationRequest request)
    {
        if (request.Principal.Type == PrincipalType.Unauthenticated)
        {
            return GatewayResult.Unauthenticated();
        }

        if (!request.Principal.IsAgent)
        {
            return GatewayResult.NotAnAgent();
        }

        var tool = _toolAllowlist.GetTool(request.ToolName);
        if (tool == null)
        {
            return GatewayResult.ToolNotAllowed(request.ToolName);
        }

        ToolResponse rawResponse;
        try
        {
            rawResponse = await _toolExecutor.ExecuteAsync(tool, request);
        }
        catch (Exception ex)
        {
            return GatewayResult.Error($"Tool execution failed: {ex.Message}");
        }

        var action = DetermineAction(tool, request);
        
        if (!_scrubber.ValidateToolDeclarations(rawResponse, tool, action))
        {
            return GatewayResult.UndeclaredFieldClass(request.ToolName);
        }

        var scrubbedResponse = _scrubber.Scrub(
            rawResponse,
            request.Principal,
            request.ResourceContext,
            action);

        return GatewayResult.Ok(scrubbedResponse);
    }

    public bool IsToolAvailable(string toolName)
    {
        return _toolAllowlist.IsAllowed(toolName);
    }

    public IEnumerable<string> GetAvailableToolNames()
    {
        return _toolAllowlist.GetAllowedToolNames();
    }

    private static FieldAction DetermineAction(AgentTool tool, ToolInvocationRequest request)
    {
        var declaredActions = tool.FieldDeclarations
            .SelectMany(d => d.AllowedActions)
            .Distinct()
            .ToList();

        if (declaredActions.Contains(FieldAction.ShareOutbound) &&
            (tool.Name.Contains("share", StringComparison.OrdinalIgnoreCase) ||
             tool.Name.Contains("accept", StringComparison.OrdinalIgnoreCase)))
        {
            return FieldAction.ShareOutbound;
        }

        return FieldAction.Read;
    }
}
