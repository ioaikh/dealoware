# Security checklist — SD · MVP Stage B #41 Minimal Strategy create/edit (P3)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/41 · Minimal Strategy create/edit (P3 partial)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #42 — keep separate SD; cross-ref only  
**Plan:** `plans/2026-09-22__devplan__plan__mvp-stage-b-minimal-strategy-crud.md`  
**Dev Plan Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-devplan-qa-confirm.md` (SoR PR #47)  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-spec-qa-confirm.md` (SoR PR #44)  
**Hold:** Gate **#25** backlog. Stage C Assistant (#26) + #27 + #18 Spec/SD (whole) HOLD. Product QA HOLD until SD-step Security PASS. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-sd-checklist.md`

## Scope note
SD must **implement** Spec + Dev Plan Security for minimal Strategy CRUD + **StrategyBody** FieldClass ACL (Owner+OwnAgent only). Real points — not N/A. No Assistant runtime. Evidence via tests/done-list.

## Itemized security points (SD must satisfy)

1. **Owner-scoped query-plane** — create/edit/get/list-own bound to owning Participant on query plane; IDOR fail-closed (verified).

2. **StrategyBody FieldClass ACL** — `IFieldPolicy` rows: User R/W; OwnAgent R/W for owner; Counterparty/Stranger/Unauth Deny (verified).

3. **Never-to-counterparty** — Negotiation DTOs never expose StrategyBody / private Strategy fields (verified).

4. **Authn fail-closed** — Unauth **401**; wrong principal **403**/**404**; uniform deny bodies; no private leakage.

5. **OwnAgent = API policy only** — OwnAgent StrategyBody R/W is API policy evidence only — **no** Assistant / tool runtime delivery.

6. **Consume #31, don’t rewrite** — Add StrategyBody enforcement on #31 registry; keep #40/#42 separate.

7. **No Stage C / Assistant inventing** — No thin/full Assistant, #26 hard wall, Cognito, MM/DC4.

8. **OUT locked** — P3 minimal; free-form → V1; A5 sandbox → V4; X1 Assistant → Stage C; gate #25 backlog.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision.

10. **Evidence + handshake** — Done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
