# Security checklist — Spec · MVP Stage B #40 Instant search / discovery (P2)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/40 · Instant search / discovery (P2)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #41 · #42 — keep separate Spec; cross-ref only  
**Architecture:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` (Stage B discovery row)  
**Baselines:** Stage A #31+#32 CLOSED; gate #24 CA PASS; PoC Artifact + #5 auth  
**Hold:** Gate **#25** backlog until Stage B **delivery**. Stage C + #26–#27 + #18 Spec/SD HOLD. Saved-search → V1; A1 matching → V2. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-checklist.md`

## Scope note
Spec-binding for MVP **instant** discovery only: authenticated search over discoverable Artifact fields; discovery is a **separate surface** from owner inventory (#32); search payloads must **not** leak account secrets (parent #18). Real points — not N/A. Do not invent saved-search, A1 matching, Stage C, or Cognito.

## Itemized security points (Spec must bind)

1. **Authn fail-closed** — Instant search requires validated #5 principal; unauthenticated → **401** with no private-field leakage.

2. **Discovery ≠ owner inventory** — Spec binds discovery as a **separate surface** from Artifact owner list/get (#32); must **not** dump another Participant’s private account inventory or secrets.

3. **Search payload omit secrets** — Spec requires serializers omit denied classes from search results. Explicitly **absent**: **StrategyBody**, **LoginEmail**, **ContactEmail**, private account lists / Strategy inventory, auth secrets.

4. **Discoverable fields only** — Spec limits search/result fields to Artifact / negotiation-scoped fields **allowed for discovery** (PoC Artifact model / MVP Artifact surface — do **not** invent new Artifact schema for search).

5. **Uniform deny / no leak** — Wrong-principal / stranger misuse → fail-closed with **uniform deny bodies** and **no** private-field leakage (align #18 / #31 / #32 spirit).

6. **Consume Field ACL, don’t rewrite #31** — Spec consumes Stage A `IFieldPolicy` / FieldClass for projection omit; does not rewrite #31 registry Story AC.

7. **No Stage C / #18 inventing** — Spec does not invent Assistant hard wall, Cognito/SSO, MM/DC4, or full #18 Spec/SD delivery; parent #18 Spec/SD stays HOLD.

8. **OUT locked** — Spec documents **P2 instant only**; saved-search / market monitoring → **V1**; first-class complementary-intent matching (**A1**) → **V2**; gate **#25** stays backlog.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision required to accept this Spec.

10. **Traceability + handshake** — Spec cites #40 AC + #18 Option A Stage B only. Spec QA must **not** PASS until Security QA confirms. Keep #41/#42 separate.

## Handshake next
Senior Spec weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Spec.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
