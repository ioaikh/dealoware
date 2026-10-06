using System.Text;
using System.Text.Json;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Two-step CoreOwner sign-in and TOTP enrollment. JSON only on r3 §2.2 paths.
/// No HTML pages (Step 14). Session stays unverified until TOTP or a recovery code passes.
/// </summary>
public static class AdminAuthEndpoints
{
    public static void MapAdminAuthEndpoints(this WebApplication app)
    {
        app.MapGet(AdminSignedOutExemptions.SignIn, GetSignInPage)
            .WithTags("AdminAuth")
            .AllowAnonymous();
        app.MapPost(AdminSignedOutExemptions.SignIn, SignIn)
            .WithTags("AdminAuth")
            .AllowAnonymous();
        app.MapPost(AdminSignedOutExemptions.ApiSignIn, SignIn)
            .WithTags("AdminAuth")
            .AllowAnonymous();

        app.MapGet(AdminSignedOutExemptions.SignInCode, GetSignInCodePage)
            .WithTags("AdminAuth")
            .AllowAnonymous();
        app.MapPost(AdminSignedOutExemptions.SignInCode, VerifyTotp)
            .WithTags("AdminAuth")
            .AllowAnonymous();
        app.MapPost(AdminSignedOutExemptions.ApiSignInCode, VerifyTotp)
            .WithTags("AdminAuth")
            .AllowAnonymous();

        app.MapGet(AdminSignedOutExemptions.SignInRecovery, GetSignInRecoveryPage)
            .WithTags("AdminAuth")
            .AllowAnonymous();
        app.MapPost(AdminSignedOutExemptions.SignInRecovery, VerifyRecovery)
            .WithTags("AdminAuth")
            .AllowAnonymous();
        app.MapPost(AdminSignedOutExemptions.ApiSignInRecovery, VerifyRecovery)
            .WithTags("AdminAuth")
            .AllowAnonymous();

        app.MapGet(AdminSignedOutExemptions.SetupAuthenticator, GetSetupAuthenticator)
            .WithTags("AdminAuth")
            .AllowAnonymous();
        app.MapPost(AdminSignedOutExemptions.SetupAuthenticator, PostSetupAuthenticator)
            .WithTags("AdminAuth")
            .AllowAnonymous();
        app.MapGet(AdminSignedOutExemptions.SetupRecoveryCodes, GetSetupRecoveryCodes)
            .WithTags("AdminAuth")
            .AllowAnonymous();

        app.MapPost(AdminSignedOutExemptions.SignOut, SignOut)
            .WithTags("AdminAuth")
            .AllowAnonymous();
        app.MapPost(AdminSignedOutExemptions.ApiSignOut, SignOut)
            .WithTags("AdminAuth")
            .AllowAnonymous();
    }

    private sealed record SignInRequest(string? Email, string? Password, string? ReturnPath);

    private sealed record TotpCodeRequest(string? Code, string? RecoveryCode);

    private static IResult GetSignInPage(HttpContext context, IAntiforgery antiforgery)
        => AdminAntiForgeryCookie.IssuePage(context, antiforgery, "sign-in");

    private static IResult GetSignInCodePage(HttpContext context, IAntiforgery antiforgery)
        => AdminAntiForgeryCookie.IssuePage(context, antiforgery, "sign-in-code");

    private static IResult GetSignInRecoveryPage(HttpContext context, IAntiforgery antiforgery)
        => AdminAntiForgeryCookie.IssuePage(context, antiforgery, "sign-in-recovery");

