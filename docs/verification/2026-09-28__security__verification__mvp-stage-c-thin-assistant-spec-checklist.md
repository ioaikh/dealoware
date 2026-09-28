# Security checklist — Spec · MVP Stage C #66 Thin Strategy-driven AI Assistant runtime (X1 thin)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/66 · Thin Strategy-driven AI Assistant runtime (X1 thin)  
**Parent:** #18 · Option A · Stage C named slice  
**Siblings:** #67 (hard wall — **mandatory bind**) · #68 · #69 — keep separate Spec; cross-ref only  
**Architecture:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` (§3b thin Assistant) + Option A tip  
**Prior Security PASS (SA step):** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Baselines:** Soft **#41 Assistant OUT** → this Story + #67 close it; Stage A/B on `main` @ `ca827a2`; Gates #24/#25 CLOSED  
**Hold:** Gate **#26** backlog until Stage C **delivery** (not this Spec). Gate **#27** HOLD. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-checklist.md`

## Scope note
Spec-binding for **thin** OwnAgent-only Strategy-driven Assistant runtime under the agent/tool hard wall (#67). Real points — not N/A. Soft #41 Assistant OUT closes by delivering this runtime **under** #67 — not by claiming Stage B delivered Assistant. Do not invent fuller Assistant, multi-party agents, or Gate #26 unlock.

## Itemized security points (Spec must bind)

1. **OwnAgent-only 1:1** — Spec binds thin Assistant as **OwnAgent** for the owning Participant only (P6 spirit); never Counterparty / Stranger agent identity; no multi-party invent.

2. **StrategyBody via FieldPolicy** — Spec binds StrategyBody consume/write only when `IFieldPolicy.Evaluate` allows OwnAgent R/W for owner context; never expose another Participant’s StrategyBody.

3. **Mandatory bind to #67 hard wall** — Spec requires runtime uses **platform tools / gateway path only** (no raw DB / arbitrary internal HTTP); does **not** invent prompt-only soft wall as sole control; cross-ref #67 for allowlist + scrub.

4. **No LoginEmail in agent context** — Spec requires LoginEmail never enters model / agent context packs or tool outputs; LoginEmail remains User-only (distinct from ContactEmail).

5. **Authn / IDOR fail-closed** — Unauthenticated → **401**; wrong principal / cross-tenant → **403** or **404** (Spec consistency); uniform deny; **no** private-field leakage in responses or model context.

6. **Soft #41 OUT closed by design only** — Spec documents soft #41 Assistant OUT closed by **#66 + #67** delivery path — does **not** claim Assistant/tool runtime was Stage B–delivered.

7. **OUT locked (X1 thin)** — Spec documents **X1 MVP thin** only; fuller / stronger Assistant → **V1**; free-form Strategy engine → **V1**; Strategy sandbox (**A5**) → **V4**; BYO / multi-LLM breadth → later; no 5th Story for OTel/audit/idempotent (Soft Spec weave only).

8. **Sibling / Gate HOLDs** — Spec does not invent #68/#69 surfaces into this Story; Gate **#26** stays backlog until delivery; Gate **#27** HOLD; no Cognito/SSO/MM/DC4 invent.

9. **Cost / spend** — PoC **$0**; any named LLM/API spend → escalate **COO → CEO**; do not provision in Spec acceptance.

10. **Traceability + handshake** — Spec cites #66 AC + Option A Stage C + SA delta + SA Security PASS; keep #67/#68/#69 separate. Spec QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Spec weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Spec + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
