namespace Dealoware.Domain.FieldAcl;

/// <summary>
/// Represents the principal making a field access request.
/// </summary>
public sealed class FieldPrincipal
{
    /// <summary>
    /// The principal's subject identifier (from JWT sub claim or API key).
    /// Null for unauthenticated requests.
    /// </summary>
    public string? Sub { get; init; }
    
    /// <summary>
    /// Type of principal (User, OwnAgent, Counterparty, Stranger, Unauthenticated).
    /// </summary>
    public PrincipalType Type { get; init; }
    
    /// <summary>
    /// Whether this is an agent acting on behalf of a user.
    /// Agents have restricted access to certain fields even for the owner.
    /// </summary>
    public bool IsAgent { get; init; }
    
    /// <summary>
    /// Creates an unauthenticated principal.
    /// </summary>
    public static FieldPrincipal Unauthenticated() => new()
    {
        Sub = null,
        Type = PrincipalType.Unauthenticated,
        IsAgent = false
    };
    
    /// <summary>
    /// Creates an authenticated user principal.
    /// </summary>
    public static FieldPrincipal User(string sub) => new()
    {
        Sub = sub,
        Type = PrincipalType.User,
        IsAgent = false
    };
    
    /// <summary>
    /// Creates an agent principal acting on behalf of a user.
    /// </summary>
    public static FieldPrincipal Agent(string ownerSub) => new()
    {
        Sub = ownerSub,
        Type = PrincipalType.OwnAgent,
        IsAgent = true
    };
    
    /// <summary>
    /// Determines the principal type relative to a resource context.
    /// </summary>
    public PrincipalType GetTypeForContext(FieldResourceContext context)
    {
        if (Type == PrincipalType.Unauthenticated)
            return PrincipalType.Unauthenticated;
        
        if (string.IsNullOrEmpty(Sub))
            return PrincipalType.Unauthenticated;
        
        if (Sub == context.ResourceOwnerSub)
            return IsAgent ? PrincipalType.OwnAgent : PrincipalType.User;
        
        if (!string.IsNullOrEmpty(context.CounterpartySub) && Sub == context.CounterpartySub)
            return PrincipalType.Counterparty;
        
        return PrincipalType.Stranger;
    }
}
