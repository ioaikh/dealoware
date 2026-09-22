namespace Dealoware.Application.Strategies.Dtos;

/// <summary>
/// Request to create a new strategy.
/// MVP Stage B minimal CRUD.
/// </summary>
public class CreateStrategyRequest
{
    /// <summary>
    /// Optional human-readable name for this strategy.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The strategy body content (opaque/text for MVP).
    /// FieldClass: StrategyBody - owner-only access.
    /// </summary>
    public string? StrategyBody { get; set; }
}
