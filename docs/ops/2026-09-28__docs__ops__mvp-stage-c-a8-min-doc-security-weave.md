# Doc step weave — Security checklist pts 1–10 · MVP Stage C #68 A8-minimum per-Participant meters + hard budgets

| Field | Value |
|-------|-------|
| **Status** | **ISSUED** — Soft Soft CLOSE Soft HOLD treat #68 Doc as PASS until Doc handshake Soft Soft CLOSE Soft HOLD SoR MERGED (Product QA SoR ≠ Doc). Soft Soft CLOSE Soft HOLD status:done until CBA. |
| **Date** | 2026-09-28 |
| **Author** | Dealoware Senior Docs |
| **Story** | GitHub issue #68 · A8-minimum meters + hard budgets (cutoff) |
| **Issue** | https://github.com/ioaikh/dealoware/issues/68 |
| **Impl PR** | https://github.com/ioaikh/dealoware/pull/87 (**MERGED**) @ `c5485cf` |
| **Checklist (binding, ISSUED)** | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-doc-checklist.md` (SoR twin PR **#107** @ `3c5063c`) |
| **DOC-FLOW** | `ops/2026-09-28__docs__ops__mvp-stage-c-a8-min-doc-security-weave.md` |
| **Constraints** | Stage C A8 Option 1 · A8-minimum only · **#66** primary metered consumer · **#67** metered path wall-bound (bind only; do not re-score) · **#69 OUT** (may show budget status minimally; separate Doc) · Soft **#41** OUT via **#66+#67** (do not re-open) · Parent #18 framing-only · Gate **#26** backlog · Gate **#27** HOLD · PoC **$0** · no MotorMarket / Cognito / DC4 / vault invent · no mature billing / owner admin suite |

## Primary surfaces Security will score
1. Merged PR #87 — A8-minimum per-Participant meters + hard cutoff + StageCBudgetMeterTests ×29 on main `c5485cf`
2. This Doc weave + living `INDEX.md` annotations for #68 Doc paths
3. Binding Doc checklist Soft Soft CLOSE Soft HOLD SoR twin PR #107 @ `3c5063c`

Supporting (not primary score surface): Product QA Security trio (PASS — **not** overall Doc PASS); SD Security trio (PASS — SD Sec only); Spec / DevPlan / SD verifies; Product QA report Soft Soft CLOSE Soft HOLD SoR (PR **#103** @ `961b815` + INDEX catch-up **#104** @ `a78fc77`). Handshake files indexed after Security PASS (not invented).

## Explicit separations (non-merge)
- **#66 thin Assistant** — primary metered consumer; separate Doc Soft Soft CLOSE Soft HOLD SoR track (already closed).
- **#67 hard wall** — metered path must stay wall-bound (bind only); do **not** re-score #67 as this Story.
- **#69 UI/bot** — OUT of this Story; may show budget status **minimally** to respect cutoff only; separate Doc.
- **Soft #41 Assistant OUT** — via **#66+#67** only (do not re-open from #68).
- **Parent #18** — framing-only; does not Field-capture #68.
- **Gate #26** — backlog. **Gate #27** — HOLD.
- **PoC $0** — no IdP/vault/LLM provision / mature billing as delivered by #68 Docs.

## Point → how Doc artifacts satisfy

Mapped strictly to `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-doc-checklist.md` (pts 1–10). Handshake files indexed after Security PASS (not invented).

| # | Security point | Doc satisfaction (cites) |
|---|----------------|--------------------------|
| 1 | Per-Participant meters | Docs: A8-minimum viable meters for MVP-metered Assistant/LLM (and Product-named Stage C metered surfaces without inventing extras); counters scoped to Participant (PR #87 / StageCBudgetMeterTests). |
| 2 | Hard cutoff fail-closed | Docs: when budget exhausted, further metered Assistant/tool invocations **deny server-side** (fail-closed) — **not** soft warn only. |
| 3 | Cross-tenant / unauth cannot burn budget | Docs: unauthenticated / wrong-principal cannot consume another Participant’s budget; cross-tenant meter misuse fail-closed. |
| 4 | Metered path still wall-bound (#67) | Docs: metered Assistant/tool path behind agent/tool hard wall; budget status is not a privilege escalation or field-leak channel (do **not** re-score #67). |
| 5 | Authn fail-closed on meter APIs | Docs: GET `/budget/status` (and meter APIs): unauthenticated → **401**; wrong principal / own-only → **403** or **404**; uniform deny; no FieldClass / private-field leakage via meter/budget payloads. |
| 6 | OUT locked (A8-minimum only) | Docs: A8-minimum MVP only; mature metering / platform-owner cost UI → **V3**; no inventing full billing/settlement/escrow. |
| 7 | Sibling surfaces | Docs: this Story does **not** deliver #69 owner-admin suite; #69 may show budget status **minimally** to respect cutoff only; keep #66/#67/#69 separate Doc. |
| 8 | No 5th Story / Gate HOLDs | Docs: Soft OTel/audit/idempotent = weave only (no 5th Story); Gate #26 backlog; Gate #27 HOLD; no Cognito/MM/DC4/vault invent; Soft #41 OUT via #66+#67 (do not re-open). |
| 9 | Cost / spend | Docs: PoC $0; any named LLM/API spend → COO → CEO. |
| 10 | Handshake close | **OPEN.** Docs QA must **not** PASS until Security QA confirms these points. Handshake Soft Soft CLOSE Soft HOLD SoR later after Senior Security + Security QA. Parent #18 framing-only does not Field-capture #68. |

## Indexed Product QA Security (Sec10 for Product QA — already closed; not overall Doc PASS)
- `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md` (PR #101 @ `2762b95`; content `cc203fef`)
- `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-points-review.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-qa-confirm.md` (**PASS**; Soft Soft CLOSE Soft HOLD SoR PR #105 @ `d3c59a9`)
- Product QA report: `qa/2026-09-28__qa__qa-report__mvp-stage-c-a8-min-meters-budgets.md` (PR #103 @ `961b815` + INDEX #104 @ `a78fc77`)

## Indexed SD Security (SD step — already closed; not overall Doc PASS)
- `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-checklist.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-points-review.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-qa-confirm.md` (**PASS**; Soft Soft CLOSE Soft HOLD SoR PR #93 @ `0b40cab`)

## Indexed Doc Security handshake
- Binding checklist (**ISSUED**, Soft Soft CLOSE Soft HOLD SoR CLEAR): `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-doc-checklist.md` (PR #107 @ `3c5063c`)
- Senior Security points-review: **not invented** — Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR later after this weave + Senior Security + Security QA
- Security QA confirm: **not invented** — Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR later

## Close
**Soft Soft CLOSE Soft HOLD treat #68 Doc as PASS** until handshake Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX. Soft Soft CLOSE Soft HOLD status:done until CBA. Do **not** CLOSE issue #68 from this weave.
