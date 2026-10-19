namespace Dealoware.Domain.Admin;

/// <summary>
/// CoreOwner credential checks. Implementations must not invent password values in source.
/// </summary>
public interface IAdminCredentialDirectory
{
    string OwnerEmail { get; }

    bool IsOwnerEmail(string? email);

    Task<bool> VerifyPasswordAsync(string email, string password, CancellationToken ct);

    Task<bool> VerifyTotpAsync(string email, string code, CancellationToken ct);

    Task<bool> VerifyRecoveryCodeAsync(string email, string code, CancellationToken ct);

    /// <summary>
    /// Always runs a password-hash compare so unknown-email timing matches a wrong password.
    /// </summary>
    Task RunDummyPasswordCheckAsync(CancellationToken ct);

    Task SetPasswordAsync(string email, string password, CancellationToken ct);
}
