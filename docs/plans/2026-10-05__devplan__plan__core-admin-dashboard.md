# Dev Plan — Core admin dashboard (A5)

**Status:** DRAFT woven — READY for Chief Dev Planner. Soft HOLD Dev Plan QA until Soft HOLD SoR qa-confirm CLEAR at `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md`. Do **not** done-list Dev Plan QA until weave + Soft HOLD SoR path are intact. Not a build unlock.  
**Date:** 2026-10-05  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner → Senior — DRAFT A5 Core admin dashboard Dev Plan (CPM formal unlock confirms A5). Executable plan only. Soft HOLD invent Stories/code until Dev Plan QA + Test design PASS. Soft HOLD A7 until H4 live PASS. Soft HOLD invent password / AWS account IDs. Soft HOLD deploy until Ivan OK. Soft HOLD harden redeploy (H1) separate. PoC **$0**. App target .NET 10 — plan assumes TFM `net10.0`; do **not** schedule .NET 10 retarget as this Story's work.  
**DOC-FLOW:** `plans/2026-10-05__devplan__plan__core-admin-dashboard.md`  
**Host:** `admin.core.dealoware.com` only (Core admin — **not** `admin.platform.dealoware.com`)  
**Principal:** **CoreOwner** = single system superadmin `io@aiknowhow.com`  
**Constraints:** Soft HOLD invent Stories/build until Dev Plan QA + Test design PASS · Soft HOLD A7 until H4 live PASS · Soft HOLD invent password / AWS account IDs · Soft HOLD deploy until Ivan OK · Soft HOLD invent process-global auth flood limiter until Chief Security explicitly approves · Soft HOLD harden redeploy (H1) separate · PoC **$0** · No Cognito invent · No MM/DC4 · No password values anywhere · No AWS account / CDK / bot-platform internals in this plan · After Dev Plan QA PASS, **A6 Test design** goes to Chief QA **before** Stories/code · Cost/critical (LLM if any) → COO → CEO (do not provision)

---

## 1. Sources (cite only; no invented requirements)

