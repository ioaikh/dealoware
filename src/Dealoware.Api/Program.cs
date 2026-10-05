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

var jwtSettings = new JwtSettings
{
    // Production fails fast on a missing, placeholder, or too-short key; other environments
    // keep the development placeholder fallback.
    SigningKey = JwtSigningKeyValidator.Validate(
        Environment.GetEnvironmentVariable(JwtSigningKeyValidator.EnvironmentVariableName)
            ?? builder.Configuration["Jwt:SigningKey"],
        builder.Environment.IsProduction()),
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
