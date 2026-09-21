namespace Dealoware.Domain.FieldAcl;

/// <summary>
/// Type of principal accessing a field.
/// Determines which policy row applies.
/// </summary>
public enum PrincipalType
{
    /// <summary>
    /// The resource owner (User) - typically has R/W access to their own fields.
    /// </summary>
    User,
    
    /// <summary>
    /// An agent acting on behalf of the owner.
    /// LoginEmail: Deny all; ContactEmail: Read only.
    /// </summary>
    OwnAgent,
    
    /// <summary>
    /// A counterparty in a negotiation.
    /// Deny for LoginEmail and ContactEmail in Stage A.
    /// </summary>
    Counterparty,
    
    /// <summary>
    /// A stranger with no relationship to the resource.
    /// Deny for all protected fields.
    /// </summary>
    Stranger,
    
    /// <summary>
    /// Unauthenticated request - always deny, return 401.
    /// </summary>
    Unauthenticated
}
