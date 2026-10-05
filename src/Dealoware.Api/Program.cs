using Dealoware.Api.Endpoints;
using Dealoware.Infrastructure;
using Dealoware.Infrastructure.Auth;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DEALOWARE_CONNECTION_STRING")
    ?? "Data Source=dealoware.db";

builder.Services.AddInfrastructure(connectionString);

// Fail closed outside Development: catch InvalidOperationException from key validation and
// Environment.Exit(1) when running as the real Dealoware.Api process. An unhandled throw here
// aborts the runtime (exit 134/SIGABRT locally; some containers report 139/SIGSEGV) instead of a
// clean exit code 1. WebApplicationFactory hosts keep the throw so existing startup tests work.
string signingKey;
try
{
    // Every environment except Development fails fast on a missing, placeholder, or too-short key;
    // only Development keeps the placeholder fallback. The environment variable is read through
    // configuration (environment variables are a default configuration source).
    signingKey = JwtSigningKeyValidator.Validate(
        builder.Configuration[JwtSigningKeyValidator.EnvironmentVariableName]
            ?? builder.Configuration["Jwt:SigningKey"],
        requireStrictKey: !builder.Environment.IsDevelopment());
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    var entryName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
    if (string.Equals(entryName, "Dealoware.Api", StringComparison.Ordinal))
    {
        Environment.Exit(1);
    }

    throw;
}

var jwtSettings = new JwtSettings
{
    SigningKey = signingKey,
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
    await db.Database.EnsureCreatedAsync();
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
