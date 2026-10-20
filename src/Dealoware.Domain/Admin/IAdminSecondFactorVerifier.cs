namespace Dealoware.Domain.Admin;

/// <summary>
/// Verifies TOTP or an unused recovery code for CoreOwner.
/// Step 3 registers <c>AdminSecondFactorVerifier</c>. Reset confirm uses this
/// verifier (account-wide factor lock + timestep / recovery redemption).
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

    /// <summary>
    /// Same match as <see cref="VerifyAsync"/> but does not record a TOTP
    /// timestep or redeem a recovery code. Reset confirm commits those only
    /// after the password update succeeds.
    /// </summary>
    Task<AdminSecondFactorVerifyResult> EvaluateWithoutCommitAsync(
        string email,
        string? totpCode,
        string? recoveryCode,
        CancellationToken cancellationToken = default);
}

/// <param name="Succeeded">True when the presented factor is valid and unused.</param>
/// <param name="UsedRecoveryCode">True only when a recovery code was consumed.</param>
public readonly record struct AdminSecondFactorVerifyResult(
    bool Succeeded,
    bool UsedRecoveryCode,
    long? TotpTimestep = null,
    string? RecoveryCodeHash = null)
{
    public static AdminSecondFactorVerifyResult Failed { get; } = new(false, false);

    public static AdminSecondFactorVerifyResult TotpSucceeded { get; } = new(true, false);

    public static AdminSecondFactorVerifyResult RecoverySucceeded { get; } = new(true, true);

    public static AdminSecondFactorVerifyResult TotpAt(long timestep) => new(true, false, timestep);

    public static AdminSecondFactorVerifyResult RecoveryHash(string codeHash)
        => new(true, true, RecoveryCodeHash: codeHash);
}
