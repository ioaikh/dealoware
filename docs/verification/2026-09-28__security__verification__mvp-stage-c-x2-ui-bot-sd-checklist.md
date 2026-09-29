# Security checklist — SD · MVP Stage C #69 Basic UI and/or one first-party bot (X2 partial)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/69 · Basic UI and/or one first-party bot surface (X2 partial)  
**Parent:** #18 · Stage C · roadmap X2 MVP partial  
**Siblings:** #66 · #67 · #68 — keep separate SD; cross-ref only  
**Order preference:** **#67 → #66 → #68 → #69 → #18** (#69 after #68)  
**Surface pick (from Spec):** **basic UI** as X2 MVP minimum (exactly-one first-party bot **not** required for this minimum; no multi-channel invent)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-qa-confirm.md`  
**Plan:** `plans/2026-09-28__devplan__plan__mvp-stage-c-basic-ui-first-party-bot-x2.md`  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-devplan-qa-confirm.md`  
**Tip:** `main` @ `dc8ee46` (Dev Plan SoR PR **#76** MERGED; checklist **#74** CLEAR)  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. Soft Soft CLOSE Soft HOLD Dev Code QA until SD-step Security PASS. Soft **#41** → **#66+#67** under wall. Eng dual-wall remainder = **#67**. Parent #18 framing-only — BIND #67; does **not** Field-capture #67. No Cognito/MM/DC4. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-checklist.md`

## Scope note
SD must **implement** Spec + Dev Plan Security for Spec-locked surface = **basic UI** that exercises MVP Participant flows **without** bypassing API FieldPolicy or agent/tool hard wall (#67). Real points — not N/A. Surface = basic UI only (not fuller bot). **No** UI-only security. Reject prompt-only soft wall on the client. Consume wall/auth; do not invent Cognito/MM/DC4/vault, Gate #26 unlock, multi-bot marketplace, or a 5th Story. Evidence via tests/done-list.

## Itemized security points (SD must satisfy)

1. **No privileged back doors** — Implement basic UI so it does **not** bypass API FieldPolicy or agent/tool hard wall (#67); **no** UI-only filtering as security (verified).

2. **Authn fail-closed on protected actions** — Enforce unauthenticated protected actions → fail-closed (**401**); wrong principal → **403**/**404**; no private-field leakage via UI payloads (verified).

3. **Assistant path binds #67** — When the UI invokes thin Assistant (#66), enforce tool/agent path binds hard wall + scrub; **reject** prompt-only soft wall invent on the client (verified).

4. **Budget status minimal only (#68)** — Show budget status **minimally** to respect cutoff only — **not** platform-owner admin / mature cost UI (**V3**); no admin-suite invent (verified).

5. **Surface pick = basic UI** — Implement Spec-locked **basic UI** minimum only (not fuller bot); exactly-one first-party bot **not** required for this minimum; no multi-bot marketplace invent (verified).

6. **OUT locked** — Keep **X2 MVP partial**; public OpenAPI/webhooks → **V1**; MCP breadth → **V5**; Soft OTel/audit/idempotent = weave only on named Stories #66/#67/#68 (no 5th Story / no observability product invent).

7. **Consume tip authz** — Exercise existing auth (#5) + FieldPolicy paths already on tip; do not rewrite Stage A/B ACL Stories (verified).

8. **No Gate unlock / no invent** — Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/SSO/MM/DC4/vault invent; no Marketing eng Story; keep #66/#67/#68 separate SD.

9. **Cost / spend** — PoC **$0**; any spend → COO → CEO.

10. **Evidence + handshake** — Done-list cites paths/tests for 1–9; Dev Code QA / Product QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
