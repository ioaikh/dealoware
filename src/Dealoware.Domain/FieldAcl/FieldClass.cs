namespace Dealoware.Domain.FieldAcl;

/// <summary>
/// Represents a classification of field data for ACL purposes.
/// This is an extensible registry - new field classes can be added
/// without rewriting the dual-wall architecture.
/// 
/// CEO examples (starters, not exhaustive):
/// - LoginEmail: User's login credential email (highly sensitive)
/// - ContactEmail: Contact email for business communication
/// - DisplayName: Soft/illustrative field (non-blocking if deferred)
/// </summary>
public sealed class FieldClass
{
    public string Name { get; }
    
    private FieldClass(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
    
    /// <summary>
    /// Login credential email - User R/W; OwnAgent Deny all; counterparty/stranger Deny.
    /// </summary>
    public static readonly FieldClass LoginEmail = new("LoginEmail");
    
    /// <summary>
    /// Contact email for business - User R/W; OwnAgent Read; counterparty Deny; ShareOutbound Deny.
    /// </summary>
    public static readonly FieldClass ContactEmail = new("ContactEmail");
    
    /// <summary>
    /// Display name - soft/illustrative, non-blocking if deferred.
    /// </summary>
    public static readonly FieldClass DisplayName = new("DisplayName");
    
    /// <summary>
    /// Strategy body - negotiation strategy content (MVP Stage B).
    /// User R/W; OwnAgent R/W (for owner); Counterparty Deny; Stranger/Unauth Deny.
    /// Never exposed to counterparty via Negotiation DTOs. CoreOwner Deny (no dump).
    /// </summary>
    public static readonly FieldClass StrategyBody = new("StrategyBody");

    /// <summary>Participant operational active/suspended flag. CoreOwner R/W.</summary>
    public static readonly FieldClass ParticipantActive = new("ParticipantActive");

    /// <summary>Artifact name / title / subject / intent. CoreOwner R/W.</summary>
    public static readonly FieldClass ArtifactName = new("ArtifactName");

    /// <summary>Artifact description. CoreOwner R/W.</summary>
    public static readonly FieldClass ArtifactDescription = new("ArtifactDescription");

    /// <summary>Artifact owner Participant id. CoreOwner R/W when resource rules allow.</summary>
    public static readonly FieldClass ArtifactOwnerParticipantId = new("ArtifactOwnerParticipantId");

    /// <summary>Negotiation status. CoreOwner R/W.</summary>
    public static readonly FieldClass NegotiationStatus = new("NegotiationStatus");

    /// <summary>Negotiation end / expiry. CoreOwner R/W.</summary>
    public static readonly FieldClass NegotiationEndsAt = new("NegotiationEndsAt");

    /// <summary>Negotiation party A. CoreOwner Read (no silent party rewrite).</summary>
    public static readonly FieldClass NegotiationPartyA = new("NegotiationPartyA");

    /// <summary>Negotiation party B. CoreOwner Read (no silent party rewrite).</summary>
    public static readonly FieldClass NegotiationPartyB = new("NegotiationPartyB");

    /// <summary>Negotiation artifact id. CoreOwner Read.</summary>
    public static readonly FieldClass NegotiationArtifactId = new("NegotiationArtifactId");

    /// <summary>Offer amount. CoreOwner R/W.</summary>
    public static readonly FieldClass OfferAmount = new("OfferAmount");

    /// <summary>Offer currency. CoreOwner R/W.</summary>
    public static readonly FieldClass OfferCurrency = new("OfferCurrency");

    /// <summary>Offer terms. CoreOwner R/W.</summary>
    public static readonly FieldClass OfferTerms = new("OfferTerms");

    /// <summary>Offer status. CoreOwner R/W.</summary>
    public static readonly FieldClass OfferStatus = new("OfferStatus");

    /// <summary>Offer parent negotiation id. CoreOwner Read.</summary>
    public static readonly FieldClass OfferNegotiationId = new("OfferNegotiationId");

    /// <summary>Soft-delete marker (DeletedAt) on the four admin entities. CoreOwner Read.</summary>
    public static readonly FieldClass SoftDeletedAt = new("SoftDeletedAt");

    /// <summary>Optimistic concurrency token. CoreOwner Read.</summary>
    public static readonly FieldClass EntityVersion = new("EntityVersion");

    /// <summary>Created-at timestamp. CoreOwner Read.</summary>
    public static readonly FieldClass EntityCreatedAt = new("EntityCreatedAt");
    
    /// <summary>
    /// Creates a custom FieldClass for extension.
    /// Unknown/unregistered classes default to deny.
    /// </summary>
    public static FieldClass Custom(string name) => new(name);
    
    public override bool Equals(object? obj) => obj is FieldClass fc && fc.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
    
    public static bool operator ==(FieldClass? left, FieldClass? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));
    
    public static bool operator !=(FieldClass? left, FieldClass? right) => !(left == right);
}
