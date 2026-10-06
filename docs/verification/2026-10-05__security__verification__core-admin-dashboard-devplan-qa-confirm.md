# Security QA — Dev Plan · Core admin dashboard (qa-confirm)

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~8:33pm ET) |
| Verdict | **PASS** — checklist pts 1–14 MET; Step 14 binds 6/6 MET; Step 15 binds 3/3 MET; final Spec-cite refresh MET |
| Asked by | Chief Dev Planner (final tip reconfirm; voids PASS on `1c173e98…` and interim `970b453a…`) |
| Checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` (pts 1–14) |
| Plan tip | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` |
| Plan tip sha256 | `3385872a881095b8b44a90a4148f5faf87594c39fd742897f089e1425b93caff` — **MATCH** (re-hashed with `sha256sum` before scoring and again immediately before this write) |
| Prior tips VOID | `1c173e98f9c3babd05d4a423e12dda5a2e5bac9bc573d39215dd379c46639670` (prior PASS) **VOID**; interim `970b453a…` **VOID**; earlier `abfa672a…` / `5fefb550…` also VOID |
| Host | `admin.core.dealoware.com` only |
| Principal | **CoreOwner** = single system superadmin `io@aiknowhow.com` |
| PoC | **$0** |
| Handshake | **qa-confirm only** (this file). Points-review is not the gate. |
| DOC-FLOW | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` |
| QA twin | `/workspace/qa/2026-10-05__qa__qa-report__core-admin-dashboard-devplan-qa-confirm.md` |

Paper-only qa-confirm. This file does **not** unlock Stories, code, CDK, spend, provision, or deploy.

## Active holds

- Do not invent passwords, AWS account IDs, or Turnstile / HMAC / SES keys in the plan, UI copy, fixtures, or examples.
- The process-global auth flood limiter stays paused until Chief Security explicitly approves; this slice is per-IP plus per-account only.
- No Stories, code, CDK, spend, provision, or deploy from this confirm alone.
- A7 stays paused until H4 live PASS.
- Deploy waits for Ivan's OK.
- H1 harden redeploy remains a separate track.
- Handshake is qa-confirm only (this file); points-review is not the gate.
- Binding Spec notes cited here are password-rules v2.3 `1a7b342c…`, lockout-window v5.5 `8a194eb9…`, and sign-in steps v1 `0a3db5f4…`. Any newer note hash voids the matching Spec QA / Security QA PASS and needs a fresh confirm before the plan may cite it.

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Dev Plan checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` | Binding pts 1–14 |
| Plan tip (re-hashed) | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` | sha256 **MATCH** `3385872a…caff`; Steps 1–15 present (617 lines) |
| Main Spec v2.2 | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | On-disk sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — MATCH plan cite |
| UX1-A16 decision | `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` | On-disk sha256 `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34` — MATCH plan cite |
| UX1-A03 decision | `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md` | On-disk sha256 `0817b7676a0103d606197c07fb6ac48782c12fbd363304637e5340b86af6d3f0` — MATCH plan cite |
| Password answers | `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` | On-disk sha256 `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` — MATCH; `f03a82c9…` marked void |
| Password answers confirm | `verification/2026-10-05__security__verification__admin-core-auth-ui-security-answers-confirm.md` | Security QA **PASS** on `a87e293b…` |
| Password-rules note | `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` | On-disk sha256 `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` (v2.3) — MATCH; promoted binding |
| Password-rules Security confirm | `verification/2026-10-05__security__verification__core-admin-password-rules-note-confirm.md` | Security QA **PASS** on v2.3 `1a7b342c…` |
| Password-rules Spec QA | `verification/2026-10-05__spec__verification__core-admin-password-rules-note.md` | Spec QA **PASS** on v2.3 `1a7b342c…` (~8:31pm ET) — plan cite accurate |
| Lockout-window note | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` | On-disk sha256 `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` (v5.5) — MATCH; promoted binding; v5.4/v5.3 void |
| Lockout-window Security pt 5 confirm | `verification/2026-10-05__security__verification__core-admin-lockout-window-note-pt5-confirm.md` | Security QA **PASS** (point 5) on v5.5 `8a194eb9…` |
| Lockout-window Spec QA | `verification/2026-10-05__spec__verification__core-admin-lockout-window-note.md` | Spec QA **PASS** on v5.5 `8a194eb9…` (~8:30pm ET) — plan cite accurate |
| Sign-in steps note | `specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` | On-disk sha256 `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` — MATCH |
| Sign-in steps Security confirm | `verification/2026-10-05__security__verification__core-admin-signin-steps-note-confirm.md` | Security QA **PASS** on `0a3db5f4…` |
| Sign-in steps Spec QA | `verification/2026-10-05__spec__verification__core-admin-signin-steps-note.md` | Spec QA **PASS** on `0a3db5f4…` — plan cite accurate |
| Invent-secrets scan | plan body | Zero invent passwords / AWS account IDs / keys |

