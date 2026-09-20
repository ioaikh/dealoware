namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Defines complementary intent pairs for 1:1 negotiations.
/// Intents are complementary when they represent opposite sides of a transaction.
/// 
/// All pairs are directional: the seeker/acquirer intent complements the provider/offerer intent.
/// For example: buy↔sell, rent↔rent out, borrow↔lend.
/// 
/// Intent matching is case-insensitive.
/// See docs/product for the full catalog of ~40 intent tokens across 20 pairs.
/// </summary>
public static class IntentComplement
{
    /// <summary>
    /// Known complementary intent pairs (stored as lowercase).
    /// Each pair is stored in both directions for O(1) lookup.
    /// </summary>
    private static readonly HashSet<(string, string)> ComplementaryPairs = new()
    {
        // Commerce/Goods
        ("buy", "sell"),
        ("sell", "buy"),
        ("order", "fulfill"),
        ("fulfill", "order"),
        ("acquire", "dispose"),
        ("dispose", "acquire"),
        ("import", "export"),
        ("export", "import"),

        // Services
        ("provide", "consume"),
        ("consume", "provide"),
        ("deliver", "receive"),
        ("receive", "deliver"),
        ("perform", "commission"),
        ("commission", "perform"),

        // Rentals/Temporary Use (seeker ↔ provider with "X out" pattern)
        ("rent", "rent out"),
        ("rent out", "rent"),
        ("lease", "lease out"),
        ("lease out", "lease"),
        ("borrow", "lend"),
        ("lend", "borrow"),
        ("hire", "hire out"),
        ("hire out", "hire"),

        // Hospitality/Space
        ("visit", "host"),
        ("host", "visit"),
        ("stay", "accommodate"),
        ("accommodate", "stay"),
        ("book", "list"),
        ("list", "book"),

        // Attention/Visibility
        ("see", "show"),
        ("show", "see"),
        ("watch", "stream"),
        ("stream", "watch"),
        ("attend", "present"),
        ("present", "attend"),

        // Access/Permissions
        ("access", "grant"),
        ("grant", "access"),
        ("subscribe", "publish"),
        ("publish", "subscribe"),

        // General
        ("seek", "offer"),
        ("offer", "seek")
    };

    /// <summary>
    /// Maps each intent to its complement for quick lookup.
    /// </summary>
    private static readonly Dictionary<string, string> ComplementMap = new()
    {
        // Commerce/Goods
        ["buy"] = "sell",
        ["sell"] = "buy",
        ["order"] = "fulfill",
        ["fulfill"] = "order",
        ["acquire"] = "dispose",
        ["dispose"] = "acquire",
        ["import"] = "export",
        ["export"] = "import",

        // Services
        ["provide"] = "consume",
        ["consume"] = "provide",
        ["deliver"] = "receive",
        ["receive"] = "deliver",
        ["perform"] = "commission",
        ["commission"] = "perform",

        // Rentals/Temporary Use
        ["rent"] = "rent out",
        ["rent out"] = "rent",
        ["lease"] = "lease out",
        ["lease out"] = "lease",
        ["borrow"] = "lend",
        ["lend"] = "borrow",
        ["hire"] = "hire out",
        ["hire out"] = "hire",

        // Hospitality/Space
        ["visit"] = "host",
        ["host"] = "visit",
        ["stay"] = "accommodate",
        ["accommodate"] = "stay",
        ["book"] = "list",
        ["list"] = "book",

        // Attention/Visibility
        ["see"] = "show",
        ["show"] = "see",
        ["watch"] = "stream",
        ["stream"] = "watch",
        ["attend"] = "present",
        ["present"] = "attend",

        // Access/Permissions
        ["access"] = "grant",
        ["grant"] = "access",
        ["subscribe"] = "publish",
        ["publish"] = "subscribe",

        // General
        ["seek"] = "offer",
        ["offer"] = "seek"
    };

    /// <summary>
    /// Checks if two intents are complementary (can form a valid negotiation).
    /// </summary>
    /// <param name="intentA">Intent of party A (from their Artifact)</param>
    /// <param name="intentB">Intent of party B (from their Artifact)</param>
    /// <returns>True if intents are complementary</returns>
    public static bool AreComplementary(string intentA, string intentB)
    {
        if (string.IsNullOrWhiteSpace(intentA) || string.IsNullOrWhiteSpace(intentB))
            return false;

        var normalizedA = intentA.Trim().ToLowerInvariant();
        var normalizedB = intentB.Trim().ToLowerInvariant();

        return ComplementaryPairs.Contains((normalizedA, normalizedB));
    }

    /// <summary>
    /// Gets the expected complementary intent for a given intent.
    /// Returns null if no known complement exists.
    /// </summary>
    public static string? GetComplement(string intent)
    {
        if (string.IsNullOrWhiteSpace(intent))
            return null;

        var normalized = intent.Trim().ToLowerInvariant();

        return ComplementMap.TryGetValue(normalized, out var complement) ? complement : null;
    }

    /// <summary>
    /// Gets all known intent tokens (for documentation/validation purposes).
    /// </summary>
    public static IReadOnlyCollection<string> GetAllIntents() => ComplementMap.Keys;
}
