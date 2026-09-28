# Security checklist — Spec · MVP Stage C #68 A8-minimum per-Participant meters + hard budgets

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/68 · A8-minimum meters + hard budgets (cutoff)  
**Parent:** #18 · Stage C · roadmap A8 Option 1  
**Siblings:** #66 (primary metered consumer) · #67 (metered path still wall-bound) · #69 — keep separate Spec; cross-ref only  
**Architecture:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` (§3c A8-min pick A)  
**Prior Security PASS (SA step):** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Hold:** Gate **#26** backlog until delivery. Gate **#27** HOLD. Mature metering/owner cost UI → **V3**. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-checklist.md`

## Scope note
Spec-binding for **A8-minimum** per-Participant meters + **hard cutoff** fail-closed (not soft-warn-only). Real points — not N/A. Primary consumer #66; do not invent mature billing, owner admin suite, or a 5th Story.

## Itemized security points (Spec must bind)

1. **Per-Participant meters** — Spec binds minimum viable meters for MVP-metered Assistant/LLM (and Product-named Stage C metered surfaces without inventing extras); counters scoped to Participant.

2. **Hard cutoff fail-closed** — Spec requires when budget exhausted, further metered Assistant/tool invocations **deny server-side** (fail-closed) — **not** soft warn only.

3. **Cross-tenant / unauth cannot burn budget** — Spec binds unauthenticated / wrong-principal cannot consume another Participant’s budget; cross-tenant meter misuse fail-closed.

4. **Metered path still wall-bound (#67)** — Spec requires metered Assistant/tool path remains behind agent/tool hard wall; budget status must not become a privilege escalation or field-leak channel.

5. **Authn fail-closed on meter APIs** — Unauthenticated → **401**; wrong principal → **403**/**404**; uniform deny; no private leakage via meter/budget payloads.

6. **OUT locked (A8-minimum only)** — Spec documents **A8-minimum** MVP; mature metering / platform-owner cost UI → **V3**; no inventing full billing/settlement/escrow; Soft O7 align only if already on path (no second product).

7. **Sibling surfaces** — Spec does not invent #69 owner-admin suite; #69 may show budget status **minimally** to respect cutoff only.

8. **No 5th Story / Gate HOLDs** — Soft Spec weave only for audit/OTel if meters touch those hooks; Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/MM/DC4.

9. **Cost / spend** — PoC **$0**; any named LLM/API spend → **COO → CEO**; Spec does not provision spend.

10. **Traceability + handshake** — Spec cites #68 AC + roadmap A8 Option 1 + SA Security PASS; keep #66/#67/#69 separate. Spec QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Spec weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Spec + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
