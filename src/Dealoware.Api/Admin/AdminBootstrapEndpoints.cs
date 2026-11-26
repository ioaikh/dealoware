using System.Text.Json.Serialization;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// First-time setup (bootstrap): the S-A4 screen at /admin/bootstrap and its
/// password-set request there and at the JSON twin /admin/api/auth/bootstrap.
/// The GET takes no token and has no side effects; the token travels in the
/// mail fragment and the POST body only. No session is created here: the owner
/// signs in next and the existing gate sends them to TOTP enrolment.
/// The same POST also answers the page's link pre-check (<c>check: true</c>):
/// "is this link usable?", with no password, no security check and no change.
/// </summary>
public static class AdminBootstrapEndpoints
{
    public const string PagePath = AdminSignedOutExemptions.Bootstrap;
    public const string ApiPath = AdminSignedOutExemptions.ApiBootstrap;
    public const string CheckLogCategory = "Dealoware.Api.Admin.BootstrapCheck";

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
    /// The S-A4 screen. It takes no token and changes nothing: the link token
    /// stays in the URL fragment, which the browser never sends, and the page
    /// script posts it in the body. Everything but the heading is hidden until
    /// that script runs, so with JavaScript off only the heading and one
    /// sentence show. The script shows the form only after the server has said
    /// the link is usable. The submit button is disabled in the markup and
    /// only a fresh Turnstile token enables it; with no site key configured
    /// nothing can.
    /// </summary>
    private static IResult PageGet(
        HttpContext http,
        AdminAntiForgeryService antiforgery,
        IOptions<TurnstileOptions> turnstile)
    {
        var widget = turnstile.Value;
        return AdminSignedOutPage.Render(http, antiforgery, new AdminSignedOutPageContent
        {
            Title = AdminBootstrapCopy.PageTitle,
            Heading = AdminBootstrapCopy.Heading,
            Noscript = AdminBootstrapCopy.Noscript,
            BodyHtml = PageBody(widget),
            Scripts =
            [
                // First, so the link token leaves the address bar even if a later script fails to load.
                AdminSignedOutPage.LinkScriptPath,
                AdminSignedOutPage.DomScriptPath,
                AdminSignedOutPage.PasswordScriptPath,
                AdminSignedOutPage.TurnstileScriptPath,
                AdminBootstrapCopy.ScriptPath
            ],
            IssueAntiForgery = true,
            AllowTurnstile = widget.ConfiguredSiteKey() is not null
        });
    }

    /// <summary>
    /// Password rules and their messages are the server's own
    /// (<see cref="AdminPasswordRules"/>), passed to the script as data
    /// attributes. Confirm password has no name: it is compared in the
    /// browser and never sent.
    /// </summary>
    private static string PageBody(TurnstileOptions turnstile)
    {
        static string E(string value) => AdminSignedOutPage.Encode(value);

        return
            // Link pre-check states, as on the reset page: checking, then either the form or "try again".
            // The status text is filled in by the script once the region is on screen, so it is announced.
            $"<p id=\"link-status\" role=\"status\" hidden data-checking=\"{E(AdminPasswordResetCopy.CheckingLink)}\"></p>" +
            $"<p id=\"link-alert\" hidden tabindex=\"-1\" data-retry=\"{E(AdminPasswordResetCopy.CheckRetry)}\"></p>" +
            $"<button type=\"button\" id=\"link-retry\" class=\"secondary\" hidden" +
            $" data-busy=\"{E(AdminSignedOutCopy.Checking)}\">{E(AdminPasswordResetCopy.TryAgain)}</button>" +
            "<div id=\"bootstrap-panel\" hidden>" +
            $"<p class=\"lede\">{E(AdminBootstrapCopy.Lede)}</p>" +
            "<div id=\"form-alert\" class=\"alert\" role=\"alert\" tabindex=\"-1\" hidden></div>" +
            $"<form id=\"bootstrap-form\" method=\"post\" action=\"{PagePath}\" novalidate" +
            $" data-retry=\"{E(AdminBootstrapCopy.SubmitRetry)}\">" +
            "<div class=\"field\">" +
            $"<label id=\"new-password-label\" for=\"new-password\">{E(AdminBootstrapCopy.NewPasswordLabel)}</label>" +
            "<div class=\"field-row\">" +
            "<input id=\"new-password\" name=\"newPassword\" type=\"password\" autocomplete=\"new-password\" required" +
            " aria-describedby=\"password-helper new-password-error\"" +
            $" data-min-length=\"{AdminPasswordRules.MinCodePoints}\"" +
            $" data-max-length=\"{AdminPasswordRules.MaxCodePoints}\"" +
            $" data-too-short=\"{E(AdminPasswordRules.TooShortMessage)}\"" +
            $" data-too-long=\"{E(AdminPasswordRules.TooLongMessage)}\">" +
            ToggleButton("new-password") +
            "</div>" +
            $"<span id=\"password-helper\" class=\"helper\">{E(AdminSignedOutCopy.PasswordHelper)}</span>" +
            "<span id=\"new-password-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
            "</div>" +
            "<div class=\"field\">" +
            $"<label id=\"confirm-password-label\" for=\"confirm-password\">{E(AdminBootstrapCopy.ConfirmPasswordLabel)}</label>" +
            "<div class=\"field-row\">" +
            "<input id=\"confirm-password\" type=\"password\" autocomplete=\"new-password\" required" +
            " aria-describedby=\"confirm-password-error\"" +
            $" data-mismatch=\"{E(AdminSignedOutCopy.PasswordMismatch)}\">" +
            ToggleButton("confirm-password") +
            "</div>" +
            "<span id=\"confirm-password-error\" class=\"field-error\" aria-live=\"polite\"></span>" +
            "</div>" +
            AdminSignedOutPage.TurnstileBlock(turnstile) +
            "<button type=\"submit\" id=\"bootstrap-submit\" disabled aria-describedby=\"security-check-status\">" +
            $"{E(AdminBootstrapCopy.Submit)}</button>" +
            "</form></div>";
    }

