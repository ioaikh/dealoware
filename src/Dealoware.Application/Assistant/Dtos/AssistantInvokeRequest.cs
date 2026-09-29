namespace Dealoware.Application.Assistant.Dtos;

/// <summary>
/// API request to invoke the thin OwnAgent assistant.
/// Stage C #66: X1 thin invocation model.
/// 
/// Security note: All tool I/O goes through #67 gateway.
/// LoginEmail never exposed in responses.
/// </summary>
public sealed class AssistantInvokeRequest
{
    /// <summary>
    /// Optional strategy ID to use for this invocation.
    /// Must be owned by the authenticated participant.
    /// </summary>
    public Guid? StrategyId { get; set; }

    /// <summary>
    /// Optional user input for the assistant.
    /// X1 thin: Simple string. Fuller input → V1.
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Optional tool name to invoke via #67 gateway.
    /// Must be on platform tools allowlist.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Optional parameters for tool invocation.
    /// </summary>
    public Dictionary<string, object?>? ToolParameters { get; set; }
}
