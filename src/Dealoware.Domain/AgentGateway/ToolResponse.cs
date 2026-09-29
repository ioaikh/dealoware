namespace Dealoware.Domain.AgentGateway;

/// <summary>
/// Represents the response from a platform tool invocation.
/// Contains typed fields that will be scrubbed before entering model context.
/// </summary>
public sealed class ToolResponse
{
    public string ToolName { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<AgentContextField> Fields { get; }

    private ToolResponse(string toolName, bool success, string? errorMessage, IReadOnlyList<AgentContextField> fields)
    {
        ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
        Success = success;
        ErrorMessage = errorMessage;
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

    public static ToolResponse Ok(string toolName, params AgentContextField[] fields)
    {
        return new ToolResponse(toolName, true, null, fields.ToList().AsReadOnly());
    }

    public static ToolResponse Ok(string toolName, IEnumerable<AgentContextField> fields)
    {
        return new ToolResponse(toolName, true, null, fields.ToList().AsReadOnly());
    }

    public static ToolResponse Denied(string toolName, string reason)
    {
        return new ToolResponse(toolName, false, reason, Array.Empty<AgentContextField>());
    }

    public static ToolResponse Error(string toolName, string message)
    {
        return new ToolResponse(toolName, false, message, Array.Empty<AgentContextField>());
    }
}
