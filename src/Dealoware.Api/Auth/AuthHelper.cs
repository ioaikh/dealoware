using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Auth;
using Microsoft.Net.Http.Headers;

namespace Dealoware.Api.Auth;

/// <summary>
/// Helper for extracting authenticated principal from Authorization header.
/// Supports both JWT Bearer tokens and API keys.
/// 
/// Authorization header formats:
/// - Bearer {jwt_token}     - JWT Bearer token
/// - ApiKey {api_key}       - API key authentication
/// - Bearer {api_key}       - API key as bearer (also supported)
/// 
/// IMPORTANT: Tokens in query strings or request bodies are NOT supported
/// and will be rejected. Authorization header is the only valid transport.
/// </summary>
public static class AuthHelper
{
    private const string BearerScheme = "Bearer";
    private const string ApiKeyScheme = "ApiKey";

    /// <summary>
    /// Extracts and validates the authenticated principal's sub claim from Authorization header.
    /// Returns (sub, apiKeyUsedIfAny) tuple.
    /// </summary>
    public static async Task<(string? Sub, string? ApiKeyUsed)> GetAuthenticatedSub(
        HttpContext context,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken = default)
    {
        if (!context.Request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader))
        {
            return (null, null);
        }

        var authValue = authHeader.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authValue))
        {
            return (null, null);
        }

        if (authValue.StartsWith(BearerScheme + " ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authValue[(BearerScheme.Length + 1)..].Trim();
            return await ValidateCredential(token, participantRepository, apiKeyRepository, jwtService, cancellationToken);
        }

        if (authValue.StartsWith(ApiKeyScheme + " ", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = authValue[(ApiKeyScheme.Length + 1)..].Trim();
            var sub = await ValidateApiKey(apiKey, participantRepository, apiKeyRepository, cancellationToken);
            return (sub, sub is not null ? apiKey : null);
        }

        return (null, null);
    }

    private static async Task<(string? Sub, string? ApiKeyUsed)> ValidateCredential(
        string credential,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        if (credential.StartsWith("dlw_"))
        {
            var sub = await ValidateApiKey(credential, participantRepository, apiKeyRepository, cancellationToken);
            return (sub, sub is not null ? credential : null);
        }

        var (isValid, principal, _) = await jwtService.ValidateTokenAsync(credential, cancellationToken);
        if (!isValid || principal is null)
        {
            return (null, null);
        }

        var sub2 = JwtService.GetSubFromPrincipal(principal);
        if (string.IsNullOrEmpty(sub2))
        {
            return (null, null);
        }

        var participant = await participantRepository.GetBySubAsync(sub2, cancellationToken);
        if (participant is null || !participant.IsActive)
        {
            return (null, null);
        }

        return (sub2, null);
    }

    private static async Task<string?> ValidateApiKey(
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

        return participant.Sub;
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
