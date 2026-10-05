# Security QA Soft HOLD SoR — Dev Plan · Core admin dashboard (Soft HOLD SoR qa-confirm)

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~2:05pm ET; refresh cite Senior PASS 14/14) |
| Verdict | **PASS** (14/14 MET, 0 GAP, 0 PARTIAL) |
| Soft HOLD SoR role | **This file is Soft HOLD SoR** (handshake Soft HOLD SoR = qa-confirm only). Do **not** invent Soft HOLD SoR from points-review. |
| Asked by | CPM Soft HOLD SoR qa-confirm; Senior Security PASS 14/14 points-review filed → refresh Soft HOLD SoR cite; Chief Security Soft HOLD invent process-global auth flood limiter until approved |
| Checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` (**ISSUED**, pts 1–14) |
| Plan tip | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` |
| Plan tip sha256 | `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf` — **MATCH** (verified `sha256sum` before score) |
| Senior Security points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-points-review.md` (**PASS** 14/14 MET — cited; independently re-confirmed vs plan tip + Senior review; agrees; **not** Soft HOLD SoR) |
| Sibling Spec Soft HOLD SoR (cite only) | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` (**PASS** 15/15) — Spec not re-scored |
| Sibling SA Soft HOLD SoR (cite only) | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (**PASS** 14/14) — Arch not re-scored |
| Host lock | `admin.core.dealoware.com` only |
| Principal | **CoreOwner** = single system superadmin `io@aiknowhow.com` |
| PoC | **$0** |
| DOC-FLOW / Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` |
| QA twin | `qa/2026-10-05__qa__qa-report__core-admin-dashboard-devplan-qa-confirm.md` |

**Handshake Soft HOLD SoR = qa-confirm only.** Do **not** invent points-review Soft HOLD SoR. Soft HOLD invent Soft HOLD SoR as PASS claim from any points-review alone. **Not** build / deploy / Stories / code / CDK / spend unlock. Soft HOLD invent password values. Soft HOLD invent AWS account IDs. Soft HOLD invent process-global auth flood limiter until Chief Security explicitly approves. Soft HOLD A7 until H4 live PASS. Soft HOLD harden redeploy (H1) separate. Soft HOLD Dev Plan QA PASS until this Soft HOLD SoR CLEAR (checklist Status). No AWS account / CDK / bot-platform internals invented here. Names only.

## Soft notes accepted (non-blocking)

| Soft note | Disposition |
|-----------|-------------|
| Soft HOLD Dev Plan QA PASS until Security Soft HOLD SoR qa-confirm CLEAR | **Accepted** — Security Soft HOLD SoR CLEARED by this PASS |
| Handshake Soft HOLD SoR = qa-confirm only; do not invent points-review Soft HOLD SoR | **Accepted** — Soft HOLD SoR is this qa-confirm only; Senior points-review is content evidence, **not** Soft HOLD SoR |
| Senior points-review PASS 14/14 filed after initial Soft HOLD SoR | **Accepted** — Soft HOLD SoR refreshed to cite; independent plan-body score unchanged PASS 14/14; Senior nit on §2/§5 "hashed IP" wording non-blocking (binding Steps 5/8 are keyed HMAC) |
| Soft HOLD invent process-global auth flood limiter until Chief Security explicitly approves | **Accepted** — plan Step 5 / Locked #6 / Step 12 Soft HOLD; per-IP + per-account only this slice |
| Soft HOLD invent password values in plan / Stories / fixtures / README / chat / commit history | **Accepted** — plan Soft HOLDs; scan: none invented here |
| Soft HOLD invent AWS account IDs / region / SES identity / from-address / Turnstile site keys / provision / spend | **Accepted** — plan Soft HOLDs; none invented here |
| Soft HOLD invent Stories / code / CDK / spend / provision / **deploy** | **Accepted** |
| Soft HOLD A7 until H4 live PASS | **Accepted** — Step 12 / Explicit OUT |
| Soft HOLD harden redeploy (H1) separate | **Accepted** |
| Soft HOLD invent HMAC key value / Turnstile secrets in plan/docs/chat | **Accepted** |
| After Dev Plan QA PASS → A6 Test design to Chief QA before Stories/code | **Accepted** — plan Status / Step 12 |
| Not build unlock; PoC $0; Cost/critical → COO → CEO | **Accepted** |

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Chief Security Dev Plan checklist ISSUED | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` | Binding pts **1–14** ISSUED |
| Plan tip | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` | Body §§1–10 + Steps 1–13 + Locked + Explicit OUT + §6 weave; sha256 **MATCH** |
| Senior Dev Plan points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-points-review.md` | **PASS** 14/14 — present; aligned; not rubber-stamped; **not** Soft HOLD SoR |
| Sibling Spec Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` | Cite-only PASS 15/15 — Spec not re-scored |
| Sibling SA Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` | Cite-only PASS 14/14 — Arch not re-scored |

