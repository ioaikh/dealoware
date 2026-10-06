# Security QA — Dev Plan · Core admin dashboard (qa-confirm) — tip `41928cbe…`

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~8:50pm ET) |
| Verdict | **PASS** — H4 cite bounce fix MET; retired hold phrase absent (0); checklist pts 1–14 MET; Step 14 binds 6/6 MET; Step 15 binds 3/3 MET; Steps 7/9 W/E substance unchanged from `bd814d40…` PASS |
| Asked by | Chief Dev Planner (H4 cite bounce fix; voids PASS on tip `bd814d40…` / evidence sha256 `bed27d57…` for current tip) |
| Checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` (pts 1–14) |
| Plan tip | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` |
| Plan tip sha256 | `41928cbeab5823849f9b4eabd91a62ad75150a8a1d9fd97c1a5ef3cea7c48d8c` — **MATCH** (re-hashed with `sha256sum` before scoring and again immediately before this write) |
| Prior tips VOID | `bd814d401d31e8272473c1da64f1ffc22191a82e1ac299c0b452deec4fc9479d` (Steps 7/9 PASS; H4 primary cite was wrong) **VOID** for current tip; also VOID: `21535e66…`, `3385872a…`, `1c173e98…`, `970b453a…`, `abfa672a…`, `5fefb550…` |
| Frozen / prior confirms (untouched) | (1) `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` — frozen sha256 `5d7e7fca8dca7fb4cef71a52b5cecd95b9bb400a1641ff16686851042d9923ac` (covers VOID tip `21535e66…` only). (2) `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-bd814d40-qa-confirm.md` — left untouched; sha256 `bed27d5740333185b7c63f3f31bc50c309f59d218782f11f36bab2068ba03d5c` (VOID for current tip). |
| Host | `admin.core.dealoware.com` only |
| Principal | **CoreOwner** = single system superadmin `io@aiknowhow.com` |
| PoC | **$0** |
| Handshake | **qa-confirm only** (this file). Points-review is not the gate. |
| DOC-FLOW | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-41928cbe-qa-confirm.md` |
| QA twin | `/workspace/qa/2026-10-05__qa__PASS__core-admin-dashboard-devplan-41928cbe.path` |

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

## Retired hold phrase / repetition check (this tip)

| Check | Result |
|-------|--------|
| Tip sha256 MATCH `41928cbe…8cd8` | **YES** |
| Exact banned status phrase (`Soft HOLD`) absent from plan body | **YES** (0 hits; python + rg) |
| Active holds header uses plain English | **YES** |
| Word/phrase repeated >5× in a row | **NONE** |
| Frozen / prior tip confirms overwritten? | **NO** — `5d7e7fca…` and `bed27d57…` left untouched |

**OBS (non-blocking):** Sources / Cost may still cite historical finance artifact **filenames** containing the substring `soft-hold`. Those are path strings to existing estimate files, not Active-holds status language. Not a bounce.

## Delta — H4 cite bounce fix (vs VOID tip `bd814d40…`)

| # | Claim | Plan finding (tip `41928cbe…`) | On-disk verify | Score |
|---|-------|--------------------------------|----------------|-------|
| H4-1 | Primary A7-on-H4 closed evidence = Chief QA live retest PASS path | Closed gates: `/workspace/qa/2026-10-05__qa__PASS__h4-live-retest-4911b08.path` (`331d2c63…`) | Path file sha256 **MATCH** `331d2c634b322185bc3bcaebcd45e8fc8af13fc53190e97c742e441f75108512`; content points to report + register addendum | **MET** |
| H4-2 | Report cite MATCH | Plan cites report `…h4-live-retest-4911b08.md` (`521e3bcc…`) | On-disk sha256 **MATCH** `521e3bccff9e1ff4ddb8573f287966558db105b2c3b35a977b80eb9947983601`; verdict **PASS** (token + register 429 + Retry-After) | **MET** |
| H4-3 | Register addendum cite MATCH | Plan cites addendum `…h4-live-retest-4911b08-addendum-register.md` (`71bde564…`) | On-disk sha256 **MATCH** `71bde564610390adea474edb7858bf6d2be8b2d3f15c43edb58d5e2dc7a309a4`; file exists | **MET** |
| H4-4 | `11c35ee7…` is Debug-off redeploy only; does **not** alone close H4 | Status / Closed gates / Sources / Active-holds table: "Separate cite only" / "does not by itself close H4" / "cited separately" | Redeploy path exists (`…devops-qa-PASS__h4-redeploy-11c35ee7.path`); subject = Debug-off redeploy tip `11c35ee7…`; plan never treats it as sole A7 closer | **MET** |
| H4-5 | Steps 7/9 W/E substance unchanged from `bd814d40…` PASS | W1–W5 + no writer; E1–E12 + UpdatedAt in Step 7 only | Spot-check: W1–W5, E1, E12, "No writer in Step 7", UpdatedAt all present; same Product/coverage shape as prior PASS | **MET** |

**H4 delta result:** **5/5 MET.** (This is the only claimed tip change vs `bd814d40…`.)

## Carry-forward — Steps 7/9 Product/CD coverage (unchanged substance)

| # | Claim | Score |
|---|-------|-------|
| D1 | W1–W5 Withdrawn enum + domain + filter/badge | **MET** (carry) |
| D2 | No public writer endpoint for Withdrawn | **MET** (carry) |
| D3 | E1–E12 SA §4 edit paths (no narrowing) | **MET** (carry) |
| D4 | UpdatedAt migration in Step 7 only | **MET** (carry) |

## Closed gates / Active holds refresh

| # | Claim | Plan finding | Score |
|---|-------|--------------|-------|
| H1 | A6 Test design PASS closed | Closed gates: design tip `8df392554ed887322bd7dc3f6633933bca26209c3bcfbb29321bf055f89814c9` on-disk MATCH | **MET** |
| H2 | A7-on-H4 closed with **live retest** as primary | Primary = path `331d2c63…` + report `521e3bcc…` + addendum `71bde564…`; `11c35ee7…` separate only | **MET** |
| H3 | UI / deploy / secrets / H1 / flood limiter holds stay | Active holds: UI lanes, deploy Ivan OK, H1 separate, flood limiter paused, no invent secrets, no Withdrawn writer | **MET** |

**Holds refresh result:** **3/3 MET.**

## Sources checked (re-hashed)

| Source | Path | Result |
|--------|------|--------|
| Dev Plan checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` | Binding pts 1–14 |
| Plan tip (re-hashed) | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` | sha256 **MATCH** `41928cbe…8cd8` |
| UX1-A16 decision | `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` | On-disk sha256 `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34` — MATCH |
| UX1-A03 decision | `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md` | On-disk sha256 `0817b7676a0103d606197c07fb6ac48782c12fbd363304637e5340b86af6d3f0` — MATCH |
| Password answers | `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` | On-disk sha256 `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` — MATCH; `f03a82c9…` void |
| Password-rules note | `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` | On-disk sha256 `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` (v2.3) — MATCH |
| Lockout-window note | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` | On-disk sha256 `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` (v5.5) — MATCH |
| Sign-in steps note | `specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` | On-disk sha256 `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` — MATCH |
| A6 Test design | `qa/2026-10-05__qa__test-design__core-admin-dashboard.md` | On-disk sha256 `8df39255…` — MATCH |
| H4 live retest PASS path | `/workspace/qa/2026-10-05__qa__PASS__h4-live-retest-4911b08.path` | sha256 **MATCH** `331d2c63…` |
| H4 live retest report | `/workspace/qa/2026-10-05__qa__qa-report__h4-live-retest-4911b08.md` | sha256 **MATCH** `521e3bcc…` |
| H4 register addendum | `/workspace/qa/2026-10-05__qa__qa-report__h4-live-retest-4911b08-addendum-register.md` | sha256 **MATCH** `71bde564…` |
| H4 Debug-off redeploy (separate) | `/workspace/qa/2026-10-05__qa__devops-qa-PASS__h4-redeploy-11c35ee7.path` | Exists; tip `11c35ee7…`; not sole closer |
| Invent-secrets scan | plan body | Zero invent passwords / AWS account IDs / keys |

