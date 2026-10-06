namespace Dealoware.Api.Admin;

/// <summary>
/// Generic fail-closed deny for admin routes.
/// Does not reveal whether an account exists, why the session failed, or TOTP state.
/// </summary>
public static class AdminDeny
{
    public const string GenericError = "Unauthorized";

    public static IResult UnauthorizedResult() =>
        Results.Json(new { error = GenericError }, statusCode: StatusCodes.Status401Unauthorized);

    public static Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync("""{"error":"Unauthorized"}""");
    }
}
