# Security checklist — Spec · MVP Stage C #67 Agent/tool hard wall + response scrubber (#18 dual-wall remainder)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/67 · Agent/tool hard wall + response scrubber  
**Parent:** #18 · Option A §3b dual-wall remainder (eng-facing slice — not a rewrite of #18 history)  
**Siblings:** #66 (must bind) · #68 · #69 — keep separate Spec; cross-ref only  
**Architecture:** Option A tip §3b + `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` (hard wall pick A)  
**Prior Security PASS (SA step):** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Baselines:** Stage A FieldPolicy API/DB wall on tip; Stage B ShareOutbound-after-Accept; soft #41 OUT closes with #66 under this wall  
**Hold:** Gate **#26** backlog until Stage C delivery. Gate **#27** HOLD. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-checklist.md`

## Scope note
Spec-binding for **defense #2**: agent runtime gateway + tool allowlist + server-side scrub via the **same** `IFieldPolicy.Evaluate` as API/DB. Real points — not N/A. Reject prompt-only soft guidance and parallel agent ACL tables. Dual wall for **all** FieldClasses (open-ended registry).

## Itemized security points (Spec must bind)

1. **Separate agent runtime gateway** — Spec binds agents to **platform tools only** (not raw DB, not arbitrary internal HTTP).

2. **Tool allowlist deny-by-default** — Spec binds each tool to declare FieldClasses it may `Read` / `ShareOutbound`; undeclared tools denied.

3. **Server-side scrub before model context** — Spec requires every tool response / context pack runs `IFieldPolicy.Evaluate` (or equivalent); denied fields stripped **before** model context.

4. **No LoginEmail tool / never in agent context** — Spec binds LoginEmail User-only; OwnAgent Deny held; no LoginEmail in context packs.

5. **ShareOutbound Accept-gated; prompt cannot escalate** — Spec binds share tools (e.g. ContactEmail) to require `HasAcceptGrant` / AcceptGrant **server-side**; deny regardless of prompt text; prompt injection cannot grant Evaluate denies.

6. **Cross-agent mediated exfil posture** — Spec binds any cross-agent messaging (if in MVP scope) so payloads cannot include denied FieldClasses; threats addressed by gateway + scrub, not model trust.

7. **Reject prompt-only / parallel ACL** — Spec explicitly **rejects** system-prompt soft guidance as sole control and **rejects** separate agent ACL tables that drift from FieldPolicy.

8. **Dual wall all FieldClasses; consume tip** — Spec binds dual wall for open-ended FieldClass registry (CEO examples ≠ exhaustive); consumes Stage A/B tip without rewriting Option A; soft #41 OUT closes with #66 under this wall (not Stage B claim).

9. **OUT / Gate / spend** — Mature vault → **V3**; MCP/public tool marketplace OUT; Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/MM/DC4; PoC **$0**; cost/critical → COO → CEO.

10. **Traceability + handshake** — Spec cites #67 AC + Option A §3b + SA Security PASS; keep #66/#68/#69 separate; parent #18 Spec not Field-captured here. Spec QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Spec weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Spec + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
