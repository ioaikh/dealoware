# Security checklist — Doc · PoC Participant auth D6 (#5)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-11  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #5 · Participant minimal register/auth (D6)  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**PR:** https://github.com/ioaikh/dealoware/pull/13 (merged)  
**Product QA Security PASS:** `verification/2026-09-11__security__verification__poc-auth-productqa-qa-confirm.md`  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-auth-doc-checklist.md`

## Scope note
Doc must not invent SSO/Cognito, leak secrets, or weaken fail-closed guidance. Real points — not N/A. PoC $0. Keep separate from #4 Doc Security.

## Itemized security points (Doc must satisfy)

1. **Fail-closed guidance** — Docs state Artifact create/get/list-own require auth; unauthenticated calls fail (401/403).
2. **Mechanism accuracy** — Docs describe API key and/or JWT only; explicitly out: password UX, cookie sessions, SSO IdP, Cognito, social login for PoC.
3. **OIDC-shaped `sub`** — Docs explain `sub`/principal → Participant/owner mapping without claiming an IdP is shipped.
4. **Secrets hygiene** — Examples use placeholders; no real keys/tokens; warn against logging raw credentials; env for signing material.
5. **Transport** — Docs show `Authorization` header (or Spec equivalent); forbid query-string/body token examples.
6. **Lifecycle** — Docs note issue/validate and PoC revoke/rotate (or re-issue); no implying perpetual embedded master keys.
7. **Bootstrap/register** — Docs bound seed/bootstrap/register; no platform-owner RBAC claims.
8. **Authn ≠ authz** — Docs state valid token ≠ owns Artifact; owner checks remain.
9. **Local/$0** — No Cognito/IdP provision how-tos as delivered; ECS Express Mode sketch only; PoC local.
10. **Handshake close** — Docs QA must not PASS until Security QA confirms these points.

## Handshake next
Senior Docs weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No managed IdP spend without COO→CEO.
