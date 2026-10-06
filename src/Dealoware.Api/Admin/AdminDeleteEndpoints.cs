using System.Security.Claims;
using Dealoware.Domain.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

public static class AdminDeleteEndpoints
{
    public static void MapAdminDelete(this WebApplication app)
    {
        var api = app.MapGroup("/admin/api")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .WithTags("AdminDelete");

        api.MapPost("/{entityType}/{id:guid}/delete-intent", CreateIntent);
        api.MapDelete("/{entityType}/{id:guid}", ConfirmDelete);
        api.MapGet("/delete-ui/stats", GetStats);
        api.MapGet("/delete-ui/{entityType}", ListEntities);
        api.MapGet("/delete-ui/{entityType}/{id:guid}", GetEntity);

        var pages = app.MapGroup("/admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .WithTags("AdminDeleteUi");

        pages.MapGet("/", () => AdminDeleteUiPages.Page("Stats", "stats", null, null));
        pages.MapGet("/participants", () => AdminDeleteUiPages.Page("Participants", "list", "participants", null));
        pages.MapGet("/artifacts", () => AdminDeleteUiPages.Page("Artifacts", "list", "artifacts", null));
        pages.MapGet("/negotiations", () => AdminDeleteUiPages.Page("Negotiations", "list", "negotiations", null));
        pages.MapGet("/offers", () => AdminDeleteUiPages.Page("Offers", "list", "offers", null));
        pages.MapGet("/participants/{id:guid}", (Guid id) => AdminDeleteUiPages.Page("Participant", "detail", "participants", id));
        pages.MapGet("/artifacts/{id:guid}", (Guid id) => AdminDeleteUiPages.Page("Artifact", "detail", "artifacts", id));
        pages.MapGet("/negotiations/{id:guid}", (Guid id) => AdminDeleteUiPages.Page("Negotiation", "detail", "negotiations", id));
        pages.MapGet("/offers/{id:guid}", (Guid id) => AdminDeleteUiPages.Page("Offer", "detail", "offers", id));
        pages.MapGet("/assets/admin-delete.css", () => AdminDeleteUiPages.Css());
        pages.MapGet("/assets/admin-delete.js", () => AdminDeleteUiPages.JavaScript());

        var seed = app.Services.GetRequiredService<IOptions<AdminUiTestSeedOptions>>().Value;
        if (AdminUiTestSeedGuard.IsAllowed(app.Environment, seed))
        {
            AdminUiTestSeed.Map(app);
            AdminUiTestSeed.TrySeed(app);
        }
    }

    public static IServiceCollection AddAdminDelete(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        var seedOptions = AdminUiTestSeedGuard.Resolve(configuration);
        AdminUiTestSeedGuard.Validate(environment, seedOptions);
        var seedAllowed = AdminUiTestSeedGuard.IsAllowed(environment, seedOptions);

        services.AddOptions<AdminDeleteConfirmOptions>()
            .BindConfiguration(AdminDeleteConfirmOptions.SectionName);
        services.AddOptions<AdminUiTestSeedOptions>()
            .BindConfiguration(AdminUiTestSeedOptions.SectionName)
            .PostConfigure(options => AdminUiTestSeedGuard.ApplyResolvedEnabled(options, configuration));
        services.PostConfigure<Dealoware.Infrastructure.Admin.AdminHostOptions>(hosts =>
        {
            if (!seedAllowed)
                return;
            hosts.AllowedHosts ??= [];
            if (!hosts.AllowedHosts.Contains("127.0.0.1", StringComparer.OrdinalIgnoreCase))
                hosts.AllowedHosts.Add("127.0.0.1");
        });
        services.AddSingleton<IAdminClock, Infrastructure.Admin.SystemAdminClock>();
        services.AddScoped<IAdminDeleteConfirmTokenRepository, Infrastructure.Admin.AdminDeleteConfirmTokenRepository>();
        services.AddScoped<AdminDeleteService>();
        return services;
    }

    private static async Task<IResult> CreateIntent(
        string entityType,
        Guid id,
        HttpContext http,
        AdminDeleteService deletes)
    {
        if (!TryActor(http, out var email, out var sessionId))
            return AdminDeny.UnauthorizedResult();

        var result = await deletes.CreateIntentAsync(
            entityType, id, email, sessionId, http.RequestAborted);
        return ToResult(result);
    }

    private static async Task<IResult> ConfirmDelete(
        string entityType,
        Guid id,
        HttpContext http,
        AdminDeleteService deletes)
    {
        if (!TryActor(http, out var email, out var sessionId))
            return AdminDeny.UnauthorizedResult();

        http.Request.Headers.TryGetValue(AdminDeleteService.ConfirmTokenHeader, out var tokenValues);
        var ifMatch = http.Request.Headers.IfMatch.ToString();
        if (string.IsNullOrEmpty(ifMatch))
            ifMatch = http.Request.Headers["If-Match"].ToString();

        try
        {
            var result = await deletes.ConfirmAsync(
                entityType,
                id,
                tokenValues.ToString(),
                ifMatch,
                email,
                sessionId,
                http.Connection.RemoteIpAddress?.ToString(),
                http.RequestAborted);
            return ToResult(result);
        }
        catch
        {
            return Results.Json(new { error = "Nothing was deleted." }, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ListEntities(
        string entityType,
        AdminDeleteService deletes,
        CancellationToken cancellationToken,
        bool includeDeleted = false)
    {
        if (!AdminDeleteService.TryNormalizeType(entityType, out var type))
            return Results.Json(new { error = "Not found" }, statusCode: StatusCodes.Status404NotFound);

        var items = await deletes.ListAsync(type, includeDeleted, cancellationToken);
        return Results.Json(new { items, includeDeleted });
    }

    private static async Task<IResult> GetEntity(
        string entityType,
        Guid id,
        AdminDeleteService deletes,
        CancellationToken cancellationToken,
        bool includeDeleted = false)
    {
        if (!AdminDeleteService.TryNormalizeType(entityType, out var type))
            return Results.Json(new { error = "Not found" }, statusCode: StatusCodes.Status404NotFound);

        var items = await deletes.ListAsync(type, includeDeleted: true, cancellationToken);
        var row = items.FirstOrDefault(i => i.Id == id);
        if (row is null)
            return Results.Json(new { error = "Not found" }, statusCode: StatusCodes.Status404NotFound);
        if (!includeDeleted && row.DeletedAt is not null)
            return Results.Json(new { error = "Not found" }, statusCode: StatusCodes.Status404NotFound);
        return Results.Json(row);
    }

    private static async Task<IResult> GetStats(AdminDeleteService deletes, CancellationToken cancellationToken)
        => Results.Json(await deletes.StatsAsync(cancellationToken));

    private static IResult ToResult(AdminDeleteResult result)
    {
        if (result.StatusCode == StatusCodes.Status204NoContent)
            return Results.NoContent();
        return Results.Json(result.Body, statusCode: result.StatusCode);
    }

    private static bool TryActor(HttpContext http, out string email, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        email = http.User.FindFirstValue(ClaimTypes.Email)
                ?? http.User.FindFirstValue(ClaimTypes.Name)
                ?? string.Empty;
        var raw = http.User.FindFirstValue("admin_session_id");
        if (string.IsNullOrEmpty(raw) && http.Request.Cookies.TryGetValue(AdminSessionCookie.Name, out var cookie))
            raw = cookie;
        return !string.IsNullOrEmpty(email) && Guid.TryParse(raw, out sessionId);
    }
}
