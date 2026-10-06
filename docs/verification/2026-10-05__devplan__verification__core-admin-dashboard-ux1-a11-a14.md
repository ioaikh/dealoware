# Dev Plan QA — A5 Core admin dashboard, UX1-A11 / UX1-A14 amendment

| Field | Value |
|-------|-------|
| Author | Dealoware Dev Plan QA |
| Date | 2026-10-05 (~8:34pm ET) |
| Verdict | **PASS** |
| Plan | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` |
| Final tip sha256 | `3385872a881095b8b44a90a4148f5faf87594c39fd742897f089e1425b93caff` (MATCH, re-hashed before writing) |
| Tip history | `5fefb550…` (A5 PASS) → `abfa672a…` (Security bounce) → `1c173e98…` (Security PASS) → `3385872a…` (final, Spec-cite refresh) |
| Security qa-confirm | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` (file sha256 `ae61c17e…`) — **PASS** on tip `3385872a…`: pts 1–14 MET, Step 14 binds 6/6, Step 15 binds 3/3, Spec-cite refresh MET |
| Prior A5 evidence | `verification/2026-10-05__devplan__verification__core-admin-dashboard.md` |
| Asked by | Chief Dev Planner (UX1-A11/A14 amend; confirm to Chief only) |
| Host | `admin.core.dealoware.com` only |

## Binding sources (hashes match on disk)

| Source | sha256 |
|--------|--------|
| UX findings `ux/2026-10-05__ux__findings__ux1-admin-requirements.md` (UX1-A11, UX1-A14) | `168db839…` |
| UX addendum `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` (Parts B–D) | `dd847e70…` |
| Routing r2 `ux/2026-10-05__ux__redlines__ux1-routing.md` | `fde55fba…` |
| CPM decision Steps 14/15 `ops/2026-10-05__cpm__decision__core-admin-dashboard-a7-ui-gates.md` | `322cfa0e…` |
| A16 two-step sign-in decision | `bb0bcaa2…` |
| A03 generic-copy decision | `0817b767…` |
| Password answers (current; `f03a82c9…` is void) | `a87e293b…` |
| Sign-in steps Spec note | `0a3db5f4…` (Spec QA + Security QA PASS) |
| Lockout-window note v5.5 | `8a194eb9…` (Spec QA + Security QA PASS) |
| Password-rules note v2.3 | `1a7b342c…` (Spec QA + Security QA PASS) |

## Checks

| # | Check | Result |
|---|-------|--------|
| 1 | Same path; Status names UX1-A11/A14, 15 steps, PoC $0, not a build unlock | MET |
| 2 | A11: Steps 2–5 UI deliverables cite UXR / S-A IDs only; all 61 IDs exist in addendum `dd847e70` | MET |
| 3 | A11: Steps 2–5 Story bullets map to UX1-A01–A10 UI AC (cases green); DoD in Step 13, Explicit OUT, Next; backend and security gates unchanged | MET |
| 4 | A14: Step 14 covers sign-in, 2FA enrol/verify, recovery codes, Turnstile states, email-link reset, lockout; Chief Developer accountable, Senior Developer implements; Steps 2–5 are machinery plus cites | MET |
| 5 | A14: Step 15 audit viewer cites S-D1/S-D2, UXR-D12–D13; read-only; shows an HMAC IP prefix only, never raw IP | MET |
| 6 | Step count 15 in §4 note, Status, Locked #15, §5 mapping, done-lists | MET |
| 7 | Amend creates no A7 Brief 4, step-14, or checklist files (those that exist belong to CPM / Senior PM) | MET |
| 8 | Sources cite findings, addendum, routing, CPM decision and the Step 14 decision and Spec notes, each with sha256 | MET |
| 9 | Plain-English Active holds list; new and edited amendment text has no retired hold phrase and no word repeated more than 5 times in a row | MET |
| 10 | No new Dev Plan security points; §6 still traces pts 1–14 unchanged; no passwords, AWS IDs or keys | MET |

## Fixes confirmed across tips

- Step 14 cites A16 (conditions 1–8), A03, answers `a87e293b`, password rules and lockout.
- The UXR-A40 banner leaves out the S-A12 link until Spec adopts S-A12.
- Step 15 says HMAC IP prefix and cites UXR-D13.
- Locked #15 and the §6 note both list pts 1–6, 8, 9, 11, 13, 14.
- The Step 5 Story bullet adds UXR-A19–A20, and Step 5 cross-cites A16 for the server-side work (conditions 1–3, 5, 7).
- Step 8 adds the `signin.second_factor_failed` audit event.
- The finance cite now points to `verification/`.

## Non-blocking notes

1. Plan L39 and the §8 paragraph still carry the retired hold phrase in older wording. Chief Dev Planner set the cleanup for the next edit.
2. The three Spec notes still have a "Draft" status in their headers, although PASS records exist. The sign-in-steps note's cross-reference still points to lockout v5.4 and password rules `7f73db9b`. Spec Team upkeep, not the plan's.
3. The addendum Status line still says Part A only and cites findings `0a50e61d`. Routed to Chief UI/UX.
4. Plan L20 timestamp nit: it says ~8:31pm ET for the password-rules Spec QA PASS, but that file was last written at 8:28pm ET.

## Active holds

- No Stories, code, CDK, spend or provisioning until A6 Test design passes.
- No deploy until Ivan approves.
- A7 stays closed until H4 passes live.
- The H1 hardening redeploy is a separate track.
- No password values, AWS account IDs, keys or SES identities may be invented.
- No process-wide sign-in flood limiter until Chief Security approves it.
- This is not a build unlock. PoC cost stays at $0.

## Handshake

PASS to Chief Dev Planner only.
