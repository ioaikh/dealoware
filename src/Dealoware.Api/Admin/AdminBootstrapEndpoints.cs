using System.Text.Json.Serialization;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Step 2 bootstrap password-set API and signed-out landings.
/// Pages are Step 14; this file only serves the JSON the screens call.
/// </summary>
public static class AdminBootstrapEndpoints
{
    public static IServiceCollection AddAdminBootstrap(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<AdminMailOptions>(configuration.GetSection("Admin:Bootstrap"));
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
        services.AddSingleton<IAdminMailSender, NoOpAdminMailSender>();
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
        var antiForgery = AdminAntiForgery.Issue(context);
        return Results.Json(new { valid = true, antiForgeryToken = antiForgery });
    }

    private static IResult LinkExpired()
        => Results.Json(new { error = AdminAuthMessages.InvalidOrExpiredLink });

    private static async Task<IResult> SetBootstrapPassword(
        HttpContext context,
        IAdminBootstrapService bootstrap,
        IIpHasher ipHasher,
        BootstrapPasswordRequest body)
    {
        if (!AdminAntiForgery.Validate(context, body.AntiForgeryToken))
        {
            return AdminDeny.UnauthorizedResult();
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var ipHmac = string.IsNullOrWhiteSpace(remoteIp) ? string.Empty : ipHasher.Hash(remoteIp);
        var token = body.Token;
        if (string.IsNullOrWhiteSpace(token)
            && context.Request.Query.TryGetValue("token", out var queryToken))
        {
            token = queryToken.ToString();
        }

        var result = await bootstrap.SetPasswordAsync(
            token,
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
            _ => AdminDeny.UnauthorizedResult()
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
