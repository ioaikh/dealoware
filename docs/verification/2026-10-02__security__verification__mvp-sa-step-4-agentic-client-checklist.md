# Security checklist — Architecture (SA) · Step 4 Provider-neutral agentic client surface

**Status:** Chief Security itemized points for Architecture step (handshake per `ops/ORG-OPS.md`). Issue **before** Architecture QA PASS. Architecture is **Security-critical**.
**Date:** 2026-10-02
**Author:** Dealoware Chief Security
**Issue:** https://github.com/ioaikh/dealoware/issues/155 · Spec Step 4 — Provider-neutral agentic client surface (design Soft HOLD SoR Soft HOLD build)
**Moment:** Product Step 4 — `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 4 · **SA-REV-STEP4-CLIENT**
**CEO unlock:** 2026-10-02 ~9:52am ET via CPM · status:in-dev · Step 3 [#148](https://github.com/ioaikh/dealoware/issues/148) CLOSED @ tip `ff707ae` · **Step 4 Spec track UNLOCKED** (design SoR only)
**Deliverable (Architecture design):** SA written architecture grounding for Spec #155 — provider-neutral negotiation/assistant API (first-party bots + external agentic clients same API); client-surface contract (auth, Participant principal, FieldPolicy/hard wall all clients, identity seal equal on bot path); one reference client path designed not built; V1 OpenAPI/webhooks framing; external systems = example client classes only. Soft HOLD build / implementation / spend Soft HOLD until a separately named CEO unlock. Expected Soft HOLD SoR under `architecture/` (DOC-FLOW SA architecture; Soft HOLD invent Stories).
**Sibling Spec Security checklist (ISSUED):** `verification/2026-10-02__security__verification__mvp-spec-step-4-agentic-client-surface-checklist.md`
**Grounding (binding cite):**
- `product/2026-10-02__product__strategy__extended-next-steps-1y.md` Step 4 (strategy What is the bound until Product Step 4 note lands)
- Option A tip `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3a/§3b (dual wall)
- Step 3 #148 CLOSED Soft HOLD SoR pack tip `ff707ae`
- Step 2 #142 CLOSED Soft HOLD SoR pack tip `c28361f` (AWS design constraints Soft HOLD provision — design only)
- Participant UI #69 CLOSED Soft HOLD SoR
- Gate **#27** CLOSED Soft HOLD SoR (do **not** reopen)
- Soft **#41** CLOSED via **#66+#67** (do **not** re-open)
**Hold:** Soft HOLD Architecture QA PASS until Security QA confirms 1–10. Soft HOLD score until this checklist Soft HOLD SoR CLEAR. Handshake Soft HOLD SoR = **qa-confirm only** after Senior+QA+Chief PASS (Gate **#25/#26/#27** pattern; **do not invent points-review Soft HOLD SoR**). Soft HOLD Spec content Soft HOLD until **CA design grounding PASS**. Soft HOLD **build / implementation / spend** until separately named CEO unlock. Soft HOLD invent connector auth / rate limits ahead of CEO Soft HOLD. Soft HOLD **invent product Stories / invent AC beyond strategy What**. Soft HOLD Marketing publish. Soft HOLD **AWS account / resource provision / spend**. Soft HOLD Step **5** until Step 4 CLOSED + explicit CEO invent-confirm. Multi-provider Soft HOLD lifts for **Step 4 Spec design SoR only** (not implementation, not spend). Soft HOLD invent MCP breadth (V5) · Soft HOLD invent multi-LLM / BYO (V4). Soft **#41** CLOSED via **#66+#67**. App Runner excluded. No MotorMarket / Cognito invent as delivered / DC4. Settlement / escrow OUT. Gate **#27** CLOSED stay closed. PoC **$0**.
**DOC-FLOW:** `verification/2026-10-02__security__verification__mvp-sa-step-4-agentic-client-checklist.md`
**Tip context:** `ff707ae`

## Scope note
Architecture Step 4 grounding shapes the **provider-neutral agentic client surface** (design Soft HOLD SoR Soft HOLD build) under Option A dual-wall fail-closed, with first-party bots and external agentic clients as first-class clients of the **same** API. Real points — not N/A. This design does **not** authorize build/implementation/spend, invent Stories, invent AC beyond strategy What, invent connector auth/rate limits, V4 multi-LLM/BYO, V5 MCP marketplace, Soft HOLD Marketing publish, Soft HOLD AWS provision/spend, Soft HOLD Step 5 Spec/AC, reopen Gate #27, App Runner greenfield, MotorMarket/DC4, settlement/escrow/checkout, or weakening dual-wall FieldPolicy / identity seal.

## Itemized security points (Architecture design must answer)

1. **Provider-neutral same-API first-class clients Soft HOLD Grok-only** — Architecture defines one provider-neutral negotiation/assistant API where first-party bots **and** external agentic clients are first-class clients of the **same** API. Reject designs that are Grok-only, provider-locked, or that fork a weaker external path vs first-party. Multi-provider Soft HOLD lifts for this **design SoR** only — Soft HOLD invent implementation / spend / live partner wiring in this Architecture. Cite strategy Step 4.

2. **Written client-surface contract Soft HOLD invent connector auth / rate limits** — Architecture's written client-surface contract covers auth, Participant principal, FieldPolicy / hard wall on **all** clients, and identity seal equal on the bot path. Soft HOLD invent connector auth schemes, rate-limit regimes, or partner-specific auth Soft HOLD ahead of separately named CEO Soft HOLD. Soft HOLD invent live partner credentials. Cite strategy Step 4 + Soft HOLD invent connector auth.

3. **Fail-closed dual-wall on all clients (Option A §3a/§3b)** — Architecture binds CEO-accepted Option A **§3a/§3b**: Platform API/DB FieldPolicy wall **and** agent/tool hard wall (same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses) on **every** client of the surface — first-party bot path and external agentic clients alike. Reject prompt-only controls, parallel ACL tables that drift from FieldPolicy, tenant shortcuts that weaken fail-closed list/discovery scrub, or client-class bypass that dumps denied FieldClasses. Cite Option A tip + Gate #27 CLOSED / Stage C #67 + Step 3 Soft HOLD SoR tip `ff707ae`.

4. **Identity seal equal on the bot path Soft HOLD invent identity shortcuts** — Architecture requires identity seal equal on the bot path vs other clients of the same API. Soft HOLD invent designs that weaken identity-until-accept for agentic clients: no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side. Soft HOLD invent client paths that leak LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into prompts, logs, webhooks, or exports without explicit Accept-gated / FieldPolicy allow. Cite Option A + Stage A/B Security PASS posture.

5. **One reference client path designed Soft HOLD invent build** — Architecture designs **one** reference client path (not built). Soft HOLD invent implementation / SDK ship / deploy / paid plugins / non-local client runtime as delivered in this Architecture. Soft HOLD invent Stories for building the reference path. Soft HOLD invent AC that require a running client. Cite CEO unlock Soft HOLD build + Soft HOLD invent Stories.

6. **V1 OpenAPI/webhooks Soft HOLD invent V4/V5** — Architecture's delivery primary framing is **V1** OpenAPI/webhooks. Soft HOLD invent multi-LLM / BYO (V4) and Soft HOLD invent MCP breadth / marketplace (V5) as in-scope for Step 4. Soft HOLD invent designs that presuppose V4/V5 delivery. Cite strategy Step 4 OUT.

7. **External systems = example client classes only Soft HOLD invent live partners** — Architecture may name external systems as **example client classes only**. Soft HOLD invent live partner integrations, partner NDAs, partner-specific Secrets Soft HOLD, or Soft HOLD invent AC that require a named live partner. Soft HOLD invent Marketing publish of partner names as delivered partners. Cite Soft HOLD Marketing + Soft HOLD invent Stories.

8. **Soft HOLD AWS provision; inherit Step 2 design constraints only** — Architecture may cite Step 2 AWS Soft HOLD SoR @ tip `c28361f` as **design constraints only**. Soft HOLD AWS account create / resource provision / spend / paid plugins / non-local deploy. Any later provision / spend → escalate **CA → CPM → COO → CEO**. Soft HOLD invent build Soft HOLD stands. PoC **$0**. Cite #142 CLOSED + Soft HOLD AWS.

9. **OUT / Soft HOLD locked pack** — Architecture OUT list must include: Soft HOLD invent Stories; Soft HOLD invent AC beyond strategy What; Soft HOLD build/implementation/spend until separately named CEO unlock; Soft HOLD invent connector auth / rate limits; Soft HOLD invent V4 multi-LLM/BYO; Soft HOLD invent V5 MCP; Soft HOLD Marketing publish; Soft HOLD AWS provision/spend; Soft HOLD invent Spec/AC Step **5** until Step 4 CLOSED + explicit CEO invent-confirm; settlement/escrow/checkout; Gate **#27** CLOSED stay closed; Soft **#41** CLOSED via #66+#67; App Runner; MotorMarket/DC4; Cognito as delivered; PoC **$0**. Cost/critical → COO → CEO.

10. **Traceability + handshake Soft HOLD SoR pattern** — Architecture cites strategy Step 4 + Option A §3a/§3b + Step 3 #148 tip `ff707ae` + Step 2 #142 tip `c28361f` + #69 Participant boundary + Gate #27 CLOSED + Soft #41 CLOSED via #66+#67. Architecture QA must **not** PASS until Security QA confirms these points via `…mvp-sa-step-4-agentic-client-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** (Gate #25/#26/#27 pattern) — **do not invent points-review Soft HOLD SoR**. Soft HOLD Arch QA until Soft HOLD SoR CLEAR + Security QA. Soft HOLD CA formal content PASS Soft HOLD until checklist Soft HOLD SoR CLEAR + Senior Architect answers § Security + Security QA → Chief PASS (then Spec Soft HOLD until CA design grounding PASS per Chief Spec). Sibling Spec checklist remains separate Soft HOLD SoR / Soft HOLD Spec QA track.

## Handshake next
1. Soft HOLD SoR twin (this checklist) via Docs **now**; Soft HOLD score until Soft HOLD SoR CLEAR.
2. Senior Architect answers points in the Step 4 client-surface architecture under `architecture/` § Security (cite sections / evidence) — Soft HOLD invent Stories; Soft HOLD invent build; Soft HOLD provision.
3. Senior Security may file points-review (content OK) — **handshake Soft HOLD SoR = qa-confirm only** (do **not** Soft HOLD SoR points-review).
4. Security QA PASS to Chief Security (or further instructions) → `…mvp-sa-step-4-agentic-client-qa-confirm.md`.
5. Chief Security PASS/HOLD to CPM + Chief Architect (+ Spec Soft HOLD until CA grounding PASS).
6. Later handshake Soft HOLD SoR **qa-confirm only** after Senior+QA+Chief PASS.
7. Sibling Spec checklist remains separate Soft HOLD SoR / Soft HOLD Spec QA track.

## Cost/critical
No AWS / IdP / LLM / paid observability / connector spend without COO → CEO. Soft HOLD build Soft HOLD stands. Soft HOLD invent Stories stands. Soft HOLD Marketing publish stands. Soft HOLD AWS provision stands. Soft HOLD Step 5 stands. Multi-provider Soft HOLD lifts for Step 4 Spec design SoR only (not implementation, not spend). App Runner excluded. Gate #27 CLOSED. Soft #41 CLOSED. PoC **$0**.
