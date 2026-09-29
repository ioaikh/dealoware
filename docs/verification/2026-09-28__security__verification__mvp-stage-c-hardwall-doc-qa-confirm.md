# Security QA — MVP Stage C #67 Agent/tool hard wall + response scrubber Doc vs Chief Security Doc checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 Doc-step Security points MET)  
**Asked by:** Senior Security / Chief Security — Doc Security handshake (PRIORITY — Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX)  
**Chief checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-checklist.md` (10 points)  
**SoR checklist twin (GitHub):** `docs/verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-checklist.md` (PR **#90** @ `9cf6248`) CLEAR  
**Senior Security done-list:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-points-review.md` (**PASS** 10/10 — cited; independently re-scored; agrees)  
**Doc weave (locked):** `ops/2026-09-28__docs__ops__mvp-stage-c-hardwall-doc-security-weave.md`  
**Weave SoR PR:** https://github.com/ioaikh/dealoware/pull/95 (**MERGED**) @ `7064c0fb3e4c4155455ee2383a157c1f25c2babf`  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/78 (**MERGED**) @ `dab5822`  
**Product QA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-qa-confirm.md` (supporting; Product QA SoR ≠ Doc)  
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md` (supporting; SD Sec only)  
**Tests:** `tests/Dealoware.Api.Tests/StageCAgentHardwallTests.cs` (39) — supporting code surface  
**Issue:** https://github.com/ioaikh/dealoware/issues/67 · Agent/tool hard wall + response scrubber  
**Parent:** #18 · Option A · Stage C · eng dual-wall remainder = **#67** — framing-only; does **not** Field-capture #67  
**Siblings:** #66 · #68 · #69 — **OUT** of this Story (keep separate Doc; cross-ref only; not scored / not confirmed here)  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-qa-confirm.md`  
**Constraints:** Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX (do **not** invent handshake SoR / claim Docs unlock). Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS). Product QA SoR ≠ Doc. Soft **#41** → **#66+#67** under wall (not Stage B; #67 Docs deliver the wall only). **#66/#68/#69 OUT**. Soft HOLD #69/#18. Gate **#26** backlog; Gate **#27** HOLD. Parent #18 framing-only. PoC **$0**; no Cognito/MotorMarket/DC4/vault invent. Path is **hardwall-doc** (not thin-assistant-doc).

## Soft notes accepted (non-blocking)

| Soft note | Disposition |
|-----------|-------------|
| Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX | **Accepted** — this confirm does **not** invent handshake SoR beyond Senior points-review + this file; does **not** claim Docs unlock / overall Doc PASS as MERGED SoR |
| Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS) | **Accepted** — do not reopen; not cleared by this confirm |
| Product QA SoR ≠ Doc | **Accepted** — Product QA Security PASS is supporting only; not overall Doc PASS |
| Soft **#41 Assistant OUT** — closes only with **#66 + #67** under this wall | **Accepted** — not a Stage B claim; #67 Docs deliver the wall only; #66 not scored as delivered here |
| **#66 / #68 / #69 OUT** | **Accepted** — separate Doc tracks; cross-ref only; not scored / not confirmed here |
| Soft HOLD #69 / parent #18 | **Accepted** — #69 OUT; #18 framing-only does not Field-capture #67 |
| Gate **#26** backlog; Gate **#27** HOLD; PoC **$0** | **Accepted** — held; no Cognito/MM/DC4/vault invent |
| Soft OTel/audit/idempotent = weave only (no 5th Story) | **Accepted** — aligns Senior + weave §pt9 |

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security Doc checklist (KB) | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-checklist.md` | Binding 10 points |
| SoR checklist twin (GitHub) | `docs/verification/…-hardwall-doc-checklist.md` (PR #90 @ `9cf6248`) | CLEAR — Soft Soft CLOSE Soft HOLD SoR twin only (≠ handshake SoR) |
| Doc Security weave (locked) | `ops/2026-09-28__docs__ops__mvp-stage-c-hardwall-doc-security-weave.md` | Soft Soft CLOSE Soft HOLD overall Doc until handshake SoR MERGED + INDEX; pts 1–10 mapped |
| Weave SoR PR #95 | MERGED @ `7064c0fb3e4c4155455ee2383a157c1f25c2babf` | Verified `get_pull_request` merged=true |
| Senior Doc points-review | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-points-review.md` | **PASS** 10/10 — present; aligned |
| Product QA Security PASS | `…hardwall-productqa-qa-confirm.md` | Supporting (Sec10 Product QA closed; not overall Doc PASS) |
| SD Security PASS | `…hardwall-sd-qa-confirm.md` | Supporting (SD Sec only; not overall Doc PASS) |
| Impl PR #78 | MERGED @ `dab5822` + StageCAgentHardwallTests ×39 | Supporting code / Doc surface |
| INDEX.md | hardwall-doc-checklist + weave indexed (~514 / ~578) | MATCH per weave; handshake files **not** invented / not claimed INDEX-cleared here |

