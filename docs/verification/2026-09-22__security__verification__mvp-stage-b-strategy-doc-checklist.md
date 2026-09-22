# Security checklist — Doc · MVP Stage B #41 Minimal Strategy create/edit (P3)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/41 · Minimal Strategy create/edit (P3 partial)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #42 — keep separate Doc; cross-ref only  
**PR:** https://github.com/ioaikh/dealoware/pull/51 MERGED @ `43adb283…`  
**Product QA Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-productqa-qa-confirm.md`  
**SD Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-sd-qa-confirm.md`  
**Hold:** Gate **#25** backlog. Stage C Assistant (#26) + #27 + #18 Spec/SD (whole) HOLD. Soft **Assistant OUT** — OwnAgent = API policy only. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-doc-checklist.md`

## Scope note
Doc must accurately describe minimal Strategy CRUD + **StrategyBody** FieldClass ACL (Owner+OwnAgent only) and must **not** invent Assistant/tool runtime, free-form engine, A5 sandbox, Cognito, or MM/DC4 as delivered. Soft Assistant OUT = Stage C. Real points — not N/A.

## Itemized security points (Doc must satisfy)

1. **Owner-scoped query-plane** — Docs state create/edit/get/list-own bound to owner on query plane; IDOR fail-closed.

2. **StrategyBody FieldClass ACL** — Docs state User R/W; OwnAgent R/W for owner; Counterparty/Stranger/Unauth Deny.

3. **Never-to-counterparty** — Docs state Negotiation DTOs never expose StrategyBody / private Strategy fields.

4. **Authn fail-closed** — Docs state unauth **401**; wrong principal **403**/**404**; uniform deny; no private leakage.

5. **OwnAgent = API policy only** — Docs state OwnAgent StrategyBody R/W is API policy only — **no** Assistant / tool runtime (soft Assistant OUT / Stage C).

6. **Consume #31, don’t rewrite** — Docs state StrategyBody on #31 registry; #40/#42 separate.

7. **No Stage C / Assistant inventing** — Docs do not invent thin/full Assistant, #26 hard wall, Cognito, MM/DC4 as delivered.

8. **OUT locked** — Docs state P3 minimal; free-form → V1; A5 → V4; X1 Assistant → Stage C; gate #25 backlog.

9. **Cost / spend** — Docs state PoC **$0**; no IdP/vault provision as delivered.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Docs weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
