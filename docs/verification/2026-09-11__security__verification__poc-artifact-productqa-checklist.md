# Security checklist — Product QA · PoC Artifact D1–D5 (#4)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-11  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #4 · D1–D5 Artifact + create/get/list-own  
**Issue:** https://github.com/ioaikh/dealoware/issues/4  
**PR:** https://github.com/ioaikh/dealoware/pull/11  
**SD Security PASS:** `verification/2026-09-11__security__verification__poc-artifact-sd-qa-confirm.md`  
**CQ:** no-refactor  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-artifact-productqa-checklist.md`

## Scope note
Product QA must **verify with evidence** that SD Security constraints hold. Real points — not N/A. PoC $0. No MM. #5 Auth SD may still be in flight — verify interim principal / #5 consume as implemented. CQ = no-refactor.

## Itemized security points (Product QA must evidence)

1. **API surface** — Only create/get/list-own present; no public list/search/discovery; no Update/Delete for PoC done.
2. **Owner-scope** — list-own caller-owned only; get non-owned/missing → 404 preferred (or 403) without cross-owner leak.
3. **Principal** — Interim `X-PoC-Owner-Id` (or Spec equiv) and/or #5 JWT/`sub` as wired; no password/SSO/IdP claimed delivered by #4.
4. **Authn ≠ authz** — Owner checks still enforced with a valid principal.
5. **Persistence local/$0** — Local store; no AWS DB provision; secrets via env/placeholders.
6. **Input bounds + D3 currency uniqueness** — Validation evidenced (tests or runtime).
7. **Secrets hygiene** — No secrets/tokens in repo artifacts, query strings, or Artifact bodies; no raw key logging in evidence.
8. **Zero MM/DC4** — No MM/DC4 coupling in PR.
9. **No scope creep / CQ** — No Negotiation/Strategy/AI/discovery/settlement; CQ no-refactor respected.
10. **Handshake close** — Product QA must not PASS until Security QA confirms these points.

## Handshake next
Product QA weaves evidence → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No AWS spend. Cost/critical → COO → CEO.
