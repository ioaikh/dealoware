using System.Net;

namespace Dealoware.Api.Tests;

/// <summary>
/// Tests that verify Swagger is gated by environment:
/// - Production: Swagger JSON and UI return 404 (disabled)
/// - Development: Swagger JSON and UI return 200 (enabled)
/// </summary>
public class SwaggerEnvironmentTests
{
    [Fact]
    public async Task Swagger_Json_Production_Returns404()
    {
        await using var factory = new EnvironmentWebApplicationFactory(
            "Production",
            EnvironmentWebApplicationFactory.TestSigningKey64);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_UI_Production_Returns404()
    {
        await using var factory = new EnvironmentWebApplicationFactory(
            "Production",
            EnvironmentWebApplicationFactory.TestSigningKey64);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_Json_Development_Returns200()
    {
        await using var factory = new EnvironmentWebApplicationFactory(
            "Development",
            EnvironmentWebApplicationFactory.TestSigningKey64);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Dealoware API", content);
    }

    [Fact]
    public async Task Swagger_Json_Development_HasJwtBearerSecurityScheme()
    {
        await using var factory = new EnvironmentWebApplicationFactory(
            "Development",
            EnvironmentWebApplicationFactory.TestSigningKey64);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"type\": \"http\"", content);
        Assert.Contains("\"scheme\": \"bearer\"", content);
        Assert.Contains("\"bearerFormat\": \"JWT\"", content);
    }

    [Fact]
    public async Task Swagger_UI_Development_Returns200()
    {
        await using var factory = new EnvironmentWebApplicationFactory(
            "Development",
            EnvironmentWebApplicationFactory.TestSigningKey64);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_Json_Staging_Returns404()
    {
        await using var factory = new EnvironmentWebApplicationFactory(
            "Staging",
            EnvironmentWebApplicationFactory.TestSigningKey64);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IndexHtml_Production_Returns404()
    {
        await using var factory = new EnvironmentWebApplicationFactory(
            "Production",
            EnvironmentWebApplicationFactory.TestSigningKey64);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
