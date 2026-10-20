using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Development/Testing-only POST used to prove CSRF+Origin default-deny on a
/// newly mapped /admin route that is not on any exemption list.
/// </summary>
public static class AdminCsrfProbeEndpoint
{
    public const string Path = "/admin/api/__test/csrf-probe";

    public static void MapAdminCsrfProbeEndpoint(this WebApplication app)
    {
        if (!AdminHostOptions.AllowsTestExceptionEndpoint(app.Environment.EnvironmentName))
        {
            return;
        }

        app.MapPost(Path, () => Results.Json(new { ok = true }))
            .ExcludeFromDescription()
            .AllowAnonymous();
    }
}
