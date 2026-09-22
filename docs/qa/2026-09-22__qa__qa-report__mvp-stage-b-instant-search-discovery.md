# QA Report — MVP Stage B Instant search / discovery (#40)

**Status:** **PASS** — Security QA confirm landed (pts 1–10 MET); QAQA confirmed Product QA PASS to Chief (no bounce)  
**Date:** 2026-09-22  
**Author:** Dealoware Senior Product QA  
**Confirm to:** Dealoware QA (Chief QA) via QAQA **after** Security handshake  
**Story:** GitHub issue #40 · Instant search / discovery (P2)  
**Issue:** https://github.com/ioaikh/dealoware/issues/40  
**PR:** https://github.com/ioaikh/dealoware/pull/53 (**MERGED**)  
**main merge commit:** `767ab29e373871db71657c87d330a9671d779443`  
**CI (main @ merge):** https://github.com/ioaikh/dealoware/actions/runs/35766371083 — **SUCCESS**  
**Security checklist:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-productqa-checklist.md`  
**Security QA confirm (this step):** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-productqa-qa-confirm.md` — **PASS** (pts 1–10 MET)  
**Prior SD:** `verification/2026-09-22__sd__verification__mvp-stage-b-instant-search-discovery.md` (SD PASS)  
**Prior SD Security QA:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-qa-confirm.md` (PASS 10/10)  
**CQ:** `cq:no-refactor`  
**DOC-FLOW:** `qa/2026-09-22__qa__qa-report__mvp-stage-b-instant-search-discovery.md`  
**Constraints:** PoC $0; #41 separate track; #42 separate track; Stage B P2 instant only; no MotorMarket; no inventing  

## Method

- `gh` contents at main merge `767ab29e…` (no clone)
- Soft gap: no live `dotnet test` → **CI + `DiscoverySearchTests.cs` equivalent**
- Soft: structural DTO omit (IFieldPolicy not on search path) — SD Security soft-accepted; Product QA cites same
- **Formal weave:** Product QA Security checklist pts 1–10 table below (not interim SD-spirit); checklist path cited in header

## Product acceptance criteria

| AC | Verdict | Evidence (main `767ab29e…`) |
|----|---------|------------------------------|
| 1 Auth’d instant search over discoverable Artifact fields; no new schema | **MET** | `Search_Auth_ReturnsDiscoverableFieldsOnly`; `Search_ByIntent/EntityDescription/EntityName_Works`; query-plane Facts |
| 2 Results omit StrategyBody / LoginEmail / ContactEmail / private lists / Strategy inventory / auth secrets | **MET** | `Search_Results_NoStrategyBody`; `NoLoginEmail`; `NoContactEmail`; `NoOwnerParticipantId`; `NoAuthSecrets`; `Search_DoesNotDumpPrivateInventory` |
| 3 Discovery ≠ #32 owner inventory; no private dump | **MET** | `Search_IsSeparateFromOwnerInventory`; `Search_CanDiscoverOthersArtifacts`; `Search_DoesNotDumpPrivateInventory` |
| 4 Unauth → 401; stranger misuse fail-closed; no private leak | **MET** | `Search_Unauth_Returns401`; invalid auth 401; `Search_Unauth_NoPrivateFieldsInError` |
| 5 Automated tests matrix | **MET** | `DiscoverySearchTests.cs` (~25 Facts); CI SUCCESS run 35766371083 |
| 6 P2 instant only — saved-search V1 / A1 V2 OUT | **MET** | PR/tests scoped to instant GET search; no saved-search invent (SD cite) |

## Security checklist points 1–10 (Product QA evidence)

| # | Point | Verdict | Evidence |
|---|-------|---------|----------|
| 1 | Authn fail-closed | **EVIDENCED** | `Search_Unauth_Returns401`; invalid auth 401; `Search_Unauth_NoPrivateFieldsInError` |
| 2 | Discovery ≠ inventory (#32) | **EVIDENCED** | `Search_IsSeparateFromOwnerInventory`; `Search_DoesNotDumpPrivateInventory`; `Search_CanDiscoverOthersArtifacts` |
| 3 | Search payload omit secrets | **EVIDENCED** | `Search_Results_NoStrategyBody` / `NoLoginEmail` / `NoContactEmail` / `NoOwnerParticipantId` / `NoAuthSecrets` |
| 4 | Discoverable-fields-only; no new schema | **EVIDENCED** | `Search_Auth_ReturnsDiscoverableFieldsOnly`; `Search_ByIntent/EntityDescription/EntityName_Works`; `Search_QueryPlaneFilter_MatchesQuery`; empty/limit Facts — existing Artifact fields only |
| 5 | Uniform deny / no-leak | **EVIDENCED** | Unauth/invalid → 401 + `Search_Unauth_NoPrivateFieldsInError`; empty/no-match returns empty (not foreign private rows); payload omit Facts |
| 6 | Consume #31 Field ACL (not rewrite) | **EVIDENCED** | Results omit LoginEmail/ContactEmail/StrategyBody (#31 deny classes); structural DTO omit aligns FieldClass deny semantics; #31 registry not rewritten; soft: IFieldPolicy not invoked on search path (checklist soft + SD Security soft-accepted) |
| 7 | No Stage C / #18 inventing | **EVIDENCED** | No Assistant/Cognito/MM/#18 unlock in Product QA scope |
| 8 | OUT locked (P2 instant; V1/V2; #41/#42 not this Story) | **EVIDENCED** | Instant search only; tracks separate |
| 9 | Cost / spend $0 | **EVIDENCED** | No IdP/vault; PoC $0 |
| 10 | Handshake close | **PASS** | Security QA `…discovery-productqa-qa-confirm.md` PASS (pts 1–10 MET); soft gaps accepted |

## Soft gaps / non-blockers

- No live `dotnet` — CI + tests equivalent  
- Structural DTO omit vs per-field Evaluate loop — non-blocking (checklist soft + SD Security soft-accepted)  

## OUT / HOLD (verified)

- #41 separate track · saved-search V1 · A1 V2 · MotorMarket · inventing · #42 merge into this report  

## Disposition

**PASS** — Security QA `…discovery-productqa-qa-confirm.md` PASS (pts 1–10 MET); soft gaps accepted.  
QAQA confirmed Product QA PASS to Chief (no bounce).  
Do not set GitHub status from this step alone.

### Done-list

- [x] Evidence at main `767ab29e…` (+ CI SUCCESS)
- [x] AC 1–6 woven
- [x] Product QA Security checklist woven (pts 1–9)
- [x] Security QA confirm PASS (pt 10)
- [x] QAQA confirm to Chief
- [ ] SoR publish under `docs/qa/` (after PASS; learn from #31)