    private static async Task<IResult> SignIn(
        [FromBody] SignInRequest? body,
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminAuditRepository audit,
        IIpHasher ipHasher,
        TotpSecretProtector protector,
        IOptions<CoreOwnerOptions> coreOwnerOptions)
    {
        var ownerEmail = coreOwnerOptions.Value.Email;
        var submittedEmail = (body?.Email ?? string.Empty).Trim();
        var password = body?.Password ?? string.Empty;
        var now = DateTimeOffset.UtcNow;
        var ipHmac = HashIp(ipHasher, context);
        var returnPath = AdminReturnPath.Resolve(body?.ReturnPath);

        var isOwner = !string.IsNullOrWhiteSpace(ownerEmail)
                      && string.Equals(submittedEmail, ownerEmail, StringComparison.OrdinalIgnoreCase);

        AdminCoreOwnerAccount? account = null;
        if (isOwner)
        {
            account = await accounts.GetByEmailAsync(ownerEmail, context.RequestAborted);
        }

        var passwordOk = account is not null && !string.IsNullOrEmpty(account.PasswordHash)
            ? AdminPasswordHasher.Verify(password, account.PasswordHash)
            : AdminPasswordHasher.DummyVerify(password);

        if (!isOwner || account is null || !passwordOk)
        {
            await WriteAuthEventAsync(
                audit,
                AdminAuthEvents.LoginFailure,
                isOwner ? ownerEmail : "unknown",
                ipHmac,
                AdminAuthEvents.ReasonBadPassword,
                context.RequestAborted);
            return AdminSignInDeny.Failure();
        }

        await accounts.ConsumeOutstandingPendingAsync(ownerEmail, now, context.RequestAborted);
        account.ClearRecoveryCodesReveal();

        if (!account.IsTotpEnrolled && string.IsNullOrEmpty(account.PendingTotpSecretCipher))
        {
            var secret = Rfc6238Totp.GenerateSecret();
            account.SetPendingEnrollment(protector.Encrypt(secret));
            CryptographicClear(secret);
        }

        var rawToken = AdminPendingToken.Create();
        var pending = AdminPendingAuth.Create(ownerEmail, AdminPendingToken.Hash(rawToken), now, returnPath);
        await accounts.AddPendingAsync(pending, context.RequestAborted);
        await accounts.SaveChangesAsync(context.RequestAborted);

        context.Response.Cookies.Append(AdminPendingAuthCookie.Name, rawToken, AdminPendingAuthCookie.CreateOptions());
        return Results.Json(new
        {
            next = account.IsTotpEnrolled ? "totp" : "enroll"
        });
    }

    private static async Task<IResult> GetSetupAuthenticator(
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        IAntiforgery antiforgery,
        TotpSecretProtector protector)
    {
        SetNoStore(context);
        AdminAntiForgeryCookie.IssuePage(context, antiforgery, "setup-authenticator");

        var pending = await LoadUsablePendingAsync(context, accounts);
        if (pending is null)
            return AdminSignInDeny.Failure();

        var account = await accounts.GetByEmailAsync(pending.Email, context.RequestAborted);
        if (account is null)
            return AdminSignInDeny.Failure();

        return ReadEnrollmentSecret(account, protector);
    }

    private static async Task<IResult> PostSetupAuthenticator(
        [FromBody] TotpCodeRequest? body,
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminAuditRepository audit,
        IIpHasher ipHasher,
        TotpSecretProtector protector)
    {
        SetNoStore(context);
        var pending = await LoadUsablePendingAsync(context, accounts);
        if (pending is null)
            return AdminSignInDeny.Failure();

        var account = await accounts.GetByEmailAsync(pending.Email, context.RequestAborted);
        if (account is null)
            return AdminSignInDeny.Failure();

        if (string.IsNullOrWhiteSpace(body?.Code))
            return ReadEnrollmentSecret(account, protector);

        return await EnrollConfirmAsync(body, account, pending, context, accounts, audit, ipHasher, protector);
    }

    private static IResult ReadEnrollmentSecret(AdminCoreOwnerAccount account, TotpSecretProtector protector)
    {
        if (account.IsTotpEnrolled)
            return Results.Json(new { enrolled = true });

        if (string.IsNullOrEmpty(account.PendingTotpSecretCipher))
            return AdminSignInDeny.Failure();

        byte[] secret;
        try
        {
            secret = protector.Decrypt(account.PendingTotpSecretCipher);
        }
        catch
        {
            return AdminSignInDeny.Failure();
        }

        var base32 = Rfc6238Totp.ToBase32(secret);
        CryptographicClear(secret);

        return Results.Json(new
        {
            secret = base32,
            otpauthUrl = Rfc6238Totp.BuildOtpAuthUri(account.Email, base32),
            digits = Rfc6238Totp.Digits,
            period = Rfc6238Totp.PeriodSeconds
        });
    }

