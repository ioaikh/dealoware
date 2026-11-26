using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

public enum AdminBootstrapLinkOutcome
{
    Issued,
    AlreadySetUp,
    OwnerNotConfigured
}

/// <param name="Outcome">What the issue call did.</param>
/// <param name="MailSent">
/// Completes when the link mail has left the queue: true when the sender
/// accepted it. Already false when no link was issued.
/// </param>
public sealed record AdminBootstrapLinkResult(AdminBootstrapLinkOutcome Outcome, Task<bool> MailSent);

/// <summary>
/// First-time setup (bootstrap) of the CoreOwner (Spec §8.2). Issues the
/// single-use link; <see cref="AdminBootstrapEndpoints"/> redeems it. The raw
/// token leaves this class only inside the mail and is never logged.
/// </summary>
public sealed class AdminBootstrapService
{
    /// <summary>
    /// Audit <c>IpHmac</c> value for a link issued by the one-shot command,
    /// where there is no client address to hash.
    /// </summary>
    public const string CommandOrigin = "command";

    private readonly IAdminBootstrapTokenRepository _tokens;
    private readonly IAdminCredentialStore _credentials;
    private readonly IAdminCoreOwnerAccountRepository _accounts;
    private readonly IAdminAuditRepository _audit;
    private readonly AdminMailSendQueue _mail;
    private readonly IAdminClock _clock;
    private readonly DealowareDbContext _db;
    private readonly string _ownerEmail;

    public AdminBootstrapService(
        IAdminBootstrapTokenRepository tokens,
        IAdminCredentialStore credentials,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminAuditRepository audit,
        AdminMailSendQueue mail,
        IAdminClock clock,
        IOptions<CoreOwnerOptions> coreOwner,
        DealowareDbContext db)
    {
        _tokens = tokens;
        _credentials = credentials;
        _accounts = accounts;
        _audit = audit;
        _mail = mail;
        _clock = clock;
        _db = db;
        _ownerEmail = AdminAuthService.NormalizeEmail(coreOwner.Value.Email);
    }

    /// <summary>
    /// Issues a bootstrap link for the configured CoreOwner: refuses when a
    /// password already exists, burns every earlier unused link, stores only
    /// the hash of a new 24-hour token, and queues the mail that carries it.
    /// </summary>
    public async Task<AdminBootstrapLinkResult> IssueLinkAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_ownerEmail))
        {
            return NotIssued(AdminBootstrapLinkOutcome.OwnerNotConfigured);
        }

        if (await OwnerHasPasswordAsync(cancellationToken))
        {
            return NotIssued(AdminBootstrapLinkOutcome.AlreadySetUp);
        }

        var now = _clock.UtcNow;
        var raw = AdminAuthService.CreateOpaqueToken();

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _tokens.InvalidateUnusedAsync(now, cancellationToken);
        await _tokens.AddAsync(
            AdminBootstrapToken.Create(_ownerEmail, AdminBootstrapToken.HashRaw(raw), now),
            cancellationToken);
        await _audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent(
                AdminAuthEvents.BootstrapLinkIssued,
                _ownerEmail,
                CommandOrigin,
                timestamp: now),
            cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        // Queue only after the hash is committed, so no mail carries a link that was never stored.
        return new AdminBootstrapLinkResult(
            AdminBootstrapLinkOutcome.Issued,
            _mail.EnqueueBootstrap(_ownerEmail, raw, traceId: null));
    }

    /// <summary>
    /// True when the configured owner already has a password in either store:
    /// the credential row password reset reads, or the account row sign-in reads.
    /// </summary>
    public async Task<bool> OwnerHasPasswordAsync(CancellationToken cancellationToken)
    {
        if (await _credentials.GetByEmailAsync(_ownerEmail, cancellationToken) is not null)
        {
            return true;
        }

        var account = await _accounts.GetByEmailAsync(_ownerEmail, cancellationToken);
        return account is not null && !string.IsNullOrEmpty(account.PasswordHash);
    }

    private static AdminBootstrapLinkResult NotIssued(AdminBootstrapLinkOutcome outcome)
        => new(outcome, Task.FromResult(false));
}
