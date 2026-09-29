namespace Dealoware.Domain.AgentGateway;

using Dealoware.Domain.FieldAcl;

/// <summary>
/// Interface for server-side response scrubbing before model context.
/// Stage C #67: Uses same IFieldPolicy.Evaluate as API/DB wall — no parallel scrub table.
/// </summary>
public interface IAgentContextScrubber
{
    /// <summary>
    /// Scrubs a tool response, stripping fields that the principal cannot access.
    /// Uses IFieldPolicy.Evaluate for each field — same policy as API/DB wall.
    /// </summary>
    /// <param name="response">The raw tool response to scrub.</param>
    /// <param name="principal">The agent principal (IsAgent=true).</param>
    /// <param name="resourceContext">Context for access evaluation.</param>
    /// <param name="action">The action being performed (typically Read or ShareOutbound).</param>
    /// <returns>Scrubbed response with denied fields stripped.</returns>
    ScrubbedToolResponse Scrub(
        ToolResponse response,
        FieldPrincipal principal,
        FieldResourceContext resourceContext,
        FieldAction action);

    /// <summary>
    /// Validates that a tool's response doesn't contain undeclared FieldClasses.
    /// Returns false if the response contains fields the tool didn't declare.
    /// </summary>
    bool ValidateToolDeclarations(ToolResponse response, AgentTool tool, FieldAction action);
}
