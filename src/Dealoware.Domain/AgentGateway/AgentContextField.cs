namespace Dealoware.Domain.AgentGateway;

using Dealoware.Domain.FieldAcl;

/// <summary>
/// Represents a field value in agent context with its FieldClass classification.
/// Used by the scrubber to evaluate and strip denied fields before model context.
/// </summary>
public sealed class AgentContextField
{
    public string Name { get; }
    public FieldClass FieldClass { get; }
    public object? Value { get; }

    private AgentContextField(string name, FieldClass fieldClass, object? value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FieldClass = fieldClass ?? throw new ArgumentNullException(nameof(fieldClass));
        Value = value;
    }

    public static AgentContextField Create(string name, FieldClass fieldClass, object? value)
    {
        return new AgentContextField(name, fieldClass, value);
    }

    public static AgentContextField ForDisplayName(object? value)
    {
        return Create("displayName", FieldClass.DisplayName, value);
    }

    public static AgentContextField ForContactEmail(object? value)
    {
        return Create("contactEmail", FieldClass.ContactEmail, value);
    }

    public static AgentContextField ForLoginEmail(object? value)
    {
        return Create("loginEmail", FieldClass.LoginEmail, value);
    }

    public static AgentContextField ForStrategyBody(object? value)
    {
        return Create("strategyBody", FieldClass.StrategyBody, value);
    }
}
