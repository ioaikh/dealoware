# Security checklist — Doc · MVP Stage C #67 Agent/tool hard wall + response scrubber

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/67 · Agent/tool hard wall + response scrubber  
**Parent:** #18 · Option A · Stage C · eng dual-wall remainder = **#67**  
**Siblings:** #66 · #68 · #69 — **OUT** of this Story (keep separate Doc; cross-ref only)  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/78 MERGED @ `dab5822`  
**Product QA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-qa-confirm.md`  
**Product QA handshake SoR:** PR **#85** @ `22e84a1` (Product QA SoR ≠ Doc)  
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md`  
**Tests:** `tests/Dealoware.Api.Tests/StageCAgentHardwallTests.cs` (39)  
**Hold:** Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS). Soft **#41** → **#66+#67** under wall. Gate **#26** backlog. Gate **#27** HOLD. **#66/#68/#69** OUT of this Story. Parent #18 framing-only. PoC **$0**; no MotorMarket/Cognito/DC4.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-doc-checklist.md`  
**Tip:** `7043314`

## Scope note
Doc must accurately describe defense **#2**: agent runtime gateway + tool allowlist + server-side scrub via the **same** `IFieldPolicy.Evaluate` as API/DB. Product QA SoR ≠ Doc — this is a separate Doc-step checklist. Real points — not N/A. Do not invent #66/#68/#69 delivery, mature vault, MCP marketplace, Cognito, MotorMarket/DC4 as delivered. Soft Soft CLOSE Soft HOLD status:done stays (CBA PASS).

## Itemized security points (Doc must satisfy)

1. **Agent runtime gateway** — Docs state agents run on **platform tools only** (not raw DB, not arbitrary internal HTTP); deny/fail-closed off-gateway paths.

2. **Tool allowlist deny-by-default** — Docs state each tool declares FieldClasses it may Read / ShareOutbound; undeclared tools are denied.

3. **Server-side scrub before model** — Docs state every tool response / context pack passes `IFieldPolicy.Evaluate`; denied fields are stripped before model context.

4. **No LoginEmail in agent context** — Docs state LoginEmail remains User-only; OwnAgent Deny; no LoginEmail tool and no LoginEmail in context packs.

5. **ShareOutbound Accept-gated** — Docs state share tools (e.g. ContactEmail) check AcceptGrant server-side; prompt injection cannot grant Evaluate denies.

6. **Reject prompt-only / parallel ACL** — Docs state no system-prompt soft guidance as sole control; no separate agent ACL tables that drift from FieldPolicy.

7. **Dual wall + soft #41 path** — Docs state dual wall for the open-ended FieldClass registry; soft #41 Assistant OUT closes only with #66 under this wall (not a Stage B claim); #66/#68/#69 remain OUT of this Story.

8. **Cross-agent mediated exfil** — Docs state payloads cannot include denied FieldClasses if cross-agent messaging is in MVP scope; else documented none. Threats addressed by gateway + scrub, not model trust.

9. **OUT / Gate / spend** — Docs state mature vault → V3; MCP/public tool marketplace OUT; Gate #26 backlog; Gate #27 HOLD; no Cognito/MotorMarket/DC4/vault invent; Soft OTel/audit/idempotent = weave only (no 5th Story); PoC $0.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points. Parent #18 framing-only does not Field-capture #67.

## Handshake next
Senior Docs weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
