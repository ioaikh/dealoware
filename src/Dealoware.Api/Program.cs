using Dealoware.Api;
using Dealoware.Api.Admin;
using Dealoware.Api.Endpoints;
using Dealoware.Infrastructure;
using Dealoware.Infrastructure.Admin;
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

// Fail closed outside Development: catch InvalidOperationException from provider selection (SQLite
// refused, TLS cert missing, weak SSL mode) and Environment.Exit(1) when running as the real
// Dealoware.Api process. An unhandled throw here aborts the runtime (exit 134/SIGABRT locally;
// some containers report 139/SIGSEGV) instead of a clean exit code 1. WebApplicationFactory hosts
// keep the throw so existing startup tests work.
try
{
    builder.Services.AddInfrastructure(
        connectionString,
        Environment.GetEnvironmentVariable,
        strictNonDevelopment: !builder.Environment.IsDevelopment());
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

builder.Services.Configure<CoreOwnerOptions>(
    builder.Configuration.GetSection(CoreOwnerOptions.SectionName));
builder.Services.Configure<AdminHostOptions>(
    builder.Configuration.GetSection(AdminHostOptions.SectionName));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminSessionMiddleware.PolicyName, policy =>
        policy.RequireAuthenticatedUser()
            .RequireRole(AdminSessionMiddleware.CoreOwnerRole));
});

try
{
    var ipHasher = IpHasher.Create(
        name => builder.Configuration[name] ?? Environment.GetEnvironmentVariable(name),
        isDevelopment: builder.Environment.IsDevelopment());
    builder.Services.AddSingleton<IIpHasher>(ipHasher);
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

builder.Services.AddAdminMail(builder.Configuration);


var rateLimitOptions = builder.Configuration
    .GetSection(AuthRateLimitOptions.SectionName)
    .Get<AuthRateLimitOptions>() ?? new AuthRateLimitOptions();

// Log rate limit configuration at startup for diagnostics
Console.WriteLine($"[RateLimiting] AuthRegister: {rateLimitOptions.AuthRegister.PermitLimit}/{rateLimitOptions.AuthRegister.WindowSeconds}s, " +
                  $"AuthToken: {rateLimitOptions.AuthToken.PermitLimit}/{rateLimitOptions.AuthToken.WindowSeconds}s");

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
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token authentication. Use 'Bearer {token}' obtained from POST /auth/token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// First in the pipeline so RemoteIpAddress is the client IP before rate limiting and endpoints run.
app.UseForwardedHeaders();

// Two-way admin host gate: admin paths only on the allowlist host; that host
// serves only /admin/* and /health.
app.UseMiddleware<AdminHostMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dealoware API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();

app.UseMiddleware<AdminSessionMiddleware>();
app.UseAuthorization();

// Development only: EnsureCreated for local SQLite. Non-Development schema changes
// use the migrate one-shot (see DatabaseMigrateCommand) — not baked into API startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
    await DatabaseSchemaBootstrap.ApplyStartupSchemaAsync(db, app.Environment.EnvironmentName);

    // Long-lived API only (this path is after the migrate early-return). PostgreSQL
    // outside Development: fail closed if the current login is privileged.
    try
    {
        var allowPrivilegedRuntimeLogin = app.Configuration.GetValue(
            RuntimeLoginPrivilegeGuard.AllowPrivilegedRuntimeLoginKey,
            false);
        await RuntimeLoginPrivilegeGuard.EnforceAsync(
            db,
            app.Environment.EnvironmentName,
            allowPrivilegedRuntimeLogin);
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
app.MapAdminMeEndpoints();
app.MapAdminReadEndpoints();

app.Run();

public partial class Program { }
