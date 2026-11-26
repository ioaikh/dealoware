using Dealoware.Api.Admin;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Endpoints;

/// <summary>
/// The engine's one answer to a save that found its row changed since the
/// request read it: 409, in the shape the engine's routes already use for a
/// conflict (<c>{ "error": "..." }</c>).
/// <para>
/// The four admin entities carry a <c>Version</c> that every save checks.
/// The engine's own actions never move it, so until the admin console could
/// write, the check never failed. An admin write moves it; an engine request
/// that had read the row before that write then fails the check as it saves
/// (<see cref="DbUpdateConcurrencyException"/>), and without this it would be
/// answered 500. Nothing of the losing request is stored: the save that
/// failed is rolled back as a whole. No engine action's own logic is changed.
/// </para>
/// <para>
/// Only the engine's routes: under /admin the edit API answers its own 409.
/// </para>
/// </summary>
public sealed class EngineWriteConflictMiddleware
{
    public const string Message = "The record was changed by another request. Read it again and retry.";

    private readonly RequestDelegate _next;
    private readonly ILogger<EngineWriteConflictMiddleware> _logger;

    public EngineWriteConflictMiddleware(RequestDelegate next, ILogger<EngineWriteConflictMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateConcurrencyException) when (IsEngineRoute(context) && !context.Response.HasStarted)
        {
            _logger.LogInformation("An engine request found its row changed as it saved; answered 409.");
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = Message }, context.RequestAborted);
        }
    }

    private static bool IsEngineRoute(HttpContext context)
        => !context.Request.Path.StartsWithSegments(AdminHostMiddleware.AdminPathPrefix);
}
