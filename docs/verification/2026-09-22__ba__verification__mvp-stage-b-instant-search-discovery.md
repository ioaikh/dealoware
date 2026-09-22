# BA business verification — Story #40 Instant search / discovery (P2)

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-22 (BA verify executed ~2:42 PM ET)  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/40  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/53 (**MERGED** @ main `767ab29e373871db71657c87d330a9671d779443`; MergedAt 2026-09-22 2:19:09 PM ET)  
**CI (main @ merge):** https://github.com/ioaikh/dealoware/actions/runs/35766371083 — **SUCCESS**  
**Method:** Business-intent AC check from Story #40 body + merged PR #53 evidence on **main** (`gh` remote reads of SearchEndpoints / DiscoverableArtifactResponse / DiscoverySearchTests; no clone) + KB Product QA / Security Doc+ProductQA confirms / Doc weave (not code review). No invented requirements. Stage B named slice only. Sibling **#41/#42 OUT** (separate verifies). Soft CLOSE SoR CLEAR (#59/#60/#61 @ `68c2196…`). PoC $0. No MotorMarket/DC4. Soft Assistant OUT not in this Story.

## Binding AC (Story #40 — do not invent)

1. Authenticated Participant can run **instant search** over **discoverable Artifact fields already on path**; Spec does **not** invent new Artifact schema for search
2. Search results return only fields **allowed for discovery** — explicitly **absent**: StrategyBody, LoginEmail, ContactEmail, private account lists / Strategy inventory, auth secrets
3. Discovery is a **separate surface** from Artifact **owner inventory** (#32); must **not** dump private inventory or secrets
4. Unauthenticated → **401**; wrong-principal / stranger misuse → fail-closed with **uniform deny** and **no** private-field leakage
5. Automated tests (or equivalent): auth’d search OK; no StrategyBody/LoginEmail/ContactEmail/private lists; unauth deny; no inventory/secrets dump
6. Documented as **P2** MVP **instant** only — saved-search → **V1**; A1 matching → **V2**

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | Auth’d instant search over discoverable Artifact fields; no new schema | **PASS** | Main `GET /search/artifacts` (`SearchEndpoints.cs`); `DiscoverableArtifactResponse` projects D1–D5 fields only; Facts `Search_Auth_ReturnsDiscoverableFieldsOnly`, `Search_ByIntent/EntityDescription/EntityName_Works`, `Search_QueryPlaneFilter_MatchesQuery`. Product QA AC1 MET @ `767ab29e…`. |
| 2 | Results omit StrategyBody / LoginEmail / ContactEmail / private lists / Strategy inventory / auth secrets | **PASS** | DTO omits OwnerParticipantId / LoginEmail / ContactEmail / StrategyBody / auth secrets. Facts: `Search_Results_NoStrategyBody`, `NoLoginEmail`, `NoContactEmail`, `NoOwnerParticipantId`, `NoAuthSecrets`, `Search_DoesNotDumpPrivateInventory`. Product QA AC2 MET. |
| 3 | Discovery ≠ #32 owner inventory; no private dump | **PASS** | Separate endpoint from `GET /artifacts`. Facts: `Search_IsSeparateFromOwnerInventory`, `Search_CanDiscoverOthersArtifacts`, `Search_DoesNotDumpPrivateInventory`. Product QA AC3 MET; Security ProductQA pt2 MET. |
| 4 | Unauth → 401; stranger misuse fail-closed; no private leak | **PASS** | Facts: `Search_Unauth_Returns401`, invalid auth 401, `Search_Unauth_NoPrivateFieldsInError`; empty/no-match → empty results (not foreign private rows). Product QA AC4 MET; Security ProductQA pts 1+5 MET. |
| 5 | Automated tests matrix | **PASS** | `tests/Dealoware.Api.Tests/DiscoverySearchTests.cs` on main (~25 Facts; size 21029 via `gh`); CI SUCCESS run 35766371083. Product QA AC5 MET. Soft: no live `dotnet test` on evidence box — CI + test inventory equivalent. |
| 6 | P2 instant only — saved-search V1 / A1 V2 OUT | **PASS** | PR/tests scoped to instant GET search; no saved-search / A1 invent. Story OOS + Doc weave Constraints + Product QA AC6 MET; Security pts 7–8 MET. |

## Out of scope held

| OOS item (Story #40 + locks) | Held? | Evidence |
|------------------------------|-------|----------|
| Saved-search / market monitoring (**V1**) | **Yes** | Instant GET only; PR OUT; Doc weave Constraints; Product QA AC6 / Sec pt8 |
| A1 complementary-intent matching (**V2**) | **Yes** | No A1 invent in PR file set; weave + Security pt8 |
| Strategy ACL / Strategy CRUD (**#41**) | **Yes** | #41 OUT — separate BA verify; StrategyBody omitted from search payloads; tracks not merged |
| Contact-on-accept ShareOutbound (**#42**) | **Yes** | #42 OUT — separate BA verify; ContactEmail omitted from search |
| Stage C / Assistant / #26 hard wall | **Yes** | No Assistant/Cognito invent; weave Constraints; Security pt7 |
| Unlocking gate **#25** / parent **#18** Spec/SD | **Yes** | #25 backlog; #18 HOLD; Doc weave Explicit separations; Security pts 7–8 |
| MotorMarket / DC4 | **Yes** | Story + PR OUT; weave; Security pts 7–9 |
| Cognito / SSO / IdP inventing | **Yes** | Reuses #5 auth; PoC $0; Security pt9 |
| PoC/MVP spend (**$0**) | **Yes** | No IdP/vault/AWS provision; Product QA Sec pt9; Doc weave PoC $0 |

## Soft gaps (non-blocking for BA business verify)

- No live `dotnet restore|build|test` on evidence box — accepted; CI SUCCESS run 35766371083 + `DiscoverySearchTests` inventory via `gh` (same pattern as Product QA / Stage A BA).
- Structural DTO omit vs per-field `IFieldPolicy.Evaluate` loop on search path — SD Security + Product QA Security soft-accepted (non-blocking); projection aligns #31 FieldClass deny semantics without rewriting #31 registry.
- Soft CLOSE SoR CLEAR via docs PRs #59/#60/#61 @ `68c2196…` — eng HOLD until CBA confirm (not a BA AC gap).

## Prior gate chain (evidence, not re-scored here)

| Gate | Result | Path / link |
|------|--------|-------------|
| Spec QA | PASS | `verification/2026-09-22__spec__verification__mvp-stage-b-instant-search-discovery.md` + Spec Security confirm |
| Dev Plan QA | PASS | `verification/2026-09-22__devplan__verification__mvp-stage-b-instant-search-discovery.md` + DevPlan Security confirm |
| SD / Dev Code QA | PASS | `verification/2026-09-22__sd__verification__mvp-stage-b-instant-search-discovery.md` |
| SD Security | PASS (1–10 MET) | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-qa-confirm.md` |
| CQ | `cq:no-refactor` | Eng context + Product QA header |
| Product QA | PASS | `qa/2026-09-22__qa__qa-report__mvp-stage-b-instant-search-discovery.md` |
| Product QA Security | PASS (1–10 MET) | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-productqa-qa-confirm.md` |
| Doc Security weave | PASS (overall Doc step) | `ops/2026-09-22__docs__ops__mvp-stage-b-discovery-doc-security-weave.md` |
| Doc Security QA | PASS (1–10 MET) | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-doc-qa-confirm.md` |
| Soft CLOSE SoR | CLEAR | PRs #59/#60/#61 @ `68c219699fff9e8afccd491510e587ca087bf024` |

## Recommendation to CBA

**PASS** — deliverable meets Story #40 business AC (auth’d instant search over discoverable Artifact fields already on path; search payloads omit StrategyBody/LoginEmail/ContactEmail/private inventory/auth secrets; discovery separate from #32 owner inventory; unauth 401 + uniform deny without private leak; DiscoverySearchTests + CI SUCCESS; P2 instant only with saved-search→V1 / A1→V2 OUT), and OOS held (#41/#42 separate; Stage C; #25/#18 HOLD; MotorMarket; Cognito; PoC $0). Soft gaps (no live dotnet; structural DTO omit) non-blocking. Hand to BAQA for verify-QA; eng `done` HOLD until CBA final confirm after BAQA. Do **not** CLOSE issue #40, unlock #25, invent Assistant/Stage C, merge sibling tracks, or invent MotorMarket from this step. Stage B named slice only. PoC $0.
