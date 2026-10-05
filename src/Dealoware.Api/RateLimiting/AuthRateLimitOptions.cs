namespace Dealoware.Api.RateLimiting;

/// <summary>
/// Fixed-window rate limit settings for auth bootstrap endpoints.
/// Bound from configuration section "RateLimiting".
/// </summary>
public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Policy name applied to POST /auth/register.</summary>
    public const string RegisterPolicy = "auth-register";

    /// <summary>Policy name applied to POST /auth/token (login equivalent).</summary>
    public const string TokenPolicy = "auth-token";

    public EndpointLimit AuthRegister { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public EndpointLimit AuthToken { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60 };

    public sealed class EndpointLimit
    {
        /// <summary>Max requests per partition (client IP) within the window.</summary>
        public int PermitLimit { get; set; } = 10;

        /// <summary>Fixed window length in seconds.</summary>
        public int WindowSeconds { get; set; } = 60;
    }
}
