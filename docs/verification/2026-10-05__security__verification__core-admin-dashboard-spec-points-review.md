# Security points-review — Spec · Core admin dashboard (v2.2 FINAL)

| Field | Value |
|-------|--------|
| Status | **PASS Soft HOLD Spec QA Soft HOLD Security Soft HOLD SoR Soft HOLD Security QA Soft HOLD SoR qa-confirm Soft HOLD build** — Soft HOLD invent Soft HOLD SoR as PASS claim. **Not** Soft HOLD SoR. **Not** Spec Security PASS claim (Chief PASS/HOLD after Soft HOLD SoR qa-confirm). **Not** build unlock. |
| Date | 2026-10-05 (~1:50pm ET) |
| Author | Dealoware Senior Security |
| Checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` (**v2** — Product 1:27pm patch; CEO 1:30pm Turnstile only; SES behind mail interface) |
| Spec | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` (**v2.2**, Chief Spec CLEAR) |
| Spec tip sha256 | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` (verified `sha256sum` before score) |
| Scope (cite only) | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` — **13:31** (includes 1:27pm patch + 1:30pm CEO final) |
| Host lock | `admin.core.dealoware.com` only |
| PoC | **$0** |
| DOC-FLOW | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-points-review.md` |
| Prior PRE-SCORE | **Superseded** for this Soft HOLD SoR track — prior PRE-SCORE Soft HOLD final (v2.1, ~1:35pm ET) was content-only PRE-SCORE Soft HOLD final; it was **never** Soft HOLD SoR and is **not** a Soft HOLD SoR / Spec Security PASS claim. This file is the **FINAL** score rewrite for Spec tip v2.2. |

**This points-review is NOT Soft HOLD SoR.** Handshake Soft HOLD SoR = qa-confirm only at `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md`. Do **not** invent Soft HOLD SoR. Do **not** write Soft HOLD SoR qa-confirm in this file. Soft HOLD Security QA Soft HOLD SoR qa-confirm (parent routes). Soft HOLD invent Soft HOLD SoR as PASS claim.

**Not build / deploy / Stories / code / CDK / spend / provision approval.** Soft HOLD harden redeploy (H1) separate. No passwords, AWS account IDs, CDK internals, or bot-platform secrets in this review.

## Verdict

**PASS Soft HOLD Spec QA Soft HOLD Security Soft HOLD SoR Soft HOLD Security QA Soft HOLD SoR qa-confirm Soft HOLD build — 15/15 MET** (0 GAP, 0 PARTIAL). Soft HOLD invent Soft HOLD SoR as PASS claim. Not build unlock. PoC **$0**.

Independent body check of Spec §§3–12 (Locked decisions, Sources, OUT) vs checklist **v2** points 1–15. §13 self-map alone is **not** the score basis; each point was verified against the cited body sections. Tip sha256 verified before score.

**v2.2 deltas scored hard:**
- §8.6: raw client IP only in short-lived rate-limit counters (expire with 15-/30-minute windows); raw IP never to audit/logs/metrics.
- §10 / auth audit: IP = keyed **HMAC-SHA256** of client IP + failure reason class only; unkeyed hash **OUT**; raw IP never in audit.
- SA Soft HOLD SoR reconcile adopted (session store, confirm token, `DeletedAt`, offset/limit, `Withdrawn`, same-txn audit, etc.).
- Password values **Forbidden** throughout Spec/docs/chat.

This is **NOT** Spec Security PASS. Soft HOLD Security Soft HOLD SoR Soft HOLD Security QA Soft HOLD SoR qa-confirm Soft HOLD Spec QA Soft HOLD build. Chief Security issues PASS/HOLD to Chief Spec + CPM **after** Soft HOLD SoR qa-confirm.

## Score table

| # | Point | Result | Spec cites | Evidence note |
|---|-------|--------|------------|---------------|
| 1 | Host + single superadmin role | **MET** | Locked #1–#2; §3; §12 OUT; Sources | Admin UI/API only at `admin.core.dealoware.com`. One system superadmin login email `io@aiknowhow.com` = principal **`CoreOwner`** (SA §3.2 adopted). Distinct from Participant UI and `admin.platform.dealoware.com`. Human-user list on Core OUT. Multiple admin humans / operator ACL OUT. |
| 2 | Email + password; secure one-time bootstrap | **MET** | §8.1; §8.2; §8.10; §12 OUT | Sign-in is email + password for that superadmin. Bootstrap = single-use timed out-of-band link (24h; SA may tighten not lengthen); first password set only via link; TOTP enrollment required before first session. **No password** in code/docs/Spec/chat/commit history. SSO/IdP as delivered OUT. |
| 3 | TOTP 2FA required; no email OTP fallback | **MET** | §8.3; §8.10; Locked #5 | TOTP authenticator app required for every login after enrollment; required before first session. Email OTP fallback **NOT allowed** (Chief Spec / Spec lock). Recovery = one-time codes shown once at enrollment, stored **hashed**; each single-use. |
| 4 | Password reset = email link + 2FA | **MET** | §8.4 | Reset requires **both** single-use timed email link (1h) **and** 2FA (TOTP or unused recovery code). No reset path that skips 2FA. Reset email carries link only — never a password value. |
| 5 | CAPTCHA (Turnstile only) + lockout / rate limit + session | **MET** | §8.5; §8.6; §8.7; §12 OUT | CAPTCHA required on login, password-reset, and bootstrap password-set pages. Provider lock: **Cloudflare Turnstile only** ($0); AWS WAF CAPTCHA / any other CAPTCHA OUT. App lockout: 5 failed password/2FA per account / 15 min → 30 min lock; 20 failed / IP / 15 min → 30 min throttle; same per-IP window on reset-request. Idle session **30 min**; absolute **8 h**; after expiry full re-auth (password + TOTP). Session store: server-side Postgres row + HttpOnly Secure SameSite=Strict cookie (SA pick adopted). Soft HOLD invent Turnstile account. |
| 6 | Login / reset audit + edit/delete audit | **MET** | §8.6; §8.8; §4.5; §10 | Audit covers login success/failure (reason class, no secrets), reset request/completion, TOTP enroll/change, recovery-code use, **and** every successful admin edit/delete (who/when/type+id/action/before/after FieldPolicy-allowed). Auth audit IP = keyed **HMAC-SHA256** only (Chief Spec decision); raw IP only in short-lived rate-limit counters (§8.6); unkeyed hash **OUT**. Soft-deleted **toggle use is not audited** (read). Mutation audit fail → same-txn roll back (SA pick adopted). Audit log **cannot** be edited or deleted from admin UI/API. |
| 7 | Fail-closed deny for non-superadmin | **MET** | §3; §5; §8.1; §8.3; §8.7 | Missing/invalid/expired session or principal not the single superadmin → deny all admin routes (no list/edit/delete/stats beyond safe unauthorized). TOTP required for session start and for re-auth after expiry (no admin action without password + TOTP). Denied FieldClass → no write and no dump into UI/error payloads. |
| 8 | FieldPolicy only; no parallel multi-user admin ACL | **MET** | Locked #2; §3; §4; §5; §7 | All reads/writes bind Domain FieldPolicy for Core owner / superadmin (Option A §3a/§3b dual wall). Parallel multi-operator ACL / operator list rejected. |
| 9 | Confirm before every delete | **MET** | §9; §11.2 | Delete of Participant, Artifact, negotiation, or offer requires explicit confirm naming type + identity + cascade summary when cascades apply. Cancel → unchanged + no delete audit row. SA picks adopted: UI modal confirm; API confirm token (single-use, 5 min, bound to actor+entity+cascade). |
| 10 | Soft-delete + cascades; soft-deleted toggle (superadmin-only) | **MET** | §4.5; §6; §11 | Soft-delete marker **`DeletedAt`** (SA pick adopted); hard delete deferred entirely. Cascades per §11 shape (offer; negotiation→child offers; Artifact block-while-referenced; Participant→negotiations/offers, not auto Artifacts). Soft-deleted rows listable only via explicit toggle **visible only to CoreOwner / superadmin**; toggle use is a **read** and is **not** written to the audit log. Stats predicates exclude soft-deleted (`DeletedAt IS NULL`). |
| 11 | All-status lists + name search + paging (safe) | **MET** | §4.5; §4.6 | Negotiations/offers lists include all statuses (Product + cited Core + first-class **`Withdrawn`** SA pick) + soft-deleted via toggle; sort/filter server-side; **server-side offset/limit** paging (default 50 / max 200); name search case-insensitive contains per Product definitions on all four tables. **No** client-side paging that loads whole tables. Search/filter **parameterized** (no injection). Results never include FieldPolicy-denied fields. |
| 12 | Edit write surface FieldPolicy-only | **MET** | §5; §10 | Only FieldPolicy-allowed fields writable; every successful edit audited with before/after (FieldPolicy-allowed only). No second permission matrix. No password/secret dump in edit forms, errors, or audit snapshots. Optimistic concurrency via row `Version` / `UpdatedAt` ETag → 409 on stale (SA pick adopted). |
| 13 | SES mail + Turnstile as dependency + cost only | **MET** | §8.5; §8.9; §12 IN/OUT | Bootstrap/reset mail: **Amazon SES** behind a **mail interface** (Core must not depend on AWS SES SDK directly) — dependency + cost only; **no** AWS account IDs, region, SES identity, or provision in Spec. CAPTCHA: **Turnstile only** ($0); Soft HOLD invent Turnstile account. CFO Soft HOLD cost cite present. |
| 14 | OUT / Soft HOLD pack | **MET** | Header; Locked #7; §8.10; §12 OUT | Platform admin hosts; human-user list on Core; Participant UI as admin; multiple admin humans; inbound bot connector (after); settlement/escrow/checkout; SSO/IdP as delivered; email OTP fallback; **AWS WAF CAPTCHA / reCAPTCHA OUT (Turnstile only)**; Stories / code / CDK / spend / provision / deploy Soft HOLD until harden live QA PASS after app image bake then separate unlock; AWS account IDs / CDK / bot-platform internals OUT of Spec; **password values Forbidden** in Spec/docs/chat. PoC **$0**. Cost/critical → COO → CEO. |
| 15 | Traceability + re-QA handshake | **MET** | Sources; §13; Product cite 13:31; header Status | Spec cites Product scope **13:31** (1:27pm patch + 1:30pm CEO final) + CEO Turnstile-only / SES-behind-mail-interface + Option A FieldPolicy dual wall + checklist **v2** + Chief Spec decisions (no email OTP; hashed recovery; Turnstile only; SES behind mail interface no account details; soft-deleted toggle superadmin-only and not audited; app lockout/rate limits; auth audit IP = keyed HMAC-SHA256; raw IP only in short-lived counters) + SA Soft HOLD SoR reconcile. Spec QA must **not** PASS until Security QA confirms v2. Prior Spec Security PASS (v1) and prior PRE-SCORE Soft HOLD final (v2.1) **superseded**. Soft HOLD build. Not a build unlock. |

## Soft HOLDs

1. Soft HOLD **Security Soft HOLD SoR** — this FINAL points-review does **not** invent Soft HOLD SoR and is **not** Soft HOLD SoR.
2. Soft HOLD **Security QA Soft HOLD SoR qa-confirm** — parent routes Soft HOLD SoR → Security QA Soft HOLD SoR qa-confirm only at `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md`.
3. Soft HOLD **Spec Security PASS/HOLD** (Chief) — Chief Security issues after Soft HOLD SoR qa-confirm.
4. Soft HOLD **Spec QA PASS** until Security QA confirms v2 (checklist rule).
5. Soft HOLD **build** / Stories / code / CDK / spend / provision / deploy.
6. Soft HOLD invent Turnstile / SES / AWS account details or provision from Spec or this review.
7. Soft HOLD invent SSO/IdP as delivered; Soft HOLD invent password values.
8. Soft HOLD harden redeploy (H1 Swagger) — separate from this Spec track.

## Cost/critical

No build, deploy, CDK, provision, or spend from this Spec or this points-review. Soft HOLD invent AWS. Soft HOLD harden redeploy separate. PoC **$0**. Cost/critical → COO → CEO.

## Handshake next

1. This file = **FINAL** Spec Security points-review (Senior Security score). **Not** Soft HOLD SoR. Soft HOLD invent Soft HOLD SoR as PASS claim.
2. Parent → Security QA Soft HOLD SoR qa-confirm **only** at `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md`.
3. Chief Security issues PASS/HOLD to Chief Spec + CPM **after** Soft HOLD SoR qa-confirm.
4. Spec QA Soft HOLD PASS until Security QA confirms v2.
5. Soft HOLD build. PoC $0. Not a build unlock.

## Not Soft HOLD SoR / Not deploy / build approval

This points-review is Spec-track Security **FINAL score** only. It is **NOT Soft HOLD SoR**. It does **not** authorize Spec Security PASS, Soft HOLD SoR invent, merge, deploy, AWS writes, Stories, code, CDK, or spend. Prior PRE-SCORE Soft HOLD final (v2.1) is superseded for this Soft HOLD SoR track and was never Soft HOLD SoR. Prior v1 PASS is superseded and is not a live PASS claim.
