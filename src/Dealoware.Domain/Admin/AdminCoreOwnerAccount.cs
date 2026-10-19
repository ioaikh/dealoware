namespace Dealoware.Domain.Admin;

/// <summary>
/// Single CoreOwner account row: password hash (set by bootstrap / tests),
/// encrypted TOTP secret, and enrollment state. The TOTP secret is never
/// stored in plaintext.
/// </summary>
public sealed class AdminCoreOwnerAccount
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    /// <summary>Password hash. Null until bootstrap (Step 2) or a test seeds it.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>AES-GCM ciphertext of the TOTP shared secret. Null until enrollment completes.</summary>
    public string? TotpSecretCipher { get; private set; }

    public DateTimeOffset? TotpEnrolledAt { get; private set; }

    /// <summary>True after recovery codes have been issued once. Codes are never re-shown.</summary>
    public bool RecoveryCodesIssued { get; private set; }

    /// <summary>Last TOTP time-step that succeeded, to reject same-window replay.</summary>
    public long? LastUsedTotpTimestep { get; private set; }

    /// <summary>
    /// Failed TOTP/recovery attempts in the current factor window. Durable account
    /// row (SC-6); never an in-process counter. Not reset by password login.
    /// </summary>
    public int FailedFactorAttempts { get; private set; }

    /// <summary>Start of the 15-minute factor-attempt window. Stored on this row.</summary>
    public DateTimeOffset? FactorAttemptWindowStartedAt { get; private set; }

    /// <summary>When set and in the future, step 2 is locked (30-minute lockout). Survives restart.</summary>
    public DateTimeOffset? FactorLockedUntil { get; private set; }

    public const int MaxFailedFactorAttempts = 5;

    public const int FactorAttemptWindowMinutes = 15;

    public const int FactorLockoutMinutes = 30;

    /// <summary>Encrypted pending secret held between enroll/start and enroll/confirm.</summary>
    public string? PendingTotpSecretCipher { get; private set; }

    /// <summary>Encrypted recovery-code list, held until the one-time S-A7 reveal.</summary>
    public string? RecoveryCodesRevealCipher { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private AdminCoreOwnerAccount() { }

    public static AdminCoreOwnerAccount Create(string email, DateTimeOffset? now = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new AdminCoreOwnerAccount
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            CreatedAt = now ?? DateTimeOffset.UtcNow
        };
    }

    public bool IsTotpEnrolled => TotpEnrolledAt is not null && !string.IsNullOrEmpty(TotpSecretCipher);

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public void SetPendingEnrollment(string pendingSecretCipher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pendingSecretCipher);
        PendingTotpSecretCipher = pendingSecretCipher;
    }

    public void MarkRecoveryCodesIssued()
    {
        RecoveryCodesIssued = true;
    }

    public void CompleteEnrollment(
        string totpSecretCipher,
        string? recoveryCodesRevealCipher = null,
        DateTimeOffset? now = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(totpSecretCipher);
        TotpSecretCipher = totpSecretCipher;
        TotpEnrolledAt = now ?? DateTimeOffset.UtcNow;
        RecoveryCodesIssued = true;
        PendingTotpSecretCipher = null;
        RecoveryCodesRevealCipher = recoveryCodesRevealCipher;
    }

    public string? TakeRecoveryCodesReveal()
    {
        var cipher = RecoveryCodesRevealCipher;
        RecoveryCodesRevealCipher = null;
        return cipher;
    }

    public void ClearRecoveryCodesReveal()
    {
        RecoveryCodesRevealCipher = null;
    }

    public void RecordTotpTimestep(long timestep)
    {
        LastUsedTotpTimestep = timestep;
    }

    public bool IsFactorLocked(DateTimeOffset? now = null)
    {
        var timestamp = now ?? DateTimeOffset.UtcNow;
        return FactorLockedUntil is { } until && timestamp < until;
    }

    public void ClearFactorFailures()
    {
        FailedFactorAttempts = 0;
        FactorAttemptWindowStartedAt = null;
        FactorLockedUntil = null;
    }

    public void ClearPendingEnrollment()
    {
        PendingTotpSecretCipher = null;
    }
}
