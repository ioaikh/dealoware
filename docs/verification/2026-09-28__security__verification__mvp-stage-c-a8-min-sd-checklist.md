# Security checklist — SD · MVP Stage C #68 A8-minimum per-Participant meters + hard budgets

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/68 · A8-minimum meters + hard budgets (cutoff)  
**Parent:** #18 · Stage C · roadmap A8 Option 1  
**Siblings:** #66 (primary metered consumer) · #67 (metered path still wall-bound) · #69 — keep separate SD; cross-ref only  
**Order preference:** **#67 → #66 → #68 → #69 → #18** (#68 after #66)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-a8-minimum-meters-budgets.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-qa-confirm.md`  
**Plan:** `plans/2026-09-28__devplan__plan__mvp-stage-c-a8-minimum-meters-budgets.md`  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-devplan-qa-confirm.md`  
**Tip:** `main` @ `dc8ee46` (Dev Plan SoR PR **#76** MERGED; checklist **#74** CLEAR)  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. Soft Soft CLOSE Soft HOLD Dev Code QA until SD-step Security PASS. Soft **#41** → **#66+#67** under wall. Eng dual-wall remainder = **#67**. Parent #18 framing-only — BIND #67; does **not** Field-capture #67. No Cognito/MM/DC4. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-checklist.md`

## Scope note
SD must **implement** Spec + Dev Plan Security for **A8-minimum** per-Participant meters + **hard cutoff** fail-closed (**not** soft-warn-only). Real points — not N/A. Primary consumer #66; metered path still wall-bound (#67). Do not invent mature billing, owner admin suite, Cognito/MM/DC4/vault, Gate #26 unlock, or a 5th Story. Evidence via tests/done-list.

## Itemized security points (SD must satisfy)

1. **Per-Participant meters** — Implement minimum viable meters for MVP-metered Assistant/LLM (and Product-named Stage C metered surfaces without inventing extras); counters scoped to Participant (verified).

2. **Hard cutoff fail-closed** — Enforce when budget exhausted, further metered Assistant/tool invocations **deny server-side** (fail-closed) — **not** soft warn only (verified).

3. **Cross-tenant / unauth cannot burn budget** — Enforce unauthenticated / wrong-principal cannot consume another Participant’s budget; cross-tenant meter misuse fail-closed (verified).

4. **Metered path still wall-bound (#67)** — Keep metered Assistant/tool path behind agent/tool hard wall; budget status must not become a privilege escalation or field-leak channel (verified).

5. **Authn fail-closed on meter APIs** — Enforce unauthenticated → **401**; wrong principal → **403**/**404**; uniform deny; no private leakage via meter/budget payloads (verified).

6. **OUT locked (A8-minimum only)** — Keep **A8-minimum** MVP; mature metering / platform-owner cost UI → **V3**; no inventing full billing/settlement/escrow; Soft O7 align only if already on path (no second product).

7. **Sibling surfaces** — Do not invent #69 owner-admin suite; #69 may show budget status **minimally** to respect cutoff only; keep #66/#67/#69 separate SD.

8. **No 5th Story / Gate HOLDs** — Soft OTel/audit/idempotent = weave only on named Stories if meters touch those hooks (no 5th Story); Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/MM/DC4/vault invent — do **not** invent Gate #26 unlock.

9. **Cost / spend** — PoC **$0**; any named LLM/API spend → **COO → CEO**; do not provision spend without that path.

10. **Evidence + handshake** — Done-list cites paths/tests for 1–9; Dev Code QA / Product QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
