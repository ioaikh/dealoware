namespace Dealoware.Domain.AgentGateway;

/// <summary>
/// Interface for executing platform tools.
/// Stage C #67: Tool executor is a platform abstraction — not raw DB access.
/// 
/// Implementations must:
/// - Return typed ToolResponse with FieldClass-tagged fields
/// - Not bypass the gateway scrub path
/// - Not expose LoginEmail in any tool response
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Executes a platform tool and returns the raw response.
    /// Response will be scrubbed by the gateway before entering model context.
    /// </summary>
    Task<ToolResponse> ExecuteAsync(AgentTool tool, ToolInvocationRequest request);
}
