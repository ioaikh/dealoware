using Dealoware.Api;
using Dealoware.Api.Endpoints;
using Dealoware.Infrastructure;
using Dealoware.Infrastructure.Auth;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Dealoware.Api.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

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


var rateLimitOptions = builder.Configuration
    .GetSection(AuthRateLimitOptions.SectionName)
    .Get<AuthRateLimitOptions>() ?? new AuthRateLimitOptions();

// The API runs behind a load balancer that is its only ingress, so the TCP peer is the balancer,
// not the client. Read the client IP from X-Forwarded-For (and scheme from X-Forwarded-Proto) and
// trust whichever proxy hop sends them, instead of the default loopback-only list (the balancer's
// address is not fixed). ForwardLimit stays at the default of 1: only the right-most entry, which
// the balancer appends itself, is used, so client-supplied entries to its left cannot pick the bucket.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = AuthRateLimiting.CreateOnRejected(
        fallbackRetryAfterSeconds: Math.Max(1, Math.Max(
            rateLimitOptions.AuthRegister.WindowSeconds,
            rateLimitOptions.AuthToken.WindowSeconds)));

    options.AddPolicy(AuthRateLimitOptions.RegisterPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: AuthRateLimiting.GetPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, rateLimitOptions.AuthRegister.PermitLimit),
                Window = TimeSpan.FromSeconds(Math.Max(1, rateLimitOptions.AuthRegister.WindowSeconds)),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(AuthRateLimitOptions.TokenPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: AuthRateLimiting.GetPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, rateLimitOptions.AuthToken.PermitLimit),
                Window = TimeSpan.FromSeconds(Math.Max(1, rateLimitOptions.AuthToken.WindowSeconds)),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

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

// First in the pipeline so RemoteIpAddress is the client IP before rate limiting and endpoints run.
app.UseForwardedHeaders();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dealoware API v1");
    c.RoutePrefix = "swagger";
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();

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
