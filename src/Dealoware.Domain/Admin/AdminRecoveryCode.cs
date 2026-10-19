namespace Dealoware.Domain.Admin;

/// <summary>
/// Single-use recovery code stored as a hash only. Plaintext is shown once at enrollment.
/// </summary>
public sealed class AdminRecoveryCode
{
    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private AdminRecoveryCode() { }

    public static AdminRecoveryCode Create(Guid accountId, string codeHash, DateTimeOffset? now = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        return new AdminRecoveryCode
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            CodeHash = codeHash,
            CreatedAt = now ?? DateTimeOffset.UtcNow
        };
    }

    public bool IsUsed => UsedAt is not null;

    /// <summary>
    /// Marks the code used. Returns false if it was already spent (race-safe caller still
    /// must persist with a filter on UsedAt IS NULL).
    /// </summary>
    public bool TryMarkUsed(DateTimeOffset? now = null)
    {
        if (UsedAt is not null)
            return false;

        UsedAt = now ?? DateTimeOffset.UtcNow;
        return true;
    }
}
