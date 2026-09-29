# Verification — Security points vs MVP Stage C #67 Agent/tool hard wall + response scrubber Doc

**Author:** Dealoware Senior Security  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 Doc-step Security points MET)  
**Checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-checklist.md` (10 points)  
**SoR checklist twin:** `docs/verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-checklist.md` (PR #90 @ `9cf6248`) CLEAR  
**Weave (locked):** `ops/2026-09-28__docs__ops__mvp-stage-c-hardwall-doc-security-weave.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/95 MERGED @ `7064c0fb3e4c4155455ee2383a157c1f25c2babf`  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/78 MERGED @ `dab5822`  
**Product QA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-qa-confirm.md`  
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/67 · Agent/tool hard wall + response scrubber  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-points-review.md`  
**Constraints:** Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX · Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS) · Product QA SoR ≠ Doc · Soft **#41** → **#66+#67** under wall (not Stage B) · **#66/#68/#69 OUT** of this Story · Parent #18 framing-only · Gate **#26** backlog · Gate **#27** HOLD · PoC **$0** · no Cognito/MotorMarket/DC4/vault invent · Soft Soft CLOSE Soft HOLD handshake SoR not invented here

## Checklist vs Doc surfaces

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Agent runtime gateway — platform tools only; fail-closed off-gateway | **MET** | Weave §pt1: agents on platform tools only (`AgentGateway`); not raw DB / arbitrary internal HTTP; off-gateway deny fail-closed. Supporting: PR #78 MERGED @ `dab5822` + StageCAgentHardwallTests; Product QA Sec PASS; SD Sec PASS. |
| 2 | Tool allowlist deny-by-default — FieldClasses declared; undeclared denied | **MET** | Weave §pt2: each tool declares FieldClasses Read / ShareOutbound; undeclared tools denied (`ToolAllowlist` / `FieldClassDeclaration`). Supporting Product QA / SD Sec PASS. |
| 3 | Server-side scrub before model — same IFieldPolicy.Evaluate; strip denied | **MET** | Weave §pt3: every tool response / context pack passes the **same** `IFieldPolicy.Evaluate` as API/DB; denied fields stripped before model (`AgentContextScrubber`). Supporting Product QA / SD Sec PASS. |
| 4 | No LoginEmail in agent context — User-only; OwnAgent Deny | **MET** | Weave §pt4: LoginEmail remains User-only; OwnAgent Deny; no LoginEmail tool; no LoginEmail in context packs. Supporting Product QA / SD Sec PASS. |
| 5 | ShareOutbound Accept-gated — AcceptGrant server-side; prompt cannot grant | **MET** | Weave §pt5: share tools (e.g. ContactEmail) check AcceptGrant server-side; prompt injection cannot grant Evaluate denies. Supporting Product QA / SD Sec PASS. |
| 6 | Reject prompt-only / parallel ACL | **MET** | Weave §pt6: no system-prompt soft guidance as sole control; no separate agent ACL tables that drift from FieldPolicy. Supporting Product QA / SD Sec PASS. |
| 7 | Dual wall + soft #41 path — #41 OUT closes only with #66 under wall; #66/#68/#69 OUT | **MET** | Weave §pt7 + Explicit separations: dual wall for open-ended FieldClass registry; soft #41 Assistant OUT closes only with **#66** under this wall (not Stage B); **#66/#68/#69 OUT** of this Story (cross-ref only). No invent of sibling delivery. |
| 8 | Cross-agent mediated exfil — if none in MVP, docs say none; gateway+scrub not model trust | **MET** | Weave §pt8: MVP Status — no cross-agent messaging path exists; future must be mediated + scrubbed; threats addressed by gateway + scrub, not model trust. Supporting Product QA / SD Sec PASS (README MVP none). |
| 9 | OUT / Gate / spend — vault→V3; MCP OUT; Gate #26 backlog; #27 HOLD; PoC $0 | **MET** | Weave §pt9 + Constraints: mature vault → V3; MCP/public tool marketplace OUT; Gate #26 backlog; Gate #27 HOLD; no Cognito/MotorMarket/DC4/vault invent; Soft OTel/audit/idempotent = weave only (no 5th Story); PoC **$0**. |
| 10 | Handshake close — Docs QA not PASS until Security QA; Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake SoR MERGED + INDEX; status:done stays | **MET** | Weave §pt10 **OPEN** + Close: Docs QA must **not** PASS until Security QA confirms these points; correctly HOLDs overall Doc PASS until handshake Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX. Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS). Parent #18 framing-only does not Field-capture #67. This points-review → Security QA. Handshake Soft Soft CLOSE Soft HOLD SoR **not invented**. |

## Soft notes (non-blocking)

- Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX — do **not** invent handshake SoR beyond this points-review + pending Security QA confirm.
- Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS; do not reopen).
- Product QA SoR ≠ Doc — Product QA Security PASS is supporting, not overall Doc PASS.
- Soft **#41 Assistant OUT** — closes only with **#66 + #67** under this wall (not a Stage B claim); #67 Docs deliver the wall only.
- **#66 Thin Assistant / #68 A8 meters / #69 UI/bot** — OUT of this Story; separate Doc tracks; cross-ref only; not scored as delivered.
- Parent #18 framing-only — does not Field-capture #67.
- Gate **#26** backlog; Gate **#27** HOLD; PoC **$0**.

## Gaps

**None.** #66/#68/#69 not scored here. Soft Soft CLOSE Soft HOLD handshake SoR not invented. Soft Soft CLOSE Soft HOLD status:done stays.

## Done-list (for Security QA)

- [x] DOC-FLOW filed
- [x] Weave + checklist + PR #95 MERGED @ `7064c0fb3e4c4155455ee2383a157c1f25c2babf` cites for pts 1–10
- [x] Binding checklist SoR twin PR #90 @ `9cf6248` CLEAR cited
- [x] Product QA Sec PASS + SD Sec PASS cited (supporting; not overall Doc PASS)
- [x] Impl PR #78 MERGED @ `dab5822` + tip after merge `7064c0f` cited
- [x] Soft #41 → #66+#67 under wall; #66/#68/#69 OUT; Gate #26 backlog; #27 HOLD; parent #18 not Field-capture; PoC $0; Soft Soft CLOSE Soft HOLD status:done stays; Soft Soft CLOSE Soft HOLD treat Doc as PASS until handshake SoR MERGED + INDEX (no invent)
- [ ] Security QA: confirm **PASS** to Chief Security **or** further instructions

## Cost/critical

None. PoC **$0**. Any named LLM/API/IdP spend → COO → CEO.
