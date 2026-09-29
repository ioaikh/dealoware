# Security checklist — Dev Plan · MVP Stage C #67 Agent/tool hard wall + response scrubber (#18 dual-wall remainder)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/67 · Agent/tool hard wall + response scrubber  
**Parent:** #18 · Option A · Stage C · #18 dual-wall remainder (eng-facing slice — not a rewrite of #18 history)  
**Siblings:** #66 (must bind) · #68 · #69 — keep separate plan; cross-ref only  
**Order preference:** **#67 → #66 → #68 → #69 → #18** (this Story FIRST)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-qa-confirm.md` (Spec Security SoR **#71+#72**; tip `main` @ `f133e90` Senior PM — Spec Soft Soft CLOSE Soft HOLD SoR CLEAR / PR #73 cited by PM for Spec)  
**Architecture:** Option A tip §3b + `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` (hard wall pick A)  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. SD HOLD until Dev Plan QA + Security PASS. Eng dual-wall remainder = **#67**. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate/verify** Spec Security for defense **#2**: agent runtime gateway + tool allowlist + server-side scrub via the **same** `IFieldPolicy.Evaluate` as API/DB. Real points — not N/A. Reject prompt-only soft wall; reject parallel agent ACL tables. Soft #41 Assistant OUT closes only via **#66+#67** delivery under this wall — not Stage B claim. Parent #18 is framing-only / maps slices — does **not** Field-capture #67.

## Itemized security points (Dev Plan must weave)

1. **Agent runtime gateway tasks** — Plan schedules agents on **platform tools only** (not raw DB, not arbitrary internal HTTP); verify fail-closed off-gateway paths.

2. **Tool allowlist deny-by-default tasks** — Plan schedules each tool declaring FieldClasses it may `Read` / `ShareOutbound`; undeclared tools denied; verify.

3. **Server-side scrub-before-model tasks** — Plan requires every tool response / context pack runs `IFieldPolicy.Evaluate` (or equivalent); denied fields stripped **before** model context; verify.

4. **No LoginEmail in agent context tasks** — Plan schedules LoginEmail User-only; OwnAgent Deny held; no LoginEmail tool / no LoginEmail in context packs; verify.

5. **ShareOutbound Accept-gated tasks** — Plan requires share tools (e.g. ContactEmail) check `HasAcceptGrant` / AcceptGrant **server-side**; deny regardless of prompt text; prompt injection cannot grant Evaluate denies; verify.

6. **Reject prompt-only / parallel ACL** — Plan explicitly **rejects** system-prompt soft guidance as sole control and **rejects** separate agent ACL tables that drift from FieldPolicy; verify no soft-wall-only or parallel-table tasks land.

7. **Dual wall all FieldClasses; soft #41 path** — Plan schedules dual wall for open-ended FieldClass registry (CEO examples ≠ exhaustive); soft #41 OUT closes only with **#66** under this wall (not Stage B claim); keep #66/#68/#69 separate plans.

8. **Cross-agent mediated exfil posture** — Plan schedules (if MVP scope includes cross-agent messaging) payloads cannot include denied FieldClasses; threats addressed by gateway + scrub, not model trust; verify.

9. **OUT / Gate / spend** — Plan keeps mature vault → **V3**; MCP/public tool marketplace OUT; Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/MM/DC4/vault invent; Soft OTel/audit/idempotent = weave only on named plans (no 5th Story); PoC **$0**.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then. Parent #18 Dev Plan does **not** Field-capture this wall.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
