namespace Dealoware.Domain.FieldAcl;

/// <summary>
/// Default implementation of IFieldPolicy.
/// Implements deny-by-default and Stage A/B policy rows.
/// 
/// Policy Matrix (Stage A + Stage B #41/#42):
/// | FieldClass    | User | OwnAgent      | Counterparty          | Stranger | ShareOutbound              |
/// |---------------|------|---------------|-----------------------|----------|----------------------------|
/// | LoginEmail    | R/W  | Deny all      | Deny                  | Deny     | Deny (never shared)        |
/// | ContactEmail  | R/W  | Read only     | Deny until grant      | Deny     | Allow with HasAcceptGrant  |
/// | DisplayName   | R/W  | R/W (soft)    | Read (soft)           | Deny     | N/A                        |
/// | StrategyBody  | R/W  | R/W (owner)   | Deny                  | Deny     | Deny                       |
/// | Unknown       | Deny | Deny          | Deny                  | Deny     | Deny                       |
/// 
/// Stage B (#42): ShareOutbound(ContactEmail) allowed only when:
/// - HasAcceptGrant is true in resourceContext
/// - Principal is the authorized counterparty
/// - LoginEmail never shared via ShareOutbound
/// </summary>
public sealed class FieldPolicy : IFieldPolicy
{
    private static readonly HashSet<string> RegisteredFieldClasses = new(StringComparer.Ordinal)
    {
        FieldClass.LoginEmail.Name,
        FieldClass.ContactEmail.Name,
        FieldClass.DisplayName.Name,
        FieldClass.StrategyBody.Name
    };

    public bool IsRegistered(FieldClass fieldClass)
    {
        return RegisteredFieldClasses.Contains(fieldClass.Name);
    }

    public bool Evaluate(FieldPrincipal principal, FieldClass fieldClass, FieldAction action, FieldResourceContext resourceContext)
    {
        if (principal.Type == PrincipalType.Unauthenticated)
            return false;

        var effectiveType = principal.GetTypeForContext(resourceContext);
        
        if (effectiveType == PrincipalType.Unauthenticated)
            return false;

        if (!IsRegistered(fieldClass))
            return false;

        if (action == FieldAction.ShareOutbound)
            return EvaluateShareOutbound(effectiveType, fieldClass, resourceContext);

        return EvaluateRegisteredField(effectiveType, fieldClass, action);
    }
    
    /// <summary>
    /// Evaluates ShareOutbound action for Stage B contact-on-accept.
    /// ContactEmail: allowed only to counterparty when HasAcceptGrant.
    /// LoginEmail: never shared via ShareOutbound.
    /// </summary>
    private static bool EvaluateShareOutbound(PrincipalType principalType, FieldClass fieldClass, FieldResourceContext resourceContext)
    {
        if (fieldClass == FieldClass.LoginEmail)
            return false;
        
        if (fieldClass == FieldClass.ContactEmail)
        {
            return resourceContext.HasAcceptGrant && principalType == PrincipalType.Counterparty;
        }
        
        return false;
    }

    private static bool EvaluateRegisteredField(PrincipalType principalType, FieldClass fieldClass, FieldAction action)
    {
        if (fieldClass == FieldClass.LoginEmail)
            return EvaluateLoginEmail(principalType, action);
        
        if (fieldClass == FieldClass.ContactEmail)
            return EvaluateContactEmail(principalType, action);
        
        if (fieldClass == FieldClass.DisplayName)
            return EvaluateDisplayName(principalType, action);
        
        if (fieldClass == FieldClass.StrategyBody)
            return EvaluateStrategyBody(principalType, action);
        
        return false;
    }

    /// <summary>
    /// LoginEmail: User R/W; OwnAgent Deny all; counterparty/stranger Deny.
    /// </summary>
    private static bool EvaluateLoginEmail(PrincipalType principalType, FieldAction action)
    {
        return principalType switch
        {
            PrincipalType.User => action is FieldAction.Read or FieldAction.Write or FieldAction.List,
            PrincipalType.OwnAgent => false,
            PrincipalType.Counterparty => false,
            PrincipalType.Stranger => false,
            PrincipalType.Unauthenticated => false,
            _ => false
        };
    }

    /// <summary>
    /// ContactEmail: User R/W; OwnAgent Read; counterparty Deny; ShareOutbound Deny.
    /// </summary>
    private static bool EvaluateContactEmail(PrincipalType principalType, FieldAction action)
    {
        return principalType switch
        {
            PrincipalType.User => action is FieldAction.Read or FieldAction.Write or FieldAction.List,
            PrincipalType.OwnAgent => action == FieldAction.Read,
            PrincipalType.Counterparty => false,
            PrincipalType.Stranger => false,
            PrincipalType.Unauthenticated => false,
            _ => false
        };
    }

    /// <summary>
    /// DisplayName: Soft/illustrative - User R/W; OwnAgent R/W; counterparty Read; stranger Deny.
    /// Non-blocking if deferred.
    /// </summary>
    private static bool EvaluateDisplayName(PrincipalType principalType, FieldAction action)
    {
        return principalType switch
        {
            PrincipalType.User => action is FieldAction.Read or FieldAction.Write or FieldAction.List,
            PrincipalType.OwnAgent => action is FieldAction.Read or FieldAction.Write or FieldAction.List,
            PrincipalType.Counterparty => action == FieldAction.Read,
            PrincipalType.Stranger => false,
            PrincipalType.Unauthenticated => false,
            _ => false
        };
    }

    /// <summary>
    /// StrategyBody: User R/W; OwnAgent R/W (for owner); Counterparty Deny; Stranger/Unauth Deny.
    /// MVP Stage B - never exposed to counterparty via Negotiation DTOs.
    /// </summary>
    private static bool EvaluateStrategyBody(PrincipalType principalType, FieldAction action)
    {
        return principalType switch
        {
            PrincipalType.User => action is FieldAction.Read or FieldAction.Write or FieldAction.List,
            PrincipalType.OwnAgent => action is FieldAction.Read or FieldAction.Write or FieldAction.List,
            PrincipalType.Counterparty => false,
            PrincipalType.Stranger => false,
            PrincipalType.Unauthenticated => false,
            _ => false
        };
    }
}