| Source | Path | Role |
|--------|------|------|
| Spec v2.2 (binding) | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | Locked decisions; §§3–12; AC map §14; Security Spec §13 (v2 pts 1–15). Tip sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` |
| Spec QA formal PASS | `verification/2026-10-05__spec__verification__core-admin-dashboard.md` | Spec gate PASS on v2.2; sha256 MATCH; Soft HOLDs remaining cited |
| Spec Security Soft HOLD SoR qa-confirm PASS 15/15 | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` | Spec Security Soft HOLD SoR CLEAR (checklist v2) |
| Spec Security checklist (cite) | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` | Spec-step v2 pts 1–15 — upstream; not a substitute for Dev Plan-step |
| SA Soft HOLD SoR (amended) | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` | Option A; CoreOwner; §§3.1–3.9 mechanism picks; IN/OUT/HOLD |
| SA Security Soft HOLD SoR qa-confirm PASS 14/14 | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` | SA Security Soft HOLD SoR CLEAR |
| Arch QA formal PASS (CEO amend) | `verification/2026-10-05__sa__verification__core-admin-dashboard-ceo-amend-review.md` | Formal Architecture QA PASS on amended Soft HOLD SoR |
| Product scope (binding 13:31) | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | IN 1–9; AC1–AC9; Product OUT pack; CEO host standing rule |
| Product QA PASS | `verification/2026-10-05__product__verification__core-admin-dashboard-scope.md` | Product requirements check PASS |
| CFO Soft HOLD estimate | `finance/2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` (also `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md`) | Turnstile **$0/mo**; SES **under $0.01/mo** assumed; estimate only — not spend approval |
| Finance QA PASS | `finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md` (KB twin `verification/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md`) | Finance QA PASS — estimate only; Soft HOLD invent spend |
| Dev Plan Security checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` | Dev Plan-step handshake pts **1–14** — woven in §6 |
| Expected Soft HOLD SoR (qa-confirm only) | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` | Handshake Soft HOLD SoR — **do not invent points-review Soft HOLD SoR** |
| FieldPolicy dual wall (cite) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3a/§3b | Cite; do not rewrite; no parallel admin ACL |
| CEO host standing rule | COO / Product / Spec Sources | `admin.core.dealoware.com` = Core admin; platform hosts ≠ Core; Core may stay public / non-AWS; never put AWS account info in public docs/issues/PRs/messages |

**Product alignment:** Binding Product **13:31** (includes 1:27pm patch + 1:30pm CEO final). Conflicts → PM → Product → CEO. Cost/critical → **COO → CEO**. PoC **$0**.

**Distinct tracks (cross-ref only — do not merge):** Platform admin (`admin.platform.dealoware.com`); Stage C bot-isolation Stories (#66/#67/#68/#69/#18); inbound bot connector (after this); H1–H6 harden track; A6 Test design; A7 admin build.

---

## 2. Restated understanding (short)

Executable plan for **SD only** (after Soft HOLDs lift): deliver Core owner admin dashboard at **`admin.core.dealoware.com`** inside the same Negotiation Core modular monolith (separate admin route surface; Option A). Single system superadmin **`io@aiknowhow.com`** → principal **`CoreOwner`**; auth = email + password + required TOTP; **Cloudflare Turnstile only**; bootstrap/reset mail via **SES behind mail interface** (no AWS SDK in Core app). Entities: **Participants / Artifacts / negotiations / offers** — list/view/edit/delete; all-status lists; sort/filter; server-side paging; name search; stats; confirm-before-delete; soft-delete + cascades; append-only audit (auth + edit/delete; hashed IP). FieldPolicy dual wall; **no** parallel admin ACL; **no** human users list. App assumes TFM **`net10.0`** — **do not** schedule .NET 10 retarget as this Story's work.

**This artifact is design planning only.** Soft HOLD invent Stories/code until Dev Plan QA + Test design PASS. Soft HOLD A7 until H4 live PASS. Soft HOLD deploy until Ivan OK. Soft HOLD invent password values and AWS account IDs. Soft HOLD invent process-global auth flood limiter until Chief Security explicitly approves. Soft HOLD harden redeploy (H1) separate. PoC **$0**. Not a build unlock.

---

## 3. Locked decisions (Spec / SA / Product / Security → plan steps)

| # | Decision | Binding lock | Plan steps |
|---|----------|--------------|------------|
| 1 | Host | Core admin **only** at `admin.core.dealoware.com`. Platform admin / platform app hosts OUT | Steps 1, 12; Explicit OUT; Sec pt 1 |
| 2 | Principal | One CoreOwner superadmin `io@aiknowhow.com`; email+password+TOTP; no multi-admin ACL | Steps 2–5, 12; Sec pts 1–5 |
| 3 | CAPTCHA | **Turnstile only**; WAF CAPTCHA / reCAPTCHA / other CAPTCHA OUT | Steps 2, 5; Sec pt 5 |
| 4 | Mail | SES via **mail interface**; no AWS SDK in Core app; Soft HOLD invent AWS account IDs | Steps 2, 6; Sec pt 7 |
| 5 | IP / audit | Raw IP only in short-lived rate-limit counters; auth audit IP = keyed **HMAC-SHA256** only | Steps 5, 8; Sec pt 6 |
| 6 | Flood limiter | Soft HOLD invent **process-global** auth flood limiter until Chief Security explicitly approves — per-IP + per-account only this slice | Step 5; Sec pts 5, 13 |
| 7 | Entities | Participants / Artifacts / negotiations / offers — list/view/edit/delete; all-status; sort/filter/paging; name search; stats | Steps 7–10; Sec pts 10–12 |
| 8 | FieldPolicy | Option A dual wall; no parallel admin ACL; no human users list | Steps 1, 7–9; Sec pt 9 |
| 9 | Confirm / soft-delete | Modal + API confirm token; soft-delete `DeletedAt`; hard delete deferred; cascades per Spec §11 / SA §3.6 | Steps 9–10; Sec pt 10 |
| 10 | Audit | Append-only; auth + edit/delete; soft-deleted toggle not audited; same-txn pairing | Step 8; Sec pt 8 |
| 11 | TFM | Assume `net10.0`; **do not** schedule .NET 10 retarget as this Story's work | Step 1; Explicit OUT |
| 12 | Gates | Soft HOLD invent Stories/build until Dev Plan QA + Test design PASS; Soft HOLD A7 until H4 live PASS; Soft HOLD deploy until Ivan OK; Soft HOLD harden redeploy (H1) separate; after Dev Plan QA PASS → **A6 Test design** to Chief QA before Stories/code | Steps 12–13; Explicit OUT; Sec pts 13–14 |
| 13 | Cost | PoC **$0**; CFO Soft HOLD cost cite only; Soft HOLD invent spend; LLM if any → COO → CEO | Cost/critical; Sec pt 13 |
| 14 | Secrets | Soft HOLD invent password values; Soft HOLD invent AWS account IDs; Soft HOLD invent HMAC/Turnstile/SES secrets in plan/docs/chat | All steps; Sec pts 2, 6, 7, 13 |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Prerequisite Soft HOLDs (do not skip):** Dev Plan QA PASS + Soft HOLD SoR qa-confirm CLEAR + Chief unlock · A6 Test design PASS via Chief QA · Soft HOLD A7 until H4 live PASS · Soft HOLD deploy until Ivan OK · Soft HOLD harden redeploy (H1) separate · Soft HOLD invent password / AWS account IDs · Soft HOLD invent process-global auth flood limiter until Chief Security explicitly approves.

**Repo / host:** Extend Negotiation Core modular monolith; admin UI + admin API only at `admin.core.dealoware.com`. Assume TFM `net10.0` — do **not** schedule retarget work under this plan.

**Sibling HOLD:** Platform admin; Stage C bot Stories; inbound bot connector; settlement; SSO/IdP; charts/warehouse — **cross-ref only**.

### Step 1 — Host, route surface, CoreOwner principal, FieldPolicy bind, TFM assume

- Serve admin UI/API **only** as Core admin at `admin.core.dealoware.com` (Spec §3; SA §3.1).
- Soft HOLD invent tasks for `admin.platform.dealoware.com`, `platform.dealoware.com`, or their APIs.
- Same Core modular monolith; **separate** admin route surface (Option A — SA §2 pick A).
- Bind principal **`CoreOwner`** to single superadmin login email `io@aiknowhow.com` (SA §3.2). Soft HOLD invent multi-admin / operator ACL / human-user list on Core / Participant UI-as-admin.
- All admin reads/writes bind Domain FieldPolicy for CoreOwner (Option A §3a/§3b — cite; do not rewrite). Soft HOLD invent parallel admin ACL / second permission matrix.
- Assume app TFM **`net10.0`**. Soft HOLD invent .NET 10 retarget Stories/tasks under this plan.
- Soft HOLD invent AWS account IDs, CDK, bot-platform internals, App Runner, Cognito, MM/DC4.

**Verify checklist:**

- [ ] Admin surface documented/served only for `admin.core.dealoware.com` (Sec pt 1)
- [ ] No platform-admin host tasks in this plan's delivery path (Sec pts 1, 13)
- [ ] CoreOwner ≠ Participant UI ≠ platform human-user admin (Sec pt 1)
- [ ] FieldPolicy dual wall cited; no parallel ACL invent (Sec pt 9)
- [ ] TFM assumed `net10.0`; no retarget Story scheduled here
- [ ] No AWS account / CDK / bot-platform internals in plan or delivery notes (Sec pts 7, 13)

### Step 2 — Bootstrap + email/password (no invent password) + Turnstile on bootstrap page

Per Spec §8.1–§8.2, §8.5; SA §3.3; Sec pts 2, 5:

| Item | Plan lock |
|------|-----------|
| Identity | One superadmin `io@aiknowhow.com`; email + password |
| Bootstrap | Single-use timed out-of-band bootstrap link (≤ **24h**; SA may tighten); first password set **only** via that link |
| TOTP gate | TOTP enrollment **required before** first interactive admin session |
| Secrets | Soft HOLD invent password values in Stories, fixtures, README, chat, or commit history — env/placeholders for secrets only |
| CAPTCHA | **Turnstile only** on bootstrap password-set page; Soft HOLD invent Turnstile account/site keys in plan |
| OUT | Soft HOLD invent SSO/IdP as delivered |

**Verify checklist:**

- [ ] Bootstrap is single-use + time-limited; password set only via link (Sec pt 2)
- [ ] No password value appears in code/docs/plan/chat/commit history (Sec pts 2, 13)
- [ ] First session blocked until TOTP enrolled (Sec pts 2, 3)
- [ ] Turnstile present on bootstrap password-set; no other CAPTCHA provider (Sec pt 5)
- [ ] No SSO/IdP-as-delivered tasks (Sec pts 2, 13)

### Step 3 — TOTP required; no email OTP; hashed recovery codes

Per Spec §8.3; SA §3.3; Sec pt 3:

| Item | Plan lock |
|------|-----------|
| TOTP | Authenticator-app TOTP required on **every** login after enrollment |
| Email OTP | Soft HOLD invent email OTP fallback tasks — **NOT allowed** |
| Recovery | One-time recovery codes shown **once** at enrollment; stored **hashed**; each single-use |

**Verify checklist:**

- [ ] TOTP required on every post-enrollment login (Sec pt 3)
- [ ] Verify step proves **email OTP path absent** (Sec pt 3)
- [ ] Recovery codes hashed at rest; shown once; single-use (Sec pt 3)
- [ ] No plaintext recovery codes / TOTP secrets in logs, UI dump, audit, or plan (Sec pts 3, 8)

### Step 4 — Password reset = email link + 2FA

Per Spec §8.4; SA §3.3; Sec pt 4:

| Item | Plan lock |
|------|-----------|
| Factors | Reset requires **both** single-use short-lived email link (**1 hour**) **and** 2FA (TOTP or unused recovery code) |
| Mail body | Reset mail carries **link only** — never a password value |
| OUT | Soft HOLD invent reset that skips 2FA |

**Verify checklist:**

- [ ] Reset requires email link **and** 2FA (Sec pt 4)
- [ ] Blind / password-only reset path absent (Sec pt 4)
- [ ] Reset email contains no password value (Sec pts 4, 2)
- [ ] Reset completion audited (ties Step 8; Sec pt 8)

### Step 5 — Turnstile only + lockout/rate limit + session (no process-global flood limiter)

Per Spec §8.5–§8.7; SA §3.3; Sec pts 5, 6, 13:

| Control | Plan lock |
|---------|-----------|
| CAPTCHA | **Cloudflare Turnstile only** on login, password-reset, and bootstrap password-set. Soft HOLD invent Turnstile account/site keys. Soft HOLD invent AWS WAF CAPTCHA / reCAPTCHA / any other CAPTCHA |
| Per-account | **5** failed password or 2FA attempts / **15 min** → lock **30 min** (or unlock via completed reset) |
| Per-IP | **20** failed logins / **15 min** → throttle **30 min** (HTTP 429); same per-IP window on reset-request |
| Process-global | Soft HOLD invent **process-global auth flood limiter** (shared budget across all IPs) until Chief Security **explicitly** approves — **per-IP + per-account only** this slice |
| Session store | Server-side session row in Core Postgres; **HttpOnly Secure SameSite=Strict** cookie on `admin.core.dealoware.com` |
| Idle / absolute | Idle **30 min** (sliding renewal); absolute **8 h** from login; after expiry full re-auth (password + TOTP) |
| Raw IP | Raw client IP lives **only** in short-lived rate-limit counters (expire with 15-/30-minute windows); **never** written to audit, other logs, or metrics (Sec pt 6) |

**Verify checklist:**

- [ ] Turnstile only on login / reset / bootstrap password-set; no WAF CAPTCHA / reCAPTCHA tasks (Sec pts 5, 13)
- [ ] Lockout numbers match Spec (5/15→30 account; 20/15→30 IP) (Sec pt 5)
- [ ] No process-global auth flood limiter scheduled without Chief Security approval (Sec pts 5, 13)
- [ ] Session idle 30m / absolute 8h; HttpOnly Secure SameSite=Strict (Sec pt 5)
- [ ] Raw IP absent from audit/logs/metrics; present only in short-lived counters (Sec pt 6)
- [ ] Soft HOLD invent Turnstile secrets in plan/docs/chat (Sec pts 5, 13)

### Step 6 — SES behind mail interface; Soft HOLD invent AWS

Per Spec §8.9; SA §3.3; CFO Soft HOLD estimate; Sec pt 7:

| Item | Plan lock |
|------|-----------|
| Need | Bootstrap + password-reset email delivery |
| Provider | Amazon SES (chosen) behind a **mail interface** (port/adapter) |
| Core coupling | Soft HOLD invent direct AWS SES SDK dependency / SES types in Core app tasks |
| Soft HOLD invent | AWS account IDs, region, SES identity, from-address, provision, spend |
| Cost cite | CFO Soft HOLD: Turnstile **$0/mo**; SES **under $0.01/mo** assumed — Finance QA PASS; **not** spend approval; Soft HOLD invent spend |
| PoC | **$0** until separate spend unlock |

**Verify checklist:**

- [ ] Mail sent only through mail interface / adapter (Sec pt 7)
- [ ] No AWS SES SDK PackageReference / types in Core app delivery path (Sec pt 7)
- [ ] No AWS account IDs / region / SES identity / from-address invented in plan or Stories (Sec pts 7, 13)
- [ ] CFO Soft HOLD cost cited as estimate only — not spend approval (Sec pt 7)

### Step 7 — Lists, sort/filter, paging, name search, stats (safe)

Per Spec §4.5–§4.6, §6; SA §3.8–§3.9; Product IN 5–7; Sec pt 11:

| Item | Plan lock |
|------|-----------|
| Entities | Participants, Artifacts, negotiations, offers — list/view surfaces |
| Statuses | All Core statuses on negotiation/offer lists incl. first-class **`Withdrawn`**; soft-deleted via **CoreOwner-only** toggle (toggle use = read; **not** audited) |
| Sort/filter | status; created/updated; participant; artifact; value/price; negotiation id — **server-side** |
| Paging | Server-side **offset/limit**; default **50**; max **200**; Soft HOLD invent client-only full-table dump |
| Name search | Case-insensitive **contains** per Product definitions on all four tables; combines with filter/paging; parameterized |
| FieldPolicy | Result payloads never include denied fields |
| Stats | Participants; open negotiations (`Status = Open` AND `DeletedAt IS NULL`); offers; accepts; declines — all exclude soft-deleted. Soft HOLD invent charts/warehouse |

**Verify checklist:**

- [ ] All-status lists + CoreOwner-only soft-deleted toggle; toggle not audited (Sec pts 8, 11)
- [ ] Server-side paging 50/200; no client full-table dump (Sec pt 11)
- [ ] Name search per Product on all four tables; parameterized; FieldPolicy on hits (Sec pt 11)
- [ ] Stats predicates exclude soft-deleted; no charts/warehouse (Sec pt 11)
- [ ] No human-user list on Core (Sec pts 1, 13)

### Step 8 — Audit log (auth + edit/delete); IP HMAC; same-txn pairing

Per Spec §8.8, §10; SA §3.5; Sec pts 6, 8:

| Item | Plan lock |
|------|-----------|
| Storage | Append-only audit table in Core Postgres; insert-only; no update/delete from admin UI/API |
| Auth events | login_success / login_failure (reason class) / reset_request / reset_complete / totp_enroll / totp_change / recovery_code_use |
| Entity events | Every successful admin edit/delete (+ cascade rows with correlation id) |
| Soft-deleted toggle | **Not** audited; CoreOwner-only |
| IP in auth audit | Keyed **HMAC-SHA256** of client IP (server-side rotatable secret) + failure reason class only. Soft HOLD invent unkeyed hash or raw IP in audit/logs/metrics. Soft HOLD invent HMAC key value in plan/docs/chat |
| Pairing | Same DB transaction as mutation / auth-state change; audit insert fail → **roll back** |
| Snapshots | FieldPolicy-allowed fields only; no secrets; bodies >4 KiB truncated + length + hash |
| Retention | Retain indefinitely this slice (purge later) |

**Verify checklist:**

- [ ] Auth + edit/delete (+ cascades) audited; soft-deleted toggle not audited (Sec pt 8)
- [ ] Audit not editable/deletable from admin UI/API (Sec pt 8)
- [ ] Same-txn pairing: audit fail → roll back (Sec pt 8)
- [ ] Auth audit IP = keyed HMAC-SHA256 only; raw IP not in audit/logs/metrics (Sec pt 6)
- [ ] No password / TOTP secret / recovery plaintext / HMAC key in snapshots or plan (Sec pts 6, 8, 13)

### Step 9 — Edit write surface + concurrency

Per Spec §5; SA §3.7–§3.8; Sec pt 12:

| Item | Plan lock |
|------|-----------|
| Writable | Only FieldPolicy-allowed fields for CoreOwner; no second permission matrix |
| Audit | Every successful edit: before/after FieldPolicy-allowed only |
| Concurrency | Optimistic via row `Version` (or `UpdatedAt` ETag); stale → **409 Conflict** safe message; no silent overwrite |
| Secrets | Soft HOLD invent password/secret fields in edit forms, errors, or audit snapshots |
| Fail-closed | Missing CoreOwner → unauthorized; denied FieldClass → no write and no dump |

**Verify checklist:**

- [ ] Only FieldPolicy-allowed fields writable; no parallel matrix (Sec pts 9, 12)
- [ ] Edit before/after audited FieldPolicy-only (Sec pts 8, 12)
- [ ] Stale write → 409 safe conflict; no silent overwrite (Sec pt 12)
- [ ] No password/secret in edit forms / errors / audit (Sec pts 12, 13)

### Step 10 — Confirm-before-delete + soft-delete cascades

Per Spec §9, §11; SA §3.4, §3.6; Sec pt 10:

| Item | Plan lock |
|------|-----------|
| UI | Modal confirm: entity type + identity + cascade summary; cancel → unchanged + no delete audit |
| API | `POST …/delete-intent` → `confirmToken` + identity + cascade summary; delete must present token; blind DELETE rejected; token single-use **5 min**, bound to actor + entity + cascade set |
| Soft vs hard | Soft-delete default marker **`DeletedAt`**; hard delete **deferred entirely** |
| Cascades | Offer → that offer; Negotiation → negotiation + child offers (open warn); Artifact → **block** while non-deleted negotiation references it; Participant → Participant + negotiations + those offers (confirm counts); **do not** auto-delete Artifacts |
| OUT | Soft HOLD invent settlement / escrow cascades |

**Verify checklist:**

- [ ] UI modal + API confirm-token paths; blind DELETE rejected (Sec pt 10)
- [ ] Cancel leaves data unchanged and writes no delete audit (Sec pt 10)
- [ ] Soft-delete `DeletedAt`; hard delete absent this slice (Sec pt 10)
- [ ] Cascades match Spec §11 / SA §3.6; settlement cascades absent (Sec pts 10, 13)
- [ ] Cascade soft-deletes audited with correlation id (Sec pts 8, 10)

### Step 11 — Fail-closed deny for non-CoreOwner / missing TOTP / expired session

Per Spec §3, §5, §8.1; SA §3.2–§3.3; Sec pts 1, 9:

- Missing/invalid/expired session, missing TOTP step, or principal not the single CoreOwner → deny all admin routes (safe unauthorized).
- Denied FieldClass → no write and no dump into UI/errors.
- Soft HOLD invent bypass routes that skip FieldPolicy or session/TOTP gates.

**Verify checklist:**

- [ ] Non-CoreOwner / expired / missing TOTP → deny all admin entity/stats/edit/delete routes (Sec pts 1, 9)
- [ ] Denied FieldClass never dumped into UI/errors (Sec pt 9)
- [ ] No parallel ACL or FieldPolicy bypass Story (Sec pts 9, 13)

### Step 12 — Explicit Soft HOLD / OUT / gate pack (do not implement)

SD must **not** deliver or unlock any of:

| OUT / Soft HOLD | Note |
|-----------------|------|
| Soft HOLD invent Stories / build | Until **Dev Plan QA + Test design PASS** |
| Soft HOLD A7 | Until **H4 live PASS** (separate track — do not invent H4 steps here) |
| Soft HOLD deploy | Until **Ivan OK** |
| Soft HOLD harden redeploy (H1) | **Separate** until H1–H6(+H6b) PASS + CEO OK — do not invent H1 steps here |
| Soft HOLD invent password values | Forbidden in plan/docs/chat/code/commit history |
| Soft HOLD invent AWS account IDs | No account IDs / region / SES identity / Turnstile keys / provision / spend invent |
| Soft HOLD invent process-global auth flood limiter | Until Chief Security **explicitly** approves |
| Platform admin hosts | `admin.platform.dealoware.com` / platform app — OUT |
| Human-user list on Core | OUT |
| Participant UI as admin | OUT |
| Multiple admin humans / operator ACL | OUT |
| Email OTP as 2FA fallback | OUT |
| WAF CAPTCHA / reCAPTCHA / other CAPTCHA | OUT — **Turnstile only** |
| SSO / IdP as delivered | OUT |
| Inbound bot connector | After this track |
| Settlement / escrow / checkout | OUT |
| Charts / warehouse | OUT |
| MotorMarket / DC4 | OUT |
| App Runner / CDK invent / bot-platform internals | OUT |
| .NET 10 retarget as this Story | Soft HOLD — assume `net10.0`; do not schedule retarget here |
| Hard delete this slice | Deferred |
| Process-global auth flood limiter | Soft HOLD until Chief Security approves |
| After Dev Plan QA PASS | **A6 Test design** → Chief QA **before** Stories/code |

**Verify checklist:**

- [ ] Soft HOLD invent Stories/build until Dev Plan QA + Test design PASS stated (Sec pts 13, 14)
- [ ] Soft HOLD A7 until H4 live PASS stated; no invented H4 steps (Sec pts 13, 14)
- [ ] Soft HOLD deploy until Ivan OK; Soft HOLD harden redeploy (H1) separate (Sec pt 13)
- [ ] Soft HOLD invent password / AWS / process-global flood limiter stated (Sec pts 5, 13)
- [ ] Turnstile only; SES behind mail interface; platform admin OUT (Sec pts 5, 7, 13)
- [ ] A6 Test design gate noted after Dev Plan QA PASS (Sec pt 14)
- [ ] PoC $0; not a build unlock (Sec pts 13, 14)

### Step 13 — Self-verify before any handoff (after Soft HOLDs lift)

Before marking delivery ready for CQ / handoff (only after Soft HOLDs lift + unlocks):

- [ ] Host only `admin.core.dealoware.com`; CoreOwner `io@aiknowhow.com`; FieldPolicy dual wall; no parallel ACL
- [ ] Bootstrap + TOTP + reset=link+2FA + Turnstile only + lockout/session per Spec
- [ ] Raw IP counters only; audit IP keyed HMAC-SHA256; SES via mail interface; no AWS SDK in Core
- [ ] Lists/search/paging/stats/edit/delete/confirm/soft-delete/cascades/audit per Spec
- [ ] No password values; no AWS account IDs; no process-global flood limiter without approval
- [ ] Soft HOLD A7 / deploy / H1 gates respected; A6 Test design done before Stories/code
- [ ] Zero MM/DC4; PoC $0; no Cognito invent; LLM if any → COO → CEO (do not provision)
- [ ] Security Dev Plan-step pts 1–14 evidence ready for Soft HOLD SoR qa-confirm

---

## 5. Spec + AC mapping

| Spec / Product AC | Content | Plan step(s) |
|-------------------|---------|--------------|
| Spec Locked #1 / AC1 | Host `admin.core.dealoware.com` only | Steps 1, 12 |
| Spec Locked #2 / AC6 | CoreOwner single superadmin; FieldPolicy; no parallel ACL | Steps 1, 11 |
| Spec §4 / AC2–AC4 | List/view/edit/delete four entities; all-status; sort/filter/paging; name search | Steps 7, 9–10 |
| Spec §6 / AC5 | Overall stats; no charts/warehouse | Step 7 |
| Spec §8 / AC7 | Auth: bootstrap, TOTP, reset, Turnstile, SES mail interface, lockout, session, auth audit | Steps 2–6, 8 |
| Spec §9 / AC7 | Confirm before delete | Step 10 |
| Spec §10 / AC7 | Audit edits/deletes + auth events; hashed IP | Step 8 |
| Spec §11 / AC7 | Soft-delete + cascades | Step 10 |
| Spec §12 / AC8–AC9 | OUT pack; Soft HOLD build/deploy/invent AWS; PoC $0 | Step 12; Cost/critical |
| Spec §13 / Spec Security v2 1–15 | Upstream Spec Security bind (PASS Soft HOLD SoR) | Sources; §6 handshake |
| Product 13:31 | Binding Product scope | Sources; Locked table |
| SA Soft HOLD SoR | Option A + mechanism picks §§3.1–3.9 | Steps 1–11 |
| CFO Soft HOLD + Finance QA PASS | Turnstile $0; SES under $0.01/mo assumed | Step 6; Cost/critical |
| Dev Plan Security checklist 1–14 | Dev Plan-step handshake | §6 woven |

---

## 6. Security Dev Plan-step binding (pts 1–14 woven)

**Binding checklist:** `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` (**ISSUED**, pts **1–14**)  
**Upstream Spec tip sha256:** `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`  
**Upstream Spec Security Soft HOLD SoR:** `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` (**PASS** 15/15)  
**Upstream SA Security Soft HOLD SoR:** `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (**PASS** 14/14)  
**Expected Soft HOLD SoR (qa-confirm only):** `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` — **do not invent points-review Soft HOLD SoR**  
**Status:** Pts **1–14 woven** below. Soft HOLD Dev Plan QA until Soft HOLD SoR qa-confirm CLEAR. Soft HOLD invent Stories/code/deploy from this plan alone. Soft HOLD A7 until H4. Soft HOLD invent process-global auth flood limiter until Chief Security explicitly approves. Soft HOLD harden redeploy (H1) separate. Not a build unlock. PoC **$0**.

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Host + single CoreOwner gate** — admin only `admin.core.dealoware.com`; `io@aiknowhow.com` → **CoreOwner**; Soft HOLD invent platform-admin host / human-user list / Participant UI-as-admin / multi-admin ACL | Step 1 host + CoreOwner + FieldPolicy; Step 11 fail-closed; Step 12 OUT pack | Steps 1, 11, 12; Locked #1–#2; Explicit OUT |
| 2 | **Bootstrap + email/password (no invent password)** — single-use timed OOB bootstrap; password only via link; TOTP before first session; Soft HOLD invent password values; Soft HOLD invent SSO/IdP | Step 2 bootstrap + secrets Soft HOLD; Step 12 password Forbidden | Steps 2, 12; Locked #2, #14 |
| 3 | **TOTP required; no email OTP; hashed recovery** — TOTP every login; Soft HOLD invent email OTP; recovery show-once hashed single-use; verify email OTP path absent | Step 3 TOTP + recovery; verify checklist includes email OTP absent | Step 3; Locked #2 |
| 4 | **Password reset = email link + 2FA** — both factors; Soft HOLD invent skip-2FA reset; mail link only | Step 4 reset locks + verify | Step 4 |
| 5 | **Turnstile only + lockout/rate + session** — Turnstile on login/reset/bootstrap; Soft HOLD invent other CAPTCHA + Turnstile keys; Spec lockout numbers; session 30m/8h; HttpOnly Secure SameSite=Strict; Soft HOLD invent process-global auth flood limiter until Chief Security approves | Step 5 CAPTCHA + lockout + session + flood Soft HOLD; Step 12 OUT | Steps 5, 12; Locked #3, #6 |
| 6 | **Raw IP counters only; audit IP = keyed HMAC-SHA256** — raw IP only short-lived counters; audit IP keyed HMAC only; Soft HOLD invent unkeyed/raw IP in audit/logs/metrics; Soft HOLD invent HMAC key value | Steps 5 + 8 IP rules; verify checklists | Steps 5, 8; Locked #5 |
| 7 | **SES behind mail interface; Soft HOLD invent AWS** — mail interface; Soft HOLD invent SES SDK in Core; Soft HOLD invent AWS account IDs / region / identity / from-address / provision / spend; CFO Soft HOLD cost cite only | Step 6 mail interface + Soft HOLDs; Cost/critical | Step 6; Locked #4, #13; Cost/critical |
| 8 | **Audit: auth + edit/delete; soft-deleted toggle not audited** — append-only; auth + edit/delete + cascades; toggle CoreOwner-only not audited; same-txn pairing; Soft HOLD invent secret dump | Step 8 audit; Step 7 toggle note; Steps 9–10 edit/delete | Steps 7, 8, 9, 10; Locked #10 |
| 9 | **Fail-closed FieldPolicy dual wall; no parallel ACL** — all admin I/O via FieldPolicy CoreOwner (Option A §3a/§3b cite); deny missing session/TOTP; Soft HOLD invent parallel ACL / second matrix | Steps 1, 9, 11 FieldPolicy + fail-closed; Step 12 OUT | Steps 1, 9, 11, 12; Locked #8 |
| 10 | **Confirm-before-delete + soft-delete cascades** — UI modal + API confirm token; soft-delete `DeletedAt`; hard delete deferred; cascades per Spec §11 / SA; Soft HOLD invent settlement cascades | Step 10 confirm + soft-delete + cascades | Step 10; Locked #9 |
| 11 | **Lists / search / paging safe** — all-status; CoreOwner soft-deleted toggle; server-side sort/filter/paging 50/200; name search; parameterized; FieldPolicy on results; stats exclude soft-deleted; Soft HOLD invent client dump / charts/warehouse | Step 7 lists/search/paging/stats | Step 7; Locked #7 |
| 12 | **Edit write surface + concurrency** — FieldPolicy-only writes; before/after audit; optimistic concurrency 409; Soft HOLD invent password/secret in edit/audit | Step 9 edit + concurrency | Step 9; Locked #8 |
| 13 | **OUT / Soft HOLD / A7 gate pack** — Soft HOLD invent Stories/code/CDK/spend/provision/**deploy**; Soft HOLD A7 until **H4 live PASS**; Soft HOLD harden redeploy (H1) separate; Soft HOLD invent password / AWS IDs / process-global flood limiter; platform admin / human users / Participant UI-as-admin / multi-admin / email OTP / WAF CAPTCHA / reCAPTCHA / inbound connector / settlement / SSO OUT; PoC $0; Cost/critical → COO → CEO | Step 12 Explicit Soft HOLD / OUT; Cost/critical; Status | Step 12; Locked #12–#14; Explicit OUT; Cost/critical |
| 14 | **Traceability + handshake Soft HOLD SoR** — cite Spec v2.2 sha256 + Spec Security Soft HOLD SoR PASS 15/15 + SA Soft HOLD SoR PASS 14/14 + this Dev Plan checklist; Dev Plan QA must **not** PASS until Soft HOLD SoR qa-confirm CLEAR at `…devplan-qa-confirm.md`; handshake = qa-confirm only; Soft HOLD invent Stories/code/deploy from checklist; Soft HOLD A7 until H4; not build unlock; PoC $0 | This §6; Sources; Status; Done-list; Next gate note (A6 Test design after Dev Plan QA PASS) | §6; Sources; Status; Done-list Dev Plan QA; Explicit OUT |

### Handshake note (point 14)

**Dev Plan QA must not PASS** until Senior Security → Security QA Soft HOLD SoR qa-confirm CLEAR at `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** — do **not** invent points-review Soft HOLD SoR. Soft HOLD invent Stories/code/deploy from this plan alone. Soft HOLD A7 until H4 live PASS. Soft HOLD invent process-global auth flood limiter until Chief Security explicitly approves. Soft HOLD harden redeploy (H1) separate. After Dev Plan QA PASS, **A6 Test design** goes to Chief QA **before** Stories/code. Do not skip Chief. Not a build unlock. PoC **$0**.

---

## 7. Explicit OUT

| OUT / Soft HOLD | Note |
|-----------------|------|
| Soft HOLD invent Stories / build | Until Dev Plan QA + Test design PASS |
| Soft HOLD A7 | Until H4 live PASS (do not invent H4 steps) |
| Soft HOLD deploy | Until Ivan OK |
| Soft HOLD harden redeploy (H1) | Separate track — do not invent H1 steps |
| Soft HOLD invent password values | Forbidden |
| Soft HOLD invent AWS account IDs / CDK / bot-platform internals | Forbidden in plan / public docs / issues / PRs / messages |
| Soft HOLD invent process-global auth flood limiter | Until Chief Security explicitly approves |
| Platform admin (`admin.platform.dealoware.com`) | OUT — Core admin host only |
| Human-user list on Core | OUT |
| Participant UI as admin | OUT |
| Multiple admin humans / operator ACL | OUT |
| Email OTP 2FA fallback | OUT |
| CAPTCHA other than Turnstile | OUT |
| Direct AWS SES SDK in Core app | OUT — mail interface only |
| SSO / IdP as delivered | OUT |
| Inbound bot connector | After this |
| Settlement / escrow / checkout | OUT |
| Charts / warehouse / cost views | OUT |
| MotorMarket / DC4 | OUT |
| App Runner | OUT |
| .NET 10 retarget as this Story's work | Soft HOLD — assume `net10.0` |
| Hard delete this slice | Deferred |
| Stage C bot-isolation Stories merge | Cross-ref only — do not merge |
| A6 Test design | After Dev Plan QA PASS → Chief QA **before** Stories/code |

---

## 8. Cost/critical

PoC **$0**. This plan schedules **no** AWS account create, CDK apply, SES provision, Turnstile account invent, spend unlock, or LLM provider provision. Cite CFO Soft HOLD estimate (`finance/2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md`) + Finance QA PASS (`finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md`): Turnstile **$0/mo**; SES **under $0.01/mo** assumed — **estimate only**, Soft HOLD invent spend, **not** spend/provision approval. Soft HOLD invent AWS account IDs. Soft HOLD A7 until H4 live PASS. Soft HOLD deploy until Ivan OK. Soft HOLD harden redeploy (H1) separate. Any named **LLM/API spend** → escalate **COO → CEO** (do **not** provision). Other paid provision → escalate **CPM → COO → CEO**. Soft HOLD invent password. Not a build unlock.

---

## 9. Done-list for Dev Plan QA

- [x] Path/name DOC-FLOW: `plans/2026-10-05__devplan__plan__core-admin-dashboard.md`
- [x] Spec v2.2 coverage Locked + §§3–12 + AC1–AC9 mapped; tip sha256 cited `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`
- [x] Spec QA formal PASS + Spec Security Soft HOLD SoR PASS 15/15 + Arch QA PASS + Product QA PASS + CFO Soft HOLD + Finance QA PASS cited
- [x] SA Soft HOLD SoR + SA Security Soft HOLD SoR PASS 14/14 cited
- [x] Itemized steps executable by SD without inventing requirements (Soft HOLDs explicit)
- [x] Host only `admin.core.dealoware.com`; CoreOwner `io@aiknowhow.com`; Turnstile only; SES mail interface; FieldPolicy dual wall; entities + lists/search/paging/stats/confirm/audit/soft-delete
- [x] Soft HOLD invent Stories/build until Dev Plan QA + Test design PASS; Soft HOLD A7 until H4; Soft HOLD deploy until Ivan OK; Soft HOLD invent password / AWS IDs; Soft HOLD invent process-global flood limiter; Soft HOLD harden redeploy (H1) separate; A6 Test design gate after Dev Plan QA PASS
- [x] Assume `net10.0`; no retarget steps; no password values; no AWS account IDs; no platform-admin host
- [x] **Security Dev Plan-step points 1–14 all woven** with cites (table §6) — Soft HOLD Dev Plan QA until Soft HOLD SoR qa-confirm CLEAR
- [ ] Soft HOLD SoR qa-confirm CLEAR at `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` (handshake = qa-confirm only)
- [ ] **Security QA confirm required** on Dev Plan-step pts 1–14 **before** Dev Plan QA PASS to Chief Dev Planner
- [ ] **SD HOLD** until Dev Plan QA + Soft HOLD SoR CLEAR + Chief unlock (+ CPM per ops); then A6 Test design before Stories/code; Soft HOLD A7 until H4
- [ ] No product code in this artifact (SD instructions only)

**Next:** Soft HOLD Dev Plan QA until Soft HOLD SoR qa-confirm CLEAR → Dev Plan QA verify with evidence → ask Senior Security → Security QA Soft HOLD SoR qa-confirm → Chief Security PASS/HOLD to Chief Dev Planner + CPM. **Dev Plan QA must NOT PASS until Soft HOLD SoR qa-confirm CLEAR.** After Dev Plan QA PASS, **A6 Test design** → Chief QA **before** Stories/code. Soft HOLD A7 until H4 live PASS. Soft HOLD deploy until Ivan OK. Soft HOLD harden redeploy (H1) separate. Soft HOLD invent password / AWS / process-global flood limiter. Not a build unlock. PoC **$0**.

---

## 10. Done-list for SD (only after Soft HOLDs lift + Chief unlock + A6 Test design PASS + A7 unlock after H4)

- [ ] Step 1: Host / CoreOwner / FieldPolicy / TFM assume
- [ ] Step 2: Bootstrap + Turnstile on bootstrap; no invent password
- [ ] Step 3: TOTP + hashed recovery; email OTP absent
- [ ] Step 4: Reset = email link + 2FA
- [ ] Step 5: Turnstile + lockout/rate + session; raw IP counters only; no process-global flood limiter without approval
- [ ] Step 6: SES via mail interface; Soft HOLD invent AWS
- [ ] Step 7: Lists / search / paging / stats
- [ ] Step 8: Audit + keyed HMAC IP
- [ ] Step 9: Edit + concurrency
- [ ] Step 10: Confirm-delete + soft-delete cascades
- [ ] Step 11: Fail-closed non-CoreOwner
- [ ] Step 12: Respect Explicit Soft HOLD / OUT / gates
- [ ] Step 13: Self-verify checklist
- [ ] Hand off to CQ gate (never skip CQ)
