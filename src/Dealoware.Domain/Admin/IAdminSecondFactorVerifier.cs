namespace Dealoware.Domain.Admin;

/// <summary>
/// Verifies TOTP or an unused recovery code for CoreOwner.
/// Step 3 owns the real implementation. This PR registers a fail-closed
/// stub and tests replace it with <c>FakeAdminSecondFactorVerifier</c>.
/// </summary>
public interface IAdminSecondFactorVerifier
{
    /// <summary>
    /// Exactly one of <paramref name="totpCode"/> or <paramref name="recoveryCode"/>
    /// should be provided. Missing or both is a failed verification.
    /// </summary>
    Task<AdminSecondFactorVerifyResult> VerifyAsync(
        string email,
        string? totpCode,
        string? recoveryCode,
        CancellationToken cancellationToken = default);
}

/// <param name="Succeeded">True when the presented factor is valid and unused.</param>
/// <param name="UsedRecoveryCode">True only when a recovery code was consumed.</param>
public readonly record struct AdminSecondFactorVerifyResult(
    bool Succeeded,
    bool UsedRecoveryCode)
{
    public static AdminSecondFactorVerifyResult Failed { get; } = new(false, false);

    public static AdminSecondFactorVerifyResult TotpSucceeded { get; } = new(true, false);

    public static AdminSecondFactorVerifyResult RecoverySucceeded { get; } = new(true, true);
}
