# Security checklist — SD · MVP Stage B #40 Instant search / discovery (P2)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/40 · Instant search / discovery (P2)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #41 · #42 — keep separate SD; cross-ref only  
**Plan:** `plans/2026-09-22__devplan__plan__mvp-stage-b-instant-search-discovery.md`  
**Dev Plan Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-devplan-qa-confirm.md` (SoR PR #47)  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-qa-confirm.md` (SoR PR #44)  
**Hold:** Gate **#25** backlog. Stage C + #26–#27 + #18 Spec/SD (whole) HOLD. Product QA HOLD until SD-step Security PASS. Saved-search → V1; A1 → V2. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-checklist.md`

## Scope note
SD must **implement** Spec + Dev Plan Security for MVP **instant** discovery: authn fail-closed; discovery ≠ owner inventory; search payloads omit secrets; consume #31 Field ACL. Real points — not N/A. Evidence via tests/done-list.

## Itemized security points (SD must satisfy)

1. **Authn fail-closed** — Instant-search paths require validated #5 principal; unauth → **401**; error bodies omit private fields / secrets.

2. **Discovery ≠ inventory** — Discovery surface separate from #32 owner list/get; no private inventory dump via search.

3. **Search payload omit secrets** — Serializers omit StrategyBody / LoginEmail / ContactEmail / private lists / Strategy inventory / auth secrets from search results.

4. **Discoverable-fields-only** — Search/result fields limited to allowed Artifact fields already on path; no new Artifact schema for search.

5. **Uniform deny / no-leak** — Wrong-principal / stranger misuse → fail-closed; uniform deny bodies; no private-field leakage (verified).

6. **Consume #31 Field ACL** — Projection omit via `IFieldPolicy` / FieldClass; do **not** rewrite #31.

7. **No Stage C / #18 inventing** — No Assistant hard wall, Cognito, MM/DC4, or unlocking #18 Spec/SD as a whole.

8. **OUT locked** — P2 instant only; saved-search → V1; A1 → V2; gate #25 backlog; do not implement #41/#42 here.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision.

10. **Evidence + handshake** — Done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
