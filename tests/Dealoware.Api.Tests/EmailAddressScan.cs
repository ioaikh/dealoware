using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-150, extended for T59 (answer 11): no email address in the public repository except
/// an allowed placeholder. The allowlist is kept here and reviewed with this file. A finding
/// names only the file and the line, never the address, so a failed run does not copy the
/// address into the CI log.
/// </summary>
public static class EmailAddressScan
{
    /// <summary>
    /// Domains reserved for documentation and testing (RFC 2606, RFC 6761): the domain itself
    /// or any name under it is allowed.
    /// </summary>
    public static readonly IReadOnlyList<string> ReservedDomains =
    [
        "example.com",
        "example.org",
        "example.net",
        "example",
        "invalid",
        "test"
    ];

    /// <summary>
    /// The only other addresses allowed, each exactly as written (compared without case).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedAddresses =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // The engine's sending address (T59 spec §2.7, answer 5).
            ["no-reply@dealoware.com"] = "the bare sending address",
            // Not addresses: the user-info part of URLs that tests expect to be refused.
            ["pass@admin.core.dealoware.com"] = "e2e/signed-out/require-proxy.check.cjs, a URL with user info",
            ["5287@api.core.dealoware.com"] = "PostmanCollectionHygieneTests, a URL with user info",
            // Google Authenticator's published key-URI example, used as a QR test input.
            ["alice@google.com"] = "AdminQrCodeTests, the published otpauth example",
            // Made-up fixtures under a name that is not a real domain.
            ["seller@login.secret"] = "StageBContactOnAcceptTests fixture",
            ["buyer@login.secret"] = "StageBContactOnAcceptTests fixture"
        };

    // Local part, "@", then at least two labels; the last label is letters only. This also
    // matches "user:password@host" in a URL, which is worth refusing as well.
    private static readonly Regex AddressPattern = new(
        @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(?:\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public sealed record Finding(string RelativePath, int Line);

    public static bool IsAllowed(string address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (AllowedAddresses.ContainsKey(address))
            return true;

        var at = address.LastIndexOf('@');
        var domain = address[(at + 1)..].ToLowerInvariant();
        return ReservedDomains.Any(d => domain == d || domain.EndsWith("." + d, StringComparison.Ordinal));
    }

    /// <summary>Every address in <paramref name="text"/> that is not allowed, with its line number.</summary>
    public static IEnumerable<(string Address, int Line)> FindDisallowed(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        foreach (Match match in AddressPattern.Matches(text))
        {
            if (IsAllowed(match.Value))
                continue;

            var line = 1;
            for (var i = 0; i < match.Index; i++)
            {
                if (text[i] == '\n')
                    line++;
            }

            yield return (match.Value, line);
        }
    }

    /// <summary>Scans the given files under <paramref name="root"/>. Binary files are skipped.</summary>
    public static IReadOnlyList<Finding> Scan(string root, IEnumerable<string> relativePaths)
    {
        var findings = new List<Finding>();
        foreach (var relative in relativePaths)
        {
            var path = Path.Combine(root, relative);
            if (!File.Exists(path))
                continue; // deleted in the working tree but still in the index

            var bytes = File.ReadAllBytes(path);
            if (LooksBinary(bytes))
                continue;

            var text = Encoding.UTF8.GetString(bytes);
            findings.AddRange(FindDisallowed(text).Select(f => new Finding(relative, f.Line)));
        }

        return findings;
    }

    /// <summary>
    /// The files git tracks in <paramref name="repoRoot"/>. Fails, never returns an empty list,
    /// when git cannot list them: a scan that read nothing must not pass.
    /// </summary>
    public static IReadOnlyList<string> TrackedFiles(string repoRoot)
    {
        var start = new ProcessStartInfo("git", "ls-files -z")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("git could not be started to list the repository's files.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60_000))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("git ls-files did not finish in 60 seconds.");
        }

        if (process.ExitCode != 0)
            throw new InvalidOperationException("git ls-files failed: " + stderr.Result.Trim());

        var files = stdout.Result.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        if (files.Length == 0)
            throw new InvalidOperationException("git ls-files listed no files.");

        return files;
    }

    private static bool LooksBinary(byte[] bytes)
    {
        var length = Math.Min(bytes.Length, 8000);
        for (var i = 0; i < length; i++)
        {
            if (bytes[i] == 0)
                return true;
        }

        return false;
    }
}
