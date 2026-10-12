namespace Dealoware.Api.Admin;

/// <summary>
/// Generic sign-in failure (UX1-A03 r2). Same status and body for unknown email,
/// wrong password, wrong TOTP, and wrong recovery code. Never names the factor.
/// </summary>
public static class AdminSignInDeny
{
    public const string GenericCopy =
        "We couldn't sign you in. Check your details and try again later. You can also reset your password.";

    public static IResult Failure() =>
        Results.Json(new { error = GenericCopy }, statusCode: StatusCodes.Status401Unauthorized);

    public static Task WriteFailureAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(
            """{"error":"We couldn't sign you in. Check your details and try again later. You can also reset your password."}""");
    }
}
