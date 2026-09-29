# Doc step weave — Security checklist pts 1–10 · MVP Stage C #69 Basic UI and/or one first-party bot (X2 partial)

| Field | Value |
|-------|-------|
| **Status** | **ISSUED** — Soft Soft CLOSE Soft HOLD treat #69 Doc as PASS until Doc handshake Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR MERGED (Product QA SoR ≠ Doc). Soft Soft CLOSE Soft HOLD status:done until CBA. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. |
| **Date** | 2026-09-28 |
| **Author** | Dealoware Senior Docs |
| **Story** | GitHub issue #69 · Basic UI and/or one first-party bot surface (X2 partial) |
| **Issue** | https://github.com/ioaikh/dealoware/issues/69 |
| **Impl PR** | https://github.com/ioaikh/dealoware/pull/100 (**MERGED**) @ `da61210` |
| **Checklist (binding, ISSUED)** | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-doc-checklist.md` (SoR twin PR **#117** @ `95e04e4`) |
| **DOC-FLOW** | `ops/2026-09-28__docs__ops__mvp-stage-c-x2-ui-bot-doc-security-weave.md` |
| **Constraints** | Spec-locked **basic UI** only (exactly-one first-party bot **not** required) · Soft HOLD multi-provider until BM multi-provider start · **#66/#67/#68** separate Doc (bind only; do not re-score) · Soft **#41** CLOSED via **#66+#67** · Parent #18 framing-only · Gate **#26** backlog · Gate **#27** HOLD · PoC **$0** · no MotorMarket / Cognito / DC4 / vault invent · no multi-bot marketplace / fuller bot channel / 5th Story |

## Primary surfaces Security will score
1. Merged PR #100 — basic UI (wwwroot SPA) + StageCBasicUITests ×32 on main `da61210`
2. This Doc weave + living `INDEX.md` annotations for #69 Doc paths
3. Binding Doc checklist Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR twin PR #117 @ `95e04e4`

Supporting (not primary score surface): Product QA Security trio (PASS — **not** overall Doc PASS; Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR PR **#115** @ `3b7b323`); SD Security trio (PASS — SD Sec only; Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR PR **#106** @ `259813e`); Spec / DevPlan / SD verifies; Product QA report on KB Soft HOLD until Doc SoR if assigned. Handshake files indexed after Security PASS (not invented).

## Explicit separations (non-merge)
- **#66 thin Assistant** — bind when UI invokes Assistant; separate Doc Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR track (already closed). Do **not** re-score.
- **#67 hard wall** — UI must not bypass agent/tool hard wall; bind only; do **not** re-score #67 as this Story.
- **#68 A8-min meters** — budget status **minimal** only to respect cutoff; not V3 admin / owner cost UI; do **not** re-score #68.
- **Soft #41 Assistant OUT** — CLOSED via **#66+#67** only (do not re-open from #69).
- **Parent #18** — framing-only; does not Field-capture #69.
- **Gate #26** — backlog. **Gate #27** — HOLD.
- **Soft HOLD multi-provider** — Spec/doc rewrite until BM multi-provider start.
- **PoC $0** — no IdP/vault/LLM provision / multi-bot marketplace as delivered by #69 Docs.

## Point → how Doc artifacts satisfy

Mapped strictly to `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-doc-checklist.md` (pts 1–10). Handshake files indexed after Security PASS (not invented).

| # | Security point | Doc satisfaction (cites) |
|---|----------------|--------------------------|
| 1 | No privileged back doors | Docs: basic UI does **not** bypass API FieldPolicy or agent/tool hard wall (#67); **no** UI-only filtering as security; UI is not a security boundary (PR #100 / StageCBasicUITests; app.js "UI is NOT a security boundary"). |
| 2 | Authn fail-closed on protected actions | Docs: unauthenticated protected actions → fail-closed (**401**); wrong principal → **403**/**404**; no private-field leakage via UI payloads (`*_Unauth_Returns401`, CrossTenant 404 Facts). |
| 3 | Assistant path binds #67 | Docs: when UI invokes thin Assistant (#66), tool/agent path binds hard wall + scrub; **reject** client prompt-only soft wall invent (do **not** re-score #67). |
| 4 | Budget status minimal only (#68) | Docs: budget status shown **minimally** to respect cutoff only — **not** platform-owner admin / mature cost UI (**V3**); no admin-suite invent (do **not** re-score #68). |
| 5 | Surface pick = basic UI | Docs: Spec-locked **basic UI** minimum only; exactly-one first-party bot **not** required; no multi-bot marketplace invent; Soft HOLD multi-provider until BM multi-provider start. |
| 6 | OUT locked (X2 MVP partial) | Docs: **X2 MVP partial**; public OpenAPI/webhooks → **V1**; MCP breadth → **V5**; Soft OTel/audit/idempotent = weave only on named #66/#67/#68 (no 5th Story). |
| 7 | Consume tip authz | Docs: exercises existing auth (#5) + FieldPolicy paths already on tip; does not rewrite Stage A/B ACL Stories. |
| 8 | No Gate unlock / no invent | Docs: Gate **#26** backlog; Gate **#27** HOLD; no Cognito/SSO/MM/DC4/vault invent; Soft **#41** CLOSED via **#66+#67**; keep #66/#67/#68 separate Doc. |
| 9 | Cost / spend | Docs: PoC **$0**; any named LLM/API spend → **COO → CEO**. |
| 10 | Handshake close | **OPEN.** Docs QA must **not** PASS until Security QA confirms these points. Handshake Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR later after Senior Security + Security QA. Parent #18 framing-only does not Field-capture #69. |

## Indexed Product QA Security (Sec10 for Product QA — already closed; not overall Doc PASS)
- `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-checklist.md` (PR #112 @ `fdc26b9`)
- `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-points-review.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-qa-confirm.md` (**PASS**; Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR PR #115 @ `3b7b323`)
- Product QA report: `qa/2026-09-28__qa__qa-report__mvp-stage-c-basic-ui-first-party-bot-x2.md` (KB Soft HOLD until Doc SoR if assigned)

## Indexed SD Security (SD step — already closed; not overall Doc PASS)
- `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-checklist.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-points-review.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-qa-confirm.md` (**PASS**; Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR PR #106 @ `259813e`)

## Indexed Doc Security handshake
- Binding checklist (**ISSUED**, Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR CLEAR): `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-doc-checklist.md` (PR #117 @ `95e04e4`)
- Senior Security points-review: **not invented** — Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR later after this weave + Senior Security + Security QA
- Security QA confirm: **not invented** — Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR later

## Close
**Soft Soft CLOSE Soft HOLD treat #69 Doc as PASS** until handshake Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX. Soft Soft CLOSE Soft HOLD status:done until CBA. Soft HOLD multi-provider. Do **not** CLOSE issue #69 from this weave.
