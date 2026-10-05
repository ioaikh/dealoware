# CA design grounding PASS — Core admin dashboard Soft HOLD SoR CEO amend (SA-REV-CORE-ADMIN)

| Field | Value |
|-------|--------|
| Author | Dealoware Chief Architect |
| Date | 2026-10-05 (~1:46pm ET) |
| Verdict | **PASS** — CA design grounding PASS (CEO expansion Soft HOLD SoR) |
| Moment | SA-REV-CORE-ADMIN |
| Disposition | Soft HOLD SoR CLEAR for Spec reconcile / CPM Stage 2 Spec open / BM routing. Soft HOLD invent Stories / build / code / CDK / spend / provision / deploy. Soft HOLD invent password. Soft HOLD invent AWS account IDs. Soft HOLD invent spend. |
| Host | `admin.core.dealoware.com` only |
| Principal | **CoreOwner** (single system superadmin `io@aiknowhow.com`) |
| PoC | **$0** |
| DOC-FLOW | `verification/2026-10-05__ca__verification__core-admin-dashboard-design-grounding-pass.md` |

**Not a build unlock.** Soft HOLD harden redeploy (H1) stands separately. No password values. No AWS account / CDK / bot-platform internals invented here. Prior pre-expansion CA PASS content in this path is **superseded** by this CEO-amend Soft HOLD SoR CLEAR.

## Option A pick (binding)

Same Core modular monolith; separate Core admin route surface at `admin.core.dealoware.com`; distinct **CoreOwner** principal = single system superadmin `io@aiknowhow.com`; FieldPolicy dual wall (Option A §3a/§3b; cite, do not rewrite); no parallel admin ACL. O1 human users dropped. Platform-admin host replaced.

CEO expansion bound: all-status lists + sort/filter + offset/limit paging (default 50 / max 200) + name search; email + password + required TOTP (no email OTP fallback; recovery codes hashed); one-time bootstrap; Turnstile only (WAF CAPTCHA OUT); SES behind mail interface; lockout 5/15m→30m account + 20/15m IP; session 8h absolute / 30m idle; login/reset/2FA + edit/delete audit; confirm-before-delete; soft-delete + cascades; Soft HOLD invent password/AWS.

## Evidence Soft HOLD SoR pack (CEO amend)

| Artifact | Path | Status |
|----------|------|--------|
| Binding Product | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | Cleared (1:27 lists/search/superadmin + 1:30 Turnstile-only) |
| Spec draft (input only) | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | v2.1 input; Soft HOLD Spec invent lifted for reconcile after Soft HOLD SoR CLEAR |
| Architecture Soft HOLD SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` | Amended; §6 answers 1–14 **MET** vs checklist v2 |
| SA Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` | **ISSUED v2** (pts 1–14) |
| Senior Security points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-points-review.md` | PASS 14/14 (not handshake Soft HOLD SoR) |
| SA Security Soft HOLD SoR qa-confirm | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` | **PASS 14/14** — Soft HOLD SoR CLEARED |
| Arch QA formal PASS (CEO amend) | `verification/2026-10-05__sa__verification__core-admin-dashboard-ceo-amend-review.md` | Formal Arch QA PASS (~1:46pm ET) |
| CFO Soft HOLD cost estimate | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` | Turnstile $0/mo; SES under $0.01/mo assumed |
| Finance QA | `finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md` | **PASS** — estimate only; Soft HOLD invent spend |
| Prior Arch QA (pre-amend) | `verification/2026-10-05__sa__verification__core-admin-dashboard-review.md` | **Superseded** for this amend |
| Prior SA Soft HOLD SoR qa-confirm (pre-expansion) | same Soft HOLD SoR qa-confirm path before overwrite | **Superseded** |

Arch QA formal PASS alone is not CA PASS; this file is the CA design grounding PASS Soft HOLD SoR.

## Soft HOLD Spec invent

Soft HOLD Spec invent **LIFTED** for Spec triad to reconcile Spec to this Soft HOLD SoR (Architecture-first Soft HOLD SoR CLEAR done). Soft HOLD Spec Security final until Spec reconciles then Spec Security QA v2. Soft HOLD invent Stories. Soft HOLD build. Soft HOLD deploy. Soft HOLD invent password. Soft HOLD invent AWS. Soft HOLD invent spend. PoC **$0**. Not a build unlock.

## Locks that remain (do not lift)

- invent Stories
- invent build / implementation / code / CDK / spend / provision / deploy
- Soft HOLD invent password (no password values in Soft HOLD SoR/docs/chat)
- Soft HOLD invent AWS account IDs / SES identity / from-address / provision
- Soft HOLD invent spend (CFO estimate + Finance QA PASS are not spend approval)
- Soft HOLD until harden live QA PASS after app image bake, then a separate unlock
- inbound bot connector (comes after)
- platform admin hosts; human-user list on Core; Participant UI as admin; multi-admin ACL
- SSO/IdP as delivered; email OTP as 2FA; WAF CAPTCHA / reCAPTCHA; App Runner; MotorMarket/DC4; settlement
- PoC **$0**

## Disposition

CA design grounding **PASS**. Soft HOLD SoR CLEAR (Arch QA formal + Security Soft HOLD SoR CLEAR + CFO Soft HOLD cost + CA grounding). CPM may open Stage 2 Spec reconcile. Soft HOLD build. PoC **$0**. Not a build unlock.
