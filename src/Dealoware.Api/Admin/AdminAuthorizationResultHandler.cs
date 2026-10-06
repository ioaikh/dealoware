using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Dealoware.Api.Admin;

/// <summary>
/// H-7: an admin authorization policy failure after the session middleware must
/// return the generic 401 body, not a 500 from a missing authentication scheme.
/// </summary>
public sealed class AdminAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _fallback = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded
            && context.Request.Path.StartsWithSegments(AdminHostMiddleware.AdminPathPrefix))
        {
            return AdminDeny.WriteUnauthorizedAsync(context);
        }

        return _fallback.HandleAsync(next, context, policy, authorizeResult);
    }
}

public static class AdminAuthorizationExtensions
{
    public static IServiceCollection AddAdminAuthorizationResultHandler(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AdminAuthorizationResultHandler>();
        return services;
    }
}