## Independent re-score (Security QA Soft HOLD SoR)

Score vs **checklist pts 1–14** against **plan body** (Locked §3, Steps 1–13, Explicit OUT §7, Cost/critical §8, Done-list §9) — §6 bind table alone is **not** the sole score basis. Hard Soft HOLDs verified in step bodies and verify checklists.

| # | Point | Security QA | Plan cites | Evidence |
|---|-------|-------------|------------|----------|
| 1 | Host + single CoreOwner gate | **MET** | Step 1; Step 11; Step 12 OUT; Locked #1–#2; Header Host/Principal | Admin UI/API only at `admin.core.dealoware.com`. Soft HOLD invent platform-admin host tasks. Single superadmin `io@aiknowhow.com` → **CoreOwner**. Soft HOLD invent multi-admin / operator ACL / human-user list on Core / Participant UI-as-admin. Fail-closed deny non-CoreOwner (Step 11). |
| 2 | Bootstrap + email/password (no invent password) | **MET** | Step 2; Step 12; Locked #2, #14 | Single-use timed OOB bootstrap link (≤24h); first password set **only** via that link; TOTP enrollment **before** first session. Soft HOLD invent password values in Stories/fixtures/README/chat/commit history — env/placeholders only. Soft HOLD invent SSO/IdP as delivered. Verify checklist covers no password value + first session blocked until TOTP. |
| 3 | TOTP required; no email OTP; hashed recovery | **MET** | Step 3; Locked #2 | Authenticator-app TOTP on **every** login after enrollment. Soft HOLD invent email OTP fallback — **NOT allowed**; verify step proves email OTP path absent. Recovery codes: show once, store **hashed**, single-use. No plaintext recovery / TOTP secrets in logs/UI dump/audit/plan. |
| 4 | Password reset = email link + 2FA | **MET** | Step 4 | Reset requires **both** single-use short-lived email link (**1 hour**) **and** 2FA (TOTP or unused recovery code). Soft HOLD invent reset that skips 2FA. Reset mail carries **link only** — never a password value. Reset completion audited (ties Step 8). |
| 5 | Turnstile only + lockout/rate + session | **MET** | Step 5; Step 12 OUT; Locked #3, #6 | **Cloudflare Turnstile only** on login, password-reset, bootstrap password-set. Soft HOLD invent Turnstile account/site keys. Soft HOLD invent AWS WAF CAPTCHA / reCAPTCHA / other CAPTCHA. Lockout: 5 fails/account/15m → 30m; 20 fails/IP/15m → 30m throttle; same per-IP on reset-request. Session: server-side Postgres; HttpOnly Secure SameSite=Strict; idle **30m**; absolute **8h**; after expiry full re-auth (password + TOTP). **Soft HOLD invent process-global auth flood limiter** until Chief Security explicitly approves — **per-IP + per-account only** this slice (Locked #6; Step 5 Process-global row; Step 12). |
| 6 | Raw IP counters only; audit IP = keyed HMAC-SHA256 | **MET** | Step 5 Raw IP; Step 8 IP in auth audit; Locked #5 | Raw client IP lives **only** in short-lived rate-limit counters (expire with 15-/30-minute windows); **never** written to audit, other logs, or metrics. Auth audit stores IP **only** as keyed **HMAC-SHA256** (server-side rotatable secret) + failure reason class. Soft HOLD invent unkeyed hash or raw IP in audit/logs/metrics. Soft HOLD invent HMAC key value in plan/docs/chat. |
| 7 | SES behind mail interface; Soft HOLD invent AWS | **MET** | Step 6; Locked #4, #13; Cost/critical §8 | Bootstrap/reset mail through **mail interface** (port/adapter). Soft HOLD invent direct AWS SES SDK dependency / SES types in Core app. Soft HOLD invent AWS account IDs, region, SES identity, from-address, provision, spend. CFO Soft HOLD cost cite only (Turnstile $0/mo; SES under $0.01/mo assumed) — Finance QA PASS cited; **not** spend approval. PoC **$0**. |
| 8 | Audit: auth + edit/delete; soft-deleted toggle not audited | **MET** | Step 8; Step 7 toggle; Steps 9–10; Locked #10 | Append-only audit table; insert-only; no update/delete from admin UI/API. Auth events: login_success/fail (reason class), reset_request/complete, totp_enroll/change, recovery_code_use. Entity events: every successful admin edit/delete (+ cascade rows with correlation id). Soft-deleted **toggle use not audited**; toggle **CoreOwner-only**. Same-txn pairing: audit fail → roll back. Snapshots FieldPolicy-allowed only; Soft HOLD invent secret dump. |
| 9 | Fail-closed FieldPolicy dual wall; no parallel ACL | **MET** | Steps 1, 9, 11, 12; Locked #8 | All admin reads/writes through Domain FieldPolicy for **CoreOwner** (Option A §3a/§3b — cite, do not rewrite). Missing/invalid/expired session or missing TOTP → deny all admin routes. Denied FieldClass → no write and no dump. Soft HOLD invent parallel multi-operator ACL / second permission matrix. |
| 10 | Confirm-before-delete + soft-delete cascades | **MET** | Step 10; Locked #9 | UI modal confirm (type + identity + cascade summary); API confirm-token path (single-use **5 min**, bound to actor + entity + cascade set); blind DELETE rejected; cancel → no mutation / no delete audit. Soft-delete default (`DeletedAt`); hard delete deferred. Cascades: offer; negotiation→child offers; Artifact block-while-referenced; Participant→negotiations/offers, not auto Artifacts. Soft HOLD invent settlement cascades. |
| 11 | Lists / search / paging safe | **MET** | Step 7; Locked #7 | All-status negotiation/offer lists incl. `Withdrawn`; soft-deleted via CoreOwner-only toggle; sort/filter **server-side**; server-side paging default **50** / max **200**; Soft HOLD invent client-only full-table dump; name search case-insensitive contains on all four tables; parameterized; results never include FieldPolicy-denied fields. Stats exclude soft-deleted. Soft HOLD invent charts/warehouse. No human-user list on Core. |
| 12 | Edit write surface + concurrency | **MET** | Step 9; Locked #8 | Only FieldPolicy-allowed fields writable; every successful edit audited with before/after (FieldPolicy-allowed only). Optimistic concurrency via `Version` / `UpdatedAt` ETag; stale → **409 Conflict**; no silent overwrite. Soft HOLD invent password/secret fields in edit forms, errors, or audit snapshots. |
| 13 | OUT / Soft HOLD / A7 gate pack | **MET** | Step 12 Explicit Soft HOLD/OUT; Explicit OUT §7; Locked #12–#14; Cost/critical; Status | Soft HOLD invent Stories/code/CDK/spend/provision/**deploy** until Dev Plan QA + Test design PASS. Soft HOLD A7 until **H4 live PASS** (no invented H4 steps). Soft HOLD harden redeploy (H1) separate. Soft HOLD invent password / AWS IDs / process-global flood limiter. Platform admin hosts OUT; human-user list OUT; Participant UI as admin OUT; multiple admin humans OUT; email OTP OUT; WAF CAPTCHA / reCAPTCHA OUT; inbound bot connector after; settlement OUT; SSO/IdP OUT. PoC **$0**. Cost/critical → COO → CEO. |
| 14 | Traceability + handshake Soft HOLD SoR | **MET** | §6; Sources §1; Status; Done-list §9; Handshake note | Plan cites Spec v2.2 tip sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` + Spec Security Soft HOLD SoR PASS 15/15 + SA Soft HOLD SoR PASS 14/14 + this Dev Plan checklist. Done-list: Soft HOLD SoR qa-confirm CLEAR at this path required before Dev Plan QA PASS. Handshake Soft HOLD SoR = **qa-confirm only** — do not invent points-review Soft HOLD SoR. Soft HOLD invent Stories/code/deploy from checklist alone. Soft HOLD A7 until H4. After Dev Plan QA PASS → A6 Test design before Stories/code. Not build unlock. PoC **$0**. |


## Alignment with Senior Security done-list

Senior Dev Plan points-review (**PASS** 14/14 MET, 0 GAP) scored plan **body** Steps 1–13 vs checklist with Soft HOLDs for Soft HOLD invent process-global flood limiter, Soft HOLD invent password/AWS, Soft HOLD A7 until H4, Soft HOLD H1 separate, Soft HOLD deploy, Soft HOLD Dev Plan QA until Soft HOLD SoR CLEAR. Independent Soft HOLD SoR re-score vs same plan tip sha256 `5fefb550…` **agrees** on all 14. Senior non-blocking nit (§2/§5 "hashed IP" wording vs Steps 5/8 keyed HMAC) **accepted**. Soft HOLD SoR remains this qa-confirm only — points-review is **not** Soft HOLD SoR. Soft HOLD Security Soft HOLD SoR CLEARED stands.

## Guardrails (hard spot-check)

| Guardrail | Status | Plan body cite |
|-----------|--------|----------------|
| Host `admin.core.dealoware.com` only | **Held** | Header; Step 1; Step 12 OUT |
| CoreOwner + FieldPolicy; no parallel ACL | **Held** | Steps 1, 9, 11, 12 |
| Soft HOLD invent **process-global** auth flood limiter (per-IP + per-account only) | **Held** | Step 5 Process-global; Locked #6; Step 12 |
| Raw IP counters only (short-lived); never audit/logs/metrics | **Held** | Step 5 Raw IP; Step 8 verify |
| Auth audit IP = keyed HMAC-SHA256 only; Soft HOLD invent HMAC key value | **Held** | Step 8 IP in auth audit |
| Turnstile only; WAF CAPTCHA / reCAPTCHA OUT | **Held** | Step 5; Step 12 OUT |
| SES behind mail interface; Soft HOLD invent AWS account IDs | **Held** | Step 6; Cost/critical |
| Soft HOLD invent password values | **Held** | Steps 2, 12; Locked #14 |
| Soft HOLD A7 until H4; Soft HOLD build / Stories / deploy | **Held** | Step 12; Status; Done-list |
| Soft HOLD harden redeploy (H1) separate | **Held** | Step 12; Explicit OUT |
| Handshake Soft HOLD SoR = qa-confirm only | **Held** — this file |
| PoC $0; not build unlock | **Held** | Cost/critical; Status |

## Gaps

**None.** Soft notes non-blocking. Soft HOLD invent Soft HOLD SoR beyond this qa-confirm not done. Soft HOLD invent Soft HOLD SoR as PASS from points-review alone not done (Senior PASS 14/14 cited as content evidence only). Soft HOLD invent process-global auth flood limiter not done. Soft HOLD invent password / AWS not done. Soft HOLD build / Stories / code / CDK / spend / A7 not unlocked.

## Handshake status

Security QA Soft HOLD SoR → **PASS** (14/14 MET). Soft HOLD Dev Plan Security Soft HOLD SoR **CLEARED**. Soft HOLD Dev Plan QA Soft HOLD Security gate may lift per checklist Status (Dev Plan QA may proceed after Soft HOLD SoR CLEAR — formal Dev Plan QA PASS remains with Dev Plan QA / Chief Dev Planner path). Soft HOLD invent Stories / code / CDK / spend / provision / deploy. Soft HOLD A7 until H4 live PASS. Soft HOLD invent password / AWS. Soft HOLD invent process-global auth flood limiter until Chief Security explicitly approves. Soft HOLD harden redeploy (H1) separate. Senior points-review PASS 14/14 cited (content only — **not** Soft HOLD SoR). Sibling Spec Soft HOLD SoR PASS 15/15 + SA Soft HOLD SoR PASS 14/14 cite-only (Spec/Arch not re-scored). Not build unlock. PoC **$0**. Cost/critical: none from this Soft HOLD SoR → COO → CEO if any later spend.
