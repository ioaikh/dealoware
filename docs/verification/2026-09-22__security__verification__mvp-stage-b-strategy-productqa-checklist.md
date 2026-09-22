# Security checklist — Product QA · MVP Stage B #41 Minimal Strategy create/edit (P3)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/41 · Minimal Strategy create/edit (P3 partial)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #42 — separate Product QA tracks  
**PR:** https://github.com/ioaikh/dealoware/pull/51 MERGED @ `43adb28…` (CQ cq:no-refactor)  
**SD Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-sd-qa-confirm.md` (SoR PR #54)  
**SD checklist (ref):** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-sd-checklist.md` (SoR PR #49)  
**Hold:** Gate **#25** backlog. Stage C Assistant (#26) + #27 + #18 Spec/SD (whole) HOLD. Soft **Assistant OUT** — OwnAgent = API policy only. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-productqa-checklist.md`

## Scope note
Product QA must evidence that SD Security constraints for minimal Strategy CRUD + **StrategyBody** FieldClass ACL (Owner+OwnAgent only) hold on delivered PR #51. Soft gap no-live-dotnet OK if stated with equivalent evidence (CI + StrategyCrudTests). Real points — not N/A. Soft Assistant OUT = Stage C (no Assistant/tool runtime observed).

## Itemized security points (Product QA must evidence)

1. **Owner-scoped query-plane** — create/edit/get/list-own bound to owning Participant on query plane; IDOR fail-closed.

2. **StrategyBody FieldClass ACL** — User R/W; OwnAgent R/W for owner; Counterparty/Stranger/Unauth Deny.

3. **Never-to-counterparty** — Negotiation DTOs never expose StrategyBody / private Strategy fields.

4. **Authn fail-closed** — Unauth **401**; wrong principal **403**/**404**; uniform deny; no private leakage.

5. **OwnAgent = API policy only** — OwnAgent StrategyBody R/W is API policy evidence only — **no** Assistant / tool runtime delivery (soft Assistant OUT / Stage C).

6. **Consume #31, don’t rewrite** — StrategyBody enforcement on #31 registry; #40/#42 remain separate.

7. **No Stage C / Assistant inventing** — No thin/full Assistant, #26 hard wall, Cognito, MM/DC4 observed.

8. **OUT locked** — P3 minimal; free-form → V1; A5 sandbox → V4; X1 Assistant → Stage C; gate #25 backlog.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision observed.

10. **Handshake close** — Product QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Product QA weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