## Independent re-score (pts 1–14 vs plan body)

Carry-forward security substance from VOID tip `bd814d40…` PASS (`bed27d57…`). H4 cite fix corrects Closed-gates primary evidence without weakening pts 9/11/12/13. Spot-checked Locked §3, Steps 1–15, Explicit OUT, Cost/critical, Done-list, Active holds (plain English + Closed gates).

| # | Point | Score |
|---|-------|-------|
| 1 | Host + single CoreOwner gate | **MET** |
| 2 | Bootstrap + email/password (no invented password) | **MET** |
| 3 | TOTP required; no email OTP; hashed recovery | **MET** |
| 4 | Password reset = email link + 2FA | **MET** |
| 5 | Turnstile only + lockout/rate + session | **MET** |
| 6 | Raw IP counters only; audit IP = keyed HMAC-SHA256 | **MET** |
| 7 | SES behind mail interface; no invented AWS | **MET** |
| 8 | Audit: auth + edit/delete; toggle not audited; `signin.second_factor_failed` | **MET** |
| 9 | Fail-closed FieldPolicy dual wall; no parallel ACL | **MET** |
| 10 | Confirm-before-delete + soft-delete cascades | **MET** |
| 11 | Lists / search / paging safe | **MET** |
| 12 | Edit write surface + concurrency | **MET** |
| 13 | OUT / gate pack | **MET** (H4 closed with correct primary cite; Withdrawn writer OUT) |
| 14 | Traceability + handshake | **MET** |

