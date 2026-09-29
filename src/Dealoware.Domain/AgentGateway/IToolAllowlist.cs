namespace Dealoware.Domain.AgentGateway;

/// <summary>
/// Tool allowlist interface for deny-by-default access control.
/// Stage C #67: Only allowlisted tools may be invoked by agents.
/// Undeclared tools are denied. Each tool declares its FieldClass access.
/// </summary>
public interface IToolAllowlist
{
    /// <summary>
    /// Gets a tool by name if it is registered on the allowlist.
    /// Returns null if the tool is not on the allowlist (deny).
    /// </summary>
    AgentTool? GetTool(string toolName);

    /// <summary>
    /// Checks if a tool is registered on the allowlist.
    /// </summary>
    bool IsAllowed(string toolName);

    /// <summary>
    /// Gets all registered tool names for audit/diagnostic purposes.
    /// </summary>
    IEnumerable<string> GetAllowedToolNames();
}
