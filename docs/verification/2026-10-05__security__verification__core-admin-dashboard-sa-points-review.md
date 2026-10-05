# Security points-review: Architecture (SA) · Core admin dashboard (checklist v2)

| Field | Value |
|-------|--------|
| Status | **PASS** (14/14 MET, 0 GAP, 0 PARTIAL). Soft HOLD Architecture QA **formal** PASS until Security Soft HOLD SoR CLEAR via Security QA **qa-confirm Soft HOLD SoR only**. Soft HOLD Spec invent. Soft HOLD Spec Security final. Soft HOLD build. |
| Date | 2026-10-05 (~1:50pm ET) |
| Author | Dealoware Senior Security |
| Checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md`: **ISSUED v2** (CEO expansion; pts 1–14) |
| Soft HOLD SoR scored | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` (amend 13:35 ET; §6 rebound to v2) |
| Moment | **SA-REV-CORE-ADMIN** |
| Prior SA Soft HOLD SoR qa-confirm | **Superseded** (pre-expansion tip). A fresh Security QA qa-confirm against v2 is required |
| Prior points-review (10-pt, amended v1) | **Overwritten** by this file |
| Expected handshake Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (Security QA writes; **qa-confirm only**) |
| CFO Soft HOLD cost (cited by SoR) | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md`: Turnstile $0/mo; SES under $0.01/mo assumed; Finance QA PASS. Estimate only, Soft HOLD invent spend |
| Host | `admin.core.dealoware.com` only |
| Principal | **CoreOwner** = `io@aiknowhow.com` (single system superadmin) |
| PoC | **$0** |
| DOC-FLOW | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-points-review.md` |

**This points-review is NOT the Soft HOLD SoR.** It is Senior Security scoring evidence only (content). Handshake Soft HOLD SoR = **qa-confirm only** at the path above. No points-review Soft HOLD SoR is invented here.

**Not a build / deploy / Stories unlock.** Soft HOLD harden redeploy (H1) stands separately. This review contains no passwords, no AWS account IDs, and no CDK or bot-platform internals.

**Spec:** The Spec PRE-SCORE (Soft HOLD final) at `verification/2026-10-05__security__verification__core-admin-dashboard-spec-points-review.md` stands separately. **No Spec Security PASS is claimed here.**

## Method

I scored each point against the SoR **body** (§§1–4, §§3.1–3.10, Sources, header Locks) and checked the §6 cites independently. §6 self-map text was not accepted on its own. I opened each cited source and confirmed it exists: Product scope note (1:27pm lists/search/superadmin + 1:30pm Turnstile-only), Spec v2.1 input, CFO estimate, Finance QA, Option A §3a/§3b (`architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md`), Step 3 SA, and H2/H3 check. I also scanned the SoR for password values, AWS account IDs, region, SES identity, from-address, and CDK internals. **None found.**

## Verdict

**PASS: 14/14 MET** against SA checklist **ISSUED v2**. Option A binds every point with concrete body picks:
- §3.3: email + password + one-time bootstrap ≤24h, required TOTP, no email OTP fallback, hashed recovery codes, reset = link + 2FA, Turnstile only, 5/15m→30m account lockout and 20/15m IP throttle, 8h/30m session, SES behind a mail interface
- §3.2: fail-closed FieldPolicy dual wall for CoreOwner
- §3.4: modal confirm + 5-minute bound confirm token
- §3.5: append-only same-transaction audit including auth events; toggle not audited
- §3.6: `DeletedAt` soft-delete + cascades
- §§3.7–3.8: FieldPolicy-only edits + 409 concurrency + stats scrub
- §3.9: all-status lists incl. Withdrawn, offset/limit 50/200, parameterized name search on all four tables
- §4 / §3.10: OUT/HOLD pack

There are six non-blocking observations below (O1–O6). None is a GAP.

## Score table (checklist v2, pts 1–14)

