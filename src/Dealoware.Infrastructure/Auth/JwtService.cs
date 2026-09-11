using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dealoware.Domain.Participants;
using Microsoft.IdentityModel.Tokens;

namespace Dealoware.Infrastructure.Auth;

/// <summary>
/// JWT token service for issuing and validating tokens.
/// Uses OIDC-shaped claims for interoperability.
/// </summary>
public class JwtService
{
    private readonly JwtSettings _settings;
    private readonly IRevokedTokenRepository _revokedTokenRepository;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public const string Issuer = "dealoware";
    public const string Audience = "dealoware-api";

    public JwtService(JwtSettings settings, IRevokedTokenRepository revokedTokenRepository)
    {
        _settings = settings;
        _revokedTokenRepository = revokedTokenRepository;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    /// <summary>
    /// Issues a JWT token for a participant.
    /// Returns the token string and its unique JTI for revocation purposes.
    /// </summary>
    public (string Token, string Jti, DateTimeOffset ExpiresAt) IssueToken(Participant participant)
    {
        var jti = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_settings.TokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, participant.Sub),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Iss, Issuer),
            new Claim(JwtRegisteredClaimNames.Aud, Audience)
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _signingCredentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, jti, expiresAt);
    }

    /// <summary>
    /// Validates a JWT token and returns the claims principal if valid.
    /// Checks revocation list as part of validation.
    /// </summary>
    public async Task<(bool IsValid, ClaimsPrincipal? Principal, string? Error)> ValidateTokenAsync(
        string token, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParameters, out var validatedToken);

            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti))
            {
                var isRevoked = await _revokedTokenRepository.IsRevokedAsync(jti, cancellationToken);
                if (isRevoked)
                {
                    return (false, null, "Token has been revoked");
                }
            }

            return (true, principal, null);
        }
        catch (SecurityTokenExpiredException)
        {
            return (false, null, "Token has expired");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return (false, null, "Invalid token signature");
        }
        catch (SecurityTokenException ex)
        {
            return (false, null, $"Invalid token: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return (false, null, $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return (false, null, "Invalid token");
        }
    }

    /// <summary>
    /// Extracts the sub claim from a claims principal.
    /// </summary>
    public static string? GetSubFromPrincipal(ClaimsPrincipal principal)
    {
        return principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

/// <summary>
/// JWT configuration settings.
/// Values should come from environment variables - never commit real secrets.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Signing key for JWT tokens. Must be at least 32 characters.
    /// Source: DEALOWARE_JWT_SIGNING_KEY environment variable.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Token lifetime in minutes. Default: 60 minutes.
    /// Source: DEALOWARE_JWT_LIFETIME_MINUTES environment variable.
    /// </summary>
    public int TokenLifetimeMinutes { get; set; } = 60;
}
