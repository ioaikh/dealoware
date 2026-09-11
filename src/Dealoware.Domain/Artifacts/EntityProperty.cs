namespace Dealoware.Domain.Artifacts;

/// <summary>
/// D1: Property of a subject entity (name/type/value tuple).
/// </summary>
public sealed class EntityProperty
{
    public Guid Id { get; private set; }
    
    public string Name { get; private set; } = string.Empty;
    
    public string Type { get; private set; } = string.Empty;
    
    public string Value { get; private set; } = string.Empty;

    private EntityProperty() { }

    public static EntityProperty Create(string name, string type, string value)
    {
        return new EntityProperty
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Value = value
        };
    }
}
