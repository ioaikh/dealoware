using System.Security.Claims;

namespace Dealoware.Api.Admin;

/// <summary>
/// Stub CoreOwner identity endpoint. JSON only; no UI.
/// </summary>
public static class AdminMeEndpoints
{
    public static void MapAdminMeEndpoints(this WebApplication app)
    {
        app.MapGet("/admin/api/me", GetMe)
            .WithName("AdminMe")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static IResult GetMe(HttpContext context)
    {
        var email = context.User.FindFirstValue(ClaimTypes.Email)
                    ?? context.User.FindFirstValue(ClaimTypes.Name)
                    ?? string.Empty;

        if (string.IsNullOrEmpty(email)
            || !context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole))
        {
            return AdminDeny.UnauthorizedResult();
        }

        return Results.Json(new
        {
            principal = AdminSessionMiddleware.CoreOwnerRole,
            email
        });
    }
}
