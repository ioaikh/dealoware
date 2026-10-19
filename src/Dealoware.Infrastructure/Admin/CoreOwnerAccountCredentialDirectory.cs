using Dealoware.Domain.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Production credential adapter over the Step 3 CoreOwner account.
/// No in-memory stand-in and no hard-coded owner email.
/// </summary>
public sealed class CoreOwnerAccountCredentialDirectory : IAdminCredentialDirectory
{
    private readonly IAdminCoreOwnerAccountRepository _accounts;
    private readonly IOptions<CoreOwnerOptions> _options;

    public CoreOwnerAccountCredentialDirectory(
        IAdminCoreOwnerAccountRepository accounts,
        IOptions<CoreOwnerOptions> options)
    {
        _accounts = accounts;
        _options = options;
        OwnerEmail = AdminAuthService.NormalizeEmail(options.Value.Email);
    }

    public string OwnerEmail { get; }

    public bool IsOwnerEmail(string? email) =>
        !string.IsNullOrWhiteSpace(OwnerEmail)
        && string.Equals(AdminAuthService.NormalizeEmail(email), OwnerEmail, StringComparison.Ordinal);

    public async Task<bool> VerifyPasswordAsync(string email, string password, CancellationToken ct)
    {
        if (!IsOwnerEmail(email) || string.IsNullOrEmpty(password))
        {
            AdminPasswordHasher.DummyVerify(password ?? string.Empty);
            return false;
        }

        var account = await _accounts.GetByEmailAsync(OwnerEmail, ct).ConfigureAwait(false);
        if (account is null || string.IsNullOrEmpty(account.PasswordHash))
        {
            AdminPasswordHasher.DummyVerify(password);
            return false;
        }

        return AdminPasswordHasher.Verify(password, account.PasswordHash);
    }

    public Task<bool> VerifyTotpAsync(string email, string code, CancellationToken ct)
        => Task.FromResult(false);

    public Task<bool> VerifyRecoveryCodeAsync(string email, string code, CancellationToken ct)
        => Task.FromResult(false);

    public Task RunDummyPasswordCheckAsync(CancellationToken ct)
    {
        AdminPasswordHasher.DummyVerify(Guid.NewGuid().ToString("N"));
        return Task.CompletedTask;
    }

    public async Task SetPasswordAsync(string email, string password, CancellationToken ct)
    {
        if (!IsOwnerEmail(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var account = await _accounts.GetByEmailAsync(OwnerEmail, ct).ConfigureAwait(false);
        if (account is null)
        {
            return;
        }

        account.SetPasswordHash(AdminPasswordHasher.Hash(password));
        await _accounts.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
