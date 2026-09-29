namespace Dealoware.Domain.Assistant;

/// <summary>
/// Request to invoke the thin OwnAgent assistant.
/// Stage C #66: X1 thin minimal invocation.
/// 
/// Note: This is an X1 thin implementation. Fuller request models → V1.
/// </summary>
public sealed class AssistantInvocationRequest
{
    /// <summary>
    /// Optional strategy ID to use for this invocation.
    /// If null, assistant uses default behavior.
    /// Strategy must be owned by the invoking participant (OwnAgent R/W check).
    /// </summary>
    public Guid? StrategyId { get; init; }

    /// <summary>
    /// The user input/prompt for the assistant.
    /// X1 thin: Simple string input. Fuller input models → V1.
    /// </summary>
    public string? Input { get; init; }

    /// <summary>
    /// Optional tool name to invoke directly.
    /// Must be on #67 allowlist.
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Optional parameters for tool invocation.
    /// </summary>
    public Dictionary<string, object?>? ToolParameters { get; init; }

    public static AssistantInvocationRequest Create(
        Guid? strategyId = null,
        string? input = null,
        string? toolName = null,
        Dictionary<string, object?>? toolParameters = null)
    {
        return new AssistantInvocationRequest
        {
            StrategyId = strategyId,
            Input = input,
            ToolName = toolName,
            ToolParameters = toolParameters
        };
    }
}
