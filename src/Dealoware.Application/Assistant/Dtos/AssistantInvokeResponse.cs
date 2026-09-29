namespace Dealoware.Application.Assistant.Dtos;

/// <summary>
/// API response from thin OwnAgent assistant invocation.
/// Stage C #66: Scrubbed response — no LoginEmail/denied fields.
/// 
/// Security guarantees:
/// - No LoginEmail in any field
/// - No denied FieldClasses
/// - No other Participant's StrategyBody
/// - All data passed through #67 gateway + IFieldPolicy.Evaluate
/// </summary>
public sealed class AssistantInvokeResponse
{
    /// <summary>
    /// Whether the invocation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error code if failed.
    /// Does not contain LoginEmail or private Strategy data.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message if failed.
    /// Sanitized — no LoginEmail or private data leak.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Assistant's response text.
    /// </summary>
    public string? ResponseText { get; set; }

    /// <summary>
    /// Tool result if a tool was invoked.
    /// Scrubbed via #67 gateway.
    /// </summary>
    public AssistantToolResult? ToolResult { get; set; }

    /// <summary>
    /// Strategy context if strategy was used.
    /// Only owner's own strategy (OwnAgent R/W).
    /// </summary>
    public AssistantStrategyInfo? StrategyUsed { get; set; }
}

/// <summary>
/// Scrubbed tool result from #67 gateway.
/// </summary>
public sealed class AssistantToolResult
{
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Allowed fields that passed IFieldPolicy.Evaluate.
    /// Denied fields have been stripped.
    /// </summary>
    public List<AssistantContextField> AllowedFields { get; set; } = new();
    
    /// <summary>
    /// Number of fields stripped by the scrubber.
    /// Soft OTel touchpoint — no 5th Story.
    /// </summary>
    public int StrippedFieldCount { get; set; }
}

/// <summary>
/// A field value safe for assistant context.
/// </summary>
public sealed class AssistantContextField
{
    public string Name { get; set; } = string.Empty;
    public string FieldClass { get; set; } = string.Empty;
    public object? Value { get; set; }
}

/// <summary>
/// Strategy context information (owner's own only).
/// </summary>
public sealed class AssistantStrategyInfo
{
    public Guid StrategyId { get; set; }
    public string StrategyName { get; set; } = string.Empty;
}
