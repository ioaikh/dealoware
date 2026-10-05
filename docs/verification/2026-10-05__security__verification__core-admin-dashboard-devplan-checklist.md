# Security checklist — Dev Plan · Core admin dashboard

| Field | Value |
|-------|--------|
| Status | **ISSUED.** Soft HOLD Dev Plan QA PASS until Security Soft HOLD SoR qa-confirm CLEAR. Soft HOLD invent Stories / code / build / deploy Soft HOLD A7 until H4 live PASS. |
| Date | 2026-10-05 (~1:56pm ET) |
| Author | Dealoware Chief Security |
| Expected plan | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` (Senior drafting) |
| Spec (binding) | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` (**v2.2**, Chief Spec CLEAR) |
| Spec tip sha256 | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` |
| Spec Security Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` (**PASS** 15/15) |
| Spec checklist (cite) | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` (v2 pts 1–15) |
| SA Soft HOLD SoR (cite) | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` + `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (PASS 14/14) |
| Expected Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` (qa-confirm only — **do not invent points-review Soft HOLD SoR**) |
| Host | `admin.core.dealoware.com` only |
| Hold | Soft HOLD invent passwords / AWS account IDs · Soft HOLD deploy · Soft HOLD A7 until H4 live PASS · Soft HOLD invent Stories/code/CDK/spend/provision until separate unlock · Soft HOLD harden redeploy (H1) separate · Soft HOLD invent process-global auth flood limiter until Chief Security explicitly approves · PoC **$0** |
| DOC-FLOW | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` |

## Scope note

Dev Plan must **schedule, gate, and verify** Spec Security locks for Core admin (not redesign Spec/SA). Real points — not N/A. Plan is KB design planning only until Soft HOLD A7 / Soft HOLD build lifts. **No password values** in plan/docs/chat. Soft HOLD invent AWS account IDs / ARNs / SES identity / Turnstile site keys. Soft HOLD invent process-global auth budget (DoS trade-off) until Chief Security explicitly approves. Not a build unlock. Handshake Soft HOLD SoR = **qa-confirm only**.

## Itemized security points (Dev Plan must weave)

1. **Host + single CoreOwner gate** — Plan schedules admin UI/API only for `admin.core.dealoware.com`. Single superadmin login email `io@aiknowhow.com` → principal **`CoreOwner`**. Soft HOLD invent platform-admin host tasks, human-user list on Core, Participant UI-as-admin, or multi-admin / operator ACL Stories.

2. **Bootstrap + email/password (no invent password)** — Plan schedules single-use timed out-of-band bootstrap link; first password set only via that link; TOTP enrollment **before** first session. Plan Soft HOLD invent password values in Stories, fixtures, README, chat, or commit history (placeholders / env-only for secrets). Soft HOLD invent SSO/IdP as delivered.

3. **TOTP required; no email OTP; hashed recovery** — Plan schedules authenticator-app TOTP on every login after enrollment; Soft HOLD invent email OTP fallback tasks. Recovery codes: show once, store **hashed**, single-use. Plan names verify steps for “email OTP path absent.”

4. **Password reset = email link + 2FA** — Plan schedules reset requiring **both** single-use short-lived email link **and** 2FA (TOTP or unused recovery code). Soft HOLD invent reset that skips 2FA. Reset mail carries link only — never a password value.

5. **Turnstile only + lockout/rate + session** — Plan schedules Cloudflare **Turnstile only** on login, password-reset, and bootstrap password-set pages (Soft HOLD invent Turnstile account/site keys in plan). Soft HOLD invent AWS WAF CAPTCHA / reCAPTCHA / any other CAPTCHA. Weave Spec lockout: 5 fails/account/15m → 30m lock; 20 fails/IP/15m → 30m throttle (same per-IP window on reset-request). Session: idle **30m**, absolute **8h**; after expiry full re-auth (password + TOTP). Server-side session + HttpOnly Secure SameSite=Strict cookie per SA Soft HOLD SoR. **Soft HOLD invent process-global auth flood limiter** (shared budget across all IPs) until Chief Security explicitly approves the lockout/DoS trade-off — per-IP + per-account only for this plan slice.

6. **Raw IP counters only; audit IP = keyed HMAC-SHA256** — Plan schedules: raw client IP lives **only** in short-lived rate-limit counters (expire with 15-/30-minute windows); never written to audit, other logs, or metrics. Auth audit stores IP **only** as keyed **HMAC-SHA256** (server-side secret, rotatable) + failure reason class. Soft HOLD invent unkeyed hash or raw IP in audit/logs/metrics. Soft HOLD invent HMAC key value in plan/docs/chat.

7. **SES behind mail interface; Soft HOLD invent AWS** — Plan schedules bootstrap/reset mail through a **mail interface** (port/adapter); Soft HOLD invent direct AWS SES SDK dependency in Core app tasks. Soft HOLD invent AWS account IDs, region, SES identity, from-address, provision, or spend. Cite CFO Soft HOLD cost only (Turnstile $0; SES under $0.01/mo assumed) — not spend approval.

8. **Audit: auth + edit/delete; soft-deleted toggle not audited** — Plan schedules append-only audit for login success/fail (reason class), reset request/completion, TOTP enroll/change, recovery-code use, **and** every successful admin edit/delete (+ cascade rows). Soft-deleted **toggle use is not audited**; toggle **CoreOwner-only**. Audit cannot be edited/deleted from admin UI/API. Same-transaction pairing: audit fail → roll back mutation / auth-state change. Soft HOLD invent secret dump in snapshots/errors.

9. **Fail-closed FieldPolicy dual wall; no parallel ACL** — Plan schedules all admin reads/writes through Domain FieldPolicy for **CoreOwner** (Option A §3a/§3b — cite, do not rewrite). Missing/invalid/expired session or missing TOTP → deny all admin routes. Denied FieldClass → no write and no dump. Soft HOLD invent parallel multi-operator ACL / second permission matrix Stories.

10. **Confirm-before-delete + soft-delete cascades** — Plan schedules UI modal confirm (type + identity + cascade summary) and API confirm-token path (single-use, short-lived, bound to actor + entity + cascade set); blind DELETE rejected; cancel → no mutation / no delete audit. Soft-delete default (`DeletedAt`); hard delete deferred. Cascades per Spec §11 / SA Soft HOLD SoR (offer; negotiation→offers; Artifact block-while-referenced; Participant→negotiations/offers, not auto Artifacts). Soft HOLD invent settlement cascades.

11. **Lists / search / paging safe** — Plan schedules all-status negotiation/offer lists; soft-deleted via CoreOwner-only toggle; sort/filter server-side; **server-side** paging (default 50 / max 200; Soft HOLD invent client-only full-table dump); name search per Product on all four tables; parameterized inputs; results never include FieldPolicy-denied fields. Stats exclude soft-deleted. Soft HOLD invent charts/warehouse.

12. **Edit write surface + concurrency** — Plan schedules only FieldPolicy-allowed fields writable; every successful edit audited with before/after (FieldPolicy-allowed only). Optimistic concurrency; stale write → safe conflict (no silent overwrite). Soft HOLD invent password/secret fields in edit forms or audit snapshots.

13. **OUT / Soft HOLD / A7 gate pack** — Plan Soft HOLD invent: Stories execution / code / CDK / spend / provision / **deploy** Soft HOLD A7 until **H4 live PASS** (and Soft HOLD harden redeploy H1 separate until H1–H6(+H6b) PASS + CEO OK). Soft HOLD invent password values. Soft HOLD invent AWS account IDs. Soft HOLD invent process-global auth flood limiter until Chief Security approves. Platform admin hosts OUT; human-user list OUT; Participant UI as admin OUT; multiple admin humans OUT; email OTP fallback OUT; WAF CAPTCHA / reCAPTCHA OUT; inbound bot connector after; settlement OUT; SSO/IdP as delivered OUT. PoC **$0**. Cost/critical → COO → CEO.

14. **Traceability + handshake Soft HOLD SoR** — Plan cites Spec v2.2 tip sha256 `25f2647…` + Spec Security Soft HOLD SoR PASS 15/15 + SA Soft HOLD SoR PASS 14/14 + this **Dev Plan** checklist. Dev Plan QA must **not** PASS until Security Soft HOLD SoR qa-confirm CLEAR at `…core-admin-dashboard-devplan-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** — do **not** invent points-review Soft HOLD SoR. Soft HOLD invent Stories/code/deploy from this checklist. Soft HOLD A7 until H4. Not a build unlock. PoC **$0**.

## Handshake next

1. Senior Dev Planner weaves pts **1–14** into `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` (section cites).
2. Soft HOLD Dev Plan QA PASS until Senior Security → Security QA Soft HOLD SoR qa-confirm → Chief Security PASS/HOLD to Chief Dev Planner + CPM.
3. Soft HOLD invent Stories / code / build / Soft HOLD A7 until H4 live PASS. Soft HOLD invent password/AWS. Soft HOLD invent process-global auth flood limiter until Chief Security approves. Soft HOLD harden redeploy (H1) separate. PoC $0.

## Cost/critical

No build, deploy, CDK, provision, or spend from this Dev Plan checklist. Soft HOLD invent AWS. Soft HOLD invent password. Soft HOLD A7 until H4. Soft HOLD harden redeploy separate. PoC **$0**. Cost/critical → COO → CEO.
