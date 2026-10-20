namespace Dealoware.Api.Admin;

/// <summary>
/// Generic fail-closed deny for admin routes.
/// Does not reveal whether an account exists, why the session failed, or TOTP state.
/// </summary>
public static class AdminDeny
{
    public const string GenericError = "Unauthorized";
    public const string GenericForbidden = "Forbidden";

    public const string GenericBadRequest = "BadRequest";
    public const string GenericNotFound = "NotFound";

    public static IResult UnauthorizedResult() =>
        Results.Json(new { error = GenericError }, statusCode: StatusCodes.Status401Unauthorized);

    public static IResult BadRequestResult() =>
        Results.Json(new { error = GenericBadRequest }, statusCode: StatusCodes.Status400BadRequest);

    public static IResult NotFoundResult() =>
        Results.Json(new { error = GenericNotFound }, statusCode: StatusCodes.Status404NotFound);

    public static Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync("""{"error":"Unauthorized"}""");
    }

    public static IResult ForbiddenResult() =>
        Results.Json(new { error = GenericForbidden }, statusCode: StatusCodes.Status403Forbidden);

    public static Task WriteForbiddenAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/admin/api/", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("""{"error":"Forbidden"}""");
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        return context.Response.WriteAsync(
            """<!DOCTYPE html><html lang="en"><head><meta charset="utf-8"><title>Forbidden</title></head><body><p>Forbidden</p></body></html>""");
    }
}
