# Security checklist — Product QA · MVP Stage C #69 Basic UI and/or one first-party bot (X2 partial)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA / QAQA PASS.
**Date:** 2026-09-28
**Author:** Dealoware Chief Security
**Story:** https://github.com/ioaikh/dealoware/issues/69 · Basic UI and/or one first-party bot surface (X2 partial)
**Parent:** #18 · Stage C · roadmap X2 MVP partial
**Siblings:** #66 · #67 · #68 — keep separate Product QA; cross-ref only
**Surface pick:** **basic UI** as X2 MVP minimum (exactly-one first-party bot **not** required; no multi-channel invent)
**Impl PR:** https://github.com/ioaikh/dealoware/pull/100 (tip @ `da61210`)
**CQ gate:** PASS `cq:no-refactor` @ `da61210`
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-qa-confirm.md`
**SD handshake SoR:** PR **#106** @ `259813e` CLEAR
**SD checklist (ref):** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-checklist.md`
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md`
**Tests:** `tests/Dealoware.Api.Tests/StageCBasicUITests.cs` (×32 cited at SD PASS)
**Hold:** Soft HOLD Doc until Product QA PASS (+ handshake SoR). Soft HOLD Product QA PASS until Senior Security → Security QA productqa-qa-confirm → QAQA. Soft **#41** CLOSED via **#66+#67**. Soft HOLD multi-provider until BM multi-provider start. Gate **#26** backlog; Gate **#27** HOLD. Parent #18 framing-only. PoC **$0**.
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-checklist.md`
**Tip:** `03f9659` (docs) / impl `da61210`

## Scope note
Product QA must **verify** Spec + Plan + SD Security for Spec-locked surface = **basic UI** that exercises MVP Participant flows **without** bypassing API FieldPolicy or agent/tool hard wall (#67). Real points — not N/A. Surface = basic UI only (not fuller bot). **No** UI-only security. Reject prompt-only soft wall on the client. Soft HOLD multi-provider. Evidence via StageCBasicUITests + SD qa-confirm PASS + impl tip `da61210`. Do not invent Cognito/MM/DC4/vault, Gate #26 unlock, multi-bot marketplace, or a 5th Story. Product QA SoR ≠ Doc.

## Itemized security points (Product QA must verify)

1. **No privileged back doors** — Verify basic UI does **not** bypass API FieldPolicy or agent/tool hard wall (#67); **no** UI-only filtering as security.

2. **Authn fail-closed on protected actions** — Verify unauthenticated protected actions → fail-closed (**401**); wrong principal → **403**/**404**; no private-field leakage via UI payloads.

3. **Assistant path binds #67** — Verify when the UI invokes thin Assistant (#66), tool/agent path binds hard wall + scrub; **reject** prompt-only soft wall invent on the client (do **not** re-score #67 as this Story).

4. **Budget status minimal only (#68)** — Verify budget status shown **minimally** to respect cutoff only — **not** platform-owner admin / mature cost UI (**V3**); no admin-suite invent.

5. **Surface pick = basic UI** — Confirm Spec-locked **basic UI** minimum only (not fuller bot); exactly-one first-party bot **not** required for this minimum; no multi-bot marketplace invent; Soft HOLD multi-provider until BM multi-provider start.

6. **OUT locked** — Confirm **X2 MVP partial**; public OpenAPI/webhooks → **V1**; MCP breadth → **V5**; Soft OTel/audit/idempotent = weave only on named Stories #66/#67/#68 (no 5th Story).

7. **Consume tip authz** — Verify exercises existing auth (#5) + FieldPolicy paths already on tip; does not rewrite Stage A/B ACL Stories.

8. **No Gate unlock / no invent** — Confirm Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/SSO/MM/DC4/vault invent; Soft **#41** CLOSED via **#66+#67**; keep #66/#67/#68 separate Product QA.

9. **Cost / spend** — Confirm PoC **$0**; any named LLM/API spend → **COO → CEO**.

10. **Handshake close** — Product QA / QAQA must **not** PASS until Security QA confirms Product QA points-review. Parent #18 framing-only does **not** Field-capture #69. Soft HOLD Doc until Product QA PASS (+ handshake SoR).

## Handshake next
Senior Product QA → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief QA + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
