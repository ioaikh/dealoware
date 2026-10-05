# Core admin Soft HOLD — SES + Turnstile cost note

**Soft HOLD design SoR verification path. Estimate only. PoC $0. Not spend or provision approval. Finance QA has NOT signed.**
Prepared by CFO desk, 2026-10-05. Public list prices only; no AWS calls; no account IDs/ARNs.

## Scope
- IN: Amazon SES (password-reset / bootstrap email for the single Core admin superadmin at admin.core.dealoware.com); Cloudflare Turnstile (CAPTCHA on login/reset).
- OUT: AWS WAF CAPTCHA — dropped per CEO decision (2026-10-05, 1:30pm ET). Not estimated.

## Turnstile — $0/month (CEO figure confirmed)
- Cloudflare Turnstile Free plan: Pricing "Free"; up to 20 widgets per account; **unlimited challenges** (traffic or verification requests); 10 hostnames per widget; usable without other Cloudflare services.
- Need: 1 widget on admin.core.dealoware.com (login + reset). Well inside the free limits.
- **Result: $0.00/month.** Enterprise tier (contact sales) not needed.

## SES — ≈ $0.00/month at assumed volume
- **Finance assumption (not a product requirement):** 5 outbound emails/month (a few resets + occasional bootstrap), 1 recipient each, no attachments, us-east-1.
- SES has two possible rate bases; which applies depends on the account's SES plan status (unverified):
  - À la carte outbound: $0.10 per 1,000 emails (charged per recipient).
  - Essentials plan (default for new accounts / account×region with no metered SES activity since 2025-06-01, starting 2026-07-21): $0.16 per 1,000 emails, no fixed fee.
  - Pro ($105/mo) and Enterprise ($500/mo) plans have fixed fees — **do not select**; not needed.

| Line | Math | $/month |
|---|---|---|
| Outbound, à la carte | 5 × $0.10 / 1,000 | $0.0005 |
| Outbound, Essentials | 5 × $0.16 / 1,000 | $0.0008 |
| Attachment data | 0 GB × $0.12/GB | $0.00 |
| **Total (either basis)** | | **< $0.01 (rounds to $0.00)** |

- Sensitivity: even 100 emails/month = $0.01 (à la carte) / $0.016 (Essentials).
- Free tier: the cited page lists only AWS Free Tier credits (up to $200) for **new** AWS customers; the older "free emails per month" allowance is not on the page. No free tier assumed.
- Core may run off AWS: if the sender is not on EC2, no EC2 data-transfer line. Behind a mail interface, so a swap to another provider changes this line only.

## Always-on gate note
Rule: no new always-on resource without a CFO estimate. SES (pay-per-email; no fixed fee on à la carte or Essentials) and Turnstile (Free plan) are **not always-on compute**. Gate stays satisfied provided no SES Pro/Enterprise plan, dedicated IP ($24.95/mo/IP), or Mail Manager ingress endpoint ($50/mo) is added — each of those would need a new CFO estimate.

## Sources (read 2026-10-05)
- Amazon SES pricing: https://aws.amazon.com/ses/pricing/
- Cloudflare Turnstile plans: https://developers.cloudflare.com/turnstile/plans/ (page "Last updated Aug 14, 2026")

## Unverified
- Which SES rate basis applies to Dealoware's account/region (à la carte vs Essentials default). Both computed; difference < $0.001/month.
- Monthly email volume — finance assumption only.
- Whether the account still has AWS Free Tier credits — not assumed.
- Not covered (no cost estimated here): SES sandbox/production-access approval, domain DNS (SPF/DKIM/DMARC) setup, any compute hosting the Core admin.
