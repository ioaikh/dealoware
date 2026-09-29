namespace Dealoware.Domain.AgentGateway;

/// <summary>
/// Tool response after passing through the agent context scrubber.
/// Denied fields have been stripped; only allowed fields remain.
/// This is safe to pass to model context.
/// 
/// Stage C #67: Scrubbing uses same IFieldPolicy.Evaluate as API/DB wall.
/// </summary>
public sealed class ScrubbedToolResponse
{
    public string ToolName { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// Fields that passed Evaluate and are safe for model context.
    /// Denied fields have been stripped — they will not appear here.
    /// </summary>
    public IReadOnlyList<AgentContextField> AllowedFields { get; }
    
    /// <summary>
    /// Number of fields that were stripped by the scrubber.
    /// For audit/OTel touchpoint (soft weave only — no 5th Story).
    /// </summary>
    public int StrippedFieldCount { get; }

    private ScrubbedToolResponse(
        string toolName,
        bool success,
        string? errorMessage,
        IReadOnlyList<AgentContextField> allowedFields,
        int strippedFieldCount)
    {
        ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
        Success = success;
        ErrorMessage = errorMessage;
        AllowedFields = allowedFields ?? throw new ArgumentNullException(nameof(allowedFields));
        StrippedFieldCount = strippedFieldCount;
    }

    public static ScrubbedToolResponse Create(
        string toolName,
        bool success,
        string? errorMessage,
        IReadOnlyList<AgentContextField> allowedFields,
        int strippedFieldCount)
    {
        return new ScrubbedToolResponse(toolName, success, errorMessage, allowedFields, strippedFieldCount);
    }

    public static ScrubbedToolResponse Denied(string toolName, string reason)
    {
        return new ScrubbedToolResponse(toolName, false, reason, Array.Empty<AgentContextField>(), 0);
    }
}
