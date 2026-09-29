# Security checklist — Product QA · MVP Stage C #66 Thin Strategy-driven AI Assistant runtime (X1 thin)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA / QAQA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/66 · Thin Strategy-driven AI Assistant runtime (X1 thin)  
**Parent:** #18 · Option A · Stage C named slice  
**Siblings:** #67 (hard wall — **mandatory bind only**; already Product QA Security PASSed) · #68 · #69 — **OUT** of this Story (keep separate Product QA; cross-ref only)  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/79 MERGED @ `199125a`  
**SD Soft Soft CLOSE Soft HOLD SoR:** PR **#83** @ `32014a6` · tip Soft Soft CLOSE Soft HOLD SD verify #86 @ `7043314`  
**SD checklist (ref):** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-sd-checklist.md`  
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-sd-qa-confirm.md`  
**Tests:** `tests/Dealoware.Api.Tests/StageCThinAssistantTests.cs` (28)  
**Hold:** Soft Soft CLOSE Soft HOLD Doc until Product QA PASS. Soft Soft CLOSE Soft HOLD status:done until CBA BA-verify. Soft **#41** → **#66+#67** under wall (not Stage B). Gate **#26** backlog. Gate **#27** HOLD. **#67/#68/#69** OUT of this Story (bind wall only). Parent #18 framing-only. PoC **$0**; no MotorMarket/Cognito/DC4.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-productqa-checklist.md`

## Scope note
Product QA must **verify** Spec + Plan + SD Security for **thin** OwnAgent-only Strategy-driven Assistant runtime **under** the agent/tool hard wall (#67). Real points — not N/A. Evidence via tests / done-list / QA runs (`StageCThinAssistantTests.cs` ×28; SD qa-confirm PASS; Impl PR #79 @ `199125a`). Soft #41 Assistant OUT closes by delivering this runtime under #67 — not by claiming Stage B delivered Assistant. Soft Soft CLOSE Soft HOLD Doc until Product QA PASS; Soft Soft CLOSE Soft HOLD status:done until CBA BA-verify.

## Itemized security points (Product QA must verify)

1. **OwnAgent-only 1:1** — Verify thin Assistant is **OwnAgent** for the owning Participant only (P6 spirit); never Counterparty / Stranger agent identity; no multi-party invent (evidence: `StageCThinAssistantTests.cs` ×28).

2. **StrategyBody via FieldPolicy** — Verify StrategyBody consume/write only when `IFieldPolicy.Evaluate` allows OwnAgent R/W for owner context; confirm no foreign StrategyBody leak.

3. **Mandatory bind to #67 hard wall** — Verify runtime on **platform tools / gateway path only** (no raw DB / arbitrary internal HTTP); reject prompt-only soft wall as sole control; confirm consume of #67 allowlist + the **same** `IFieldPolicy` scrub (do not re-score #67 as this Story).

4. **No LoginEmail in agent context** — Verify LoginEmail stripped/denied from model / agent context packs, tools, capabilities, and errors; LoginEmail remains User-only (distinct from ContactEmail).

5. **Authn / IDOR fail-closed** — Verify unauthenticated → **401**; wrong principal / cross-tenant → **403** or **404**; uniform deny; no private-field leakage in responses or model context.

6. **Soft #41 OUT closed by delivery path only** — Confirm soft #41 Assistant OUT closes by **#66 + #67** under wall — do **not** claim Assistant/tool runtime was Stage B–delivered.

7. **OUT locked (X1 thin) + one Dealoware API** — Confirm X1 MVP thin only (fuller / free-form / A5 / BYO OUT). Multi-provider clients, if present, are interchangeable vs **one** Dealoware API — no provider-specific invent in #66; same wall + same scrub; no provider bypass.

8. **Sibling / Gate HOLDs** — Confirm **#67/#68/#69** remain OUT of this Story (bind wall only); Gate **#26** backlog; Gate **#27** HOLD; no Cognito/MotorMarket/DC4/vault invent; Soft OTel/audit/idempotent = weave only (no 5th Story).

9. **OUT / spend** — Confirm PoC **$0**; any named LLM/API spend → **COO → CEO**; do not provision spend without that path.

10. **Handshake close** — Product QA / QAQA must **not** PASS until Security QA confirms Product QA points-review. Parent #18 framing-only does **not** Field-capture #66.

## Handshake next
Senior Product QA → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief QA + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
