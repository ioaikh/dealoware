namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-150, extended for T59 (spec §2.9, answer 11): the build fails on any email address in
/// the public repository that is not an allowed placeholder. The owner address is supplied by
/// the environment and is never written in the repository.
/// </summary>
public class EmailAddressScanTests
{
    [Fact]
    public void TD_ADM_150_Repository_HasNoEmailAddressOutsideTheAllowlist()
    {
        var root = RepoRoot();
        var files = EmailAddressScan.TrackedFiles(root);

        var findings = EmailAddressScan.Scan(root, files);

        // Only the file and line are reported, never the address itself.
        Assert.True(
            findings.Count == 0,
            "Email address outside the placeholder allowlist (see EmailAddressScan.cs) at: "
            + string.Join(", ", findings.Select(f => f.RelativePath + ":" + f.Line)));
        Assert.Contains(files, f => f.EndsWith("appsettings.json", StringComparison.Ordinal));
        Assert.Contains(files, f => f.StartsWith("docs/", StringComparison.Ordinal));
    }

    [Fact]
    public void TD_ADM_150_AddressScan_FailsOnAPlantedAddress()
    {
        // Built at run time so this file itself holds no address.
        var planted = "planted.person" + "@" + "nowhere-real" + ".net";
        var dir = Directory.CreateTempSubdirectory("dealoware-scan-");
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "clean.md"), "Nothing to see: owner@example.test, no-reply@dealoware.com.\n");
            File.WriteAllText(Path.Combine(dir.FullName, "planted.md"), "line one\nline two\ncontact " + planted + " today\n");

            var findings = EmailAddressScan.Scan(dir.FullName, ["clean.md", "planted.md"]);

            var finding = Assert.Single(findings);
            Assert.Equal("planted.md", finding.RelativePath);
            Assert.Equal(3, finding.Line);
            Assert.DoesNotContain(planted, finding.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void TD_ADM_150_AddressScan_SkipsBinaryFiles()
    {
        var dir = Directory.CreateTempSubdirectory("dealoware-scan-");
        try
        {
            var bytes = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x00 }
                .Concat(System.Text.Encoding.UTF8.GetBytes("someone" + "@" + "nowhere-real" + ".net"))
                .ToArray();
            File.WriteAllBytes(Path.Combine(dir.FullName, "image.png"), bytes);

            Assert.Empty(EmailAddressScan.Scan(dir.FullName, ["image.png"]));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("owner@example.test")]
    [InlineData("Owner@Example.Test")]
    [InlineData("alex.doe@sub.example.test")]
    [InlineData("contact@example.com")]
    [InlineData("qa@example.org")]
    [InlineData("qa@example.net")]
    [InlineData("seller@contact.example")]
    [InlineData("alerts@example.invalid")]
    [InlineData("no-reply@dealoware.com")]
    [InlineData("NO-REPLY@DEALOWARE.COM")]
    [InlineData("alice@google.com")]
    public void TD_ADM_150_AddressScan_PassesOnTheAllowlist(string address)
    {
        Assert.True(EmailAddressScan.IsAllowed(address));
        Assert.Empty(EmailAddressScan.FindDisallowed("text " + address + " text"));
    }

    [Theory]
    [InlineData("someone", "dealoware.com")]
    [InlineData("no-reply", "dealoware.com.nowhere-real.net")]
    [InlineData("contact", "notexample.com")]
    [InlineData("contact", "example.com.nowhere-real.net")]
    [InlineData("person", "testing.net")]
    [InlineData("person", "nowhere-real.org")]
    public void TD_ADM_150_AddressScan_RefusesLookalikes(string local, string domain)
    {
        var address = local + "@" + domain;
        Assert.False(EmailAddressScan.IsAllowed(address));
        Assert.Single(EmailAddressScan.FindDisallowed("x " + address + " y"));
    }

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
}
