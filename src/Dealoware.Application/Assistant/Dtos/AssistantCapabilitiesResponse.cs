namespace Dealoware.Application.Assistant.Dtos;

/// <summary>
/// API response for assistant capabilities.
/// Stage C #66: Available tools via #67 gateway.
/// 
/// CRITICAL: No LoginEmail tool. LoginEmail is User-only, never agent context.
/// </summary>
public sealed class AssistantCapabilitiesResponse
{
    /// <summary>
    /// Tools available via #67 gateway.
    /// All pass through allowlist + FieldPolicy.Evaluate.
    /// </summary>
    public List<string> AvailableTools { get; set; } = new();

    /// <summary>
    /// Whether the owner has strategies.
    /// </summary>
    public bool HasStrategies { get; set; }

    /// <summary>
    /// Number of strategies available.
    /// </summary>
    public int StrategyCount { get; set; }
}
