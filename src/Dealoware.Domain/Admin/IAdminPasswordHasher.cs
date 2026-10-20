namespace Dealoware.Domain.Admin;

/// <summary>
/// Password-rules note v2.3 rule 6: PBKDF2-HMAC-SHA512 ≥220,000 iterations,
/// per-user salt, never logged. Argon2id is the preferred floor; this slice
/// uses the allowed PBKDF2 path so Core stays package-neutral.
/// </summary>
public interface IAdminPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
