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
    public const string GenericConflict = "Conflict";
    public const string GenericPreconditionRequired = "PreconditionRequired";

    public static IResult UnauthorizedResult() =>
        Results.Json(new { error = GenericError }, statusCode: StatusCodes.Status401Unauthorized);

    public static IResult BadRequestResult() =>
        Results.Json(new { error = GenericBadRequest }, statusCode: StatusCodes.Status400BadRequest);

    public static IResult NotFoundResult() =>
        Results.Json(new { error = GenericNotFound }, statusCode: StatusCodes.Status404NotFound);

    /// <summary>
    /// A write prepared against a record that has changed since (Spec §5:
    /// 409 with a safe message, no silent overwrite). Says nothing of what changed.
    /// </summary>
    public static IResult ConflictResult() =>
        Results.Json(
            new { error = GenericConflict, message = AdminEditCopy.Conflict },
            statusCode: StatusCodes.Status409Conflict);

    /// <summary>A write sent without the <c>If-Match</c> it must carry.</summary>
    public static IResult PreconditionRequiredResult() =>
        Results.Json(
            new { error = GenericPreconditionRequired },
            statusCode: StatusCodes.Status428PreconditionRequired);

    /// <summary>
    /// The generic 400, with a message for each field whose value breaks a
    /// rule. Each message is a fixed sentence about the rule; no submitted or
    /// stored value is echoed, and nothing says what state the record is in.
    /// </summary>
    public static IResult ValidationResult(IReadOnlyDictionary<string, string> fields) =>
        Results.Json(new { error = GenericBadRequest, fields }, statusCode: StatusCodes.Status400BadRequest);

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
