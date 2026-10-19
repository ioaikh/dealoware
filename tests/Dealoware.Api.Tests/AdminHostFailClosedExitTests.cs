using System.Diagnostics;
using System.Security.Cryptography;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Auth;
using Dealoware.Infrastructure.Persistence;

namespace Dealoware.Api.Tests;

/// <summary>
/// A11 items 3, 4, and 36: the real Dealoware.Api process must exit 1 with a clear
/// log line (no stack-trace crash) when admin host hardening or the IP HMAC key fails.
/// </summary>
public class AdminHostFailClosedExitTests
{
    [Fact]
    public void Process_Production_EnforceHostValidationFalse_ExitsWithCode1()
    {
        var (exitCode, stderr) = RunApiProcess(environment =>
        {
            environment["AdminHost__EnforceHostValidation"] = "false";
        });

        Assert.Equal(1, exitCode);
        Assert.Contains(AdminHostOptionsValidator.EnforceHostValidationMessage, stderr);
        Assert.DoesNotContain("Unhandled exception", stderr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void Process_Production_ExtraAllowedHost_ExitsWithCode1()
    {
        var (exitCode, stderr) = RunApiProcess(environment =>
        {
            environment["AdminHost__AllowedHosts__1"] = "evil.example";
        });

        Assert.Equal(1, exitCode);
        Assert.Contains(AdminHostOptionsValidator.AllowedHostsMessage, stderr);
        Assert.DoesNotContain("evil.example", stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("Unhandled exception", stderr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void Process_Production_ShortIpHmacKey_ExitsWithCode1()
    {
        var shortKey = Convert.ToBase64String(new byte[16]);
        var (exitCode, stderr) = RunApiProcess(environment =>
        {
            environment[IpHasher.KeyEnvironmentVariable] = shortKey;
        });

        Assert.Equal(1, exitCode);
        Assert.Contains(IpHasher.KeyEnvironmentVariable, stderr);
        Assert.Contains("too short", stderr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(shortKey, stderr);
        Assert.DoesNotContain("Unhandled exception", stderr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", stderr, StringComparison.Ordinal);
    }

    private static (int ExitCode, string Stderr) RunApiProcess(Action<IDictionary<string, string?>> configure)
    {
        var apiDll = Path.Combine(AppContext.BaseDirectory, "Dealoware.Api.dll");
        Assert.True(File.Exists(apiDll), $"Expected Api dll next to tests at {apiDll}");

        var start = new ProcessStartInfo
        {
            FileName = ResolveDotnetPath(),
            Arguments = $"\"{apiDll}\"",
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment[JwtSigningKeyValidator.EnvironmentVariableName] =
            EnvironmentWebApplicationFactory.TestSigningKey64;
        start.Environment["Jwt__SigningKey"] = "";
        start.Environment[IpHasher.KeyEnvironmentVariable] =
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        start.Environment["DB_HOST"] = "127.0.0.1";
        start.Environment["DB_NAME"] = "dealoware_test";
        if (TestStartup.TestCertificatePath is { } certPath)
        {
            start.Environment[DatabaseProviderSelector.RdsCertPathEnvVar] = certPath;
        }

        configure(start.Environment);

        using var process = Process.Start(start);
        Assert.NotNull(process);

        var stderrTask = process!.StandardError.ReadToEndAsync();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var exited = process.WaitForExit(30_000);
        Assert.True(exited, "Dealoware.Api should exit promptly on fail-closed admin host hardening");

        _ = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (process.ExitCode, stderr);
    }

    private static string ResolveDotnetPath()
    {
        var fromRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(fromRoot))
        {
            var candidate = Path.Combine(fromRoot, "dotnet");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var homeCandidate = Path.Combine(home, ".dotnet", "dotnet");
        if (File.Exists(homeCandidate))
        {
            return homeCandidate;
        }

        return "dotnet";
    }
}
