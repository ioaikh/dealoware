# Security checklist — Product QA · MVP Stage B #40 Instant search / discovery (P2)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/40 · Instant search / discovery (P2)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #41 HOLD (no Product QA checklist yet); #42 separate track  
**PR:** https://github.com/ioaikh/dealoware/pull/53 MERGED @ `767ab29…` (CQ cq:no-refactor)  
**SD Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-qa-confirm.md` (SoR PR #55)  
**SD checklist (ref):** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-checklist.md` (SoR PR #49)  
**Hold:** Gate **#25** backlog. Stage C + #26–#27 + #18 Spec/SD (whole) HOLD. Saved-search → V1; A1 → V2. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-productqa-checklist.md`

## Scope note
Product QA must evidence that SD Security constraints for MVP **instant** discovery hold on delivered PR #53. Soft gap no-live-dotnet OK if stated with equivalent evidence (CI + DiscoverySearchTests), same Stage A Product QA pattern. Real points — not N/A. Soft: structural DTO omit vs per-field Evaluate loop is non-blocking if secrets absent from payloads.

## Itemized security points (Product QA must evidence)

1. **Authn fail-closed** — Instant-search requires validated #5 principal; unauth → **401**; error bodies omit private fields / secrets.

2. **Discovery ≠ inventory** — Discovery surface separate from #32 owner list/get; no private inventory dump via search.

3. **Search payload omit secrets** — Results omit StrategyBody / LoginEmail / ContactEmail / private lists / Strategy inventory / auth secrets.

4. **Discoverable-fields-only** — Results limited to allowed Artifact fields already on path; no new Artifact schema for search.

5. **Uniform deny / no-leak** — Wrong-principal / stranger misuse → fail-closed; uniform deny; no private-field leakage.

6. **Consume #31 Field ACL** — Projection omit aligns #31 FieldClass deny semantics for discovery; #31 not rewritten.

7. **No Stage C / #18 inventing** — No Assistant hard wall, Cognito, MM/DC4, or unlocking #18 Spec/SD as a whole observed.

8. **OUT locked** — P2 instant only; saved-search → V1; A1 → V2; gate #25 backlog; #41/#42 not delivered under this Story.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision observed.

10. **Handshake close** — Product QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Product QA weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
