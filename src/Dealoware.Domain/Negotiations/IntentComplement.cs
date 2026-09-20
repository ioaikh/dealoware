namespace Dealoware.Domain.Negotiations;

/// <summary>
/// Defines complementary intent pairs for 1:1 negotiations.
/// Intents are complementary when they represent opposite sides of a transaction.
/// 
/// PoC documented pairs (from Product examples):
/// - buy ↔ sell: Buyer seeks product, Seller offers product
/// - provide ↔ consume: Provider offers service, Consumer seeks service
/// - rent (bidirectional): Renter offering ↔ Renter seeking (tenant or landlord perspective)
/// 
/// Intent matching is case-insensitive.
/// </summary>
public static class IntentComplement
{
    /// <summary>
    /// Known complementary intent pairs (stored as lowercase).
    /// </summary>
    private static readonly HashSet<(string, string)> ComplementaryPairs = new()
    {
        ("buy", "sell"),
        ("sell", "buy"),
        ("provide", "consume"),
        ("consume", "provide"),
        ("rent", "rent")
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

        return normalized switch
        {
            "buy" => "sell",
            "sell" => "buy",
            "provide" => "consume",
            "consume" => "provide",
            "rent" => "rent",
            _ => null
        };
    }
}
