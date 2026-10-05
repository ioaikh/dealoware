using System.Diagnostics;
using Dealoware.Infrastructure.Auth;

namespace Dealoware.Api.Tests;

/// <summary>
/// H6: when the real Dealoware.Api process starts outside Development without a valid signing key,
/// it must exit with code 1 (clean fail-closed) rather than aborting (134/SIGABRT or 139/SIGSEGV).
/// </summary>
public class JwtFailClosedExitTests
{
    [Fact]
    public void Process_Production_MissingSigningKey_ExitsWithCode1()
    {
        var (exitCode, stderr) = RunApiProcess(
            environment: "Production",
            signingKey: "");

        Assert.Equal(1, exitCode);
        Assert.Contains(JwtSigningKeyValidator.EnvironmentVariableName, stderr);
        Assert.Contains("not set", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            JwtSigningKeyValidator.DevelopmentPlaceholderKey,
            stderr,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Process_Production_PlaceholderSigningKey_ExitsWithCode1()
    {
        var (exitCode, stderr) = RunApiProcess(
            environment: "Production",
            signingKey: JwtSigningKeyValidator.DevelopmentPlaceholderKey);

        Assert.Equal(1, exitCode);
        Assert.Contains("placeholder", stderr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            JwtSigningKeyValidator.DevelopmentPlaceholderKey,
            stderr,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unhandled exception", stderr, StringComparison.OrdinalIgnoreCase);
    }

    private static (int ExitCode, string Stderr) RunApiProcess(string environment, string signingKey)
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
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment[JwtSigningKeyValidator.EnvironmentVariableName] = signingKey;
        start.Environment["Jwt__SigningKey"] = "";

        using var process = Process.Start(start);
        Assert.NotNull(process);

        var stderrTask = process!.StandardError.ReadToEndAsync();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var exited = process.WaitForExit(30_000);
        Assert.True(exited, "Dealoware.Api should exit promptly on fail-closed JWT validation");

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
