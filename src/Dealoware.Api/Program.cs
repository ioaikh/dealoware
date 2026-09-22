using Dealoware.Api.Endpoints;
using Dealoware.Infrastructure;
using Dealoware.Infrastructure.Auth;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DEALOWARE_CONNECTION_STRING")
    ?? "Data Source=dealoware.db";

builder.Services.AddInfrastructure(connectionString);

var jwtSettings = new JwtSettings
{
    SigningKey = Environment.GetEnvironmentVariable("DEALOWARE_JWT_SIGNING_KEY")
        ?? builder.Configuration["Jwt:SigningKey"]
        ?? "DEVELOPMENT_PLACEHOLDER_KEY_CHANGE_IN_PRODUCTION_32CHARS",
    TokenLifetimeMinutes = int.TryParse(
        Environment.GetEnvironmentVariable("DEALOWARE_JWT_LIFETIME_MINUTES") 
        ?? builder.Configuration["Jwt:LifetimeMinutes"], 
        out var lifetime) ? lifetime : 60
};

builder.Services.AddAuthServices(jwtSettings);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapAuthEndpoints();
app.MapArtifactEndpoints();
app.MapNegotiationEndpoints();
app.MapOfferEndpoints();
app.MapProfileEndpoints();
app.MapStrategyEndpoints();

app.Run();

public partial class Program { }
