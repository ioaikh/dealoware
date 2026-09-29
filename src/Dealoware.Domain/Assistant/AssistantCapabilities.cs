namespace Dealoware.Domain.Assistant;

/// <summary>
/// Available capabilities for the thin OwnAgent assistant.
/// Stage C #66: Exposes only tools available via #67 gateway.
/// 
/// CRITICAL: No LoginEmail tool. LoginEmail is User-only, never agent context.
/// </summary>
public sealed class AssistantCapabilities
{
    /// <summary>
    /// Tool names available via #67 gateway.
    /// All tools pass through allowlist + FieldPolicy.Evaluate.
    /// </summary>
    public IReadOnlyList<string> AvailableTools { get; }

    /// <summary>
    /// Whether the owner has strategies available for assistant use.
    /// </summary>
    public bool HasStrategies { get; }

    /// <summary>
    /// Number of strategies available (owner's own only).
    /// </summary>
    public int StrategyCount { get; }

    private AssistantCapabilities(
        IReadOnlyList<string> availableTools,
        bool hasStrategies,
        int strategyCount)
    {
        AvailableTools = availableTools;
        HasStrategies = hasStrategies;
        StrategyCount = strategyCount;
    }

    public static AssistantCapabilities Create(
        IEnumerable<string> availableTools,
        int strategyCount)
    {
        var tools = availableTools.ToList().AsReadOnly();
        return new AssistantCapabilities(tools, strategyCount > 0, strategyCount);
    }
}
