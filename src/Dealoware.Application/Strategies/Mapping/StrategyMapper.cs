using Dealoware.Application.Strategies.Dtos;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Strategies;

namespace Dealoware.Application.Strategies.Mapping;

/// <summary>
/// Maps Strategy domain entities to response DTOs with field ACL projection.
/// </summary>
public static class StrategyMapper
{
    /// <summary>
    /// Maps a Strategy to a StrategyResponse with field ACL projection.
    /// StrategyBody is only included if the principal has Read access.
    /// </summary>
    public static StrategyResponse ToResponse(Strategy strategy, FieldPrincipal principal, IFieldPolicy fieldPolicy)
    {
        var resourceContext = FieldResourceContext.ForSelfProfile(strategy.OwnerParticipantId);

        var canReadStrategyBody = fieldPolicy.Evaluate(
            principal, FieldClass.StrategyBody, FieldAction.Read, resourceContext);

        return new StrategyResponse
        {
            Id = strategy.Id,
            OwnerParticipantId = strategy.OwnerParticipantId,
            Name = strategy.Name,
            StrategyBody = canReadStrategyBody ? strategy.StrategyBody : null,
            IncludesStrategyBody = canReadStrategyBody,
            CreatedAt = strategy.CreatedAt,
            UpdatedAt = strategy.UpdatedAt
        };
    }
}
