# Security checklist — Dev Plan · MVP Stage B #40 Instant search / discovery (P2)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/40 · Instant search / discovery (P2)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #41 · #42 — keep separate plan; cross-ref only  
**Spec:** `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md`  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-qa-confirm.md` (SoR PR #44)  
**Hold:** Gate **#25** backlog. Stage C + #26–#27 + #18 Spec/SD HOLD. SD HOLD until Dev Plan QA + Security PASS. Saved-search → V1; A1 → V2. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate** Spec Security for MVP **instant** discovery: authn fail-closed; discovery ≠ owner inventory; search payloads omit secrets; consume #31 Field ACL. Real points — not N/A.

## Itemized security points (Dev Plan must weave)

1. **Authn fail-closed tasks** — Plan schedules authenticated search only; unauth → 401; verify tests.

2. **Discovery ≠ inventory tasks** — Plan keeps discovery surface separate from #32 owner list/get; no private inventory dump tasks.

3. **Search payload omit-secrets tasks** — Plan schedules serializers omit StrategyBody / LoginEmail / ContactEmail / private lists / auth secrets from search results.

4. **Discoverable-fields-only tasks** — Plan limits search/result fields to allowed Artifact fields; no new Artifact schema for search.

5. **Uniform deny / no-leak tasks** — Plan includes stranger misuse fail-closed + uniform deny bodies; verify.

6. **Consume #31 Field ACL** — Plan consumes IFieldPolicy for projection omit; does not rewrite #31.

7. **No Stage C / #18 inventing** — No Assistant hard wall, Cognito, MM/DC4, or #18 Spec/SD tasks.

8. **OUT locked** — P2 instant only; saved-search → V1; A1 → V2; gate #25 backlog.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision tasks.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
