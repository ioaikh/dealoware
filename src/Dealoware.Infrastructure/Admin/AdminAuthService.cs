using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Admin;

public sealed class AdminAuthService
{
    public const int AccountFailureLimit = 5;
    public const int IpFailureLimit = 20;
    public const int PendingAttemptLimit = 5;
    public const int MinimumPasswordLength = 15;
    public const string AccountKeyHmacPrefix = "account:";
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
    private readonly IAdminMailQueue _mailQueue;
    private readonly IIpHasher _ipHasher;
    private readonly IAdminClock _clock;

    public AdminAuthService(
        ITurnstileVerifier turnstile,
        IAdminLockoutStore lockouts,
        IAdminAuthTokenStore tokens,
        IAdminCredentialDirectory credentials,
        IAdminSessionRepository sessions,
        IAdminAuditRepository audit,
        IAdminMailQueue mailQueue,
        IIpHasher ipHasher,
        IAdminClock clock)
    {
        _turnstile = turnstile;
        _lockouts = lockouts;
        _tokens = tokens;
        _credentials = credentials;
        _sessions = sessions;
        _audit = audit;
        _mailQueue = mailQueue;
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

        var accountKey = NormalizeEmail(email);
        var accountSubject = AccountSubjectKey(accountKey);
        var ipReserved = await AdminLockoutFlow.ReserveAsync(
                _lockouts, AdminAuthScopes.LoginIp, ip, now, IpFailureLimit, ct)
            .ConfigureAwait(false);
        if (!ipReserved.Incremented)
        {
            await AuditAsync(AdminAuthAction.LoginFailure, accountKey, ipHmac, AdminAuthReason.RateLimited, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.ThrottledSignIn();
        }

        var accountReserved = await AdminLockoutFlow.ReserveAsync(
                _lockouts, AdminAuthScopes.Account, accountSubject, now, AccountFailureLimit, ct)
            .ConfigureAwait(false);
        if (!accountReserved.Incremented)
        {
            await _credentials.RunDummyPasswordCheckAsync(ct).ConfigureAwait(false);
            await AuditAsync(AdminAuthAction.LoginFailure, accountKey, ipHmac, AdminAuthReason.Locked, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.SignInFailed();
        }

        var passwordOk = await _credentials.VerifyPasswordAsync(accountKey, password ?? string.Empty, ct)
            .ConfigureAwait(false);
        if (!passwordOk)
        {
            await ConfirmAccountLockAsync(accountReserved, accountSubject, accountKey, ipHmac, now, ct)
                .ConfigureAwait(false);
            await AdminLockoutFlow.ConfirmFailureAsync(
                    _lockouts, ipReserved, AdminAuthScopes.LoginIp, ip, now, IpFailureLimit, null, ct)
                .ConfigureAwait(false);
            await AuditAsync(AdminAuthAction.LoginFailure, accountKey, ipHmac, AdminAuthReason.BadPassword, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.SignInFailed();
        }

        await AdminLockoutFlow.ReleaseSuccessAsync(
                _lockouts, accountReserved, AdminAuthScopes.Account, accountSubject, now, AccountFailureLimit, ct)
            .ConfigureAwait(false);
        await AdminLockoutFlow.ReleaseSuccessAsync(
                _lockouts, ipReserved, AdminAuthScopes.LoginIp, ip, now, IpFailureLimit, ct)
            .ConfigureAwait(false);

        var ownerKey = NormalizeEmail(_credentials.OwnerEmail);
        var rawPending = CreateOpaqueToken();
        await _tokens.CancelUnusedAsync(AdminAuthToken.KindPending, ownerKey, now, ct)
            .ConfigureAwait(false);
        var pending = AdminAuthToken.Create(
            AdminAuthToken.KindPending,
            HashToken(rawPending),
            ownerKey,
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

        var ipReserved = await AdminLockoutFlow.ReserveAsync(
                _lockouts, AdminAuthScopes.LoginIp, ip, now, IpFailureLimit, ct)
            .ConfigureAwait(false);
        if (!ipReserved.Incremented)
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

        var accountKey = NormalizeEmail(pending.Email);
        var accountSubject = AccountSubjectKey(accountKey);
        var accountReserved = await AdminLockoutFlow.ReserveAsync(
                _lockouts, AdminAuthScopes.Account, accountSubject, now, AccountFailureLimit, ct)
            .ConfigureAwait(false);
        if (!accountReserved.Incremented
            || !await _tokens.TryIncrementPendingAttemptAsync(pending.Id, now, PendingAttemptLimit, ct)
                .ConfigureAwait(false))
        {
            await AuditAsync(AdminAuthAction.LoginFailure, accountKey, ipHmac, AdminAuthReason.Locked, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.SignInFailed();
        }

        var totp = totpCode?.Trim() ?? string.Empty;
        var recovery = recoveryCode?.Trim() ?? string.Empty;
        var usedRecovery = !string.IsNullOrEmpty(recovery);
        var ok = usedRecovery
            ? await _credentials.VerifyRecoveryCodeAsync(accountKey, recovery, ct).ConfigureAwait(false)
            : await _credentials.VerifyTotpAsync(accountKey, totp, ct).ConfigureAwait(false);

        if (!ok)
        {
            await ConfirmAccountLockAsync(accountReserved, accountSubject, accountKey, ipHmac, now, ct)
                .ConfigureAwait(false);
            await AdminLockoutFlow.ConfirmFailureAsync(
                    _lockouts, ipReserved, AdminAuthScopes.LoginIp, ip, now, IpFailureLimit, null, ct)
                .ConfigureAwait(false);
            await AuditAsync(AdminAuthAction.SecondFactorFailed, accountKey, ipHmac, AdminAuthReason.Bad2Fa, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.SignInFailed();
        }

        if (!await _tokens.TryConsumeAsync(pending.TokenHash, now, ct).ConfigureAwait(false))
        {
            return AdminAuthOutcome.SignInFailed();
        }

        await AdminLockoutFlow.ReleaseSuccessAsync(
                _lockouts, ipReserved, AdminAuthScopes.LoginIp, ip, now, IpFailureLimit, ct)
            .ConfigureAwait(false);
        await AdminLockoutFlow.ReleaseSuccessAsync(
                _lockouts, accountReserved, AdminAuthScopes.Account, accountSubject, now, AccountFailureLimit, ct)
            .ConfigureAwait(false);

        var session = await RotateSessionAsync(accountKey, ipHmac, now, ct).ConfigureAwait(false);
        await AuditAsync(AdminAuthAction.LoginSuccess, accountKey, ipHmac, null, ct).ConfigureAwait(false);
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
        var accountKey = NormalizeEmail(email);

        if (!await _turnstile.VerifyAsync(turnstileToken ?? string.Empty, ip, ct).ConfigureAwait(false))
        {
            await AuditAsync(AdminAuthAction.ResetRequest, accountKey, ipHmac, AdminAuthReason.CaptchaFailed, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.CaptchaFailed();
        }

        var ipReserved = await AdminLockoutFlow.ReserveAsync(
                _lockouts, AdminAuthScopes.ResetIp, ip, now, IpFailureLimit, ct)
            .ConfigureAwait(false);
        if (!ipReserved.Incremented)
        {
            await AuditAsync(AdminAuthAction.ResetRequest, accountKey, ipHmac, AdminAuthReason.RateLimited, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.ThrottledReset();
        }

        if (ipReserved.Count >= IpFailureLimit)
        {
            await AdminLockoutFlow.ConfirmFailureAsync(
                    _lockouts, ipReserved, AdminAuthScopes.ResetIp, ip, now, IpFailureLimit, null, ct)
                .ConfigureAwait(false);
        }

        var tokenEmail = _credentials.IsOwnerEmail(accountKey)
            ? NormalizeEmail(_credentials.OwnerEmail)
            : AccountSubjectKey(accountKey, isOwner: false, _ipHasher);
        var raw = CreateOpaqueToken();
        await _tokens.CancelUnusedAsync(AdminAuthToken.KindReset, tokenEmail, now, ct)
            .ConfigureAwait(false);
        var token = AdminAuthToken.Create(
            AdminAuthToken.KindReset,
            HashToken(raw),
            tokenEmail,
            now,
            ResetLifetime);
        if (!_credentials.IsOwnerEmail(accountKey))
        {
            token.Consume(now);
        }

        await _tokens.AddAsync(token, ct).ConfigureAwait(false);
        await _tokens.SaveChangesAsync(ct).ConfigureAwait(false);
        AdminMailMessage? mail = _credentials.IsOwnerEmail(accountKey)
            ? new AdminMailMessage(tokenEmail, "Reset", AdminAuthLinks.ResetConfirm(raw))
            : null;
        await AuditAsync(
                AdminAuthAction.ResetRequest,
                _credentials.IsOwnerEmail(accountKey) ? tokenEmail : "anonymous",
                ipHmac,
                null,
                ct)
            .ConfigureAwait(false);

        // Owner and unknown both enqueue one work item so the request returns
        // before SendAsync (SC-9).
        await _mailQueue.EnqueueAsync(mail, ct).ConfigureAwait(false);
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

        var ipReserved = await AdminLockoutFlow.ReserveAsync(
                _lockouts, AdminAuthScopes.ResetIp, ip, now, IpFailureLimit, ct)
            .ConfigureAwait(false);
        if (!ipReserved.Incremented)
        {
            await AuditAsync(action, claimedEmail, ipHmac, AdminAuthReason.RateLimited, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.ThrottledReset();
        }

        var token = await LoadUsableAsync(rawToken, kind, now, ct).ConfigureAwait(false);
        var claimedKey = NormalizeEmail(claimedEmail);
        var emailOk = token is null
            || string.IsNullOrWhiteSpace(claimedEmail)
            || string.Equals(claimedKey, NormalizeEmail(token.Email), StringComparison.Ordinal);
        if (token is null || !emailOk)
        {
            await AdminLockoutFlow.ConfirmFailureAsync(
                    _lockouts, ipReserved, AdminAuthScopes.ResetIp, ip, now, IpFailureLimit, null, ct)
                .ConfigureAwait(false);
            await AuditAsync(AdminAuthAction.LinkRejected, claimedEmail, ipHmac, AdminAuthReason.InvalidLink, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.InvalidLink();
        }

        if (IsShortPassword(password))
        {
            return new AdminAuthOutcome(400, AdminAuthCopy.PasswordTooShort, null, null);
        }

        var accountKey = NormalizeEmail(token.Email);
        if (!await _tokens.TryConsumeAsync(token.TokenHash, now, ct).ConfigureAwait(false))
        {
            await AdminLockoutFlow.ConfirmFailureAsync(
                    _lockouts, ipReserved, AdminAuthScopes.ResetIp, ip, now, IpFailureLimit, null, ct)
                .ConfigureAwait(false);
            await AuditAsync(AdminAuthAction.LinkRejected, accountKey, ipHmac, AdminAuthReason.InvalidLink, ct)
                .ConfigureAwait(false);
            return AdminAuthOutcome.InvalidLink();
        }

        await _sessions.DeleteByEmailAsync(accountKey, ct).ConfigureAwait(false);
        await _credentials.SetPasswordAsync(accountKey, password, ct).ConfigureAwait(false);
        await AuditAsync(action, accountKey, ipHmac, null, ct).ConfigureAwait(false);
        return AdminAuthOutcome.LinkCompleted();
    }

    public Task<int> CountFailuresAsync(string scope, string subjectKey, CancellationToken ct)
    {
        var windowStart = _clock.UtcNow - FailureWindow;
        return _lockouts.CountFailuresAsync(scope, NormalizeSubject(scope, subjectKey), windowStart, ct);
    }

    public Task<AdminAuthLockout?> GetActiveLockoutAsync(string scope, string subjectKey, CancellationToken ct)
        => _lockouts.GetActiveLockoutAsync(scope, NormalizeSubject(scope, subjectKey), _clock.UtcNow, ct);

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
        var accountKey = NormalizeEmail(email);
        await _tokens.CancelUnusedAsync(kind, accountKey, now, ct).ConfigureAwait(false);
        var token = AdminAuthToken.Create(kind, HashToken(rawToken), accountKey, now, LifetimeFor(kind));
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
        var accountKey = NormalizeEmail(email);
        await _sessions.DeleteByEmailAsync(accountKey, ct).ConfigureAwait(false);
        var session = AdminSession.Create(accountKey, ipHmac, now);
        session.MarkTotpVerified();
        await _sessions.AddAsync(session, ct).ConfigureAwait(false);
        await _sessions.SaveChangesAsync(ct).ConfigureAwait(false);
        return session;
    }

    public static bool IsShortPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return true;
        }

        var stripped = StripFormatCharacters(password);
        if (string.IsNullOrWhiteSpace(stripped))
        {
            return true;
        }

        return new StringInfo(stripped).LengthInTextElements < MinimumPasswordLength;
    }

    public static string AccountSubjectKey(string normalizedEmail, bool isOwner, IIpHasher hasher)
    {
        var hashed = hasher.Hash(AccountKeyHmacPrefix + normalizedEmail);
        return isOwner ? normalizedEmail : hashed;
    }

    private static string StripFormatCharacters(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.Format)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private Task ConfirmAccountLockAsync(
        FailureIncrementResult reserved,
        string accountSubject,
        string accountEmail,
        string ipHmac,
        DateTimeOffset now,
        CancellationToken ct)
        => AdminLockoutFlow.ConfirmFailureAsync(
            _lockouts,
            reserved,
            AdminAuthScopes.Account,
            accountSubject,
            now,
            AccountFailureLimit,
            async () =>
            {
                await AuditAsync(AdminAuthAction.LockStart, accountEmail, ipHmac, AdminAuthReason.Locked, ct)
                    .ConfigureAwait(false);
                await _tokens.ConsumeOutstandingPendingAsync(accountEmail, now, ct).ConfigureAwait(false);
                await _sessions.DeleteByEmailAsync(accountEmail, ct).ConfigureAwait(false);
            },
            ct);

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
        var actor = AuditActorEmail(email);
        await _audit.AddAsync(AdminAuditEntry.CreateAuthEvent(action, actor, ipHmac, reason, _clock.UtcNow), ct)
            .ConfigureAwait(false);
        await _audit.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private string AuditActorEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "anonymous";
        }

        var key = NormalizeEmail(email);
        if (key.Length == 0 || string.Equals(key, "anonymous", StringComparison.Ordinal))
        {
            return "anonymous";
        }

        return _credentials.IsOwnerEmail(key)
            ? key
            : AccountSubjectKey(key, isOwner: false, _ipHasher);
    }

    private string HashIp(string ip) => _ipHasher.Hash(ip);

    private string AccountSubjectKey(string email)
    {
        var key = NormalizeEmail(email);
        return AccountSubjectKey(key, _credentials.IsOwnerEmail(key), _ipHasher);
    }

    private string NormalizeSubject(string scope, string subjectKey) =>
        scope == AdminAuthScopes.Account ? AccountSubjectKey(subjectKey) : subjectKey;

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
