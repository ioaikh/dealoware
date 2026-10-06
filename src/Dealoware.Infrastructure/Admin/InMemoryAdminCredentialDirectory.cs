using System.Security.Cryptography;
using System.Text;
using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Process-local CoreOwner credentials. Passwords are hashed at runtime; none are committed.
/// </summary>
public sealed class InMemoryAdminCredentialDirectory : IAdminCredentialDirectory
{
    private readonly object _gate = new();
    private readonly string _dummyHash;
    private string _passwordHash;
    private readonly HashSet<string> _recoveryHashes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _totpCodes = new(StringComparer.Ordinal);

    public InMemoryAdminCredentialDirectory(string ownerEmail)
    {
        OwnerEmail = ownerEmail;
        _dummyHash = HashSecret(Convert.ToHexString(RandomNumberGenerator.GetBytes(24)));
        _passwordHash = _dummyHash;
    }

    public string OwnerEmail { get; }

    public bool IsOwnerEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && string.Equals(email.Trim(), OwnerEmail, StringComparison.OrdinalIgnoreCase);

    public Task<bool> VerifyPasswordAsync(string email, string password, CancellationToken ct)
    {
        if (!IsOwnerEmail(email) || string.IsNullOrEmpty(password))
        {
            VerifySecret(_dummyHash, password ?? string.Empty);
            return Task.FromResult(false);
        }

        string hash;
        lock (_gate)
        {
            hash = _passwordHash;
        }

        return Task.FromResult(VerifySecret(hash, password));
    }

    public Task<bool> VerifyTotpAsync(string email, string code, CancellationToken ct)
    {
        if (!IsOwnerEmail(email) || string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult(false);
        }

        lock (_gate)
        {
            return Task.FromResult(_totpCodes.Contains(code.Trim()));
        }
    }

    public Task<bool> VerifyRecoveryCodeAsync(string email, string code, CancellationToken ct)
    {
        if (!IsOwnerEmail(email) || string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult(false);
        }

        lock (_gate)
        {
            return Task.FromResult(_recoveryHashes.Remove(HashRecovery(code)));
        }
    }

    public Task RunDummyPasswordCheckAsync(CancellationToken ct)
    {
        VerifySecret(_dummyHash, Convert.ToHexString(RandomNumberGenerator.GetBytes(8)));
        return Task.CompletedTask;
    }

    public Task SetPasswordAsync(string email, string password, CancellationToken ct)
    {
        if (!IsOwnerEmail(email) || string.IsNullOrWhiteSpace(password))
        {
            return Task.CompletedTask;
        }

        lock (_gate)
        {
            _passwordHash = HashSecret(password);
        }

        return Task.CompletedTask;
    }

    public void SeedTotpCode(string code)
    {
        lock (_gate)
        {
            _totpCodes.Add(code);
        }
    }

    public void SeedRecoveryCode(string code)
    {
        lock (_gate)
        {
            _recoveryHashes.Add(HashRecovery(code));
        }
    }

    private static string HashRecovery(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim()));
        return Convert.ToHexString(bytes);
    }

    private static string HashSecret(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(secret, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    private static bool VerifySecret(string stored, string secret)
    {
        var parts = stored.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(secret, salt, 100_000, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
