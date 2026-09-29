# Security checklist — Doc · MVP Stage C #68 A8-minimum per-Participant meters + hard budgets

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.
**Date:** 2026-09-28
**Author:** Dealoware Chief Security
**Story:** https://github.com/ioaikh/dealoware/issues/68 · A8-minimum meters + hard budgets (cutoff)
**Parent:** #18 · Stage C · roadmap A8 Option 1
**Siblings:** #66 (primary metered consumer) · #67 (metered path wall-bound — bind only) · #69 — **OUT** of this Story (keep separate Doc; cross-ref only)
**Impl PR:** https://github.com/ioaikh/dealoware/pull/87 MERGED @ `c5485cf`
**Product QA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-qa-confirm.md`
**Product QA handshake SoR:** PR **#105** @ `d3c59a9` (Product QA SoR ≠ Doc)
**Product QA checklist SoR:** PR **#101** @ `2762b95` (content `cc203fef`)
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-qa-confirm.md`
**SD handshake SoR:** PR **#93** @ `0b40cab`
**Tests:** `tests/Dealoware.Api.Tests/StageCBudgetMeterTests.cs` (29)
**Hold:** Treat #68 Doc as PASS until handshake SoR MERGED + INDEX. `status:done` until CBA. Soft **#41** OUT via **#66+#67** (do not re-open). Gate **#26** backlog; Gate **#27** HOLD. Parent #18 framing-only. PoC **$0**; no MotorMarket/Cognito/DC4.
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-doc-checklist.md`
**Tip:** `d3c59a9`

## Scope note
Doc must accurately describe **A8-minimum** per-Participant meters + **hard cutoff** fail-closed (**not** soft-warn-only). Product QA SoR ≠ Doc — this is a separate Doc-step checklist. Real points — not N/A. Do not invent mature billing, owner admin suite, Cognito/MM/DC4/vault, Gate #26 unlock, or a 5th Story. Primary consumer #66; metered path still wall-bound (#67).

## Itemized security points (Doc must satisfy)

1. **Per-Participant meters** — Docs state minimum viable meters for MVP-metered Assistant/LLM (and Product-named Stage C metered surfaces without inventing extras); counters scoped to Participant.

2. **Hard cutoff fail-closed** — Docs state when budget exhausted, further metered Assistant/tool invocations **deny server-side** (fail-closed) — **not** soft warn only.

3. **Cross-tenant / unauth cannot burn budget** — Docs state unauthenticated / wrong-principal cannot consume another Participant’s budget; cross-tenant meter misuse fail-closed.

4. **Metered path still wall-bound (#67)** — Docs state metered Assistant/tool path behind agent/tool hard wall; budget status is not a privilege escalation or field-leak channel (do **not** re-score #67 as this Story).

5. **Authn fail-closed on meter APIs** — Docs state GET `/budget/status` (and meter APIs): unauthenticated → **401**; wrong principal / own-only → **403** or **404**; uniform deny; no FieldClass / private-field leakage via meter/budget payloads.

6. **OUT locked (A8-minimum only)** — Docs state A8-minimum MVP only; mature metering / platform-owner cost UI → **V3**; no inventing full billing/settlement/escrow.

7. **Sibling surfaces** — Docs state this Story does **not** deliver #69 owner-admin suite; #69 may show budget status **minimally** to respect cutoff only; keep #66/#67/#69 separate Doc.

8. **No 5th Story / Gate HOLDs** — Docs state Soft OTel/audit/idempotent = weave only (no 5th Story); Gate **#26** backlog; Gate **#27** HOLD; no Cognito/MM/DC4/vault invent; Soft **#41** OUT via **#66+#67** (do not re-open).

9. **Cost / spend** — Docs state PoC **$0**; any named LLM/API spend → **COO → CEO**.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points. Parent #18 framing-only does **not** Field-capture #68.

## Handshake next
Senior Docs weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
