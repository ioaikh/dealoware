# Security QA — MVP Stage A #31 Field ACL registry SD vs Chief Security SD checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Asked by:** Bot Manager / Chief Security — SD review handshake (PRIORITY ESCALATION; Chief authorized direct PR evidence; Senior catch-up OK)  
**Chief checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-checklist.md` (10 points) — **SD checklist only** (not Dev Plan checklist)  
**Senior Security done-list:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-points-review.md` (**PASS** 10/10)  
**Dev Plan Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-qa-confirm.md`  
**Format ref:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/37  
**HEAD:** `2228718eb7d5f7e7e0a95ac984a01532055c398b` (verified via `gh pr view 37`; matches PR headRefOid)  
**CI:** Build & Test **SUCCESS** (Actions run 35669924556 @ HEAD)  
**Tests:** `tests/Dealoware.Api.Tests/FieldAclTests.cs` (29 new; CI green)  
**Issue:** https://github.com/ioaikh/dealoware/issues/31 · Field ACL registry + API projection (FieldClass / IFieldPolicy)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #32 Account list fail-closed — **not scored / not confirmed here** (keep separate)  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-qa-confirm.md`  
**Constraints:** Stage A API/DB only; agent wall Stage C HOLD; deny-default; LoginEmail OwnAgent Deny; ContactEmail ShareOutbound Deny; #7 stub unchanged; Stage B/C + gate #24 OUT; PoC **$0**; no Cognito/MM/DC4; never skip Chief. Reviewed via `gh` remote reads (no clone). Soft: no live `dotnet test` on this box — CI SUCCESS used.

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security SD checklist | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-checklist.md` | Binding 10 points |
| Senior Security points-review | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-points-review.md` | PASS 10/10 — present (catch-up after direct-evidence start) |
| PR #37 | `ioaikh/dealoware` @ `2228718e…` | 16 files; Domain FieldAcl + ProfileEndpoints/DTOs/Mapper + Participant + FieldAclTests |
| CI | Actions run 35669924556 | SUCCESS @ same HEAD |
| Spec / Dev Plan Security PASS | `…field-acl-spec-qa-confirm.md` / `…field-acl-devplan-qa-confirm.md` | Upstream unlock context |

## Independent re-score (Security QA)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | API/DB scope only — FieldClass registry + IFieldPolicy on API/DB projection; agent hard-wall impl not delivered (Stage C HOLD) | **MET** | Domain `src/Dealoware.Domain/FieldAcl/` (`FieldClass`, `IFieldPolicy`, `FieldPolicy`, principals/actions/context). Application `ProfileMapper` omits denied fields. API `ProfileEndpoints` GET/PATCH `/profile`. No agent/tool gateway / hard-wall code in PR file set. Health remains open (`Health_StillNoAuthRequired`). |
| 2 | Open-ended FieldClass registry — extensible; LoginEmail/ContactEmail examples not exhaustive | **MET** | `FieldClass.Custom(string)`; starters LoginEmail / ContactEmail / DisplayName. Test `FieldClass_IsExtensible`, `FieldClass_EqualsWorks`. |
| 3 | Deny-by-default — unknown/unregistered FieldClass → deny; verified by tests | **MET** | `FieldPolicy.Evaluate`: `!IsRegistered` → false; ShareOutbound / Unauthenticated → false. Tests `FieldPolicy_UnknownFieldClass_DeniedByDefault`, `FieldPolicy_IsRegistered_ReturnsFalseForUnknown`, `FieldPolicy_IsRegistered_ReturnsTrueForKnown`. |
| 4 | LoginEmail User-only (API) — not projected to counterparty, stranger, or OwnAgent via API/DB | **MET** | `EvaluateLoginEmail`: User Read/Write/List only; OwnAgent/Counterparty/Stranger/Unauth → false. Tests `FieldPolicy_OwnAgent_DeniedLoginEmail`, `FieldPolicy_LoginEmailMatrix`, stranger/counterparty deny; User OK `Profile_Get_UserOK_ReturnsAllFields` / `Profile_Patch_UserOK_CanUpdateAllFields`. Mapper omits LoginEmail for OwnAgent (`ProfileMapper_OmitsDeniedFields`). |
| 5 | ContactEmail rules (no share path) — OwnAgent Read / counterparty Deny; ShareOutbound Deny until Stage B; PoC #7 stub unchanged | **MET** | `EvaluateContactEmail`: OwnAgent Read only; Counterparty/Stranger Deny. Global `action == ShareOutbound` → false (Stage B HOLD). Tests `FieldPolicy_OwnAgent_AllowedContactEmailRead`, `FieldPolicy_ContactEmailMatrix`, `FieldPolicy_ShareOutbound_DeniedForAll`. PR files do not touch IdentitySeal / #7 stub (`IdentitySealTests` / StageAFailClosed absent from diff). |
| 6 | Authn fail-closed on projection — protected projection requires validated #5 principal; 401/403; no private fields in error bodies | **MET** | `ProfileEndpoints` Get/Update: `AuthHelper.GetAuthenticatedSub` → `Results.Unauthorized()` when sub null; inactive participant → Unauthorized. Deny write → 403 ProblemDetails Title only (no field values). Tests `Profile_Get_Unauth_Returns401`, `Profile_Get_Unauth_NoPrivateFieldsInBody`, `Profile_Patch_Unauth_Returns401`, `Profile_Get_InvalidAuth_Returns401`, `Profile_Get_InvalidApiKey_Returns401`, `Profile_DenyError_NoPrivateFields`. |
| 7 | No Stage B/C inventing — no Strategy ACL, agent hard-wall, Cognito/SSO, or MM/DC4 | **MET** | Diff/tree: FieldAcl + Profile + Participant columns + DI + FieldAclTests only. No Strategy ACL, Cognito, MM/DC4, agent hard-wall. PR Explicit OUT aligns Spec §6. |
| 8 | Cross-story non-merge — does not rewrite #32 or PoC Stories except consume/cross-ref | **MET** | No `StageAFailClosedTests` / Negotiation/Offer/Artifact list edits. Consumes #5 `AuthHelper` / Jwt/ApiKey. #7 stub untouched. Sibling **#32 not scored here**. |
| 9 | Cost / spend — PoC **$0**; no IdP/vault provision | **MET** | Local #5 JWT/ApiKey + SQLite Participant columns; no Cognito/IdP/vault/AWS packages in PR. |
| 10 | Evidence + handshake — done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms | **MET** | This confirm + Senior done-list cite paths/tests for 1–9. CI SUCCESS @ HEAD. **Dev Code QA / Product QA must not PASS until this Security QA confirm.** |

