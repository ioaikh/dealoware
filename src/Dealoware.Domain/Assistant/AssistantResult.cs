namespace Dealoware.Domain.Assistant;

using Dealoware.Domain.AgentGateway;

/// <summary>
/// Result from thin OwnAgent assistant invocation.
/// Stage C #66: Response with scrubbed context (no LoginEmail/denied fields).
/// 
/// Security guarantees:
/// - No LoginEmail in any field or context
/// - No denied FieldClasses
/// - No other Participant's StrategyBody
/// - All data passed through #67 gateway + IFieldPolicy.Evaluate
/// </summary>
public sealed class AssistantResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// The assistant's response text.
    /// X1 thin: Simple string output. Fuller response models → V1.
    /// </summary>
    public string? ResponseText { get; }
    
    /// <summary>
    /// Tool invocation result if a tool was called.
    /// Scrubbed via #67 gateway — denied fields stripped.
    /// </summary>
    public ScrubbedToolResponse? ToolResponse { get; }
    
    /// <summary>
    /// Strategy context used (if any).
    /// Only present if owner's own strategy was used (OwnAgent R/W allowed).
    /// Never contains another Participant's StrategyBody.
    /// </summary>
    public AssistantStrategyContext? StrategyContext { get; }

    private AssistantResult(
        bool success,
        string? errorCode,
        string? errorMessage,
        string? responseText,
        ScrubbedToolResponse? toolResponse,
        AssistantStrategyContext? strategyContext)
    {
        Success = success;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ResponseText = responseText;
        ToolResponse = toolResponse;
        StrategyContext = strategyContext;
    }

    public static AssistantResult Ok(
        string? responseText = null,
        ScrubbedToolResponse? toolResponse = null,
        AssistantStrategyContext? strategyContext = null)
    {
        return new AssistantResult(true, null, null, responseText, toolResponse, strategyContext);
    }

    public static AssistantResult StrategyUsed(
        Guid strategyId,
        string strategyName,
        string responseText,
        ScrubbedToolResponse? toolResponse = null)
    {
        var context = new AssistantStrategyContext(strategyId, strategyName);
        return new AssistantResult(true, null, null, responseText, toolResponse, context);
    }

    public static AssistantResult ToolInvoked(ScrubbedToolResponse toolResponse)
    {
        return new AssistantResult(true, null, null, null, toolResponse, null);
    }

    public static AssistantResult StrategyNotFound()
    {
        return new AssistantResult(false, "STRATEGY_NOT_FOUND", "Strategy not found or access denied", null, null, null);
    }

    public static AssistantResult ToolNotAllowed(string toolName)
    {
        return new AssistantResult(false, "TOOL_NOT_ALLOWED", $"Tool '{toolName}' is not available", null, null, null);
    }

    public static AssistantResult GatewayError(GatewayResult gatewayResult)
    {
        return new AssistantResult(false, gatewayResult.ErrorCode, gatewayResult.ErrorMessage, null, null, null);
    }

    public static AssistantResult Error(string code, string message)
    {
        return new AssistantResult(false, code, message, null, null, null);
    }
}

/// <summary>
/// Strategy context used by the assistant.
/// Only populated when the owner's own strategy was used via OwnAgent R/W.
/// </summary>
public sealed class AssistantStrategyContext
{
    public Guid StrategyId { get; }
    public string StrategyName { get; }

    public AssistantStrategyContext(Guid strategyId, string strategyName)
    {
        StrategyId = strategyId;
        StrategyName = strategyName ?? throw new ArgumentNullException(nameof(strategyName));
    }
}