    private static async Task<IResult> EnrollConfirmAsync(
        TotpCodeRequest body,
        AdminCoreOwnerAccount account,
        AdminPendingAuth pending,
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminAuditRepository audit,
        IIpHasher ipHasher,
        TotpSecretProtector protector)
    {
        if (string.IsNullOrEmpty(account.PendingTotpSecretCipher))
            return AdminSignInDeny.Failure();

        byte[] secret;
        try
        {
            secret = protector.Decrypt(account.PendingTotpSecretCipher);
        }
        catch
        {
            return AdminSignInDeny.Failure();
        }

        var now = DateTimeOffset.UtcNow;
        if (!Rfc6238Totp.TryVerify(secret, body.Code ?? string.Empty, now, lastUsedTimestep: null, out var step))
        {
            CryptographicClear(secret);
            pending.IncrementFailedCodeAttempt(now);
            await accounts.SaveChangesAsync(context.RequestAborted);
            await WriteAuthEventAsync(
                audit,
                AdminAuthEvents.SecondFactorFailed,
                account.Email,
                HashIp(ipHasher, context),
                AdminAuthEvents.ReasonBad2Fa,
                context.RequestAborted);
            return AdminSignInDeny.Failure();
        }

        var plaintextCodes = AdminRecoveryCodes.Generate();
        var hashed = plaintextCodes
            .Select(code => AdminRecoveryCode.Create(account.Id, AdminRecoveryCodes.Hash(code)))
            .ToList();
        await accounts.AddRecoveryCodesAsync(hashed, context.RequestAborted);

        var revealJson = JsonSerializer.Serialize(plaintextCodes);
        var revealCipher = protector.Encrypt(Encoding.UTF8.GetBytes(revealJson));
        account.CompleteEnrollment(protector.Encrypt(secret), revealCipher, now);
        account.MarkRecoveryCodesIssued();
        account.RecordTotpTimestep(step);
        CryptographicClear(secret);
        await accounts.SaveChangesAsync(context.RequestAborted);
        await WriteAuthEventAsync(
            audit,
            AdminAuthEvents.TotpEnroll,
            account.Email,
            HashIp(ipHasher, context),
            reasonClass: null,
            context.RequestAborted);
        return Results.Json(new { enrolled = true, next = "recovery-codes" });
    }

    private static async Task<IResult> GetSetupRecoveryCodes(
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        TotpSecretProtector protector)
    {
        SetNoStore(context);
        var pending = await LoadUsablePendingAsync(context, accounts);
        if (pending is null)
            return AdminSignInDeny.Failure();

        var account = await accounts.GetByEmailAsync(pending.Email, context.RequestAborted);
        if (account is null || !account.IsTotpEnrolled)
            return AdminSignInDeny.Failure();

        // C3: GET is side-effect free. Reveal cipher is cleared when pending is
        // replaced or consumed, so codes are not re-shown after the enrol window.
        var cipher = account.RecoveryCodesRevealCipher;
        if (string.IsNullOrEmpty(cipher))
            return Results.Json(new { enrolled = true });

        try
        {
            var json = Encoding.UTF8.GetString(protector.Decrypt(cipher));
            var codes = JsonSerializer.Deserialize<string[]>(json) ?? [];
            return Results.Json(new { recoveryCodes = codes });
        }
        catch
        {
            return AdminSignInDeny.Failure();
        }
    }

    private static Task<IResult> VerifyTotp(
        [FromBody] TotpCodeRequest? body,
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminSessionRepository sessions,
        IAdminAuditRepository audit,
        IIpHasher ipHasher,
        TotpSecretProtector protector)
        => CompleteSecondFactorAsync(
            body,
            useRecovery: false,
            context,
            accounts,
            sessions,
            audit,
            ipHasher,
            protector);

    private static Task<IResult> VerifyRecovery(
        [FromBody] TotpCodeRequest? body,
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminSessionRepository sessions,
        IAdminAuditRepository audit,
        IIpHasher ipHasher,
        TotpSecretProtector protector)
        => CompleteSecondFactorAsync(
            body,
            useRecovery: true,
            context,
            accounts,
            sessions,
            audit,
            ipHasher,
            protector);