| # | Point (exact checklist title) | Result | Soft HOLD SoR body cites | Evidence note |
|---|-------------------------------|--------|--------------------------|---------------|
| 1 | Host + single superadmin CoreOwner | **MET** | header Host; §2 Option A (reject B/E/F); §3.1; §3.2; §3.10; §4 IN/OUT | Admin only at `admin.core.dealoware.com`. `admin.platform` / `platform` / `api.platform` are not this SoR (§3.1). **CoreOwner** = single superadmin `io@aiknowhow.com`, no multi-admin operator ACL (§3.2). CoreOwner ≠ Participant UI ≠ platform admin (§3.2, §3.10). O1 human users dropped (§2 reconcile; §4 OUT) |
| 2 | Email + password + one-time bootstrap; Soft HOLD invent password | **MET** | §3.3 Identity/Bootstrap; §3.10; §4 OUT/HOLD | Email + password for one superadmin. Seed identity only. Initial password set only via out-of-band **single-use**, time-limited link (≤24h; SA may tighten). No password values in SoR/docs/Spec/chat/commit history, and no embedded password or live link. SSO/IdP/Cognito as delivered OUT (§2 C reject; §3.3 OUT; §4 OUT). Scan: no password values present |
| 3 | TOTP required; no email OTP fallback; hashed recovery codes | **MET** | §3.3 TOTP/Email OTP/Recovery/Credential storage | TOTP is required on every login after enrollment, and the first interactive admin session cannot start until TOTP is enrolled. Email OTP is **NOT** a 2FA fallback (CEO). This resolves Product's "Spec decides" in favor of no fallback. Recovery = one-time codes **hashed at rest**. Auth secrets never appear in API bodies, audit, logs, metrics, or traces. TOTP secret is encrypted/protected. (O6) |
| 4 | Password reset = email link + 2FA | **MET** | §3.3 Password reset; §3.5 auth events | Reset requires email link **plus** TOTP, or the recovery-code path under the same 2FA rule. Password-only reset without 2FA is forbidden. Reset request and completion are audited (§3.5) |
| 5 | Turnstile only + lockout / rate limit + session | **MET** | §3.3 CAPTCHA/Session store/Session lifetime/Lockout/Credential storage; §2 G reject; §3.10; §4 OUT | **Cloudflare Turnstile only** on login + reset. AWS WAF CAPTCHA, reCAPTCHA, and any other provider OUT. Account **5 fails/15m → 30m** lock. IP **20 fails/15m** → throttle (O5). Failures counted by reason class only. Server-side session row + **HttpOnly Secure SameSite=Strict** cookie on the admin host. **8h absolute / 30m idle** sliding; after either expiry, full re-auth (password + TOTP). Auth secrets never in API bodies/audit/logs/metrics/traces |
| 6 | SES behind mail interface; Soft HOLD invent AWS | **MET** | §3.3 Mail/CFO Soft HOLD cost; Sources; §4 HOLD | Bootstrap/reset mail goes via **SES** through a **mail interface** (port/adapter). Core must not depend on AWS SES SDK types directly, so Core may run off AWS. Account IDs, region, SES identity, from-address, and provision are not invented. CFO note cited (Turnstile $0/mo; SES under $0.01/mo at assumed 5/mo; Finance QA PASS) as estimate only, not spend or provision approval. Scan: none of those values appear in the SoR (O3) |
| 7 | Audit: login/reset/2FA + edit/delete; soft-deleted toggle not audited | **MET** | §3.5 (all rows); §3.3 Credential storage; §3.6 List default; §3.9 Soft-deleted rows | Append-only insert-only table, with no update/delete UI or API path. Mutation rows cover who/when/type+id/action/before/after. Each cascade gets its own row with a correlation id. Auth events: login success, login failure (reason class only), reset request, reset completion, TOTP enroll, TOTP change, recovery-code use. Mutations pair with audit in the **same transaction** (audit fail → roll back). Toggle use **not** audited, and the toggle is **CoreOwner only** (§3.5, §3.6, §3.9). Snapshots are FieldPolicy-redacted with no auth secrets. Bodies >4 KiB are truncated with length + hash |
| 8 | Fail-closed dual-wall FieldPolicy; no parallel admin ACL | **MET** | §3.2 FieldPolicy/Dual wall/Fail-closed; §3.3 Role claim attach/Fail-closed; §3.7 lead; Sources (Option A §3a/§3b); §4 OUT | All admin reads/writes bind `IFieldPolicy.Evaluate` for **CoreOwner**, citing Option A §3a/§3b (verified present) without rewriting. A session is only created after password **+ TOTP** verify, so missing TOTP means no session and no claim. Missing claim, expired/invalid session, non-owner, or locked account → deny all admin routes. Denied FieldClass → no write, no dump. Parallel admin ACL, admin mesh, and admin agents are OUT |
| 9 | Confirm-before-delete + cascade summary integrity | **MET** | §3.4; §3.6 cascade/open-negotiation | UI modal names type + identity (display name + id) + cascade summary. Cancel → no mutation and no delete audit. API: `delete-intent` returns `confirmToken` + identity + cascade summary, and delete must present that token. Blind DELETE is rejected. Token is single-use, **5 minutes**, bound to actor + entity + intended cascade set. Applies to all four entity types, UI and API (O4) |
| 10 | Soft-delete + cascades | **MET** | §3.6; §4 IN/OUT/HOLD | `DeletedAt` marker by default. Hard delete deferred entirely (§4 OUT/HOLD). Offer → offer only. Negotiation → negotiation + child offers, with an open warning stating the child count. Artifact → **block** while a non-deleted negotiation references it, and the error lists the ids. Participant → Participant + its negotiations + those offers, confirm shows counts, and Artifacts are **not** auto-deleted. Settlement/escrow OUT |
| 11 | All-status lists + name search + server-side paging (safe) | **MET** | §3.9 (all rows); §3.8 Stats predicates; §3.6 List default; §4 IN/OUT | Every status is listed: Open/Accepted/Declined/Expired/**Withdrawn** as a first-class enum, plus soft-deleted via the CoreOwner-only toggle, for both negotiations and offers. Sort/filter is a fixed allowlist (status, created/updated, participant, artifact, value/price, negotiation id). Server-side **offset/limit**, default **50** / max **200**. Client-side full-table load forbidden. Name search on all four tables matches the Product composites (verified against Product §5–7). Queries are parameterized only, and FieldPolicy binds hit payloads. All five stats use `DeletedAt IS NULL`. Charts/warehouse OUT |
| 12 | Edit write surface FieldPolicy-only; concurrency fail-closed | **MET** | §3.7 (lead, table, Reads); §3.8 Concurrency/Validation | Every write is fail-closed through FieldPolicy for CoreOwner, with **no second permission matrix**. Per-entity editable sets are gated "if FieldPolicy Write allows". Auth secrets and raw storage credentials are never writable. LoginEmail changes only via dedicated fail-closed rotation. Optimistic concurrency uses `Version`/ETag, and a stale write returns **409 Conflict** with a safe message and no silent overwrite. Denied FieldClass values are not echoed in validation errors. Auth secrets and StrategyBody are never dumped into logs/metrics/traces (O2) |
| 13 | OUT / Soft HOLD pack | **MET** | header Status/Locks; §3.10; §4 OUT/HOLD; §5 Rules; §8 | Stories/code/CDK/spend/provision/deploy held until harden live QA PASS after app image bake, then a separate unlock. Password and AWS account IDs not invented. Platform admin hosts, human-user list, Participant UI as admin, multiple admin humans (§3.2/§3.3 OUT/§4 parallel ACL), email OTP fallback, WAF CAPTCHA/reCAPTCHA, App Runner, settlement, and SSO/IdP as delivered are all OUT. Inbound bot connector comes after. PoC **$0** (O1) |
| 14 | Traceability + handshake Soft HOLD SoR | **MET** | header; Sources; §3.2; §§3.3–3.9; §4 HOLD; §5; §6 preamble; §9 | Cites Product (1:27 + 1:30; verified in Product status row), Spec v2.1 input (verified revision row), CFO cost + Finance QA PASS, Option A §3a/§3b, and mechanism picks §§3.3–3.9. Arch QA formal PASS is held until Security QA qa-confirm via `…core-admin-dashboard-sa-qa-confirm.md` (§4 HOLD; §5; §6). Handshake = **qa-confirm only**, with no points-review Soft HOLD SoR (header; §6; §9). Prior pre-expansion qa-confirm marked **superseded** (§6; §9). Spec invent and Spec Security final held until Soft HOLD SoR CLEAR (§4 HOLD; §8). Not a build unlock (O3) |

**Tally:** 14 MET · 0 PARTIAL · 0 GAP.

## Observations (non-blocking; not GAPs)

- **O1 (pt 13):** The literal routing line "Cost/critical → COO → CEO" appears only in the §6 row 13 self-map. The body has PoC $0, Soft HOLD spend, "COO A2 critical path" (header), and the CFO cite. It is a governance route, not a design mechanism, so it is scored MET. Recommend the Senior Architect add one line to §4 HOLD on next touch.
- **O2 (pt 12):** `ContactEmail` is named only in §6 row 12. The body covers it through §3.7's generic fail-closed FieldPolicy write rule (not in any editable set) and §3.8 (no echo). Spec should name it explicitly.
- **O3 (pts 6/14):** The Finance QA path (`finance-out/…`) and H2/H3 check path (`architecture/2026-10-05__sa__architecture__h2-h3-db-tls-login-split-check.md`) resolve at the workspace root, not inside the KB tree. Both files exist, and Finance QA reads PASS. Separately, the CFO estimate header still says "Finance QA has NOT signed", which is stale relative to the Finance QA PASS. This is Finance/KB housekeeping, not a security gap.
- **O4 (pt 9):** A delete whose actual cascade set differs from the token-bound set (for example, a new child offer created after intent) should be **rejected**. The binding implies this, but it is not stated as an explicit reject. Spec should state it.
- **O5 (pt 5):** "IP throttle for the same window class" leaves the throttle duration loose. Spec should pin it (for example, 15m).
- **O6 (pt 3):** One-time display of recovery codes at generation, plus regeneration invalidating old codes, is not stated. Spec should state both. The hashed-at-rest rule already holds.

## Soft HOLDs

1. Soft HOLD Architecture QA **formal** PASS until Security Soft HOLD SoR CLEAR via Security QA **qa-confirm Soft HOLD SoR only**.
2. Handshake Soft HOLD SoR = **qa-confirm only**. This points-review is **not** the Soft HOLD SoR. Prior pre-expansion SA qa-confirm **superseded**.
3. Soft HOLD Spec invent and Soft HOLD **Spec Security final** until SA Soft HOLD SoR CLEAR + Arch QA + CFO + Spec reconcile. Spec PRE-SCORE (Soft HOLD final) stands separately. **No Spec Security PASS claimed.**
4. Soft HOLD build / Stories / code / CDK / spend / provision / deploy until harden live QA PASS after app image bake, then a separate unlock.
5. Soft HOLD invent password. Soft HOLD invent AWS account IDs / region / SES identity / from-address.
6. Soft HOLD harden redeploy (H1): separate.
7. Soft HOLD invent SSO/IdP/Cognito as delivered, admin agents, a second permission matrix, and charts/warehouse.

## Cost/critical

This Architecture or review triggers no build, deploy, CDK, provision, or spend. The CFO note is an estimate only (Turnstile $0/mo; SES under $0.01/mo assumed; Finance QA PASS) and is not spend approval. PoC **$0**. Cost/critical → COO → CEO.

## Handshake next

1. Senior Security **PASS 14/14** (this file, checklist v2) → **Security QA** confirms via Soft HOLD SoR `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` **only** (qa-confirm; supersedes the pre-expansion qa-confirm).
2. Security QA Soft HOLD SoR CLEAR → Chief Security PASS/HOLD to CPM + CA.
3. Soft HOLD Architecture QA formal PASS until that Soft HOLD SoR CLEAR.
4. Soft HOLD Spec Security final until after SA Soft HOLD SoR CLEAR + Spec reconcile. O2/O4/O5/O6 should then fold into Spec.
5. Soft HOLD build. PoC $0.

## Not build unlock

This points-review is Architecture Security scoring evidence only. It does **not** authorize merge, deploy, AWS writes, provision, Stories, code, CDK, or spend, and it is not a Spec Security PASS.
