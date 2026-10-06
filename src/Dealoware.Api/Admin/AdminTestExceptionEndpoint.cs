#if DEBUG
using Dealoware.Infrastructure.Admin;
#endif

namespace Dealoware.Api.Admin;

/// <summary>
/// Test-only probe used to prove an unhandled exception on the admin host
/// still returns a generic 500 with security headers. Mapped only when the
/// environment is Development or Testing and the config flag is explicitly true.
/// </summary>
public static class AdminTestExceptionEndpoint
{
    public const string Path = "/admin/api/__test/throw";
    public const string ConfigKey = "AdminHost:EnableTestExceptionEndpoint";
    public const string ProbeExceptionMessage = "admin-test-exception-probe";

    public static void MapAdminTestExceptionEndpoint(this WebApplication app)
    {
#if DEBUG
        if (!AdminHostOptions.AllowsTestExceptionEndpoint(app.Environment.EnvironmentName)
            || !app.Configuration.GetValue(ConfigKey, false))
        {
            return;
        }

        app.MapGet(Path, ThrowProbe)
            .ExcludeFromDescription()
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);
#else
        _ = app;
#endif
    }

#if DEBUG
    private static IResult ThrowProbe()
        => throw new InvalidOperationException(ProbeExceptionMessage);
#endif
}
