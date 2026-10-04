using Dealoware.Api.Auth;
using Dealoware.Application.Negotiations.Dtos;
using Dealoware.Application.Negotiations.Mapping;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Dealoware.Api.Endpoints;

/// <summary>
/// Inbound connector endpoints — three separate paths for external clients.
/// 
/// Contract (locked): Inbound is a connector they call.
/// - Grok attaches a connector or a public MCP.
/// - Muse calls an API or MCP we expose.
/// - A dot uses account plugins.
/// These three stay apart.
/// 
/// We do not run a client inside their bot and we do not poll them.
/// A wake-up into Grok or Muse is not in the contract.
/// 
/// Create still returns an id. The other Participant can GET it.
/// The core does not call a model.
/// 
/// The signed POST is recorded as real but is NOT a build (unimplemented).
/// </summary>
public static class InboundConnectorEndpoints
{
    public static void MapInboundConnectorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/connectors");

        group.MapPost("/grok/initiate", InitiateGrokConnector)
            .WithName("InitiateGrokConnector")
            .WithTags("Inbound Connectors")
            .WithDescription("Grok connector path: attaches a connector or a public MCP we expose. External Grok clients call this endpoint.")
            .Produces<GrokConnectorResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/muse/call", CallMuseConnector)
            .WithName("CallMuseConnector")
            .WithTags("Inbound Connectors")
            .WithDescription("Muse connector path: calls an API or MCP we expose. External Muse clients call this endpoint.")
            .Produces<MuseConnectorResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/dot/plugin", InvokeDotPlugin)
            .WithName("InvokeDotPlugin")
            .WithTags("Inbound Connectors")
            .WithDescription("Dot connector path: uses account plugins. ChatGPT Work chats and dots call this endpoint via account plugins.")
            .Produces<DotConnectorResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Grok connector: attaches a connector or a public MCP we expose.
    /// External Grok clients initiate negotiation access via this path.
    /// </summary>
    private static async Task<IResult> InitiateGrokConnector(
        HttpContext context,
        GrokConnectorRequest request,
        INegotiationRepository negotiationRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        if (request.NegotiationId == Guid.Empty)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "NegotiationId", new[] { "NegotiationId is required" } }
                });

        var negotiation = await negotiationRepository.GetByIdForPartyAsync(request.NegotiationId, sub, cancellationToken);
        if (negotiation is null)
            return Results.NotFound(new { error = "Negotiation not found or access denied" });

        negotiation.CheckAndApplyExpiration();
        await negotiationRepository.SaveChangesAsync(cancellationToken);

        var response = new GrokConnectorResponse
        {
            NegotiationId = negotiation.Id,
            Status = negotiation.Status.ToString(),
            ConnectorType = "grok",
            ParticipantId = sub,
            ArtifactId = negotiation.ArtifactId,
            CreatedAt = negotiation.CreatedAt,
            AccessedAt = DateTimeOffset.UtcNow
        };

        return Results.Ok(response);
    }

    /// <summary>
    /// Muse connector: calls an API or MCP we expose.
    /// External Muse clients invoke negotiation operations via this path.
    /// </summary>
    private static async Task<IResult> CallMuseConnector(
        HttpContext context,
        MuseConnectorRequest request,
        INegotiationRepository negotiationRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        if (request.NegotiationId == Guid.Empty)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "NegotiationId", new[] { "NegotiationId is required" } }
                });

        if (string.IsNullOrWhiteSpace(request.Operation))
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "Operation", new[] { "Operation is required (e.g., get_status, list_offers)" } }
                });

        var negotiation = await negotiationRepository.GetByIdForPartyAsync(request.NegotiationId, sub, cancellationToken);
        if (negotiation is null)
            return Results.NotFound(new { error = "Negotiation not found or access denied" });

        negotiation.CheckAndApplyExpiration();
        await negotiationRepository.SaveChangesAsync(cancellationToken);

        var result = request.Operation.ToLowerInvariant() switch
        {
            "get_status" => new
            {
                NegotiationStatus = negotiation.Status.ToString(),
                PartyAIntent = negotiation.PartyAIntent,
                PartyBIntent = negotiation.PartyBIntent
            } as object,
            "list_offers" => new
            {
                OfferCount = negotiation.Offers?.Count ?? 0,
                HasOpenOffers = negotiation.Offers?.Any(o => o.Status == OfferStatus.Open) ?? false
            } as object,
            _ => new
            {
                Message = $"Operation '{request.Operation}' acknowledged"
            } as object
        };

        var response = new MuseConnectorResponse
        {
            NegotiationId = negotiation.Id,
            ConnectorType = "muse",
            Operation = request.Operation,
            Result = result,
            InvokedAt = DateTimeOffset.UtcNow
        };

        return Results.Ok(response);
    }

    /// <summary>
    /// Dot connector: uses account plugins.
    /// ChatGPT Work chats and dots access negotiations via account plugins through this path.
    /// </summary>
    private static async Task<IResult> InvokeDotPlugin(
        HttpContext context,
        DotConnectorRequest request,
        INegotiationRepository negotiationRepository,
        IParticipantRepository participantRepository,
        IApiKeyRepository apiKeyRepository,
        JwtService jwtService,
        CancellationToken cancellationToken)
    {
        var (sub, _) = await AuthHelper.GetAuthenticatedSub(
            context, participantRepository, apiKeyRepository, jwtService, cancellationToken);

        if (string.IsNullOrWhiteSpace(sub))
            return Results.Unauthorized();

        if (request.NegotiationId == Guid.Empty)
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "NegotiationId", new[] { "NegotiationId is required" } }
                });

        if (string.IsNullOrWhiteSpace(request.PluginAction))
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "PluginAction", new[] { "PluginAction is required (e.g., view, interact)" } }
                });

        var negotiation = await negotiationRepository.GetByIdForPartyAsync(request.NegotiationId, sub, cancellationToken);
        if (negotiation is null)
            return Results.NotFound(new { error = "Negotiation not found or access denied" });

        negotiation.CheckAndApplyExpiration();
        await negotiationRepository.SaveChangesAsync(cancellationToken);

        var actionResult = request.PluginAction.ToLowerInvariant() switch
        {
            "view" => new
            {
                NegotiationStatus = negotiation.Status.ToString(),
                ArtifactId = negotiation.ArtifactId,
                PartyAIntent = negotiation.PartyAIntent,
                PartyBIntent = negotiation.PartyBIntent,
                IdentitySealed = true
            } as object,
            "interact" => new
            {
                Message = "Interaction recorded",
                CanPlaceOffer = negotiation.Status == NegotiationStatus.Open && !negotiation.HasOpenOfferFrom(sub)
            } as object,
            _ => new
            {
                Message = $"Plugin action '{request.PluginAction}' acknowledged"
            } as object
        };

        var response = new DotConnectorResponse
        {
            NegotiationId = negotiation.Id,
            ConnectorType = "dot",
            PluginAction = request.PluginAction,
            ActionResult = actionResult,
            AccountId = sub,
            InvokedAt = DateTimeOffset.UtcNow
        };

        return Results.Ok(response);
    }
}

