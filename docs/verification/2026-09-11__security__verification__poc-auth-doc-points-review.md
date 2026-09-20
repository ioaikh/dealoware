# Verification — Security points vs PoC Auth Doc (#5)

**Author:** Dealoware Senior Security  
**Date:** 2026-09-11  
**Verdict:** **PASS** (all 10 Doc-step Security points MET)  
**Checklist (binding):** `verification/2026-09-11__security__verification__poc-auth-doc-checklist.md` (10 points)  
**Doc surfaces:**  
- Weave: `ops/2026-09-11__docs__ops__poc-auth-doc-security-weave.md`  
- README (main): Authentication (PoC) + Artifact API auth notes — https://github.com/ioaikh/dealoware/blob/main/README.md  
- Product QA report: `qa/2026-09-11__qa__qa-report__poc-participant-d6-auth.md`  
- INDEX.md (auth Doc/Security entries)  
**PR:** https://github.com/ioaikh/dealoware/pull/13 (MERGED)  
**Product QA Security PASS:** `verification/2026-09-11__security__verification__poc-auth-productqa-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**Optional Doc mirror PR:** https://github.com/ioaikh/dealoware/pull/14 (`docs/` weave + handshake mirrors; OPEN — optional cite; score remains on KB weave + main README)  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-auth-doc-points-review.md`  
**Constraints:** PoC $0; no Cognito/SSO; no MM/DC4; keep separate from #4. Soft gap no-live-dotnet OK (stated). Reviewed via `gh` remote read of main README + KB weave/INDEX/qa.

## Sources checked

| Source | Path / link | Result |
|--------|-------------|--------|
| Auth Doc Security checklist (Chief) | `verification/2026-09-11__security__verification__poc-auth-doc-checklist.md` | Binding 10 points |
| Doc Security weave | `ops/2026-09-11__docs__ops__poc-auth-doc-security-weave.md` | Pts 1–10 mapped; overall Doc PASS HELD for Security QA |
| README main | Authentication + Artifact API sections | Primary in-repo Doc surface |
| Product QA report + Security PASS | `qa/…poc-participant-d6-auth.md` + `…productqa-qa-confirm.md` | Prior step clear |
| INDEX.md | Auth Doc checklist + weave indexed | Index hygiene |
| Doc mirror PR #14 (optional) | `docs/ops/…poc-auth-doc-security-weave.md` + verification handshake paths | Mirrors KB Doc surfaces; no verdict change |

## Checklist vs Doc surface (evidence)

| # | Security point | Result | Evidence |
|---|----------------|--------|----------|
| 1 | **Fail-closed guidance** | **MET** | README: Artifact endpoints require Authorization (fail-closed); 401 missing/invalid. Weave pt1 + Product QA report. Health documented open. |
| 2 | **Mechanism accuracy** | **MET** | README: API key and/or JWT only. Weave pt2 explicitly outs password UX, cookie sessions, SSO IdP, Cognito, social login. Bad-pattern scan of README: no Cognito/password/SSO how-tos. Soft note: README implies-only; explicit OUT list lives on weave (acceptable Doc surface pair). |
| 3 | **OIDC-shaped `sub`** | **MET** | README “OIDC Principal Mapping”: `sub` → OwnerParticipantId; `iss`/`aud` table; does not claim IdP shipped. |
| 4 | **Secrets hygiene** | **MET** | README: env vars; “Never commit real signing keys”; Development placeholder; apiKey “shown once only” / store securely. Placeholders in curl examples (`dlw_AbCdEfGh_...`). |
| 5 | **Transport** | **MET** | All examples use `-H "Authorization: …"`; no query-string/body token examples. |
| 6 | **Lifecycle** | **MET** | README: `/auth/token`, rotate-key, revoke (API key + JWT JTI). No perpetual master-key claim. |
| 7 | **Bootstrap/register** | **MET** | README Register (Bootstrap) with displayName only; no platform-owner RBAC. |
| 8 | **Authn ≠ authz** | **MET** | README: 404 not found **or not owned**; “authn ≠ authz” in Error Responses; get/list owner-scoped. |
| 9 | **Local/$0** | **MET** | Localhost curls; ECS Express Mode sketch only; Dockerfile local-only note; no Cognito provision how-to. |
| 10 | **Handshake close** | **MET** | Weave HOLD until Security QA; this done-list → Security QA. **Docs QA must not overall-PASS until Security QA confirms.** |

## Soft notes (non-blocking)

- No live `dotnet`/`curl` for Doc step (static README + weave) — accepted per Chief.
- README mechanism OUT list is implicit; weave carries explicit OUT wording.

## Gaps for Senior Docs

**None** blocking. Optional polish: one README sentence listing PoC-out mechanisms (password/cookie/SSO/Cognito/social).

## Done-list (for Security QA)

- [x] Path/name DOC-FLOW: `verification/2026-09-11__security__verification__poc-auth-doc-points-review.md`
- [x] All 10 checklist points scored with README + weave + INDEX evidence
- [x] Kept separate from #4 Artifact Doc review
- [ ] Security QA: confirm **PASS** to Chief Security **or** further instructions

## Cost/critical

None. No Cognito/IdP spend. No escalate.
