# Security checklist — Architecture (SA) · Core admin dashboard (v2 — CEO expansion)

| Field | Value |
|-------|--------|
| Status | **ISSUED v2.** Supersedes prior SA Security Soft HOLD SoR qa-confirm (pre-expansion tip). Soft HOLD Architecture QA formal PASS until Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only. |
| Date | 2026-10-05 |
| Author | Dealoware Chief Security |
| Architecture Soft HOLD SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` (amended ~13:35 ET) |
| Product (binding) | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` (1:27 lists/search/superadmin + 1:30 Turnstile-only) |
| Spec input (not Spec Security final) | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` v2.1 — Soft HOLD Spec invent until Soft HOLD SoR CLEAR; Soft HOLD Spec Security final until after SA CLEAR |
| Sibling Spec checklist v2 | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` — Soft HOLD final Spec Security QA until SA CLEAR |
| Prior SA Soft HOLD SoR qa-confirm | **Superseded** for expansion — fresh Soft HOLD SoR CLEAR required |
| Expected Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (qa-confirm only — **do not invent points-review Soft HOLD SoR**) |
| CFO Soft HOLD cost | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` — Turnstile $0/mo; SES under $0.01/mo assumed; Finance QA PASS; Soft HOLD invent spend |
| Moment | SA-REV-CORE-ADMIN |
| Host | `admin.core.dealoware.com` only |
| Hold | Soft HOLD invent Stories / build / deploy / code / CDK / spend / provision. Soft HOLD invent password. Soft HOLD invent AWS account IDs. Soft HOLD harden redeploy (H1) separate. PoC **$0**. |
| DOC-FLOW | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` |

## Scope note

Score amended Architecture Soft HOLD SoR Option A for Core admin (design only). Real points — not N/A. Architecture is **Security-critical**. No password values. No AWS account / CDK / bot-platform internals in Soft HOLD SoR. Not a build unlock. Handshake Soft HOLD SoR = **qa-confirm only**.

## Itemized security points (Architecture design must bind)

1. **Host + single superadmin CoreOwner** — Admin only at `admin.core.dealoware.com`. Principal **CoreOwner** = single system superadmin login email **`io@aiknowhow.com`**. CoreOwner ≠ Participant UI ≠ platform admin. O1 human users OUT. Reject multi-admin operator ACL.

2. **Email + password + one-time bootstrap; Soft HOLD invent password** — Email + password for that superadmin. Bootstrap via **single-use** out-of-band link. **No password values** in Soft HOLD SoR, docs, Spec text, chat, or commit history. Soft HOLD invent SSO/IdP as delivered.

3. **TOTP required; no email OTP fallback; hashed recovery codes** — TOTP authenticator required before first interactive admin session and on every login after. Email OTP **not** a 2FA fallback. Recovery = one-time recovery codes stored **hashed** only (never plaintext in Soft HOLD SoR/logs/UI dump).

4. **Password reset = email link + 2FA** — Reset requires email link **and** TOTP (or hashed recovery-code path under the same 2FA rule). Soft HOLD invent password-only reset without 2FA.

5. **Turnstile only + lockout / rate limit + session** — CAPTCHA on login and password-reset pages = **Cloudflare Turnstile only** (CEO). AWS WAF CAPTCHA OUT; reCAPTCHA OUT. App lockout/rate limits required (Soft HOLD SoR: account 5 fails/15m → 30m; IP 20 fails/15m). Session absolute + idle (Soft HOLD SoR: 8h / 30m); after expiry full re-auth (password + TOTP). HttpOnly Secure SameSite=Strict cookie. Auth secrets never in API bodies/audit/logs/metrics/traces.

6. **SES behind mail interface; Soft HOLD invent AWS** — Bootstrap/reset mail via **Amazon SES** through a **mail interface** (port/adapter); Core must not depend on AWS SES SDK types directly. Soft HOLD invent AWS account IDs, region, SES identity, from-address, or provision. Cite CFO Soft HOLD cost note (Turnstile $0/mo; SES under $0.01/mo assumed; Finance QA PASS) — Soft HOLD invent spend / not spend approval.

