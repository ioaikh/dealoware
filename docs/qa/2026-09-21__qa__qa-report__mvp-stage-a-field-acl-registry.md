# QA Report — MVP Stage A Field ACL registry + API projection (#31)

**Status:** **PASS** — Security QA confirm landed (pts 1–10 MET); QAQA confirmed Product QA PASS to Chief (no bounce)  
**Date:** 2026-09-21  
**Author:** Dealoware Senior Product QA  
**Confirm to:** Dealoware QA (Chief QA) via QAQA **after** Security handshake  
**Story:** GitHub issue #31 · Field ACL registry + API projection (FieldClass / IFieldPolicy)  
**Issue:** https://github.com/ioaikh/dealoware/issues/31  
**PR:** https://github.com/ioaikh/dealoware/pull/37 (**MERGED**)  
**PR HEAD:** `2228718eb7d5f7e7e0a95ac984a01532055c398b`  
**main merge commit:** `fb47fdd3c3563a6eccfbc85ee60ce1dd5b65c7ab`  
**MergedAt:** 2026-09-22T00:06:03Z (≈ 2026-09-21 20:06 EDT)  
**Branch:** `cursor/field-acl-registry-00df`  
**CI (PR HEAD):** https://github.com/ioaikh/dealoware/actions/runs/35669924556 — **SUCCESS**  
**CI (main @ merge):** https://github.com/ioaikh/dealoware/actions/runs/35670498420 — **SUCCESS**  
**Security checklist:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-checklist.md`  
**Security QA confirm (this step):** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-qa-confirm.md` — **PASS** (pts 1–10 MET)  
**Prior SD:** `verification/2026-09-21__sd__verification__mvp-stage-a-field-acl-registry.md` (SD PASS @ `2228718e…`)  
**Prior SD Security QA:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-qa-confirm.md` (PASS 10/10)  
**CQ:** `cq:no-refactor`  
**DOC-FLOW:** `qa/2026-09-21__qa__qa-report__mvp-stage-a-field-acl-registry.md`  
**Constraints:** PoC $0; Stage B/C HOLD; gate #24 not opened; agent hard-wall OUT; no Cognito/MM; do not rewrite #32/#7  

## Method

- `gh pr view 37` / contents at **main** `fb47fdd3…` (no clone); re-verified after merge
- KB: Product QA Security checklist; SD verification (cite only — do not re-litigate Code QA)
- Soft gap: no live `dotnet test` on this box → **CI + `FieldAclTests.cs` equivalent** (same pattern as #32)
- Soft: DisplayName non-blocking (CPM)

## Product acceptance criteria

| AC | Verdict | Evidence (main `fb47fdd3…`) |
|----|---------|--------------------------------|
| 1 Extensible FieldClass registry (open-ended) | **MET** | `FieldClass` starters + `Custom(name)`; `FieldClass_IsExtensible`; `FieldClass_EqualsWorks` |
| 2 IFieldPolicy.Evaluate; deny-by-default unknown | **MET** | `IFieldPolicy` / `FieldPolicy.Evaluate`; `FieldPolicy_UnknownFieldClass_DeniedByDefault`; `FieldPolicy_IsRegistered_*` |
| 3 CEO starters only: LoginEmail, ContactEmail, DisplayName | **MET** | Static starters on `FieldClass`; registered set in `FieldPolicy`; no invent beyond |
| 4 LoginEmail: User R/W; OwnAgent Deny; Counterparty/Stranger/Unauth Deny | **MET** | `FieldPolicy_LoginEmailMatrix`; `FieldPolicy_OwnAgent_DeniedLoginEmail`; stranger/counterparty Facts |
| 5 ContactEmail: User R/W; OwnAgent Read; Counterparty/Stranger Deny; ShareOutbound Deny; #7 unchanged | **MET** | `FieldPolicy_ContactEmailMatrix`; `FieldPolicy_OwnAgent_AllowedContactEmailRead`; `FieldPolicy_ShareOutbound_DeniedForAll`; SD: #7 stub untouched |
| 6 DTO omit denied / no private fields in errors | **MET** | `ProfileMapper_OmitsDeniedFields`; `Profile_Get_Unauth_NoPrivateFieldsInBody`; `Profile_DenyError_NoPrivateFields`; soft: denied → `null` + `Includes*` (SD Security accepted) |
| 7 Generic any-account-info; API-layer only (no agent gateway) | **MET** | Open `FieldClass.Custom`; Profile GET/PATCH only; no agent hard-wall in PR file set |
| 8 Tests: owner/User OK; OwnAgent LoginEmail deny; stranger/counterparty; unauth | **MET** | `FieldAclTests.cs` (~29 Facts/Theories); main CI SUCCESS run 35670498420 |

## Security checklist points 1–10 (Product QA evidence)

| # | Point | Verdict | Evidence |
|---|-------|---------|----------|
| 1 | API/DB scope only; agent hard-wall not delivered | **EVIDENCED** | Domain FieldAcl + Profile API/DB projection; Stage C agent wall OUT |
| 2 | Open-ended FieldClass registry | **EVIDENCED** | Starters + `Custom`; extensibility Facts |
| 3 | Deny-by-default unknown/unregistered | **EVIDENCED** | `FieldPolicy_UnknownFieldClass_DeniedByDefault`; unregistered → false |
| 4 | LoginEmail User-only (API) | **EVIDENCED** | LoginEmail matrix + OwnAgent deny Fact |
| 5 | ContactEmail rules; ShareOutbound Deny; #7 unchanged | **EVIDENCED** | ContactEmail matrix; ShareOutbound deny-all; SD #7 cite |
| 6 | Authn fail-closed on projection; no private in errors | **EVIDENCED** | `Profile_*_Unauth_Returns401`; invalid auth 401; no-private-fields Facts; Health open |
| 7 | No Stage B/C inventing | **EVIDENCED** | No Strategy ACL / Cognito / MM-DC4 / agent hard-wall |
| 8 | Cross-story non-merge (#32 separate) | **EVIDENCED** | PR file set is Field ACL + Profile; no StageAFailClosed rewrite |
| 9 | Cost / spend $0 | **EVIDENCED** | No IdP/vault provision; PoC $0 |
| 10 | Handshake close | **PASS** | Security QA `…field-acl-productqa-qa-confirm.md` PASS (pts 1–10 MET); soft gaps accepted |

## Soft gaps / non-blockers

- No live `dotnet restore|build|test` on evidence box — CI + tests equivalent stated
- Denied fields may surface as `null` with `Includes*` flags rather than JSON property omission — SD Security accepted; Product QA cites same soft note
- DisplayName soft/illustrative — non-blocking per CPM
- Re-verified on main merge `fb47fdd3…` after CPM merge (not PR HEAD alone)

## OUT / HOLD (verified)

- Stage B/C · gate #24 · agent hard-wall · Cognito · MotorMarket · inventing  
- #32 rewrite (separate closed track)  

## Disposition

**PASS** — Security QA `…field-acl-productqa-qa-confirm.md` PASS (pts 1–10 MET); QAQA confirmed Product QA PASS to Chief (no bounce). Soft gaps accepted (null+Includes*; DisplayName non-blocking; no live dotnet → CI+tests).  
Do not set GitHub status from this step alone (PM/Chief owns gate). Stage B/C and agent hard-wall remain HOLD/OUT.

### Done-list (QAQA)

- [x] Evidence at main merge `fb47fdd3…` (+ PR HEAD `2228718e…`)
- [x] AC 1–8 + Sec 1–9 woven with citations
- [x] Product-step Security QA confirm PASS (pt 10)
- [x] Re-verify on main after merge (CI SUCCESS run 35670498420)
- [x] QAQA confirm Product QA PASS to Chief
