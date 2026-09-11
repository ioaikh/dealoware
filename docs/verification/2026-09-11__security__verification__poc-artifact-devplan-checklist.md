# Security checklist — Dev Plan · PoC Artifact D1–D5 (#4)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-11  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #4 · D1–D5 Artifact + create/get/list-own (Option A)  
**Issue:** https://github.com/ioaikh/dealoware/issues/4  
**Spec:** `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md`  
**Spec Security PASS:** `verification/2026-09-11__security__verification__poc-artifact-spec-qa-confirm.md`  
**#5:** UNLOCKED parallel — Dev Plan may schedule consume of #5 JWT/`sub` when available; interim principal bridge until then. Do not invent #5 inside #4 plan.  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-artifact-devplan-checklist.md`

## Scope note
Dev Plan-binding checklist. Plan must **schedule/gate** Spec Security constraints for SD. Real points — not N/A. PoC $0. No MM. No invented Stories.

## Itemized security points (Dev Plan must weave)

1. **API surface tasks** — Plan only schedules **create**, **get**, **list-own** (no public list/search/discovery; no Update/Delete tasks for PoC done).
2. **Owner-scope verify gates** — Plan includes verify steps: list-own returns only caller-owned; get non-owned/missing → **404** (preferred) or 403 without cross-owner leak.
3. **Principal bridge / #5 consume** — Plan documents interim principal (`X-PoC-Owner-Id` or Spec-equivalent) **and** a task to prefer #5 JWT/`sub` when #5 SD is available — without expanding #4 into auth productization (password/SSO/IdP).
4. **Authn vs authz** — Plan keeps owner authz checks in Artifact tasks even when #5 authn is wired (valid token ≠ owns Artifact).
5. **Persistence local/$0** — Plan uses local EF/SQLite (or Spec-locked store) only; connection strings via env/placeholders; **no** AWS DB provision; ECS Express Mode remains sketch.
6. **Input bounds + currency uniqueness** — Plan includes validation tasks for Spec §4 bounds and D3 ≤1 value per currency; reject secret-smuggling unexpected fields.
7. **Secrets hygiene** — Plan gates: no committed secrets; no tokens in query strings/Artifact bodies; no logging raw keys; examples placeholders only.
8. **Zero MM/DC4** — Plan verify: no MM/DC4 packages/config/refs.
9. **No scope creep** — Plan does not schedule Negotiation (#6), Strategy/AI, discovery, settlement, Update/Delete, or Cognito/SSO spend.
10. **Handshake close** — Dev Plan QA must not PASS until Security QA confirms these points (cite checklist + evidence).

## Handshake next
1. Senior Dev Planner weaves into `plans/` Artifact Dev Plan.
2. Senior Security reviews → Security QA.
3. Chief Security PASS/HOLD to CPM + Chief Dev Planner.

## Cost/critical
No AWS spend. Cost/critical → COO → CEO.
