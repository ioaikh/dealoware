# Senior Security points-review — Dev Plan · Core admin dashboard (checklist pts 1–14)

| Field | Value |
|-------|--------|
| Status | **PASS** (14/14 MET, 0 GAP, 0 PARTIAL) — points-review **content only**. **This file is NOT Soft HOLD SoR.** Soft HOLD Dev Plan QA until Security QA Soft HOLD SoR qa-confirm CLEAR + Chief Security PASS/HOLD. Not build unlock. |
| Date | 2026-10-05 (~2:05pm ET) |
| Author | Dealoware Senior Security |
| Asked by | Chief Security → Senior Security (Dev Plan-step Security QA, pts 1–14) |
| Checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` (**ISSUED**, pts 1–14) |
| Plan tip | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` (mtime 1:59:39pm ET) |
| Plan tip sha256 | `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf` — **MATCH** (`sha256sum` before score) |
| Spec cite | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` v2.2 — sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` **MATCH**; Spec Security Soft HOLD SoR PASS 15/15 (`…-spec-qa-confirm.md`) |
| SA cite | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` + SA Security Soft HOLD SoR PASS 14/14 (`…-sa-qa-confirm.md`) |
| Host | `admin.core.dealoware.com` only · Principal **CoreOwner** = `io@aiknowhow.com` |
| PoC | **$0** |
| DOC-FLOW | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-points-review.md` |

## Verdict

**PASS — 14/14 MET.** Scored against the plan **body** (Steps 1–13 lock tables, verify checklists, §3 Locked decisions, Step 12 / §7 OUT, §8 Cost/critical, §9 Done-list), not the §6 self-map alone. Every point is scheduled as a step, gated, and has a named verify item. Plan numbers cross-checked against Spec v2.2 / SA Soft HOLD SoR (bootstrap ≤24h; reset link 1h; 5/15→30 account; 20/15→30 IP incl. reset-request; idle 30m / absolute 8h; confirm token 5 min; paging 50/200 offset/limit; 4 KiB snapshot truncation; `Withdrawn` first-class; 409 concurrency) — all match; no invented requirements.

Leak scan of plan tip: no password values, no AWS account IDs / region / SES identity / from-address, no Turnstile keys, no HMAC key value.

## Score table (pts 1–14)

