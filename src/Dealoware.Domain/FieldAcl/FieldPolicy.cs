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
        FieldClass.StrategyBody.Name,
        FieldClass.ParticipantActive.Name,
        FieldClass.ArtifactName.Name,
        FieldClass.ArtifactDescription.Name,
        FieldClass.ArtifactOwnerParticipantId.Name,
        FieldClass.NegotiationStatus.Name,
        FieldClass.NegotiationEndsAt.Name,
        FieldClass.NegotiationPartyA.Name,
        FieldClass.NegotiationPartyB.Name,
        FieldClass.NegotiationArtifactId.Name,
        FieldClass.OfferAmount.Name,
        FieldClass.OfferCurrency.Name,
        FieldClass.OfferTerms.Name,
        FieldClass.OfferStatus.Name,
        FieldClass.OfferNegotiationId.Name,
        FieldClass.SoftDeletedAt.Name,
        FieldClass.EntityVersion.Name,
        FieldClass.EntityCreatedAt.Name
    };

    public bool IsRegistered(FieldClass fieldClass)
    {
        return RegisteredFieldClasses.Contains(fieldClass.Name);
    }

    public bool Evaluate(FieldPrincipal principal, FieldClass fieldClass, FieldAction action, FieldResourceContext resourceContext)
    {
        if (principal.Type == PrincipalType.Unauthenticated)
            return false;

        if (!IsRegistered(fieldClass))
            return false;

        // CoreOwner is not remapped by resource ownership (not a Participant).
        if (principal.Type == PrincipalType.CoreOwner)
            return EvaluateCoreOwner(fieldClass, action);

        var effectiveType = principal.GetTypeForContext(resourceContext);
        
        if (effectiveType == PrincipalType.Unauthenticated)
            return false;

        if (action == FieldAction.ShareOutbound)
            return EvaluateShareOutbound(effectiveType, fieldClass, resourceContext);

        return EvaluateRegisteredField(effectiveType, fieldClass, action);
    }

    /// <summary>
    /// CoreOwner dual-wall rules (SA §3.7). Unregistered classes already denied above.
    /// StrategyBody and auth secrets: deny. LoginEmail write is not a generic edit.
    /// </summary>
    private static bool EvaluateCoreOwner(FieldClass fieldClass, FieldAction action)
    {
        if (action == FieldAction.ShareOutbound)
            return false;

        if (fieldClass == FieldClass.StrategyBody)
            return false;

        if (fieldClass == FieldClass.LoginEmail || fieldClass == FieldClass.ContactEmail)
            return action is FieldAction.Read or FieldAction.List;

        if (fieldClass == FieldClass.DisplayName || fieldClass == FieldClass.ParticipantActive)
            return action is FieldAction.Read or FieldAction.Write or FieldAction.List;

        if (fieldClass == FieldClass.ArtifactName
            || fieldClass == FieldClass.ArtifactDescription
            || fieldClass == FieldClass.ArtifactOwnerParticipantId)
            return action is FieldAction.Read or FieldAction.Write or FieldAction.List;

        if (fieldClass == FieldClass.NegotiationStatus || fieldClass == FieldClass.NegotiationEndsAt)
            return action is FieldAction.Read or FieldAction.Write or FieldAction.List;

        if (fieldClass == FieldClass.NegotiationPartyA
            || fieldClass == FieldClass.NegotiationPartyB
            || fieldClass == FieldClass.NegotiationArtifactId)
            return action is FieldAction.Read or FieldAction.List;

        if (fieldClass == FieldClass.OfferAmount
            || fieldClass == FieldClass.OfferCurrency
            || fieldClass == FieldClass.OfferTerms
            || fieldClass == FieldClass.OfferStatus)
            return action is FieldAction.Read or FieldAction.Write or FieldAction.List;

        if (fieldClass == FieldClass.OfferNegotiationId)
            return action is FieldAction.Read or FieldAction.List;

        if (fieldClass == FieldClass.SoftDeletedAt
            || fieldClass == FieldClass.EntityVersion
            || fieldClass == FieldClass.EntityCreatedAt)
            return action is FieldAction.Read or FieldAction.List;

        return false;
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
