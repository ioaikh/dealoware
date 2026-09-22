namespace Dealoware.Domain.FieldAcl;

/// <summary>
/// Evaluates field-level access control.
/// Per Spec §1: Evaluate on API/DB projection paths; deny-by-default for unknown classes.
/// </summary>
public interface IFieldPolicy
{
    /// <summary>
    /// Evaluates whether a principal can perform an action on a field.
    /// </summary>
    /// <param name="principal">The principal requesting access.</param>
    /// <param name="fieldClass">The class of field being accessed.</param>
    /// <param name="action">The action being performed (Read/Write/List/ShareOutbound).</param>
    /// <param name="resourceContext">Context about the resource (ownership, relationships).</param>
    /// <returns>True if access is allowed, false if denied.</returns>
    bool Evaluate(FieldPrincipal principal, FieldClass fieldClass, FieldAction action, FieldResourceContext resourceContext);
    
    /// <summary>
    /// Checks if a field class is registered in the policy.
    /// Unknown/unregistered classes default to deny.
    /// </summary>
    bool IsRegistered(FieldClass fieldClass);
}