| # | Point | Result | Body evidence (plan cites) |
|---|-------|--------|----------------------------|
| 1 | Host + single CoreOwner gate | **MET** | Step 1 (serve only `admin.core.dealoware.com`; Soft HOLD platform hosts; CoreOwner→`io@aiknowhow.com`; Soft HOLD multi-admin / operator ACL / human-user list / Participant UI-as-admin) + verify; Step 11 deny non-CoreOwner; Step 12 + §7 OUT; Locked #1–#2 |
| 2 | Bootstrap + email/password (no invent password) | **MET** | Step 2 table (single-use OOB link ≤24h; password set only via link; TOTP before first session; Soft HOLD password values in Stories/fixtures/README/chat/commit history — env/placeholders only; SSO/IdP OUT) + verify; Step 12 "password Forbidden"; Locked #14 |
| 3 | TOTP required; no email OTP; hashed recovery | **MET** | Step 3 (TOTP every post-enrollment login; email OTP fallback NOT allowed; recovery shown once / hashed / single-use) + verify item "email OTP path absent" + no plaintext recovery/TOTP secret in logs/UI/audit; §7 OUT |
| 4 | Password reset = email link + 2FA | **MET** | Step 4 (single-use 1h link **and** TOTP or unused recovery code; mail link only; Soft HOLD skip-2FA reset) + verify (blind/password-only reset absent; no password in mail; reset_complete audited) |
| 5 | Turnstile only + lockout/rate + session; no process-global flood limiter | **MET** | Step 5 (Turnstile only on login / reset / bootstrap password-set; Soft HOLD WAF CAPTCHA / reCAPTCHA / other; Soft HOLD Turnstile keys; 5/15→30 account; 20/15→30 IP 429 + same window on reset-request; **per-IP + per-account only**, process-global limiter Soft HOLD until Chief Security explicitly approves; server-side Postgres session + HttpOnly Secure SameSite=Strict; idle 30m / abs 8h; full re-auth) + verify; Step 2 Turnstile on bootstrap; Locked #3, #6; Step 12 / §7 OUT |
| 6 | Raw IP counters only; audit IP = keyed HMAC-SHA256 | **MET** | Step 5 Raw IP row (only short-lived counters expiring with 15/30-min windows; never audit/logs/metrics) + verify; Step 8 IP row (keyed HMAC-SHA256, server-side rotatable secret, + reason class only; Soft HOLD unkeyed hash / raw IP; Soft HOLD HMAC key value) + verify; Locked #5; Step 13 self-verify. *Nit (non-blocking):* §2 and §5 Spec §10 row say "hashed IP" — binding body (Steps 5/8) is explicit keyed HMAC with unkeyed OUT; recommend Senior Dev Planner reword to "keyed HMAC-SHA256 IP" on next touch |
| 7 | SES behind mail interface; Soft HOLD invent AWS | **MET** | Step 6 (SES behind mail interface port/adapter; Soft HOLD direct SES SDK / SES types in Core; Soft HOLD account IDs / region / identity / from-address / provision / spend; CFO cite Turnstile $0 / SES < $0.01/mo = estimate only) + verify (no SES PackageReference in Core path); Step 1 no AWS; §7 OUT "Direct AWS SES SDK"; §8 Cost/critical |
| 8 | Audit: auth + edit/delete; toggle not audited | **MET** | Step 8 (append-only insert-only; no update/delete from UI/API; login success/fail reason class, reset req/complete, TOTP enroll/change, recovery_code_use; every edit/delete + cascade rows w/ correlation id; toggle not audited + CoreOwner-only; same-txn → roll back; snapshots FieldPolicy-only, no secrets) + verify; Step 7 toggle note; Steps 9–10 edit/cascade audit; Step 9 no secrets in errors |
| 9 | Fail-closed FieldPolicy dual wall; no parallel ACL | **MET** | Step 1 FieldPolicy bind (Option A §3a/§3b cite, no rewrite; Soft HOLD parallel ACL / second matrix); Step 11 (missing/invalid/expired session or missing TOTP or non-CoreOwner → deny all admin routes; denied FieldClass → no write/no dump; Soft HOLD bypass routes) + verify; Step 9 fail-closed row; Locked #8 |
| 10 | Confirm-before-delete + soft-delete cascades | **MET** | Step 10 (UI modal type + identity + cascade summary; cancel → no mutation / no delete audit; API delete-intent → confirmToken single-use 5 min bound to actor + entity + cascade set; blind DELETE rejected; `DeletedAt` default; hard delete deferred; cascades offer / negotiation→offers / Artifact block-while-referenced / Participant→negotiations+offers, no auto Artifacts; settlement cascades OUT) + verify — matches Spec §9/§11, SA §3.4/§3.6 |
| 11 | Lists / search / paging safe | **MET** | Step 7 (all statuses incl. first-class `Withdrawn`; CoreOwner-only toggle; server-side sort/filter; offset/limit 50/200; Soft HOLD client-only full-table dump; case-insensitive contains on all four tables, parameterized; payloads never include denied fields; stats exclude soft-deleted; charts/warehouse OUT) + verify |
| 12 | Edit write surface + concurrency | **MET** | Step 9 (FieldPolicy-allowed writes only; before/after FieldPolicy-only audit; optimistic `Version`/ETag → 409 safe, no silent overwrite; Soft HOLD password/secret fields in forms/errors/snapshots) + verify |
| 13 | OUT / Soft HOLD / A7 gate pack | **MET** | Step 12 table + verify; §7 Explicit OUT; §4 Prerequisite Soft HOLDs; §8 Cost/critical. Covers Stories/build (until Dev Plan QA + Test design PASS), CDK/provision/spend, **deploy until Ivan OK**, **A7 until H4 live PASS** (no invented H4 steps), **H1 harden redeploy separate** until H1–H6(+H6b) PASS + CEO OK (no invented H1 steps), password / AWS IDs / process-global flood limiter Soft HOLD, platform admin / human-user list / Participant UI-as-admin / multi-admin / email OTP / WAF CAPTCHA / reCAPTCHA / SSO-IdP / settlement OUT, inbound connector after, PoC $0, LLM/API → COO → CEO |
| 14 | Traceability + handshake Soft HOLD SoR | **MET** | §1 Sources + §6 header cite Spec v2.2 full sha256, Spec Security Soft HOLD SoR PASS 15/15, SA Soft HOLD SoR PASS 14/14, this Dev Plan checklist; Status line, §6 handshake note, §9 Done-list (unchecked Soft HOLD SoR + Security QA boxes) + Next: Dev Plan QA must **not** PASS until qa-confirm CLEAR at `…-devplan-qa-confirm.md`; handshake = qa-confirm only; A6 Test design before Stories/code; not build unlock; PoC $0 |

