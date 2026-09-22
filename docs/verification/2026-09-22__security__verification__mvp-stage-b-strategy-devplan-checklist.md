# Security checklist — Dev Plan · MVP Stage B #41 Minimal Strategy create/edit (P3)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/41 · Minimal Strategy create/edit (P3 partial)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #42 — keep separate plan; cross-ref only  
**Spec:** `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md`  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-spec-qa-confirm.md` (SoR PR #44)  
**Hold:** Gate **#25** backlog. Stage C Assistant (#26) + #27 + #18 Spec/SD HOLD. SD HOLD until Dev Plan QA + Security PASS. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate** Spec Security for minimal Strategy CRUD + **StrategyBody** FieldClass ACL (Owner+OwnAgent only). Real points — not N/A. No Assistant runtime.

## Itemized security points (Dev Plan must weave)

1. **Owner-scoped query-plane tasks** — Plan schedules create/edit/get/list-own with owner query plane; IDOR fail-closed tasks.

2. **StrategyBody FieldClass ACL tasks** — Plan schedules IFieldPolicy rows: User R/W; OwnAgent R/W for owner; Counterparty/Stranger/Unauth Deny; verify.

3. **Never-to-counterparty tasks** — Plan requires negotiation DTOs never expose StrategyBody; verify.

4. **Authn fail-closed tasks** — Unauth 401; wrong principal 403/404; uniform deny bodies; verify.

5. **OwnAgent = API policy only** — Plan allows OwnAgent StrategyBody as API policy — **no** Assistant/tool runtime tasks (Stage C out).

6. **Consume #31, don’t rewrite** — Plan adds StrategyBody enforcement on #31 registry; keep #40/#42 separate.

7. **No Stage C / Assistant inventing** — No thin/full Assistant, #26 hard wall, Cognito, MM/DC4 tasks.

8. **OUT locked** — P3 minimal; free-form → V1; A5 sandbox → V4; X1 Assistant → Stage C; gate #25 backlog.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision tasks.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
