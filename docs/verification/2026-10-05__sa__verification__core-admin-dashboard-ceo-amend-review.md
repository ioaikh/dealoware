# Verification — Core admin Soft HOLD SoR CEO amend (SA-REV-CORE-ADMIN)

**QA:** Dealoware Architecture QA  
**Date:** 2026-10-05 (~1:46pm ET)  
**Verdict:** **Formal Architecture QA PASS** (Soft HOLD SoR CEO amend)  
**Deliverable (amended Soft HOLD SoR):** `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` (amend 13:35 ET)  
**Prior formal PASS (superseded for this amend):** `verification/2026-10-05__sa__verification__core-admin-dashboard-review.md` — pre-expansion formal PASS; **does not** cover this CEO amend  
**Confirm formal PASS to:** Chief Architect only  
**Not:** build / deploy / Stories / code / CDK / spend / Spec invent unlock. Soft HOLD invent spend. Soft HOLD invent password. Soft HOLD invent AWS account IDs. Not a build unlock. PoC **$0**.

## What changed (CEO expansion vs prior Soft HOLD SoR / prior Arch QA PASS)

| Delta area | Prior Soft HOLD SoR / prior formal PASS | Amended Soft HOLD SoR (13:35 ET) |
|------------|----------------------------------------|----------------------------------|
| Lists / search / paging | Pre-expansion (edit/delete + confirm/audit/soft-delete; no §3.9 all-status / name search / offset-limit paging lock) | **§3.9** all-status (incl. Withdrawn) + sort/filter + offset/limit paging (default 50 / max 200) + name search per Product |
| Superadmin / 2FA | Password non-SSO + session timers; TOTP/email-OTP not CEO-locked in prior Security-cleared surface the same way | **Single** superadmin `io@aiknowhow.com`; **required TOTP**; email OTP **NOT** 2FA fallback; recovery codes hashed |
| CAPTCHA | Not Turnstile-locked in prior Security-cleared pts | **Cloudflare Turnstile only**; AWS WAF CAPTCHA OUT; reCAPTCHA OUT |
| Mail | Not SES-behind-mail-interface lock in prior Security-cleared surface | **SES** via **mail interface** (port/adapter); no direct SES SDK types in Core app code |
| Cost cite | Not present | CFO Soft HOLD cost note + Finance QA **PASS** cite |
| Security gate | Prior checklist ISSUED + qa-confirm PASS 10/10 (~1:22pm ET) on **pre-amend** Soft HOLD SoR | Checklist **ISSUED v2** (pts 1–14) + Soft HOLD SoR qa-confirm **PASS 14/14** on this amend |

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Soft HOLD SoR (amended) | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` | CEO deltas bound §§3.3 / §3.9 / §4 / §6; Finance QA PASS cited; Soft HOLD Spec invent / build / invent spend / invent password / invent AWS; §6 answers 1–14 **MET** vs checklist v2 |
| Product (cleared) | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | IN 5–6 lists/search; AC1–AC9; Turnstile-only; single superadmin; Soft HOLD invent password/AWS; Soft HOLD build/deploy; PoC $0 |
| CFO Soft HOLD cost note | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` | Turnstile **$0/mo**; SES **under $0.01/mo** assumed; estimate only; Soft HOLD invent spend |
| Finance QA | `finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md` | **PASS** — estimate only; not spend/provision approval |
| Prior Arch QA (pre-amend) | `verification/2026-10-05__sa__verification__core-admin-dashboard-review.md` | Formal PASS **superseded** for this amend (pre-expansion only) |
| SA Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` | **ISSUED v2** (CEO expansion; pts 1–14) |
| Security QA Soft HOLD SoR qa-confirm | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` | **PASS** **14/14 MET** (~1:45pm ET) vs checklist **ISSUED v2**; Soft HOLD SoR CLEAR on this amend; prior pre-expansion 10/10 Soft HOLD SoR **superseded** |
| Senior Security points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-points-review.md` | Cited by Security QA as PASS 14/14 vs v2 — **not** handshake Soft HOLD SoR; handshake Soft HOLD SoR = **qa-confirm only** |

## CEO deltas — MET / MISS

| # | CEO delta | Result | Soft HOLD SoR cite (short) |
|---|-----------|--------|----------------------------|
| 1 | All-status lists + name search + server-side paging | **MET** | §3.9: every status incl. **Withdrawn**; sort/filter fields; offset/limit paging default **50** / max **200**; name search Participants/Artifacts/Negotiations/Offers per Product meanings |
| 2 | Single superadmin + TOTP (no email OTP fallback) | **MET** | §3.3: one superadmin `io@aiknowhow.com`; required authenticator TOTP; email OTP **NOT** 2FA fallback; recovery codes hashed |
| 3 | Turnstile only (WAF CAPTCHA OUT) | **MET** | §3.3 CAPTCHA Turnstile only ($0/mo); Option G reject WAF CAPTCHA/reCAPTCHA; §4 OUT |
| 4 | SES behind mail interface | **MET** | §3.3 Mail: SES through mail interface (port/adapter); Core must not depend on AWS SES SDK types directly |
| 5 | Soft HOLD invent password / AWS | **MET** | Header locks; §3.3 Soft HOLD invent password / Soft HOLD invent AWS account IDs, region, SES identity, from-address; §4 OUT |
| 6 | Soft HOLD build / deploy | **MET** | Header; §4 HOLD until harden live QA PASS after app image bake then separate unlock; Soft HOLD Spec invent until Soft HOLD SoR CLEAR |

**Product alignment note:** Product AC7 left “Spec decides whether email OTP is an allowed fallback.” Soft HOLD SoR **locks no email OTP fallback** (CEO). That is a valid SA design pick within Product’s “Spec/SA must cover” latitude and matches the CEO expansion CA asked Arch QA to verify. Turnstile-only, lists/search/paging, single superadmin, Soft HOLD invent password/AWS, Soft HOLD build/deploy align with Product IN/AC/OUT.

**Finance:** Soft HOLD SoR tip + Sources + §3.3 CFO Soft HOLD cost cite Finance QA **PASS** at `finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md`. Soft HOLD invent spend still. Not spend/provision approval. PoC **$0**.

## Standing locks (still hold)

| Lock | Result |
|------|--------|
| Host `admin.core.dealoware.com` only; platform ≠ core | **PASS** — §3.1 / §3.10 / §4 OUT |
| Principal **CoreOwner** | **PASS** — §3.2 |
| Option A same Core monolith + separate admin route + FieldPolicy dual wall; no parallel admin ACL | **PASS** — §2 Option A; §3.2 |
| Soft HOLD harden H1 separate | **PASS** — Soft HOLD noted; not this Soft HOLD SoR unlock |
| PoC $0; no CDK / AWS account invent in Soft HOLD SoR | **PASS** |
| App target .NET 10 noted; Soft HOLD SoR does not implement retarget; historical `net8.0` PoC notes untouched | **PASS** — Soft HOLD SoR header |

## Security Soft HOLD SoR CLEAR (formal gate)

| Gate | Status |
|------|--------|
| Checklist | **ISSUED v2** — `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` (pts 1–14; CEO expansion) |
| Soft HOLD SoR §6 | Answers 1–14 **MET** (rebound to v2) |
| Handshake Soft HOLD SoR | **qa-confirm only** — do not invent points-review Soft HOLD SoR |
| Security Soft HOLD SoR qa-confirm | **PASS 14/14 MET · 0 GAP · 0 PARTIAL** — `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (**~1:45pm ET**); Soft HOLD SoR tip amend 13:35 ET scored; prior pre-expansion Soft HOLD SoR qa-confirm (10/10) **superseded** |
| Security Soft HOLD SoR | **CLEARED** by that qa-confirm |

