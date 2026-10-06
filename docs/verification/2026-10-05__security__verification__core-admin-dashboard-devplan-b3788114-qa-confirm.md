# Security QA — Dev Plan · Core admin dashboard (qa-confirm) — tip `b3788114…`

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~9:08pm ET) |
| Verdict | **PASS** — UI/UX QA citation/record delta MET (6/6); retired hold phrase absent (0); checklist pts 1–14 MET; Step 14 binds 6/6 MET; Step 15 binds 3/3 MET; H4 primary cite + Steps 7/9 W/E carry MET from tip `41928cbe…` PASS |
| Asked by | Chief Dev Planner (UI/UX QA bounce — 4 citation/record fixes; voids PASS on tip `41928cbe…` / evidence sha256 `4d1970ba…` for current tip) |
| Checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` (pts 1–14) |
| Plan tip | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` |
| Plan tip sha256 | `b3788114b4ab890a8117158807773d3e5f8fe84bec4ee0f2f0f0d5df03014a58` — **MATCH** (re-hashed with `sha256sum` before scoring and again immediately before this write) |
| Prior tips VOID | `41928cbeab5823849f9b4eabd91a62ad75150a8a1d9fd97c1a5ef3cea7c48d8c` (H4 cite PASS) **VOID** for current tip; also VOID: `bd814d40…`, `21535e66…`, `3385872a…`, `1c173e98…`, `970b453a…`, `abfa672a…`, `5fefb550…` |
| Frozen / prior confirms (untouched) | (1) `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` — frozen sha256 `5d7e7fca8dca7fb4cef71a52b5cecd95b9bb400a1641ff16686851042d9923ac` (VOID tip `21535e66…` only). (2) `…devplan-bd814d40-qa-confirm.md` — sha256 `bed27d5740333185b7c63f3f31bc50c309f59d218782f11f36bab2068ba03d5c` (VOID). (3) `…devplan-41928cbe-qa-confirm.md` — sha256 `4d1970ba4caf2efa29e4d40415ffde30dcbe2bdb622c656ea008a43eb87b9090` (VOID for current tip; left untouched). |
| Host | `admin.core.dealoware.com` only |
| Principal | **CoreOwner** = single system superadmin `io@aiknowhow.com` |
| PoC | **$0** |
| Handshake | **qa-confirm only** (this file). Points-review is not the gate. |
| DOC-FLOW | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-b3788114-qa-confirm.md` |
| QA twin | `/workspace/qa/2026-10-05__qa__PASS__core-admin-dashboard-devplan-b3788114.path` |

Paper-only qa-confirm. This file does **not** unlock Stories, code, CDK, spend, provision, or deploy.

## Active holds (this confirm)

- Do not invent passwords, AWS account IDs, or Turnstile / HMAC / SES keys in the plan, UI copy, fixtures, or examples.
- The process-global auth flood limiter stays paused until Chief Security explicitly approves; this slice is per-IP plus per-account only.
- No Stories, code, CDK, spend, provision, or deploy from this confirm alone.
- UI lanes wait for Chief UI/UX approval of UX1 plus a UI/UX QA PASS file.
- Deploy waits for Ivan's OK.
- H1 harden redeploy remains a separate track.
- Handshake is qa-confirm only (this file); points-review is not the gate.
- Binding Spec notes cited here are password-rules v2.3 `1a7b342c…`, lockout-window v5.5 `8a194eb9…`, and sign-in steps v1 `0a3db5f4…`. Any newer note hash voids the matching Spec QA / Security QA PASS and needs a fresh confirm before the plan may cite it.
- CEO retired hold phrasing (2:17pm ET): plan Active holds must stay plain English; this confirm does not use that retired status label.
- Nothing in this plan writes `OfferStatus.Withdrawn`; only writer is the backlog maker-withdraw Story on `api.core.dealoware.com` (outside admin scope).

## Retired hold phrase / tip MATCH

| Check | Result |
|-------|--------|
| Tip sha256 MATCH `b3788114…4a58` | **YES** |
| Exact banned status phrase (`Soft HOLD`) absent from plan body | **YES** (0 hits; python + rg) |
| Retired hold phrase count | **0** |

## Delta — UI/UX QA citation/record fixes (vs VOID tip `41928cbe…`)

Claimed: citation/record only; no scope change. Findings tip `8efb1d30…`; addendum tip `599d6a18…`; password-rules v2.3 binding wording; Step 9 UXR-B14/B15 cites; retired hold phrase count 0.

| # | Claim | Plan cite | On-disk check | Result |
|---|--------|-----------|---------------|--------|
| C1 | Findings cite `8efb1d30…` MATCH | Sources: `ux/2026-10-05__ux__findings__ux1-admin-requirements.md` sha256 `8efb1d30f9107844b635494718c8f2ba06d5f061d6c056191dc51d9637bfcfe4` | File sha256 **MATCH** full tip | **MET** |
| C2 | Addendum cite `599d6a18…` MATCH | Sources: `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` sha256 `599d6a182fe1f894352a91afe1774f9f6334599bb5c10eb10e07d0ac0509b799` | File sha256 **MATCH** full tip | **MET** |
| C3 | Password-rules v2.3 binding wording still correct | Sources + Step 2 UXR-A22 + Step 14 item 4: **v2.3** `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` (v2.2 superseded) | Spec note on disk sha256 **MATCH** `1a7b342c…`; plan cites as binding | **MET** |
| C4 | Step 9 UXR-B14/B15 cites correct | Step 9: UXR-B14 (edit fields + Expire confirm) + UXR-B15 (Save/Cancel / discard); cite addendum tip `599d6a18…`; verify checklist names B14/B15; E8 aligns with B14 Expire | Addendum Part B defines UXR-B14 and UXR-B15; plan cite tip MATCH; checklist items present | **MET** |
| C5 | Retired hold phrase count 0 | Status: retired hold phrase scrub remains; count stays 0 | 0 hits for banned phrase (case-insensitive) | **MET** |
| C6 | Carry-forward still MET (checklist 1–14; Step 14 6/6; Step 15 3/3; H4 primary as on `41928cbe…`; Steps 7/9 W/E unchanged) | See sections below | Spot-check + prior PASS carry | **MET** |

**Citation/record delta result:** **6/6 MET.**

## Carry — H4 primary cite (unchanged from `41928cbe…` PASS)

| # | Claim | Result |
|---|--------|--------|
| H4-1 | Primary A7-on-H4 = live retest path `331d2c63…` | **MET** (path MATCH) |
| H4-2 | Report `521e3bcc…` MATCH | **MET** |
| H4-3 | Register addendum `71bde564…` MATCH | **MET** |
| H4-4 | `11c35ee7…` Debug-off redeploy only; not sole closer | **MET** |
| H4-5 | Steps 7/9 W/E substance unchanged | **MET** (W1–W5 + no writer; E1–E12 + UpdatedAt in Step 7 only) |

## Carry — Steps 7/9 W/E (unchanged)

| # | Claim | Result |
|---|--------|--------|
| D1 | W1–W5 Withdrawn enum + domain + filter/badge | **MET** |
| D2 | No public writer endpoint in Step 7 | **MET** |
| D3 | E1–E12 SA §4 edit paths (no narrowing) | **MET** |
| D4 | UpdatedAt migration in Step 7 only; none for E1–E12 | **MET** |

## On-disk tip re-hash (binding cites used this confirm)

| Artifact | Path | Result |
|----------|------|--------|
| Plan | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` | **MATCH** `b3788114…4a58` |
| UX1 findings | `ux/2026-10-05__ux__findings__ux1-admin-requirements.md` | **MATCH** `8efb1d30…cfe4` |
| UX1 addendum | `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` | **MATCH** `599d6a18…b799` |
| Password-rules note | `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` | **MATCH** `1a7b342c…c521` (v2.3) |
| Lockout-window note | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` | **MATCH** `8a194eb9…87d3` (v5.5) |
| Sign-in steps note | `specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` | **MATCH** `0a3db5f4…4da4` (v1) |
| Answers | `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` | **MATCH** `a87e293b…8d15` |
| A16 decision | `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` | **MATCH** `bb0bcaa2…8d34` |
| A03 decision | `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md` | **MATCH** `0817b767…f3f0` |
| H4 live retest PASS path | `/workspace/qa/2026-10-05__qa__PASS__h4-live-retest-4911b08.path` | **MATCH** `331d2c63…8512` |
| H4 live retest report | `/workspace/qa/2026-10-05__qa__qa-report__h4-live-retest-4911b08.md` | **MATCH** `521e3bcc…3601` |
| H4 register addendum | `/workspace/qa/2026-10-05__qa__qa-report__h4-live-retest-4911b08-addendum-register.md` | **MATCH** `71bde564…09a4` |
| Invent-secrets scan | plan body | Zero invent passwords / AWS account IDs / keys |

## Security checklist pts 1–14 (spot-check + carry)

Carry-forward from VOID tip `41928cbe…` PASS (`4d1970ba…`). Citation/record delta does not change security substance of pts 1–14.

| Pt | Topic | Result |
|----|--------|--------|
| 1 | Host / principal / fail-closed | **MET** |
| 2 | Bootstrap + email/password (no invented password) | **MET** |
| 3 | Turnstile + mail interface | **MET** |
| 4 | TOTP / recovery | **MET** |
| 5 | Lockout / throttle / A03 | **MET** |
| 6 | Raw IP counters; audit HMAC | **MET** |
| 7 | SES via mail interface | **MET** |
| 8 | Edit audit / FieldPolicy | **MET** |
| 9 | Generic deny / no fabricated outcomes | **MET** |
| 10 | Soft-delete / CoreOwner toggle | **MET** |
| 11 | Lists / filter / Withdrawn badge | **MET** |
| 12 | Edit surface / 409 | **MET** |
| 13 | OUT / gate pack | **MET** (H4 closed with correct primary cite; Withdrawn writer OUT) |
| 14 | Traceability + handshake SoR | **MET** |

**Checklist result:** **14/14 MET.**

## Step 14 security binds

| # | Bind | Plan | Result |
|---|------|------|--------|
| 1 | UX1-A16 two-step decision | Full tip `bb0bcaa2…8d34` + conditions 1–8 on Step 14 | **MET** |
| 2 | UX1-A03 generic copy on Step 14 | Full tip `0817b767…f3f0` | **MET** |
| 3 | Password answers | Tip `a87e293b…8d15` + confirm path; `f03a82c9…` void | **MET** |
| 4 | Password-rules Spec note v2.3 | Cite `1a7b342c…c521` as binding; v2.2 not current | **MET** |
| 5 | Lockout-window Spec note v5.5 | Cite `8a194eb9…87d3` as binding; v5.4/v5.3 not current | **MET** |
| 6 | Sign-in steps Spec note v1 | Cite `0a3db5f4…4da4` on Step 14; A16 kept as Security decision | **MET** |

**Step 14 result:** **6/6 MET.**

## Step 15 security binds

| Bind | Plan | Result |
|------|------|--------|
| HMAC IP prefix only; no raw IP | Viewer shows HMAC IP prefix only | **MET** |
| No secrets / tokens | Nothing secret displayed | **MET** |
| admin.core only | Viewer on `admin.core.dealoware.com` only | **MET** |

**Step 15 result:** **3/3 MET.**

## Standing

| Item | Status |
|------|--------|
| Handshake | qa-confirm only |
| PoC | **$0** |
| Build unlock | **No** |
| Retired hold phrase in this confirm | **Absent** (0) |
| No invented password values | Held |
| Flood limiter | Withdrawn until Chief Security approves |
| A7-on-H4 primary = live retest `331d2c63…`; `11c35ee7…` not sole closer | Held |
| Priors overwritten? | **No** — frozen + `bd814d40` + `41928cbe` confirms left untouched |

## Verdict

**PASS.** Tip `b3788114b4ab890a8117158807773d3e5f8fe84bec4ee0f2f0f0d5df03014a58` MATCH. Retired hold phrase absent (0). UI/UX citation/record delta 6/6. H4 primary cite carry MET. Steps 7/9 W/E carry MET. Checklist 14/14. Step 14 binds 6/6. Step 15 binds 3/3. Prior tip `41928cbe…` VOID for current plan tip; its confirm `4d1970ba…`, `bd814d40` confirm `bed27d57…`, and frozen scrub confirm `5d7e7fca…` were **not** edited. PoC **$0**. Not a build unlock. No GAPs.
