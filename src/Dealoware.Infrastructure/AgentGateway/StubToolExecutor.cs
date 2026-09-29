namespace Dealoware.Infrastructure.AgentGateway;

using Dealoware.Domain.AgentGateway;
using Dealoware.Domain.FieldAcl;

/// <summary>
/// Stub tool executor for MVP Stage C.
/// This is a platform abstraction — not raw DB access.
/// 
/// Stage C #67: Tools must return typed responses with FieldClass tags.
/// CRITICAL: No tool returns LoginEmail — LoginEmail is User-only.
/// 
/// Note: This is a stub implementation. Production would delegate to
/// actual service layer operations but still through platform abstractions.
/// </summary>
public sealed class StubToolExecutor : IToolExecutor
{
    public Task<ToolResponse> ExecuteAsync(AgentTool tool, ToolInvocationRequest request)
    {
        return tool.Name.ToLowerInvariant() switch
        {
            "get_profile" => ExecuteGetProfile(tool, request),
            "get_negotiation" => ExecuteGetNegotiation(tool, request),
            "get_strategy" => ExecuteGetStrategy(tool, request),
            "share_contact_email" => ExecuteShareContactEmail(tool, request),
            "list_artifacts" => ExecuteListArtifacts(tool, request),
            "create_offer" => ExecuteCreateOffer(tool, request),
            "accept_offer" => ExecuteAcceptOffer(tool, request),
            _ => Task.FromResult(ToolResponse.Error(tool.Name, "Unknown tool"))
        };
    }

    private static Task<ToolResponse> ExecuteGetProfile(AgentTool tool, ToolInvocationRequest request)
    {
        var fields = new List<AgentContextField>
        {
            AgentContextField.ForDisplayName("Sample User"),
            AgentContextField.ForContactEmail("contact@example.com")
        };

        return Task.FromResult(ToolResponse.Ok(tool.Name, fields));
    }

    private static Task<ToolResponse> ExecuteGetNegotiation(AgentTool tool, ToolInvocationRequest request)
    {
        var fields = new List<AgentContextField>
        {
            AgentContextField.ForDisplayName("Counterparty Name")
        };

        return Task.FromResult(ToolResponse.Ok(tool.Name, fields));
    }

    private static Task<ToolResponse> ExecuteGetStrategy(AgentTool tool, ToolInvocationRequest request)
    {
        var fields = new List<AgentContextField>
        {
            AgentContextField.ForStrategyBody("{ \"minPrice\": 100, \"maxPrice\": 500 }")
        };

        return Task.FromResult(ToolResponse.Ok(tool.Name, fields));
    }

    private static Task<ToolResponse> ExecuteShareContactEmail(AgentTool tool, ToolInvocationRequest request)
    {
        var fields = new List<AgentContextField>
        {
            AgentContextField.ForContactEmail("shared@example.com")
        };

        return Task.FromResult(ToolResponse.Ok(tool.Name, fields));
    }

    private static Task<ToolResponse> ExecuteListArtifacts(AgentTool tool, ToolInvocationRequest request)
    {
        var fields = new List<AgentContextField>
        {
            AgentContextField.ForDisplayName("Artifact 1"),
            AgentContextField.ForDisplayName("Artifact 2")
        };

        return Task.FromResult(ToolResponse.Ok(tool.Name, fields));
    }

    private static Task<ToolResponse> ExecuteCreateOffer(AgentTool tool, ToolInvocationRequest request)
    {
        var fields = new List<AgentContextField>
        {
            AgentContextField.ForDisplayName("Offer created")
        };

        return Task.FromResult(ToolResponse.Ok(tool.Name, fields));
    }

    private static Task<ToolResponse> ExecuteAcceptOffer(AgentTool tool, ToolInvocationRequest request)
    {
        var fields = new List<AgentContextField>
        {
            AgentContextField.ForContactEmail("counterparty@example.com")
        };

        return Task.FromResult(ToolResponse.Ok(tool.Name, fields));
    }
}
