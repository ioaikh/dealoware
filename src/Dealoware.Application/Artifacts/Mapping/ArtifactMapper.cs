using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Domain.Artifacts;

namespace Dealoware.Application.Artifacts.Mapping;

public static class ArtifactMapper
{
    public static Artifact ToDomain(CreateArtifactRequest request, string ownerParticipantId)
    {
        var entities = request.Entities!.Select(e => SubjectEntity.Create(
            e.Name ?? string.Empty,
            e.Description ?? string.Empty,
            e.Properties?.Select(p => EntityProperty.Create(
                p.Name ?? string.Empty,
                p.Type ?? string.Empty,
                p.Value ?? string.Empty)),
            e.Facts
        )).ToList();

        var values = request.Values?.Select(v => ArtifactValue.Create(
            v.Amount,
            v.Currency ?? string.Empty
        )).ToList();

        var timePeriods = request.TimePeriods?.Select(t => TimePeriod.Create(
            t.Start,
            t.End
        )).ToList();

        return Artifact.Create(
            ownerParticipantId,
            entities,
            request.Intent ?? string.Empty,
            values,
            request.Locations,
            timePeriods);
    }

    public static ArtifactResponse ToResponse(Artifact artifact)
    {
        return new ArtifactResponse
        {
            Id = artifact.Id,
            OwnerParticipantId = artifact.OwnerParticipantId,
            Entities = artifact.Entities.Select(e => new SubjectEntityDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Properties = e.Properties.Select(p => new PropertyDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Type = p.Type,
                    Value = p.Value
                }).ToList(),
                Facts = e.Facts.ToList()
            }).ToList(),
            Intent = artifact.Intent,
            Values = artifact.Values.Select(v => new ValueDto
            {
                Id = v.Id,
                Amount = v.Amount,
                Currency = v.Currency
            }).ToList(),
            Locations = artifact.Locations.ToList(),
            TimePeriods = artifact.TimePeriods.Select(t => new TimePeriodDto
            {
                Id = t.Id,
                Start = t.Start,
                End = t.End
            }).ToList(),
            CreatedAt = artifact.CreatedAt
        };
    }

    public static List<ArtifactResponse> ToResponseList(IEnumerable<Artifact> artifacts)
    {
        return artifacts.Select(ToResponse).ToList();
    }
}
