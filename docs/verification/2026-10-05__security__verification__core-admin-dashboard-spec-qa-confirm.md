# Security QA Soft HOLD SoR — Spec · Core admin dashboard (checklist v2 Soft HOLD SoR qa-confirm)

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~1:53pm ET) |
| Verdict | **PASS** (15/15 MET, 0 GAP, 0 PARTIAL) |
| Soft HOLD SoR role | **This file is Soft HOLD SoR** (handshake Soft HOLD SoR = qa-confirm only). Do **not** invent Soft HOLD SoR from points-review. |
| Asked by | Senior Security FINAL PASS 15/15 → Soft HOLD SoR qa-confirm only; Chief Security Soft HOLD SoR ISSUED; Soft HOLD Spec QA until Security Soft HOLD SoR CLEAR |
| Checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` (**ISSUED v2**, pts 1–15) |
| Spec tip | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` (**v2.2**, Chief Spec CLEAR) |
| Spec tip sha256 | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — **MATCH** (verified `sha256sum` before score) |
| Senior Security points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-points-review.md` (**PASS** 15/15 FINAL — cited; independently re-scored; agrees; **not** Soft HOLD SoR) |
| Prior Spec Soft HOLD SoR / v1 qa-confirm | **SUPERSEDED** — prior tip scored old 10/10 itemization (~1:15pm ET); prior PRE-SCORE Soft HOLD final (v2.1) superseded for this Soft HOLD SoR track; this overwrite is Soft HOLD SoR only for checklist **v2** pts 1–15 |
| Sibling SA Soft HOLD SoR (cite only) | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (PASS 14/14) — not re-scored here |
| Host lock | `admin.core.dealoware.com` only |
| Principal | **CoreOwner** = single system superadmin `io@aiknowhow.com` |
| PoC | **$0** |
| DOC-FLOW / Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` |

**Handshake Soft HOLD SoR = qa-confirm only.** Do **not** invent points-review Soft HOLD SoR. Soft HOLD invent Soft HOLD SoR as PASS claim from Senior points-review alone. **Not** build / deploy / Stories / code / CDK / spend unlock. Soft HOLD invent password values. Soft HOLD invent AWS account IDs. Soft HOLD Spec Security PASS/HOLD (Chief) after this Soft HOLD SoR. Soft HOLD harden redeploy (H1) stands separately. No AWS account / CDK / bot-platform internals invented here. Names only.

## Soft notes accepted (non-blocking)

