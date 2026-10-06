using Dealoware.Infrastructure.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Api.Admin;

/// <summary>
/// Two-way host gate for the Core admin surface.
/// Admin paths are served only when Host is on the configured allowlist
/// (default admin.core.dealoware.com). The admin host serves only /admin/*
/// and /health; all other paths return 404.
/// </summary>
public sealed class AdminHostMiddleware
{
    public const string AdminPathPrefix = "/admin";

    private readonly RequestDelegate _next;
    private readonly AdminHostOptions _options;

    public AdminHostMiddleware(RequestDelegate next, IOptions<AdminHostOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var isAdminHost = IsAllowedAdminHost(context.Request.Host.Host);
        var isAdminPath = context.Request.Path.StartsWithSegments(AdminPathPrefix);
        var isHealth = context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase);

        if (isAdminPath && !isAdminHost)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        if (_options.EnforceHostValidation && isAdminHost && !isAdminPath && !isHealth)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        return _next(context);
    }

    private bool IsAllowedAdminHost(string hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName) || _options.AllowedHosts is null || _options.AllowedHosts.Count == 0)
            return false;

        foreach (var allowed in _options.AllowedHosts)
        {
            if (string.Equals(allowed, hostName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
