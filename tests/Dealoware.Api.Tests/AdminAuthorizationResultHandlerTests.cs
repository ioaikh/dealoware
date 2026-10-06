using System.Net;
using Dealoware.Api.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api.Tests;

/// <summary>
/// A11 item 37 / H-7: a CoreOwner policy failure after the session middleware
/// must return the generic 401 body, not a 500 from a missing authentication scheme.
/// </summary>
public class AdminAuthorizationResultHandlerTests
{
    [Fact]
    public async Task PolicyFailureOnAdminPath_ReturnsGeneric401Not500()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminSessionMiddleware.PolicyName, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(AdminSessionMiddleware.CoreOwnerRole));
        });
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AdminAuthorizationResultHandler>();

        await using var app = builder.Build();
        app.UseAuthorization();
        app.MapGet("/admin/api/policy-probe", () => Results.Ok(new { leak = "no" }))
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/admin/api/policy-probe");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("""{"error":"Unauthorized"}""", body);
        Assert.DoesNotContain("leak", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handler_AdminChallenge_WritesGeneric401()
    {
        var handler = new AdminAuthorizationResultHandler();
        var context = new DefaultHttpContext();
        context.Request.Path = "/admin/api/me";
        context.Response.Body = new MemoryStream();

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(AdminSessionMiddleware.CoreOwnerRole)
            .Build();

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            policy,
            PolicyAuthorizationResult.Challenge());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal("""{"error":"Unauthorized"}""", body);
    }

    [Fact]
    public void Program_RegistersAdminAuthorizationResultHandler()
    {
        using var factory = new IsolatedWebApplicationFactory();
        var handler = factory.Services.GetService<IAuthorizationMiddlewareResultHandler>();
        Assert.IsType<AdminAuthorizationResultHandler>(handler);
    }
}