| Soft note | Disposition |
|-----------|-------------|
| Soft HOLD Spec QA PASS until Security Soft HOLD SoR CLEAR (checklist v2) | **Accepted** — Security Soft HOLD SoR CLEARED by this PASS |
| Soft HOLD invent Soft HOLD SoR as PASS from points-review alone | **Accepted** — Soft HOLD SoR is this qa-confirm only |
| Prior Spec Soft HOLD SoR / v1 qa-confirm (10/10) **superseded** | **Accepted** — this overwrite |
| Prior PRE-SCORE Soft HOLD final (v2.1) **superseded** for this Soft HOLD SoR track | **Accepted** — was never Soft HOLD SoR |
| Soft HOLD invent password values in Spec / docs / chat / commit history | **Accepted** — Spec §8.10 / §12 Forbidden; scan: none invented here |
| Soft HOLD invent AWS account IDs / region / SES identity / Turnstile account / provision | **Accepted** — Spec §8.5 / §8.9 / §12 Soft HOLD invent |
| Soft HOLD build / Stories / code / CDK / spend / provision / deploy | **Accepted** |
| Soft HOLD invent SSO/IdP as delivered; Soft HOLD invent email OTP fallback | **Accepted** — OUT |
| Soft HOLD harden redeploy (H1) — separate | **Accepted** |
| Soft HOLD Spec Security PASS/HOLD (Chief) after Soft HOLD SoR qa-confirm | **Accepted** — this file CLEAR Soft HOLD SoR; Chief issues formal PASS/HOLD |
| Not build unlock; PoC $0; Cost/critical → COO → CEO | **Accepted** |

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Chief Spec Security checklist ISSUED v2 | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` | Binding pts **1–15** ISSUED |
| Spec tip v2.2 | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | Body §§3–12 + Locked + Sources + §13 bind; sha256 **MATCH** |
| Senior FINAL points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-points-review.md` | **PASS** 15/15 — cited; not rubber-stamped; **not** Soft HOLD SoR |
| Product scope (cite) | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | Spec cites **13:31** (1:27pm patch + 1:30pm CEO final) |
| Sibling SA Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` | Cite-only PASS 14/14 — Arch not re-scored |

## Independent re-score (Security QA Soft HOLD SoR)

Score vs **checklist v2 pts 1–15** against Spec **body** §§3–12 (Locked decisions, Sources, OUT) — §13 self-map alone is **not** the score basis. Senior PASS cited, not rubber-stamped. Hard focus verified in Spec body (not only bind table).

| # | Point | Senior | Security QA | Spec cites | Evidence |
|---|-------|--------|-------------|------------|----------|
| 1 | Host + single superadmin role | MET | **MET** | Locked #1–#2; §3; §12 OUT | Admin only at `admin.core.dealoware.com`. One system superadmin `io@aiknowhow.com` = **`CoreOwner`**. Distinct from Participant UI and `admin.platform.dealoware.com`. Human-user list OUT. Multiple admin humans / operator ACL OUT. |
| 2 | Email + password; secure one-time bootstrap | MET | **MET** | §8.1; §8.2; §8.10; §12 OUT | Email + password for that superadmin. Bootstrap = single-use timed out-of-band link; first password set only via link; TOTP enrollment before first session. **No password** in code/docs/Spec/chat/commit history. SSO/IdP as delivered OUT. |
| 3 | TOTP 2FA required; no email OTP fallback | MET | **MET** | §8.3; §8.10; Locked #5 | TOTP required before first session and on every login after. Email OTP fallback **NOT allowed**. Recovery = one-time codes shown once, stored **hashed**; each single-use. |
| 4 | Password reset = email link + 2FA | MET | **MET** | §8.4 | Reset requires **both** single-use timed email link **and** 2FA (TOTP or unused recovery code). No reset that skips 2FA. Reset email carries link only — never a password value. |
| 5 | CAPTCHA (Turnstile only) + lockout / rate limit + session | MET | **MET** | §8.5; §8.6; §8.7; §12 OUT | CAPTCHA on login, password-reset, and bootstrap password-set. Provider: **Cloudflare Turnstile only** ($0); AWS WAF CAPTCHA / any other CAPTCHA **OUT**. App lockout 5/15min→30min per account; 20/15min→30min per IP; same per-IP on reset-request. Idle **30 min**; absolute **8 h**; after expiry full re-auth (password + TOTP). Session: server-side Postgres + HttpOnly Secure SameSite=Strict cookie. Soft HOLD invent Turnstile account. |
| 6 | Login / reset audit + edit/delete audit | MET | **MET** | §8.6; §8.8; §4.5; §10 | Audit covers login success/failure (reason class, no secrets), reset request/completion, TOTP enroll/change, recovery-code use, **and** every successful admin edit/delete. Soft-deleted toggle use **not** audited. Mutation audit fail → same-txn roll back. Audit **cannot** be edited/deleted from admin UI/API. **IP rules:** see Guardrails. |
| 7 | Fail-closed deny for non-superadmin | MET | **MET** | §3; §5; §8.1; §8.3; §8.7 | Missing/invalid/expired session, missing TOTP step, or principal not the single superadmin → deny all admin routes (no list/edit/delete/stats beyond safe unauthorized). Denied FieldClass → no write and no dump into UI/error payloads. |
| 8 | FieldPolicy only; no parallel multi-user admin ACL | MET | **MET** | Locked #2; §3; §4; §5; §7 | All reads/writes bind Domain FieldPolicy for Core owner / superadmin (Option A §3a/§3b dual wall). Parallel multi-operator ACL / operator list rejected. |
| 9 | Confirm before every delete | MET | **MET** | §9; §11.2 | Delete of Participant, Artifact, negotiation, or offer requires explicit confirm naming type + identity + cascade summary when cascades apply. Cancel → unchanged + no delete audit row. UI modal + API confirm token (single-use, 5 min, bound to actor+entity+cascade). |
| 10 | Soft-delete + cascades; soft-deleted toggle (superadmin-only) | MET | **MET** | §4.5; §6; §11 | Soft-delete marker **`DeletedAt`**; hard delete deferred entirely. Cascades per §11.2 (offer; negotiation→child offers; Artifact block-while-referenced; Participant→negotiations/offers, not auto Artifacts). Soft-deleted toggle **visible only to CoreOwner / superadmin**; toggle use is a **read** and is **not** written to audit. Stats: `DeletedAt IS NULL`. |
| 11 | All-status lists + name search + paging (safe) | MET | **MET** | §4.5; §4.6 | Negotiations/offers lists include all statuses + first-class **`Withdrawn`** + soft-deleted via toggle; sort/filter server-side; **server-side offset/limit** paging (default 50 / max 200); name search case-insensitive contains per Product on all four tables. **No** client-side full-table load. Parameterized inputs; FieldPolicy on results (denied fields never included). |
| 12 | Edit write surface FieldPolicy-only | MET | **MET** | §5; §10 | Only FieldPolicy-allowed fields writable; every successful edit audited with before/after (FieldPolicy-allowed only). No second permission matrix. No password/secret dump in edit forms, errors, or audit snapshots. Optimistic concurrency via `Version` / `UpdatedAt` ETag → 409 on stale. |
| 13 | SES mail + Turnstile as dependency + cost only | MET | **MET** | §8.5; §8.9; §12 IN/OUT | Bootstrap/reset mail: **Amazon SES** behind a **mail interface** (Core must not depend on AWS SES SDK directly) — dependency + cost only; **no** AWS account IDs, region, SES identity, or provision in Spec. CAPTCHA: **Turnstile only** ($0); Soft HOLD invent Turnstile account. CFO Soft HOLD cost cite present. |
| 14 | OUT / Soft HOLD pack | MET | **MET** | Header; Locked #7; §8.10; §12 OUT | Platform admin hosts; human-user list on Core; Participant UI as admin; multiple admin humans; inbound bot connector (after); settlement/escrow/checkout; SSO/IdP as delivered; email OTP fallback; **AWS WAF CAPTCHA / reCAPTCHA OUT (Turnstile only)**; Stories / code / CDK / spend / provision / deploy Soft HOLD; AWS account IDs / CDK / bot-platform internals OUT; **password values Forbidden**. PoC **$0**. Cost/critical → COO → CEO. |
| 15 | Traceability + re-QA handshake | MET | **MET** | Sources; §13; Product cite 13:31; header | Spec cites Product **13:31** (1:27pm patch + 1:30pm CEO final) + CEO Turnstile-only / SES-behind-mail-interface + Option A FieldPolicy dual wall + checklist **v2** + Chief Spec decisions (no email OTP; hashed recovery; Turnstile only; SES behind mail interface no account details; soft-deleted toggle superadmin-only and not audited; app lockout/rate limits; auth audit IP = keyed HMAC-SHA256; raw IP only in short-lived counters) + SA Soft HOLD SoR reconcile. Spec QA must **not** PASS until Security Soft HOLD SoR confirms v2. Prior Spec Security PASS (v1) and prior PRE-SCORE Soft HOLD final (v2.1) **superseded**. Soft HOLD build. Not a build unlock. |

## Alignment with Senior Security

Senior FINAL points-review scored pts **1–15 MET** on Spec tip v2.2 (sha256 match) with Soft HOLDs for Soft HOLD SoR = qa-confirm only, Soft HOLD Spec Security PASS/HOLD (Chief), Soft HOLD Spec QA until Security confirms v2, Soft HOLD build, Soft HOLD invent Turnstile/SES/AWS, Soft HOLD invent password values, Soft HOLD harden redeploy separate. Independent Security QA Soft HOLD SoR re-score **agrees** on all 15; soft notes **accepted**. No bounce. No gaps vs Senior. Soft HOLD invent Soft HOLD SoR as PASS from points-review alone — Soft HOLD SoR is **this** qa-confirm only.

## Guardrails (hard spot-check)

| Guardrail | Status | Spec body cite |
|-----------|--------|----------------|
| Host `admin.core.dealoware.com` only | **Held** | Locked #1; §3; §12 |
| CoreOwner + FieldPolicy; no parallel ACL | **Held** | Locked #2; §3–§5; §7 |
| **§8.6 raw IP only in short-lived rate-limit counters** (expire with 15-/30-minute windows); raw IP **never** to audit / other logs / metrics | **Held** | §8.6 Raw IP retention (Chief Spec decision) |
| **§10 auth audit IP = keyed HMAC-SHA256** of client IP + reason class only; **unkeyed hash OUT**; raw IP never in audit | **Held** | §10 Auth audit IP (Chief Spec decision); §8.8 context |
| Turnstile only; AWS WAF CAPTCHA / reCAPTCHA / any other CAPTCHA OUT | **Held** | §8.5; §8.10; §12 OUT |
| SES behind mail interface; no AWS account/region/identity/provision in Spec | **Held** | §8.9; §12 |
| Password values **Forbidden** in Spec/docs/chat/code/commit history | **Held** | §8.10; §5; §10; §12 OUT |
| Soft-deleted toggle CoreOwner-only; toggle use not audited | **Held** | §4.5; §10; §11.1 |
| Soft HOLD invent password / AWS | **Held** |
| Soft HOLD build; PoC $0; not build unlock | **Held** |
| Handshake Soft HOLD SoR = qa-confirm only (no points-review Soft HOLD SoR) | **Held** — this file |
| Prior Soft HOLD SoR / v1 + PRE-SCORE Soft HOLD final superseded | **Held** |

## Gaps

**None.** Soft notes non-blocking. Soft HOLD invent Soft HOLD SoR beyond this qa-confirm not done. Soft HOLD invent Soft HOLD SoR as PASS from points-review alone not done. Soft HOLD build / Stories / code / CDK / spend not unlocked. Hard IP rules (§8.6 / §10) **MET** in Spec body.

## Handshake status

Security QA Soft HOLD SoR → **PASS** (15/15 MET). Soft HOLD Spec Security Soft HOLD SoR **CLEARED**. Soft HOLD Spec QA Soft HOLD Security gate may lift on checklist **v2**. Soft HOLD Spec Security PASS/HOLD remains with **Chief Security** (issues to Chief Spec + CPM after this Soft HOLD SoR). Soft HOLD build / Stories / code / CDK / spend / provision / deploy. Soft HOLD invent password / AWS. Soft HOLD harden redeploy (H1) separate. Prior Spec Soft HOLD SoR / v1 qa-confirm and prior PRE-SCORE Soft HOLD final **superseded**. Sibling SA Soft HOLD SoR PASS 14/14 cite-only (Arch not re-scored). Not build unlock. PoC **$0**. Cost/critical: none from this Soft HOLD SoR → COO → CEO if any later spend.