## Independent re-score (Security QA)

Score vs **official Doc checklist 1–10** only. Surfaces: locked weave + INDEX annotations + Doc surfaces cited by Senior (PR #78 / AgentGateway / ToolAllowlist / AgentContextScrubber / README MVP none / StageCAgentHardwallTests). Soft notes non-blocking.

| # | Point | Senior | Security QA | Evidence |
|---|-------|--------|-------------|---------|
| 1 | Agent runtime gateway — platform tools only; fail-closed off-gateway | MET | **MET** | Weave §pt1: agents on platform tools only (`AgentGateway`); not raw DB / arbitrary internal HTTP; off-gateway deny fail-closed. Supporting: PR #78 MERGED @ `dab5822` + StageCAgentHardwallTests; Product QA / SD Sec PASS. |
| 2 | Tool allowlist deny-by-default — FieldClasses declared; undeclared denied | MET | **MET** | Weave §pt2: each tool declares FieldClasses Read / ShareOutbound; undeclared tools denied (`ToolAllowlist` / `FieldClassDeclaration`). Supporting Product QA / SD Sec PASS. |
| 3 | Server-side scrub before model — same IFieldPolicy.Evaluate; strip denied | MET | **MET** | Weave §pt3: every tool response / context pack passes the **same** `IFieldPolicy.Evaluate` as API/DB; denied fields stripped before model (`AgentContextScrubber`). Supporting Product QA / SD Sec PASS. |
| 4 | No LoginEmail in agent context — User-only; OwnAgent Deny | MET | **MET** | Weave §pt4: LoginEmail remains User-only; OwnAgent Deny; no LoginEmail tool; no LoginEmail in context packs. Supporting Product QA / SD Sec PASS. |
| 5 | ShareOutbound Accept-gated — AcceptGrant server-side; prompt cannot grant | MET | **MET** | Weave §pt5: share tools (e.g. ContactEmail) check AcceptGrant server-side; prompt injection cannot grant Evaluate denies. Supporting Product QA / SD Sec PASS. |
| 6 | Reject prompt-only / parallel ACL | MET | **MET** | Weave §pt6: no system-prompt soft guidance as sole control; no separate agent ACL tables that drift from FieldPolicy. Supporting Product QA / SD Sec PASS. |
| 7 | Dual wall + soft #41 path — #41 OUT closes only with #66 under wall; #66/#68/#69 OUT | MET | **MET** | Weave §pt7 + Explicit separations: dual wall for open-ended FieldClass registry; soft #41 Assistant OUT closes only with **#66** under this wall (not Stage B); **#66/#68/#69 OUT** of this Story (cross-ref only). No invent of sibling delivery. |
| 8 | Cross-agent mediated exfil — if none in MVP, docs say none; gateway+scrub not model trust | MET | **MET** | Weave §pt8: MVP Status — no cross-agent messaging path exists; future must be mediated + scrubbed; threats addressed by gateway + scrub, not model trust. Supporting Product QA / SD Sec PASS (README MVP none). |
| 9 | OUT / Gate / spend — vault→V3; MCP OUT; Gate #26 backlog; #27 HOLD; PoC $0 | MET | **MET** | Weave §pt9 + Constraints: mature vault → V3; MCP/public tool marketplace OUT; Gate #26 backlog; Gate #27 HOLD; no Cognito/MotorMarket/DC4/vault invent; Soft OTel/audit/idempotent = weave only (no 5th Story); PoC **$0**. |
| 10 | Handshake close — Docs QA not PASS until Security QA; Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake SoR MERGED + INDEX; status:done stays | MET | **MET** | Weave §pt10 **OPEN** + Close correctly HOLDs overall Doc PASS until handshake Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX. Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS). Parent #18 framing-only does not Field-capture #67. This confirm closes Doc Security gate for Chief; handshake Soft Soft CLOSE Soft HOLD SoR **not invented**. |

