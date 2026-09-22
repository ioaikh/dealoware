namespace Dealoware.Application.Strategies.Dtos;

/// <summary>
/// Request to update an existing strategy.
/// MVP Stage B minimal CRUD.
/// Null values indicate no change (PATCH semantics).
/// </summary>
public class UpdateStrategyRequest
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
