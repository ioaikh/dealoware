# Security QA — MVP Stage B #40 Instant search / discovery Product QA vs Chief Security Product QA checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-22  
**Verdict:** **PASS** (all 10 Product-QA-step Security points MET)  
**Asked by:** Senior Product QA / Chief Security (PRIORITY — Product QA HOLD PASS pts 1–9; pt 10 handshake)  
**Product QA report:** `qa/2026-09-22__qa__qa-report__mvp-stage-b-instant-search-discovery.md` (HOLD PASS pending this confirm; pts 1–9 EVIDENCED on **main**; pt 10 handshake open)  
**Chief checklist (binding):** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-productqa-checklist.md` (10 points)  
**Senior Security Product QA points-review:** **ABSENT** at score time — scored independently vs checklist + Product QA report (same pattern as prior gates when Senior late)  
**Prior SD Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-qa-confirm.md` (PASS 10/10)  
**Format ref:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/53 (**MERGED**)  
**main merge commit:** `767ab29e373871db71657c87d330a9671d779443`  
**CI (main @ merge):** https://github.com/ioaikh/dealoware/actions/runs/35766371083 — **SUCCESS**  
**Tests:** `tests/Dealoware.Api.Tests/DiscoverySearchTests.cs` (~25 Facts)  
**Issue:** https://github.com/ioaikh/dealoware/issues/40 · Instant search / discovery (P2)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #41 HOLD; #42 separate track — **not scored / not confirmed here**  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-productqa-qa-confirm.md`  
**Constraints:** Gate **#25** backlog; Stage C + #18 Spec/SD (whole) HOLD; P2 instant only; saved-search → V1; A1 → V2; PoC **$0**; no Cognito/MM/DC4; Soft: no-live-dotnet OK (CI + DiscoverySearchTests); Soft: structural DTO omit vs Evaluate loop non-blocking.

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security Product QA checklist | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-productqa-checklist.md` | Binding 10 points |
| Product QA report | `qa/2026-09-22__qa__qa-report__mvp-stage-b-instant-search-discovery.md` | HOLD PASS pts 1–9 EVIDENCED; pt 10 HOLD |
| SD Security PASS | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-qa-confirm.md` | PASS 10/10 |
| Senior Product QA points-review | `verification/*discovery*productqa*points*` | **ABSENT** — independent score |
| main merge | `767ab29e373871db71657c87d330a9671d779443` | PR #53 MERGED |
| CI | run 35766371083 | **SUCCESS** |
| Tests | `DiscoverySearchTests.cs` (~25 Facts) | Cited in report |

## Soft gaps accepted (non-blocking)

| Soft gap | Disposition |
|----------|-------------|
| No live `dotnet test` on evidence box | **Accepted** — main CI SUCCESS run 35766371083 + DiscoverySearchTests + prior SD Security PASS (Stage A Product QA pattern) |
| Structural DTO omit vs per-field `IFieldPolicy.Evaluate` loop | **Accepted** — checklist soft + SD Security soft-accepted; secrets absent from payloads (NoStrategyBody / NoLoginEmail / NoContactEmail / NoOwnerParticipantId / NoAuthSecrets) |

## Independent re-score (Security QA)

Score vs **official Product QA checklist 1–10** only.

| # | Point | Product QA | Security QA | Evidence |
|---|-------|------------|-------------|----------|
| 1 | Authn fail-closed | EVIDENCED | **MET** | `Search_Unauth_Returns401`; invalid auth 401; `Search_Unauth_NoPrivateFieldsInError` (AC4 / Sec #1); main `767ab29e…` + CI 35766371083 |
| 2 | Discovery ≠ inventory (#32) | EVIDENCED | **MET** | `Search_IsSeparateFromOwnerInventory`; `Search_DoesNotDumpPrivateInventory`; `Search_CanDiscoverOthersArtifacts` (AC3 / Sec #2) |
| 3 | Search payload omit secrets | EVIDENCED | **MET** | `Search_Results_NoStrategyBody` / `NoLoginEmail` / `NoContactEmail` / `NoOwnerParticipantId` / `NoAuthSecrets` (AC2 / Sec #3) |
| 4 | Discoverable-fields-only; no new schema | EVIDENCED | **MET** | `Search_Auth_ReturnsDiscoverableFieldsOnly`; query-plane Facts (AC1 / Sec #4) |
| 5 | Uniform deny / no-leak | EVIDENCED | **MET** | Unauth hygiene + empty/no-match + no private in errors (Sec #5) |
| 6 | Consume #31 Field ACL (not rewrite) | EVIDENCED | **MET** | Structural DTO omit aligns #31 FieldClass deny semantics for discovery; soft IFieldPolicy-not-on-search-path (SD Security soft-accepted) (Sec #6) |
| 7 | No Stage C / #18 inventing | EVIDENCED | **MET** | No Assistant/Cognito/MM/#18 unlock in Product QA scope (Sec #7) |
| 8 | OUT locked (P2 instant; V1/V2; #41/#42 not this Story) | EVIDENCED | **MET** | Instant search only; #41 HOLD; #42 separate (Sec #8) |
| 9 | Cost / spend $0 | EVIDENCED | **MET** | No IdP/vault; PoC $0 (Sec #9) |
| 10 | Handshake close | HOLD (confirm missing) | **MET** | This confirm closes Product QA Security gate; Product QA may clear HOLD → PASS |

## Alignment with Senior review

Senior Security Product QA points-review **absent** at score time. Independent Security QA re-score vs Chief checklist + Product QA report (main `767ab29e…` + CI SUCCESS + DiscoverySearchTests) + prior SD Security PASS — **all 10 MET**; soft notes align with checklist and SD Security.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Authn fail-closed on instant search; unauth → 401; no private in errors | Held |
| Discovery ≠ #32 owner inventory; no private dump | Held |
| Search DTO omits StrategyBody / LoginEmail / ContactEmail / secrets | Held |
| Discoverable fields only; no new Artifact schema | Held |
| Consume #31; soft structural DTO omit OK | Held |
| Gate #25 backlog; Stage C + #18 HOLD | Held |
| #41/#42 separate; no saved-search/A1 | Held |
| PoC $0; no Cognito/MM/DC4 | Held |
| Evidence on main `767ab29e…` + CI 35766371083 SUCCESS | Held |

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are non-blocking.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Product QA / QAQA may clear HOLD and PASS to Chief. Cost/critical: none. No AWS/IdP spend. Do **not** open gate #25. Do **not** treat this as #41 or #42 confirm. Stage C / #18 Spec/SD (whole) remain HOLD.
