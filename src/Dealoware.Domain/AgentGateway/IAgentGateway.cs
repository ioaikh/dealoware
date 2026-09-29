namespace Dealoware.Domain.AgentGateway;

/// <summary>
/// Separate agent runtime gateway — agents invoke platform tools only.
/// Stage C #67: Defense #2 — agents cannot access raw DB or arbitrary internal HTTP.
/// 
/// Key behaviors:
/// - Only allowlisted tools may be invoked (deny-by-default)
/// - All tool responses pass through server-side scrub
/// - Same IFieldPolicy.Evaluate as API/DB wall
/// - Reject prompt-only control — gateway is server-side enforcement
/// </summary>
public interface IAgentGateway
{
    /// <summary>
    /// Invokes a platform tool via the gateway.
    /// </summary>
    /// <param name="request">Tool invocation request from agent.</param>
    /// <returns>Gateway result with scrubbed response or error.</returns>
    Task<GatewayResult> InvokeToolAsync(ToolInvocationRequest request);

    /// <summary>
    /// Checks if a tool is available via the gateway.
    /// Does not check field-level access — only tool allowlist.
    /// </summary>
    bool IsToolAvailable(string toolName);

    /// <summary>
    /// Gets available tool names for the agent.
    /// Does not expose tools that would always deny for agent principals.
    /// </summary>
    IEnumerable<string> GetAvailableToolNames();
}
