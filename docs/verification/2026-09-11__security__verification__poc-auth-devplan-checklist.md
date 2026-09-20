# Security checklist — Dev Plan · PoC Participant auth D6 (#5)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-11  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #5 · Participant minimal register/auth (D6)  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**Spec:** `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md`  
**Spec Security PASS:** `verification/2026-09-11__security__verification__poc-auth-spec-qa-confirm.md`  
**Parallel:** Keep separate from #4 Artifact Dev Plan; schedule handoff so Artifact APIs fail closed once auth middleware lands.  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-auth-devplan-checklist.md`

## Scope note
Dev Plan-binding checklist for API key/JWT auth implementation tasks. Real points — not N/A. PoC $0. No Cognito/SSO IdP. No MM. No inventing.

## Itemized security points (Dev Plan must weave)

1. **Protect Artifact APIs** — Plan includes tasks to require validated principal on Artifact create/get/list-own; unauthenticated → fail closed (401/403). Coordinate with #4 plan (no silent anonymous owner).
2. **Mechanism tasks only** — Plan schedules API key and/or JWT issue+validate only. **Out of plan:** password UX, cookie sessions, SSO IdP, social login, Cognito provision.
3. **OIDC-shaped `sub`** — Plan includes mapping stable `sub` (or Spec-equivalent) → Participant id used as Artifact `ownerParticipantId`.
4. **Secrets / signing material** — Plan gates: keys/signing secrets via env only; never commit; no logging raw tokens/keys; README placeholders.
5. **Authorization header transport** — Plan specifies `Authorization` header (or Spec-documented equivalent); forbid query-string/body token passing in tasks/tests.
6. **Lifecycle (PoC-minimal)** — Plan includes issue + validate + at least simple invalidate/re-issue or rotate note; no hard-coded master key in source.
7. **Bootstrap/register bounds** — Plan documents seed/bootstrap and/or register surface with PoC abuse note; no platform-owner RBAC (O1 out).
8. **Authn ≠ authz** — Plan explicitly keeps #4 owner-scope checks after auth middleware (valid token ≠ owns Artifact).
9. **Local/$0 + no IdP spend** — No AWS Cognito/IdP provision tasks; ECS Express Mode sketch only; local host.
10. **Handshake close** — Dev Plan QA must not PASS until Security QA confirms these points.

## Handshake next
1. Senior Dev Planner weaves into #5 Dev Plan under `plans/`.
2. Senior Security → Security QA.
3. Chief Security PASS/HOLD to CPM + Chief Dev Planner.

## Cost/critical
No managed IdP / AWS spend without COO→CEO.