## Alignment with Senior Security done-list

Senior Doc points-review scored pts **1–10 MET** on locked weave (PR #95 @ `7064c0f`) + binding checklist SoR twin (PR #90 @ `9cf6248`) + Product QA / SD Sec PASS supporting cites + Impl PR #78 @ `dab5822`, with soft notes on Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake SoR MERGED + INDEX, status:done stays, Product QA SoR ≠ Doc, soft #41 → #66+#67 under wall, and #66/#68/#69 OUT. Independent Security QA re-score **agrees** on all 10; soft notes **accepted**. No bounce. No gaps vs Senior. Senior catch-up was present at confirm time — **aligned**.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Agent runtime gateway; platform tools only; fail-closed off-gateway | Held (weave §pt1 + PR #78 / AgentGateway) |
| Tool allowlist deny-by-default; FieldClass Read/ShareOutbound | Held (weave §pt2 + ToolAllowlist) |
| Server-side scrub via same `IFieldPolicy.Evaluate`; strip before model | Held (weave §pt3 + AgentContextScrubber) |
| No LoginEmail tool / context; OwnAgent Deny held | Held (weave §pt4) |
| ShareOutbound Accept-gated server-side; prompt cannot escalate | Held (weave §pt5) |
| Reject prompt-only / parallel ACL tables | Held (weave §pt6) |
| Dual wall open-ended registry; soft #41 → #66 under wall; siblings OUT | Held (weave §pt7 + Explicit separations) |
| Cross-agent: MVP documents none; gateway+scrub not model trust | Held (weave §pt8 + README MVP none) |
| Gate #26 backlog; #27 HOLD; PoC $0; no Cognito/MM/DC4/vault invent | Held (weave §pt9) |
| Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake SoR MERGED + INDEX; status:done stays; Product QA SoR ≠ Doc; handshake SoR not invented | Held (Senior + Security QA agree) |
| Path hardwall-doc (not thin-assistant-doc); #66/#68/#69 not confirmed | Held |

## Gaps

**None.** Soft notes non-blocking. **#66/#68/#69 OUT** (not scored). Soft Soft CLOSE Soft HOLD handshake SoR not invented. Soft Soft CLOSE Soft HOLD status:done stays. Soft #41 Assistant OUT not claimed closed by this confirm alone.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Next: Chief Security **PASS/HOLD** to CPM + Chief Docs. Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX — do **not** invent handshake SoR / claim Docs unlock from this file alone. Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS; do not reopen). Cost/critical: none. PoC **$0**. Do **not** open Gate #26. Do **not** treat this as #66 / #68 / #69 / parent #18 confirm. Gate **#27** HOLD. Soft #41 Assistant OUT closes only via **#66+#67** under wall — not Stage B claim. Product QA SoR ≠ Doc.
