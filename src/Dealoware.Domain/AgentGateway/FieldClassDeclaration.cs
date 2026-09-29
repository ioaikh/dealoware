namespace Dealoware.Domain.AgentGateway;

using Dealoware.Domain.FieldAcl;

/// <summary>
/// Declares which FieldClasses a tool may access and with what actions.
/// Tools must declare all FieldClasses they Read or ShareOutbound.
/// Undeclared access attempts are denied.
/// </summary>
public sealed class FieldClassDeclaration
{
    public FieldClass FieldClass { get; }
    public IReadOnlySet<FieldAction> AllowedActions { get; }

    private FieldClassDeclaration(FieldClass fieldClass, IReadOnlySet<FieldAction> allowedActions)
    {
        FieldClass = fieldClass ?? throw new ArgumentNullException(nameof(fieldClass));
        AllowedActions = allowedActions ?? throw new ArgumentNullException(nameof(allowedActions));
    }

    public static FieldClassDeclaration ForRead(FieldClass fieldClass)
    {
        return new FieldClassDeclaration(fieldClass, new HashSet<FieldAction> { FieldAction.Read });
    }

    public static FieldClassDeclaration ForReadWrite(FieldClass fieldClass)
    {
        return new FieldClassDeclaration(fieldClass, new HashSet<FieldAction> { FieldAction.Read, FieldAction.Write });
    }

    public static FieldClassDeclaration ForShareOutbound(FieldClass fieldClass)
    {
        return new FieldClassDeclaration(fieldClass, new HashSet<FieldAction> { FieldAction.ShareOutbound });
    }

    public static FieldClassDeclaration ForReadAndShare(FieldClass fieldClass)
    {
        return new FieldClassDeclaration(fieldClass, new HashSet<FieldAction> { FieldAction.Read, FieldAction.ShareOutbound });
    }

    public bool AllowsAction(FieldAction action)
    {
        return AllowedActions.Contains(action);
    }
}
