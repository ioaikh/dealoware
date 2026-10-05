# Security QA — Architecture Soft HOLD SoR · Core admin dashboard vs Chief Security SA checklist **ISSUED v2**

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~1:45pm ET) |
| Verdict | **PASS** (14/14 MET, 0 GAP, 0 PARTIAL) |
| Asked by | Senior Security PASS 14/14 → Soft HOLD SoR qa-confirm only; Chief Architect / CPM Soft HOLD Arch QA until Soft HOLD SoR CLEAR; COO A2 Soft HOLD SoR gate |
| Chief SA checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` — **ISSUED v2** (pts 1–14; CEO expansion) |
| Senior Security points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-points-review.md` (**PASS** 14/14 MET vs ISSUED v2 — cited; independently re-scored; agrees) |
| Architecture Soft HOLD SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` (amend 13:35 ET; §6 rebound to v2; score body §§3.1–3.10 + §4 independently) |
| Prior Soft HOLD SoR qa-confirm | **SUPERSEDED** — pre-expansion tip scored old 10/10 itemization; this overwrite is Soft HOLD SoR only for v2 |
| Moment | SA-REV-CORE-ADMIN |
| Host lock | `admin.core.dealoware.com` only |
| Principal | **CoreOwner** = single system superadmin `io@aiknowhow.com` |
| PoC | **$0** |
| DOC-FLOW / Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` |

**Handshake Soft HOLD SoR = qa-confirm only.** Do **not** invent points-review Soft HOLD SoR. **Not** build / deploy / Stories / code / CDK / spend unlock. Soft HOLD invent password values. Soft HOLD invent AWS account IDs. Soft HOLD Spec invent / Soft HOLD Spec Security final until Soft HOLD SoR CLEAR. Soft HOLD harden redeploy (H1) stands separately. No AWS account / CDK / bot-platform internals invented here.

## Soft notes accepted (non-blocking)

