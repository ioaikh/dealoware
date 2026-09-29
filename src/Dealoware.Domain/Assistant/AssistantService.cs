namespace Dealoware.Domain.Assistant;

using Dealoware.Domain.AgentGateway;
using Dealoware.Domain.Budget;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Strategies;

/// <summary>
/// Thin OwnAgent-only Strategy-driven AI Assistant runtime implementation.
/// Stage C #66: Mandatory bind to #67 gateway; OwnAgent 1:1 only.
/// Stage C #68: Per-Participant meters + hard cutoff (fail-closed).
/// 
/// Security enforcement:
/// 1. OwnAgent-only 1:1 — acts only for owning Participant; never Counterparty/Stranger
/// 2. StrategyBody via FieldPolicy — consume via IFieldPolicy.Evaluate OwnAgent R/W
/// 3. Mandatory #67 bind — all tool I/O via IAgentGateway only; no raw DB/HTTP
/// 4. No LoginEmail — agent principal never sees LoginEmail (policy enforced)
/// 5. Authn fail-closed — requires validated ownerSub from #5 principal
/// 6. #68 Hard cutoff — budget exhausted → deny server-side (not soft-warn-only)
/// 
/// OUT: Fuller Assistant (V1); free-form engine (V1); A5 sandbox (V4); LLM provision.
/// Soft OTel/audit touchpoints only — no 5th Story.
/// </summary>
public sealed class AssistantService : IAssistantService
{
    private readonly IAgentGateway _gateway;
    private readonly IStrategyRepository _strategyRepository;
    private readonly IFieldPolicy _fieldPolicy;
    private readonly IBudgetService? _budgetService;

    public AssistantService(
        IAgentGateway gateway,
        IStrategyRepository strategyRepository,
        IFieldPolicy fieldPolicy,
        IBudgetService? budgetService = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _strategyRepository = strategyRepository ?? throw new ArgumentNullException(nameof(strategyRepository));
        _fieldPolicy = fieldPolicy ?? throw new ArgumentNullException(nameof(fieldPolicy));
        _budgetService = budgetService;
    }

    public async Task<AssistantResult> InvokeAsync(string ownerSub, AssistantInvocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(ownerSub))
        {
            return AssistantResult.Error("INVALID_OWNER", "Owner subject identifier required");
        }

        var agentPrincipal = FieldPrincipal.Agent(ownerSub);
        var resourceContext = FieldResourceContext.ForSelfProfile(ownerSub);

        AssistantStrategyContext? strategyContext = null;
        if (request.StrategyId.HasValue)
        {
            var strategy = await _strategyRepository.GetByIdForOwnerAsync(
                request.StrategyId.Value, 
                ownerSub);
            
            if (strategy is null)
            {
                return AssistantResult.StrategyNotFound();
            }

            var canReadStrategy = _fieldPolicy.Evaluate(
                agentPrincipal,
                FieldClass.StrategyBody,
                FieldAction.Read,
                resourceContext);

            if (!canReadStrategy)
            {
                return AssistantResult.StrategyNotFound();
            }

            strategyContext = new AssistantStrategyContext(strategy.Id, strategy.Name ?? "Unnamed Strategy");
        }

        if (!string.IsNullOrWhiteSpace(request.ToolName))
        {
            if (!_gateway.IsToolAvailable(request.ToolName))
            {
                return AssistantResult.ToolNotAllowed(request.ToolName);
            }

            // Stage C #68: Hard cutoff check BEFORE tool invocation
            // Budget exhausted → deny server-side (fail-closed)
            if (_budgetService is not null)
            {
                var budgetCheck = await _budgetService.CheckBudgetAsync(ownerSub);
                if (!budgetCheck.IsAllowed)
                {
                    return budgetCheck.ErrorCode switch
                    {
                        "BUDGET_EXHAUSTED" => AssistantResult.BudgetExhausted(),
                        "NO_BUDGET" => AssistantResult.NoBudget(),
                        _ => AssistantResult.Error(budgetCheck.ErrorCode ?? "BUDGET_ERROR",
                            budgetCheck.ErrorMessage ?? "Budget check failed")
                    };
                }
            }

            var toolRequest = ToolInvocationRequest.Create(
                request.ToolName,
                agentPrincipal,
                resourceContext,
                request.ToolParameters);

            var gatewayResult = await _gateway.InvokeToolAsync(toolRequest);

            if (!gatewayResult.Success)
            {
                return AssistantResult.GatewayError(gatewayResult);
            }

            // Stage C #68: Record usage AFTER successful tool invocation
            if (_budgetService is not null)
            {
                await _budgetService.RecordUsageAsync(ownerSub, 1);
            }

            if (strategyContext != null)
            {
                return AssistantResult.StrategyUsed(
                    strategyContext.StrategyId,
                    strategyContext.StrategyName,
                    $"Tool '{request.ToolName}' invoked with strategy context.",
                    gatewayResult.Response);
            }

            return AssistantResult.ToolInvoked(gatewayResult.Response!);
        }

        if (strategyContext != null)
        {
            return AssistantResult.StrategyUsed(
                strategyContext.StrategyId,
                strategyContext.StrategyName,
                "Strategy context loaded for OwnAgent assistant.");
        }

        return AssistantResult.Ok(
            responseText: "OwnAgent assistant ready. Use a tool or provide strategy context.");
    }

    public async Task<AssistantCapabilities> GetCapabilitiesAsync(string ownerSub)
    {
        if (string.IsNullOrWhiteSpace(ownerSub))
        {
            return AssistantCapabilities.Create(Array.Empty<string>(), 0);
        }

        var availableTools = _gateway.GetAvailableToolNames();

        var strategies = await _strategyRepository.GetByOwnerAsync(ownerSub);

        return AssistantCapabilities.Create(availableTools, strategies.Count);
    }
}
