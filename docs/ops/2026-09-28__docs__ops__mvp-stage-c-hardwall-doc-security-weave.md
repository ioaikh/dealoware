# Doc step weave — Security checklist pts 1–10 · MVP Stage C #67 Agent/tool hard wall + response scrubber

| Field | Value |
|-------|-------|
| **Status** | **ISSUED** — Soft Soft CLOSE Soft HOLD overall Doc PASS until Doc handshake Soft Soft CLOSE Soft HOLD SoR MERGED (PMQA bounce: Product QA SoR ≠ Doc). Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS). |
| **Date** | 2026-09-28 |
| **Author** | Dealoware Senior Docs |
| **Story** | GitHub issue #67 · Agent/tool hard wall + response scrubber |
| **Issue** | https://github.com/ioaikh/dealoware/issues/67 |
| **Impl PR** | https://github.com/ioaikh/dealoware/pull/78 (**MERGED**) @ `dab5822` |
| **Checklist (binding, ISSUED)** | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-checklist.md` (SoR twin PR **#90** @ `9cf6248`) |
| **DOC-FLOW** | `ops/2026-09-28__docs__ops__mvp-stage-c-hardwall-doc-security-weave.md` |
| **Constraints** | Stage C named slice · CQ no-refactor · **#66/#68/#69 OUT** (separate Doc; cross-ref only) · Soft **#41** Assistant OUT via **#66+#67** under wall (not Stage B) · Parent #18 framing-only · Gate **#26** backlog · Gate **#27** HOLD · PoC **$0** · no MotorMarket / Cognito / DC4 / vault invent |

## Primary surfaces Security will score
1. Merged PR #78 — AgentGateway + ToolAllowlist + AgentContextScrubber + StageCAgentHardwallTests ×39 on main `dab5822`
2. This Doc weave + living `INDEX.md` annotations for #67 Doc paths
3. Binding Doc checklist SoR twin PR #90 @ `9cf6248`

Supporting (not primary score surface): Product QA Security trio (PASS — **not** overall Doc PASS); SD Security trio (PASS — SD Sec only); Spec / DevPlan / SD verifies; BA verify Soft Soft CLOSE Soft HOLD SoR (CBA PASS). Handshake files indexed after Security PASS (not invented).

## Explicit separations (non-merge)
- **#66 Thin Assistant** / **#68 A8 meters** / **#69 UI/bot** — OUT; separate tracks; cross-ref only.
- **Soft #41 Assistant OUT** — closes only with **#66 + #67** under this wall (not a Stage B claim).
- **Parent #18** — framing-only; does not Field-capture #67.
- **Gate #26** — backlog. **Gate #27** — HOLD.
- **PoC $0** — no IdP/vault/LLM provision as delivered by #67 Docs.

## Point → how Doc artifacts satisfy

Mapped strictly to `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-checklist.md` (pts 1–10). Handshake files indexed after Security PASS (not invented).

| # | Security point | Doc satisfaction (cites) |
|---|----------------|--------------------------|
| 1 | Agent runtime gateway | Docs: agents on platform tools only (AgentGateway); not raw DB / arbitrary internal HTTP; off-gateway deny fail-closed (PR #78 / README / StageCAgentHardwallTests). |
| 2 | Tool allowlist deny-by-default | Docs: each tool declares FieldClasses Read / ShareOutbound; undeclared tools denied (`ToolAllowlist` / `FieldClassDeclaration`). |
| 3 | Server-side scrub before model | Docs: every tool response / context pack passes the **same** `IFieldPolicy.Evaluate` as API/DB; denied fields stripped before model (`AgentContextScrubber`). |
| 4 | No LoginEmail in agent context | Docs: LoginEmail remains User-only; OwnAgent Deny; no LoginEmail tool; no LoginEmail in context packs. |
| 5 | ShareOutbound Accept-gated | Docs: share tools (e.g. ContactEmail) check AcceptGrant server-side; prompt injection cannot grant Evaluate denies. |
| 6 | Reject prompt-only / parallel ACL | Docs: no system-prompt soft guidance as sole control; no separate agent ACL tables that drift from FieldPolicy. |
| 7 | Dual wall + soft #41 path | Docs: dual wall for the open-ended FieldClass registry; soft #41 Assistant OUT closes only with #66 under this wall (not Stage B); #66/#68/#69 OUT of this Story. |
| 8 | Cross-agent mediated exfil | Docs: MVP Status — no cross-agent messaging path exists; future must be mediated + scrubbed; threats addressed by gateway + scrub, not model trust. |
| 9 | OUT / Gate / spend | Docs: mature vault → V3; MCP/public tool marketplace OUT; Gate #26 backlog; Gate #27 HOLD; no Cognito/MotorMarket/DC4/vault invent; Soft OTel/audit/idempotent = weave only (no 5th Story); PoC $0. |
| 10 | Handshake close | **OPEN.** Docs QA must **not** PASS until Security QA confirms these points. Handshake Soft Soft CLOSE Soft HOLD SoR later after Senior Security + Security QA. Parent #18 framing-only does not Field-capture #67. |

## Indexed Product QA Security (Sec10 for Product QA — already closed; not overall Doc PASS)
- `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-checklist.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-points-review.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-qa-confirm.md` (**PASS**)
- Product QA report: `qa/2026-09-28__qa__qa-report__mvp-stage-c-agent-tool-hardwall-scrubber.md` (PR #84 @ `7828faa`)

## Indexed SD Security (SD step — already closed; not overall Doc PASS)
- `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-checklist.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-points-review.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md` (**PASS**)

## Indexed Doc Security handshake
- Binding checklist (**ISSUED**, Soft Soft CLOSE Soft HOLD SoR CLEAR): `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-checklist.md` (PR #90 @ `9cf6248`)
- Senior Security points-review: **not invented** — Soft Soft CLOSE Soft HOLD SoR later after this weave + Senior Security + Security QA
- Security QA confirm: **not invented** — Soft Soft CLOSE Soft HOLD SoR later

## Close
**Soft Soft CLOSE Soft HOLD treat Doc as PASS** until handshake Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX. Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS; do not reopen). Do **not** CLOSE issue #67 from this weave.
