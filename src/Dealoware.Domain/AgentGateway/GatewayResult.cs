namespace Dealoware.Domain.AgentGateway;

/// <summary>
/// Result from the agent gateway after processing a tool invocation.
/// Contains the scrubbed response safe for model context.
/// </summary>
public sealed class GatewayResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public ScrubbedToolResponse? Response { get; }

    private GatewayResult(bool success, string? errorCode, string? errorMessage, ScrubbedToolResponse? response)
    {
        Success = success;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Response = response;
    }

    public static GatewayResult Ok(ScrubbedToolResponse response)
    {
        return new GatewayResult(true, null, null, response);
    }

    /// <summary>
    /// Tool not on allowlist — deny-by-default.
    /// </summary>
    public static GatewayResult ToolNotAllowed(string toolName)
    {
        return new GatewayResult(false, "TOOL_NOT_ALLOWED", $"Tool '{toolName}' is not on the allowlist", null);
    }

    /// <summary>
    /// Principal is not authenticated.
    /// </summary>
    public static GatewayResult Unauthenticated()
    {
        return new GatewayResult(false, "UNAUTHENTICATED", "Authentication required", null);
    }

    /// <summary>
    /// Principal is not an agent.
    /// </summary>
    public static GatewayResult NotAnAgent()
    {
        return new GatewayResult(false, "NOT_AGENT", "Only agents may invoke tools via gateway", null);
    }

    /// <summary>
    /// Tool attempted to return undeclared FieldClasses.
    /// </summary>
    public static GatewayResult UndeclaredFieldClass(string toolName)
    {
        return new GatewayResult(false, "UNDECLARED_FIELD", $"Tool '{toolName}' returned undeclared FieldClass", null);
    }

    /// <summary>
    /// Access denied by FieldPolicy for all fields.
    /// </summary>
    public static GatewayResult AccessDenied(string toolName)
    {
        return new GatewayResult(false, "ACCESS_DENIED", $"Access denied for tool '{toolName}'", null);
    }

    /// <summary>
    /// Internal error during tool execution.
    /// </summary>
    public static GatewayResult Error(string message)
    {
        return new GatewayResult(false, "ERROR", message, null);
    }
}
