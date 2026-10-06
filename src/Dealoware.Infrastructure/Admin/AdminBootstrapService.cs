using System.Diagnostics;
using Dealoware.Application.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminBootstrapService : IAdminBootstrapService
{
    /// <summary>
    /// N4: dispatch failure logs exception type and trace ID only.
    /// </summary>
    public const string MailDispatchFailedTemplate =
        "Bootstrap mail dispatch failed. ExceptionType={ExceptionType} TraceId={TraceId}";

    private readonly DealowareDbContext _db;
    private readonly AdminMailDispatcher _mail;
    private readonly ITurnstileVerifier _turnstile;
    private readonly IAdminClock _clock;
    private readonly IAdminAuditRepository _audit;
    private readonly ILogger<AdminBootstrapService> _logger;
    private readonly CoreOwnerOptions _coreOwner;

    public AdminBootstrapService(
        DealowareDbContext db,
        AdminMailDispatcher mail,
        ITurnstileVerifier turnstile,
        IAdminClock clock,
        IAdminAuditRepository audit,
        ILogger<AdminBootstrapService> logger,
        IOptions<CoreOwnerOptions> coreOwner)
    {
        _db = db;
        _mail = mail;
        _turnstile = turnstile;
        _clock = clock;
        _audit = audit;
        _logger = logger;
        _coreOwner = coreOwner.Value;
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

        try
        {
            await _mail.SendBootstrapLinkAsync(email, raw, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            foreach (var entry in _db.ChangeTracker.Entries<AdminBootstrapToken>().ToList())
            {
                entry.State = EntityState.Detached;
            }

            if (_db.Entry(credential).State == EntityState.Added)
            {
                _db.Entry(credential).State = EntityState.Detached;
            }

            // N4: exception type and trace ID only. Never email, link, fragment, or token.
            // Never pass the exception object (its message may carry those) and never write to the console.
            _logger.LogError(
                MailDispatchFailedTemplate,
                ex.GetType().FullName,
                Activity.Current?.TraceId.ToString() ?? string.Empty);
            return new AdminBootstrapIssueResult(false, AdminAuthMessages.InvalidOrExpiredLink);
        }

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
        var hmac = string.IsNullOrWhiteSpace(ipHmac) ? string.Empty : ipHmac;

        if (await IsThrottledAsync(hmac, now, cancellationToken).ConfigureAwait(false))
        {
            return new AdminBootstrapSetPasswordResult.Throttled();
        }

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
            return await FailLinkAsync(hmac, now, cancellationToken).ConfigureAwait(false);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return await FailLinkAsync(hmac, now, cancellationToken).ConfigureAwait(false);
        }

        var tokenHash = AdminTokenHasher.Hash(rawToken);
        var existing = await _db.AdminBootstrapTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null || !existing.IsUsable(now))
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return await FailLinkAsync(hmac, now, cancellationToken).ConfigureAwait(false);
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
            return await FailLinkAsync(hmac, now, cancellationToken).ConfigureAwait(false);
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

    private async Task<AdminBootstrapSetPasswordResult> FailLinkAsync(
        string ipKey,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(ipKey) ? "unknown" : ipKey;
        if (await RecordFailureAsync(key, now, cancellationToken).ConfigureAwait(false)
            == BootstrapThrottleDecision.Throttled)
        {
            return new AdminBootstrapSetPasswordResult.Throttled();
        }

        return new AdminBootstrapSetPasswordResult.InvalidLink();
    }

    private async Task<bool> IsThrottledAsync(
        string ipKey,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(ipKey) ? "unknown" : ipKey;
        var row = await _db.AdminBootstrapIpThrottles
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.IpKey == key, cancellationToken)
            .ConfigureAwait(false);
        return row is not null && row.IsLocked(now);
    }

    /// <summary>
    /// SC-6: atomic check-and-increment in the database (compare-and-swap on AttemptCount).
    /// Never increments in process memory.
    /// </summary>
    private async Task<BootstrapThrottleDecision> RecordFailureAsync(
        string ipKey,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var row = await _db.AdminBootstrapIpThrottles
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.IpKey == ipKey, cancellationToken)
                .ConfigureAwait(false);

            if (row is null)
            {
                _db.AdminBootstrapIpThrottles.Add(AdminBootstrapIpThrottle.StartWindow(ipKey, now));
                try
                {
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return BootstrapThrottleDecision.Recorded;
                }
                catch (DbUpdateException)
                {
                    foreach (var entry in _db.ChangeTracker.Entries<AdminBootstrapIpThrottle>().ToList())
                    {
                        entry.State = EntityState.Detached;
                    }

                    continue;
                }
            }

            if (row.IsLocked(now))
            {
                return BootstrapThrottleDecision.Throttled;
            }

            int next;
            DateTimeOffset windowStart;
            DateTimeOffset? lockedUntil;
            if (row.WindowExpired(now))
            {
                next = 1;
                windowStart = now;
                lockedUntil = null;
            }
            else
            {
                next = row.AttemptCount + 1;
                windowStart = row.WindowStartedAt;
                lockedUntil = next >= AdminBootstrapIpThrottle.AttemptLimit
                    ? now.AddMinutes(AdminBootstrapIpThrottle.LockMinutes)
                    : row.LockedUntil;
            }

            var updated = await _db.AdminBootstrapIpThrottles
                .Where(t => t.IpKey == ipKey && t.AttemptCount == row.AttemptCount)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(t => t.AttemptCount, next)
                        .SetProperty(t => t.WindowStartedAt, windowStart)
                        .SetProperty(t => t.LockedUntil, lockedUntil),
                    cancellationToken)
                .ConfigureAwait(false);
            if (updated != 1)
            {
                continue;
            }

            return next >= AdminBootstrapIpThrottle.AttemptLimit
                ? BootstrapThrottleDecision.Throttled
                : BootstrapThrottleDecision.Recorded;
        }

        return BootstrapThrottleDecision.Throttled;
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

    private enum BootstrapThrottleDecision
    {
        Recorded,
        Throttled
    }
}
