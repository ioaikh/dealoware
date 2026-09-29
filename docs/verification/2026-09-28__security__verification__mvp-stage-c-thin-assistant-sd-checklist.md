# Security checklist — SD · MVP Stage C #66 Thin Strategy-driven AI Assistant runtime (X1 thin)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/66 · Thin Strategy-driven AI Assistant runtime (X1 thin)  
**Parent:** #18 · Option A · Stage C named slice  
**Siblings:** #67 (hard wall — **mandatory bind**) · #68 · #69 — keep separate SD; cross-ref only  
**Order preference:** **#67 → #66 → #68 → #69 → #18** (#66 after #67)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-qa-confirm.md`  
**Plan:** `plans/2026-09-28__devplan__plan__mvp-stage-c-thin-assistant-runtime-x1.md`  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-devplan-qa-confirm.md`  
**Tip:** `main` @ `dc8ee46` (Dev Plan SoR PR **#76** MERGED; checklist **#74** CLEAR)  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. Soft Soft CLOSE Soft HOLD Dev Code QA until SD-step Security PASS. Soft **#41** → **#66+#67** under wall. Eng dual-wall remainder = **#67**. Parent #18 framing-only — BIND #67; does **not** Field-capture #67. No Cognito/MM/DC4. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-sd-checklist.md`

## Scope note
SD must **implement** Spec + Dev Plan Security for **thin** OwnAgent-only Strategy-driven Assistant runtime **under** the agent/tool hard wall (#67). Real points — not N/A. Must **bind #67**. Soft #41 Assistant OUT closes by delivering this runtime under #67 — not by claiming Stage B delivered Assistant. Do not invent fuller Assistant, multi-party agents, Gate #26 unlock, or a 5th Story. Evidence via tests/done-list.

## Itemized security points (SD must satisfy)

1. **OwnAgent-only 1:1** — Implement thin Assistant as **OwnAgent** for the owning Participant only (P6 spirit); never Counterparty / Stranger agent identity; no multi-party invent (verified).

2. **StrategyBody via FieldPolicy** — Enforce StrategyBody consume/write only when `IFieldPolicy.Evaluate` allows OwnAgent R/W for owner context; never expose another Participant’s StrategyBody (verified).

3. **Mandatory bind to #67 hard wall** — Implement runtime on **platform tools / gateway path only** (no raw DB / arbitrary internal HTTP); **reject** prompt-only soft wall as sole control; consume #67 allowlist + scrub (verified).

4. **No LoginEmail in agent context** — Strip/deny LoginEmail from model / agent context packs and tool outputs; LoginEmail remains User-only (distinct from ContactEmail) (verified).

5. **Authn / IDOR fail-closed** — Enforce unauthenticated → **401**; wrong principal / cross-tenant → **403** or **404**; uniform deny; **no** private-field leakage in responses or model context (verified).

6. **Soft #41 OUT closed by delivery path only** — Close soft #41 Assistant OUT by **#66 + #67** delivery under wall — do **not** claim Assistant/tool runtime was Stage B–delivered (verified in done-list).

7. **OUT locked (X1 thin)** — Keep **X1 MVP thin** only; fuller / stronger Assistant → **V1**; free-form Strategy engine → **V1**; Strategy sandbox (**A5**) → **V4**; BYO / multi-LLM breadth → later; Soft OTel/audit/idempotent = weave only (no 5th Story).

8. **Sibling / Gate HOLDs** — Do not invent #68/#69 surfaces into this Story; Gate **#26** stays backlog until delivery; Gate **#27** HOLD; no Cognito/SSO/MM/DC4/vault invent; keep siblings separate SD.

9. **Cost / spend** — PoC **$0**; any named LLM/API spend → escalate **COO → CEO**; do not provision spend without that path.

10. **Evidence + handshake** — Done-list cites paths/tests for 1–9; Dev Code QA / Product QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
