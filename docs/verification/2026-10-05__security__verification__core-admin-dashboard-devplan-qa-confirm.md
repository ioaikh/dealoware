# Security QA — Dev Plan · Core admin dashboard (qa-confirm)

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~8:39pm ET) |
| Verdict | **PASS** — Soft HOLD status phrase absent; checklist pts 1–14 MET; Step 14 binds 6/6 MET; Step 15 binds 3/3 MET; wording-only scrub vs prior tip |
| Asked by | Chief Dev Planner (Soft HOLD wording-only scrub reconfirm; voids PASS on `3385872a…`) |
| Checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` (pts 1–14) |
| Plan tip | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` |
| Plan tip sha256 | `21535e6684ec347361a3f4fd0521a7928392428b7e9c2ce7c8355c2553808cd8` — **MATCH** (re-hashed with `sha256sum` before scoring and again immediately before this write) |
| Prior tips VOID | `3385872a881095b8b44a90a4148f5faf87594c39fd742897f089e1425b93caff` (prior PASS) **VOID**; also VOID: `1c173e98…`, `970b453a…`, `abfa672a…`, `5fefb550…` |
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
- CEO Soft HOLD phrasing retired (2:17pm ET): plan Active holds must stay plain English; this confirm does not use Soft HOLD wording.

## Soft HOLD scrub check (this tip)

| Check | Result |
|-------|--------|
| Tip sha256 MATCH `21535e66…8cd8` | **YES** |
| Exact status phrase Soft HOLD / soft HOLD / SOFT HOLD absent from plan body | **YES** (no hits) |
| Active holds header uses plain English | **YES** (lines 12–21: "Active holds … plain English; real gates unchanged") |
| Word/phrase repeated >5× in a row | **NONE** |
| Steps / scope / binds / security tip cites changed vs prior PASS claim | **No** — CDP wording-only claim; Step 14/15 bind tips and checklist security content still present and match prior PASS scoring |

**OBS (non-blocking):** Sources / Cost cite historical finance artifact **filenames** that contain the substring `soft-hold` (`finance/…__core-admin-soft-hold-ses-turnstile.md`, Finance QA twin). Those are path strings to existing estimate files, not Soft HOLD Active-holds status language. Not a bounce.

## Sources checked (re-hashed)

| Source | Path | Result |
|--------|------|--------|
| Dev Plan checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` | Binding pts 1–14 |
| Plan tip (re-hashed) | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` | sha256 **MATCH** `21535e66…8cd8`; Steps 1–15 present (617 lines) |
| Main Spec v2.2 | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | On-disk sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — MATCH plan cite |
| UX1-A16 decision | `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` | On-disk sha256 `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34` — MATCH plan cite |
| UX1-A03 decision | `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md` | On-disk sha256 `0817b7676a0103d606197c07fb6ac48782c12fbd363304637e5340b86af6d3f0` — MATCH plan cite |
| Password answers | `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` | On-disk sha256 `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` — MATCH; `f03a82c9…` marked void |
| Password-rules note | `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` | On-disk sha256 `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` (v2.3) — MATCH |
| Lockout-window note | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` | On-disk sha256 `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` (v5.5) — MATCH |
| Sign-in steps note | `specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` | On-disk sha256 `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` — MATCH |
| Invent-secrets scan | plan body | Zero invent passwords / AWS account IDs / keys |

## Independent re-score (pts 1–14 vs plan body)

Same security substance as prior PASS on `3385872a…` (now VOID). Soft HOLD scrub did not alter steps, scope, mappings, or binds. Spot-checked Locked §3, Steps 1–15, Explicit OUT, Cost/critical, Done-list, and Active holds (plain English).

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
| 13 | OUT / gate pack | **MET** |
| 14 | Traceability + handshake | **MET** |

**Checklist score:** **14/14 MET.**

## Step 14 security binds

| # | Bind | Required | Plan finding (tip `21535e66…`) | Score |
|---|------|----------|--------------------------------|-------|
| 1 | UX1-A16 two-step decision | Full tip `bb0bcaa2…8d34` + conditions 1–8 on Step 14 | Item 1 (line 417): path + full tip; conditions 1–8 by number + short label | **MET** |
| 2 | UX1-A03 generic copy on Step 14 | Full tip `0817b767…f3f0` | Item 2 (line 426): path + full tip; generic copy rules | **MET** |
| 3 | Password answers + confirm path | Full tip `a87e293b…8d15`; `f03a82c9…` void | Item 3 (line 427): only current tip; void prior; confirm path | **MET** |
| 4 | Password-rules tip v2.3 (binding) | Cite `1a7b342c…c521`; older void | Item 4 (line 428): full v2.3 tip; v2.2 superseded | **MET** |
| 5 | Lockout-window tip v5.5 (binding) | Cite `8a194eb9…87d3`; older void | Item 5 (line 429): full v5.5 tip; v5.4/v5.3 void | **MET** |
| 6 | Sign-in steps Spec note v1 | Cite `0a3db5f4…4da4` on Step 14 | Item 6 (line 430): full tip; Spec bind; A16 kept as Security decision | **MET** |

**Step 14 result:** **6/6 MET.**

## Step 15 security binds

| Bind | Required | Plan finding | Score |
|------|----------|--------------|-------|
| HMAC IP prefix only; no raw IP | Viewer shows HMAC IP prefix only | Line 463 + verify line 470 | **MET** |
| No secrets / tokens | Nothing secret displayed | Line 463 + verify line 470 | **MET** |
| admin.core only | Viewer on `admin.core.dealoware.com` only | Line 463 + verify line 470 | **MET** |

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
| Soft HOLD status phrase absent; Active holds plain English | Held |
| A7 until H4; deploy until Ivan OK; no Stories/code from this confirm | Held |
| Handshake = qa-confirm only; PoC $0; not a build unlock | Held |

## Gaps / OBS

**Gaps (blocking):** none.

**OBS (non-blocking):**

1. Prior PASS on tip `3385872a…` is **VOID**; this tip `21535e66…` is the only current Dev Plan Security qa-confirm tip.
2. Finance estimate / Finance QA **filenames** still contain substring `soft-hold` where the plan cites those paths — historical artifact names, not Soft HOLD status language.
3. UX addendum / findings hashes "at amend" remain UI sources outside this Security scope; not re-hashed here.
4. Sign-in Spec QA Active holds may still note a non-blocking sibling pointer rename inside the sign-in note; Dev Plan cites lockout v5.5 correctly.

**Invent scan:** zero invented passwords, AWS account IDs, ARNs, or Turnstile / HMAC / SES keys; no raw IPv4 in plan body.

## Handshake status

**PASS** on tip `21535e6684ec347361a3f4fd0521a7928392428b7e9c2ce7c8355c2553808cd8` (**MATCH**). Soft HOLD status phrase **absent**. Checklist **14/14 MET**. Step 14 binds **6/6 MET**; Step 15 binds **3/3 MET**. Prior PASS on `3385872a…` (and earlier `1c173e98…` / `970b453a…`) **VOID**. This clears the Security qa-confirm handshake for the Soft HOLD wording-only scrub tip. Next gate after Dev Plan QA PASS: A6 Test design via Chief QA before any Stories or code. Not a build unlock. PoC **$0**. Flood limiter paused, A7 waits for H4, deploy waits for Ivan, H1 separate.