| Soft note | Disposition |
|-----------|-------------|
| Soft HOLD Architecture QA formal PASS until Security Soft HOLD SoR CLEAR + this qa-confirm Soft HOLD SoR | **Accepted** — Security Soft HOLD SoR CLEARED by this PASS |
| Soft HOLD Spec invent / Soft HOLD Spec Security final until Soft HOLD SoR CLEAR (CEO Architecture-first) | **Accepted** |
| Soft HOLD build / Stories / code / CDK / spend / provision / deploy | **Accepted** |
| Soft HOLD invent password values in Soft HOLD SoR / docs / Spec / chat / commit history | **Accepted** — scan: none found |
| Soft HOLD invent AWS account IDs / region / SES identity / from-address / provision | **Accepted** — scan: none found |
| Handshake Soft HOLD SoR = qa-confirm only (no points-review Soft HOLD SoR) | **Accepted** — this file only |
| Prior pre-expansion Soft HOLD SoR qa-confirm (10/10) **superseded** | **Accepted** — this overwrite |
| Soft HOLD invent SSO/IdP/Cognito as delivered; Soft HOLD invent admin agents / second permission matrix / charts/warehouse | **Accepted** |
| Soft HOLD harden redeploy (H1) — separate | **Accepted** |
| Senior O1–O6 non-blocking observations | **Accepted** — not GAPs; Spec should fold O2/O4/O5/O6 on reconcile |
| Cost/critical → COO → CEO | **Accepted** |
| Not build unlock | **Accepted** |

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Chief Security SA checklist ISSUED v2 | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` | Binding pts **1–14** ISSUED |
| Senior Security points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-points-review.md` | **PASS** 14/14 — present; content aligned; not rubber-stamped |
| Architecture Soft HOLD SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` | Body §§3.1–3.10 + §4 IN/OUT/HOLD bind all 14; §6 map consistent |
| Product (binding) | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | Present (1:27 lists/search/superadmin + 1:30 Turnstile-only) |
| Spec draft v2.1 (input) | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | Present — Soft HOLD Spec invent until Soft HOLD SoR CLEAR |
| CFO Soft HOLD cost | `dealoware-kb/2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` | Present — Turnstile $0/mo; SES under $0.01/mo assumed; Soft HOLD invent spend |
| Finance QA | `finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md` | **PASS** (estimate only; not spend approval) |
| FieldPolicy dual wall | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Present — Option A §3a/§3b cite, do not rewrite |
| Arch QA interim (optional) | `verification/2026-10-05__sa__verification__core-admin-dashboard-review.md` | Present — Soft HOLD formal until Soft HOLD SoR CLEAR |
| Secret / account scan of Soft HOLD SoR | Soft HOLD SoR body | No password values; no AWS account IDs / region / SES identity / from-address / CDK internals |

## Independent re-score (Security QA)

Score vs **Chief SA checklist ISSUED v2 pts 1–14 only**. Soft HOLD SoR surface: Option A + §§3.1–3.10 + §4 + §6 as map. Verified Soft HOLD SoR **design body**, not only §6 claim table. Senior PASS cited, not rubber-stamped.

| # | Point (Chief SA checklist v2) | Senior | Security QA | Soft HOLD SoR body evidence |
|---|-------------------------------|--------|-------------|------------------------------|
| 1 | Host + single superadmin CoreOwner | MET | **MET** | §3.1: admin only at `admin.core.dealoware.com`; platform hosts not this Soft HOLD SoR. §3.2: principal **CoreOwner** = single superadmin login email **`io@aiknowhow.com`**; no multi-admin operator ACL; CoreOwner ≠ Participant UI ≠ platform admin. §2 Option A pick / reject B·E·F. §3.10 / §4 OUT: O1 human users OUT |
| 2 | Email + password + one-time bootstrap; Soft HOLD invent password | MET | **MET** | §3.3 Identity/Bootstrap: email + password for that superadmin; seed identity only; initial password via out-of-band **single-use** time-limited link (≤24h; SA may tighten). Soft HOLD invent password — **no password values** in Soft HOLD SoR/docs/Spec/chat/commit history; no embedded password or live link. Soft HOLD invent SSO/IdP as delivered (§2 C reject; §3.3 OUT; §4 OUT). Scan confirmed none |
| 3 | TOTP required; no email OTP fallback; hashed recovery codes | MET | **MET** | §3.3: TOTP required before first interactive admin session and on every login after. Email OTP **NOT** a 2FA fallback (CEO). Recovery = one-time recovery codes stored **hashed** only. Auth secrets never in API bodies/audit/logs/metrics/traces. (Senior O6: one-time display + regen invalidate — Spec fold; not GAP) |
| 4 | Password reset = email link + 2FA | MET | **MET** | §3.3 Password reset: email link **and** TOTP (or hashed recovery-code path under same 2FA rule). Soft HOLD invent password-only reset without 2FA. §3.5 audits reset request/completion |
| 5 | Turnstile only + lockout / rate limit + session | MET | **MET** | §3.3: CAPTCHA = **Cloudflare Turnstile only** on login + password-reset. AWS WAF CAPTCHA OUT; reCAPTCHA OUT (§2 G reject; §3.10; §4 OUT). Account **5 fails/15m → 30m**; IP **20 fails/15m** throttle (Senior O5: Spec pin duration). Session **8h absolute / 30m idle** sliding; after expiry full re-auth (password + TOTP). HttpOnly Secure SameSite=Strict cookie on admin host. Auth secrets never dump |
| 6 | SES behind mail interface; Soft HOLD invent AWS | MET | **MET** | §3.3 Mail: bootstrap/reset via **SES** through **mail interface** (port/adapter); Core must not depend on AWS SES SDK types directly. Soft HOLD invent AWS account IDs, region, SES identity, from-address, or provision. Sources + §4 HOLD cite CFO Soft HOLD cost (Turnstile $0/mo; SES under $0.01/mo assumed; Finance QA **PASS**) — Soft HOLD invent spend / not spend approval. Scan: none of those values in Soft HOLD SoR |
| 7 | Audit: login/reset/2FA + edit/delete; soft-deleted toggle not audited | MET | **MET** | §3.5: append-only insert-only table; no update/delete UI/API path. Mutation rows who/when/type+id/action/before/after; cascade rows + correlation id. Auth events: login success/failure (reason class only), password reset request/completion, TOTP enroll/change, recovery-code use. Same-transaction pairing (audit fail → roll back). Soft-deleted toggle use **not** audited; toggle **CoreOwner only**. Snapshots FieldPolicy-redacted; bodies >4 KiB truncated with length + hash; no secret dump |
| 8 | Fail-closed dual-wall FieldPolicy; no parallel admin ACL | MET | **MET** | §3.2 / §3.3 / §3.7: all admin reads/writes bind Domain `IFieldPolicy.Evaluate` for **CoreOwner** (Option A §3a/§3b cite, do not rewrite — source verified present). Session only after password + TOTP; missing/invalid/expired session or missing TOTP → deny all admin routes. Denied FieldClass → no write and no dump. Parallel multi-operator ACL rejected. Soft HOLD invent admin agents (§4 OUT) |
| 9 | Confirm-before-delete + cascade summary integrity | MET | **MET** | §3.4: UI modal names type + identity (display name + id) + cascade summary; cancel → no mutation and no delete audit. API: `delete-intent` → `confirmToken` + identity + cascade; delete must present token; blind DELETE rejected. Token single-use, **5 minutes**, bound to actor + entity + intended cascade set. Applies to all four entity types. (Senior O4: cascade-set drift reject — Spec fold; binding implies; not GAP) |
| 10 | Soft-delete + cascades | MET | **MET** | §3.6: marker `DeletedAt`; hard delete deferred (§4 OUT/HOLD). Offer → offer only. Negotiation → negotiation + child offers; open warn + child count. Artifact → **block** while non-deleted negotiation references it (error lists ids). Participant → Participant + negotiations + those offers (confirm counts); **do not** auto-delete Artifacts. Settlement OUT |
| 11 | All-status lists + name search + server-side paging (safe) | MET | **MET** | §3.9: negotiations/offers all statuses incl. **Withdrawn** + soft-deleted via CoreOwner-only toggle. Sort/filter locked allowlist. Offset/limit paging default **50** / max **200**; Soft HOLD invent client-only full-table dump. Name search per Product meanings on all four tables; parameterized only (no injection); FieldPolicy binds hit payloads. §3.8 stats exclude soft-deleted (`DeletedAt IS NULL`); Soft HOLD invent charts/warehouse (§4 OUT) |
| 12 | Edit write surface FieldPolicy-only; concurrency fail-closed | MET | **MET** | §3.7: only FieldPolicy-allowed fields writable; no second permission matrix. Auth secrets / raw storage credentials never writable; LoginEmail via dedicated fail-closed rotation only. §3.8: optimistic concurrency Version/ETag; stale → **409 Conflict** safe message, no silent overwrite; denied FieldClass values not echoed. Auth secrets / StrategyBody never dump into errors/logs/metrics/traces. (Senior O2: ContactEmail named in §6 only — covered by generic FieldPolicy rule; Spec fold; not GAP) |
| 13 | OUT / Soft HOLD pack | MET | **MET** | Header Status/Locks + §3.10 + §4 OUT/HOLD + §5 Rules: Soft HOLD invent Stories/code/CDK/spend/provision/deploy until harden live QA PASS after app image bake then separate unlock. Soft HOLD invent password. Soft HOLD invent AWS account IDs. Platform admin hosts OUT. Human-user list OUT. Participant UI as admin OUT. Multiple admin humans OUT. Email OTP fallback OUT. WAF CAPTCHA / reCAPTCHA OUT. App Runner OUT. Inbound bot connector after. Settlement OUT. SSO/IdP as delivered OUT. PoC **$0**. Cost/critical → COO → CEO (Senior O1: literal route line in §6; body has PoC $0 + Soft HOLD spend + COO A2; MET) |
| 14 | Traceability + handshake Soft HOLD SoR | MET | **MET** | Sources cite Product (1:27 + 1:30) + Spec draft v2.1 input + CFO Soft HOLD cost + Finance QA PASS + Option A §3a/§3b + mechanism picks §§3.3–3.9. §4 HOLD / §5 / §6: Architecture QA must **not** formal PASS until Security QA confirms via **this** Soft HOLD SoR path. Handshake Soft HOLD SoR = **qa-confirm only** — do not invent points-review Soft HOLD SoR. Prior pre-expansion Soft HOLD SoR qa-confirm **superseded**. Soft HOLD Spec invent / Soft HOLD Spec Security final until Soft HOLD SoR CLEAR. Soft HOLD build. Not a build unlock |

**Tally:** 14 MET · 0 PARTIAL · 0 GAP.

## Alignment with Senior Security

Senior SA points-review scored **PASS 14/14** vs checklist ISSUED v2 on Soft HOLD SoR Option A + §§3.3–3.9 picks with Soft HOLDs for Arch QA formal until Soft HOLD SoR CLEAR + qa-confirm Soft HOLD SoR only, Soft HOLD Spec invent / Spec Security final, Soft HOLD build, Soft HOLD invent password/AWS, Soft HOLD harden H1 separate. Independent Security QA re-score vs **same checklist v2** against Soft HOLD SoR **body** (§§3.1–3.10 + §4) **agrees** on all 14. Senior O1–O6 accepted as non-blocking. No bounce. Soft HOLD Security Soft HOLD SoR CLEARED by this file.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Host `admin.core.dealoware.com` only; principal **CoreOwner** = `io@aiknowhow.com` | Held (§3.1–§3.2) |
| Email + password + one-time bootstrap; Soft HOLD invent password values | Held (§3.3; scan clean) |
| TOTP required; no email OTP fallback; hashed recovery codes | Held (§3.3) |
| Password reset = email link + 2FA | Held (§3.3) |
| Turnstile only; lockout 5/15m→30m + IP 20/15m; session 8h/30m; HttpOnly Secure SameSite=Strict | Held (§3.3) |
| SES via mail interface; Soft HOLD invent AWS account IDs | Held (§3.3; scan clean) |
| Audit append-only incl. login/reset/2FA; toggle not audited; same-transaction | Held (§3.5) |
| FieldPolicy dual wall; no parallel admin ACL; fail-closed missing TOTP/session | Held (§3.2–§3.3/§3.7) |
| Confirm modal + API confirm token; blind DELETE rejected | Held (§3.4) |
| Soft-delete `DeletedAt` + cascade table (Artifact block; Participant no Artifact auto-delete) | Held (§3.6) |
| All-status lists + Withdrawn + offset/limit 50/200 + parameterized name search | Held (§3.9) |
| Edit FieldPolicy-only; 409 on stale; no FieldClass echo | Held (§3.7–§3.8) |
| Soft HOLD build; Soft HOLD Spec invent; Soft HOLD SoR = qa-confirm only | Held |
| PoC $0; not build unlock; no AWS/CDK/bot-platform invent | Held |

## Gaps

**None.** Soft notes non-blocking. Prior pre-expansion Soft HOLD SoR qa-confirm (10/10) **superseded** by this overwrite. Soft HOLD invent points-review Soft HOLD SoR not done. Soft HOLD build / Stories / code / CDK / spend not unlocked. Soft HOLD invent password / AWS account IDs not done. Spec track not re-scored (Soft HOLD Spec invent / Soft HOLD Spec Security final until Soft HOLD SoR CLEAR).

## Handshake status

Security QA → **PASS** Soft HOLD SoR confirm vs checklist **ISSUED v2** pts 1–14. Soft HOLD Architecture QA Security Soft HOLD SoR **CLEARED** — Arch QA may lift Soft HOLD Security gate after this Soft HOLD SoR. Soft HOLD Spec invent / Soft HOLD Spec Security final until Soft HOLD SoR CLEAR + Spec reconcile. Soft HOLD build / Stories / code / CDK / spend / provision / deploy. Soft HOLD invent password. Soft HOLD invent AWS account IDs. Soft HOLD invent SSO/IdP/Cognito / admin agents. Soft HOLD harden redeploy (H1) stands separately. Handshake Soft HOLD SoR = **qa-confirm only**. Prior Soft HOLD SoR qa-confirm **superseded**. Not build unlock. PoC **$0**. Cost/critical: none from this Soft HOLD SoR → COO → CEO if any later spend.

**Next:** Chief Security PASS/HOLD to CPM + CA + COO (A2 Soft HOLD SoR CLEAR → CA formal Arch QA → A3 Spec reconcile). Soft HOLD BM Spec+SA until CLEAR per CPM.
