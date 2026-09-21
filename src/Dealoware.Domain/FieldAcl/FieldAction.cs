namespace Dealoware.Domain.FieldAcl;

/// <summary>
/// Actions that can be performed on a field.
/// Used by IFieldPolicy.Evaluate to determine access.
/// </summary>
public enum FieldAction
{
    /// <summary>
    /// Read/view the field value in API responses.
    /// </summary>
    Read,
    
    /// <summary>
    /// Write/update the field value via API.
    /// </summary>
    Write,
    
    /// <summary>
    /// List/enumerate entities containing this field.
    /// </summary>
    List,
    
    /// <summary>
    /// Share field outbound (Stage B HOLD - currently always Deny).
    /// </summary>
    ShareOutbound
}