    private static async Task<IResult> CompleteSecondFactorAsync(
        TotpCodeRequest? body,
        bool useRecovery,
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        IAdminSessionRepository sessions,
        IAdminAuditRepository audit,
        IIpHasher ipHasher,
        TotpSecretProtector protector)
    {
        var pending = await LoadUsablePendingAsync(context, accounts);
        if (pending is null)
            return AdminSignInDeny.Failure();

        var account = await accounts.GetByEmailAsync(pending.Email, context.RequestAborted);
        if (account is null || !account.IsTotpEnrolled || string.IsNullOrEmpty(account.TotpSecretCipher))
            return AdminSignInDeny.Failure();

        var now = DateTimeOffset.UtcNow;
        var ipHmac = HashIp(ipHasher, context);
        var accepted = false;
        var usedRecovery = false;

        if (!useRecovery)
        {
            byte[] secret;
            try
            {
                secret = protector.Decrypt(account.TotpSecretCipher);
            }
            catch
            {
                return AdminSignInDeny.Failure();
            }

            if (Rfc6238Totp.TryVerify(secret, body?.Code ?? string.Empty, now, account.LastUsedTotpTimestep, out var step))
            {
                account.RecordTotpTimestep(step);
                accepted = true;
            }

            CryptographicClear(secret);
        }
        else
        {
            var presentedHash = AdminRecoveryCodes.Hash(body?.RecoveryCode ?? body?.Code ?? string.Empty);
            var codes = await accounts.GetRecoveryCodesAsync(account.Id, context.RequestAborted);
            foreach (var stored in codes)
            {
                if (stored.IsUsed || !AdminRecoveryCodes.FixedTimeEquals(stored.CodeHash, presentedHash))
                    continue;

                if (stored.TryMarkUsed(now))
                {
                    accepted = true;
                    usedRecovery = true;
                }

                break;
            }
        }

        if (!accepted)
        {
            pending.IncrementFailedCodeAttempt(now);
            await accounts.SaveChangesAsync(context.RequestAborted);
            await WriteAuthEventAsync(
                audit,
                AdminAuthEvents.SecondFactorFailed,
                account.Email,
                ipHmac,
                AdminAuthEvents.ReasonBad2Fa,
                context.RequestAborted);
            await WriteAuthEventAsync(
                audit,
                AdminAuthEvents.LoginFailure,
                account.Email,
                ipHmac,
                AdminAuthEvents.ReasonBad2Fa,
                context.RequestAborted);
            return AdminSignInDeny.Failure();
        }

        pending.Consume(now);
        account.ClearRecoveryCodesReveal();
        await accounts.SaveChangesAsync(context.RequestAborted);

        var session = AdminSession.Create(account.Email, ipHmac, now);
        session.MarkTotpVerified();
        await sessions.AddAsync(session, context.RequestAborted);
        await sessions.SaveChangesAsync(context.RequestAborted);

        if (usedRecovery)
        {
            await WriteAuthEventAsync(
                audit,
                AdminAuthEvents.RecoveryCodeUse,
                account.Email,
                ipHmac,
                reasonClass: null,
                context.RequestAborted);
        }

        await WriteAuthEventAsync(
            audit,
            AdminAuthEvents.LoginSuccess,
            account.Email,
            ipHmac,
            reasonClass: null,
            context.RequestAborted);

        context.Response.Cookies.Delete(AdminPendingAuthCookie.Name, AdminPendingAuthCookie.ExpiredOptions());
        context.Response.Cookies.Append(
            AdminSessionCookie.Name,
            session.Id.ToString("D"),
            AdminSessionCookie.CreateOptions());

        return Results.Json(new { verified = true, next = pending.ReturnPath });
    }

    private static async Task<IResult> SignOut(
        HttpContext context,
        IAdminSessionRepository sessions)
    {
        if (context.Request.Cookies.TryGetValue(AdminSessionCookie.Name, out var raw)
            && Guid.TryParse(raw, out var sessionId))
        {
            await sessions.DeleteAsync(sessionId, context.RequestAborted);
            await sessions.SaveChangesAsync(context.RequestAborted);
        }

        context.Response.Cookies.Delete(AdminSessionCookie.Name, AdminSessionCookie.ExpiredOptions());
        context.Response.Cookies.Delete(AdminPendingAuthCookie.Name, AdminPendingAuthCookie.ExpiredOptions());
        context.Response.Cookies.Delete(AdminAntiForgeryCookie.Name, AdminAntiForgeryCookie.ExpiredOptions());
        return Results.Json(new { signedOut = true });
    }

    private static async Task<AdminPendingAuth?> LoadUsablePendingAsync(
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts)
    {
        if (!context.Request.Cookies.TryGetValue(AdminPendingAuthCookie.Name, out var raw)
            || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var pending = await accounts.GetPendingByTokenHashAsync(
            AdminPendingToken.Hash(raw),
            context.RequestAborted);
        return pending is not null && pending.IsUsable() ? pending : null;
    }

    private static string HashIp(IIpHasher ipHasher, HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ip))
            ip = "unknown";
        return ipHasher.Hash(ip);
    }

    private static async Task WriteAuthEventAsync(
        IAdminAuditRepository audit,
        string action,
        string actorEmail,
        string ipHmac,
        string? reasonClass,
        CancellationToken cancellationToken)
    {
        await audit.AddAsync(
            AdminAuditEntry.CreateAuthEvent(action, actorEmail, ipHmac, reasonClass),
            cancellationToken);
        await audit.SaveChangesAsync(cancellationToken);
    }

    private static void SetNoStore(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
    }

    private static void CryptographicClear(byte[] secret)
        => System.Security.Cryptography.CryptographicOperations.ZeroMemory(secret);
}