## Delta vs prior tip `1c173e98…` (required MET)

| # | Required change | Plan finding (tip `3385872a…`) | Score |
|---|-----------------|-------------------------------|-------|
| 1 | Lockout v5.5 `8a194eb9…` promoted binding; older void | Sources line 53; Step 14 item 5 (line 429); Active holds line 20; header Status. v5.4 `c346f0b1…` / v5.3 void stated. Spec QA + Security QA pt5 PASS on this tip exist on disk. | **MET** |
| 2 | Password-rules v2.3 `1a7b342c…` promoted binding; older void | Sources line 52; Step 14 item 4 (line 428); Active holds line 20. v2.2 superseded. Spec QA + Security QA PASS on this tip exist on disk. | **MET** |
| 3 | Sign-in steps Spec note v1 `0a3db5f4…` on Step 14 (Spec bind; A16 stays Security decision) | Step 14 item 6 (line 430) + verify row line 448; Sources line 54. States Spec bind for A16 conditions 1–8; A16 tip `bb0bcaa2…` kept as Security decision cite (item 1). | **MET** |
| 4 | Step 5 A16 two-step cross-cite; conditions 1–3, 5, 7 as server machinery | Step 5 UI deliverables (line 229): cites A16 full tip; Spec bind `0a3db5f4…`; conditions **1–3, 5, 7** named as server machinery built in Step 5; Step 14 builds screens on top. | **MET** |
| 5 | Step 8 adds `signin.second_factor_failed` | Auth events row (line 283) includes `signin.second_factor_failed` with HMAC IP prefix, UX1-A16 condition 6, sign-in steps `0a3db5f4…`, lockout v5.5 `8a194eb9…` §8.8. Verify row line 294. | **MET** |
| 6 | Answers tip `a87e293b…` explicit; `f03a82c9…` void | Sources line 50; Step 14 item 3 (line 427): only current tip `a87e293b…`; `f03a82c9…` void and must not be cited; confirm path present. | **MET** |
| 7 | Prior Step 14 binds + Step 15 HMAC IP still MET | A16 `bb0bcaa2…` + cond 1–8; A03 `0817b767…`; answers `a87e293b…`; Step 15 HMAC IP prefix — all still present (see tables below). | **MET** |

**Delta score:** **7/7 MET.**

## Independent re-score (pts 1–14 vs plan body)

Scored against Locked §3, Steps 1–15, Explicit OUT §7, Cost/critical §8, Done-list §9, and the header Active holds list. Steps 1–13 body weave unchanged in substance from the prior PASS tip; spot-checked again with the new Spec cites folded in.