**Quotes (Security Soft HOLD SoR qa-confirm):**
- Verdict: **PASS** (14/14 MET, 0 GAP, 0 PARTIAL)
- Checklist: **ISSUED v2** (pts 1–14; CEO expansion)
- Soft HOLD SoR: amend 13:35 ET; §6 rebound to v2
- Handshake: Soft HOLD SoR = qa-confirm only; prior pre-expansion Soft HOLD SoR qa-confirm **superseded**
- Tally: 14 MET · 0 PARTIAL · 0 GAP
- Handshake status: Soft HOLD Architecture QA Security Soft HOLD SoR **CLEARED** — Arch QA may lift Soft HOLD Security gate after this Soft HOLD SoR

## Soft notes (non-blocking; still Soft HOLD)

- Soft HOLD Spec invent until Soft HOLD SoR CLEAR enough for Spec track / Spec reconcile (Spec draft v2.1 is input).
- Soft HOLD build / Stories / code / CDK / spend / provision / deploy.
- Soft HOLD invent spend (Finance QA PASS ≠ spend approval).
- Soft HOLD invent password / Soft HOLD invent AWS account IDs.
- Soft HOLD harden redeploy (H1) separate.
- CA design grounding PASS remains CA after this formal Arch QA PASS.
- Handshake Soft HOLD SoR = **qa-confirm only**.
- Not a build unlock. PoC **$0**.

## Verdict

**Formal Architecture QA PASS** on Soft HOLD SoR CEO amend (SA-REV-CORE-ADMIN).

- Soft HOLD SoR content: all six CEO deltas **MET**; standing locks hold; Finance QA PASS cite kept.
- Security Soft HOLD SoR **CLEARED**: checklist **ISSUED v2** + qa-confirm Soft HOLD SoR **PASS 14/14 MET** (~1:45pm ET).
- Prior pre-amend formal Arch QA PASS remains **superseded** for this amend surface.

Confirm formal PASS **to Chief Architect only**. Soft HOLD Spec invent / build / deploy. Soft HOLD invent password / AWS. Soft HOLD invent spend. Not a build unlock. PoC **$0**.

**DOC-FLOW cite:** `verification/2026-10-05__sa__verification__core-admin-dashboard-ceo-amend-review.md`
