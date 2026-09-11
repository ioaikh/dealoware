# Security checklist — Spec · PoC Participant auth D6 (#5)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-11  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #5 · Participant minimal register/auth (D6)  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**Product lock:** PoC auth = **API key / JWT**; password + fuller reg @ MVP; OIDC-shaped claims (shape only); no SSO IdP  
**Parallel:** #4 Artifact Spec in flight — auth must protect Artifact APIs; do not invent #6+  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-auth-spec-checklist.md`

## Scope note
Spec-binding checklist for PoC API key/JWT auth. Real points — not N/A. No MotorMarket. PoC local/$0. No invented Stories (no O9 SSO, no password UX, no platform-owner RBAC).

## Itemized security points (Spec must bind)

1. **Authn required on protected APIs** — Spec states which endpoints require a validated principal (at minimum Artifact create/get/list-own once wired; Negotiation later). Unauthenticated calls to those endpoints must fail closed (401/403) — no silent anonymous owner.
2. **Mechanism lock** — Spec locks PoC to **API key and/or JWT** issue/validate only. **Out:** password login UX, cookie session productization, SSO IdP (O9), social login.
3. **OIDC-shaped claims (shape only)** — Spec defines principal claim shape compatible with later OIDC (e.g. `sub` / stable subject id) **without** shipping an IdP. Document claims used for authorization (owner id mapping).
4. **Secret handling** — API keys/JWT signing material: generate/store via env/secret store patterns for PoC; **never** commit real keys/secrets to repo; docs use placeholders. Spec forbids logging raw tokens/keys.
5. **Transport** — Spec requires tokens/keys sent via `Authorization` header (or documented equivalent) — **not** in query strings or Artifact bodies. Local HTTP OK for PoC; prod TLS deferred (cite O10 local-until-spend).
6. **Token/key lifecycle (PoC-minimal)** — Spec defines issue + validate; invalidate/rotate at least as PoC note (even if simple revoke list / re-issue). No perpetual undocumented shared master key in source.
7. **Register/bootstrap surface** — Spec bounds bootstrap/register: no open unauthenticated mass account creation without rate/abuse note for PoC; seed/bootstrap acceptable if documented. No privilege escalation to platform-owner roles (O1 out).
8. **Authorization vs authentication** — Spec separates “valid token” from “owns this Artifact”; owner checks remain required on #4 APIs (align with Artifact Spec Security checklist).
9. **Zero MM/DC4 + no scope creep** — No MM/DC4 identity federation; no Strategy/AI; no settlement; local/$0; ECS Express Mode sketch only; no AWS provision for IdP/cognito unless CEO spend OK (default: **no**).
10. **Traceability + handshake** — Spec cites issue #5 AC, BA auth note, feasibility auth shape; maps these points for Dev Plan/SD. Spec QA must not PASS until Security QA confirms.

## Handshake next
1. Senior Spec weaves/answers points in Spec (cite sections).
2. Senior Security reviews → done-list to Security QA.
3. Security QA PASS to Chief Security.
4. Chief Security PASS/HOLD to CPM + Chief Spec.

## Cost/critical
No AWS spend / no managed IdP provision without COO→CEO. Cost/critical → COO → CEO.
