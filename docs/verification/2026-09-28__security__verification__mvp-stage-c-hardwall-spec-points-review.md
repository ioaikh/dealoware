# Verification — Security points vs Stage C #67 Spec (agent/tool hard wall + scrubber)

**Author:** Dealoware Senior Security  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 Spec-step Security points MET)  
**Checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-checklist.md` (10 points)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md` (§6 Security weave + Locked decisions + §§1–4/7–9)  
**Prior SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Architecture:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` (hard wall pick A §2a/§3a)  
**Story:** https://github.com/ioaikh/dealoware/issues/67 · Parent #18 dual-wall remainder (eng-facing slice)  
**Checklist SoR:** PR #71 @ `f64a3d11`  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-points-review.md`  
**Constraints:** Gate **#26** backlog until Stage C delivery · Gate **#27** HOLD · PoC **$0** · no MM/DC4 · no Cognito invent · keep #66/#68/#69 separate · parent #18 Spec not Field-captured here · soft OTel/audit/idempotent = weave only (no 5th Story)

## Scope note

This is the **Spec-step** Security score for **#67** defense #2 (agent runtime gateway + tool allowlist + same-`IFieldPolicy.Evaluate` scrub). Not Gate #26 post-delivery. Soft #41 Assistant OUT closes with sibling **#66** under this wall — not Stage B claim.

## Checklist vs Spec

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Separate agent runtime gateway | **MET** | Spec Locked #0/#1 + §1: separate **agent runtime gateway**; agents → **platform tools only**; forbidden raw DB / arbitrary internal HTTP / privileged back doors that skip Evaluate. Same Domain FieldPolicy as API/DB wall (defense #2). §6 Security weave row 1; AC §9. |
| 2 | Tool allowlist deny-by-default | **MET** | Locked #2 + §2: each allowlisted tool declares FieldClasses it may `Read` / `ShareOutbound`; undeclared → **deny**. Examples ≠ exhaustive; no inventing FieldClasses. §6#2; AC §9. |
| 3 | Server-side scrub before model context | **MET** | Locked #3 + §3: every tool response / context pack runs same `IFieldPolicy.Evaluate` (or equivalent); denied fields **stripped before** model / agent context; no parallel scrub table. §6#3; §9.1 denied-field strip case. |
| 4 | No LoginEmail tool / never in agent context | **MET** | Locked #4 + §4: LoginEmail User-only; **no** LoginEmail tool; **never** in agent context packs; OwnAgent Deny held from Stage A. §6#4; §9.1 LoginEmail case. |
| 5 | ShareOutbound Accept-gated; prompt cannot escalate | **MET** | Locked #5 + §4: share tools (e.g. `share_contact_email`) require `HasAcceptGrant` / Stage B AcceptGrant **server-side**; deny regardless of prompt text; prompt injection **cannot** grant Evaluate denies. §6#5; §9.1 pre-Accept deny. |
| 6 | Cross-agent mediated exfil posture | **MET** | Locked #6 + §4: if any cross-agent messaging at MVP → mediated gateway path; payloads scrubbed; no denied FieldClasses; threats addressed by gateway + scrub — **not** model trust. §6#6; AC §9. |
| 7 | Reject prompt-only / parallel ACL | **MET** | Locked #7 + Locked decisions: explicitly **rejects** system-prompt soft guidance as sole control and **rejects** separate agent ACL tables that drift from FieldPolicy. §6#7; §9.1 prompt-only rejected test. |
| 8 | Dual wall all FieldClasses; consume tip | **MET** | Locked #8/#10 + §7 OUT + Sources: open-ended registry (CEO examples ≠ exhaustive); dual wall for **all** FieldClasses via same Evaluate; consumes Stage A/B tip / Option A §3b without rewriting; soft #41 OUT closes with **#66** under this wall (not Stage B claim). §6#8. |
| 9 | OUT / Gate / spend | **MET** | Locked #10 + §7 OUT + §8 Host: mature vault → **V3**; MCP/public marketplace OUT; Gate **#26** backlog; Gate **#27** HOLD; no Cognito/MM/DC4; PoC **$0**; cost/critical → COO → CEO; soft OTel/audit/idempotent = §5 weave only (no 5th Story). §6#9. |
| 10 | Traceability + handshake | **MET** | Sources + §6 + Constraints: cites #67 AC + Option A §3b + CA PASS hard-wall pick A + SA Security PASS (`...-sa-qa-confirm.md`); keeps #66/#68/#69 separate; parent #18 Spec not Field-captured here; Spec QA must **not** PASS until Security QA confirms. §6#10. |

## Soft notes (non-blocking)

- Soft **#41 Assistant OUT** — Spec correctly closes by design with sibling **#66** under this wall; delivery evidence remains Stage C eng / Gate #26.
- Soft OTel/audit/idempotent — §5 weave-only touchpoints; no 5th Story invented (aligns with Spec-step locks).
- Soft sibling Specs (#66/#68/#69) and parent #18 framing — correctly OUT / cross-ref only; Stories kept separate.

## Gaps

**None.**

## Done-list

- [x] Scored Spec §6 Security weave + Locked + §§1–4/7–9 vs Chief checklist points 1–10
- [x] **PASS** 10/10 MET — DOC-FLOW filed
- [x] Soft #41 OUT / Gate #26 backlog / #27 HOLD / PoC $0 / no MM / no 5th Story / Stories separate
- [ ] → Security QA confirm (Spec QA HOLD until confirm)
- [ ] → Chief Security after Security QA PASS

## Cost/critical

None. PoC **$0**. Any named LLM/API spend → COO → CEO.
