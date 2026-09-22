using Dealoware.Api.Auth;
using Dealoware.Application.Profile.Dtos;
using Dealoware.Application.Profile.Mapping;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Dealoware.Api.Endpoints;

public static class ProfileEndpoints
{
    /// <summary>
    /// Maps authenticated self-profile endpoints.
    /// GET /profile - Returns profile with field ACL projection.
    /// PATCH /profile - Updates profile fields (subject to ACL).
    /// 
    /// Protected paths require authenticated principal (#5).
    /// Field projection applies IFieldPolicy - denied fields are omitted.
    /// Error responses never contain private fields.
    /// </summary>
    public static void MapProfileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/profile");

        group.MapGet("", GetProfile)
            .WithName("GetProfile")
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPatch("", UpdateProfile)
            .WithName("UpdateProfile")
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Get authenticated self-profile with field ACL projection.
    /// Denied fields are omitted from response.
    /// </summary>
    private static async Task<IResult> GetProfile(
        HttpContext context,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        IFieldPolicy fieldPolicy,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (sub is null)
        {
            return Results.Unauthorized();
        }

        var participant = await participantRepository.GetBySubAsync(sub, cancellationToken);
        if (participant is null || !participant.IsActive)
        {
            return Results.Unauthorized();
        }

        var principal = FieldPrincipal.User(sub);
        var response = ProfileMapper.ToResponse(participant, principal, fieldPolicy);
        
        return Results.Ok(response);
    }

    /// <summary>
    /// Update authenticated self-profile fields.
    /// Only fields allowed by ACL for the principal can be updated.
    /// </summary>
    private static async Task<IResult> UpdateProfile(
        HttpContext context,
        UpdateProfileRequest request,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        IFieldPolicy fieldPolicy,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (sub is null)
        {
            return Results.Unauthorized();
        }

        var participant = await participantRepository.GetBySubAsync(sub, cancellationToken);
        if (participant is null || !participant.IsActive)
        {
            return Results.Unauthorized();
        }

        var principal = FieldPrincipal.User(sub);
        var resourceContext = FieldResourceContext.ForSelfProfile(sub);

        if (request.DisplayName is not null)
        {
            var canWriteDisplayName = fieldPolicy.Evaluate(
                principal, FieldClass.DisplayName, FieldAction.Write, resourceContext);
            
            if (!canWriteDisplayName)
            {
                return Results.Json(
                    new ProblemDetails 
                    { 
                        Title = "Access denied",
                        Status = StatusCodes.Status403Forbidden
                    },
                    statusCode: StatusCodes.Status403Forbidden);
            }
            participant.UpdateDisplayName(request.DisplayName);
        }

        if (request.LoginEmail is not null)
        {
            var canWriteLoginEmail = fieldPolicy.Evaluate(
                principal, FieldClass.LoginEmail, FieldAction.Write, resourceContext);
            
            if (!canWriteLoginEmail)
            {
                return Results.Json(
                    new ProblemDetails 
                    { 
                        Title = "Access denied",
                        Status = StatusCodes.Status403Forbidden
                    },
                    statusCode: StatusCodes.Status403Forbidden);
            }
            participant.UpdateLoginEmail(request.LoginEmail);
        }

        if (request.ContactEmail is not null)
        {
            var canWriteContactEmail = fieldPolicy.Evaluate(
                principal, FieldClass.ContactEmail, FieldAction.Write, resourceContext);
            
            if (!canWriteContactEmail)
            {
                return Results.Json(
                    new ProblemDetails 
                    { 
                        Title = "Access denied",
                        Status = StatusCodes.Status403Forbidden
                    },
                    statusCode: StatusCodes.Status403Forbidden);
            }
            participant.UpdateContactEmail(request.ContactEmail);
        }

        await participantRepository.SaveChangesAsync(cancellationToken);

        var response = ProfileMapper.ToResponse(participant, principal, fieldPolicy);
        return Results.Ok(response);
    }
}
