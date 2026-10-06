using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Password-reset API under /admin/api/auth/reset*. Confirm GET is token-free
/// (b6194918 item 6). The token travels in the mail fragment and the POST body.
/// </summary>
public static class AdminPasswordResetEndpoints
{
    public const string RequestApiPath = AdminSignedOutExemptions.ResetApi;
    public const string ConfirmApiPath = AdminSignedOutExemptions.ResetConfirmApi;
    public const string RequestPagePath = AdminSignedOutExemptions.ResetPage;
    public const string ConfirmPagePath = AdminSignedOutExemptions.ResetConfirmPage;
    public const string LinkExpiredPath = AdminSignedOutExemptions.LinkExpiredPage;
    public const string MailSubject = "Reset your Dealoware admin password";
    public const string ResetLinkOrigin = "https://admin.core.dealoware.com";
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
            .Produces(StatusCodes.Status200OK);

        app.MapPost(RequestPagePath, RequestReset)
            .WithName("AdminPasswordResetRequestPage")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK);

        app.MapPost(ConfirmApiPath, CompleteReset)
            .WithName("AdminPasswordResetConfirm")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status303SeeOther);

        app.MapPost(ConfirmPagePath, CompleteReset)
            .WithName("AdminPasswordResetConfirmPage")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status303SeeOther);
    }

    private static IResult ConfirmPageGet(HttpContext http)
    {
        ApplyNoStoreNoReferrer(http);
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        http.Response.Headers.ContentSecurityPolicy =
            $"default-src 'none'; script-src 'nonce-{nonce}'; form-action 'self'; base-uri 'none'";
        var af = http.Response.Headers.TryGetValue(AdminCookieNames.AntiForgeryHeader, out var issued)
                 && !string.IsNullOrWhiteSpace(issued)
            ? issued.ToString()
            : AdminAntiForgery.Issue(http);
        var html =
            "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
            "<title>Reset password</title></head><body>" +
            $"<form method=\"post\" action=\"{ConfirmPagePath}\" id=\"reset-form\">" +
            $"<input type=\"hidden\" name=\"af\" value=\"{af}\">" +
            "<input type=\"hidden\" name=\"token\" id=\"token\" value=\"\">" +
            "<label>New password <input type=\"password\" name=\"newPassword\" autocomplete=\"new-password\"></label>" +
            "<label>Authenticator code <input name=\"totp\" inputmode=\"numeric\" autocomplete=\"one-time-code\"></label>" +
            "<label>Recovery code <input name=\"recoveryCode\" autocomplete=\"off\"></label>" +
            "<button type=\"submit\">Update password</button></form>" +
            $"<script nonce=\"{nonce}\">(function(){{var h=location.hash;history.replaceState(null,\"\",location.pathname);var m=/[#&]token=([^&]*)/.exec(h);var el=document.getElementById(\"token\");if(el&&m)el.value=decodeURIComponent(m[1].replace(/\\+/g,\" \"));}})();</script>" +
            "</body></html>";
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static IResult LinkExpiredGet(HttpContext http)
    {
        ApplyNoStoreNoReferrer(http);
        var html =
            "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
            $"<title>Link expired</title></head><body><p>{AdminPasswordResetCopy.InvalidLink}</p></body></html>";
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> RequestReset(
        ResetRequestBody? body,
        IAdminPasswordResetTokenRepository tokens,
        IAdminMailSender mail,
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

        if (string.Equals(submitted, ownerEmail, StringComparison.Ordinal)
            && !string.IsNullOrEmpty(ownerEmail))
        {
            await tokens.InvalidateUnusedForEmailAsync(ownerEmail, now, http.RequestAborted);

            var raw = CreateRawToken();
            var hash = AdminPasswordResetToken.HashRaw(raw);
            var row = AdminPasswordResetToken.Create(ownerEmail, hash, now);
            await tokens.AddAsync(row, http.RequestAborted);

            await mail.SendAsync(
                new AdminMailMessage(ownerEmail, MailSubject, BuildResetLink(raw)),
                http.RequestAborted);
        }
        else
        {
            _ = AdminPasswordResetToken.HashRaw(CreateRawToken());
        }

        await audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent("reset_request", submitted, ipHmac, timestamp: now),
            http.RequestAborted);
        await audit.SaveChangesAsync(http.RequestAborted);

        return Results.Json(new { message = AdminPasswordResetCopy.RequestAccepted });
    }

    private static async Task<IResult> CompleteReset(
        ResetCompleteBody? body,
        IAdminPasswordResetTokenRepository tokens,
        IAdminCredentialStore credentials,
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
        var presented = NullIfWhiteSpace(body?.Token)
                        ?? await AdminSignedOutExemptions.ReadResetTokenAsync(http, http.RequestAborted);
        var hash = string.IsNullOrWhiteSpace(presented)
            ? null
            : AdminPasswordResetToken.HashRaw(presented.Trim());
        var row = hash is null
            ? null
            : await tokens.FindUsableByHashAsync(hash, now, http.RequestAborted);

        if (row is null
            || !string.Equals(row.Email, ownerEmail, StringComparison.Ordinal))
        {
            return await FailExpiredAsync(audit, throttle, ipHmac, now, http);
        }

        var totp = NullIfWhiteSpace(body?.Totp);
        var recovery = NullIfWhiteSpace(body?.RecoveryCode);
        if (totp is null && recovery is null)
        {
            return await FailExpiredAsync(audit, throttle, ipHmac, now, http);
        }

        var factor = await secondFactor.VerifyAsync(
            ownerEmail,
            totp,
            recovery,
            http.RequestAborted);
        if (!factor.Succeeded)
        {
            return await FailExpiredAsync(audit, throttle, ipHmac, now, http);
        }

        var credential = await credentials.GetByEmailAsync(ownerEmail, http.RequestAborted);
        if (credential is null)
        {
            return await FailExpiredAsync(audit, throttle, ipHmac, now, http);
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

        await using var tx = await db.Database.BeginTransactionAsync(http.RequestAborted);
        if (!await tokens.TryConsumeAsync(hash!, now, http.RequestAborted))
        {
            await tx.RollbackAsync(http.RequestAborted);
            return await FailExpiredAsync(audit, throttle, ipHmac, now, http);
        }

        credential.ReplacePassword(hasher.Hash(AdminPasswordRules.Normalize(proposed)), now);
        await credentials.SaveChangesAsync(http.RequestAborted);
        await sessions.RevokeAllForEmailAsync(ownerEmail, http.RequestAborted);

        await audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent("reset_complete", ownerEmail, ipHmac, timestamp: now),
            http.RequestAborted);
        if (factor.UsedRecoveryCode)
        {
            await audit.AddAsync(
                AdminAuditEntry.CreateAuthEvent("recovery_code_use", ownerEmail, ipHmac, timestamp: now),
                http.RequestAborted);
        }

        await audit.SaveChangesAsync(http.RequestAborted);
        await tx.CommitAsync(http.RequestAborted);

        return Results.Json(new { message = AdminPasswordResetCopy.CompleteSucceeded });
    }

    private static async Task<IResult> FailExpiredAsync(
        IAdminAuditRepository audit,
        IAdminResetIpThrottle throttle,
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
        await throttle.RecordFailureAsync(ipHmac, http.RequestAborted);

        ApplyNoStoreNoReferrer(http);
        http.Response.Headers.Location = LinkExpiredPath;
        return Results.StatusCode(StatusCodes.Status303SeeOther);
    }

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
        => $"{ResetLinkOrigin}{ConfirmPagePath}#token={Uri.EscapeDataString(rawToken)}";

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
