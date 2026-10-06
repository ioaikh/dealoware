using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Password-reset API under /admin/api/auth/reset*. Page twins under
/// /admin/reset* are owned by Step 14; this PR maps the POST twins so
/// the §2.2 rows this step owns are live.
/// </summary>
public static class AdminPasswordResetEndpoints
{
    public const string RequestApiPath = AdminSignedOutExemptions.ResetApi;
    public const string ConfirmApiPath = AdminSignedOutExemptions.ResetConfirmApi;
    public const string RequestPagePath = AdminSignedOutExemptions.ResetPage;
    public const string ConfirmPagePath = AdminSignedOutExemptions.ResetConfirmPage;
    public const string MailSubject = "Reset your Dealoware admin password";

    public static void MapAdminPasswordReset(this WebApplication app)
    {
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
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost(ConfirmPagePath, CompleteReset)
            .WithName("AdminPasswordResetConfirmPage")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> RequestReset(
        ResetRequestBody? body,
        IAdminPasswordResetTokenRepository tokens,
        IAdminMailSender mail,
        IAdminAuditRepository audit,
        IIpHasher ipHasher,
        IAdminClock clock,
        IOptions<CoreOwnerOptions> coreOwner,
        IOptions<AdminHostOptions> hostOptions,
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

            var link = BuildResetLink(hostOptions.Value, raw);
            await mail.SendAsync(
                new AdminMailMessage(ownerEmail, MailSubject, link),
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
        IIpHasher ipHasher,
        IAdminClock clock,
        IOptions<CoreOwnerOptions> coreOwner,
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
            return InvalidLinkResult();
        }

        var totp = NullIfWhiteSpace(body?.Totp);
        var recovery = NullIfWhiteSpace(body?.RecoveryCode);
        if (totp is null && recovery is null)
        {
            return CompleteFailedResult();
        }

        var factor = await secondFactor.VerifyAsync(
            ownerEmail,
            totp,
            recovery,
            http.RequestAborted);
        if (!factor.Succeeded)
        {
            return CompleteFailedResult();
        }

        var credential = await credentials.GetByEmailAsync(ownerEmail, http.RequestAborted);
        if (credential is null)
        {
            return CompleteFailedResult();
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

        if (!await tokens.TryConsumeAsync(hash!, now, http.RequestAborted))
        {
            return InvalidLinkResult();
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

        return Results.Json(new { message = AdminPasswordResetCopy.CompleteSucceeded });
    }

    private static IResult InvalidLinkResult()
        => Results.Json(
            new { error = AdminPasswordResetCopy.InvalidLink },
            statusCode: StatusCodes.Status401Unauthorized);

    private static IResult CompleteFailedResult()
        => Results.Json(
            new { error = AdminPasswordResetCopy.CompleteFailed },
            statusCode: StatusCodes.Status401Unauthorized);

    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string CreateRawToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string BuildResetLink(AdminHostOptions hosts, string rawToken)
    {
        var host = hosts.AllowedHosts is { Count: > 0 }
            ? hosts.AllowedHosts[0]
            : "admin.core.dealoware.com";
        return $"https://{host}{ConfirmPagePath}?token={Uri.EscapeDataString(rawToken)}";
    }

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
