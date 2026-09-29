using Dealoware.Api.Auth;
using Dealoware.Application.Assistant.Dtos;
using Dealoware.Application.Assistant.Mapping;
using Dealoware.Domain.Assistant;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Dealoware.Api.Endpoints;

/// <summary>
/// Thin OwnAgent-only Strategy-driven AI Assistant endpoints.
/// MVP Stage C #66: X1 thin assistant behind #67 gateway.
/// 
/// Security SD checklist points enforced:
/// 1. OwnAgent-only 1:1 — acts only for owning Participant
/// 2. StrategyBody via FieldPolicy — OwnAgent R/W for owner only
/// 3. Mandatory bind #67 — platform tools/gateway only
/// 4. No LoginEmail — never in context packs/tool outputs
/// 5. Authn/IDOR fail-closed — unauth 401; wrong principal 403/404
/// 
/// OUT: Fuller Assistant (V1); free-form engine (V1); A5 sandbox (V4);
///      Multi-party; LLM provision; #68 meters; #69 UI.
/// </summary>
public static class AssistantEndpoints
{
    public static void MapAssistantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/assistant");

        group.MapPost("/invoke", InvokeAssistant)
            .WithName("InvokeAssistant")
            .Produces<AssistantInvokeResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/capabilities", GetCapabilities)
            .WithName("GetAssistantCapabilities")
            .Produces<AssistantCapabilitiesResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Invokes the thin OwnAgent assistant for the authenticated owner.
    /// All tool I/O goes through #67 gateway exclusively.
    /// 
    /// Security:
    /// - Requires valid #5 principal (unauth → 401)
    /// - OwnAgent-only 1:1 — acts only for authenticated owner
    /// - StrategyBody via FieldPolicy OwnAgent R/W
    /// - No LoginEmail in context packs or tool outputs
    /// - Wrong principal / cross-tenant → fail-closed (404 for strategy, tool errors)
    /// </summary>
    private static async Task<IResult> InvokeAssistant(
        HttpContext context,
        AssistantInvokeRequest request,
        IAssistantService assistantService,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var domainRequest = AssistantMapper.ToDomain(request);
        var result = await assistantService.InvokeAsync(sub, domainRequest);

        var response = AssistantMapper.ToResponse(result);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "STRATEGY_NOT_FOUND" => Results.NotFound(response),
                "TOOL_NOT_ALLOWED" => Results.BadRequest(response),
                "UNAUTHENTICATED" => Results.Unauthorized(),
                "NOT_AGENT" => Results.Forbid(),
                "ACCESS_DENIED" => Results.Forbid(),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Gets available assistant capabilities for the authenticated owner.
    /// Only returns tools available via #67 gateway.
    /// 
    /// Security:
    /// - Requires valid #5 principal (unauth → 401)
    /// - CRITICAL: No LoginEmail tool exposed
    /// </summary>
    private static async Task<IResult> GetCapabilities(
        HttpContext context,
        IAssistantService assistantService,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        var capabilities = await assistantService.GetCapabilitiesAsync(sub);
        var response = AssistantMapper.ToResponse(capabilities);

        return Results.Ok(response);
    }
}
