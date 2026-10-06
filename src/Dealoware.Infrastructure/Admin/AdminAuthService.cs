using System.Security.Cryptography;
using System.Text;
using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminAuthService
{
    public const int AccountFailureLimit = 5;
    public const int IpFailureLimit = 20;
    public const int PendingAttemptLimit = 5;
    public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan ResetLifetime = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan BootstrapLifetime = TimeSpan.FromHours(24);
    public const int LinkTokenByteCount = 32;

    private readonly ITurnstileVerifier _turnstile;
    private readonly IAdminLockoutStore _lockouts;
    private readonly IAdminAuthTokenStore _tokens;
    private readonly IAdminCredentialDirectory _credentials;
    private readonly IAdminSessionRepository _sessions;
    private readonly IAdminAuditRepository _audit;
    private readonly IAdminMailSender _mailer;
    private readonly IIpHasher _ipHasher;
    private readonly IAdminClock _clock;

    public AdminAuthService(
        ITurnstileVerifier turnstile,
        IAdminLockoutStore lockouts,
        IAdminAuthTokenStore tokens,
        IAdminCredentialDirectory credentials,
        IAdminSessionRepository sessions,
        IAdminAuditRepository audit,
        IAdminMailSender mailer,
        IIpHasher ipHasher,
        IAdminClock clock)
    {
        _turnstile = turnstile;
        _lockouts = lockouts;
        _tokens = tokens;
        _credentials = credentials;
        _sessions = sessions;
        _audit = audit;
        _mailer = mailer;
        _ipHasher = ipHasher;
        _clock = clock;
    }

    public async Task<AdminAuthOutcome> SignInAsync(
        string? email,
        string? password,
        string? turnstileToken,
        string? remoteIp,
        CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var ip = NormalizeIp(remoteIp);
        var ipHmac = HashIp(ip);

        if (!await _turnstile.VerifyAsync(turnstileToken ?? string.Empty, ip, ct).ConfigureAwait(false))
        {
            await AuditAsync(AdminAuthAction.LoginFailure, email, ipHmac, AdminAuthReason.CaptchaFailed, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.CaptchaFailed();
        }

        if (await IsThrottledAsync(AdminAuthScopes.LoginIp, ip, now, ct).ConfigureAwait(false))
        {
            await AuditAsync(AdminAuthAction.LoginFailure, email, ipHmac, AdminAuthReason.RateLimited, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.ThrottledSignIn();
        }

        var normalized = NormalizeEmail(email);
        if (await IsLockedAsync(AdminAuthScopes.Account, normalized, now, ct).ConfigureAwait(false))
        {
            await _credentials.RunDummyPasswordCheckAsync(ct).ConfigureAwait(false);
            await AuditAsync(AdminAuthAction.LoginFailure, normalized, ipHmac, AdminAuthReason.Locked, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.SignInFailed();
        }

        var passwordOk = await _credentials.VerifyPasswordAsync(normalized, password ?? string.Empty, ct)
            .ConfigureAwait(false);
        if (!passwordOk)
        {
            await CountFailureAsync(normalized, ip, AdminAuthScopes.LoginIp, AdminAuthReason.BadPassword, ipHmac, now, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.SignInFailed();
        }

        var rawPending = CreateOpaqueToken();
        await _tokens.CancelUnusedAsync(AdminAuthToken.KindPending, _credentials.OwnerEmail, now, ct)
            .ConfigureAwait(false);
        var pending = AdminAuthToken.Create(
            AdminAuthToken.KindPending,
            HashToken(rawPending),
            _credentials.OwnerEmail,
            now,
            PendingLifetime);
        await _tokens.AddAsync(pending, ct).ConfigureAwait(false);
        await _tokens.SaveChangesAsync(ct).ConfigureAwait(false);
        return AdminAuthOutcome.Pending(rawPending);
    }

    public async Task<AdminAuthOutcome> VerifySecondFactorAsync(
        string? pendingRaw,
        string? totpCode,
        string? recoveryCode,
        string? remoteIp,
        CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var ip = NormalizeIp(remoteIp);
        var ipHmac = HashIp(ip);

        if (await IsThrottledAsync(AdminAuthScopes.LoginIp, ip, now, ct).ConfigureAwait(false))
        {
            await AuditAsync(AdminAuthAction.LoginFailure, null, ipHmac, AdminAuthReason.RateLimited, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.ThrottledSignIn();
        }

        var pending = await LoadUsableAsync(pendingRaw, AdminAuthToken.KindPending, now, ct).ConfigureAwait(false);
        if (pending is null)
        {
            return AdminAuthOutcome.SignInFailed();
        }

        if (await IsLockedAsync(AdminAuthScopes.Account, pending.Email, now, ct).ConfigureAwait(false))
        {
            await AuditAsync(AdminAuthAction.LoginFailure, pending.Email, ipHmac, AdminAuthReason.Locked, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.SignInFailed();
        }

        var totp = totpCode?.Trim() ?? string.Empty;
        var recovery = recoveryCode?.Trim() ?? string.Empty;
        var usedRecovery = !string.IsNullOrEmpty(recovery);
        var ok = usedRecovery
            ? await _credentials.VerifyRecoveryCodeAsync(pending.Email, recovery, ct).ConfigureAwait(false)
            : await _credentials.VerifyTotpAsync(pending.Email, totp, ct).ConfigureAwait(false);

        if (!ok)
        {
            pending.IncrementAttempt();
            if (pending.AttemptCount >= PendingAttemptLimit)
            {
                pending.Consume(now);
            }

            await _tokens.SaveChangesAsync(ct).ConfigureAwait(false);
            await AuditAsync(AdminAuthAction.SecondFactorFailed, pending.Email, ipHmac, AdminAuthReason.Bad2Fa, ct)
                .ConfigureAwait(false);
            await CountFailureAsync(pending.Email, ip, AdminAuthScopes.LoginIp, AdminAuthReason.Bad2Fa, ipHmac, now, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.SignInFailed();
        }

        pending.Consume(now);
        await _tokens.SaveChangesAsync(ct).ConfigureAwait(false);
        await _lockouts.ClearAccountFailuresAsync(pending.Email, ct).ConfigureAwait(false);
        await _lockouts.SaveChangesAsync(ct).ConfigureAwait(false);

        var session = await RotateSessionAsync(pending.Email, ipHmac, now, ct).ConfigureAwait(false);
        await AuditAsync(AdminAuthAction.LoginSuccess, pending.Email, ipHmac, null, ct).ConfigureAwait(false);
        return AdminAuthOutcome.SignedIn(session.Id);
    }

    public async Task<AdminAuthOutcome> RequestResetAsync(
        string? email,
        string? turnstileToken,
        string? remoteIp,
        CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var ip = NormalizeIp(remoteIp);
        var ipHmac = HashIp(ip);

        if (!await _turnstile.VerifyAsync(turnstileToken ?? string.Empty, ip, ct).ConfigureAwait(false))
        {
            await AuditAsync(AdminAuthAction.ResetRequest, email, ipHmac, AdminAuthReason.CaptchaFailed, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.CaptchaFailed();
        }

        if (await IsThrottledAsync(AdminAuthScopes.ResetIp, ip, now, ct).ConfigureAwait(false))
        {
            await AuditAsync(AdminAuthAction.ResetRequest, email, ipHmac, AdminAuthReason.RateLimited, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.ThrottledReset();
        }

        // Every reset request counts toward the per-IP window; Turnstile
        // failures above do not. In-lock requests never reach here.
        await CountIpOnlyAsync(AdminAuthScopes.ResetIp, ip, now, ct).ConfigureAwait(false);

        if (_credentials.IsOwnerEmail(email))
        {
            var raw = CreateOpaqueToken();
            await _tokens.CancelUnusedAsync(AdminAuthToken.KindReset, _credentials.OwnerEmail, now, ct)
                .ConfigureAwait(false);
            var token = AdminAuthToken.Create(
                AdminAuthToken.KindReset,
                HashToken(raw),
                _credentials.OwnerEmail,
                now,
                ResetLifetime);
            await _tokens.AddAsync(token, ct).ConfigureAwait(false);
            await _tokens.SaveChangesAsync(ct).ConfigureAwait(false);
            await _mailer.SendAsync(
                    new AdminMailMessage(
                        _credentials.OwnerEmail,
                        "Reset",
                        AdminAuthLinks.ResetConfirm(raw)),
                    ct)
                .ConfigureAwait(false);
            await AuditAsync(AdminAuthAction.ResetRequest, _credentials.OwnerEmail, ipHmac, null, ct)
                .ConfigureAwait(false);
        }

        return AdminAuthOutcome.ResetAccepted();
    }

    public async Task<AdminAuthOutcome> CompleteLinkAsync(
        string kind,
        string? rawToken,
        string? password,
        string? claimedEmail,
        string? turnstileToken,
        string? remoteIp,
        CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var ip = NormalizeIp(remoteIp);
        var ipHmac = HashIp(ip);
        var action = kind == AdminAuthToken.KindBootstrap
            ? AdminAuthAction.BootstrapComplete
            : AdminAuthAction.ResetComplete;

        if (!await _turnstile.VerifyAsync(turnstileToken ?? string.Empty, ip, ct).ConfigureAwait(false))
        {
            await AuditAsync(action, claimedEmail, ipHmac, AdminAuthReason.CaptchaFailed, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.CaptchaFailed();
        }

        if (await IsThrottledAsync(AdminAuthScopes.ResetIp, ip, now, ct).ConfigureAwait(false))
        {
            await AuditAsync(action, claimedEmail, ipHmac, AdminAuthReason.RateLimited, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.ThrottledReset();
        }

        var token = await LoadUsableAsync(rawToken, kind, now, ct).ConfigureAwait(false);
        var emailOk = token is null
            || string.IsNullOrWhiteSpace(claimedEmail)
            || string.Equals(claimedEmail.Trim(), token.Email, StringComparison.OrdinalIgnoreCase);
        if (token is null || !emailOk)
        {
            await CountIpOnlyAsync(AdminAuthScopes.ResetIp, ip, now, ct).ConfigureAwait(false);
            await AuditAsync(AdminAuthAction.LinkRejected, claimedEmail, ipHmac, AdminAuthReason.InvalidLink, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.InvalidLink();
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return new AdminAuthOutcome(400, "Use at least 15 characters.", null, null);
        }

        // Consume the token, set the password, and end sessions together.
        // The in-memory credential directory cannot join the EF transaction;
        // the token row and session deletes share this request's DbContext.
        token.Consume(now);
        await _sessions.DeleteByEmailAsync(token.Email, ct).ConfigureAwait(false);
        await _credentials.SetPasswordAsync(token.Email, password, ct).ConfigureAwait(false);
        await _tokens.SaveChangesAsync(ct).ConfigureAwait(false);
        await AuditAsync(action, token.Email, ipHmac, null, ct).ConfigureAwait(false);
        return AdminAuthOutcome.LinkCompleted();
    }

    public Task<int> CountFailuresAsync(string scope, string subjectKey, CancellationToken ct)
    {
        var windowStart = _clock.UtcNow - FailureWindow;
        return _lockouts.CountFailuresAsync(scope, subjectKey, windowStart, ct);
    }

    public Task<AdminAuthLockout?> GetActiveLockoutAsync(string scope, string subjectKey, CancellationToken ct)
        => _lockouts.GetActiveLockoutAsync(scope, subjectKey, _clock.UtcNow, ct);

    public static string HashToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public static string NormalizeIp(string? ip) =>
        string.IsNullOrWhiteSpace(ip) ? "0.0.0.0" : ip.Trim();

    public async Task IssueLinkAsync(string kind, string email, string rawToken, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        await _tokens.CancelUnusedAsync(kind, email, now, ct).ConfigureAwait(false);
        var token = AdminAuthToken.Create(kind, HashToken(rawToken), email, now, LifetimeFor(kind));
        await _tokens.AddAsync(token, ct).ConfigureAwait(false);
        await _tokens.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public static TimeSpan LifetimeFor(string kind) =>
        kind == AdminAuthToken.KindBootstrap ? BootstrapLifetime
        : kind == AdminAuthToken.KindPending ? PendingLifetime
        : ResetLifetime;

    private async Task<AdminSession> RotateSessionAsync(
        string email,
        string ipHmac,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await _sessions.DeleteByEmailAsync(email, ct).ConfigureAwait(false);
        var session = AdminSession.Create(email, ipHmac, now);
        session.MarkTotpVerified();
        await _sessions.AddAsync(session, ct).ConfigureAwait(false);
        await _sessions.SaveChangesAsync(ct).ConfigureAwait(false);
        return session;
    }

    private async Task CountFailureAsync(
        string email,
        string ip,
        string ipScope,
        string reason,
        string ipHmac,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var isOwner = _credentials.IsOwnerEmail(email);
        if (isOwner)
        {
            await _lockouts.AddFailureAsync(
                    AdminAuthFailureEvent.Create(AdminAuthScopes.Account, email, now), ct)
                .ConfigureAwait(false);
        }

        await _lockouts.AddFailureAsync(AdminAuthFailureEvent.Create(ipScope, ip, now), ct)
            .ConfigureAwait(false);
        await _lockouts.SaveChangesAsync(ct).ConfigureAwait(false);

        await AuditAsync(AdminAuthAction.LoginFailure, email, ipHmac, reason, ct).ConfigureAwait(false);

        if (isOwner)
        {
            await MaybeLockAsync(AdminAuthScopes.Account, email, AccountFailureLimit, ipHmac, now, ct)
                .ConfigureAwait(false);
        }

        await MaybeLockAsync(ipScope, ip, IpFailureLimit, ipHmac, now, ct).ConfigureAwait(false);
    }

    private async Task CountIpOnlyAsync(string ipScope, string ip, DateTimeOffset now, CancellationToken ct)
    {
        await _lockouts.AddFailureAsync(AdminAuthFailureEvent.Create(ipScope, ip, now), ct)
            .ConfigureAwait(false);
        await _lockouts.SaveChangesAsync(ct).ConfigureAwait(false);
        await MaybeLockAsync(ipScope, ip, IpFailureLimit, HashIp(ip), now, ct).ConfigureAwait(false);
    }

    private async Task MaybeLockAsync(
        string scope,
        string subjectKey,
        int limit,
        string ipHmac,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (await IsLockedAsync(scope, subjectKey, now, ct).ConfigureAwait(false))
        {
            return;
        }

        var count = await _lockouts
            .CountFailuresAsync(scope, subjectKey, now - FailureWindow, ct)
            .ConfigureAwait(false);
        if (count < limit)
        {
            return;
        }

        var lockout = AdminAuthLockout.Create(scope, subjectKey, now, LockDuration);
        await _lockouts.AddLockoutAsync(lockout, ct).ConfigureAwait(false);
        await _lockouts.SaveChangesAsync(ct).ConfigureAwait(false);
        if (scope == AdminAuthScopes.Account)
        {
            await AuditAsync(AdminAuthAction.LockStart, subjectKey, ipHmac, AdminAuthReason.Locked, ct)
                .ConfigureAwait(false);
        }
    }

    private Task<bool> IsLockedAsync(string scope, string subjectKey, DateTimeOffset now, CancellationToken ct)
        => ActiveAsync(scope, subjectKey, now, ct);

    private Task<bool> IsThrottledAsync(string scope, string subjectKey, DateTimeOffset now, CancellationToken ct)
        => ActiveAsync(scope, subjectKey, now, ct);

    private async Task<bool> ActiveAsync(string scope, string subjectKey, DateTimeOffset now, CancellationToken ct)
    {
        var lockout = await _lockouts.GetActiveLockoutAsync(scope, subjectKey, now, ct).ConfigureAwait(false);
        return lockout is not null && lockout.IsActive(now);
    }

    private async Task<AdminAuthToken?> LoadUsableAsync(
        string? raw,
        string kind,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var token = await _tokens.GetByHashAsync(HashToken(raw), ct).ConfigureAwait(false);
        if (token is null || token.Kind != kind || !token.IsUsable(now))
        {
            return null;
        }

        return token;
    }

    private async Task AuditAsync(
        string action,
        string? email,
        string ipHmac,
        string? reason,
        CancellationToken ct)
    {
        var actor = string.IsNullOrWhiteSpace(email) ? "anonymous" : NormalizeEmail(email);
        await _audit.AddAsync(AdminAuditEntry.CreateAuthEvent(action, actor, ipHmac, reason, _clock.UtcNow), ct)
            .ConfigureAwait(false);
        await _audit.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private string HashIp(string ip) => _ipHasher.Hash(ip);

    public static string CreateOpaqueToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(LinkTokenByteCount));
}

public sealed record AdminAuthOutcome(
    int StatusCode,
    string Body,
    string? PendingToken,
    Guid? SessionId)
{
    public static AdminAuthOutcome CaptchaFailed() =>
        new(401, AdminAuthCopy.VerificationFailed, null, null);

    public static AdminAuthOutcome SignInFailed() =>
        new(401, AdminAuthCopy.SignInFailure, null, null);

    public static AdminAuthOutcome ThrottledSignIn() =>
        new(429, AdminAuthCopy.SignInFailure, null, null);

    public static AdminAuthOutcome ThrottledReset() =>
        new(429, AdminAuthCopy.ResetExists, null, null);

    public static AdminAuthOutcome ResetAccepted() =>
        new(200, AdminAuthCopy.ResetExists, null, null);

    public static AdminAuthOutcome InvalidLink() =>
        new(401, AdminAuthCopy.InvalidLink, null, null);

    public static AdminAuthOutcome LinkCompleted() =>
        new(200, "ok", null, null);

    public static AdminAuthOutcome Pending(string token) =>
        new(200, "second_factor", token, null);

    public static AdminAuthOutcome SignedIn(Guid sessionId) =>
        new(200, "ok", null, sessionId);
}
