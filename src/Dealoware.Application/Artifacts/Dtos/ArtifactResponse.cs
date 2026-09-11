using System.Text.Json.Serialization;

namespace Dealoware.Application.Artifacts.Dtos;

/// <summary>
/// Response DTO for an artifact.
/// </summary>
public sealed record ArtifactResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("ownerParticipantId")]
    public string OwnerParticipantId { get; init; } = string.Empty;

    [JsonPropertyName("entities")]
    public List<SubjectEntityDto> Entities { get; init; } = new();

    [JsonPropertyName("intent")]
    public string Intent { get; init; } = string.Empty;

    [JsonPropertyName("values")]
    public List<ValueDto> Values { get; init; } = new();

    [JsonPropertyName("locations")]
    public List<string> Locations { get; init; } = new();

    [JsonPropertyName("timePeriods")]
    public List<TimePeriodDto> TimePeriods { get; init; } = new();

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record SubjectEntityDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("properties")]
    public List<PropertyDto> Properties { get; init; } = new();

    [JsonPropertyName("facts")]
    public List<string> Facts { get; init; } = new();
}

public sealed record PropertyDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;
}

public sealed record ValueDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;
}

public sealed record TimePeriodDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("start")]
    public DateTimeOffset Start { get; init; }

    [JsonPropertyName("end")]
    public DateTimeOffset End { get; init; }
}