7. **Audit: login/reset/2FA + edit/delete; soft-deleted toggle not audited** — Append-only audit for successful admin edits/deletes (+ cascades) **and** login success/failure (reason class only), password reset request/completion, TOTP enroll/change, recovery-code use. Audit **cannot** be edited/deleted from admin UI/API. Same-transaction pairing for mutations (audit fail → roll back). Soft-deleted **toggle use is not audited**; toggle is **CoreOwner / superadmin only**. No secret dump in snapshots.

8. **Fail-closed dual-wall FieldPolicy; no parallel admin ACL** — All admin reads/writes bind Domain `IFieldPolicy.Evaluate` for **CoreOwner** (Option A §3a/§3b; cite, do not rewrite). Missing/invalid/expired session or missing TOTP → deny all admin routes. Denied FieldClass → no write and no dump. Reject parallel multi-operator ACL. Soft HOLD invent admin agents in this Soft HOLD SoR.

9. **Confirm-before-delete + cascade summary integrity** — UI confirm names type + identity + cascade summary; cancel → no mutation and no delete audit. API confirm token required (single-use, short-lived, bound to actor + entity + cascade set); blind DELETE rejected.

10. **Soft-delete + cascades** — Soft-delete marker default; hard delete deferred. Offer → offer only. Negotiation → negotiation + child offers; open warn. Artifact → **block** while non-deleted negotiation references it. Participant → Participant + negotiations + those offers (confirm counts); do not auto-delete Artifacts. Settlement OUT.

11. **All-status lists + name search + server-side paging (safe)** — Negotiations/offers all statuses (incl. withdrawn + soft-deleted via toggle). Sort/filter locked fields. Server-side paging (Soft HOLD SoR default 50 / max 200). Name search per Product meanings on all four tables. Soft HOLD invent client-only full-table dump. Search/filter Soft HOLD injection / denied FieldClass dump. Stats exclude soft-deleted; Soft HOLD invent charts/warehouse.

12. **Edit write surface FieldPolicy-only; concurrency fail-closed** — Only FieldPolicy-allowed fields writable; no second permission matrix. Optimistic concurrency; stale write → safe conflict. LoginEmail / ContactEmail / StrategyBody / auth secrets follow FieldPolicy — no dump into errors/logs/metrics/traces.

13. **OUT / Soft HOLD pack** — Soft HOLD invent Stories / code / CDK / spend / provision / deploy Soft HOLD until harden live QA PASS after app image bake then separate unlock. Soft HOLD invent password. Soft HOLD invent AWS account IDs. Platform admin hosts OUT. Human-user list OUT. Participant UI as admin OUT. Multiple admin humans OUT. Email OTP fallback OUT. WAF CAPTCHA / reCAPTCHA OUT. App Runner OUT. Inbound bot connector after. Settlement OUT. SSO/IdP as delivered OUT. PoC **$0**. Cost/critical → COO → CEO.

14. **Traceability + handshake Soft HOLD SoR** — Architecture cites Product (1:27 + 1:30) + Spec draft v2.1 input + CFO Soft HOLD cost + Option A §3a/§3b + mechanism picks. Architecture QA must **not** formal PASS until Security QA confirms via Soft HOLD SoR `…core-admin-dashboard-sa-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** — **do not invent points-review Soft HOLD SoR**. Prior pre-expansion Soft HOLD SoR qa-confirm **superseded**. Soft HOLD Spec invent / Soft HOLD Spec Security final until Soft HOLD SoR CLEAR (CEO Architecture-first). Soft HOLD build. Not a build unlock.

## Handshake next

1. Senior Architect rebinds §6 to this **v2** checklist (section cites) if numbering differs.
2. Senior Security points-review (content OK) → Security QA **qa-confirm Soft HOLD SoR only**.
3. Soft HOLD Architecture QA formal PASS until Soft HOLD SoR CLEAR.
4. Soft HOLD Spec Security final until after SA Soft HOLD SoR CLEAR + Spec reconcile.
5. Soft HOLD build. PoC $0.

## Cost/critical

No build, deploy, CDK, provision, or spend from this Architecture. Soft HOLD invent AWS. Soft HOLD invent password. Soft HOLD harden redeploy separate. PoC **$0**. Cost/critical → COO → CEO.
