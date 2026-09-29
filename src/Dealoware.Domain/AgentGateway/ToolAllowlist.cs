namespace Dealoware.Domain.AgentGateway;

using Dealoware.Domain.FieldAcl;

/// <summary>
/// Deny-by-default tool allowlist implementation.
/// Stage C #67: Only explicitly registered tools are allowed.
/// 
/// MVP Platform Tools (example set — not exhaustive; no LoginEmail tool):
/// - get_profile: Read DisplayName, ContactEmail (no LoginEmail)
/// - get_negotiation: Read DisplayName (identity sealed pre-Accept)
/// - share_contact_email: ShareOutbound ContactEmail (requires AcceptGrant)
/// - get_strategy: Read StrategyBody (owner/OwnAgent only)
/// 
/// CRITICAL: No tool declares LoginEmail — LoginEmail is User-only, never agent context.
/// </summary>
public sealed class ToolAllowlist : IToolAllowlist
{
    private readonly Dictionary<string, AgentTool> _tools;

    public ToolAllowlist()
    {
        _tools = new Dictionary<string, AgentTool>(StringComparer.OrdinalIgnoreCase);
        RegisterMvpTools();
    }

    private void RegisterMvpTools()
    {
        Register(AgentTool.Create(
            "get_profile",
            "Get participant profile (DisplayName, ContactEmail). LoginEmail excluded.",
            FieldClassDeclaration.ForRead(FieldClass.DisplayName),
            FieldClassDeclaration.ForRead(FieldClass.ContactEmail)
        ));

        Register(AgentTool.Create(
            "get_negotiation",
            "Get negotiation details. Identity sealed pre-Accept.",
            FieldClassDeclaration.ForRead(FieldClass.DisplayName)
        ));

        Register(AgentTool.Create(
            "get_strategy",
            "Get strategy body (owner/OwnAgent only).",
            FieldClassDeclaration.ForRead(FieldClass.StrategyBody)
        ));

        Register(AgentTool.Create(
            "share_contact_email",
            "Share ContactEmail with counterparty. Requires AcceptGrant server-side.",
            FieldClassDeclaration.ForShareOutbound(FieldClass.ContactEmail)
        ));

        Register(AgentTool.Create(
            "list_artifacts",
            "List discoverable artifacts.",
            FieldClassDeclaration.ForRead(FieldClass.DisplayName)
        ));

        Register(AgentTool.Create(
            "create_offer",
            "Create an offer on a negotiation.",
            FieldClassDeclaration.ForRead(FieldClass.DisplayName)
        ));

        Register(AgentTool.Create(
            "accept_offer",
            "Accept an offer. Triggers ContactEmail ShareOutbound if AcceptGrant conditions met.",
            FieldClassDeclaration.ForShareOutbound(FieldClass.ContactEmail)
        ));
    }

    private void Register(AgentTool tool)
    {
        _tools[tool.Name] = tool;
    }

    public AgentTool? GetTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return null;

        return _tools.TryGetValue(toolName, out var tool) ? tool : null;
    }

    public bool IsAllowed(string toolName)
    {
        return GetTool(toolName) != null;
    }

    public IEnumerable<string> GetAllowedToolNames()
    {
        return _tools.Keys.AsEnumerable();
    }
}
