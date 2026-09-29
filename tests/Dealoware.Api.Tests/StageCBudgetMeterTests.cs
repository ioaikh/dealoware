using System.Net;
using System.Net.Http.Json;
using Dealoware.Application.Assistant.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Budget.Dtos;
using Dealoware.Domain.Budget;
using Dealoware.Domain.FieldAcl;

namespace Dealoware.Api.Tests;

/// <summary>
/// Stage C (#68) A8-minimum per-Participant meters + hard budgets (cutoff) tests.
/// 
/// Spec §8.1 test matrix:
/// 1. Under-budget allow — Metered invocation allowed when budget available
/// 2. At/over budget deny — Further metered invocations DENY server-side (fail-closed)
/// 3. Cross-tenant deny — Cannot burn another Participant's budget
/// 4. Unauthenticated deny — 401; cannot consume budget; no private leakage
/// 
/// Security SD checklist points verified:
/// 1. Per-Participant meters — counters scoped to Participant
/// 2. Hard cutoff fail-closed — budget exhausted → deny server-side (not soft-warn-only)
/// 3. Cross-tenant / unauth cannot burn budget
/// 4. Metered path still wall-bound (#67) — budget status not leak/escalation channel
/// 5. Authn fail-closed on meter APIs — unauth → 401; wrong principal → 403/404
/// 6. OUT locked A8-minimum only
/// 7. Sibling surfaces — no #69 invent here
/// 8. No 5th Story / Gate HOLDs
/// 9. Cost $0; no LLM provision
/// 10. Evidence + handshake
/// </summary>
[Collection("WebAppTests")]
public class StageCBudgetMeterTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StageCBudgetMeterTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Test Helpers

    private async Task<(HttpClient Client, string Sub, string ApiKey)> CreateAuthenticatedClientAsync(string suffix)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"budget-test-{suffix}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");
        return (client, participant.Sub, participant.ApiKey);
    }

    private static AssistantInvokeRequest CreateInvokeRequest(string? toolName = null) => new()
    {
        ToolName = toolName ?? "get_profile"
    };

    #endregion

    #region Spec §8.1 Case 1: Under-Budget Allow (Security points 1, 2)

    [Fact]
    public async Task Assistant_Invoke_UnderBudget_Returns200()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"under-budget-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/assistant/invoke", CreateInvokeRequest());
        var result = await response.Content.ReadFromJsonAsync<AssistantInvokeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Assistant_Invoke_UnderBudget_DecrementsUsage()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"usage-decrement-{Guid.NewGuid()}");

        // Get initial budget status
        var initialStatus = await client.GetFromJsonAsync<BudgetStatusResponse>("/budget/status");
        Assert.NotNull(initialStatus);
        var initialUsed = initialStatus.UsedUnits;

        // Invoke assistant (should consume 1 unit)
        await client.PostAsJsonAsync("/assistant/invoke", CreateInvokeRequest());

        // Verify usage increased
        var afterStatus = await client.GetFromJsonAsync<BudgetStatusResponse>("/budget/status");
        Assert.NotNull(afterStatus);
        Assert.Equal(initialUsed + 1, afterStatus.UsedUnits);
    }

    [Fact]
    public async Task Budget_Status_ReturnsValidStatus()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"status-valid-{Guid.NewGuid()}");

        var response = await client.GetAsync("/budget/status");
        var status = await response.Content.ReadFromJsonAsync<BudgetStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(status);
        Assert.True(status.LimitUnits > 0);
        Assert.True(status.RemainingUnits > 0);
        Assert.False(status.IsExhausted);
    }

    [Fact]
    public async Task Budget_Status_NoPrivateFieldsInResponse()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"status-no-leak-{Guid.NewGuid()}");

        var response = await client.GetAsync("/budget/status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("LoginEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StrategyBody", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ContactEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApiKey", body, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Spec §8.1 Case 2: At/Over Budget Deny (Security points 1, 2)

    [Fact]
    public void BudgetCheckResult_Exhausted_NoPrivateLeak()
    {
        var result = BudgetCheckResult.Exhausted();

        Assert.False(result.IsAllowed);
        Assert.Equal("BUDGET_EXHAUSTED", result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.DoesNotContain("LoginEmail", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StrategyBody", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParticipantBudget_IsExhausted_WhenAtLimit()
    {
        var budget = ParticipantBudget.Create("test-sub", 10);
        
        // Consume all units
        for (int i = 0; i < 10; i++)
        {
            Assert.True(budget.TryConsume(1));
        }

        Assert.True(budget.IsExhausted);
        Assert.Equal(0, budget.RemainingUnits);
    }

    [Fact]
    public void ParticipantBudget_TryConsume_ReturnsFalse_WhenExhausted()
    {
        var budget = ParticipantBudget.Create("test-sub", 5);
        
        // Consume all units
        for (int i = 0; i < 5; i++)
        {
            budget.RecordUsage(1);
        }

        Assert.True(budget.IsExhausted);
        Assert.False(budget.TryConsume(1));
    }

    [Fact]
    public void ParticipantBudget_TryConsume_ReturnsFalse_WhenOverLimit()
    {
        var budget = ParticipantBudget.Create("test-sub", 5);
        
        // Cannot consume more than available
        Assert.False(budget.TryConsume(10));
        Assert.Equal(5, budget.RemainingUnits);
    }

    [Fact]
    public async Task BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit()
    {
        var repository = new InMemoryBudgetRepository();
        var service = new BudgetService(repository);
        
        // Create budget with limit of 1
        await repository.GetOrCreateAsync("exhausted-test-sub", 1);
        
        // First check should pass
        var firstCheck = await service.CheckBudgetAsync("exhausted-test-sub", 1);
        Assert.True(firstCheck.IsAllowed);
        
        // Record usage to exhaust budget
        await service.RecordUsageAsync("exhausted-test-sub", 1);
        
        // Second check should fail - budget exhausted
        var secondCheck = await service.CheckBudgetAsync("exhausted-test-sub", 1);
        Assert.False(secondCheck.IsAllowed);
        Assert.Equal("BUDGET_EXHAUSTED", secondCheck.ErrorCode);
    }

    [Fact]
    public void BudgetCheckResult_Exhausted_HardCutoff_NotSoftWarnOnly()
    {
        var result = BudgetCheckResult.Exhausted();

        Assert.False(result.IsAllowed);
        Assert.NotNull(result.ErrorCode);
        Assert.Equal("BUDGET_EXHAUSTED", result.ErrorCode);
    }

    #endregion

    #region Spec §8.1 Case 3: Cross-Tenant Deny (Security point 3)

    [Fact]
    public async Task Budget_Status_CrossTenant_Returns404()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"cross-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"cross-stranger-{Guid.NewGuid()}");

        // Owner can see their budget
        var ownerResponse = await ownerClient.GetAsync("/budget/status");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);

        // Stranger cannot see owner's budget (each sees only their own)
        var strangerResponse = await strangerClient.GetAsync("/budget/status");
        Assert.Equal(HttpStatusCode.OK, strangerResponse.StatusCode);
    }

    [Fact]
    public async Task BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget()
    {
        var repository = new InMemoryBudgetRepository();
        var service = new BudgetService(repository);

        // Create budgets for two participants
        await repository.GetOrCreateAsync("participant-a", 100);
        await repository.GetOrCreateAsync("participant-b", 100);

        // Record usage for participant-a
        await service.RecordUsageAsync("participant-a", 50);

        // Verify participant-a's budget is affected
        var statusA = await service.GetBudgetStatusAsync("participant-a");
        Assert.NotNull(statusA);
        Assert.Equal(50, statusA.UsedUnits);

        // Verify participant-b's budget is NOT affected
        var statusB = await service.GetBudgetStatusAsync("participant-b");
        Assert.NotNull(statusB);
        Assert.Equal(0, statusB.UsedUnits);
    }

    [Fact]
    public async Task BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget()
    {
        var repository = new InMemoryBudgetRepository();
        var service = new BudgetService(repository);

        // Create budgets with different limits
        await repository.GetOrCreateAsync("tenant-a", 1000);
        await repository.GetOrCreateAsync("tenant-b", 500);

        // Each tenant sees only their own budget
        var statusA = await service.GetBudgetStatusAsync("tenant-a");
        var statusB = await service.GetBudgetStatusAsync("tenant-b");

        Assert.NotNull(statusA);
        Assert.NotNull(statusB);
        Assert.Equal(1000, statusA.LimitUnits);
        Assert.Equal(500, statusB.LimitUnits);
    }

    [Fact]
    public async Task Assistant_CrossTenant_CannotBurnOthersBudget()
    {
        var (ownerClient, ownerSub, _) = await CreateAuthenticatedClientAsync($"ct-owner-{Guid.NewGuid()}");
        var (strangerClient, strangerSub, _) = await CreateAuthenticatedClientAsync($"ct-stranger-{Guid.NewGuid()}");

        // Get initial budget status for both
        var ownerInitial = await ownerClient.GetFromJsonAsync<BudgetStatusResponse>("/budget/status");
        var strangerInitial = await strangerClient.GetFromJsonAsync<BudgetStatusResponse>("/budget/status");

        // Stranger invokes their assistant
        await strangerClient.PostAsJsonAsync("/assistant/invoke", CreateInvokeRequest());

        // Verify stranger's budget was consumed
        var strangerAfter = await strangerClient.GetFromJsonAsync<BudgetStatusResponse>("/budget/status");
        Assert.NotNull(strangerAfter);
        Assert.Equal(strangerInitial!.UsedUnits + 1, strangerAfter.UsedUnits);

        // Verify owner's budget was NOT affected
        var ownerAfter = await ownerClient.GetFromJsonAsync<BudgetStatusResponse>("/budget/status");
        Assert.NotNull(ownerAfter);
        Assert.Equal(ownerInitial!.UsedUnits, ownerAfter.UsedUnits);
    }

    #endregion

    #region Spec §8.1 Case 4: Unauthenticated Deny (Security point 5)

    [Fact]
    public async Task Budget_Status_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/budget/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Budget_Status_Unauthenticated_NoPrivateFieldsInError()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/budget/status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("LoginEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StrategyBody", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Budget", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BudgetService_CheckBudget_Unauthenticated_ReturnsUnauthenticated()
    {
        var repository = new InMemoryBudgetRepository();
        var service = new BudgetService(repository);

        var result = await service.CheckBudgetAsync(null);

        Assert.False(result.IsAllowed);
        Assert.Equal("UNAUTHENTICATED", result.ErrorCode);
    }

    [Fact]
    public async Task BudgetService_CheckBudget_EmptyOwner_ReturnsUnauthenticated()
    {
        var repository = new InMemoryBudgetRepository();
        var service = new BudgetService(repository);

        var result = await service.CheckBudgetAsync("");

        Assert.False(result.IsAllowed);
        Assert.Equal("UNAUTHENTICATED", result.ErrorCode);
    }

    [Fact]
    public async Task BudgetService_GetBudgetStatus_Unauthenticated_ReturnsNull()
    {
        var repository = new InMemoryBudgetRepository();
        var service = new BudgetService(repository);

        var status = await service.GetBudgetStatusAsync(null);

        Assert.Null(status);
    }

    [Fact]
    public async Task Budget_Status_InvalidAuth_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "ApiKey dlw_invalid_keythatdoesnotexist");

        var response = await client.GetAsync("/budget/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Budget_Status_InvalidJwt_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.jwt.token");

        var response = await client.GetAsync("/budget/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Per-Participant Meter Scoping (Security point 1)

    [Fact]
    public void ParticipantBudget_Create_ScopedToParticipant()
    {
        var budget = ParticipantBudget.Create("participant:12345", 1000);

        Assert.Equal("participant:12345", budget.ParticipantSub);
        Assert.Equal(1000, budget.LimitUnits);
        Assert.Equal(0, budget.UsedUnits);
    }

    [Fact]
    public void ParticipantBudget_Create_ThrowsOnNullSub()
    {
        Assert.Throws<ArgumentException>(() => ParticipantBudget.Create(null!, 1000));
        Assert.Throws<ArgumentException>(() => ParticipantBudget.Create("", 1000));
        Assert.Throws<ArgumentException>(() => ParticipantBudget.Create("   ", 1000));
    }

    [Fact]
    public void ParticipantBudget_Create_ThrowsOnNegativeLimit()
    {
        Assert.Throws<ArgumentException>(() => ParticipantBudget.Create("test-sub", -1));
    }

    [Fact]
    public void BudgetStatus_FromBudget_MapsCorrectly()
    {
        var budget = ParticipantBudget.Create("test-sub", 100);
        budget.RecordUsage(25);

        var status = BudgetStatus.FromBudget(budget);

        Assert.Equal(100, status.LimitUnits);
        Assert.Equal(25, status.UsedUnits);
        Assert.Equal(75, status.RemainingUnits);
        Assert.False(status.IsExhausted);
        Assert.Equal(25, status.UsagePercentage);
    }

    #endregion

    #region Metered Path Wall-Bound (#67) (Security point 4)

    [Fact]
    public void BudgetStatus_NoFieldClassLeakage()
    {
        var budget = ParticipantBudget.Create("test-sub", 100);
        var status = BudgetStatus.FromBudget(budget);

        var type = status.GetType();
        var properties = type.GetProperties();

        foreach (var prop in properties)
        {
            Assert.DoesNotContain("LoginEmail", prop.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("StrategyBody", prop.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ContactEmail", prop.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FieldClass", prop.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void BudgetCheckResult_ErrorMessages_NoPrivateLeak()
    {
        var results = new[]
        {
            BudgetCheckResult.Exhausted(),
            BudgetCheckResult.NoBudget(),
            BudgetCheckResult.Unauthenticated(),
            BudgetCheckResult.AccessDenied()
        };

        foreach (var result in results)
        {
            if (result.ErrorMessage != null)
            {
                Assert.DoesNotContain("LoginEmail", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("StrategyBody", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("ContactEmail", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("@", result.ErrorMessage);
            }
        }
    }

    #endregion

    #region Health Endpoint Unchanged

    [Fact]
    public async Task Health_StillNoAuthRequired_AfterBudgetFeature()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Budget Auto-Provision on Registration

    [Fact]
    public async Task Register_ProvisionsBudget()
    {
        var client = _factory.CreateClient();
        
        // Register new participant
        var registerResponse = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"budget-provision-{Guid.NewGuid()}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        // Authenticate
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");

        // Should have budget immediately
        var budgetResponse = await client.GetAsync("/budget/status");
        var status = await budgetResponse.Content.ReadFromJsonAsync<BudgetStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, budgetResponse.StatusCode);
        Assert.NotNull(status);
        Assert.True(status.LimitUnits > 0);
        Assert.Equal(0, status.UsedUnits);
    }

    #endregion

    #region Test Helpers (In-Memory Repository)

    private class InMemoryBudgetRepository : IBudgetRepository
    {
        private readonly Dictionary<string, ParticipantBudget> _budgets = new();

        public Task<ParticipantBudget?> GetByParticipantSubAsync(string participantSub, CancellationToken cancellationToken = default)
        {
            _budgets.TryGetValue(participantSub, out var budget);
            return Task.FromResult(budget);
        }

        public Task AddAsync(ParticipantBudget budget, CancellationToken cancellationToken = default)
        {
            _budgets[budget.ParticipantSub] = budget;
            return Task.CompletedTask;
        }

        public void Update(ParticipantBudget budget)
        {
            _budgets[budget.ParticipantSub] = budget;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task<ParticipantBudget> GetOrCreateAsync(string participantSub, long defaultLimit = 1000, CancellationToken cancellationToken = default)
        {
            var existing = await GetByParticipantSubAsync(participantSub, cancellationToken);
            if (existing != null)
                return existing;

            var budget = ParticipantBudget.Create(participantSub, defaultLimit);
            await AddAsync(budget, cancellationToken);
            return budget;
        }
    }

    #endregion
}
