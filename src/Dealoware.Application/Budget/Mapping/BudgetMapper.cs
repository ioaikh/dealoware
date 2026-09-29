using Dealoware.Application.Budget.Dtos;
using Dealoware.Domain.Budget;

namespace Dealoware.Application.Budget.Mapping;

/// <summary>
/// Maps between Budget domain entities and DTOs.
/// Stage C #68: A8-minimum budget status mapping.
/// </summary>
public static class BudgetMapper
{
    public static BudgetStatusResponse ToResponse(BudgetStatus status)
    {
        return new BudgetStatusResponse
        {
            LimitUnits = status.LimitUnits,
            UsedUnits = status.UsedUnits,
            RemainingUnits = status.RemainingUnits,
            IsExhausted = status.IsExhausted,
            UsagePercentage = status.UsagePercentage
        };
    }
}
