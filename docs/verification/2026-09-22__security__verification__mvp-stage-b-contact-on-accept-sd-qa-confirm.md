# Security QA — MVP Stage B #42 Contact on accept SD vs Chief Security SD checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-22  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Asked by:** Dev Code QA / Chief Developer / Chief Security — SD-step Security handshake  
**Chief checklist (binding):** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-checklist.md` (10 points) — **SD checklist only** (not Dev Plan / Spec checklists)  
**SoR twin (PR #49 / PR #52):** `docs/verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-checklist.md`  
**Senior Security done-list:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-points-review.md` (**PASS** 10/10)  
**Dev Plan Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-devplan-qa-confirm.md` (SoR PR #47)  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-qa-confirm.md` (SoR PR #44)  
**Format ref:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/52 · OPEN  
**HEAD:** `7e6d77e5943ab80029105a57496a76eee9155905` (verified via `gh pr view 52`; matches PR headRefOid)  
**CI:** No checks reported on branch `cursor/mvp-stage-b-contact-on-accept-5768` (empty statusCheckRollup) — soft note only; static + tests evidence holds  
**Mergeable:** CONFLICTING (`mergeStateStatus` DIRTY) — soft note only; does not block Security QA score  
**Tests:** `tests/Dealoware.Api.Tests/StageBContactOnAcceptTests.cs` (21 new) + IdentitySealTests / PocNegotiationScenarioTests updates; PR claims **242** total pass (not re-run live on this box)  
**Issue:** https://github.com/ioaikh/dealoware/issues/42 · Contact on accept (P7 / A9 minimum)  
**Parent:** #18 · Option A · Stage B named slice  
**Precursor:** PoC #7 identity-seal stub CLOSED — **extend**, do not rewrite  
**Siblings:** #40 · #41 — **not scored / not confirmed here** (keep separate)  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-qa-confirm.md`  
**Constraints:** Gate **#25** backlog; Stage C + #18 Spec/SD (whole) HOLD; #7 extend-only; LoginEmail never on Accept; #40/#41 separate; PoC **$0**; no Cognito/MM/DC4/vault inventing. Reviewed via `gh` remote reads (no clone). Soft: empty CI + CONFLICTING merge — do **not** invent CI SUCCESS.

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security SD checklist (KB) | `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-checklist.md` | Binding 10 points |
| SoR checklist twin | `docs/verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-checklist.md` (in PR #52 file set; checklist SoR PR #49) | Present — SoR evidence |
| Senior Security points-review | `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-points-review.md` | **PASS** 10/10 |
| PR #52 | `ioaikh/dealoware` @ `7e6d77e5…` | 15 files; AcceptGrant + FieldPolicy ShareOutbound + AcceptOffer + StageBContactOnAcceptTests |
| CI | branch checks | **Empty** — soft note; PR body claims 242 green |
| Spec / Dev Plan Security PASS | `…contact-on-accept-spec-qa-confirm.md` / `…contact-on-accept-devplan-qa-confirm.md` | Upstream unlock context (SoR PR #44 / #47) |

## Independent re-score (Security QA)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Pre-Accept seal — Neg/Offer omit counterparty contact PII until Accept; extend #7; no regression | **MET** | `NegotiationMapper.ToResponse` / `ToOfferResponse` keep `IdentitySealed = true` and omit contact fields. Tests `PreAccept_GetNegotiation_NoContactEmail_NoLoginEmail`, `PreAccept_GetOffer_NoContactEmail_NoLoginEmail`; IdentitySealTests adapted for Stage B (pre-Accept PII forbid retained). |
| 2 | Accept-grant — User Accept persists grant; `resourceContext.HasAcceptGrant` for FieldPolicy | **MET** | `AcceptGrant.CreatePair` + `IAcceptGrantRepository` persist in `OfferEndpoints.AcceptOffer`; `FieldResourceContext.HasAcceptGrant` + `ForAcceptedNegotiation`; test `Accept_HasAcceptGrant_PersistsInDatabase` (Accept → `IncludesContactEmail`). |
| 3 | ShareOutbound-after-Accept — ContactEmail ShareOutbound only with Accept grant to authorized counterparty; before → Deny all | **MET** | `FieldPolicy.EvaluateShareOutbound`: ContactEmail Allow only when `HasAcceptGrant && Counterparty`; else Deny. Mapper `ToAcceptOfferResponse` evaluates ShareOutbound via policy. Tests `PreAccept_ShareOutbound_Deny_FieldPolicy`, `PostAccept_ShareOutbound_Allow_FieldPolicy`. |
| 4 | ContactEmail policy rows — User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny | **MET** | `EvaluateContactEmail` matrix; ShareOutbound path gated separately. Tests `ContactEmail_PolicyMatrix_PreAccept` (theory), `ContactEmail_Counterparty_ShareOutbound_AllowedPostAccept`. |
| 5 | LoginEmail never-on-Accept — LoginEmail User-only; never shared on Accept; distinct from ContactEmail | **MET** | `EvaluateShareOutbound` always returns false for `FieldClass.LoginEmail`. `AcceptOfferResponse` has ContactEmail fields only — no LoginEmail property. Tests `Accept_CounterpartyReceivesContactEmail_LoginEmailNever`, `Accept_LoginEmail_NeverIncluded`, `ShareOutbound_LoginEmail_AlwaysDeny`, `LoginEmail_PolicyMatrix`. |
| 6 | Authn / stranger fail-closed — Unauth deny; stranger deny ContactEmail post-Accept; uniform deny; no private leakage | **MET** | Accept/Get paths require `AuthHelper.GetAuthenticatedSub` → 401. Tests `Unauth_CannotAccessNegotiation_NoPiiLeak`, `Unauth_CannotAcceptOffer`, `PostAccept_StrangerCannotAccessNegotiation` (404 + no PII in body). |
| 7 | Extend #7 under ACL — Extend seal→contact; do **not** rewrite #7 history or invent #18 Spec/SD unlock | **MET** | IdentitySealTests updated (+43/−15) for Stage B post-Accept ContactEmail path; pre-Accept seal assertions retained. No Stage C share-tool / agent gateway. PR does not unlock whole #18 Spec/SD. `OfferResponse` unchanged for non-Accept paths. |
| 8 | OUT locked — P7/A9 minimum; mature vault → V3; gate #25 backlog; no Cognito/MM inventing; no Stage C share-tool | **MET** | PR Explicit OUT: vault→V3; Stage C share-tool; #40/#41; gate #25; Cognito/SSO; MotorMarket/DC4. File set is AcceptGrant + FieldAcl ShareOutbound + AcceptOffer + tests only — no Cognito/vault/MM/DC4/Strategy packages. |
| 9 | Cost / spend — PoC **$0**; no IdP/vault provision | **MET** | Local SQLite in-memory tests; no AWS/IdP/vault packages in PR. |
| 10 | Evidence + handshake — Done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms | **MET** | This confirm + Senior done-list cite paths/tests for 1–9. **Dev Code QA / Product QA must not PASS until this Security QA confirm.** |

## Soft notes (non-blocking)

- **Merge CONFLICTING** (`mergeable: CONFLICTING`, `mergeStateStatus: DIRTY`) at score time — merge hygiene for parent/Chief Developer; **does not** overturn MET evidence on static code + tests.
- **CI empty** — `gh pr checks` reports no checks on branch; `statusCheckRollup: []`. PR body claims `Passed! Failed: 0, Passed: 242`. Soft: do **not** invent CI SUCCESS; static FieldPolicy/AcceptGrant/AcceptOffer + StageBContactOnAcceptTests evidence holds for Security QA.
- **Senior SD points-review present** and scored 10/10 MET — independent Security QA re-score **agrees**; no content gaps.
- **LoginEmail ≠ ContactEmail** — FieldClass + EvaluateShareOutbound + AcceptOfferResponse keep them distinct (LoginEmail never on Accept).
- **#7 extend-only** — IdentitySealTests adapted, not a rewrite of CLOSED #7 Spec/history.
- **Accept_HasAcceptGrant_PersistsInDatabase** asserts Accept success + `IncludesContactEmail` (grant path exercised via Accept endpoint + CreatePair); repository-level row assert is soft polish only — non-blocking given CreatePair + AddRangeAsync wiring.
- No live `dotnet test` on this box — static `gh` review + PR-claimed 242.
- **#40 / #41 not reviewed / not confirmed** in this document.
- Scoring is against **SD checklist only** (not Dev Plan checklist).

## Alignment with Senior review

Senior Security done-list (`verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-points-review.md`) scored all 10 **MET** with matching PR #52 / AcceptGrant / FieldPolicy / AcceptOffer / StageBContactOnAcceptTests cites. Independent Security QA re-score **agrees** — no gaps; soft notes complement (CONFLICTING merge + empty CI non-blocking; #40/#41 unscored; SD checklist binding).

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| #7 extend-only (no rewrite; seal→contact under ACL) | Held (IdentitySealTests adapt; OfferResponse pre-Accept sealed) |
| LoginEmail ≠ ContactEmail; LoginEmail never on Accept | Held (EvaluateShareOutbound always Deny LoginEmail; AcceptOfferResponse has no LoginEmail) |
| ShareOutbound ContactEmail only with HasAcceptGrant + Counterparty | Held (FieldPolicy + mapper + tests) |
| Gate #25 backlog; Stage C + #18 Spec/SD HOLD | Held (PR OUT; no share-tool / whole-#18 unlock) |
| #40 / #41 separate — not scored here | Held |
| PoC $0; no Cognito/MM/DC4/vault inventing | Held |

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are non-blocking (CI pending / merge CONFLICTING / test-name polish).

## Handshake status

Security QA → **PASS** confirm to Chief Security. Dev Code QA / Product QA / Chief Developer may proceed on Security gate after this confirm (subject to Chief clear). Cost/critical: none. No AWS/IdP/vault spend. Do **not** open gate #25. Do **not** treat this as #40/#41 confirm. Stage C + #18 Spec/SD (whole) remain HOLD.
