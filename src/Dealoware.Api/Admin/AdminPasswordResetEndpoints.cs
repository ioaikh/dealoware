using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Dealoware.Application.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Password-reset API under /admin/api/auth/reset*. Confirm GET is token-free
/// (b6194918 item 6). The token travels in the mail fragment and the POST body.
/// CSRF uses #33's keyed token (X-CSRF-TOKEN), never a cookie echo.
/// </summary>
public static class AdminPasswordResetEndpoints
{
    public const string RequestApiPath = AdminSignedOutExemptions.ResetApi;
    public const string ConfirmApiPath = AdminSignedOutExemptions.ResetConfirmApi;
    public const string ConfirmCheckApiPath = AdminSignedOutExemptions.ResetConfirmCheckApi;
    public const string RequestPagePath = AdminSignedOutExemptions.ResetPage;
    public const string ConfirmPagePath = AdminSignedOutExemptions.ResetConfirmPage;
    public const string LinkExpiredPath = AdminSignedOutExemptions.LinkExpiredPage;
    public const string ConfirmScriptPath = AdminPasswordResetCopy.ConfirmScriptPath;
    public const int TokenEntropyBytes = 32;

    public static void MapAdminPasswordReset(this WebApplication app)
    {
        app.MapGet(ConfirmPagePath, ConfirmPageGet)
            .WithName("AdminPasswordResetConfirmPageGet")
            .WithTags("Admin")
            .AllowAnonymous();

        app.MapGet(LinkExpiredPath, LinkExpiredGet)
            .WithName("AdminPasswordResetLinkExpired")
            .WithTags("Admin")
            .AllowAnonymous();

        app.MapPost(RequestApiPath, RequestReset)
            .WithName("AdminPasswordResetRequest")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status429TooManyRequests);

