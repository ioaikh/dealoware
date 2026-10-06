namespace Dealoware.Domain.Admin;

/// <summary>
/// Seeded CoreOwner credential row. Password hash is null until the first
/// password is set through a consumed bootstrap link.
/// </summary>
public sealed class AdminCredential
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string? PasswordHash { get; private set; }

    public DateTimeOffset? PasswordSetAt { get; private set; }

    private AdminCredential()
    {
    }

    public static AdminCredential Create(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new AdminCredential
        {
            Id = Guid.NewGuid(),
            Email = email.Trim()
        };
    }

    public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

    public void SetPasswordHash(string passwordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
        PasswordSetAt = now;
    }
}
