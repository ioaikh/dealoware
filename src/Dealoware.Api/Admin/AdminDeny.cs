namespace Dealoware.Api.Admin;

/// <summary>
/// Generic fail-closed deny for admin routes.
/// Does not reveal whether an account exists, why the session failed, or TOTP state.
/// </summary>
public static class AdminDeny
{
    public const string GenericError = "Unauthorized";
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
}
