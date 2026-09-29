using Dealoware.Api.Auth;
using Dealoware.Application.Budget.Dtos;
using Dealoware.Application.Budget.Mapping;
using Dealoware.Domain.Budget;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Dealoware.Api.Endpoints;

/// <summary>
/// Budget status endpoints for per-Participant meters.
/// MVP Stage C #68: A8-minimum meters + hard budgets (cutoff).
/// 
/// Security SD checklist points enforced:
/// 1. Per-Participant meters — returns only authenticated principal's own budget
/// 2. Hard cutoff — status shows exhausted state for cutoff enforcement
/// 3. Cross-tenant cannot access — wrong principal → 404 (uniform deny)
/// 4. Metered path wall-bound (#67) — budget status not leak/escalation channel
/// 5. Authn fail-closed — unauth → 401; no budget burn; no private leak
/// 
/// OUT: Mature metering/analytics (V3); billing/escrow; owner admin UI.
/// </summary>
public static class BudgetEndpoints
{
    public static void MapBudgetEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/budget");

        group.MapGet("/status", GetBudgetStatus)
            .WithName("GetBudgetStatus")
            .Produces<BudgetStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Gets budget status for the authenticated principal.
    /// Returns ONLY the principal's own budget — cross-tenant access denied.
    /// 
    /// Security:
    /// - Requires valid #5 principal (unauth → 401)
    /// - Returns only authenticated owner's budget
    /// - Cross-tenant / wrong principal → 404 (uniform deny; no leak)
    /// - Budget status does not contain private FieldClass values
    /// </summary>
    private static async Task<IResult> GetBudgetStatus(
        HttpContext context,
        IBudgetService budgetService,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var status = await budgetService.GetBudgetStatusAsync(sub, cancellationToken);

        if (status is null)
            return Results.NotFound(new { message = "Budget not found" });

        var response = BudgetMapper.ToResponse(status);
        return Results.Ok(response);
    }
}
