# Security checklist — Spec · Core admin dashboard (v2 — Product 1:27pm patch)

| Field | Value |
|-------|--------|
| Status | **ISSUED v2** (CEO 1:30pm: Turnstile only; SES behind mail interface). Supersedes prior Spec Security PASS for re-QA. Soft HOLD Spec QA PASS until Security QA confirms v2 points. |
| Date | 2026-10-05 |
| Author | Dealoware Chief Security |
| Spec | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` (Senior Spec revising; bind in §13) |
| Scope (binding) | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` (patched **1:27pm ET**) |
| Prior checklist PASS | Superseded for re-QA — Product patch adds superadmin auth, lists/search |
| Host | `admin.core.dealoware.com` only |
| Hold | Soft HOLD build / Stories / code / CDK / spend / provision / deploy. Soft HOLD invent AWS account details. Soft HOLD harden redeploy (H1) separate. PoC **$0**. |
| DOC-FLOW | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` |

## Scope note

Core admin at `admin.core.dealoware.com`: single system superadmin; list/view/edit/delete Participants (not human users), Artifacts, negotiations, offers; all-status lists + soft-deleted toggle + name search. Real points — not N/A. **No passwords in Spec/docs/chat.** No AWS account information, CDK details, or bot-platform internals. Not a build unlock.

## Chief Spec decisions to score (binding for Spec)

| Decision | Spec lock |
|----------|-----------|
| Email OTP fallback | **OUT.** No email OTP as 2FA fallback. |
| Account recovery | One-time recovery codes, stored **hashed** only. |
| CAPTCHA | **Cloudflare Turnstile only** (CEO 1:30pm ET). AWS WAF CAPTCHA **OUT** — no alternative. Note cost; Soft HOLD invent Turnstile account. |
| Mail | **Amazon SES** approved for bootstrap/reset mail, behind a **mail interface**. No AWS account details in Spec. Soft HOLD invent account IDs / provision. |
| Abuse controls | App **lockout + rate limits** required on failed logins (independent of any later edge WAF). |
| Soft-deleted toggle | Visible **only** to the superadmin. Toggle use is **not** written to the audit log. |

## Itemized security points (Spec must bind)

1. **Host + single superadmin role** — Admin only at `admin.core.dealoware.com`. **One** system superadmin login email: `io@aiknowhow.com`. Distinct from Participant UI and `admin.platform.dealoware.com`. Human-user list on Core OUT. Multiple admin humans / operator ACL OUT.

2. **Email + password; secure one-time bootstrap** — Sign-in is email + password for that superadmin. Bootstrap via **single-use** out-of-band link. **No password** in code, docs, Spec text, chat, or commit history. Soft HOLD invent SSO/IdP as delivered.

3. **TOTP 2FA required; no email OTP fallback** — TOTP authenticator app required for sign-in. Spec locks **no email OTP fallback**. Recovery uses **one-time recovery codes stored hashed** (never plaintext in Spec/logs/UI dump).

4. **Password reset = email link + 2FA** — Reset requires reset email link **and** 2FA (TOTP or a hashed recovery code per Spec). Soft HOLD invent reset that skips 2FA.

5. **CAPTCHA (Turnstile only) + lockout / rate limit + session** — CAPTCHA required on login and password-reset pages. Spec locks **Cloudflare Turnstile only** (CEO 1:30pm ET); **AWS WAF CAPTCHA OUT** — no CAPTCHA alternative. Note Turnstile cost; Soft HOLD invent Turnstile account. App **lockout + rate limits** on failed logins required either way. Admin sessions expire (absolute + idle); after expiry, re-auth (password + TOTP) before any admin action.

6. **Login / reset audit + edit/delete audit** — Spec requires audit of **logins** and **password resets** (success/fail as Spec defines) in addition to every successful admin **edit** and **delete**. Audit log **cannot** be edited or deleted from admin UI/API. Soft-deleted **toggle use is not audited**. If mutation audit write fails, edit/delete must not commit (or must roll back).

7. **Fail-closed deny for non-superadmin** — Missing/invalid/expired session, missing TOTP step, or principal not the single superadmin → deny all admin routes (no list/edit/delete/stats beyond safe unauthorized). Denied FieldClass → no write and no dump into UI/error payloads.

8. **FieldPolicy only; no parallel multi-user admin ACL** — All reads/writes bind Domain FieldPolicy for the Core owner / superadmin role (Option A §3a/§3b dual wall). Reject parallel multi-operator ACL.

9. **Confirm before every delete** — Delete of Participant, Artifact, negotiation, or offer requires explicit confirm naming type + identity + cascade summary when cascades apply. Cancel → unchanged + no delete audit row.

10. **Soft-delete + cascades; soft-deleted toggle (superadmin-only)** — Soft-delete default recommended; hard delete deferred. Cascades per Spec §11 shape (offer; negotiation+offers; Artifact block-while-referenced; Participant→negotiations/offers, not auto Artifacts). Soft-deleted rows listable only via toggle **visible only to the superadmin**; toggle use **not** written to audit log.

11. **All-status lists + name search + paging (safe)** — Negotiations/offers lists include all statuses; sort/filter + **server-side paging**; name search per Product definitions on all four tables. Spec Soft HOLD invent client-only paging that dumps full tables. Search/filter Soft HOLD injection / unconstrained dump of denied FieldClasses.

12. **Edit write surface FieldPolicy-only** — Only FieldPolicy-allowed fields writable; every successful edit audited with before/after. Soft HOLD invent second permission matrix. No password/secret dump in edit forms, errors, or audit snapshots beyond FieldPolicy allow.

13. **SES mail + Turnstile as dependency + cost only** — Bootstrap/reset mail uses **Amazon SES** behind a **mail interface** (dependency + cost). Spec Soft HOLD invent AWS account IDs, ARNs, or provision. CAPTCHA is **Turnstile only** (cost noted); Soft HOLD invent Turnstile account. No account details in Spec.

14. **OUT / Soft HOLD pack** — Platform admin hosts; human-user list on Core; Participant UI as admin; multiple admin humans; inbound bot connector (after); settlement/escrow/checkout; SSO/IdP as delivered; email OTP fallback; **AWS WAF CAPTCHA** (dropped — Turnstile only); Stories / code / CDK / spend / provision / deploy Soft HOLD until harden live QA PASS after app image bake then separate unlock; AWS account IDs / CDK / bot-platform internals OUT of Spec; **password values Forbidden** in Spec/docs/chat. PoC **$0**. Cost/critical → COO → CEO.

15. **Traceability + re-QA handshake** — Spec cites Product scope patched 1:27pm + CEO 1:30pm Turnstile-only / SES-behind-mail-interface + Option A FieldPolicy dual wall + this **v2** checklist + Chief Spec decisions (no email OTP; hashed recovery codes; Turnstile **only**; SES behind mail interface no account details; soft-deleted toggle superadmin-only and not audited; app lockout/rate limits required). Spec QA must **not** PASS until Security QA confirms v2. Prior Spec Security PASS is **superseded** pending re-confirm. Soft HOLD build. Not a build unlock.

## Handshake next

1. Senior Spec binds pts 1–15 in Spec §13 (cite sections).
2. Soft HOLD Spec QA PASS until Senior Security → Security QA → Chief Security PASS/HOLD to Chief Spec + CPM.
3. Soft HOLD SA re-QA / SA checklist revise after Spec Security re-PASS (Product requires Spec+SA re-QA).
4. Soft HOLD build. PoC $0.

## Cost/critical

No build, deploy, CDK, provision, or spend from this Spec. Soft HOLD invent AWS. Soft HOLD harden redeploy separate. PoC **$0**. Cost/critical → COO → CEO.
