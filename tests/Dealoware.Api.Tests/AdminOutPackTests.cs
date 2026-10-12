using System.Xml.Linq;
using Dealoware.Api.RateLimiting;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-110 / TD-ADM-150 / TD-ADM-054 process checks for this PR.
/// </summary>
public class AdminOutPackTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Dealoware.sln");
    }

    [Fact]
    public void TdAdm110_NoAwsSesSdkPackageReference()
    {
        var root = RepoRoot();
        foreach (var csproj in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            if (csproj.Contains($"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}"))
                continue;
            var xml = XDocument.Load(csproj);
            var refs = xml.Descendants("PackageReference")
                .Select(e => (string?)e.Attribute("Include") ?? "")
                .ToList();
            Assert.DoesNotContain(refs, r => r.Contains("AWSSDK.", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(refs, r => r.Contains("Amazon.SimpleEmail", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(refs, r => r.Contains("Amazon.SES", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void TdAdm054_NoProcessGlobalAuthFloodLimiterType()
    {
        var limiterType = typeof(AuthRateLimiting).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name.Contains("ProcessGlobal", StringComparison.OrdinalIgnoreCase)
                                 && t.Name.Contains("Auth", StringComparison.OrdinalIgnoreCase));
        Assert.Null(limiterType);
    }

    [Fact]
    public void TdAdm150_IpHasherKeyNameOnly_NoHardCodedKeyInSource()
    {
        var hasherPath = Path.Combine(RepoRoot(), "src", "Dealoware.Infrastructure", "Admin", "IpHasher.cs");
        var source = File.ReadAllText(hasherPath);
        Assert.Contains("DEALOWARE_ADMIN_IP_HMAC_KEY", source);
        Assert.DoesNotContain("Turnstile", source, StringComparison.OrdinalIgnoreCase);

        var totpPath = Path.Combine(RepoRoot(), "src", "Dealoware.Infrastructure", "Admin", "TotpSecretProtector.cs");
        var totpSource = File.ReadAllText(totpPath);
        Assert.Contains("DEALOWARE_ADMIN_TOTP_KEY", totpSource);
        Assert.DoesNotContain("otpauth://", totpSource, StringComparison.OrdinalIgnoreCase);

        var recoveryPath = Path.Combine(RepoRoot(), "src", "Dealoware.Infrastructure", "Admin", "AdminRecoveryCodeHasher.cs");
        var recoverySource = File.ReadAllText(recoveryPath);
        Assert.Contains("DEALOWARE_ADMIN_RECOVERY_HMAC_KEY", recoverySource);
        Assert.DoesNotContain("otpauth://", recoverySource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SecSc6_FactorLockStateIsNotHeldInProcessMemory()
    {
        var root = RepoRoot();
        var repoPath = Path.Combine(root, "src", "Dealoware.Infrastructure", "Admin", "AdminCoreOwnerAccountRepository.cs");
        var endpointsPath = Path.Combine(root, "src", "Dealoware.Api", "Admin", "AdminAuthEndpoints.cs");
        var repoSource = File.ReadAllText(repoPath);
        var endpointsSource = File.ReadAllText(endpointsPath);

        Assert.Contains("ExecuteUpdateAsync", repoSource, StringComparison.Ordinal);
        Assert.Contains("FailedFactorAttempts", repoSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteSqlInterpolatedAsync", repoSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ConcurrentDictionary", repoSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MemoryCache", repoSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ConcurrentDictionary", endpointsSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MemoryCache", endpointsSource, StringComparison.Ordinal);
    }
}
