using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminBootstrapService : IAdminBootstrapService
{
    public const string BootstrapPagePath = "/admin/bootstrap";
    public const string BootstrapMailSubject = "Dealoware admin";

    private readonly DealowareDbContext _db;
    private readonly IAdminMailSender _mail;
    private readonly ITurnstileVerifier _turnstile;
    private readonly IAdminClock _clock;
    private readonly IAdminAuditRepository _audit;
    private readonly CoreOwnerOptions _coreOwner;
    private readonly AdminMailOptions _mailOptions;

    public AdminBootstrapService(
        DealowareDbContext db,
        IAdminMailSender mail,
        ITurnstileVerifier turnstile,
        IAdminClock clock,
        IAdminAuditRepository audit,
        IOptions<CoreOwnerOptions> coreOwner,
        IOptions<AdminMailOptions> mailOptions)
    {
        _db = db;
        _mail = mail;
        _turnstile = turnstile;
        _clock = clock;
        _audit = audit;
        _coreOwner = coreOwner.Value;
        _mailOptions = mailOptions.Value;
    }

    public bool HasPasswordSet()
        => _db.AdminCredentials.AsNoTracking().Any(c => c.PasswordHash != null);

    public async Task<AdminBootstrapIssueResult> IssueLinkAsync(
        string? ipHmac,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var email = _coreOwner.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return new AdminBootstrapIssueResult(false, AdminAuthMessages.InvalidOrExpiredLink);
        }

        var credential = await GetOrCreateCredentialAsync(email, cancellationToken).ConfigureAwait(false);
        if (credential.HasPassword)
        {
            return new AdminBootstrapIssueResult(false, AdminAuthMessages.InvalidOrExpiredLink);
        }

        var raw = AdminTokenHasher.CreateRawToken();
        var hash = AdminTokenHasher.Hash(raw);
        _db.AdminBootstrapTokens.Add(AdminBootstrapToken.Create(hash, now));

        var origin = string.IsNullOrWhiteSpace(_mailOptions.PublicOrigin)
            ? "https://admin.core.dealoware.com"
            : _mailOptions.PublicOrigin.TrimEnd('/');
        var link = $"{origin}{BootstrapPagePath}?token={Uri.EscapeDataString(raw)}";
        await _mail.SendAsync(
            new AdminMailMessage(email, BootstrapMailSubject, link),
            cancellationToken).ConfigureAwait(false);

        await _audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent(
                AdminAuthMessages.BootstrapLinkSentAction,
                email,
                ipHmac ?? string.Empty),
            cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new AdminBootstrapIssueResult(true, null);
    }

    public async Task<AdminBootstrapInspectResult> InspectAsync(
        string? rawToken,
        CancellationToken cancellationToken = default)
    {
        var token = await FindUsableAsync(rawToken, cancellationToken).ConfigureAwait(false);
        return new AdminBootstrapInspectResult(token is not null);
    }

    public async Task<AdminBootstrapSetPasswordResult> SetPasswordAsync(
        string? rawToken,
        string? password,
        string? turnstileToken,
        string? remoteIp,
        string? ipHmac,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var email = _coreOwner.Email;
        var hmac = ipHmac ?? string.Empty;

        var turnstile = await _turnstile.VerifyAsync(turnstileToken, remoteIp, cancellationToken)
            .ConfigureAwait(false);
        if (!turnstile.Success)
        {
            await WriteAuditAsync(
                AdminAuthMessages.BootstrapPasswordSetAction,
                email,
                hmac,
                AdminAuthMessages.CaptchaFailedReason,
                cancellationToken).ConfigureAwait(false);
            return new AdminBootstrapSetPasswordResult.TurnstileFailed();
        }

        var credential = await GetOrCreateCredentialAsync(email, cancellationToken).ConfigureAwait(false);
        var rule = AdminPasswordRules.Validate(
            password,
            credential.PasswordHash,
            AdminPasswordHasher.Verify);
        if (!rule.Succeeded)
        {
            return new AdminBootstrapSetPasswordResult.PasswordRejected(rule.Error!);
        }

        if (credential.HasPassword)
        {
            return new AdminBootstrapSetPasswordResult.AlreadySet();
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new AdminBootstrapSetPasswordResult.InvalidLink();
        }

        var tokenHash = AdminTokenHasher.Hash(rawToken);
        var existing = await _db.AdminBootstrapTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null || !existing.IsUsable(now))
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new AdminBootstrapSetPasswordResult.InvalidLink();
        }

        var consumed = await _db.AdminBootstrapTokens
            .Where(t => t.TokenHash == tokenHash && t.ConsumedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.ConsumedAt, now),
                cancellationToken)
            .ConfigureAwait(false);
        if (consumed != 1)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new AdminBootstrapSetPasswordResult.InvalidLink();
        }

        credential.SetPasswordHash(AdminPasswordHasher.Hash(password!), now);
        await _audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent(
                AdminAuthMessages.BootstrapPasswordSetAction,
                email,
                hmac),
            cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new AdminBootstrapSetPasswordResult.Succeeded();
    }

    private async Task<AdminBootstrapToken?> FindUsableAsync(string? rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = AdminTokenHasher.Hash(rawToken);
        var now = _clock.UtcNow;
        var token = await _db.AdminBootstrapTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);
        return token is not null && token.IsUsable(now) ? token : null;
    }

    private async Task<AdminCredential> GetOrCreateCredentialAsync(string email, CancellationToken cancellationToken)
    {
        var existing = await _db.AdminCredentials
            .SingleOrDefaultAsync(c => c.Email == email, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var created = AdminCredential.Create(email);
        _db.AdminCredentials.Add(created);
        return created;
    }

    private async Task WriteAuditAsync(
        string action,
        string email,
        string ipHmac,
        string? reason,
        CancellationToken cancellationToken)
    {
        await _audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent(action, email, ipHmac, reason),
            cancellationToken).ConfigureAwait(false);
        await _audit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
