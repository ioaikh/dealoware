using System.Net;
using Dealoware.Api.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Tests;

/// <summary>
/// Admin UI test harness is compiled out of Release and 404s in Production
/// even when DEALOWARE_ADMIN_UI_TEST is set.
/// </summary>
public class AdminUiTestHarnessIsolationTests
{
    [Fact]
    public void HarnessType_IsAbsentFromReleaseBinary()
    {
#if DEBUG
        Assert.NotNull(typeof(Program).Assembly.GetType("Dealoware.Api.Admin.AdminUiTestHarness"));
#else
        Assert.Null(typeof(Program).Assembly.GetType("Dealoware.Api.Admin.AdminUiTestHarness"));
#endif
    }

    [Theory]
    [InlineData("/admin/ui-test")]
    [InlineData("/admin/ui-test/session")]
    [InlineData("/admin/ui-test/seed")]
    public async Task Production_HarnessRoutes_Return404_EvenWithFlag(string path)
    {
        await using var factory = new ProductionHarnessFlagFactory();
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = "admin.core.dealoware.com";
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("session", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ProductionHarnessFlagFactory : IsolatedWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment(Environments.Production);
            builder.UseSetting(
                JwtSigningKeyValidator.EnvironmentVariableName,
                EnvironmentWebApplicationFactory.TestSigningKey64);
            builder.UseSetting("Jwt:SigningKey", string.Empty);
            builder.UseSetting(IpHasher.KeyEnvironmentVariable, EnvironmentWebApplicationFactory.TestIpHmacKey);
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;Port=5432;Database=dealoware_test;Username=test;Password=test");
            builder.UseSetting(AdminUiRoutes.TestHarnessFlag, "1");
        }
    }
}
