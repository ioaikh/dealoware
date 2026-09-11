namespace Dealoware.Domain.Artifacts;

/// <summary>
/// D3: Value with amount and currency.
/// Currency is stored normalized (uppercase) for case-insensitive uniqueness.
/// </summary>
public sealed class ArtifactValue
{
    public Guid Id { get; private set; }
    
    public decimal Amount { get; private set; }
    
    /// <summary>
    /// Currency code, stored uppercase for case-insensitive comparison.
    /// </summary>
    public string Currency { get; private set; } = string.Empty;

    private ArtifactValue() { }

    public static ArtifactValue Create(decimal amount, string currency)
    {
        return new ArtifactValue
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Currency = currency.ToUpperInvariant()
        };
    }
}
