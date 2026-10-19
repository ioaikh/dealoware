using System.Security.Cryptography;
using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Step 3 TOTP / recovery-code verifier used by password reset completion.
/// </summary>
public sealed class AdminSecondFactorVerifier : IAdminSecondFactorVerifier
{
    private readonly IAdminCoreOwnerAccountRepository _accounts;
    private readonly TotpSecretProtector _protector;
    private readonly AdminRecoveryCodeHasher _recoveryHasher;
    private readonly IAdminClock _clock;

    public AdminSecondFactorVerifier(
        IAdminCoreOwnerAccountRepository accounts,
        TotpSecretProtector protector,
        AdminRecoveryCodeHasher recoveryHasher,
        IAdminClock clock)
    {
        _accounts = accounts;
        _protector = protector;
        _recoveryHasher = recoveryHasher;
        _clock = clock;
    }

    public async Task<AdminSecondFactorVerifyResult> VerifyAsync(
        string email,
        string? totpCode,
        string? recoveryCode,
        CancellationToken cancellationToken = default)
    {
        var totp = string.IsNullOrWhiteSpace(totpCode) ? null : totpCode.Trim();
        var recovery = string.IsNullOrWhiteSpace(recoveryCode) ? null : recoveryCode.Trim();
        if ((totp is null) == (recovery is null))
            return AdminSecondFactorVerifyResult.Failed;

        var account = await _accounts.GetByEmailAsync(email, cancellationToken);
        if (account is null || !account.IsTotpEnrolled || string.IsNullOrEmpty(account.TotpSecretCipher))
            return AdminSecondFactorVerifyResult.Failed;

        var now = _clock.UtcNow;
        if (totp is not null)
        {
            byte[] secret;
            try
            {
                secret = _protector.Decrypt(account.TotpSecretCipher);
            }
            catch (CryptographicException)
            {
                return AdminSecondFactorVerifyResult.Failed;
            }

            try
            {
                if (!Rfc6238Totp.TryVerify(
                        secret,
                        totp,
                        now,
                        account.LastUsedTotpTimestep,
                        out var step))
                {
                    return AdminSecondFactorVerifyResult.Failed;
                }

                if (!await _accounts.TryRecordTotpTimestepAsync(account.Id, step, cancellationToken))
                    return AdminSecondFactorVerifyResult.Failed;

                return AdminSecondFactorVerifyResult.TotpSucceeded;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secret);
            }
        }

        Rfc6238Totp.RecordEvaluation();
        var presentedHash = _recoveryHasher.Hash(recovery!);
        var codes = await _accounts.GetRecoveryCodesAsync(account.Id, cancellationToken);
        string? matchedHash = null;
        foreach (var stored in codes)
        {
            if (!AdminRecoveryCodes.FixedTimeEquals(stored.CodeHash, presentedHash))
                continue;
            if (!stored.IsUsed)
                matchedHash = stored.CodeHash;
        }

        if (matchedHash is null
            || !await _accounts.TryRedeemRecoveryCodeAsync(account.Id, matchedHash, now, cancellationToken))
        {
            return AdminSecondFactorVerifyResult.Failed;
        }

        return AdminSecondFactorVerifyResult.RecoverySucceeded;
    }
}
