namespace Dealoware.Application.Assistant.Mapping;

using Dealoware.Application.Assistant.Dtos;
using Dealoware.Domain.Assistant;

/// <summary>
/// Maps domain Assistant types to API DTOs.
/// Stage C #66: Ensures no LoginEmail or denied fields leak through.
/// </summary>
public static class AssistantMapper
{
    public static AssistantInvokeResponse ToResponse(AssistantResult result)
    {
        return new AssistantInvokeResponse
        {
            Success = result.Success,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            ResponseText = result.ResponseText,
            ToolResult = MapToolResponse(result.ToolResponse),
            StrategyUsed = MapStrategyContext(result.StrategyContext)
        };
    }

    public static AssistantCapabilitiesResponse ToResponse(AssistantCapabilities capabilities)
    {
        return new AssistantCapabilitiesResponse
        {
            AvailableTools = capabilities.AvailableTools.ToList(),
            HasStrategies = capabilities.HasStrategies,
            StrategyCount = capabilities.StrategyCount
        };
    }

    public static AssistantInvocationRequest ToDomain(AssistantInvokeRequest request)
    {
        return AssistantInvocationRequest.Create(
            strategyId: request.StrategyId,
            input: request.Input,
            toolName: request.ToolName,
            toolParameters: request.ToolParameters);
    }

    private static AssistantToolResult? MapToolResponse(
        Domain.AgentGateway.ScrubbedToolResponse? toolResponse)
    {
        if (toolResponse is null) return null;

        return new AssistantToolResult
        {
            ToolName = toolResponse.ToolName,
            Success = toolResponse.Success,
            ErrorMessage = toolResponse.ErrorMessage,
            AllowedFields = toolResponse.AllowedFields
                .Select(f => new AssistantContextField
                {
                    Name = f.Name,
                    FieldClass = f.FieldClass.Name,
                    Value = f.Value
                })
                .ToList(),
            StrippedFieldCount = toolResponse.StrippedFieldCount
        };
    }

    private static AssistantStrategyInfo? MapStrategyContext(AssistantStrategyContext? context)
    {
        if (context is null) return null;

        return new AssistantStrategyInfo
        {
            StrategyId = context.StrategyId,
            StrategyName = context.StrategyName
        };
    }
}
