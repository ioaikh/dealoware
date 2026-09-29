# Security checklist — Product QA · MVP Stage C #67 Agent/tool hard wall + response scrubber

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA / QAQA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/67 · Agent/tool hard wall + response scrubber  
**Parent:** #18 · Option A · Stage C · eng dual-wall remainder = **#67**  
**Siblings:** #66 · #68 · #69 — **OUT** of this Story (keep separate Product QA; cross-ref only)  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/78 MERGED @ `dab5822`  
**SD Soft Soft CLOSE Soft HOLD SoR:** PR **#80** @ `8f3f643` · tip Soft Soft CLOSE Soft HOLD SoR @ `8f3f643`  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md`  
**Plan:** `plans/2026-09-28__devplan__plan__mvp-stage-c-agent-tool-hardwall-scrubber.md`  
**SD checklist (ref):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-checklist.md`  
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md`  
**Tests:** `tests/Dealoware.Api.Tests/StageCAgentHardwallTests.cs` (39)  
**Hold:** Soft Soft CLOSE Soft HOLD Doc until Product QA PASS. Soft Soft CLOSE Soft HOLD status:done until CBA BA-verify. Soft **#41** → **#66+#67** under wall. Gate **#26** backlog. Gate **#27** HOLD. **#66/#68/#69** OUT of this Story. Parent #18 framing-only — does **not** Field-capture #67. PoC **$0**; no MotorMarket/Cognito/DC4.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-checklist.md`

## Scope note
Product QA must **verify** Spec + Plan + SD Security for defense **#2**: agent runtime gateway + tool allowlist + server-side scrub via the **same** `IFieldPolicy.Evaluate` as API/DB. Real points — not N/A. Evidence via tests / done-list / QA runs (`StageCAgentHardwallTests.cs` ×39; SD qa-confirm PASS; Impl PR #78 @ `dab5822`). Do not invent requirements beyond the SD checklist. Soft Soft CLOSE Soft HOLD Doc until Product QA PASS; Soft Soft CLOSE Soft HOLD status:done until CBA BA-verify.

## Itemized security points (Product QA must verify)

1. **Agent runtime gateway** — Verify agents run on **platform tools only** (not raw DB, not arbitrary internal HTTP); re-spot deny/fail-closed off-gateway paths (tests / QA run evidence).

2. **Tool allowlist deny-by-default** — Verify each tool declares FieldClasses it may `Read` / `ShareOutbound`; confirm undeclared tools are **denied**.

3. **Server-side scrub before model** — Verify every tool response / context pack passes `IFieldPolicy.Evaluate` (or equivalent); confirm denied fields are **stripped before** model context.

4. **No LoginEmail in agent context** — Verify LoginEmail remains User-only; OwnAgent Deny held; confirm no LoginEmail tool and no LoginEmail in context packs.

5. **ShareOutbound Accept-gated** — Verify share tools (e.g. ContactEmail) check `HasAcceptGrant` / AcceptGrant **server-side**; confirm deny regardless of prompt text; prompt injection cannot grant Evaluate denies.

6. **Reject prompt-only / parallel ACL** — Confirm no system-prompt soft guidance as sole control; confirm no separate agent ACL tables that drift from FieldPolicy; re-spot no soft-wall-only or parallel-table delivery under #67.

7. **Dual wall all FieldClasses; soft #41 path** — Verify dual wall for open-ended FieldClass registry (CEO examples ≠ exhaustive); confirm soft #41 OUT closes only with **#66** under this wall (not Stage B claim); confirm **#66/#68/#69** remain OUT of this Story.

8. **Cross-agent mediated exfil posture** — If MVP scope includes cross-agent messaging, verify payloads cannot include denied FieldClasses; else confirm documented none; threats addressed by gateway + scrub, not model trust.

9. **OUT / Gate / spend** — Confirm mature vault → **V3**; MCP/public tool marketplace OUT; Gate **#26** backlog; Gate **#27** HOLD; no Cognito/MotorMarket/DC4/vault invent; Soft OTel/audit/idempotent = weave only on named Stories (no 5th Story); PoC **$0**.

10. **Handshake close** — Product QA / QAQA must **not** PASS until Security QA confirms Product QA points-review. Parent #18 framing-only does **not** Field-capture #67.

## Handshake next
Senior Product QA → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief QA + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
