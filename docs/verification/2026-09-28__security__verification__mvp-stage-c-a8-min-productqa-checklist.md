# Security checklist — Product QA · MVP Stage C #68 A8-minimum per-Participant meters + hard budgets

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA / QAQA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/68 · A8-minimum meters + hard budgets (cutoff)  
**Parent:** #18 · Stage C · roadmap A8 Option 1  
**Siblings:** #66 (primary metered consumer) · #67 (metered path wall-bound) · #69 — **OUT** of this Story (keep separate Product QA; cross-ref only)  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/87 MERGED @ `c5485cf`  
**Live tip:** `main` @ `056faf5` (INDEX Soft Soft CLOSE Soft HOLD SoR **#99**; docs Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR ahead of impl tip)  
**SD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR:** PR **#93** CLEAR  
**SD checklist (ref):** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-checklist.md`  
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-qa-confirm.md`  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-a8-minimum-meters-budgets.md` (§5 Security 1–10)  
**Tests:** `tests/Dealoware.Api.Tests/StageCBudgetMeterTests.cs`  
**Hold:** Soft Soft CLOSE Soft HOLD Doc until Product QA PASS. Soft Soft CLOSE Soft HOLD Product QA PASS until Senior Security → Security QA productqa-qa-confirm → QAQA. Soft **#41** OUT via **#66+#67** (do not re-open). Gate **#26** backlog; Gate **#27** HOLD. Parent #18 framing-only. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md`

## Scope note
Product QA must **verify** Spec + Plan + SD Security for **A8-minimum** per-Participant meters + **hard cutoff** fail-closed (**not** soft-warn-only). Real points — not N/A. Primary consumer #66; metered path still wall-bound (#67). Evidence via `StageCBudgetMeterTests` + SD qa-confirm PASS + Impl PR #87 @ `c5485cf`. Do not invent mature billing, owner admin suite, Cognito/MM/DC4/vault, Gate #26 unlock, or a 5th Story. Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (Product QA Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR ≠ Doc).

## Itemized security points (Product QA must verify)

1. **Per-Participant meters** — Verify minimum viable meters for MVP-metered Assistant/LLM (and Product-named Stage C metered surfaces without inventing extras); counters scoped to Participant.

2. **Hard cutoff fail-closed** — Verify when budget exhausted, further metered Assistant/tool invocations **deny server-side** (fail-closed) — **not** soft warn only.

3. **Cross-tenant / unauth cannot burn budget** — Verify unauthenticated / wrong-principal cannot consume another Participant’s budget; cross-tenant meter misuse fail-closed.

4. **Metered path still wall-bound (#67)** — Verify metered Assistant/tool path behind agent/tool hard wall; budget status is not a privilege escalation or field-leak channel (do **not** re-score #67 as this Story).

5. **Authn fail-closed on meter APIs** — Verify GET `/budget/status` (and meter APIs): unauthenticated → **401**; wrong principal / own-only → **403** or **404**; uniform deny; no FieldClass / private-field leakage via meter/budget payloads.

6. **OUT locked (A8-minimum only)** — Confirm A8-minimum MVP only; mature metering / platform-owner cost UI → **V3**; no inventing full billing/settlement/escrow; Soft O7 align only if already on path (no second product).

7. **Sibling surfaces** — Confirm does **not** invent #69 owner-admin suite; #69 may show budget status **minimally** to respect cutoff only; keep #66/#67/#69 separate Product QA.

8. **No 5th Story / Gate HOLDs** — Confirm Soft OTel/audit/idempotent = weave only (no 5th Story); Gate **#26** backlog; Gate **#27** HOLD; no Cognito/MM/DC4/vault invent; Soft **#41** OUT via **#66+#67** (do not re-open).

9. **Cost / spend** — Confirm PoC **$0**; any named LLM/API spend → **COO → CEO**; do not provision spend without that path.

10. **Handshake close** — Product QA / QAQA must **not** PASS until Security QA confirms Product QA points-review. Parent #18 framing-only does **not** Field-capture #68.

## Handshake next
Senior Product QA → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief QA + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