## Soft notes (non-blocking)

- **Senior SD points-review present** (filed mid-run catch-up) and scored 10/10 MET — independent Security QA re-score **agrees**; no content gaps. Direct PR evidence was authorized by Chief before Senior file appeared.
- **Nullable + Includes* flags vs strict JsonOmit.** `ProfileResponse` uses nullable fields + `IncludesLoginEmail` / `IncludesContactEmail`; policy + mapper + tests enforce deny/omit semantics. Confirm serializer omits nulls or treat Includes* as authoritative — polish only; **non-blocking** (aligns Senior soft note).
- OwnAgent exercised primarily via unit `FieldPolicy` matrix + mapper unit test (correct for Stage A before agent wall); HTTP path is User-authenticated self-profile — expected Stage A API half.
- Soft DisplayName remains illustrative / non-blocking per Spec.
- No live `dotnet test` on this box — static `gh` review + CI Build & Test SUCCESS @ HEAD.
- **#32 not reviewed / not confirmed** in this document.
- PR body evidence table referenced a Dev Plan checklist filename — **ignored**; scoring is against **SD checklist** only.

## Alignment with Senior review

Senior Security done-list (`…mvp-stage-a-field-acl-sd-points-review.md`) scored all 10 **MET** with matching PR #37 / FieldAcl / ProfileEndpoints / ProfileMapper / FieldAclTests cites and CI run 35669924556. Independent Security QA re-score **agrees** — no gaps; soft notes complement (nullable+Includes* non-blocking; #32 unscored; SD checklist binding not Dev Plan).

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| API/DB only; agent wall Stage C HOLD | Held (FieldAcl Domain + Profile projection; no agent gateway) |
| Deny-by-default unknown FieldClass | Held (Evaluate + UnknownFieldClass tests) |
| LoginEmail OwnAgent Deny | Held (EvaluateLoginEmail + OwnAgent_DeniedLoginEmail + matrix) |
| ContactEmail ShareOutbound Deny; #7 stub unchanged | Held (ShareOutbound_DeniedForAll; IdentitySeal not in diff) |
| Authn fail-closed; no private leak in errors | Held (401/403 + NoPrivateFields tests) |
| Stage A only; Stage B/C + #24 OUT | Held (PR OUT; no Strategy/Cognito/MM/DC4) |
| #32 separate — not scored here | Held |
| PoC $0; no Cognito/MM/DC4 | Held |

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are non-blocking.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Bot Manager / Senior Developer / Chief Developer may proceed. Dev Code QA / Product QA may PASS on Security gate after this confirm. Cost/critical: none. No AWS/IdP/vault spend. Do **not** open gate #24. Do **not** treat this as #32 confirm.
