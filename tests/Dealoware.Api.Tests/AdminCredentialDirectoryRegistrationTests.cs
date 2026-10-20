using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

public class AdminCredentialDirectoryRegistrationTests
{
    [Fact]
    public void R4_InMemoryDirectory_IsRegisteredInDevelopmentOnly()
    {
        using var development = new IsolatedWebApplicationFactory();
        Assert.NotNull(development.Services.GetService<InMemoryAdminCredentialDirectory>());
        Assert.IsType<InMemoryAdminCredentialDirectory>(
            development.Services.GetRequiredService<IAdminCredentialDirectory>());

        using var production = new EnvironmentWebApplicationFactory(
            "Production",
            EnvironmentWebApplicationFactory.TestSigningKey64);
        Assert.Null(production.Services.GetService<InMemoryAdminCredentialDirectory>());
        using var scope = production.Services.CreateScope();
        Assert.IsType<CoreOwnerAccountCredentialDirectory>(
            scope.ServiceProvider.GetRequiredService<IAdminCredentialDirectory>());
    }
}
