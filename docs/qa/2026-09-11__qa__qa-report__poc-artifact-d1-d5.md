# QA Report — PoC Artifact D1–D5 (Product QA evidence)

**Status:** PASS — Security QA confirm landed; QAQA confirmed to Chief (no bounce)  
**Date:** 2026-09-11  
**Author:** Dealoware Senior Product QA  
**Confirm to:** Dealoware QA (Chief QA) via QAQA after Security handshake  
**Story:** GitHub issue #4 · Artifact D1–D5 + create/get/list-own  
**Issue:** https://github.com/ioaikh/dealoware/issues/4 (CLOSED — verified)  
**PR:** https://github.com/ioaikh/dealoware/pull/11 (MERGED)  
**SD HEAD (PR head):** `11be7939ef4bb9cee2a24e663bc0b6e6e184b702`  
**main tip / merge commit:** `8d8cad30726034101110648e9552f7cb89d93fbd`  
**Merge parents:** `2b7da9cf0e5fd88b08c7b862b93c42a1515765a8` + `11be7939…`  
**Branch (merged):** `cursor/artifact-core-model-d1-d5-220e`  
**Security checklist:** `verification/2026-09-11__security__verification__poc-artifact-productqa-checklist.md`  
**Security QA confirm (this step):** `verification/2026-09-11__security__verification__poc-artifact-productqa-qa-confirm.md` — PASS (pts 1–10 MET)  
**Prior SD:** `verification/2026-09-11__sd__verification__poc-artifact-d1-d5.md` (SD PASS)  
**Prior SD Security QA:** `verification/2026-09-11__security__verification__poc-artifact-sd-qa-confirm.md` (PASS, 1–10 MET @ `11be7939…`)  
**DOC-FLOW:** `qa/2026-09-11__qa__qa-report__poc-artifact-d1-d5.md`  
**CQ:** no-refactor  

## Method
- `gh pr view 11` / `gh api` contents & tree at **main** `8d8cad3…` (no clone)
- KB: productqa checklist, SD verification, ORG-OPS handshake
- Soft gap: .NET SDK absent on evidence box → live `dotnet test` / `curl` not run
- CI: `gh pr checks 11` → no checks reported on branch

## Product acceptance criteria

| AC | Verdict | Evidence (main `8d8cad30726034101110648e9552f7cb89d93fbd`) |
|----|---------|----------|
| 1 D1 Subject | SOFT | `SubjectEntity` name/description/properties/facts; `Artifact.Entities`; tests multi-entity + all D1–D5 persist |
| 2 D2 Intent | SOFT | `Artifact.Intent`; validator + missing-intent → 400 test |
| 3 D3 Value | SOFT | amount+currency; ≤1 currency case-insensitive; 0..n; duplicate → 400; uppercase normalize |
| 4 D4 Location 0..n | SOFT | `Locations` list; EF `_locations` column; MaxLocations=50 |
| 5 D5 Time 0..n | SOFT | `TimePeriod` start/end; MaxTimePeriods=50 |
| 6 create/get/list-own | SOFT | POST/GET/{id}/GET list; persisted via EF Add+SaveChanges; owner-scoped; 17 ArtifactEndpointTests |
| 7 Update/Delete OUT | SOFT | No Put/Patch/Delete mappers; repo has no Update/Delete |
| Health present | SOFT | `GET /health` → `{status:ok}`; HealthEndpointTests |
| README ECS / local/$0 | SOFT | ECS Express Mode; App Runner NOT; local/$0; SQLite |

## Security checklist points 1–10 (Product QA evidence)

| # | Point | Verdict | Evidence |
|---|-------|---------|----------|
| 1 | API surface Option A only | EVIDENCED | `ArtifactEndpoints.cs`: MapPost `/`, MapGet `/{id}`, MapGet `/` only |
| 2 | Owner-scope | EVIDENCED | Get: NotFound if null or `OwnerParticipantId != ownerId`; List: `GetByOwnerAsync`; tests cross-owner 404 + filter |
| 3 | Principal interim | EVIDENCED | `X-PoC-Owner-Id`; #5 JWT `sub` consume-path docs; no password/SSO by #4 |
| 4 | Authn ≠ authz | EVIDENCED | Principal required then owner check/filter still applied |
| 5 | Persistence local/$0 | EVIDENCED | EF Core + SQLite; `Data Source=dealoware.db`; env override |
| 6 | Input bounds + D3 uniqueness | EVIDENCED | `ArtifactValidator` Spec bounds + currency HashSet |
| 7 | Secrets hygiene | EVIDENCED | Local sqlite placeholder; header principal; no cloud secrets in source |
| 8 | Zero MM/DC4 | EVIDENCED | Tree path scan clean; no MM/DC4 packages |
| 9 | No scope creep / CQ | EVIDENCED | Option A only; health kept; no Negotiation/Strategy/AI/Update/Delete implementation |
| 10 | Handshake close | PASS | Checklist + `…poc-artifact-productqa-qa-confirm.md` PASS (pts 1–10 MET) |

## Soft gaps / non-blockers (except #10)
- No live `dotnet restore|build|test` or `curl` on this box
- No CI checks on PR branch
- Relies on static `gh` evidence + prior SD / SD-Security PASS for runtime claims
- README product-vision prose mentions negotiation/strategy (marketing); no implemented coupling

## Disposition

**PASS → Chief QA** (2026-09-11). Security QA PASS + QAQA PASS. Soft gaps non-blocking. PM → Doc / ready-for-ba-verify.

### Prior disposition note
**PASS** — Security QA `…poc-artifact-productqa-qa-confirm.md` PASS (pts 1–10 MET); QAQA confirmed Product QA PASS to Chief (no bounce).  
Soft gaps (no live dotnet / no CI) accepted as non-blocking.  
Do not set GitHub status from this step alone (PM/Chief owns gate).
