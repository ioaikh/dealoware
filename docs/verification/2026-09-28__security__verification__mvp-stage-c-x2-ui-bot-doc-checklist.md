# Security checklist — Doc · MVP Stage C #69 Basic UI and/or one first-party bot (X2 partial)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.
**Date:** 2026-09-28
**Author:** Dealoware Chief Security
**Story:** https://github.com/ioaikh/dealoware/issues/69 · Basic UI and/or one first-party bot surface (X2 partial)
**Parent:** #18 · Stage C · roadmap X2 MVP partial
**Siblings:** #66 · #67 · #68 — keep separate Doc; cross-ref only
**Surface pick:** **basic UI** as X2 MVP minimum (exactly-one first-party bot **not** required; no multi-channel invent)
**Impl PR:** https://github.com/ioaikh/dealoware/pull/100 MERGED @ `da61210`
**Product QA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-qa-confirm.md`
**Product QA handshake SoR:** PR **#115** @ `3b7b323` (Product QA SoR ≠ Doc)
**Product QA checklist SoR:** PR **#112** @ `fdc26b9`
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-qa-confirm.md`
**SD handshake SoR:** PR **#106** @ `259813e`
**Tests:** `tests/Dealoware.Api.Tests/StageCBasicUITests.cs` (×32)
**Hold:** Treat #69 Doc as PASS until handshake SoR MERGED + INDEX. `status:done` until CBA. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft **#41** CLOSED via **#66+#67**. Gate **#26** backlog; Gate **#27** HOLD. Parent #18 framing-only. PoC **$0**; no MotorMarket/Cognito/DC4.
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-doc-checklist.md`
**Tip:** `3b7b323`

## Scope note
Doc must accurately describe Spec-locked **basic UI** (X2 MVP partial) that exercises MVP Participant flows **without** bypassing API FieldPolicy or agent/tool hard wall (#67). Product QA SoR ≠ Doc — this is a separate Doc-step checklist. Real points — not N/A. **No** UI-only security. Soft HOLD multi-provider. Do not invent Cognito/MM/DC4/vault, Gate #26 unlock, multi-bot marketplace, fuller bot channel, or a 5th Story.

## Itemized security points (Doc must satisfy)

1. **No privileged back doors** — Docs state basic UI does **not** bypass API FieldPolicy or agent/tool hard wall (#67); **no** UI-only filtering as security; UI is not a security boundary.

2. **Authn fail-closed on protected actions** — Docs state unauthenticated protected actions → fail-closed (**401**); wrong principal → **403**/**404**; no private-field leakage via UI payloads.

3. **Assistant path binds #67** — Docs state when the UI invokes thin Assistant (#66), tool/agent path binds hard wall + scrub; **reject** prompt-only soft wall invent on the client (do **not** re-score #67 as this Story).

4. **Budget status minimal only (#68)** — Docs state budget status shown **minimally** to respect cutoff only — **not** platform-owner admin / mature cost UI (**V3**); no admin-suite invent.

5. **Surface pick = basic UI** — Docs state Spec-locked **basic UI** minimum only (not fuller bot); exactly-one first-party bot **not** required for this minimum; no multi-bot marketplace invent; Soft HOLD multi-provider until BM multi-provider start.

6. **OUT locked** — Docs state **X2 MVP partial**; public OpenAPI/webhooks → **V1**; MCP breadth → **V5**; Soft OTel/audit/idempotent = weave only on named Stories #66/#67/#68 (no 5th Story).

7. **Consume tip authz** — Docs state exercises existing auth (#5) + FieldPolicy paths already on tip; does not rewrite Stage A/B ACL Stories.

8. **No Gate unlock / no invent** — Docs state Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/SSO/MM/DC4/vault invent; Soft **#41** CLOSED via **#66+#67**; keep #66/#67/#68 separate Doc.

9. **Cost / spend** — Docs state PoC **$0**; any named LLM/API spend → **COO → CEO**.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points. Parent #18 framing-only does **not** Field-capture #69.

## Handshake next
Senior Docs weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
