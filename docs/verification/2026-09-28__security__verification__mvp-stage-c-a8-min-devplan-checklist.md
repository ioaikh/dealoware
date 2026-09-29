# Security checklist — Dev Plan · MVP Stage C #68 A8-minimum per-Participant meters + hard budgets

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/68 · A8-minimum meters + hard budgets (cutoff)  
**Parent:** #18 · Stage C · roadmap A8 Option 1  
**Siblings:** #66 (primary metered consumer) · #67 (metered path still wall-bound) · #69 — keep separate plan; cross-ref only  
**Order preference:** **#67 → #66 → #68 → #69 → #18** (#68 after #66)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-a8-minimum-meters-budgets.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-qa-confirm.md` (Spec Security SoR **#71+#72**; tip `main` @ `f133e90` Senior PM — Spec Soft Soft CLOSE Soft HOLD SoR CLEAR / PR #73 cited by PM for Spec)  
**Architecture:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` (§3c A8-min pick A)  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. SD HOLD until Dev Plan QA + Security PASS. Mature metering/owner cost UI → **V3**. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate/verify** Spec Security for **A8-minimum** per-Participant meters + **hard cutoff** fail-closed (**not** soft-warn-only). Real points — not N/A. Primary consumer #66; metered path still wall-bound (#67). Do not invent mature billing, owner admin suite, Cognito/MM/DC4/vault, Gate #26 unlock, or a 5th Story.

## Itemized security points (Dev Plan must weave)

1. **Per-Participant meters tasks** — Plan schedules minimum viable meters for MVP-metered Assistant/LLM (and Product-named Stage C metered surfaces without inventing extras); counters scoped to Participant; verify.

2. **Hard cutoff fail-closed tasks** — Plan requires when budget exhausted, further metered Assistant/tool invocations **deny server-side** (fail-closed) — **not** soft warn only; verify.

3. **Cross-tenant / unauth cannot burn budget** — Plan schedules unauthenticated / wrong-principal cannot consume another Participant’s budget; cross-tenant meter misuse fail-closed; verify.

4. **Metered path still wall-bound (#67)** — Plan requires metered Assistant/tool path remains behind agent/tool hard wall; budget status must not become a privilege escalation or field-leak channel; verify.

5. **Authn fail-closed on meter APIs** — Plan schedules unauthenticated → **401**; wrong principal → **403**/**404**; uniform deny; no private leakage via meter/budget payloads; verify.

6. **OUT locked (A8-minimum only)** — Plan keeps **A8-minimum** MVP; mature metering / platform-owner cost UI → **V3**; no inventing full billing/settlement/escrow; Soft O7 align only if already on path (no second product).

7. **Sibling surfaces** — Plan does not invent #69 owner-admin suite; #69 may show budget status **minimally** to respect cutoff only; keep #66/#67/#69 separate plans.

8. **No 5th Story / Gate HOLDs** — Soft OTel/audit/idempotent = weave only on named plans if meters touch those hooks (no 5th Story); Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/MM/DC4/vault invent.

9. **Cost / spend** — PoC **$0**; any named LLM/API spend → **COO → CEO**; Plan does not provision spend without that path.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
