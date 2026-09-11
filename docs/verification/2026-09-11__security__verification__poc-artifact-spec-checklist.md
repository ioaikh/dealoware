# Security checklist — Spec · PoC Artifact D1–D5 (#4)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-11  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #4 · D1–D5 Artifact + create/get/list-own (Option A)  
**Issue:** https://github.com/ioaikh/dealoware/issues/4  
**Product lock:** Option A — create + get + list-own; Update/Delete → MVP (P1)  
**Auth note:** Issue #5 (Participant auth) is **UNLOCKED in parallel** (not parked). #4 Spec may still bind an **interim principal** until #5 auth lands; must not invent password/SSO/OIDC IdP; owner-scope remains required. #5 has its own Spec Security checklist.  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-artifact-spec-checklist.md`

## Scope note
Spec-binding checklist for Artifact persistence + minimal owner-scoped API. Real points — not N/A. No MotorMarket. PoC local/$0. No invented Stories (#5 has separate Spec Security; #6+ out).

## Itemized security points (Spec must bind)

1. **Owner-scoped API only** — Spec locks PoC API to **create**, **get**, **list-own** only. No public/global list, search, or discovery endpoints in #4. Get/list-own must be **owner-scoped** (caller cannot read another owner’s Artifacts).
2. **Interim principal until #5 wires in** — Spec states how PoC identifies “owner” if #5 auth is not yet consumed (e.g. explicit PoC principal / API-key placeholder / request header bound to owner id) **as temporary bridge only**. Prefer aligning to #5 API key/JWT + OIDC-shaped `sub` when both Specs land. Must **not** claim password/SSO/OIDC IdP delivered; full auth productization stays in #5 / MVP — do not duplicate #5 scope inside #4.
3. **Authorization failure behavior** — Spec requires get of non-owned or missing Artifact returns **404 or 403** without leaking existence of other owners’ data beyond what Product/SA allow; list-own returns **only** caller-owned rows.
4. **No Update/Delete in PoC** — Spec forbids Update/Delete (and bulk mutate) endpoints for PoC acceptance (Option A). Reduces write/attack surface until MVP P1.
5. **Persistence trust boundary** — Spec requires local/dev persistence only for PoC (no cloud DB provision). Connection strings/secrets via env/placeholders only — never commit real credentials. Local/$0; ECS Express Mode remains host **sketch** only; no AWS provision.
6. **Input / data hygiene** — Spec requires validation bounds for D1–D5 payloads (sizes/counts for entities, properties, facts, locations, time windows, values) to limit abuse; reject unexpected fields that would smuggle auth/secrets (e.g. raw tokens in Subject properties) where practical.
7. **Secrets not in Artifact body** — Spec states Artifact fields are domain data, not a secrets store; no requirement to persist API keys/passwords/cloud creds inside Subject/Facts; docs/examples use placeholders only.
8. **Zero MotorMarket / DC4** — Spec forbids MM/DC4 schemas, feeds, SFTP, shared DB, or package refs in #4.
9. **No scope creep** — Spec does not add Negotiation (#6), Strategy/AI, discovery/search, settlement, multi-party, or full CRUD. Claims lock unchanged.
10. **Traceability + handshake** — Spec cites issue #4 AC, Product Option A, feasibility roadmap; maps these Security points for Dev Plan/SD. Spec QA must not PASS until Security QA confirms.

## Handshake next
1. Senior Spec weaves/answers points in Spec (cite sections).
2. Senior Security reviews → done-list to Security QA.
3. Security QA PASS to Chief Security (or further instructions).
4. Chief Security PASS/HOLD to CPM + Chief Spec.

## Cost/critical
No AWS spend. Cost/critical → COO → CEO.
