# Finance QA — Soft HOLD SES + Turnstile estimate

**PASS.** Estimate only. PoC $0. Not spend or provision approval.
QA by Dealoware Finance QA, 2026-10-05 ~1:35pm ET.
Source under review: `/workspace/dealoware-kb/2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md`

## Verdict
PASS against the Soft HOLD scope (Turnstile Free + SES pay-per-use ballpark for one Core admin superadmin at admin.core.dealoware.com). WAF CAPTCHA correctly out of scope. Cited rates and math match the public pages read today. Unverified items are marked; no invented numbers found.

## Evidence checked

### Turnstile — $0/month
Re-read https://developers.cloudflare.com/turnstile/plans/ (page Last updated Aug 14, 2026) on 2026-10-05:
- Free plan Pricing: Free
- Up to 20 widgets; unlimited challenges; 10 hostnames per widget
- Usable without other Cloudflare services
- Enterprise is Contact Sales only
Need of 1 widget on admin.core.dealoware.com is inside Free limits. **$0.00/month confirmed.**

### SES — < $0.01/month at assumed volume
Re-read https://aws.amazon.com/ses/pricing/ on 2026-10-05:
- À la carte outbound: $0.10 / 1,000 emails (per recipient) — matches
- Essentials 0–10M: $0.16 / 1,000 emails; no fixed fee — matches
- Pro $105/mo and Enterprise $500/mo fixed fees — matches; estimate correctly excludes
- Essentials default for new accounts / account×region with no metered SES activity since 2025-06-01, starting 2026-07-21 — matches
- Attachment data $0.12/GB — matches
- Dedicated IPs Standard $24.95/month/IP; Mail Manager Open Ingress Endpoint $50/month — matches always-on gate exclusions
- AWS Free Tier: up to $200 credits for new customers; no legacy free-email allowance on the page — matches estimate’s “no free tier assumed”

Math rechecked:
| Line | Recheck | Status |
|---|---|---|
| À la carte 5 emails | 5 × $0.10 / 1,000 = $0.0005 | OK |
| Essentials 5 emails | 5 × $0.16 / 1,000 = $0.0008 | OK |
| Attachments 0 GB | $0.00 | OK |
| 100 emails sensitivity | $0.01 / $0.016 | OK |
| Rounded total | < $0.01 → $0.00 | OK |

### Scope / gates
- WAF CAPTCHA out — consistent with CEO cancel of FIN-2026-10-05-01
- Host name admin.core.dealoware.com — matches CEO standing host rule
- Estimate-only / PoC $0 / not spend or provision — stated
- Always-on gate note: SES à la carte or Essentials has no fixed fee; Turnstile Free has none; Pro/Enterprise, dedicated IP, and Mail Manager ingress correctly flagged as needing a new estimate

## Unverified (acceptably disclosed)
- Which SES rate basis applies to Dealoware’s account/region
- Monthly email volume (finance assumption of 5)
- Whether Free Tier credits remain
- Sandbox/production access, DNS (SPF/DKIM/DMARC), and Core admin compute (explicitly not covered)

## Not done
- No AWS account calls, no provision, no spend approval
- Did not message CPM
