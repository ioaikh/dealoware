namespace Dealoware.Domain.FieldAcl;

/// <summary>
/// Context for field policy evaluation.
/// Carries ownership and relationship signals needed for access decisions.
/// 
/// Stage B (#42): HasAcceptGrant enables ShareOutbound(ContactEmail) after Accept.
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
    /// Whether there is an Accept grant for ShareOutbound.
    /// Stage B (#42): True when Accept is recorded on the offer/negotiation,
    /// enabling ShareOutbound(ContactEmail) to the authorized counterparty.
    /// </summary>
    public bool HasAcceptGrant { get; init; }
    
    /// <summary>
    /// Creates context for self-profile access (owner accessing own data).
    /// </summary>
    public static FieldResourceContext ForSelfProfile(string ownerSub)
    {
        return new FieldResourceContext { ResourceOwnerSub = ownerSub };
    }
    
    /// <summary>
    /// Creates context for negotiation-related access (pre-Accept, no grant).
    /// </summary>
    public static FieldResourceContext ForNegotiation(string ownerSub, string? counterpartySub)
    {
        return new FieldResourceContext
        {
            ResourceOwnerSub = ownerSub,
            CounterpartySub = counterpartySub,
            HasAcceptGrant = false
        };
    }
    
    /// <summary>
    /// Creates context for post-Accept access with ShareOutbound grant.
    /// Stage B (#42): After Accept, counterparty receives ContactEmail via ShareOutbound.
    /// </summary>
    public static FieldResourceContext ForAcceptedNegotiation(string ownerSub, string counterpartySub)
    {
        return new FieldResourceContext
        {
            ResourceOwnerSub = ownerSub,
            CounterpartySub = counterpartySub,
            HasAcceptGrant = true
        };
    }
}
