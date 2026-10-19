namespace Dealoware.Domain.Admin;

/// <summary>
/// Cloudflare Turnstile verification port. Tests bind a fake; production uses HTTP.
/// </summary>
public interface ITurnstileVerifier
{
    Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken ct);
}
