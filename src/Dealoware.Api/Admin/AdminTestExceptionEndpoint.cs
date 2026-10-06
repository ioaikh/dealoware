namespace Dealoware.Api.Admin;

/// <summary>
/// Test-only probe used to prove an unhandled exception on the admin host
/// still returns a generic 500 with security headers. Off unless the config
/// flag is explicitly true — never enabled by a production appsettings file.
/// </summary>
public static class AdminTestExceptionEndpoint
{
    public const string Path = "/admin/api/__test/throw";
    public const string ConfigKey = "AdminHost:EnableTestExceptionEndpoint";
    public const string ProbeExceptionMessage = "admin-test-exception-probe";

    public static void MapAdminTestExceptionEndpoint(this WebApplication app)
    {
        if (!app.Configuration.GetValue(ConfigKey, false))
            return;

        app.MapGet(Path, ThrowProbe)
            .ExcludeFromDescription();
    }

    private static IResult ThrowProbe()
        => throw new InvalidOperationException(ProbeExceptionMessage);
}
