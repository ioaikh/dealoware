using Dealoware.Api.Auth;
using Dealoware.Api.RateLimiting;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Domain.Budget;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dealoware.Api.Endpoints;

public static class AuthEndpoints
{
    /// <summary>
    /// Maps authentication endpoints for participant registration and credential management.
    /// POST /auth/register and POST /auth/token are fixed-window rate limited per client IP
    /// (see RateLimiting in appsettings). CAPTCHA / proof-of-work remain out of scope for this PoC.
    /// </summary>
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", Register)
            .WithName("Register")
            .RequireRateLimiting(AuthRateLimitOptions.RegisterPolicy)
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status429TooManyRequests);

        group.MapPost("/token", IssueToken)
            .WithName("IssueToken")
            .RequireRateLimiting(AuthRateLimitOptions.TokenPolicy)
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests);

        group.MapPost("/revoke", Revoke)
            .WithName("Revoke")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/rotate-key", RotateKey)
            .WithName("RotateKey")
            .Produces<RotateKeyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Register a new participant and issue initial API key.
    /// This is a bootstrap endpoint - creates principal + credential in one call.
    /// Stage C #68: Also provisions initial budget for metered invocations.
    /// 
    /// Rate limited per client IP (AuthRateLimitOptions.RegisterPolicy).
    /// CAPTCHA / proof-of-work remain out of scope for this PoC.
    /// </summary>
    private static async Task<IResult> Register(
        HttpContext httpContext,
        RegisterRequest? request,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        IBudgetService budgetService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Dealoware.Api.Auth.Register");
        var partitionKey = AuthRateLimiting.GetPartitionKey(httpContext);
        var xff = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        logger.LogDebug(
            "Register request: PartitionKey={PartitionKey}, XFF={XffPresent}, RemoteIp={RemoteIp}",
            AuthRateLimiting.MaskIp(partitionKey),
            xff is not null,
            AuthRateLimiting.MaskIp(httpContext.Connection.RemoteIpAddress?.ToString()));
        
        var participant = Participant.Create(request?.DisplayName);
        var (credential, rawApiKey) = ApiKeyCredential.Create(participant.Id);

        await participantRepository.AddAsync(participant, cancellationToken);
        await apiKeyRepository.AddAsync(credential, cancellationToken);
        await participantRepository.SaveChangesAsync(cancellationToken);

        // Stage C #68: Provision initial budget for the new participant
        // A8-minimum default limit; mature analytics → V3
        await budgetService.EnsureBudgetExistsAsync(participant.Sub, 1000, cancellationToken);

        var response = new RegisterResponse
        {
            Sub = participant.Sub,
            DisplayName = participant.DisplayName,
            ApiKey = rawApiKey,
            ApiKeyPrefix = credential.KeyPrefix,
            CreatedAt = participant.CreatedAt
        };

        return Results.Created($"/auth/participants/{participant.Sub}", response);
    }

    /// <summary>
    /// Issue a JWT token using API key authentication.
    /// The API key must be valid and not revoked.
    /// </summary>
    private static async Task<IResult> IssueToken(
        HttpContext httpContext,
        TokenRequest request,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Dealoware.Api.Auth.Token");
        var partitionKey = AuthRateLimiting.GetPartitionKey(httpContext);
        var xff = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        logger.LogDebug(
            "Token request: PartitionKey={PartitionKey}, XFF={XffPresent}, RemoteIp={RemoteIp}",
            AuthRateLimiting.MaskIp(partitionKey),
            xff is not null,
            AuthRateLimiting.MaskIp(httpContext.Connection.RemoteIpAddress?.ToString()));
        
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return Results.Unauthorized();
        }

        var participant = await ValidateApiKeyAndGetParticipant(
            request.ApiKey, 
            participantRepository, 
            apiKeyRepository, 
            cancellationToken);

        if (participant is null)
        {
            return Results.Unauthorized();
        }

        var (token, _, expiresAt) = jwtService.IssueToken(participant);
        var expiresIn = (int)(expiresAt - DateTimeOffset.UtcNow).TotalSeconds;

        return Results.Ok(new TokenResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            Sub = participant.Sub
        });
    }

    /// <summary>
    /// Revoke a credential (API key or JWT).
    /// Requires authentication via Authorization header.
    /// </summary>
    private static async Task<IResult> Revoke(
        HttpContext context,
        RevokeRequest request,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        IRevokedTokenRepository revokedTokenRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);
        
        if (sub is null)
        {
            return Results.Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            var keyPrefix = ExtractKeyPrefix(request.ApiKey);
            if (keyPrefix is null)
            {
                return Results.BadRequest(new ProblemDetails 
                { 
                    Title = "Invalid API key format",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var credential = await apiKeyRepository.GetByKeyPrefixAsync(keyPrefix, cancellationToken);
            if (credential is null || !credential.VerifyKey(request.ApiKey))
            {
                return Results.BadRequest(new ProblemDetails 
                { 
                    Title = "API key not found or already revoked",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var participant = await participantRepository.GetByIdAsync(credential.ParticipantId, cancellationToken);
            if (participant?.Sub != sub)
            {
                return Results.Forbid();
            }

            credential.Revoke();
            await apiKeyRepository.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }

        if (!string.IsNullOrWhiteSpace(request.TokenJti))
        {
            var isAlreadyRevoked = await revokedTokenRepository.IsRevokedAsync(request.TokenJti, cancellationToken);
            if (isAlreadyRevoked)
            {
                return Results.NoContent();
            }

            var revokedToken = RevokedToken.Create(
                request.TokenJti, 
                sub, 
                DateTimeOffset.UtcNow.AddDays(7));
            
            await revokedTokenRepository.AddAsync(revokedToken, cancellationToken);
            await revokedTokenRepository.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }

        return Results.BadRequest(new ProblemDetails 
        { 
            Title = "Either ApiKey or TokenJti must be provided",
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Rotate API key - revoke current key and issue a new one.
    /// Requires authentication via the current API key in Authorization header.
    /// </summary>
    private static async Task<IResult> RotateKey(
        HttpContext context,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, apiKeyUsed) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);
        
        if (sub is null)
        {
            return Results.Unauthorized();
        }

        var participant = await participantRepository.GetBySubAsync(sub, cancellationToken);
        if (participant is null)
        {
            return Results.Unauthorized();
        }

        string? revokedPrefix = null;

        if (!string.IsNullOrWhiteSpace(apiKeyUsed))
        {
            var keyPrefix = ExtractKeyPrefix(apiKeyUsed);
            if (keyPrefix is not null)
            {
                var oldCredential = await apiKeyRepository.GetByKeyPrefixAsync(keyPrefix, cancellationToken);
                if (oldCredential is not null && oldCredential.ParticipantId == participant.Id)
                {
                    oldCredential.Revoke();
                    revokedPrefix = oldCredential.KeyPrefix;
                }
            }
        }

        var (newCredential, rawApiKey) = ApiKeyCredential.Create(participant.Id);
        await apiKeyRepository.AddAsync(newCredential, cancellationToken);
        await apiKeyRepository.SaveChangesAsync(cancellationToken);

        return Results.Ok(new RotateKeyResponse
        {
            ApiKey = rawApiKey,
            ApiKeyPrefix = newCredential.KeyPrefix,
            RevokedKeyPrefix = revokedPrefix ?? ""
        });
    }

    private static async Task<Participant?> ValidateApiKeyAndGetParticipant(
        string apiKey,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        CancellationToken cancellationToken)
    {
        var keyPrefix = ExtractKeyPrefix(apiKey);
        if (keyPrefix is null) return null;

        var credential = await apiKeyRepository.GetByKeyPrefixAsync(keyPrefix, cancellationToken);
        if (credential is null || !credential.VerifyKey(apiKey)) return null;

        var participant = await participantRepository.GetByIdAsync(credential.ParticipantId, cancellationToken);
        if (participant is null || !participant.IsActive) return null;

        return participant;
    }

    private static string? ExtractKeyPrefix(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        
        var parts = apiKey.Split('_');
        if (parts.Length >= 2 && parts[0] == "dlw")
        {
            return parts[1];
        }
        return null;
    }
}
