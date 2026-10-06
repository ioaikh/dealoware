namespace Dealoware.Domain.Admin;

/// <summary>
/// Server-side Cloudflare Turnstile verification. Fail closed.
/// </summary>
public interface ITurnstileVerifier
{
    Task<TurnstileVerifyResult> VerifyAsync(
        string? token,
        string? remoteIp,
        CancellationToken cancellationToken = default);
}

public sealed record TurnstileVerifyResult(bool Success);