**GAPs:** none. **PARTIALs:** none. **Nits (non-blocking):** pt 6 "hashed IP" wording in §2 / §5 (see row 6).

## Hard-check summary (body weave)

| Hard-check | Result | Where in body |
|-----------|--------|---------------|
| Turnstile only (no WAF CAPTCHA / reCAPTCHA) | MET | Steps 2, 5, 12; §7 |
| SES behind mail interface; Soft HOLD invent AWS IDs | MET | Steps 1, 6, 12; §7; §8 |
| Soft HOLD invent password values | MET | Steps 2, 4, 9, 12; §7; leak scan clean |
| Raw IP counters only; audit IP keyed HMAC-SHA256; unkeyed OUT | MET | Steps 5, 8, 13 |
| Soft HOLD process-global flood limiter (per-IP + per-account only) | MET | Step 5 row + verify; §4 prereqs; Step 12; §7 |
| Soft HOLD A7 until H4 live PASS | MET | §4 prereqs; Step 12; §7; §8; §10 header |
| Soft HOLD H1 harden redeploy separate | MET | §4 prereqs; Step 12; §7; §8 |
| Soft HOLD deploy until Ivan OK | MET | §4 prereqs; Step 12; §7; §8 |
| PoC $0 | MET | Header constraints; Step 6; §8 |

## Soft HOLDs (unchanged by this review)

Soft HOLD Dev Plan QA PASS until Security QA Soft HOLD SoR qa-confirm CLEAR + Chief Security PASS/HOLD · Soft HOLD invent Stories / code / CDK / provision / spend / build · Soft HOLD A7 until H4 live PASS · Soft HOLD deploy until Ivan OK · Soft HOLD harden redeploy (H1) separate · Soft HOLD invent password values / AWS account IDs / HMAC / Turnstile / SES secrets · Soft HOLD invent process-global auth flood limiter until Chief Security explicitly approves · A6 Test design → Chief QA before Stories/code.

## Cost/critical

No build, deploy, CDK, provision, or spend from this review. CFO Soft HOLD estimate (Turnstile $0/mo; SES < $0.01/mo assumed) cited as estimate only — not spend approval. PoC **$0**. Cost/critical → COO → CEO.

## Handshake next

1. Senior Security → Chief Security: PASS 14/14 + this path.
2. Handshake Soft HOLD SoR = **Security QA qa-confirm only** at `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md`. **Observation:** that file already exists (Security QA, ~2:01pm ET, PASS 14/14, scored plan tip directly at CPM ask, notes this points-review as absent). It was written before this points-review; Chief Security decides whether it stands or Security QA re-confirms against this file. Senior Security did **not** write or edit it.
3. Chief Security issues PASS/HOLD to Chief Dev Planner + CPM after Soft HOLD SoR qa-confirm.

## Not deploy / build approval

This points-review is **not** Soft HOLD SoR, **not** a Dev Plan QA PASS, **not** a build / A7 / deploy / H1 unlock, and **not** spend approval. Do not cite this file as Soft HOLD SoR. PoC **$0**.
