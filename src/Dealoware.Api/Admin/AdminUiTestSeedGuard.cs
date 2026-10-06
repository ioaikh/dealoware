namespace Dealoware.Api.Admin;

/// <summary>
/// Fail-closed gate for the Playwright seed. The flag is refused outside
/// Development and Testing so production never maps seed routes.
/// </summary>
public static class AdminUiTestSeedGuard
{
    public const string RefusedMessage =
        "Admin UI test seed is only allowed in Development or Testing.";

    public static bool IsLocalTestEnvironment(IHostEnvironment environment)
        => environment.IsDevelopment()
           || environment.IsEnvironment("Testing");

    public static bool IsAllowed(IHostEnvironment environment, AdminUiTestSeedOptions options)
        => options.Enabled && IsLocalTestEnvironment(environment);

    public static void Validate(IHostEnvironment environment, AdminUiTestSeedOptions options)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);
        if (options.Enabled && !IsLocalTestEnvironment(environment))
            throw new InvalidOperationException(RefusedMessage);
    }

    public static AdminUiTestSeedOptions Resolve(IConfiguration configuration)
    {
        var options = new AdminUiTestSeedOptions();
        configuration.GetSection(AdminUiTestSeedOptions.SectionName).Bind(options);
        if (IsTruthy(configuration[AdminUiTestSeedOptions.EnvironmentVariableName]
                     ?? Environment.GetEnvironmentVariable(AdminUiTestSeedOptions.EnvironmentVariableName)))
        {
            options.Enabled = true;
        }

        return options;
    }

    public static bool IsSeedRoute(PathString path)
        => path.Equals(AdminUiTestSeedOptions.Route, StringComparison.OrdinalIgnoreCase);

    public static bool IsTruthy(string? raw)
        => string.Equals(raw, "1", StringComparison.Ordinal)
           || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);

    public static void ApplyResolvedEnabled(AdminUiTestSeedOptions options, IConfiguration configuration)
    {
        if (IsTruthy(configuration[AdminUiTestSeedOptions.EnvironmentVariableName]
                     ?? Environment.GetEnvironmentVariable(AdminUiTestSeedOptions.EnvironmentVariableName)))
        {
            options.Enabled = true;
        }
    }
}
