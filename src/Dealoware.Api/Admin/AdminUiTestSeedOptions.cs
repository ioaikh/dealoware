namespace Dealoware.Api.Admin;

/// <summary>
/// Local / CI Playwright seed. Enabled only in Development or Testing.
/// </summary>
public sealed class AdminUiTestSeedOptions
{
    public const string SectionName = "Admin:UiTestSeed";

    public const string EnvironmentVariableName = "ADMIN_UI_TEST_SEED";

    public const string Route = "/admin/api/ui-test-seed";

    /// <summary>Must stay false outside Development / Testing.</summary>
    public bool Enabled { get; set; }
}
