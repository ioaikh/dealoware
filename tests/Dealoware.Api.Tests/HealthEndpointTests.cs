using System.Net;
using System.Text.Json;

namespace Dealoware.Api.Tests;

[Collection("WebAppTests")]
public class HealthEndpointTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public HealthEndpointTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOkWithStatusOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("ok", doc.RootElement.GetProperty("status").GetString());
    }
}
