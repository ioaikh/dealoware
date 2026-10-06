using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;

namespace Dealoware.Api.UiTests;

/// <summary>
/// Local in-job API for Playwright. Never talks to live admin.core.dealoware.com.
/// </summary>
public sealed class AdminUiServerFixture : IAsyncLifetime
{
    public const string AdminHost = "admin.core.dealoware.com";

    private Process? _process;
    private string? _dbPath;
    private string? _sessionFile;

    public string BaseUrl { get; private set; } = string.Empty;
    public Guid SessionId { get; private set; }
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var root = FindRepoRoot();
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        _dbPath = Path.Combine(Path.GetTempPath(), $"dealoware-admin-ui-{Guid.NewGuid():N}.db");
        _sessionFile = Path.Combine(Path.GetTempPath(), $"dealoware-admin-ui-session-{Guid.NewGuid():N}.txt");

        var project = Path.Combine(root, "src", "Dealoware.Api", "Dealoware.Api.csproj");
        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{project}\" --no-launch-profile --urls {BaseUrl}",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        psi.Environment["DEALOWARE_ADMIN_UI_TEST"] = "1";
        psi.Environment["DEALOWARE_ADMIN_UI_TEST_SESSION_FILE"] = _sessionFile;
        psi.Environment["ConnectionStrings__DefaultConnection"] = $"Data Source={_dbPath}";

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process.Start();

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var ready = false;
        for (var i = 0; i < 90; i++)
        {
            if (_process.HasExited)
                throw new InvalidOperationException("Admin UI test API exited before becoming ready.");
            try
            {
                using var response = await client.GetAsync($"{BaseUrl}/health");
                if (response.IsSuccessStatusCode)
                {
                    ready = true;
                    break;
                }
            }
            catch (Exception)
            {
                // Process is still binding.
            }

            await Task.Delay(1000);
        }

        if (!ready)
            throw new TimeoutException("Admin UI test API did not become ready on /health.");

        for (var i = 0; i < 20 && !File.Exists(_sessionFile); i++)
            await Task.Delay(250);
        SessionId = Guid.Parse((await File.ReadAllTextAsync(_sessionFile)).Trim());

        var install = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (install != 0)
            throw new InvalidOperationException("Playwright Chromium install failed.");
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
            await Browser.DisposeAsync();
        Playwright?.Dispose();

        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
            catch
            {
                // Best-effort teardown.
            }
        }

        _process?.Dispose();
        TryDelete(_dbPath);
        TryDelete(_sessionFile);
    }

    private static void TryDelete(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Temp cleanup is best-effort.
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    internal static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Dealoware.sln from the UI test output directory.");
    }
}

[CollectionDefinition("AdminUiPlaywright")]
public sealed class AdminUiPlaywrightCollection : ICollectionFixture<AdminUiServerFixture>;
