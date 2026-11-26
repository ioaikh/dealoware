using System.Text.Json.Serialization;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// First-time setup (bootstrap) password-set request for the S-A4 screen,
/// at /admin/bootstrap and its JSON twin /admin/api/auth/bootstrap. The GET
/// takes no token and has no side effects; the token travels in the mail
/// fragment and the POST body only. No session is created here: the owner
/// signs in next and the existing gate sends them to TOTP enrolment.
/// </summary>
public static class AdminBootstrapEndpoints
{
    public const string PagePath = AdminSignedOutExemptions.Bootstrap;
    public const string ApiPath = AdminSignedOutExemptions.ApiBootstrap;

    public static void MapAdminBootstrap(this WebApplication app)
    {
        app.MapGet(PagePath, PageGet)
            .WithName("AdminBootstrapPageGet")
            .WithTags("Admin")
            .AllowAnonymous();

        app.MapPost(PagePath, SetPassword)
            .WithName("AdminBootstrapSetPasswordPage")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status303SeeOther)
            .Produces(StatusCodes.Status429TooManyRequests);

        app.MapPost(ApiPath, SetPassword)
            .WithName("AdminBootstrapSetPassword")
            .WithTags("Admin")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status303SeeOther)
            .Produces(StatusCodes.Status429TooManyRequests);
    }

    /// <summary>
    /// Until the S-A4 screen lands this answers like the other unbuilt
    /// signed-out pages: it only issues the anti-forgery token the POST needs.
    /// </summary>
    private static IResult PageGet(HttpContext http, AdminAntiForgeryService antiforgery)
    {
        AdminPasswordResetEndpoints.ApplyNoStoreNoReferrer(http);
        return antiforgery.IssuePage(http, "bootstrap");
    }

    private static async Task<IResult> SetPassword(
        BootstrapPasswordBody? body,
        AdminBootstrapService bootstrap,
        IAdminBootstrapTokenRepository tokens,
        IAdminCredentialStore credentials,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminPasswordHasher hasher,
        IAdminAuditRepository audit,
        IAdminResetIpThrottle throttle,
        ITurnstileVerifier turnstile,
        IIpHasher ipHasher,
        IAdminClock clock,
        IOptions<CoreOwnerOptions> coreOwner,
        DealowareDbContext db,
        HttpContext http)
    {
        AdminPasswordResetEndpoints.ApplyNoStoreNoReferrer(http);
        var now = clock.UtcNow;
        var ct = http.RequestAborted;
        var clientIp = AdminAuthService.NormalizeIp(http.Connection.RemoteIpAddress?.ToString());
        var ipHmac = ipHasher.Hash(clientIp);

        // A locked IP is refused first. This read counts nothing.
        if (await throttle.IsLockedAsync(ipHmac, now, ct))
        {
            return ThrottledResult();
        }

        // Turnstile before the link or the password is looked at. A reject
        // counts toward no counter (TD-ADM-040).
        if (!await turnstile.VerifyAsync(body?.TurnstileToken ?? string.Empty, clientIp, ct))
        {
            await audit.AddAsync(
                AdminAuditEntry.CreateAuthEvent(
                    AdminAuthEvents.BootstrapCompleteFailed,
                    "anonymous",
                    ipHmac,
                    AdminAuthReason.CaptchaFailed,
                    now),
                ct);
            await audit.SaveChangesAsync(ct);
            return Results.Json(
                new { error = AdminAuthCopy.VerificationFailed },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // Every submission past Turnstile takes one slot of the shared reset/bootstrap IP budget.
        if (!await throttle.TryReserveAsync(ipHmac, now, ct))
        {
            return ThrottledResult();
        }

        var ownerEmail = AdminAuthService.NormalizeEmail(coreOwner.Value.Email);
        var hash = AdminBootstrapToken.HashRaw(body?.Token?.Trim() ?? string.Empty);
        var row = await tokens.FindUsableByHashAsync(hash, now, ct);
        if (row is null
            || string.IsNullOrEmpty(ownerEmail)
            || !string.Equals(row.Email, ownerEmail, StringComparison.Ordinal)
            || await bootstrap.OwnerHasPasswordAsync(ct))
        {
            return await FailInvalidLinkAsync(audit, ipHmac, now, http);
        }

        var proposed = body?.NewPassword ?? string.Empty;
        var rules = AdminPasswordRules.Evaluate(proposed, ownerEmail, matchesCurrentPassword: false);
        if (!rules.Accepted)
        {
            return Results.Json(
                new { error = rules.Message, reason = AdminPasswordResetEndpoints.ReasonName(rules.Reason) },
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Hash before the transaction so the slow step holds no row lock.
        var passwordHash = hasher.Hash(AdminPasswordRules.Normalize(proposed));

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (!await tokens.TryConsumeAsync(hash, now, ct))
        {
            await tx.RollbackAsync(ct);
            return await FailInvalidLinkAsync(audit, ipHmac, now, http);
        }

        // Read the owner row again inside the transaction, dropping what the
        // check above tracked: a parallel request may have set the password
        // since. Bootstrap never replaces a password.
        db.ChangeTracker.Clear();
        var account = await accounts.GetByEmailAsync(ownerEmail, ct);
        if (account is not null && !string.IsNullOrEmpty(account.PasswordHash))
        {
            await tx.RollbackAsync(ct);
            return await FailInvalidLinkAsync(audit, ipHmac, now, http);
        }

        if (account is null)
        {
            account = AdminCoreOwnerAccount.Create(ownerEmail, now);
            await accounts.AddAsync(account, ct);
        }

        account.SetPasswordHash(passwordHash);
        await credentials.AddAsync(AdminCredential.Create(ownerEmail, passwordHash, now), ct);
        await audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent(AdminAuthEvents.BootstrapComplete, ownerEmail, ipHmac, timestamp: now),
            ct);

        try
        {
            await audit.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another request created the owner rows first (unique email).
            // Roll back so this link's consume and both rows go together.
            await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return await FailInvalidLinkAsync(audit, ipHmac, now, http);
        }

        return Results.Json(new { passwordSet = true, next = "sign-in" });
    }

    /// <summary>
    /// One answer for an unknown, expired, used or superseded link, so the
    /// reply does not reveal which case applied.
    /// </summary>
    private static async Task<IResult> FailInvalidLinkAsync(
        IAdminAuditRepository audit,
        string ipHmac,
        DateTimeOffset now,
        HttpContext http)
    {
        await audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent(
                AdminAuthEvents.BootstrapCompleteFailed,
                "anonymous",
                ipHmac,
                AdminAuthEvents.ReasonInvalidLink,
                now),
            http.RequestAborted);
        await audit.SaveChangesAsync(http.RequestAborted);

        http.Response.Headers.Location = AdminPasswordResetEndpoints.LinkExpiredSetupLocation;
        return Results.StatusCode(StatusCodes.Status303SeeOther);
    }

    /// <summary>Same reply as a throttled reset link: generic sign-in text, no Retry-After.</summary>
    private static IResult ThrottledResult()
        => Results.Json(
            new { message = AdminPasswordResetCopy.SignInGeneric },
            statusCode: StatusCodes.Status429TooManyRequests);

    public sealed class BootstrapPasswordBody
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("newPassword")]
        public string? NewPassword { get; set; }

        [JsonPropertyName("turnstileToken")]
        public string? TurnstileToken { get; set; }
    }
}
