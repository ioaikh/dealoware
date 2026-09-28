# Spec QA — MVP Stage C Agent/tool hard wall + response scrubber (#67) — Spec gate

**QA:** Dealoware Spec QA  
**Date:** 2026-09-28  
**Verdict:** **PASS (Spec gate)**  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/67  
**DOC-FLOW:** `verification/2026-09-28__spec__verification__mvp-stage-c-agent-tool-hardwall-scrubber.md`  
**Checklist (KB):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-checklist.md`  
**SoR Security QA confirm:** `docs/verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-qa-confirm.md` (KB twin: `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-qa-confirm.md`) — **PASS** 10/10; SoR MERGED PR **#72** @ `32d2e0bc`  
**SoR points-review:** `docs/verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-points-review.md` (KB twin: `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-points-review.md`) — **PASS** 10/10  
**Checklist SoR:** PR **#71** @ `f64a3d11` — CLEAR  
**Constraints:** Confirm to Chief Spec only. Soft **#41** → **#66+#67** only (not Stage B claim). Gate **#26** backlog; Gate **#27** HOLD. Soft OTel/audit/idempotent weave only — no 5th Story. PoC **$0**. Markdown Spec only — no Spec file edits by Spec QA. Security Spec-step already PASSed on SoR. Parent #18 not Field-captured here. Siblings #66/#68/#69 cross-ref only.

## DOC-FLOW

| Artifact | Path | Result |
|----------|------|--------|
| Spec | `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md` | **PASS** — DOC-FLOW header present |
| This evidence | `verification/2026-09-28__spec__verification__mvp-stage-c-agent-tool-hardwall-scrubber.md` | **PASS** — filed |
| Security checklist | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-checklist.md` | **PASS** — binding 1–10 (PR #71) |
| SoR qa-confirm | `docs/verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-qa-confirm.md` | **PASS** — MERGED PR #72 @ `32d2e0bc` |
| SoR points-review | `docs/verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-points-review.md` | **PASS** — MERGED PR #72 |

## Issue AC vs §9 map

| Issue #67 AC bullet | Spec §9 / cites | Result |
|---------------------|-----------------|--------|
| Agents call platform tools only (separate agent runtime gateway) — not raw DB, not arbitrary HTTP to internal APIs | Locked #0/#1; §1; §9 row 1 | **PASS** |
| Tool allowlist deny-by-default: FieldClasses Read/ShareOutbound; undeclared denied | Locked #2; §2; §9 row 2 | **PASS** |
| Server-side scrub: every tool response/context pack → IFieldPolicy.Evaluate; denied stripped before model | Locked #3; §3; §9 row 3 | **PASS** |
| No LoginEmail in agent context packs / tools — User-only (not even OwnAgent) | Locked #4; §4; §9 row 4 | **PASS** |
| ShareOutbound Accept-gated; deny regardless of prompt text | Locked #5; §4; §9 row 5 | **PASS** |
| Cross-agent messaging mediated; payloads cannot include denied FieldClasses; prompt cannot escalate | Locked #6; §4; §9 row 6 | **PASS** |
| Automated tests: allowlisted OK+scrub; denied strip; LoginEmail never; pre-Accept deny; stranger/cross-tenant; unauth; reject prompt-only | §9 row 7; §9.1 cases | **PASS** |
| Documented Stage C #18 remainder dual-wall; mature vault → V3; MCP/marketplace OUT | Locked #10; §7 OUT; §9 row 8 | **PASS** |
| Soft Spec weave OTel/audit/idempotent-offers — no 5th Story / new product surface | Locked #9; §5; §9 row 9 | **PASS** |

**All 9 issue AC bullets mapped.**

## Sec10 weave vs checklist 1–10

| # | Checklist point | Spec bind | Result |
|---|-----------------|-----------|--------|
| 1 | Separate agent runtime gateway | Locked #0/#1; §1; §6 row 1 | **PASS** (Security SoR PASS) |
| 2 | Tool allowlist deny-by-default | Locked #2; §2; §6 row 2 | **PASS** |
| 3 | Server-side scrub before model context | Locked #3; §3; §6 row 3 | **PASS** |
| 4 | No LoginEmail tool / never in agent context | Locked #4; §4; §6 row 4 | **PASS** |
| 5 | ShareOutbound Accept-gated; prompt cannot escalate | Locked #5; §4; §6 row 5 | **PASS** |
| 6 | Cross-agent mediated exfil posture | Locked #6; §4; §6 row 6 | **PASS** |
| 7 | Reject prompt-only / parallel ACL | Locked #7; Locked decisions; §6 row 7 | **PASS** |
| 8 | Dual wall all FieldClasses; consume tip | Locked #8/#10; §7 OUT; Sources; §6 row 8 | **PASS** |
| 9 | OUT / Gate / spend | Locked #10; §7 OUT; §8 Host; §6 row 9 | **PASS** |
| 10 | Traceability + handshake | Sources; §6; Constraints; §6 row 10 | **PASS** |

Security QA SoR confirm + Senior points-review both **PASS 10/10** (PR #72 @ `32d2e0bc`; checklist PR #71 @ `f64a3d11`). Spec §6 maps 1–10 with section cites — weave intact.

## Scope / constraints (locks)

| Constraint | Result | Evidence |
|------------|--------|----------|
| Soft #41 → #66+#67 only (not Stage B claim) | **PASS** | Locked #10; §7 OUT; soft #41 closes with #66 under this wall |
| Gate #26 backlog; #27 HOLD | **PASS** | Constraints; Locked #10; §7 OUT |
| Soft OTel/audit/idempotent weave only — no 5th Story | **PASS** | Locked #9; §5 |
| PoC $0; markdown Spec only | **PASS** | §8 Host; header; no product code |
| Separate from #66/#68/#69; parent #18 not Field-captured | **PASS** | Constraints; §7 OUT; Sources |
| Spec files unmodified by Spec QA | **PASS** | Evidence-only write |

## Done-lists / tests readiness

| Item | Result | Evidence |
|------|--------|----------|
| Spec QA Done-list items cover DOC-FLOW, locks, §9, Sec10, scope | **PASS** | Spec Done-list Spec QA section |
| §9.1 automated tests detail binding | **PASS** | Allowlist+scrub; strip; LoginEmail; pre-Accept; stranger; unauth; prompt-only rejected |
| Dev Plan / SD Done-list ready after Spec gate | **PASS** | Spec Dev Plan/SD section — implement after Spec QA + Security + Chief |
| No product code in Spec | **PASS** | Markdown Spec only |

## Gaps

**None.**

## Handshake status

1. Security Spec-step **PASS** on SoR (qa-confirm + points-review; PR #72 @ `32d2e0bc`; checklist PR #71 @ `f64a3d11`).  
2. Spec QA Spec-side + Spec gate **PASS** (this artifact).  
3. Confirm to **Chief Spec** only (parent messaging). Triad CLOSE / Dev Plan unlock remains Chief’s. Gate #26 backlog; #27 HOLD; soft #41 → #66+#67; PoC $0.
