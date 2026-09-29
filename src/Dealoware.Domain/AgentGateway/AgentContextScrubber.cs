namespace Dealoware.Domain.AgentGateway;

using Dealoware.Domain.FieldAcl;

/// <summary>
/// Server-side response scrubber using same IFieldPolicy.Evaluate as API/DB wall.
/// Stage C #67: Denied fields stripped BEFORE model context.
/// 
/// Key behaviors:
/// - Every field runs through IFieldPolicy.Evaluate
/// - Denied fields are stripped from response
/// - LoginEmail NEVER allowed for agents (OwnAgent Deny held from Stage A)
/// - ShareOutbound(ContactEmail) requires HasAcceptGrant
/// - No parallel scrub table — uses same Domain IFieldPolicy
/// 
/// Soft OTel touchpoint: StrippedFieldCount in response for audit (no 5th Story).
/// </summary>
public sealed class AgentContextScrubber : IAgentContextScrubber
{
    private readonly IFieldPolicy _fieldPolicy;

    public AgentContextScrubber(IFieldPolicy fieldPolicy)
    {
        _fieldPolicy = fieldPolicy ?? throw new ArgumentNullException(nameof(fieldPolicy));
    }

    public ScrubbedToolResponse Scrub(
        ToolResponse response,
        FieldPrincipal principal,
        FieldResourceContext resourceContext,
        FieldAction action)
    {
        if (!response.Success)
        {
            return ScrubbedToolResponse.Create(
                response.ToolName,
                false,
                SanitizeErrorMessage(response.ErrorMessage),
                Array.Empty<AgentContextField>(),
                0);
        }

        var allowedFields = new List<AgentContextField>();
        var strippedCount = 0;

        foreach (var field in response.Fields)
        {
            var allowed = _fieldPolicy.Evaluate(principal, field.FieldClass, action, resourceContext);
            
            if (allowed)
            {
                allowedFields.Add(field);
            }
            else
            {
                strippedCount++;
            }
        }

        return ScrubbedToolResponse.Create(
            response.ToolName,
            true,
            null,
            allowedFields.AsReadOnly(),
            strippedCount);
    }

    public bool ValidateToolDeclarations(ToolResponse response, AgentTool tool, FieldAction action)
    {
        foreach (var field in response.Fields)
        {
            if (!tool.DeclaresAccess(field.FieldClass, action))
            {
                return false;
            }
        }
        return true;
    }

    private static string? SanitizeErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return errorMessage;

        var lowerMessage = errorMessage.ToLowerInvariant();
        if (lowerMessage.Contains("loginemail") || 
            lowerMessage.Contains("login_email") ||
            lowerMessage.Contains("contactemail") ||
            lowerMessage.Contains("contact_email") ||
            lowerMessage.Contains("@"))
        {
            return "Access denied";
        }

        return errorMessage;
    }
}
