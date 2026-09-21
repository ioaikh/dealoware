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
