# Security checklist — Doc · MVP Stage B #40 Instant search / discovery (P2)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/40 · Instant search / discovery (P2)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #41 · #42 — keep separate Doc; cross-ref only  
**PR:** https://github.com/ioaikh/dealoware/pull/53 MERGED @ `767ab29e…`  
**Product QA Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-productqa-qa-confirm.md`  
**SD Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-qa-confirm.md`  
**Hold:** Gate **#25** backlog. Stage C + #18 Spec/SD (whole) HOLD. Saved-search → V1; A1 → V2. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-doc-checklist.md`

## Scope note
Doc must accurately describe MVP **instant** discovery security (authn fail-closed; discovery ≠ inventory; omit secrets; consume #31) and must **not** invent saved-search, A1, Strategy ACL, ContactEmail share, Assistant hard wall, Cognito, or MM/DC4 as delivered. Real points — not N/A. Soft structural DTO omit vs Evaluate loop may be noted; soft no-live-dotnet OK with CI/tests cites.

## Itemized security points (Doc must satisfy)

1. **Authn fail-closed** — Docs state instant-search requires #5 principal; unauth → **401**; error bodies omit private fields / secrets.

2. **Discovery ≠ inventory** — Docs state discovery is separate from #32 owner inventory; no private inventory dump via search.

3. **Search payload omit secrets** — Docs state results omit StrategyBody / LoginEmail / ContactEmail / private lists / Strategy inventory / auth secrets.

4. **Discoverable-fields-only** — Docs state results limited to allowed Artifact fields already on path; no new Artifact schema for search.

5. **Uniform deny / no-leak** — Docs state stranger/wrong-principal misuse fail-closed; uniform deny; no private leakage.

6. **Consume #31 Field ACL** — Docs state projection omit aligns #31 FieldClass deny semantics; #31 not rewritten.

7. **No Stage C / #18 inventing** — Docs do not invent Assistant hard wall, Cognito, MM/DC4, or #18 Spec/SD unlock as delivered.

8. **OUT locked** — Docs state P2 instant only; saved-search → V1; A1 → V2; gate #25 backlog; #41/#42 separate.

9. **Cost / spend** — Docs state PoC **$0**; no IdP/vault provision as delivered.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Docs weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
