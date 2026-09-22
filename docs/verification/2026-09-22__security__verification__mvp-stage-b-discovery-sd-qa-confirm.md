# Security QA — MVP Stage B #40 Instant search / discovery SD vs Chief Security SD checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-22  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Asked by:** Senior Security / Dev Code QA / Chief Security — SD review handshake (PRIORITY)  
**Chief checklist (binding):** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-checklist.md` (10 points) — **SD checklist only** (not PR-body renumber; not Dev Plan checklist)  
**SoR (GitHub, PR #49):** `docs/verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-checklist.md`  
**Senior Security done-list:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-points-review.md` (**PASS** 10/10)  
**Dev Plan Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-devplan-qa-confirm.md` (SoR PR #47)  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-qa-confirm.md` (SoR PR #44)  
**Format ref:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-sd-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/53 · OPEN  
**HEAD:** `7973da3e8c8cd27825073947c5c1cecb45df1e5d` (verified via `gh pr view 53`; re-spot-check after Senior rebase watch — HEAD unchanged)  
**CI:** `statusCheckRollup` **empty**; no checks reported on branch `cursor/mvp-stage-b-discovery-search-c1c3` (soft non-blocking; see Soft notes)  
**Tests:** `tests/Dealoware.Api.Tests/DiscoverySearchTests.cs` (25 Facts; PR claims suite green — not independently re-run on this box)  
**Issue:** https://github.com/ioaikh/dealoware/issues/40 · Instant search / discovery (P2)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #41 · #42 — **not scored / not confirmed here** (keep separate)  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-qa-confirm.md`  
**Constraints:** Gate **#25** backlog; Stage C Assistant (#26) + #27 + #18 Spec/SD (whole) HOLD; discovery ≠ #32; #41/#42 separate; PoC **$0**; no Cognito/MM/DC4; no saved-search/A1 inventing; Soft Assistant OUT. Reviewed via `gh` remote reads (no clone). Soft: mergeable **CONFLICTING** + empty CI — non-blocking given static+tests evidence MET and Senior PASS.

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security SD checklist (KB) | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-checklist.md` | Binding 10 points |
| SoR checklist (GitHub) | `docs/verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-checklist.md` (PR #49) | Present — SoR twin (also in PR #53 file set) |
| Senior Security points-review | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-points-review.md` | **PASS** 10/10 |
| PR #53 | `ioaikh/dealoware` @ `7973da3e…` | 8 files; SearchEndpoints + DiscoverableArtifactResponse + mapper/repo + DiscoverySearchTests |
| CI | `statusCheckRollup` / branch checks | Empty / none reported (soft) |
| Spec / Dev Plan Security PASS | `…discovery-spec-qa-confirm.md` (PR #44) / `…discovery-devplan-qa-confirm.md` (PR #47) | Upstream unlock context |

## Independent re-score (Security QA)

Score vs **official SD checklist 1–10** (not PR-body renumber).

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Authn fail-closed — Instant-search paths require validated #5 principal; unauth → **401**; error bodies omit private fields / secrets | **MET** | `SearchEndpoints.SearchArtifacts` → `AuthHelper.GetAuthenticatedSub` → `Results.Unauthorized()` if `sub` null/whitespace. Tests: `Search_Unauth_Returns401`, `Search_InvalidAuth_Returns401`, `Search_InvalidApiKey_Returns401`, `Search_Unauth_NoPrivateFieldsInError`. |
| 2 | Discovery ≠ inventory — Discovery surface separate from #32 owner list/get; no private inventory dump via search | **MET** | New `GET /search/artifacts` (MapGroup `/search`) separate from owner `GET /artifacts`. Tests: `Search_IsSeparateFromOwnerInventory`, `Search_CanDiscoverOthersArtifacts`, `Search_DoesNotDumpPrivateInventory`. |
| 3 | Search payload omit secrets — Serializers omit StrategyBody / LoginEmail / ContactEmail / private lists / Strategy inventory / auth secrets | **MET** | `DiscoverableArtifactResponse` structurally omits OwnerParticipantId, LoginEmail, ContactEmail, StrategyBody, auth secrets. Tests: `Search_Results_NoOwnerParticipantId`, `Search_Results_NoLoginEmail`, `Search_Results_NoContactEmail`, `Search_Results_NoStrategyBody`, `Search_Results_NoAuthSecrets`. |
| 4 | Discoverable-fields-only — Search/result fields limited to allowed Artifact fields already on path; no new Artifact schema | **MET** | DTO projects Intent / entities / values / locations / timePeriods (+ id, createdAt) only; `ArtifactRepository.SearchDiscoverableAsync` queries existing Intent/Entity name/description/Property name/value columns; no new schema in PR. Test: `Search_Auth_ReturnsDiscoverableFieldsOnly`. |
| 5 | Uniform deny / no-leak — Wrong-principal / stranger misuse → fail-closed; uniform deny bodies; no private-field leakage | **MET** | Unauth → empty 401 body path; test `Search_Unauth_NoPrivateFieldsInError`. Query-plane filter at DB (`Where` + `Take`) — tests `Search_QueryPlaneFilter_MatchesQuery`, `Search_DoesNotDumpPrivateInventory`. |
| 6 | Consume #31 Field ACL — Projection omit via `IFieldPolicy` / FieldClass; do **not** rewrite #31 | **MET** | Discovery-safe DTO omit aligns #31 FieldClass deny for discovery (Stranger); PR does not rewrite FieldPolicy registry. Soft: structural DTO omit vs per-field `Evaluate` loop — Senior accepted as soft polish (non-blocking). |
| 7 | No Stage C / #18 inventing — No Assistant hard wall, Cognito, MM/DC4, or unlocking #18 Spec/SD as a whole | **MET** | File set limited to search endpoint/DTO/mapper/repo/tests + SD checklist doc. Path scan ABSENT Cognito/Assistant/MM/DC4/gate-25. Soft Assistant OUT. #18 not unlocked. |
| 8 | OUT locked — P2 instant only; saved-search → V1; A1 → V2; gate #25 backlog; do not implement #41/#42 here | **MET** | PR Out of Scope: saved-search→V1; A1→V2; #41/#42 not implemented; gate #25 backlog. Diff has no Strategy CRUD / Contact-on-accept / saved-search / A1 matching. |
| 9 | Cost / spend — PoC **$0**; no IdP/vault provision | **MET** | Local SQLite + existing #5 JWT/ApiKey auth; no Cognito/IdP/vault/AWS packages. Test: `Health_StillNoAuthRequired`. |
| 10 | Evidence + handshake — Done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms | **MET** | This confirm + Senior done-list cite paths/tests for 1–9. Upstream Spec PASS (PR #44) + Dev Plan PASS (PR #47) held. **Dev Code QA / Product QA must not PASS until this Security QA confirm.** |

## Soft notes (non-blocking)

- **mergeable CONFLICTING** on PR #53 at score time — code/tests evidence still MET; non-blocking for Security QA (merge hygiene for Dev/CPM). Do not invent CI SUCCESS.
- **CI empty** — `statusCheckRollup: []`; no checks reported on branch. Senior also noted pending CI. Static `gh` review of HEAD files/tests holds; Security QA may re-check CI on merge. **Non-blocking.**
- **Structural DTO omit vs per-field `IFieldPolicy.Evaluate` loop** — projection is discovery-safe shape omit rather than Evaluate loop; still consumes #31 FieldClass deny semantics for discovery. Senior accepted as soft polish; Chief PRIORITY reinforces non-blocking. Aligns Senior soft note.
- **PR body renumbers** security points vs Chief SD checklist — **ignored**; scoring uses **official checklist 1–10** only.
- **HEAD re-spot-check** — after Senior rebase watch, HEAD still `7973da3e…`; security-relevant paths unchanged; no scope change.
- No live `dotnet test` on this box — static `gh` review @ HEAD `7973da3e…`.
- **#41 / #42 not reviewed / not confirmed** in this document.
- Soft Assistant OUT; Gate **#25** backlog; Stage C + #18 HOLD; PoC **$0**.

## Alignment with Senior review

Senior Security done-list (`…mvp-stage-b-discovery-sd-points-review.md`) scored all 10 **MET** with matching PR #53 / SearchEndpoints / DiscoverableArtifactResponse / ArtifactRepository / DiscoverySearchTests cites. Independent Security QA re-score **agrees** — no gaps; soft notes complement (CONFLICTING + empty CI non-blocking; structural DTO omit soft; PR-body renumber ignored; #41/#42 unscored; Soft Assistant OUT).

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Authn fail-closed on `/search/artifacts`; unauth → 401 | Held (`AuthHelper` + Unauthorized + 401 tests) |
| Discovery ≠ #32 owner inventory | Held (separate endpoint + separation/dump tests) |
| Search DTO omits StrategyBody / LoginEmail / ContactEmail / OwnerParticipantId / auth secrets | Held (DTO shape + Search_Results_No* tests) |
| Discoverable D1–D5 only; no new Artifact schema | Held (DTO + SearchDiscoverableAsync columns) |
| Uniform deny; no private leak in errors | Held (NoPrivateFieldsInError) |
| Consume #31; no registry rewrite; soft DTO-omit OK | Held |
| Gate #25 backlog; Stage C + #18 HOLD; Soft Assistant OUT | Held |
| #41/#42 separate; no saved-search/A1 | Held |
| PoC $0; no Cognito/MM/DC4 | Held |

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are non-blocking.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Bot Manager / Senior Developer / Chief Developer may proceed on Security gate. Dev Code QA / Product QA may PASS on Security gate after this confirm. Cost/critical: none. No AWS/IdP/vault spend. Do **not** open gate #25. Do **not** treat this as #41 or #42 confirm. Stage C / #18 Spec/SD (whole) remain HOLD. Soft Assistant OUT.