**Checklist score:** **14/14 MET.**

## Step 14 security binds

| # | Bind | Required | Plan finding (tip `41928cbe…`) | Score |
|---|------|----------|--------------------------------|-------|
| 1 | UX1-A16 two-step decision | Full tip `bb0bcaa2…8d34` + conditions 1–8 on Step 14 | Item 1: path + full tip; conditions 1–8 by number + short label | **MET** |
| 2 | UX1-A03 generic copy on Step 14 | Full tip `0817b767…f3f0` | Item 2: path + full tip; generic copy rules | **MET** |
| 3 | Password answers + confirm path | Full tip `a87e293b…8d15`; `f03a82c9…` void | Item 3: only current tip; void prior; confirm path | **MET** |
| 4 | Password-rules tip v2.3 (binding) | Cite `1a7b342c…c521`; older void | Item 4: full v2.3 tip; v2.2 superseded | **MET** |
| 5 | Lockout-window tip v5.5 (binding) | Cite `8a194eb9…87d3`; older void | Item 5: full v5.5 tip; v5.4/v5.3 void | **MET** |
| 6 | Sign-in steps Spec note v1 | Cite `0a3db5f4…4da4` on Step 14 | Item 6: full tip; Spec bind; A16 kept as Security decision | **MET** |

**Step 14 result:** **6/6 MET.**

## Step 15 security binds

| Bind | Required | Plan finding | Score |
|------|----------|--------------|-------|
| HMAC IP prefix only; no raw IP | Viewer shows HMAC IP prefix only | Step 15 body + verify checklist | **MET** |
| No secrets / tokens | Nothing secret displayed | Step 15 body + verify checklist | **MET** |
| admin.core only | Viewer on `admin.core.dealoware.com` only | Step 15 body + verify checklist | **MET** |

**Step 15 result:** **3/3 MET.**

## Guardrails spot-check

| Guardrail | Status |
|-----------|--------|
| Host `admin.core.dealoware.com` only | Held |
| CoreOwner + FieldPolicy; no parallel ACL | Held |
| Process-global auth flood limiter paused | Held |
| Raw IP counters only; never audit/logs/metrics | Held |
| Auth audit IP = keyed HMAC-SHA256; viewer prefix only | Held |
| Turnstile only; other CAPTCHA OUT | Held |
| SES behind mail interface; no AWS IDs | Held |
| No invented password values | Held |
| Spec cites match on-disk tips (v2.3 / v5.5 / sign-in `0a3db5f4…`) | Held |
| Retired hold phrase absent; Active holds plain English | Held |
| No public/admin Withdrawn writer in this plan | Held |
| A7-on-H4 primary = live retest `331d2c63…`; `11c35ee7…` not sole closer | Held |
| Frozen / prior tip confirms left untouched | Held |

## Verdict

**PASS.** Tip `41928cbeab5823849f9b4eabd91a62ad75150a8a1d9fd97c1a5ef3cea7c48d8c` MATCH. Retired hold phrase absent (0). H4 cite bounce fix 5/5. Steps 7/9 W/E carry MET. Holds refresh 3/3. Checklist 14/14. Step 14 binds 6/6. Step 15 binds 3/3. Prior tip `bd814d40…` VOID for current plan tip; its confirm `bed27d57…` and frozen scrub confirm `5d7e7fca…` were **not** edited. PoC **$0**. Not a build unlock. No GAPs.
