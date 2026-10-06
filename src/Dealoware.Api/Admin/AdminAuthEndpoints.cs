using System.Text.Json;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.AspNetCore.Antiforgery;

namespace Dealoware.Api.Admin;

/// <summary>
/// Admin auth API under the exact r2 §2.2 paths. No pages in this PR.
/// </summary>
public static class AdminAuthEndpoints
{
    public const string SignInPath = "/admin/api/auth/sign-in";
    public const string SignInCodePath = "/admin/api/auth/sign-in/code";
    public const string SignInRecoveryPath = "/admin/api/auth/sign-in/recovery";
    public const string ResetPath = "/admin/api/auth/reset";
    public const string ResetConfirmPath = "/admin/api/auth/reset/confirm";
    public const string BootstrapPath = "/admin/api/auth/bootstrap";
    public const string SignOutPath = "/admin/api/auth/sign-out";
    public const string PageSignOutPath = "/admin/sign-out";

    public static void MapAdminAuth(this WebApplication app)
    {
        app.MapGet("/admin/sign-in", IssueAntiForgery).WithTags("AdminAuth").AllowAnonymous();
        app.MapGet("/admin/reset", IssueAntiForgery).WithTags("AdminAuth").AllowAnonymous();
        app.MapGet("/admin/reset/sent", IssueAntiForgery).WithTags("AdminAuth").AllowAnonymous();
        app.MapGet("/admin/reset/confirm", IssueAntiForgery).WithTags("AdminAuth").AllowAnonymous();
        app.MapGet("/admin/bootstrap", IssueAntiForgery).WithTags("AdminAuth").AllowAnonymous();
        app.MapGet("/admin/link-expired", IssueAntiForgery).WithTags("AdminAuth").AllowAnonymous();
        app.MapGet("/admin/sign-in/code", IssueAntiForgery).WithTags("AdminAuth").AllowAnonymous();
        app.MapGet("/admin/sign-in/recovery", IssueAntiForgery).WithTags("AdminAuth").AllowAnonymous();
        app.MapPost(SignInPath, SignIn).WithTags("AdminAuth").AllowAnonymous();
        app.MapPost(SignInCodePath, VerifyTotp).WithTags("AdminAuth").AllowAnonymous();
        app.MapPost(SignInRecoveryPath, VerifyRecovery).WithTags("AdminAuth").AllowAnonymous();
        app.MapPost(ResetPath, RequestReset).WithTags("AdminAuth").AllowAnonymous();
        app.MapPost(ResetConfirmPath, CompleteReset).WithTags("AdminAuth").AllowAnonymous();
        app.MapPost(BootstrapPath, CompleteBootstrap).WithTags("AdminAuth").AllowAnonymous();
        app.MapPost(SignOutPath, SignOut).WithTags("AdminAuth").AllowAnonymous();
        app.MapPost(PageSignOutPath, SignOut).WithTags("AdminAuth").AllowAnonymous();
    }

    public static IServiceCollection AddAdminAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TurnstileOptions>(configuration.GetSection(TurnstileOptions.SectionName));

        var coreOwnerEmail = configuration[$"{CoreOwnerOptions.SectionName}:Email"] ?? "io@aiknowhow.com";
        var directory = new InMemoryAdminCredentialDirectory(coreOwnerEmail);
        services.AddSingleton(directory);
        services.AddSingleton<IAdminCredentialDirectory>(directory);
        services.AddScoped<IAdminLockoutStore, AdminLockoutStore>();
        services.AddScoped<IAdminAuthTokenStore, AdminAuthTokenStore>();
        services.AddSingleton<IAdminClock, SystemAdminClock>();
        services.AddScoped<AdminAuthService>();

        services.AddAntiforgery(AdminAntiForgery.Configure);

