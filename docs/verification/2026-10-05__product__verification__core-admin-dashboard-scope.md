# Verification — Core admin dashboard Product scope note (CEO locks)

**QA:** Dealoware Product QA  
**Date:** 2026-10-05 (~1:49pm ET)  
**Verdict:** **PASS** (requirements check only — not a build)  
**Deliverable:** `/workspace/dealoware-kb/product/2026-10-05__product__note__core-admin-dashboard-scope.md`  
**Also on main:** `docs/product/2026-10-05__product__note__core-admin-dashboard-scope.md` @ `d5af385` (PR #12)  
**Brief:** Chief Product → Product QA (CPM / Bot Manager A1) — verify vs CEO locks  
**Confirm to:** Dealoware Chief Product  
**Not:** build PASS. Soft HOLD invent password. Soft HOLD invent AWS. Soft HOLD build. Soft HOLD Stories until Spec/SA CLEAR + docs push. PoC **$0**. No password invented. Scope note not edited.

## Checklist

| # | Requirement | Result | Evidence |
|---|-------------|--------|----------|
| 1 | Host = `admin.core.dealoware.com` only; not `admin.platform` | PASS | Host field; Host rule table; IN 1; AC1. `admin.platform.dealoware.com` OUT. |
| 2 | IN: list/view/edit/delete Participants, Artifacts, negotiations, offers + overall stats (no charts/warehouse) | PASS | Goal; IN 2–4, 7; AC2–AC5. Stats = Participants, open negotiations, offers, accepts, declines. No charts. No warehouse. |
| 3 | OUT: human users on Core; platform admin; Participant UI as admin; inbound connector after this | PASS | Reconcile O1 OUT; OUT table rows; Sequence; AC6; AC8. |
| 4 | Lists: ALL negotiations + ALL offers every status (incl. soft-deleted via toggle); sort+filter status, created/updated, participant, artifact, value/price, negotiation id; server-side paging | PASS | IN 5; AC4. Statuses named include open/accepted/declined/expired/withdrawn/soft-deleted via toggle. Sort/filter fields match. Server-side paging required. |
| 5 | Search by name on all four tables with Product name definitions present | PASS | IN 6 definitions for Participants, Artifacts, negotiations, offers; AC2–AC4. |
| 6 | Single system superadmin login `io@aiknowhow.com`; email+password; one-time bootstrap; **no password in the note**; Soft HOLD invent password stated | PASS | Spec/SA must-cover Superadmin + Bootstrap; AC6–AC7; OUT invent password + password values Forbidden; Status Soft HOLD invent password. No password value in the note. |
| 7 | 2FA required TOTP (Spec may decide email OTP fallback); reset = email link + 2FA; login/reset audit; lockout/rate limit; session lifetime | PASS | Spec/SA must-cover 2FA, Password reset, Session/abuse; AC7. |
| 8 | CAPTCHA = **Cloudflare Turnstile only**; AWS WAF CAPTCHA OUT; reCAPTCHA OUT | PASS | Status CEO final 1:30pm ET; Spec/SA CAPTCHA row; OUT AWS WAF CAPTCHA / reCAPTCHA; AC7. |
| 9 | SES named for reset emails; Soft HOLD invent AWS; dependencies + cost only | PASS | Soft HOLD invent AWS field; Spec/SA Mail dependency; OUT invent AWS; AC7; AC9. No AWS account IDs invented. |
| 10 | Required Spec/SA topics: confirm-delete, edit/delete audit, delete semantics (soft vs hard + cascades) | PASS | Spec/SA Confirm before delete; Audit log; Delete semantics; AC7. |
| 11 | Soft HOLD build/deploy until harden live QA PASS after app image bake; Soft HOLD Stories until Spec/SA CLEAR + docs push; PoC $0; no Stories/code/CDK/spend from the note | PASS | Status; Soft HOLD build/deploy field; OUT build/Stories rows; PoC field; Success; Not in this note; AC9. |
| 12 | Claims: not settlement/escrow; not Marketing publish unlock; reconcile Step 3 O1 human users OUT | PASS | OUT Settlement/escrow; Marketing publish Soft HOLD; Reconcile vs prior Step 3 O1 **OUT**. |

## Verdict

**PASS.** All 12 CEO-lock checklist items PASS. Soft HOLD invent password. Soft HOLD invent AWS. Soft HOLD build. Soft HOLD Stories until Spec/SA CLEAR + docs push. PoC **$0**. Requirements check only — not a build PASS.
