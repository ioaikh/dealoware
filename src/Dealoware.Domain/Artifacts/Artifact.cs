namespace Dealoware.Domain.Artifacts;

/// <summary>
/// Artifact aggregate root representing a negotiable item with D1-D5 properties.
/// Owner-scoped: only the owning participant can access their artifacts.
/// </summary>
public sealed class Artifact
{
    public Guid Id { get; private set; }
    
    /// <summary>
    /// The participant who owns this artifact.
    /// Mapped from the authenticated principal's OIDC sub claim.
    /// Format: "participant:{guid}" for self-registered participants.
    /// See: Dealoware.Domain.Participants.Participant.Sub
    /// </summary>
    public string OwnerParticipantId { get; private set; } = string.Empty;

    /// <summary>
    /// D1: Subject entities (1..n required)
    /// </summary>
    public IReadOnlyList<SubjectEntity> Entities => _entities;
    private List<SubjectEntity> _entities = new();

    /// <summary>
    /// D2: Intent (e.g., buy, sell, rent, exchange, provide, consume)
    /// </summary>
    public string Intent { get; private set; } = string.Empty;

    /// <summary>
    /// D3: Values (0..n, at most one per currency, case-insensitive)
    /// </summary>
    public IReadOnlyList<ArtifactValue> Values => _values;
    private List<ArtifactValue> _values = new();

    /// <summary>
    /// D4: Locations (0..n)
    /// </summary>
    public IReadOnlyList<string> Locations => _locations;
    private List<string> _locations = new();

    /// <summary>
    /// D5: Time periods (0..n)
    /// </summary>
    public IReadOnlyList<TimePeriod> TimePeriods => _timePeriods;
    private List<TimePeriod> _timePeriods = new();

    public DateTimeOffset CreatedAt { get; private set; }

    private Artifact() { }

    public static Artifact Create(
        string ownerParticipantId,
        IEnumerable<SubjectEntity> entities,
        string intent,
        IEnumerable<ArtifactValue>? values = null,
        IEnumerable<string>? locations = null,
        IEnumerable<TimePeriod>? timePeriods = null)
    {
        var artifact = new Artifact
        {
            Id = Guid.NewGuid(),
            OwnerParticipantId = ownerParticipantId,
            Intent = intent,
            CreatedAt = DateTimeOffset.UtcNow
        };

        artifact._entities.AddRange(entities);
        
        if (values is not null)
            artifact._values.AddRange(values);
        
        if (locations is not null)
            artifact._locations.AddRange(locations);
        
        if (timePeriods is not null)
            artifact._timePeriods.AddRange(timePeriods);

        return artifact;
    }
}