        app.MapPost(RequestPagePath, RequestReset)
            .WithName("AdminPasswordResetRequestPage")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status429TooManyRequests);

        app.MapPost(ConfirmApiPath, CompleteReset)
            .WithName("AdminPasswordResetConfirm")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status303SeeOther)
            .Produces(StatusCodes.Status429TooManyRequests);

        app.MapPost(ConfirmPagePath, CompleteReset)
            .WithName("AdminPasswordResetConfirmPage")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status303SeeOther)
            .Produces(StatusCodes.Status429TooManyRequests);

        app.MapPost(ConfirmCheckApiPath, CheckReset)
            .WithName("AdminPasswordResetConfirmCheck")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status429TooManyRequests);
    }

    public static string ConfirmPageContentSecurityPolicy(string nonce)
        => "default-src 'none'; "
           + $"script-src 'nonce-{nonce}'; "
           + "connect-src 'self'; "
           + "frame-ancestors 'none'; "
           + "base-uri 'none'; "
           + "form-action 'self'; "
           + "object-src 'none'";

    private static IResult ConfirmPageGet(HttpContext http, AdminAntiForgeryService antiforgery)
    {
        ApplyNoStoreNoReferrer(http);
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        http.Response.Headers.ContentSecurityPolicy = ConfirmPageContentSecurityPolicy(nonce);
        var csrf = antiforgery.Issue(http);
        var html =
            "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
            $"<meta name=\"dw-csrf\" content=\"{csrf}\">" +
            $"<title>{AdminPasswordResetCopy.ConfirmPageTitle}</title></head>" +
            "<body><main id=\"reset-main\" aria-busy=\"true\">" +
            $"<noscript>{AdminPasswordResetCopy.ConfirmNoscript}</noscript>" +
            $"<p id=\"reset-status\" role=\"status\" hidden>{AdminPasswordResetCopy.CheckingLink}</p>" +
            $"<p id=\"reset-alert\" role=\"alert\" hidden tabindex=\"-1\" data-retry=\"{AdminPasswordResetCopy.CheckRetry}\"></p>" +
            $"<button type=\"button\" id=\"reset-retry\" hidden>{AdminPasswordResetCopy.TryAgain}</button>" +
            $"<form method=\"post\" action=\"{ConfirmPagePath}\" id=\"reset-form\" hidden>" +
            "<input type=\"hidden\" name=\"token\" id=\"token\" value=\"\">" +
            "<label>New password <input type=\"password\" name=\"newPassword\" id=\"newPassword\" autocomplete=\"new-password\"></label>" +
            "<label>Authenticator code <input name=\"totp\" id=\"totp\" inputmode=\"numeric\" autocomplete=\"one-time-code\"></label>" +
            "<label>Recovery code <input name=\"recoveryCode\" id=\"recoveryCode\" autocomplete=\"one-time-code\"></label>" +
            "<button type=\"submit\">Change password</button></form>" +
            $"<script src=\"{ConfirmScriptPath}\" nonce=\"{nonce}\"></script>" +
            "</main></body></html>";
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static IResult LinkExpiredGet(HttpContext http)
    {
        ApplyNoStoreNoReferrer(http);
        var html =
            "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
            $"<title>{AdminPasswordResetCopy.LinkExpiredPageTitle}</title></head>" +
            $"<body><p>{AdminPasswordResetCopy.InvalidLink}</p></body></html>";
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> RequestReset(
        ResetRequestBody? body,
        IAdminPasswordResetTokenRepository tokens,
        AdminMailSendQueue mail,
        IAdminResetIpThrottle throttle,
        IAdminAuditRepository audit,
        IIpHasher ipHasher,
        IAdminClock clock,
        IOptions<CoreOwnerOptions> coreOwner,
        HttpContext http)
    {
        var now = clock.UtcNow;
        var submitted = NormalizeEmail(body?.Email);
        var ownerEmail = NormalizeEmail(coreOwner.Value.Email);
        var ipHmac = HashClientIp(ipHasher, http);
        var ct = http.RequestAborted;

        if (!await throttle.TryReserveAsync(ipHmac, now, ct))
        {
            return RequestThrottledResult();
        }

        var known = string.Equals(submitted, ownerEmail, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(ownerEmail);
        var raw = CreateRawToken();
        var hash = AdminPasswordResetToken.HashRaw(raw);
        var stagedEmail = known ? ownerEmail : submitted;
        await tokens.InvalidateUnusedForEmailAsync(stagedEmail, now, ct);
        await tokens.AddAsync(AdminPasswordResetToken.Create(stagedEmail, hash, now), ct);
        if (known)
        {
            mail.EnqueuePasswordReset(ownerEmail, raw, http.TraceIdentifier);
        }

        await audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent("reset_request", submitted, ipHmac, timestamp: now),
            ct);
        await audit.SaveChangesAsync(ct);

        return Results.Json(new { message = AdminPasswordResetCopy.RequestAccepted });
    }

    private static async Task<IResult> CompleteReset(
        ResetCompleteBody? body,
        IAdminPasswordResetTokenRepository tokens,
        IAdminCredentialStore credentials,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminSecondFactorVerifier secondFactor,
        IAdminPasswordHasher hasher,
        IAdminSessionRepository sessions,
        IAdminAuditRepository audit,
        IAdminResetIpThrottle throttle,
        IIpHasher ipHasher,
        IAdminClock clock,
        IOptions<CoreOwnerOptions> coreOwner,
        DealowareDbContext db,
        HttpContext http)
    {
        var now = clock.UtcNow;
        var ownerEmail = NormalizeEmail(coreOwner.Value.Email);
        var ipHmac = HashClientIp(ipHasher, http);
        var ct = http.RequestAborted;
        if (!await throttle.TryReserveAsync(ipHmac, now, ct))
        {
            return ConfirmThrottledResult();
        }

        var presented = NullIfWhiteSpace(body?.Token)
                        ?? await AdminSignedOutExemptions.ReadResetTokenAsync(http, http.RequestAborted);
        var hash = AdminPasswordResetToken.HashRaw(presented ?? string.Empty);
        var row = await tokens.FindUsableByHashAsync(hash, now, ct);

        if (row is null
            || !string.Equals(row.Email, ownerEmail, StringComparison.Ordinal))
        {
            return await FailExpiredAsync(audit, ipHmac, now, http);
        }

        var credential = await credentials.GetByEmailAsync(ownerEmail, ct);
        if (credential is null)
        {
            return await FailExpiredAsync(audit, ipHmac, now, http);
        }

        var proposed = body?.NewPassword ?? string.Empty;
        var matchesCurrent = hasher.Verify(AdminPasswordRules.Normalize(proposed), credential.PasswordHash)
                             || hasher.Verify(proposed, credential.PasswordHash);
        var rules = AdminPasswordRules.Evaluate(proposed, ownerEmail, matchesCurrent);
        if (!rules.Accepted)
        {
            return Results.Json(
                new { error = rules.Message, reason = ReasonName(rules.Reason) },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var totp = NullIfWhiteSpace(body?.Totp);
        var recovery = NullIfWhiteSpace(body?.RecoveryCode);
        if ((totp is null) == (recovery is null))
        {
            return await FailExpiredAsync(audit, ipHmac, now, http);
        }

        var account = await accounts.GetByEmailAsync(ownerEmail, ct);
        if (account is null || !account.IsTotpEnrolled)
        {
            return await FailExpiredAsync(audit, ipHmac, now, http);
        }

        if (account.IsFactorLocked(now))
        {
            await using var lockedTx = await db.Database.BeginTransactionAsync(ct);
            await BurnAccountResetStateAsync(tokens, sessions, ownerEmail, now, ct);
            await lockedTx.CommitAsync(ct);
            return await FailExpiredAsync(audit, ipHmac, now, http);
        }

        var factor = await secondFactor.EvaluateWithoutCommitAsync(ownerEmail, totp, recovery, ct);
        if (!factor.Succeeded)
        {
            return await FailWrongFactorAsync(
                tokens, accounts, sessions, db, ownerEmail, account.Id, hash, now, http, ct);
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (!await tokens.TryConsumeAsync(hash, now, ct))
        {
            await tx.RollbackAsync(ct);
            return await FailExpiredAsync(audit, ipHmac, now, http);
        }

        if (factor.UsedRecoveryCode)
        {
            if (string.IsNullOrEmpty(factor.RecoveryCodeHash)
                || !await accounts.TryRedeemRecoveryCodeAsync(account.Id, factor.RecoveryCodeHash, now, ct))
            {
                await tx.RollbackAsync(ct);
                return await FailExpiredAsync(audit, ipHmac, now, http);
            }
        }
        else if (factor.TotpTimestep is { } step
                 && !await accounts.TryRecordTotpTimestepAsync(account.Id, step, ct))
        {
            await tx.RollbackAsync(ct);
            return await FailExpiredAsync(audit, ipHmac, now, http);
        }

        await accounts.ClearFactorLockAsync(account.Id, ct);
        account.ClearFactorFailures();

        var passwordHash = hasher.Hash(AdminPasswordRules.Normalize(proposed));
        credential.ReplacePassword(passwordHash, now);
        account.SetPasswordHash(passwordHash);
        await credentials.SaveChangesAsync(ct);
        await sessions.RevokeAllForEmailAsync(ownerEmail, ct);

        await audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent("reset_complete", ownerEmail, ipHmac, timestamp: now),
            ct);
        if (factor.UsedRecoveryCode)
        {
            await audit.AddAsync(
                AdminAuditEntry.CreateAuthEvent("recovery_code_use", ownerEmail, ipHmac, timestamp: now),
                ct);
        }

        await audit.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Results.Json(new { message = AdminPasswordResetCopy.CompleteSucceeded });
    }

    private static async Task<IResult> CheckReset(
        ResetCompleteBody? body,
        IAdminPasswordResetTokenRepository tokens,
        IAdminResetIpThrottle throttle,
        IIpHasher ipHasher,
        IAdminClock clock,
        IOptions<CoreOwnerOptions> coreOwner,
        ILoggerFactory loggers,
        HttpContext http)
    {
        var now = clock.UtcNow;
        var ownerEmail = NormalizeEmail(coreOwner.Value.Email);
        var ipHmac = HashClientIp(ipHasher, http);
        var ct = http.RequestAborted;
        if (!await throttle.TryReserveAsync(ipHmac, now, ct))
        {
            return ConfirmThrottledResult();
        }

        var presented = NullIfWhiteSpace(body?.Token);
        var hash = AdminPasswordResetToken.HashRaw(presented ?? string.Empty);
        var row = await tokens.FindUsableByHashAsync(hash, now, ct);
        var valid = row is not null
                    && string.Equals(row.Email, ownerEmail, StringComparison.Ordinal);
        loggers.CreateLogger("Dealoware.Api.Admin.ResetCheck")
            .LogInformation("Admin reset check: {OutcomeClass}", valid ? "valid" : "invalid");
        ApplyNoStoreNoReferrer(http);
        return Results.Json(new { valid });
    }

    private static async Task<IResult> FailExpiredAsync(
        IAdminAuditRepository audit,
        string ipHmac,
        DateTimeOffset now,
        HttpContext http)
    {
        await audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent(
                "reset_complete_failed",
                string.Empty,
                ipHmac,
                reasonClass: "invalid_link",
                timestamp: now),
            http.RequestAborted);
        await audit.SaveChangesAsync(http.RequestAborted);

        ApplyNoStoreNoReferrer(http);
        http.Response.Headers.Location = LinkExpiredPath;
        return Results.StatusCode(StatusCodes.Status303SeeOther);
    }

    private static async Task<IResult> FailWrongFactorAsync(
        IAdminPasswordResetTokenRepository tokens,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminSessionRepository sessions,
        DealowareDbContext db,
        string ownerEmail,
        Guid accountId,
        string tokenHash,
        DateTimeOffset now,
        HttpContext http,
        CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var tokenSlot = await tokens.TryRecordFailedCodeAttemptAsync(tokenHash, now, ct);
        await accounts.TryRecordFailedFactorAttemptAsync(accountId, now, ct);
        if (await accounts.IsFactorLockedAsync(accountId, now, ct))
        {
            await BurnAccountResetStateAsync(tokens, sessions, ownerEmail, now, ct);
        }

        await tx.CommitAsync(ct);
        if (!tokenSlot)
        {
            return WrongFactorExpiredResult(http);
        }

        return WrongFactorUnauthorizedResult(http);
    }

    /// <summary>
    /// Account-wide A16 lock: burn every unused reset token (and bootstrap
    /// tokens, when that table exists) and end every session for the owner,
    /// in the caller's transaction.
    /// </summary>
    private static async Task BurnAccountResetStateAsync(
        IAdminPasswordResetTokenRepository tokens,
        IAdminSessionRepository sessions,
        string ownerEmail,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await tokens.InvalidateUnusedForEmailAsync(ownerEmail, now, ct);
        await sessions.RevokeAllForEmailAsync(ownerEmail, ct);
    }

    private static IResult WrongFactorUnauthorizedResult(HttpContext http)
    {
        ApplyNoStoreNoReferrer(http);
        return Results.Json(
            new { error = AdminPasswordResetCopy.SignInGeneric },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult WrongFactorExpiredResult(HttpContext http)
    {
        ApplyNoStoreNoReferrer(http);
        http.Response.Headers.Location = LinkExpiredPath;
        return Results.StatusCode(StatusCodes.Status303SeeOther);
    }

    private static IResult RequestThrottledResult()
        => Results.Json(
            new { message = AdminPasswordResetCopy.RequestAccepted },
            statusCode: StatusCodes.Status429TooManyRequests);

    private static IResult ConfirmThrottledResult()
        => Results.Json(
            new { message = AdminPasswordResetCopy.SignInGeneric },
            statusCode: StatusCodes.Status429TooManyRequests);

    private static void ApplyNoStoreNoReferrer(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers["Referrer-Policy"] = "no-referrer";
    }

    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string CreateRawToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenEntropyBytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static string BuildResetLink(string rawToken)
        => AdminMailPagePaths.Build(MailLinkKind.Reset, rawToken);

    private static string HashClientIp(IIpHasher ipHasher, HttpContext http)
    {
        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrWhiteSpace(ip))
        {
            ip = "0.0.0.0";
        }

        return ipHasher.Hash(ip);
    }

    private static string ReasonName(AdminPasswordRuleReason reason)
        => reason switch
        {
            AdminPasswordRuleReason.TooShort => "too_short",
            AdminPasswordRuleReason.TooLong => "too_long",
            AdminPasswordRuleReason.CommonOrContext => "common_or_context",
            AdminPasswordRuleReason.SameAsCurrent => "same_as_current",
            _ => "rejected"
        };

    public sealed class ResetRequestBody
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    public sealed class ResetCompleteBody
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("newPassword")]
        public string? NewPassword { get; set; }

        [JsonPropertyName("totp")]
        public string? Totp { get; set; }

        [JsonPropertyName("recoveryCode")]
        public string? RecoveryCode { get; set; }
    }
}
