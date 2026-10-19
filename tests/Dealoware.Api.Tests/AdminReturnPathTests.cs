using Dealoware.Api.Admin;

namespace Dealoware.Api.Tests;

/// <summary>r3 §5 / §10 C6 remapped return-path allowlist.</summary>
public class AdminReturnPathTests
{
    [Theory]
    [InlineData("/admin/")]
    [InlineData("/admin")]
    [InlineData("/admin/participants")]
    [InlineData("/admin/artifacts")]
    [InlineData("/admin/negotiations")]
    [InlineData("/admin/offers")]
    [InlineData("/admin/audit")]
    [InlineData("/admin/participants/11111111-1111-1111-1111-111111111111")]
    [InlineData("/admin/audit/22222222-2222-2222-2222-222222222222")]
    public void TdAdm023_C6_AllowsRemappedInventory(string input)
    {
        Assert.True(AdminReturnPath.TryValidate(input, out var path));
        Assert.StartsWith("/admin/", path, StringComparison.Ordinal);
        Assert.Equal(
            "https://admin.core.dealoware.com" + path,
            AdminReturnPath.ToFixedOriginLocation(path));
    }

    [Theory]
    [InlineData("/admin/sign-in")]
    [InlineData("/admin/sign-in/code")]
    [InlineData("/admin/setup/authenticator")]
    [InlineData("/admin/setup/recovery-codes")]
    [InlineData("/admin/settings/security")]
    [InlineData("/admin/sign-out")]
    [InlineData("/admin/auth/app.js")]
    [InlineData("/artifacts")]
    [InlineData("//admin/")]
    [InlineData("/admin/../participants")]
    [InlineData("/admin/participants?next=/admin/sign-in")]
    [InlineData("/admin/%2e%2e")]
    [InlineData("https://evil.example/admin/")]
    public void TdAdm023_C6_RejectsAuthAndOffAllowlist(string input)
    {
        Assert.False(AdminReturnPath.TryValidate(input, out _));
        Assert.Equal("/admin/", AdminReturnPath.Resolve(input));
    }

    [Fact]
    public void TdAdm023_C6_NormalizesBareAdminToStats()
    {
        Assert.True(AdminReturnPath.TryValidate("/admin", out var path));
        Assert.Equal("/admin/", path);
    }
}