| # | Point | Score | Plan cites | Evidence notes |
|---|-------|-------|------------|----------------|
| 1 | Host + single CoreOwner gate | **MET** | Header; Step 1; Step 11; Step 12 OUT; Step 14 A16 cond 8; Step 15 | Admin only `admin.core.dealoware.com`; single `io@aiknowhow.com` → CoreOwner; no platform-admin / multi-admin / human-user list / Participant UI-as-admin. |
| 2 | Bootstrap + email/password (no invented password) | **MET** | Step 2; Step 12; Step 14 binds 3–4; Locked #2, #14 | Single-use timed OOB bootstrap; password only via link; TOTP before first session; no password values; SSO/IdP OUT. Answers + password-rules v2.3 cited. |
| 3 | TOTP required; no email OTP; hashed recovery | **MET** | Step 3; Step 14 verify | TOTP every login; email OTP path absent; recovery show-once, hashed, single-use. |
| 4 | Password reset = email link + 2FA | **MET** | Step 4 | Both factors; link-only mail; no skip-2FA path. |
| 5 | Turnstile only + lockout/rate + session | **MET** | Step 5; Step 12; Step 14 binds 1–2, 5–6; Locked #3, #6 | Turnstile only; Spec lockout numbers; session 30m/8h; flood limiter paused; A16 server machinery 1–3,5,7 on Step 5; lockout v5.5 + sign-in steps binds. |
| 6 | Raw IP counters only; audit IP = keyed HMAC-SHA256 | **MET** | Steps 5, 8, 15; Locked #5 | Raw IP only in short-lived counters; audit IP keyed HMAC-SHA256; viewer HMAC IP prefix only. |
| 7 | SES behind mail interface; no invented AWS | **MET** | Step 6; Locked #4, #13; Cost/critical | Mail interface; no SES SDK in Core; no AWS IDs; CFO estimate cite only. |
| 8 | Audit: auth + edit/delete; toggle not audited | **MET** | Steps 7–10, 15; Locked #10 | Append-only; auth + edit/delete + cascades; `signin.second_factor_failed` added; soft-deleted toggle not audited; same-txn pairing. |
| 9 | Fail-closed FieldPolicy dual wall; no parallel ACL | **MET** | Steps 1, 9, 11, 12, 15; Locked #8 | FieldPolicy for CoreOwner; deny on missing session/TOTP; no parallel ACL. |
| 10 | Confirm-before-delete + soft-delete cascades | **MET** | Step 10; Locked #9 | UI modal + single-use 5-min confirm token; blind DELETE rejected; `DeletedAt`; cascades per Spec §11; settlement OUT. |
| 11 | Lists / search / paging safe | **MET** | Steps 7, 15; Locked #7 | Server-side sort/filter/paging 50/200; parameterized name search; FieldPolicy on results. |
| 12 | Edit write surface + concurrency | **MET** | Step 9; Locked #8 | FieldPolicy-only writes; before/after audit; optimistic 409; no secrets in edit/audit. |
| 13 | OUT / gate pack | **MET** | Step 12; Explicit OUT §7; Header Active holds; Cost/critical | Stories/code/CDK/spend/deploy paused; A7 until H4; deploy until Ivan OK; H1 separate; flood limiter paused; full OUT pack; PoC $0. |
| 14 | Traceability + handshake | **MET** | §1 Sources; §6; Handshake note; Done-list §9 | Spec v2.2 tip + Spec Security PASS 15/15 + SA Security PASS 14/14 + Dev Plan checklist cited; handshake = qa-confirm only; not a build unlock. |

**Checklist score:** **14/14 MET.**

## Step 14 security binds

| # | Bind | Required | Plan finding (tip `3385872a…`) | Score |
|---|------|----------|-------------------------------|-------|
| 1 | UX1-A16 two-step decision | Full tip `bb0bcaa2…8d34` + conditions 1–8 on Step 14 | Item 1 (line 417): path + full tip; two-step UI; conditions 1–8 by number + short label. Verify row line 443. Also Sources line 48. | **MET** |
| 2 | UX1-A03 generic copy on Step 14 | Full tip `0817b767…f3f0` | Item 2 (line 426): path + full tip; generic copy; no Retry-After / duration / attempt / "locked"; UI shows server text. Verify line 444. | **MET** |
| 3 | Password answers + confirm path | Full tip `a87e293b…8d15`; `f03a82c9…` void | Item 3 (line 427): path + full tip; void prior; confirm path cited. Verify line 445. | **MET** |
| 4 | Password-rules tip v2.3 (binding) | Cite `1a7b342c…c521`; older void | Item 4 (line 428): full v2.3 tip; v2.2 superseded; Spec QA + Security QA PASS paths cited (accurate on disk). Verify line 446. | **MET** |
| 5 | Lockout-window tip v5.5 (binding) | Cite `8a194eb9…87d3`; older void | Item 5 (line 429): full v5.5 tip; v5.4/v5.3 void; Spec QA + Security QA pt5 PASS paths cited (accurate on disk). Verify line 447. | **MET** |
| 6 | Sign-in steps Spec note v1 | Cite `0a3db5f4…4da4` on Step 14; A16 stays Security decision | Item 6 (line 430): full tip; Spec bind for A16 conditions 1–8; A16 `bb0bcaa2…` kept as Security decision; Security QA + Spec QA confirm paths cited. Verify line 448. | **MET** |

