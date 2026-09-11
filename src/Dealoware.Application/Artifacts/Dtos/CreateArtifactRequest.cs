using System.Text.Json.Serialization;

namespace Dealoware.Application.Artifacts.Dtos;

/// <summary>
/// Request DTO for creating an artifact.
/// Uses explicit properties to prevent secret-smuggling via unknown fields.
/// JsonExtensionData is NOT used — unknown fields are ignored by default.
/// </summary>
public sealed record CreateArtifactRequest
{
    /// <summary>
    /// D1: Subject entities (1..n required, max 50)
    /// </summary>
    [JsonPropertyName("entities")]
    public List<CreateSubjectEntityDto>? Entities { get; init; }

    /// <summary>
    /// D2: Intent (required, e.g., buy, sell, rent, exchange)
    /// </summary>
    [JsonPropertyName("intent")]
    public string? Intent { get; init; }

    /// <summary>
    /// D3: Values (0..n, max 20, at most one per currency)
    /// </summary>
    [JsonPropertyName("values")]
    public List<CreateValueDto>? Values { get; init; }

    /// <summary>
    /// D4: Locations (0..n strings, max 50)
    /// </summary>
    [JsonPropertyName("locations")]
    public List<string>? Locations { get; init; }

    /// <summary>
    /// D5: Time periods (0..n, max 50)
    /// </summary>
    [JsonPropertyName("timePeriods")]
    public List<CreateTimePeriodDto>? TimePeriods { get; init; }
}

public sealed record CreateSubjectEntityDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Properties (0..n, max 50 per entity)
    /// </summary>
    [JsonPropertyName("properties")]
    public List<CreatePropertyDto>? Properties { get; init; }

    /// <summary>
    /// Facts (0..n strings, max 100 per entity)
    /// </summary>
    [JsonPropertyName("facts")]
    public List<string>? Facts { get; init; }
}

public sealed record CreatePropertyDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

public sealed record CreateValueDto
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }
}

public sealed record CreateTimePeriodDto
{
    [JsonPropertyName("start")]
    public DateTimeOffset Start { get; init; }

    [JsonPropertyName("end")]
    public DateTimeOffset End { get; init; }
}
