# Security checklist — SD · PoC Artifact D1–D5 (#4)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-11  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #4 · D1–D5 Artifact + create/get/list-own (Option A)  
**Issue:** https://github.com/ioaikh/dealoware/issues/4  
**Plan:** `plans/2026-09-11__devplan__plan__poc-artifact-d1-d5.md`  
**Dev Plan Security PASS:** `verification/2026-09-11__security__verification__poc-artifact-devplan-qa-confirm.md`  
**#5:** UNLOCKED parallel — prefer JWT/`sub` when auth middleware available; interim principal bridge until then.  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-artifact-sd-checklist.md`

## Scope note
SD-binding checklist. Implementation must satisfy Spec + Dev Plan Security gates in code. Real points — not N/A. PoC $0. No MM. No inventing #5/#6.

## Itemized security points (SD must satisfy)

1. **API surface** — Implement **only** create, get, list-own. No public list/search/discovery; no Update/Delete endpoints for PoC done.
2. **Owner-scope authz** — list-own returns only caller-owned; get non-owned/missing → **404** preferred (or 403) without cross-owner leak.
3. **Principal** — Use interim principal per Spec **or** #5 JWT/`sub` when wired; do not invent password/SSO/OIDC IdP inside #4.
4. **Authn ≠ authz** — Even with valid #5 token, enforce Artifact owner checks.
5. **Persistence local/$0** — Local EF/SQLite (or Spec store); secrets via env/placeholders; no AWS DB provision.
6. **Input validation** — Enforce Spec bounds + D3 ≤1 value per currency; reject unexpected secret-smuggling fields.
7. **Secrets hygiene** — No committed secrets; no tokens in query/Artifact body; no logging raw keys.
8. **Zero MM/DC4** — No MM/DC4 refs/packages/config.
9. **No scope creep** — No Negotiation/Strategy/AI/discovery/settlement/Update/Delete.
10. **Evidence for QA** — Done-list cites paths/commands for points 1–9; Code/Product QA must not PASS until Security QA confirms.

## Handshake next
Senior Developer implements → Senior Security review → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS spend. Cost/critical → COO → CEO.