**Step 14 result:** **6/6 MET.**

## Step 15 security binds

| Bind | Required | Plan finding | Score |
|------|----------|--------------|-------|
| HMAC IP prefix only; no raw IP | Viewer shows HMAC IP prefix only | Line 463: auth audit IP exists only as an **HMAC IP prefix** (keyed HMAC-SHA256, short prefix only — never raw IP, secrets, or the HMAC key). Verify line 470. | **MET** |
| No secrets / tokens | Nothing secret displayed | Line 463 + verify line 470: no secret, password, or HMAC key; denied fields never shown; read-only. | **MET** |
| admin.core only | Viewer on `admin.core.dealoware.com` only | Line 463: viewer on `admin.core.dealoware.com` only; verify line 470 "admin.core host only". | **MET** |

**Step 15 result:** **3/3 MET.**

## Guardrails spot-check

| Guardrail | Status | Cite |
|-----------|--------|------|
| Host `admin.core.dealoware.com` only | Held | Header; Steps 1, 12, 14 (A16 cond 8), 15 |
| CoreOwner + FieldPolicy; no parallel ACL | Held | Steps 1, 9, 11, 12 |
| Process-global auth flood limiter paused | Held | Step 5; Locked #6; Step 12; §7 |
| Raw IP counters only; never audit/logs/metrics | Held | Steps 5, 8, 15 |
| Auth audit IP = keyed HMAC-SHA256; viewer prefix only; no key value | Held | Steps 8, 15 |
| Turnstile only; other CAPTCHA OUT | Held | Steps 5, 12, 14 |
| SES behind mail interface; no AWS IDs | Held | Step 6; Cost/critical |
| No invented password values | Held | Steps 2, 12, 14; scan clean |
| Spec QA PASS cites match files on disk (v2.3 / v5.5 / sign-in `0a3db5f4…`) | Held | Sources lines 52–54; Step 14 items 4–6; Spec QA files verified |
| A7 until H4; deploy until Ivan OK; no Stories/code from this confirm | Held | Step 12; Header Active holds |
| H1 harden redeploy separate | Held | Step 12; §7 |
| Handshake = qa-confirm only | Held — this file | Handshake note |
| PoC $0; not a build unlock | Held | Cost/critical; Header |

## Gaps / OBS

**Gaps (blocking):** none.

**OBS (non-blocking):**

1. Prior PASS on tip `1c173e98…` and any interim on `970b453a…` are **VOID**; this tip `3385872a…` is the only current Dev Plan Security qa-confirm tip.
2. Plan Active holds line 20 correctly states Spec QA PASS on lockout v5.5, password-rules v2.3, and sign-in steps `0a3db5f4…`; Spec QA files on disk confirm those PASS rows (~8:30–8:31pm ET). This closes the prior OBS that Spec QA was still pending.
3. Plan body still uses the retired hold wording in many cells (header Brief/Constraints, §1, §2, Steps, §6–§9). Holds are real and correctly stated in the plain **Active holds** list (lines 12–21). Suggest Chief Dev Planner swap remaining retired wording for plain Active holds language in a later edit (that edit would change the tip).
4. UX addendum / findings hashes "at amend" are UI sources outside this Security scope; not re-hashed here.
5. Sign-in Spec QA Active holds still note a non-blocking next-edit to rename lockout sibling pointers from v5.4 to v5.5 inside the sign-in note itself; the Dev Plan already cites lockout v5.5 correctly.

**Invent scan:** zero invented passwords, AWS account IDs, ARNs, or Turnstile / HMAC / SES keys; no raw IPv4 in plan body.

## Handshake status

**PASS** on tip `3385872a881095b8b44a90a4148f5faf87594c39fd742897f089e1425b93caff` (**MATCH**). Checklist **14/14 MET**. Step 14 binds **6/6 MET**; Step 15 binds **3/3 MET**. Final Spec-cite delta **7/7 MET**. Prior PASS on `1c173e98…` and interim `970b453a…` are VOID. This clears the Security qa-confirm handshake for Dev Plan QA on the final tip; Chief Security / Chief Dev Planner own the outbound. Next gate after Dev Plan QA PASS: A6 Test design via Chief QA before any Stories or code. Not a build unlock. PoC **$0**. Flood limiter paused, A7 waits for H4, deploy waits for Ivan, H1 separate.
