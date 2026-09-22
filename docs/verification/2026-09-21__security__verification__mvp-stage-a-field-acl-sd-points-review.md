# Verification — Security points vs MVP Stage A #31 Field ACL SD

**Author:** Dealoware Senior Security  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-checklist.md` (10 points)  
**PR:** https://github.com/ioaikh/dealoware/pull/37 · OPEN · HEAD `2228718e…`  
**CI:** Build & Test **SUCCESS** (Actions run 35669924556)  
**Tests:** `tests/Dealoware.Api.Tests/FieldAclTests.cs` (~29 Facts)  
**Issue:** https://github.com/ioaikh/dealoware/issues/31  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-points-review.md`  
**Constraints:** API/DB only; agent wall Stage C HOLD; #7 stub unchanged; separate from #32; Stage B/C HOLD; gate #24 HOLD; PoC $0; no Cognito/MM/DC4.

## Checklist vs PR

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | API/DB scope only | **MET** | Domain `FieldAcl/` (`FieldClass`, `IFieldPolicy`, `FieldPolicy`); Application `ProfileMapper` projection; API `ProfileEndpoints` GET/PATCH `/profile`. No agent/tool hard-wall implementation. |
| 2 | Open-ended FieldClass | **MET** | `FieldClass.Custom(name)`; starters LoginEmail/ContactEmail/DisplayName. Test `FieldClass_IsExtensible`. |
| 3 | Deny-by-default | **MET** | `FieldPolicy.Evaluate` returns false when `!IsRegistered`. Tests `FieldPolicy_UnknownFieldClass_DeniedByDefault`, `FieldPolicy_IsRegistered_ReturnsFalseForUnknown`. |
| 4 | LoginEmail User-only (API) | **MET** | `EvaluateLoginEmail`: User R/W/List; OwnAgent/Counterparty/Stranger false. Tests `FieldPolicy_OwnAgent_DeniedLoginEmail`, stranger/counterparty deny; User OK get/patch. |
| 5 | ContactEmail rules (no share); #7 unchanged | **MET** | OwnAgent Read only; counterparty/stranger Deny; global `ShareOutbound` → false (Stage B HOLD). Tests `FieldPolicy_OwnAgent_AllowedContactEmailRead`, `FieldPolicy_ShareOutbound_DeniedForAll`. PR does not touch IdentitySeal / #7 stub. |
| 6 | Authn fail-closed on projection | **MET** | `AuthHelper.GetAuthenticatedSub` → Unauthorized on null. Tests `Profile_Get_Unauth_Returns401`, `Profile_Get_Unauth_NoPrivateFieldsInBody`, invalid auth 401s; deny paths omit LoginEmail/ContactEmail in body asserts. |
| 7 | No Stage B/C inventing | **MET** | No Strategy ACL / agent hard-wall / Cognito / MM/DC4 in PR files. OUT table in PR body. |
| 8 | Cross-story non-merge | **MET** | Diff limited to FieldAcl + Profile + Participant columns + FieldAclTests. No StageAFailClosed / Negotiation/Offer/Artifact edits (#32 separate). |
| 9 | Cost / spend PoC $0 | **MET** | Local #5 auth + SQLite; no IdP/vault packages added. |
| 10 | Evidence + handshake | **MET** | This done-list cites 1–9. Code/Product QA must **not** PASS until Security QA confirms. |

## Soft notes (non-blocking)

- `ProfileResponse` documents “omit when denied” via nullable + `IncludesLoginEmail`/`IncludesContactEmail` flags; confirm JSON serializer omits nulls (or treat flags as authoritative). Policy + tests already enforce deny. Polish only.
- OwnAgent exercised primarily via unit `FieldPolicy` matrix (correct for Stage A before agent wall); HTTP path is User-authenticated self-profile — expected for Stage A API half.

## Gaps

**None.**

## Done-list (for Security QA)

- [x] DOC-FLOW filed
- [x] Score 10/10 with path/test cites
- [x] CI SUCCESS cited
- [x] #32 not merged into this Story; #7 untouched; Stage B/C HOLD; PoC $0
- [ ] Security QA: confirm **PASS** to Chief Security **or** further instructions

## Cost/critical

None.
