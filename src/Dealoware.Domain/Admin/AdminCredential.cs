namespace Dealoware.Domain.Admin;

/// <summary>
/// Stored CoreOwner password hash. First password set is owned by Step 2
/// (bootstrap). This table lets Step 4 persist a successful reset.
/// </summary>
public sealed class AdminCredential
{
    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset PasswordUpdatedAt { get; private set; }

    private AdminCredential() { }

    public static AdminCredential Create(string email, string passwordHash, DateTimeOffset now)
    {
        return new AdminCredential
        {
            Email = email,
            PasswordHash = passwordHash,
            PasswordUpdatedAt = now
        };
    }

    public void ReplacePassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        PasswordUpdatedAt = now;
    }
}
