using Dealoware.Domain.Admin;

namespace Dealoware.Api.Tests;

/// <summary>CI fake. Never opens a network connection.</summary>
public sealed class FakeTurnstileVerifier : ITurnstileVerifier
{
    public const string ValidToken = "valid-test-token";

    public bool Unavailable { get; set; }

    public Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken ct)
    {
        if (Unavailable)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(string.Equals(token, ValidToken, StringComparison.Ordinal));
    }
}
