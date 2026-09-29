namespace Dealoware.Domain.AgentGateway;

using Dealoware.Domain.FieldAcl;

/// <summary>
/// Request to invoke a platform tool via the agent gateway.
/// </summary>
public sealed class ToolInvocationRequest
{
    public string ToolName { get; }
    public FieldPrincipal Principal { get; }
    public FieldResourceContext ResourceContext { get; }
    public Dictionary<string, object?> Parameters { get; }

    private ToolInvocationRequest(
        string toolName,
        FieldPrincipal principal,
        FieldResourceContext resourceContext,
        Dictionary<string, object?> parameters)
    {
        ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
        Principal = principal ?? throw new ArgumentNullException(nameof(principal));
        ResourceContext = resourceContext ?? throw new ArgumentNullException(nameof(resourceContext));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    public static ToolInvocationRequest Create(
        string toolName,
        FieldPrincipal principal,
        FieldResourceContext resourceContext,
        Dictionary<string, object?>? parameters = null)
    {
        return new ToolInvocationRequest(
            toolName,
            principal,
            resourceContext,
            parameters ?? new Dictionary<string, object?>());
    }
}