    /// <summary>
    /// The show toggle for one password field. Its label never changes: it is
    /// "Show", named after the field ("Show New password"), and
    /// <c>aria-pressed</c> alone says whether the password is shown.
    /// </summary>
    private static string ToggleButton(string inputId)
        => $"<button type=\"button\" id=\"{inputId}-toggle\" class=\"secondary toggle\" aria-pressed=\"false\"" +
           $" aria-controls=\"{inputId}\" aria-labelledby=\"{inputId}-toggle {inputId}-label\">" +
           $"{AdminSignedOutPage.Encode(AdminSignedOutCopy.ShowPassword)}</button>";

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
        ILoggerFactory loggers,
        DealowareDbContext db,
        HttpContext http)
    {
        AdminPasswordResetEndpoints.ApplyNoStoreNoReferrer(http);
        var now = clock.UtcNow;
        var ct = http.RequestAborted;
        var clientIp = AdminAuthService.NormalizeIp(http.Connection.RemoteIpAddress?.ToString());
        var ipHmac = ipHasher.Hash(clientIp);
        var ownerEmail = AdminAuthService.NormalizeEmail(coreOwner.Value.Email);
        var hash = AdminBootstrapToken.HashRaw(body?.Token?.Trim() ?? string.Empty);

        // A locked IP is refused first. This read counts nothing.
        if (await throttle.IsLockedAsync(ipHmac, now, ct))
        {
            return ThrottledResult();
        }

        // The page's link pre-check: "check": true asks only whether the link
        // is usable, so a dead link never shows the password form. It reads
        // no password and needs no Turnstile token (the widget is not on
        // screen yet), and it changes nothing: the link is not consumed.
        // It is not free, though. It takes one slot of the same IP budget as
        // a submission, so links cannot be probed without being throttled.
        // Like the reset pre-check it writes no audit row, only the outcome
        // class to the log, never the token.
        if (body?.Check == true)
        {
            if (!await throttle.TryReserveAsync(ipHmac, now, ct))
            {
                return ThrottledResult();
            }

            var usable = await IsUsableLinkAsync(hash, ownerEmail, tokens, bootstrap, now, ct);
            loggers.CreateLogger(CheckLogCategory)
                .LogInformation("Admin bootstrap check: {OutcomeClass}", usable ? "valid" : "invalid");
            if (!usable)
            {
                return InvalidLinkResult(http);
            }

            return Results.Json(new { valid = true });
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

        if (!await IsUsableLinkAsync(hash, ownerEmail, tokens, bootstrap, now, ct))
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

        // Read the owner row again inside the transaction, dropping what the
        // check above tracked: a parallel request may have set the password
        // since. Bootstrap never replaces a password.
        db.ChangeTracker.Clear();
        var account = await accounts.GetByEmailAsync(ownerEmail, ct);
        if (account is not null && !string.IsNullOrEmpty(account.PasswordHash))
        {
            return await RollBackAsInvalidLinkAsync(tx, db, audit, ipHmac, now, http);
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

        // The owner rows are written before any link row is touched: their
        // unique email is where parallel requests queue, so two requests
        // holding different links cannot wait on each other's link rows.
        try
        {
            await audit.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsUniqueViolation(ex))
        {
            // Another request created the owner rows first. Any other database
            // fault is not a bad link: it propagates and the transaction rolls back.
            return await RollBackAsInvalidLinkAsync(tx, db, audit, ipHmac, now, http);
        }

        if (!await tokens.TryConsumeAsync(hash, now, ct))
        {
            return await RollBackAsInvalidLinkAsync(tx, db, audit, ipHmac, now, http);
        }

        // Setup is complete: no other link may stay usable.
        await tokens.InvalidateUnusedAsync(now, ct);
        await tx.CommitAsync(ct);

        return Results.Json(new { passwordSet = true, next = "sign-in" });
    }

    /// <summary>
    /// One rule for the pre-check and the submission: the link is unused and
    /// unexpired, it was issued for the configured owner, and the owner has no
    /// password yet. Reads only.
    /// </summary>
    private static async Task<bool> IsUsableLinkAsync(
        string tokenHash,
        string ownerEmail,
        IAdminBootstrapTokenRepository tokens,
        AdminBootstrapService bootstrap,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var row = await tokens.FindUsableByHashAsync(tokenHash, now, ct);
        return row is not null
               && !string.IsNullOrEmpty(ownerEmail)
               && string.Equals(row.Email, ownerEmail, StringComparison.Ordinal)
               && !await bootstrap.OwnerHasPasswordAsync(ct);
    }

    /// <summary>Undoes the owner rows and the link consume together, then gives the one invalid-link answer.</summary>
    private static async Task<IResult> RollBackAsInvalidLinkAsync(
        IDbContextTransaction tx,
        DealowareDbContext db,
        IAdminAuditRepository audit,
        string ipHmac,
        DateTimeOffset now,
        HttpContext http)
    {
        await tx.RollbackAsync(http.RequestAborted);
        db.ChangeTracker.Clear();
        return await FailInvalidLinkAsync(audit, ipHmac, now, http);
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

        return InvalidLinkResult(http);
    }

    /// <summary>The one invalid-link reply: 303 to the link-expired page, with no body.</summary>
    private static IResult InvalidLinkResult(HttpContext http)
    {
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

        /// <summary>True for the link pre-check: only <see cref="Token"/> is read and nothing changes.</summary>
        [JsonPropertyName("check")]
        public bool? Check { get; set; }
    }
}
