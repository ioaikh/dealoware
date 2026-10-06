using System.Text.Json.Serialization;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Step 2 bootstrap password-set API and signed-out landings.
/// Pages are Step 14; this file only serves the JSON the screens call.
/// F2 (r5 sha c5e9d232): GET /admin/bootstrap takes no token and has no
/// side effects. The token is checked on POST only.
/// </summary>
public static class AdminBootstrapEndpoints
{
    public static IServiceCollection AddAdminBootstrap(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<TurnstileOptions>(options =>
        {
            options.Secret = configuration[TurnstileOptions.SecretEnvironmentVariable]
                ?? Environment.GetEnvironmentVariable(TurnstileOptions.SecretEnvironmentVariable);
        });

        if (!environment.IsDevelopment())
        {
            var secret = configuration[TurnstileOptions.SecretEnvironmentVariable]
                ?? Environment.GetEnvironmentVariable(TurnstileOptions.SecretEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 16)
            {
                throw new InvalidOperationException(
                    "Turnstile verification is not configured. Set the secret via the documented environment variable.");
            }
        }

        services.AddHttpClient<ITurnstileVerifier, CloudflareTurnstileVerifier>();
        services.AddSingleton<IAdminClock, SystemAdminClock>();
        services.AddScoped<IAdminBootstrapService, AdminBootstrapService>();
        return services;
    }

    public static WebApplication MapAdminBootstrap(this WebApplication app)
    {
        app.MapGet(AdminSignedOutAccess.BootstrapPage, InspectBootstrap)
            .WithName("AdminBootstrapInspect")
            .WithTags("Admin")
            .AllowAnonymous();

        app.MapPost(AdminSignedOutAccess.BootstrapPage, SetBootstrapPassword)
            .WithName("AdminBootstrapPasswordPage")
            .WithTags("Admin")
            .AllowAnonymous();

        app.MapPost(AdminSignedOutAccess.BootstrapApi, SetBootstrapPassword)
            .WithName("AdminBootstrapPasswordApi")
            .WithTags("Admin")
            .AllowAnonymous();

        app.MapGet(AdminSignedOutAccess.LinkExpired, LinkExpired)
            .WithName("AdminLinkExpired")
            .WithTags("Admin")
            .AllowAnonymous();

        return app;
    }

    private static IResult InspectBootstrap(HttpContext context)
    {
        // F2: no token query, no inspect, no consume.
        var antiForgery = AdminAntiForgery.Issue(context);
        return Results.Json(new { antiForgeryToken = antiForgery });
    }

    private static IResult LinkExpired()
        => Results.Json(new { error = AdminAuthMessages.InvalidOrExpiredLink });

    private static IResult GoToLinkExpired()
        => Results.Redirect(AdminSignedOutAccess.LinkExpired);

    private static IResult Throttled()
        => Results.Json(
            new { error = AdminAuthMessages.InvalidOrExpiredLink },
            statusCode: StatusCodes.Status429TooManyRequests);

    private static async Task<IResult> SetBootstrapPassword(
        HttpContext context,
        IAdminBootstrapService bootstrap,
        IIpHasher ipHasher,
        BootstrapPasswordRequest body)
    {
        if (!AdminAntiForgery.Validate(context, body.AntiForgeryToken))
        {
            return GoToLinkExpired();
        }

        var remoteIp = AdminTrustedClientIp.Get(context);
        var ipHmac = ipHasher.Hash(remoteIp);
        var result = await bootstrap.SetPasswordAsync(
            body.Token,
            body.Password,
            body.TurnstileToken,
            remoteIp,
            ipHmac,
            context.RequestAborted);

        return result switch
        {
            AdminBootstrapSetPasswordResult.Succeeded => Results.Json(new { passwordSet = true }),
            AdminBootstrapSetPasswordResult.TurnstileFailed => Results.Json(
                new { error = AdminAuthMessages.TurnstileFailed },
                statusCode: StatusCodes.Status400BadRequest),
            AdminBootstrapSetPasswordResult.PasswordRejected rejected => Results.Json(
                new { error = rejected.Error },
                statusCode: StatusCodes.Status400BadRequest),
            AdminBootstrapSetPasswordResult.Throttled => Throttled(),
            _ => GoToLinkExpired()
        };
    }

    public sealed class BootstrapPasswordRequest
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("turnstileToken")]
        public string? TurnstileToken { get; set; }

        [JsonPropertyName("antiForgeryToken")]
        public string? AntiForgeryToken { get; set; }
    }
}
