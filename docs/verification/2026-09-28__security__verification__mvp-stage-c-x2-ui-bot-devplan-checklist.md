# Security checklist — Dev Plan · MVP Stage C #69 Basic UI and/or one first-party bot (X2 partial)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/69 · Basic UI and/or one first-party bot surface (X2 partial)  
**Parent:** #18 · Stage C · roadmap X2 MVP partial  
**Siblings:** #66 · #67 · #68 — keep separate plan; cross-ref only  
**Order preference:** **#67 → #66 → #68 → #69 → #18** (#69 after #68)  
**Surface pick (from Spec):** **basic UI** as X2 MVP minimum (exactly-one first-party bot **not** required for this minimum; no multi-channel invent)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-qa-confirm.md` (Spec Security SoR **#71+#72**; tip `main` @ `f133e90` Senior PM — Spec Soft Soft CLOSE Soft HOLD SoR CLEAR / PR #73 cited by PM for Spec)  
**Architecture:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` (§3d X2 pick A)  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. SD HOLD until Dev Plan QA + Security PASS. OpenAPI/webhooks → **V1**; MCP → **V5**. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate/verify** Spec Security for Spec-locked surface = **basic UI** that exercises MVP Participant flows **without** bypassing API FieldPolicy or agent/tool hard wall (#67). Real points — not N/A. **No** UI-only security. Reject prompt-only soft wall on the client. Do not invent multi-bot marketplace, Cognito/MM/DC4/vault, Gate #26 unlock, or a 5th Story.

## Itemized security points (Dev Plan must weave)

1. **No privileged back doors** — Plan requires basic UI must **not** bypass API FieldPolicy or agent/tool hard wall (#67); **no** UI-only filtering as security; verify.

2. **Authn fail-closed on protected actions** — Plan schedules unauthenticated protected actions → fail-closed (**401**); wrong principal → **403**/**404**; no private-field leakage via UI payloads; verify.

3. **Assistant path binds #67** — Plan requires when the UI invokes thin Assistant (#66), tool/agent path binds hard wall + scrub; **rejects** prompt-only soft wall invent on the client; verify.

4. **Budget status minimal only (#68)** — Plan may schedule budget status **minimally** to respect cutoff — **not** platform-owner admin / mature cost UI (**V3**); verify no admin-suite invent.

5. **Surface pick = basic UI** — Plan schedules Spec-locked **basic UI** minimum; exactly-one first-party bot **not** required for this minimum; no multi-bot marketplace invent; verify.

6. **OUT locked** — Plan keeps **X2 MVP partial**; public OpenAPI/webhooks → **V1**; MCP breadth → **V5**; Soft OTel/audit/idempotent = weave only on named plans #66/#67/#68 (no 5th Story / no observability product invent).

7. **Consume tip authz** — Plan exercises existing auth (#5) + FieldPolicy paths already on tip; does not rewrite Stage A/B ACL Stories; verify.

8. **No Gate unlock / no invent** — Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/SSO/MM/DC4/vault invent; no Marketing eng Story; keep #66/#67/#68 separate plans.

9. **Cost / spend** — PoC **$0**; any spend → COO → CEO.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
