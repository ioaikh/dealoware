using System.Text.Json.Serialization;

namespace Dealoware.Application.Artifacts.Dtos;

/// <summary>
/// Discovery-safe response DTO for search results.
/// 
/// SECURITY: This DTO intentionally OMITS sensitive fields per #40 / #31 / #18:
/// - OwnerParticipantId: OMITTED - prevents inventory enumeration
/// - LoginEmail: OMITTED - never discoverable (#31 FieldClass)
/// - ContactEmail: OMITTED - never discoverable without accept (#31 / #42 HOLD)
/// - StrategyBody: OMITTED - Stage B #41 Strategy ACL (out of scope)
/// - Auth secrets: OMITTED - never in any DTO
/// 
/// Only Artifact D1-D5 fields already on path are projected:
/// - Subject entities (name, description, properties, facts)
/// - Intent
/// - Values
/// - Locations
/// - Time periods
/// 
/// See: Issue #40 AC - "search results return only Artifact / negotiation-scoped 
/// fields allowed for discovery — serializers omit denied classes"
/// </summary>
public sealed record DiscoverableArtifactResponse
{
    /// <summary>
    /// Artifact identifier for reference in discovery results.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>
    /// D1: Subject entities (discoverable - name/description/properties/facts)
    /// </summary>
    [JsonPropertyName("entities")]
    public List<DiscoverableSubjectEntityDto> Entities { get; init; } = new();

    /// <summary>
    /// D2: Intent (e.g., buy, sell, rent, exchange)
    /// </summary>
    [JsonPropertyName("intent")]
    public string Intent { get; init; } = string.Empty;

    /// <summary>
    /// D3: Values (amount/currency pairs)
    /// </summary>
    [JsonPropertyName("values")]
    public List<DiscoverableValueDto> Values { get; init; } = new();

    /// <summary>
    /// D4: Locations
    /// </summary>
    [JsonPropertyName("locations")]
    public List<string> Locations { get; init; } = new();

    /// <summary>
    /// D5: Time periods
    /// </summary>
    [JsonPropertyName("timePeriods")]
    public List<DiscoverableTimePeriodDto> TimePeriods { get; init; } = new();

    /// <summary>
    /// When the artifact was created (for sorting/freshness).
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Discoverable subject entity (no owner/private fields).
/// </summary>
public sealed record DiscoverableSubjectEntityDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("properties")]
    public List<DiscoverablePropertyDto> Properties { get; init; } = new();

    [JsonPropertyName("facts")]
    public List<string> Facts { get; init; } = new();
}

/// <summary>
/// Discoverable property (no IDs that could leak ownership).
/// </summary>
public sealed record DiscoverablePropertyDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;
}

/// <summary>
/// Discoverable value (amount/currency).
/// </summary>
public sealed record DiscoverableValueDto
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;
}

/// <summary>
/// Discoverable time period.
/// </summary>
public sealed record DiscoverableTimePeriodDto
{
    [JsonPropertyName("start")]
    public DateTimeOffset Start { get; init; }

    [JsonPropertyName("end")]
    public DateTimeOffset End { get; init; }
}
