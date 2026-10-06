using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Fail-closed stub until Step 3 registers the real TOTP / recovery-code verifier.
/// Production reset completion cannot succeed without that implementation.
/// </summary>
public sealed class PendingStep3SecondFactorVerifier : IAdminSecondFactorVerifier
{
    public Task<AdminSecondFactorVerifyResult> VerifyAsync(
        string email,
        string? totpCode,
        string? recoveryCode,
        CancellationToken cancellationToken = default)
        => Task.FromResult(AdminSecondFactorVerifyResult.Failed);
}
