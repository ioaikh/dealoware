namespace Dealoware.Domain.Artifacts;

/// <summary>
/// D1: Subject entity with name, description, properties, and facts.
/// </summary>
public sealed class SubjectEntity
{
    public Guid Id { get; private set; }
    
    public string Name { get; private set; } = string.Empty;
    
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Typed properties (name/type/value tuples)
    /// </summary>
    public IReadOnlyList<EntityProperty> Properties => _properties;
    private List<EntityProperty> _properties = new();

    /// <summary>
    /// Free-form facts (strings)
    /// </summary>
    public IReadOnlyList<string> Facts => _facts;
    private List<string> _facts = new();

    private SubjectEntity() { }

    public static SubjectEntity Create(
        string name,
        string description,
        IEnumerable<EntityProperty>? properties = null,
        IEnumerable<string>? facts = null)
    {
        var entity = new SubjectEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description
        };

        if (properties is not null)
            entity._properties.AddRange(properties);
        
        if (facts is not null)
            entity._facts.AddRange(facts);

        return entity;
    }

    /// <summary>Longest name and longest description, in characters (the column widths).</summary>
    public const int MaxTextLength = 4096;

    /// <summary>
    /// Admin edit path E3: a required name of at most <see cref="MaxTextLength"/> characters.
    /// </summary>
    /// <returns>False, with nothing changed, when the name is empty or too long</returns>
    public bool UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var trimmed = name.Trim();
        if (trimmed.Length > MaxTextLength)
            return false;

        Name = trimmed;
        return true;
    }

    /// <summary>
    /// Admin edit path E4: an optional description, which may be empty, of at
    /// most <see cref="MaxTextLength"/> characters.
    /// </summary>
    /// <returns>False, with nothing changed, when the description is too long</returns>
    public bool UpdateDescription(string? description)
    {
        description ??= string.Empty;
        if (description.Length > MaxTextLength)
            return false;

        Description = description;
        return true;
    }
}