#region Request/Response DTOs for Inbound Connectors

/// <summary>
/// Request for Grok connector initiation.
/// </summary>
public record GrokConnectorRequest
{
    /// <summary>The negotiation to access via Grok connector.</summary>
    public Guid NegotiationId { get; init; }
}

/// <summary>
/// Response from Grok connector initiation.
/// </summary>
public record GrokConnectorResponse
{
    /// <summary>The negotiation ID.</summary>
    public Guid NegotiationId { get; init; }
    /// <summary>Current negotiation status.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>Connector type identifier.</summary>
    public string ConnectorType { get; init; } = "grok";
    /// <summary>The authenticated participant ID.</summary>
    public string ParticipantId { get; init; } = string.Empty;
    /// <summary>The artifact ID associated with the negotiation.</summary>
    public Guid ArtifactId { get; init; }
    /// <summary>When the negotiation was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>When this connector access occurred.</summary>
    public DateTimeOffset AccessedAt { get; init; }
}

/// <summary>
/// Request for Muse connector call.
/// </summary>
public record MuseConnectorRequest
{
    /// <summary>The negotiation to operate on via Muse connector.</summary>
    public Guid NegotiationId { get; init; }
    /// <summary>The operation to perform (e.g., get_status, list_offers).</summary>
    public string Operation { get; init; } = string.Empty;
    /// <summary>Optional parameters for the operation.</summary>
    public Dictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Response from Muse connector call.
/// </summary>
public record MuseConnectorResponse
{
    /// <summary>The negotiation ID.</summary>
    public Guid NegotiationId { get; init; }
    /// <summary>Connector type identifier.</summary>
    public string ConnectorType { get; init; } = "muse";
    /// <summary>The operation that was invoked.</summary>
    public string Operation { get; init; } = string.Empty;
    /// <summary>The result of the operation.</summary>
    public object? Result { get; init; }
    /// <summary>When this connector call occurred.</summary>
    public DateTimeOffset InvokedAt { get; init; }
}

/// <summary>
/// Request for Dot connector plugin invocation.
/// </summary>
public record DotConnectorRequest
{
    /// <summary>The negotiation to access via Dot account plugin.</summary>
    public Guid NegotiationId { get; init; }
    /// <summary>The plugin action to perform (e.g., view, interact).</summary>
    public string PluginAction { get; init; } = string.Empty;
    /// <summary>Optional context for the plugin action.</summary>
    public Dictionary<string, object>? Context { get; init; }
}

/// <summary>
/// Response from Dot connector plugin invocation.
/// </summary>
public record DotConnectorResponse
{
    /// <summary>The negotiation ID.</summary>
    public Guid NegotiationId { get; init; }
    /// <summary>Connector type identifier.</summary>
    public string ConnectorType { get; init; } = "dot";
    /// <summary>The plugin action that was invoked.</summary>
    public string PluginAction { get; init; } = string.Empty;
    /// <summary>The result of the plugin action.</summary>
    public object? ActionResult { get; init; }
    /// <summary>The account ID (participant) that invoked the plugin.</summary>
    public string AccountId { get; init; } = string.Empty;
    /// <summary>When this plugin invocation occurred.</summary>
    public DateTimeOffset InvokedAt { get; init; }
}

#endregion