        services.AddHttpClient(HttpTurnstileVerifier.HttpClientName);
        services.AddSingleton<ITurnstileVerifier>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TurnstileOptions>>();
            return new HttpTurnstileVerifier(
                factory.CreateClient(HttpTurnstileVerifier.HttpClientName),
                options,
                name => configuration[name] ?? Environment.GetEnvironmentVariable(name));
        });

        return services;
    }

    private static async Task WriteOutcome(HttpContext context, AdminAuthOutcome outcome)
    {
        if (outcome.PendingToken is not null)
        {
            context.Response.Cookies.Append(
                AdminSessionExemptions.PendingCookieName,
                outcome.PendingToken,
                AdminSessionCookie.CreateOptions());
        }

        if (outcome.SessionId is { } sessionId)
        {
            ClearCookie(context, AdminSessionExemptions.PendingCookieName);
            context.Response.Cookies.Append(
                AdminSessionCookie.Name,
                sessionId.ToString("D"),
                AdminSessionCookie.CreateOptions());
        }

        context.Response.StatusCode = outcome.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = outcome.Body }));
    }

    private static string? ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();

    private static async Task SignIn(HttpContext context, AdminAuthService auth, AdminAuthRequest body)
    {
        await WriteOutcome(context, await auth.SignInAsync(
            body.Email, body.Password, body.TurnstileToken, ClientIp(context), context.RequestAborted));
    }

    private static IResult IssueAntiForgery(HttpContext context, IAntiforgery antiforgery)
        => AdminAntiForgery.IssueTokens(context, antiforgery);

    private static async Task VerifyTotp(HttpContext context, AdminAuthService auth, AdminAuthRequest body)
    {
        var pending = context.Request.Cookies[AdminSessionExemptions.PendingCookieName];
        await WriteOutcome(context, await auth.VerifySecondFactorAsync(
            pending, body.TotpCode, recoveryCode: null, ClientIp(context), context.RequestAborted));
    }

    private static async Task VerifyRecovery(HttpContext context, AdminAuthService auth, AdminAuthRequest body)
    {
        var pending = context.Request.Cookies[AdminSessionExemptions.PendingCookieName];
        await WriteOutcome(context, await auth.VerifySecondFactorAsync(
            pending, totpCode: null, body.RecoveryCode, ClientIp(context), context.RequestAborted));
    }

    private static async Task RequestReset(HttpContext context, AdminAuthService auth, AdminAuthRequest body)
    {
        await WriteOutcome(context, await auth.RequestResetAsync(
            body.Email, body.TurnstileToken, ClientIp(context), context.RequestAborted));
    }

    private static async Task CompleteReset(HttpContext context, AdminAuthService auth, AdminAuthRequest body)
    {
        await WriteOutcome(context, await auth.CompleteLinkAsync(
            AdminAuthToken.KindReset,
            body.EffectiveLinkToken,
            body.Password,
            body.Email,
            body.TurnstileToken,
            ClientIp(context),
            context.RequestAborted));
    }

    private static async Task CompleteBootstrap(HttpContext context, AdminAuthService auth, AdminAuthRequest body)
    {
        await WriteOutcome(context, await auth.CompleteLinkAsync(
            AdminAuthToken.KindBootstrap,
            body.EffectiveLinkToken,
            body.Password,
            body.Email,
            body.TurnstileToken,
            ClientIp(context),
            context.RequestAborted));
    }

    private static Task SignOut(HttpContext context, IAdminSessionRepository sessions)
    {
        if (Guid.TryParse(context.Request.Cookies[AdminSessionCookie.Name], out var sessionId))
        {
            return ClearSessionAsync(context, sessions, sessionId);
        }

        ClearAuthCookies(context);
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return Task.CompletedTask;
    }

    private static async Task ClearSessionAsync(
        HttpContext context,
        IAdminSessionRepository sessions,
        Guid sessionId)
    {
        await sessions.DeleteAsync(sessionId, context.RequestAborted);
        await sessions.SaveChangesAsync(context.RequestAborted);
        ClearAuthCookies(context);
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private static void ClearAuthCookies(HttpContext context)
    {
        ClearCookie(context, AdminSessionCookie.Name);
        ClearCookie(context, AdminSessionExemptions.PendingCookieName);
        ClearCookie(context, AdminAntiForgery.CookieName);
    }

    private static void ClearCookie(HttpContext context, string name)
    {
        var options = AdminSessionCookie.CreateClearOptions();
        context.Response.Cookies.Append(name, string.Empty, options);
    }
}

public sealed class AdminAuthRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? TurnstileToken { get; set; }
    public string? TotpCode { get; set; }
    public string? RecoveryCode { get; set; }
    public string? PendingToken { get; set; }
    public string? LinkToken { get; set; }
    public string? Token { get; set; }

    public string? EffectiveLinkToken =>
        !string.IsNullOrWhiteSpace(Token) ? Token : LinkToken;
}
