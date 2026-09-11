# Security checklist — Doc · PoC Artifact D1–D5 (#4)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-11  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #4 · D1–D5 Artifact + create/get/list-own  
**Issue:** https://github.com/ioaikh/dealoware/issues/4  
**PR:** https://github.com/ioaikh/dealoware/pull/11 (merged)  
**Product QA Security PASS:** `verification/2026-09-11__security__verification__poc-artifact-productqa-qa-confirm.md`  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-artifact-doc-checklist.md`

## Scope note
Doc must not invent prod security claims, leak secrets, or imply AWS spend / MM coupling. Real points — not N/A. PoC $0.

## Itemized security points (Doc must satisfy)

1. **API surface accuracy** — Docs describe **create / get / list-own** only; do not document Update/Delete or public discovery as PoC delivered.
2. **Owner-scope** — Docs state list-own is owner-scoped; get non-owned → 404 preferred (or 403); no implying cross-owner browse.
3. **Principal / #5** — Docs describe interim principal and/or #5 JWT/`sub` consume as implemented; do not claim password/SSO/OIDC IdP delivered by #4.
4. **Authn ≠ authz** — Docs do not equate “authenticated” with “owns Artifact.”
5. **Local/$0 + host-shape** — Docs keep PoC local/$0; ECS Express Mode sketch only; no AWS provision how-tos as done.
6. **Secrets hygiene** — Examples use placeholders; no real keys/tokens/connection strings; no tokens in query-string examples.
7. **Input / data** — Docs reflect validation bounds / currency uniqueness at a high level without encouraging secret-smuggling fields.
8. **Zero MM/DC4** — No MM/DC4 integration steps or credentials in Doc.
9. **DOC-FLOW / index** — Security verification artifacts stay under `verification/` with correct naming; no secrets at KB root.
10. **Handshake close** — Docs QA must not PASS until Security QA confirms these points.

## Handshake next
Senior Docs weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS spend. Cost/critical → COO → CEO.
