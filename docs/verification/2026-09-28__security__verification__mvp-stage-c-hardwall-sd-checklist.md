# Security checklist — SD · MVP Stage C #67 Agent/tool hard wall + response scrubber (#18 dual-wall remainder)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/67 · Agent/tool hard wall + response scrubber  
**Parent:** #18 · Option A · Stage C · #18 dual-wall remainder (eng-facing slice — not a rewrite of #18 history)  
**Siblings:** #66 (must bind) · #68 · #69 — keep separate SD; cross-ref only  
**Order preference:** **#67 → #66 → #68 → #69 → #18** (this Story FIRST)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-qa-confirm.md`  
**Plan:** `plans/2026-09-28__devplan__plan__mvp-stage-c-agent-tool-hardwall-scrubber.md`  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-devplan-qa-confirm.md`  
**Tip:** `main` @ `dc8ee46` (Dev Plan SoR PR **#76** MERGED; checklist **#74** CLEAR)  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. Soft Soft CLOSE Soft HOLD Dev Code QA until SD-step Security PASS. Soft **#41** → **#66+#67** under wall. Eng dual-wall remainder = **#67**. Parent #18 framing-only — BIND #67; does **not** Field-capture #67. No Cognito/MM/DC4. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-checklist.md`

## Scope note
SD must **implement** Spec + Dev Plan Security for defense **#2**: agent runtime gateway + tool allowlist + server-side scrub via the **same** `IFieldPolicy.Evaluate` as API/DB. Real points — not N/A. Reject prompt-only soft wall; reject parallel agent ACL tables. Soft #41 Assistant OUT closes only via **#66+#67** delivery under this wall — not Stage B claim. Parent #18 is framing-only / maps slices — does **not** Field-capture #67. Evidence via tests/done-list.

## Itemized security points (SD must satisfy)

1. **Agent runtime gateway** — Implement agents on **platform tools only** (not raw DB, not arbitrary internal HTTP); deny/fail-closed off-gateway paths (verified in tests).

2. **Tool allowlist deny-by-default** — Implement each tool declaring FieldClasses it may `Read` / `ShareOutbound`; undeclared tools **denied** (verified).

3. **Server-side scrub before model** — Enforce every tool response / context pack through `IFieldPolicy.Evaluate` (or equivalent); **strip** denied fields **before** model context (verified).

4. **No LoginEmail in agent context** — Enforce LoginEmail User-only; OwnAgent Deny held; no LoginEmail tool and no LoginEmail in context packs (verified).

5. **ShareOutbound Accept-gated** — Enforce share tools (e.g. ContactEmail) check `HasAcceptGrant` / AcceptGrant **server-side**; deny regardless of prompt text; prompt injection cannot grant Evaluate denies (verified).

6. **Reject prompt-only / parallel ACL** — Reject system-prompt soft guidance as sole control; reject separate agent ACL tables that drift from FieldPolicy; no soft-wall-only or parallel-table code lands (verified).

7. **Dual wall all FieldClasses; soft #41 path** — Implement dual wall for open-ended FieldClass registry (CEO examples ≠ exhaustive); soft #41 OUT closes only with **#66** under this wall (not Stage B claim); keep #66/#68/#69 separate SD.

8. **Cross-agent mediated exfil posture** — If MVP scope includes cross-agent messaging, enforce payloads cannot include denied FieldClasses; threats addressed by gateway + scrub, not model trust (verified).

9. **OUT / Gate / spend** — Keep mature vault → **V3**; MCP/public tool marketplace OUT; Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/MM/DC4/vault invent; Soft OTel/audit/idempotent = weave only on named Stories (no 5th Story); PoC **$0**.

10. **Evidence + handshake** — Done-list cites paths/tests for 1–9; Dev Code QA / Product QA must **not** PASS until Security QA confirms. Parent #18 SD does **not** Field-capture this wall.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
