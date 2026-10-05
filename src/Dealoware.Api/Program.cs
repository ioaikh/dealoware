using Dealoware.Api;
using Dealoware.Api.Endpoints;
using Dealoware.Infrastructure;
using Dealoware.Infrastructure.Auth;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

// One-shot migrate: same image/binary, separate process. Does not start the web host.
// Operator injects migrations-capable DB_* into this task only — never into the long-lived API.
if (DatabaseMigrateCommand.IsMigrateArgs(args))
{
    Environment.ExitCode = await DatabaseMigrateCommand.RunAsync(args);
    return;
}

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DEALOWARE_CONNECTION_STRING")
    ?? "Data Source=dealoware.db";

builder.Services.AddInfrastructure(connectionString);

var jwtSettings = new JwtSettings
{
    // Every environment except Development fails fast on a missing, placeholder, or too-short key;
    // only Development keeps the placeholder fallback. The environment variable is read through
    // configuration (environment variables are a default configuration source).
    SigningKey = JwtSigningKeyValidator.Validate(
        builder.Configuration[JwtSigningKeyValidator.EnvironmentVariableName]
            ?? builder.Configuration["Jwt:SigningKey"],
        requireStrictKey: !builder.Environment.IsDevelopment()),
    TokenLifetimeMinutes = int.TryParse(
        Environment.GetEnvironmentVariable("DEALOWARE_JWT_LIFETIME_MINUTES") 
        ?? builder.Configuration["Jwt:LifetimeMinutes"], 
        out var lifetime) ? lifetime : 60
};

builder.Services.AddAuthServices(jwtSettings);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dealoware API",
        Version = "v1",
        Description = "Universal Negotiation Platform API - PoC"
    });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key authentication. Use 'ApiKey {your-key}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dealoware API v1");
    c.RoutePrefix = "swagger";
});

app.UseDefaultFiles();
app.UseStaticFiles();

// Development only: EnsureCreated for local SQLite. Non-Development schema changes
// use the migrate one-shot (see DatabaseMigrateCommand) — not baked into API startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
    await DatabaseSchemaBootstrap.ApplyStartupSchemaAsync(db, app.Environment.EnvironmentName);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health")
    .WithTags("Health")
    .Produces<object>(StatusCodes.Status200OK);

app.MapAuthEndpoints();
app.MapArtifactEndpoints();
app.MapSearchEndpoints();
app.MapNegotiationEndpoints();
app.MapOfferEndpoints();
app.MapProfileEndpoints();
app.MapStrategyEndpoints();
app.MapAssistantEndpoints();
app.MapBudgetEndpoints();
app.MapInboundConnectorEndpoints();

app.Run();

public partial class Program { }
