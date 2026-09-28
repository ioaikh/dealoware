# Security checklist — Spec · MVP Stage C #69 Basic UI and/or one first-party bot (X2 partial)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/69 · Basic UI and/or one first-party bot surface (X2 partial)  
**Parent:** #18 · Stage C · roadmap X2 MVP partial  
**Siblings:** #66 · #67 · #68 — keep separate Spec; cross-ref only  
**Architecture:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` (§3d X2 pick A)  
**Prior Security PASS (SA step):** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Hold:** Gate **#26** backlog until delivery. Gate **#27** HOLD. OpenAPI/webhooks → **V1**; MCP → **V5**. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-checklist.md`

## Scope note
Spec-binding for **basic UI and/or exactly one** first-party bot that exercises MVP Participant flows **without** bypassing API FieldPolicy or agent/tool hard wall. Real points — not N/A. Spec picks minimum surface; no multi-channel marketplace invent.

## Itemized security points (Spec must bind)

1. **No privileged back doors** — Spec requires UI/bot must **not** bypass API FieldPolicy or agent/tool hard wall (#67); **no** UI-only filtering as security.

2. **Authn fail-closed on protected actions** — Unauthenticated protected actions → fail-closed (**401**); wrong principal → **403**/**404**; no private-field leakage via UI/bot payloads.

3. **Assistant/bot path binds #67** — Spec requires when the surface invokes thin Assistant (#66), tool/agent path binds hard wall + scrub; no prompt-only soft wall invent on the client.

4. **Budget status minimal only (#68)** — Spec may surface budget status **minimally** to respect cutoff — **not** platform-owner admin / mature cost UI (**V3**).

5. **Exactly one first-party bot and/or basic UI** — Spec picks UI and/or **exactly one** first-party bot; no multi-bot marketplace invent.

6. **OUT locked** — Spec documents **X2 MVP partial**; public OpenAPI/webhooks → **V1**; MCP breadth → **V5**; no inventing observability product (Soft weave on #66/#67/#68 only).

7. **Consume tip authz** — Spec exercises existing auth (#5) + FieldPolicy paths already on tip; does not rewrite Stage A/B ACL Stories.

8. **No Gate unlock / no invent** — Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/SSO/MM/DC4; no Marketing eng Story; no 5th Story.

9. **Cost / spend** — PoC **$0**; any spend → COO → CEO.

10. **Traceability + handshake** — Spec cites #69 AC + roadmap X2 + SA Security PASS; keep #66/#67/#68 separate. Spec QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Spec weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Spec + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
