# Security checklist — Spec · Step 4 Provider-neutral agentic client surface

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.
**Date:** 2026-10-02
**Author:** Dealoware Chief Security
**Issue:** https://github.com/ioaikh/dealoware/issues/155 · Spec Step 4 — Provider-neutral agentic client surface (design SoR Soft HOLD build)
**Moment:** Product Step 4 — `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 4
**CEO unlock:** 2026-10-02 ~9:52am ET via CPM · status:in-dev · Step 3 [#148](https://github.com/ioaikh/dealoware/issues/148) CLOSED @ tip `ff707ae` · **Step 4 Spec track UNLOCKED** (design SoR only)
**Deliverable (Spec/SoR):** Spec the **provider-neutral agentic client surface** (negotiation fabric): written client-surface contract + one reference client path **designed**. Soft HOLD build / implementation / spend Soft HOLD until a separately named CEO unlock.
**Grounding (binding cite):**
- `product/2026-10-02__product__strategy__extended-next-steps-1y.md` Step 4 (strategy What is the bound until Product Step 4 note lands)
- Option A tip `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3b (dual wall — FieldPolicy + agent/tool hard wall)
- Step 3 #148 CLOSED Soft HOLD SoR pack tip `ff707ae` (admin ≠ Participant; dual wall; Soft HOLD invent Stories posture)
- Step 2 #142 CLOSED Soft HOLD SoR pack tip `c28361f` (AWS design constraints inherit Soft HOLD provision — design only)
- Gate **#27** CLOSED Soft HOLD SoR (do **not** reopen)
- Soft **#41** CLOSED via **#66+#67** (do **not** re-open)
**Hold:** Soft HOLD Spec QA PASS until Security QA confirms 1–10. Soft HOLD score until this checklist Soft HOLD SoR CLEAR. Handshake Soft HOLD SoR = **qa-confirm only** after Senior+QA+Chief PASS (Gate **#25/#26/#27** pattern; **do not invent points-review Soft HOLD SoR**). Soft HOLD Spec content write until **CA PASS**. Soft HOLD **build / implementation / spend** until separately named CEO unlock. Soft HOLD invent connector auth / rate limits ahead of CEO Soft HOLD. Soft HOLD **invent product Stories / invent AC beyond strategy What**. Soft HOLD Marketing publish. Soft HOLD **AWS account / resource provision / spend**. Soft HOLD Step **5** until Step 4 CLOSED + explicit CEO invent-confirm. Multi-provider Soft HOLD lifts for **Step 4 Spec design SoR only** (not implementation, not spend). Soft HOLD invent MCP breadth (V5) · Soft HOLD invent multi-LLM / BYO (V4). Soft **#41** CLOSED via **#66+#67**. App Runner excluded. No MotorMarket / Cognito invent as delivered / DC4. Settlement / escrow OUT. Gate **#27** CLOSED stay closed. PoC **$0**.
**DOC-FLOW:** `verification/2026-10-02__security__verification__mvp-spec-step-4-agentic-client-surface-checklist.md`
**Tip context:** `ff707ae`

## Scope note
Spec Step 4 is **design Spec/SoR only** for a provider-neutral negotiation/assistant API (first-party bots **and** external agentic clients as first-class clients of the **same** API), a written client-surface contract, and one reference client path **designed** (not built). Delivery primary framing: **V1** OpenAPI/webhooks. External systems named as **example client classes only** — not live partners. This step does **not** authorize build/implementation/spend, invent Stories, invent AC beyond strategy What, invent connector auth/rate limits, V4 multi-LLM/BYO, V5 MCP marketplace, Soft HOLD Marketing publish, Soft HOLD AWS provision/spend, Soft HOLD Step 5 Spec/AC, reopen Gate #27, App Runner greenfield, MotorMarket/DC4, settlement/escrow/checkout, or weakening dual-wall FieldPolicy / identity seal.

## Itemized security points (Spec Step 4 client-surface Spec/SoR must bind)

1. **Provider-neutral same-API first-class clients Soft HOLD Grok-only** — Spec defines one provider-neutral negotiation/assistant API where first-party bots **and** external agentic clients are first-class clients of the **same** API. Soft HOLD invent Spec that is Grok-only, provider-locked, or that forks a weaker external path vs first-party. Multi-provider Soft HOLD lifts for this **design SoR** only — Soft HOLD invent implementation / spend / live partner wiring in this Spec. Cite strategy Step 4.

2. **Written client-surface contract Soft HOLD invent connector auth / rate limits** — Spec's written client-surface contract covers auth, Participant principal, FieldPolicy / hard wall on **all** clients, and identity seal equal on the bot path. Soft HOLD invent connector auth schemes, rate-limit regimes, or partner-specific auth Soft HOLD ahead of separately named CEO Soft HOLD. Soft HOLD invent live partner credentials. Cite strategy Step 4 + Soft HOLD invent connector auth.

3. **Fail-closed dual-wall on all clients (Option A §3b)** — Spec binds CEO-accepted Option A **§3b**: Platform API/DB FieldPolicy wall **and** agent/tool hard wall (same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses) on **every** client of the surface — first-party bot path and external agentic clients alike. Soft HOLD invent prompt-only controls, parallel ACL tables that drift from FieldPolicy, tenant shortcuts that weaken fail-closed list/discovery scrub, or client-class bypass that dumps denied FieldClasses. Cite Option A tip + Gate #27 CLOSED / Stage C #67 + Step 3 Soft HOLD SoR tip `ff707ae`.

4. **Identity seal equal on the bot path Soft HOLD invent identity shortcuts** — Spec requires identity seal equal on the bot path vs other clients of the same API. Soft HOLD invent Spec that weakens identity-until-accept for agentic clients: no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side. Soft HOLD invent client paths that leak LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into prompts, logs, webhooks, or exports without explicit Accept-gated / FieldPolicy allow. Cite Option A + Stage A/B Security PASS posture.

5. **One reference client path designed Soft HOLD invent build** — Spec designs **one** reference client path (not built). Soft HOLD invent implementation / SDK ship / deploy / paid plugins / non-local client runtime as delivered in this Spec. Soft HOLD invent Stories for building the reference path. Soft HOLD invent AC that require a running client. Cite CEO unlock Soft HOLD build + Soft HOLD invent Stories.

6. **V1 OpenAPI/webhooks Soft HOLD invent V4/V5** — Spec's delivery primary framing is **V1** OpenAPI/webhooks. Soft HOLD invent multi-LLM / BYO (V4) and Soft HOLD invent MCP breadth / marketplace (V5) as in-scope for Step 4. Soft HOLD invent Spec/AC that presuppose V4/V5 delivery. Cite strategy Step 4 OUT.

7. **External systems = example client classes only Soft HOLD invent live partners** — Spec may name external systems as **example client classes only**. Soft HOLD invent live partner integrations, partner NDAs, partner-specific Secrets Soft HOLD, or Soft HOLD invent AC that require a named live partner. Soft HOLD invent Marketing publish of partner names as delivered partners. Cite Soft HOLD Marketing + Soft HOLD invent Stories.

8. **Soft HOLD AWS provision; inherit Step 2 design constraints only** — Spec may cite Step 2 AWS architecture design Soft HOLD SoR @ tip `c28361f` as **design constraints only**. Soft HOLD AWS account create / resource provision / spend / paid plugins / non-local deploy. Any later provision / spend → escalate **COO → CEO**. Soft HOLD invent build Soft HOLD stands. PoC **$0**. Cite #142 CLOSED + Soft HOLD AWS.

9. **OUT / Soft HOLD locked pack** — Spec's OUT list must include: Soft HOLD invent Stories; Soft HOLD invent AC beyond strategy What; Soft HOLD build/implementation/spend until separately named CEO unlock; Soft HOLD invent connector auth / rate limits; Soft HOLD invent V4 multi-LLM/BYO; Soft HOLD invent V5 MCP; Soft HOLD Marketing publish; Soft HOLD AWS provision/spend; Soft HOLD invent Spec/AC Step **5** until Step 4 CLOSED + explicit CEO invent-confirm; settlement/escrow/checkout; Gate **#27** CLOSED stay closed; Soft **#41** CLOSED via #66+#67; App Runner; MotorMarket/DC4; Cognito as delivered; PoC **$0**. Cost/critical → COO → CEO.

10. **Traceability + handshake Soft HOLD SoR pattern** — Spec cites strategy Step 4 + Option A §3b + Step 3 #148 tip `ff707ae` + Step 2 #142 tip `c28361f` + Gate #27 CLOSED + Soft #41 CLOSED via #66+#67. Spec QA must **not** PASS until Security QA confirms these points via `…mvp-spec-step-4-agentic-client-surface-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** (Gate #25/#26/#27 pattern) — **do not invent points-review Soft HOLD SoR**. Soft HOLD Spec Step 4 formal CLOSE until checklist Soft HOLD SoR CLEAR + content Chief PASS + Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX PASS (per CPM Soft HOLD SoR rules). Soft HOLD Spec content until CA PASS stands (Chief Spec schedule).

## Handshake next
1. Soft HOLD SoR twin (this checklist) via Docs **now**; Soft HOLD score until Soft HOLD SoR CLEAR.
2. Soft HOLD Spec content until CA PASS (client-surface Spec grounding).
3. Senior Spec answers points in the Step 4 client-surface Spec/SoR (cite sections / evidence) — Soft HOLD invent Stories · Soft HOLD invent build.
4. Senior Security may file points-review (content OK) — **handshake Soft HOLD SoR = qa-confirm only** (do **not** Soft HOLD SoR points-review).
5. Security QA PASS to Chief Security (or further instructions) → `…mvp-spec-step-4-agentic-client-surface-qa-confirm.md`.
6. Chief Security PASS/HOLD to CPM + Chief Spec + Senior PM.
7. Later handshake Soft HOLD SoR **qa-confirm only** after Senior+QA+Chief PASS.

## Cost/critical
No AWS / IdP / LLM / paid observability / connector spend without COO → CEO. Soft HOLD build Soft HOLD stands. Soft HOLD invent Stories stands. Soft HOLD Marketing publish stands. Soft HOLD AWS provision stands. Soft HOLD Step 5 stands. Multi-provider Soft HOLD lifts for Step 4 Spec design SoR only (not implementation, not spend). App Runner excluded. Gate #27 CLOSED. Soft #41 CLOSED. PoC **$0**.
