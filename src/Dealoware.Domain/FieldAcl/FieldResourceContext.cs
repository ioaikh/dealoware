namespace Dealoware.Domain.FieldAcl;

/// <summary>
/// Context for field policy evaluation.
/// Carries ownership and relationship signals needed for access decisions.
/// </summary>
public sealed class FieldResourceContext
{
    /// <summary>
    /// The owner's subject identifier (participant sub).
    /// </summary>
    public string ResourceOwnerSub { get; init; } = string.Empty;
    
    /// <summary>
    /// Counterparty subject identifier, if any (for negotiation context).
    /// </summary>
    public string? CounterpartySub { get; init; }
    
    /// <summary>
    /// Whether there is an accepted share grant (Stage B - currently always false).
    /// </summary>
    public bool HasAcceptedShareGrant { get; init; }
    
    /// <summary>
    /// Creates context for self-profile access (owner accessing own data).
    /// </summary>
    public static FieldResourceContext ForSelfProfile(string ownerSub)
    {
        return new FieldResourceContext { ResourceOwnerSub = ownerSub };
    }
    
    /// <summary>
    /// Creates context for negotiation-related access.
    /// </summary>
    public static FieldResourceContext ForNegotiation(string ownerSub, string? counterpartySub)
    {
        return new FieldResourceContext
        {
            ResourceOwnerSub = ownerSub,
            CounterpartySub = counterpartySub
        };
    }
}
