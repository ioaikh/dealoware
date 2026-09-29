namespace Dealoware.Domain.Assistant;

using Dealoware.Domain.FieldAcl;

/// <summary>
/// Thin OwnAgent-only Strategy-driven AI Assistant runtime.
/// Stage C #66: 1:1 assistant for the owning Participant only (P6 spirit).
/// 
/// Key constraints (Spec + Security SD checklist):
/// 1. OwnAgent-only 1:1 — acts only as OwnAgent for owning Participant; never Counterparty/Stranger
/// 2. StrategyBody via FieldPolicy — consume/write only when Evaluate allows OwnAgent R/W for owner
/// 3. Mandatory bind #67 — platform tools/gateway only; no raw DB/arbitrary HTTP
/// 4. No LoginEmail — never in agent context packs or tool outputs
/// 5. Authn/IDOR fail-closed — unauth 401; wrong principal 403/404; uniform deny
/// 
/// OUT: Fuller Assistant (V1); free-form engine (V1); A5 sandbox (V4); multi-party; LLM provision.
/// </summary>
public interface IAssistantService
{
    /// <summary>
    /// Invokes the thin OwnAgent assistant for the authenticated owner.
    /// Uses platform tools via #67 gateway exclusively.
    /// </summary>
    /// <param name="ownerSub">The authenticated owner's subject identifier (from #5 principal).</param>
    /// <param name="request">The assistant invocation request.</param>
    /// <returns>Assistant result with scrubbed context (no LoginEmail/denied fields).</returns>
    Task<AssistantResult> InvokeAsync(string ownerSub, AssistantInvocationRequest request);

    /// <summary>
    /// Gets available assistant capabilities for the owner.
    /// Only returns tools available via #67 gateway that the owner can use.
    /// </summary>
    Task<AssistantCapabilities> GetCapabilitiesAsync(string ownerSub);
}
