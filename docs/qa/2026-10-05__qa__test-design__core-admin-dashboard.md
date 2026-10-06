# Test design — Core admin dashboard (A6)

| Field | Value |
|-------|--------|
| Written by | Dealoware Chief QA |
| Date | 2026-10-05 (~2:06–2:15pm ET); **revised r2 ~8:15pm ET** (UX1-A03 r2 + lockout note OQ1/OQ3 closures) |
| Revision | **r2** — prior tip sha256 `870f348c61e2b3d88c2cd6a99e6ae4398186e96c1a7169a16c0b52e2bdd7bca7` (A6 PASS covered that hash only) |
| Status | **Revised — awaiting Test design QA re-verify.** The A6 PASS on `870f348c…` does not cover this revision. Stories/build held until Test design QA confirms this tip; A7 held until H4 live PASS; deploy held until Ivan OK |
| Host | `admin.core.dealoware.com` **only** (Core admin). Platform hosts are not Core and are not positive test targets. |
| Principal | **CoreOwner** = single system superadmin `io@aiknowhow.com` |
| PoC | **$0** |
| DOC-FLOW | `qa/2026-10-05__qa__test-design__core-admin-dashboard.md` |
| Constraints | Test design only. Do not invent Stories, code, PRs, CDK, spend, provision or deploy. Do not invent passwords, AWS account IDs, region, SES identity, Turnstile site keys or HMAC key values. A7 held until H4 live PASS. Deploy held until Ivan OK. Never MotorMarket buyer/seller sessions or Arctic Circle inventory. |

---

## 0. Revision r2 (2026-10-05 ~8:15pm ET)

| Item | Change |
|------|--------|
| Why | Chief Security **UX1-A03 r2** decision (admin.core lockout message) + Chief Spec / CPM correction + lockout window note closing **OQ1 / OQ3** |
| Changed cases | TD-ADM-010, TD-ADM-020, TD-ADM-030, TD-ADM-031, TD-ADM-040, TD-ADM-041, TD-ADM-050, TD-ADM-051, TD-ADM-100 |
| Closed OQs | OQ1, OQ3 (lockout note); OQ5 (UX1-A03 r2: no Retry-After on admin.core). New: OQ7 (aligned, hash re-cite pending), OQ8 (v5.3 new scope, open) |
| Case count | **53** unchanged (no split needed; audit-only throttle proof folded into TD-ADM-051) |
| Wording | Old hold shorthand replaced by plain English and **Active holds** |
| Core API H4 | **Unchanged.** Core API `/auth/register` and `/auth/token` still expect **HTTP 429 + Retry-After** (separate harden track). That pattern is **not** applied to admin.core, and admin.core Retry-After rules are **not** applied to Core API. |

### Admin.core client rules locked in r2 (summary)

| Situation | Status | Body | Headers / content |
|-----------|--------|------|-------------------|
| Wrong password, unknown email, wrong TOTP, wrong recovery code, **account locked** | **401** (identical for all) | Generic sign-in copy: “We couldn't sign you in. Check your details and try again later. You can also reset your password.” (byte-identical across these cases) | No `Retry-After`; no remaining time; no attempt count; never the word “locked” |
| Login **IP throttle** active | **429** (MUST) | Same generic sign-in copy as the 401 | No `Retry-After`; no time / count / “locked” |
| Reset-request, IP throttle active | **429** (MUST) | Same “If that account exists…” reply as non-throttled | **No email sent**; no `Retry-After` |
| Bootstrap / reset link, bad or expired token (not throttled) | Generic invalid-link status | Same generic invalid-or-expired link reply for every bad/expired case | No `Retry-After` |
| Bootstrap / reset link submit, IP throttle active | **429** (MUST) | Same generic sign-in copy as the 401 | No `Retry-After`; valid token not consumed |
| Turnstile failed / expired / missing / unavailable | Verification-failed status (not counted) | May use own “Verification failed, please try again.” | Audit `captcha-failed`; no counter change |

Server-side proof of lock / throttle is through the **audit log** (reason class `locked` / `rate-limited`) and through the correct password still being refused while the lock is active — never through client-visible text.

---

## 1. Binding inputs

| Source | Path | sha256 / note |
|--------|------|---------------|
| Spec v2.2 | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — **MATCH** |
| Spec QA PASS | `verification/2026-10-05__spec__verification__core-admin-dashboard.md` | Formal Spec gate PASS |
| Dev Plan A5 | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` | `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf` — **MATCH** |
| Dev Plan QA PASS | `verification/2026-10-05__devplan__verification__core-admin-dashboard.md` | Dev Plan gate PASS |
| Spec Security SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` | **PASS 15/15** |
| Spec Security checklist v2 | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` | Pts **1–15** |
| Dev Plan Security SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` | **PASS 14/14** |
| Dev Plan Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` | Pts **1–14** |
| SA SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` | Option A + §§3.1–3.9 |
| Product scope 13:31 | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | AC1–AC9 binding |
| UX1-A03 Security decision (r2) | `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md` | `0817b7676a0103d606197c07fb6ac48782c12fbd363304637e5340b86af6d3f0` at revision time — **binding** for admin.core client-facing auth responses |
| Lockout window note (OQ1 / OQ3) | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` | **v5.3** on disk at revision time, sha256 `67686ce5044728db5cb754ffb65c68562e5ed38aea26161bf90988a145972b8b` (Draft for Spec QA + Security QA). Aligned with UX1-A03 r2: IP throttles MUST 429 + generic body, no Retry-After; completed reset does not lift a lock or clear counts. Senior Spec is still editing — Active hold: re-cite the final hash once Spec QA passes it |
| Architecture Turnstile vs lockout | `architecture/2026-10-05__sa__architecture__core-admin-turnstile-lockout-note.md` | Binding for OQ1 (Turnstile reject counts toward nothing); Arch QA PASS `verification/2026-10-05__sa__verification__core-admin-turnstile-lockout-note.md` |
| CFO estimate | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` | Turnstile $0; SES under $0.01/mo assumed — estimate only |

### Gates (binding)

| Gate | Rule |
|------|------|
| Stories / build | held until **this Test design PASS** |
| A7 admin build | held until **H4 live PASS** (separate harden track; do not invent H4 steps here) |
| Deploy | held until **Ivan OK** |
| Secrets / AWS | Do not invent password values and AWS account details |
| PoC | **$0** — not a spend/provision unlock |

### Scope

- Test design for Core admin dashboard at **`admin.core.dealoware.com`** / Core admin API only.
- Cover Product AC1–AC9, Spec Locked #1–#7 + §§3–12 requirement surface, Spec Security pts 1–15, Dev Plan Steps 1–13, Dev Plan Security pts 1–14.
- Automated API / integration first; minimal UI smoke (Playwright).

### Out of scope

- Writing Stories, code, PRs, CDK, provision, deploy, or spend.
- Inventing passwords, AWS account IDs, SES identity/from-address, Turnstile keys, HMAC keys.
- Platform admin hosts as positive targets; MotorMarket / DC4 / Arctic Circle inventory.
- H1–H6 harden live re-verify (separate track). Note only: A7 held until H4 live PASS. **Core API H4 stays HTTP 429 + Retry-After** on `/auth/register` and `/auth/token`. Admin.core is different: IP throttle returns **429 with the generic body and never Retry-After**; account-level failures return 401 (UX1-A03 r2).
- NAT / CDK / bot-platform internals / App Runner details.

---

## 2. Test strategy

### Layers

| Layer | Purpose | Tooling (planned) |
|-------|---------|-------------------|
| Unit | FieldPolicy helpers, HMAC IP helper, paging clamp, recovery-code hash | xUnit |
| Integration | Auth flows, audit same-txn, cascades, session store | xUnit + WebApplicationFactory |
| API | Admin REST against host-bound routes | Newman / Postman collections + WebApplicationFactory |
| E2E UI smoke | Minimal happy-path + unauth deny | Playwright (thin) |
| Security / negative | Injection, IDOR/deny-by-default, secret-dump probes, CAPTCHA fail | API + review |
| Process-review | Active holds / OUT / gate pack / no-secret scan | Manual checklist |

### Environments

| Env | When | Notes |
|-----|------|-------|
| Local | Story / PR development after Active holds lift | Fake mail adapter; Turnstile test keys via env name only |
| CI | Every admin PR | Automated P0 API/integration; UI smoke optional in CI if headed runners available |
| Live `admin.core.dealoware.com` | **Only after** Ivan deploy OK | Smoke subset; do not invent AWS details in evidence |

### Test data rules

- **Synthetic only** — no real customer data; no MotorMarket buyer/seller sessions; no Arctic Circle inventory.
- CoreOwner login email under test is the Spec lock `io@aiknowhow.com`; **password / TOTP seed / recovery codes** live only in env or secret store, referenced **by name** (e.g. `COREOWNER_PASSWORD_ENV`, `TOTP_SEED_ENV`). **Never** write values into this design, fixtures committed to git, chat, or reports.
- Mail: fake/in-memory mail interface adapter in tests; do not invent SES account/region/identity.
- CAPTCHA: Turnstile **test** keys via env; do not invent production site keys in docs.

### Prefer

1. Automated API / integration covering auth, RBAC, lists, edit, delete, audit.
2. Minimal UI smoke for AC1 shell + confirm modal cancel.
3. Manual process-review for Active holds / OUT / no-secret scans.

---

## 3. Traceability matrix

Coverage rule: every source ID → ≥1 `TD-ADM-###`. Self-verify §8 confirms **0 gaps**.

### 3.1 Product AC1–AC9

| ID | Summary | Test cases |
|----|---------|------------|
| AC1 | Open admin at admin.core only | TD-ADM-001, TD-ADM-130, TD-ADM-131, TD-ADM-162 |
| AC2 | Participants CRUD + name search | TD-ADM-006, TD-ADM-060, TD-ADM-080, TD-ADM-130, TD-ADM-140, TD-ADM-162 |
| AC3 | Artifacts CRUD + name search | TD-ADM-061, TD-ADM-080, TD-ADM-130, TD-ADM-140, TD-ADM-162 |
| AC4 | Negotiations/offers all-status + sort/filter/paging + search | TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-065, TD-ADM-066, TD-ADM-080, TD-ADM-130, TD-ADM-140, TD-ADM-162 |
| AC5 | Overall stats five counts | TD-ADM-070, TD-ADM-130, TD-ADM-162 |
| AC6 | Not Participant UI; FieldPolicy; one superadmin | TD-ADM-002, TD-ADM-003, TD-ADM-005, TD-ADM-007, TD-ADM-131 |
| AC7 | Required auth/confirm/audit/delete/lists designs | TD-ADM-002, TD-ADM-004, TD-ADM-010, TD-ADM-011, TD-ADM-012, TD-ADM-020, TD-ADM-021, TD-ADM-022, TD-ADM-030, TD-ADM-031, TD-ADM-040, TD-ADM-041, TD-ADM-050, TD-ADM-051, TD-ADM-052, TD-ADM-053, TD-ADM-080, TD-ADM-090, TD-ADM-091, TD-ADM-092, TD-ADM-093, TD-ADM-094, TD-ADM-095, TD-ADM-100, TD-ADM-101, TD-ADM-110, TD-ADM-130, TD-ADM-150, TD-ADM-161, TD-ADM-162 |
| AC8 | Does not deliver inbound connector / platform admin / payments / tokens | TD-ADM-006, TD-ADM-007, TD-ADM-120, TD-ADM-160 |
| AC9 | Build/deploy held; AWS details not invented; PoC $0 | TD-ADM-110, TD-ADM-120, TD-ADM-121, TD-ADM-150 |

### 3.2 Spec Locked decisions #1–#7

| ID | Summary | Test cases |
|----|---------|------------|
| LOCK-1 | Host admin.core only | TD-ADM-001 |
| LOCK-2 | One CoreOwner; FieldPolicy; no multi-admin ACL | TD-ADM-002, TD-ADM-005, TD-ADM-007 |
| LOCK-3 | Four entities list/view/edit/delete + lists/search | TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-162 |
| LOCK-4 | Overall stats | TD-ADM-070, TD-ADM-162 |
| LOCK-5 | Must-cover auth/confirm/audit/delete/lists | TD-ADM-090, TD-ADM-161, TD-ADM-162 |
| LOCK-6 | Inbound bot connector after this | TD-ADM-160 |
| LOCK-7 | Build/deploy/Stories held; no password invent | TD-ADM-121 |

### 3.3 Spec requirement sections

| ID | Spec section | Test cases |
|----|--------------|------------|
| SPEC-S3 | §3 Host and role | TD-ADM-001, TD-ADM-002, TD-ADM-007, TD-ADM-130 |
| SPEC-S4.1 | §4.1 Participants | TD-ADM-006, TD-ADM-060 |
| SPEC-S4.2 | §4.2 Artifacts | TD-ADM-061 |
| SPEC-S4.3 | §4.3 Negotiations | TD-ADM-062 |
| SPEC-S4.4 | §4.4 Offers | TD-ADM-063 |
| SPEC-S4.5 | §4.5 Lists/sort/filter/paging | TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-065, TD-ADM-066, TD-ADM-130, TD-ADM-140 |
| SPEC-S4.6 | §4.6 Name search | TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-066, TD-ADM-140 |
| SPEC-S5 | §5 Edit rules | TD-ADM-005, TD-ADM-060, TD-ADM-080, TD-ADM-081, TD-ADM-141 |
| SPEC-S6 | §6 Overall stats | TD-ADM-070, TD-ADM-130 |
| SPEC-S8.1 | §8.1 Identity | TD-ADM-002, TD-ADM-003, TD-ADM-011, TD-ADM-131 |
| SPEC-S8.2 | §8.2 Bootstrap | TD-ADM-004, TD-ADM-010, TD-ADM-011, TD-ADM-012 |
| SPEC-S8.3 | §8.3 TOTP 2FA | TD-ADM-004, TD-ADM-012, TD-ADM-020, TD-ADM-021, TD-ADM-022 |
| SPEC-S8.4 | §8.4 Password reset | TD-ADM-030, TD-ADM-031 |
| SPEC-S8.5 | §8.5 CAPTCHA Turnstile | TD-ADM-040, TD-ADM-041 |
| SPEC-S8.6 | §8.6 Lockout/rate + raw IP (as amended by lockout note + UX1-A03 r2) | TD-ADM-040, TD-ADM-050, TD-ADM-051, TD-ADM-053 |
| SPEC-S8.7 | §8.7 Session lifetime | TD-ADM-003, TD-ADM-052 |
| SPEC-S8.8 | §8.8 Auth audit events (+ `captcha-failed`, lockout note) | TD-ADM-040, TD-ADM-050, TD-ADM-051, TD-ADM-100 |
| SPEC-S8.9 | §8.9 Mail SES interface | TD-ADM-110 |
| SPEC-S8.10 | §8.10 Auth OUT | TD-ADM-011, TD-ADM-021, TD-ADM-150 |
| SPEC-S9 | §9 Confirm before delete | TD-ADM-090, TD-ADM-130 |
| SPEC-S10 | §10 Audit log | TD-ADM-053, TD-ADM-064, TD-ADM-080, TD-ADM-100, TD-ADM-101, TD-ADM-102 |
| SPEC-S11.1 | §11.1 Soft vs hard | TD-ADM-064, TD-ADM-091 |
| SPEC-S11.2 | §11.2 Cascades | TD-ADM-092, TD-ADM-093, TD-ADM-094, TD-ADM-095 |
| SPEC-S11.3 | §11.3 Open negotiation | TD-ADM-093, TD-ADM-095 |
| SPEC-S12-IN | §12 IN | TD-ADM-162 |
| SPEC-S12-OUT | §12 OUT | TD-ADM-006, TD-ADM-007, TD-ADM-021, TD-ADM-041, TD-ADM-120, TD-ADM-121, TD-ADM-150, TD-ADM-160 |

### 3.4 Spec Security points 1–15

| # | Point (short) | Test cases |
|---|---------------|------------|
| 1 | Host + single superadmin | TD-ADM-001, TD-ADM-002, TD-ADM-006, TD-ADM-007 |
| 2 | Email+password; bootstrap | TD-ADM-010, TD-ADM-011, TD-ADM-012, TD-ADM-150 |
| 3 | TOTP; no email OTP; hashed recovery | TD-ADM-004, TD-ADM-012, TD-ADM-020, TD-ADM-021, TD-ADM-022 |
| 4 | Reset = link + 2FA | TD-ADM-030, TD-ADM-031 |
| 5 | Turnstile + lockout/rate + session | TD-ADM-040, TD-ADM-041, TD-ADM-050, TD-ADM-051, TD-ADM-052, TD-ADM-054 |
| 6 | Login/reset + edit/delete audit; HMAC IP | TD-ADM-053, TD-ADM-064, TD-ADM-100, TD-ADM-101, TD-ADM-102 |
| 7 | Fail-closed non-superadmin | TD-ADM-002, TD-ADM-003, TD-ADM-004, TD-ADM-005, TD-ADM-131, TD-ADM-141 |
| 8 | FieldPolicy only; no parallel ACL | TD-ADM-005, TD-ADM-007 |
| 9 | Confirm before delete | TD-ADM-090 |
| 10 | Soft-delete + cascades + toggle | TD-ADM-064, TD-ADM-091, TD-ADM-092, TD-ADM-093, TD-ADM-094, TD-ADM-095 |
| 11 | Lists/search/paging safe | TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-065, TD-ADM-066, TD-ADM-070 |
| 12 | Edit write FieldPolicy-only | TD-ADM-005, TD-ADM-060, TD-ADM-080, TD-ADM-081, TD-ADM-141 |
| 13 | SES + Turnstile dependency+cost only | TD-ADM-040, TD-ADM-054, TD-ADM-110, TD-ADM-150 |
| 14 | OUT / Active holds pack | TD-ADM-006, TD-ADM-007, TD-ADM-011, TD-ADM-021, TD-ADM-041, TD-ADM-120, TD-ADM-121, TD-ADM-150 |
| 15 | Traceability + re-QA handshake | TD-ADM-121, TD-ADM-122, TD-ADM-161 |

### 3.5 Dev Plan Steps 1–13

| Step | Title (short) | Test cases |
|------|---------------|------------|
| 1 | Host / CoreOwner / FieldPolicy / TFM | TD-ADM-001, TD-ADM-002, TD-ADM-005, TD-ADM-007 |
| 2 | Bootstrap + Turnstile on bootstrap | TD-ADM-004, TD-ADM-010, TD-ADM-011, TD-ADM-012, TD-ADM-040, TD-ADM-150 |
| 3 | TOTP + hashed recovery; no email OTP | TD-ADM-004, TD-ADM-012, TD-ADM-020, TD-ADM-021, TD-ADM-022 |
| 4 | Password reset = link + 2FA | TD-ADM-030, TD-ADM-031 |
| 5 | Turnstile + lockout/rate + session | TD-ADM-040, TD-ADM-041, TD-ADM-050, TD-ADM-051, TD-ADM-052, TD-ADM-053, TD-ADM-054 |
| 6 | SES behind mail interface | TD-ADM-110, TD-ADM-150 |
| 7 | Lists / search / paging / stats | TD-ADM-006, TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-065, TD-ADM-066, TD-ADM-070, TD-ADM-130, TD-ADM-140 |
| 8 | Audit + HMAC IP | TD-ADM-053, TD-ADM-064, TD-ADM-080, TD-ADM-100, TD-ADM-101, TD-ADM-102 |
| 9 | Edit + concurrency | TD-ADM-005, TD-ADM-060, TD-ADM-061, TD-ADM-080, TD-ADM-081, TD-ADM-130, TD-ADM-141 |
| 10 | Confirm-delete + soft-delete cascades | TD-ADM-060, TD-ADM-061, TD-ADM-090, TD-ADM-091, TD-ADM-092, TD-ADM-093, TD-ADM-094, TD-ADM-095, TD-ADM-130 |
| 11 | Fail-closed non-CoreOwner | TD-ADM-002, TD-ADM-003, TD-ADM-004, TD-ADM-005, TD-ADM-131 |
| 12 | Explicit Active holds / OUT / gates | TD-ADM-006, TD-ADM-007, TD-ADM-011, TD-ADM-021, TD-ADM-041, TD-ADM-054, TD-ADM-120, TD-ADM-121, TD-ADM-150, TD-ADM-160 |
| 13 | Self-verify before handoff | TD-ADM-121, TD-ADM-122, TD-ADM-161 |

### 3.6 Dev Plan Security points 1–14

| # | Point (short) | Test cases |
|---|---------------|------------|
| 1 | Host + CoreOwner gate | TD-ADM-001, TD-ADM-002, TD-ADM-006, TD-ADM-007 |
| 2 | Bootstrap; no invent password | TD-ADM-010, TD-ADM-011, TD-ADM-012, TD-ADM-150 |
| 3 | TOTP; no email OTP; hashed recovery | TD-ADM-004, TD-ADM-012, TD-ADM-020, TD-ADM-021, TD-ADM-022 |
| 4 | Reset = link + 2FA | TD-ADM-030, TD-ADM-031 |
| 5 | Turnstile + lockout + session; no process-global limiter | TD-ADM-040, TD-ADM-041, TD-ADM-050, TD-ADM-051, TD-ADM-052, TD-ADM-054 |
| 6 | Raw IP counters; audit HMAC IP | TD-ADM-053 |
| 7 | SES mail interface; no invented AWS details | TD-ADM-110, TD-ADM-150 |
| 8 | Audit auth+edit/delete; toggle not audited | TD-ADM-053, TD-ADM-064, TD-ADM-080, TD-ADM-100, TD-ADM-101, TD-ADM-102 |
| 9 | Fail-closed FieldPolicy dual wall | TD-ADM-002, TD-ADM-003, TD-ADM-004, TD-ADM-005, TD-ADM-007, TD-ADM-141 |
| 10 | Confirm-delete + soft-delete cascades | TD-ADM-064, TD-ADM-090, TD-ADM-091, TD-ADM-092, TD-ADM-093, TD-ADM-094, TD-ADM-095 |
| 11 | Lists/search/paging safe | TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-065, TD-ADM-066, TD-ADM-070 |
| 12 | Edit + concurrency | TD-ADM-005, TD-ADM-060, TD-ADM-080, TD-ADM-081, TD-ADM-141 |
| 13 | OUT / Active holds / A7 gate pack | TD-ADM-006, TD-ADM-007, TD-ADM-011, TD-ADM-021, TD-ADM-041, TD-ADM-054, TD-ADM-120, TD-ADM-121, TD-ADM-150, TD-ADM-160 |
| 14 | Traceability + handshake SoR | TD-ADM-121, TD-ADM-122, TD-ADM-161 |

### 3.7 Coverage counts

| Universe | Count | Covered | Gaps |
|----------|-------|---------|------|
| Product AC1–AC9 | 9 | 9 | **0** |
| Spec Locked #1–#7 | 7 | 7 | **0** |
| Spec sections (listed) | 26 | 26 | **0** |
| Spec Security pts 1–15 | 15 | 15 | **0** |
| Dev Plan Steps 1–13 | 13 | 13 | **0** |
| Dev Plan Security pts 1–14 | 14 | 14 | **0** |
| **Security points total** | **29** | **29** | **0** |
| Test cases total | 53 | — | — |
| P0 / P1 / P2 | 45 / 6 / 2 | — | — |

**Zero gaps.** No open coverage holes requiring Spec invent; residual ambiguities listed in §6 as open questions (do not invent behavior).

---

## 4. Test cases

Convention: secrets referenced by **env name only**. Host under test: `admin.core.dealoware.com` (or WebApplicationFactory Host binding).

### 4.1 Host / role / fail-closed

#### TD-ADM-001 — Admin UI/API served only at admin.core.dealoware.com

- **Traces-to:** AC1, LOCK-1, SPEC-S3, SPEC-SEC-1, STEP-1, PLAN-SEC-1
- **Layer:** API+UI-smoke
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Admin surface deployed to test host or WebApplicationFactory with Host header binding.
- **Steps:** 1) Request admin UI and admin API with Host: admin.core.dealoware.com. 2) Optionally request same routes with a non-Core Host header (if Spec/SA host binding is enforced at app).
- **Expected:** Admin routes respond for admin.core.dealoware.com. Platform host names are not used as targets in this design. If app-level Host binding is implemented, non-admin.core Host is denied (safe unauthorized / not-found) without leaking secrets.
- **Negative / abuse variants:** Host spoof attempts must not grant CoreOwner. Do not target platform hosts as positive cases.

#### TD-ADM-002 — CoreOwner principal binds only to io@aiknowhow.com

- **Traces-to:** AC6, AC7, LOCK-2, SPEC-S3, SPEC-S8.1, SPEC-SEC-1, SPEC-SEC-7, STEP-1, STEP-11, PLAN-SEC-1, PLAN-SEC-9
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic CoreOwner credentials available via env (no password values in fixtures/docs). TOTP enrolled in test harness.
- **Steps:** 1) Authenticate as CoreOwner email. 2) Attempt admin actions with a non-CoreOwner principal (synthetic Participant token / wrong email).
- **Expected:** CoreOwner succeeds. Non-CoreOwner denied on all admin entity/stats/edit/delete routes (safe unauthorized).
- **Negative / abuse variants:** Participant JWT/session cannot access admin routes; unknown email cannot bind CoreOwner.

#### TD-ADM-003 — Missing/invalid/expired session denies all admin routes

- **Traces-to:** AC6, SPEC-S8.1, SPEC-S8.7, SPEC-SEC-7, STEP-11, PLAN-SEC-9
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Admin API available; no valid session cookie.
- **Steps:** 1) Call list/view/edit/delete/stats without cookie. 2) Call with forged cookie. 3) Call with expired session row.
- **Expected:** All deny with safe unauthorized; no entity dump; no FieldPolicy-denied fields in errors.
- **Negative / abuse variants:** Cookie tampering; missing cookie; expired absolute/idle session.

#### TD-ADM-004 — Missing TOTP step denies admin routes

- **Traces-to:** AC7, SPEC-S8.2, SPEC-S8.3, SPEC-SEC-3, SPEC-SEC-7, STEP-2, STEP-3, STEP-11, PLAN-SEC-3, PLAN-SEC-9
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Password verified but TOTP not yet satisfied (partial auth state).
- **Steps:** 1) Complete password step only. 2) Hit admin list/edit/delete/stats.
- **Expected:** Denied until TOTP completed; no admin data returned.
- **Negative / abuse variants:** Skip-TOTP deep links / direct API calls.

#### TD-ADM-005 — FieldPolicy dual wall — denied FieldClass no write and no dump

- **Traces-to:** AC6, LOCK-2, SPEC-S5, SPEC-SEC-7, SPEC-SEC-8, SPEC-SEC-12, STEP-1, STEP-9, STEP-11, PLAN-SEC-9, PLAN-SEC-12
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner session; FieldPolicy denies at least one FieldClass on an entity.
- **Steps:** 1) GET detail — assert denied fields absent. 2) PATCH denied field — expect deny. 3) Inspect error body.
- **Expected:** Denied fields never in list/detail/search/error payloads; write rejected; no second permission matrix.
- **Negative / abuse variants:** Error-payload probing for denied values; parallel ACL invent (must be absent).

#### TD-ADM-006 — No human-user list on Core admin

- **Traces-to:** AC2, AC8, SPEC-S4.1, SPEC-S12-OUT, SPEC-SEC-1, SPEC-SEC-14, STEP-7, STEP-12, PLAN-SEC-1, PLAN-SEC-13
- **Layer:** API+review
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner session; synthetic Participants exist.
- **Steps:** 1) List Participants. 2) Search admin routes for human-user list endpoints. 3) Confirm OUT pack in delivery notes.
- **Expected:** Lists show Participants only; no human registered-user admin list; OUT documented.
- **Negative / abuse variants:** Attempt to list human users via admin API returns not-found/deny.

#### TD-ADM-007 — Participant UI is not admin; no multi-admin ACL

- **Traces-to:** AC6, AC8, LOCK-2, SPEC-S3, SPEC-S7, SPEC-S12-OUT, SPEC-SEC-1, SPEC-SEC-8, SPEC-SEC-14, STEP-1, STEP-12, PLAN-SEC-1, PLAN-SEC-9, PLAN-SEC-13
- **Layer:** API+review
- **Priority:** P0
- **Automation:** manual+auto
- **Preconditions:** Participant UI and admin UI both available in test env (or stubs).
- **Steps:** 1) Confirm admin routes distinct from Participant UI. 2) Confirm no operator-list / multi-admin ACL APIs.
- **Expected:** Admin is separate surface; single CoreOwner only; no operator ACL.
- **Negative / abuse variants:** Invite second admin / operator list endpoints must be absent.

### 4.2 Bootstrap

#### TD-ADM-010 — Bootstrap link single-use and time-limited (≤24h); generic invalid-link reply

- **Traces-to:** AC7, SPEC-S8.2, SPEC-SEC-2, STEP-2, PLAN-SEC-2
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Test harness can mint bootstrap token via mail interface fake (no real SES).
- **Steps:** 1) Consume bootstrap link — set first password + enroll TOTP. 2) Reuse same link. 3) Use expired (>24h) link. 4) Use truncated / random token.
- **Expected:** First use succeeds. Reuse, expired and bad tokens all get the **same** generic invalid-or-expired link reply (same status, same body) — no hint which case applied (UX1-A03 r2). Each bad/expired submission that passes Turnstile counts toward the reset/bootstrap per-IP counter (lockout note OQ3). Password never in email body/docs/fixtures.
- **Negative / abuse variants:** Replay; expired; truncated token; response differences between reused vs expired vs random token (must be none).
#### TD-ADM-011 — First password set only via bootstrap link; no seeded password

- **Traces-to:** AC7, SPEC-S8.1, SPEC-S8.2, SPEC-S8.10, SPEC-SEC-2, SPEC-SEC-14, STEP-2, STEP-12, PLAN-SEC-2, PLAN-SEC-13
- **Layer:** integration+review
- **Priority:** P0
- **Automation:** auto+manual
- **Preconditions:** Fresh CoreOwner identity without password.
- **Steps:** 1) Attempt login before bootstrap. 2) Set password only via bootstrap page with Turnstile. 3) Scan fixtures/docs/repo for password values (grep for invented values).
- **Expected:** No login before bootstrap; password set only via link; zero password values in code/docs/chat/fixtures (env-only).
- **Negative / abuse variants:** Seeded password in appsettings; password in commit history fixtures.

#### TD-ADM-012 — TOTP enrollment required before first interactive admin session

- **Traces-to:** AC7, SPEC-S8.2, SPEC-S8.3, SPEC-SEC-2, SPEC-SEC-3, STEP-2, STEP-3, PLAN-SEC-2, PLAN-SEC-3
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Bootstrap password set; TOTP not enrolled.
- **Steps:** 1) Complete password. 2) Attempt to open admin session/routes before TOTP enroll. 3) Enroll TOTP. 4) Complete login.
- **Expected:** Session blocked until TOTP enrolled; after enroll, full login (password+TOTP) works.
- **Negative / abuse variants:** Skip enrollment deep-link.

### 4.3 TOTP / recovery

#### TD-ADM-020 — TOTP required on every post-enrollment login

- **Traces-to:** AC7, SPEC-S8.3, SPEC-SEC-3, STEP-3, PLAN-SEC-3
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner enrolled; synthetic TOTP secret in env/test store (name-only reference).
- **Steps:** 1) Password OK, omit TOTP. 2) Password OK + wrong TOTP. 3) Password OK + wrong recovery code. 4) Password OK + valid TOTP.
- **Expected:** Omit / wrong TOTP / wrong recovery code → **401** with the **same body** as a wrong password (UX1-A03 r2); audit reason class bad 2FA; wrong recovery code counts as a bad 2FA failure toward account + login-IP counters (lockout note). Valid → session issued.
- **Negative / abuse variants:** Reuse old TOTP code; clock skew beyond window; any client text distinguishing “bad code” from “bad password” (must be none).
#### TD-ADM-021 — Email OTP fallback path absent

- **Traces-to:** AC7, SPEC-S8.3, SPEC-S8.10, SPEC-S12-OUT, SPEC-SEC-3, SPEC-SEC-14, STEP-3, STEP-12, PLAN-SEC-3, PLAN-SEC-13
- **Layer:** API+review
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Auth surface under test.
- **Steps:** 1) Probe API/UI for email-OTP send/verify endpoints. 2) Confirm Spec OUT pack.
- **Expected:** No email OTP endpoints or UI; OUT documented.
- **Negative / abuse variants:** Any email-OTP fallback request returns not-found/deny.

#### TD-ADM-022 — Recovery codes shown once, hashed at rest, single-use

- **Traces-to:** AC7, SPEC-S8.3, SPEC-SEC-3, STEP-3, PLAN-SEC-3
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** TOTP enrollment flow.
- **Steps:** 1) Enroll TOTP — capture recovery codes once. 2) Re-fetch enrollment — codes not re-shown. 3) Use one recovery code for 2FA. 4) Reuse same code. 5) Inspect DB/store for plaintext codes.
- **Expected:** Shown once; stored hashed; single-use success then reject; no plaintext in DB/logs/audit/UI dump.
- **Negative / abuse variants:** Reuse; dump recovery codes via error payloads.

### 4.4 Password reset

#### TD-ADM-030 — Password reset requires email link AND 2FA; generic reset-request reply

- **Traces-to:** AC7, SPEC-S8.4, SPEC-SEC-4, STEP-4, PLAN-SEC-4
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Enrolled CoreOwner; mail interface fake captures reset link.
- **Steps:** 1) Request reset (+ Turnstile) for the CoreOwner email, for an unknown email, and while the CoreOwner account is locked. 2) Open link without 2FA — fail. 3) Open link + valid TOTP — set new password. 4) Attempt reset with password-only / skip-2FA path. 5) After reset, sign in with new password but no TOTP.
- **Expected:** Step 1 always returns the **same** “If that account exists, we've sent instructions” reply (status + body identical); email only for the real account. Both factors required; skip-2FA path absent; reset email carries link only (no password value). Step 5 refused — **TOTP still required after reset**; reset never bypasses or re-enrolls TOTP (UX1-A03 r2).
- **Negative / abuse variants:** Expired link (>1h); reused link; skip-2FA; reply differences between known / unknown / locked accounts (must be none).
#### TD-ADM-031 — Reset link single-use and expires in 1 hour; generic invalid-link reply

- **Traces-to:** AC7, SPEC-S8.4, SPEC-SEC-4, STEP-4, PLAN-SEC-4
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Mail interface fake.
- **Steps:** 1) Complete reset once. 2) Replay link. 3) Use link aged >1h. 4) Use a random token.
- **Expected:** Replay, expired and random tokens all get the **same** generic invalid-or-expired link reply (same status, same body). Each counts toward the reset/bootstrap per-IP counter (lockout note OQ3).
- **Negative / abuse variants:** Replay; expired; reply differences between cases (must be none).
### 4.5 Turnstile

#### TD-ADM-040 — Turnstile required on login, reset, and bootstrap; reject before credential check; no counter; audit `captcha-failed`

- **Traces-to:** AC7, SPEC-S8.5, SPEC-S8.6, SPEC-S8.8, SPEC-SEC-5, SPEC-SEC-13, STEP-2, STEP-5, PLAN-SEC-5
- **Layer:** API+UI-smoke
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Turnstile test keys via env names only (no site keys in docs). Test access to counters and audit store. Binding: lockout note OQ1 + Architecture Turnstile vs lockout note.
- **Steps:** 1) Submit login, reset-request and bootstrap/reset-link forms with Turnstile token **missing**, **invalid**, **expired**, and with Turnstile **unavailable** (test double). 2) For login, send the **correct** password and also a wrong one with the bad token. 3) Read account counter, login-IP counter, reset/bootstrap-IP counter. 4) Read audit. 5) Submit with valid test token.
- **Expected:** Every bad-Turnstile submission is rejected **before** password / TOTP / recovery-code verification (correct password with bad token still does not sign in; no credential-check side effects). Response may use its own “Verification failed, please try again.” text; it reveals nothing about the account. **No counter changes** (account, login-IP, reset/bootstrap-IP). Audit row reason class **`captcha-failed`** with keyed HMAC-SHA256 IP only; Turnstile token never stored. 25+ bad-Turnstile submissions from one IP do **not** trigger lock or throttle. Valid token proceeds to the next auth step. Provider is Turnstile only.
- **Negative / abuse variants:** Missing token; forged token; expired token; provider outage treated as pass (must fail closed); Turnstile token value in audit/logs.
#### TD-ADM-041 — Non-Turnstile CAPTCHA providers absent (WAF CAPTCHA / reCAPTCHA OUT); verification-failed text stays account-neutral

- **Traces-to:** AC7, SPEC-S8.5, SPEC-S12-OUT, SPEC-SEC-5, SPEC-SEC-14, STEP-5, STEP-12, PLAN-SEC-5, PLAN-SEC-13
- **Layer:** review+API
- **Priority:** P0
- **Automation:** manual+auto
- **Preconditions:** Code/config under review; auth pages available.
- **Steps:** 1) Confirm only Turnstile widget/integration. 2) Grep/config scan for WAF CAPTCHA / reCAPTCHA providers. 3) Compare the verification-failed response for a real CoreOwner email vs an unknown email vs a locked account.
- **Expected:** Turnstile only; other CAPTCHA providers absent from delivery path. Verification-failed response is identical regardless of account (no enumeration); no `Retry-After`; never the word “locked”.
- **Negative / abuse variants:** Second CAPTCHA provider; account-dependent verification-failed text.
### 4.6 Lockout / rate / session / IP

#### TD-ADM-050 — Per-account lockout (5 in sliding 15 min → flat 30 min); client never sees the lock

- **Traces-to:** AC7, SPEC-S8.6, SPEC-S8.8, SPEC-SEC-5, STEP-5, PLAN-SEC-5
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic CoreOwner; valid Turnstile test token on every attempt (so attempts reach the credential check); controllable clock; test access to counters and audit store. Binding: UX1-A03 r2; lockout note v5.3 OQ3 + REPLACED unlock-by-reset.
- **Steps:**
  1) Baseline: capture status + body + headers for a wrong password, an unknown email, a wrong TOTP and a wrong recovery code.
  2) Make 5 counted credential failures (mix bad password / bad TOTP / wrong recovery code) inside 15 minutes.
  3) Submit the **correct** password + valid TOTP while locked.
  4) Submit a wrong password while locked; read the account counter.
  5) Sliding-window edge: on a fresh account make 4 failures, advance the clock so the oldest is **exactly 15 min** old, make 1 more failure → not locked; then 1 more inside the window → locked.
  6) Complete a password reset (email link + 2FA) while the lock is active, then sign in with the new password + valid TOTP before 30 min.
  7) Advance clock to 30 min after the lock started (attempts in between do not extend it); sign in with correct password + TOTP.
  8) Read audit for all steps.
- **Expected:**
  - Steps 1–4: every failure, **including the locked ones and the refused correct password**, returns **401** with a body **byte-identical** to the wrong-password body (copy: “We couldn't sign you in. Check your details and try again later. You can also reset your password.”). No `Retry-After`; no remaining time; no attempt count; never the word “locked” in body or headers.
  - Step 3: correct credentials **still refused** during the lock (the server-side proof of the lock).
  - Step 4: attempt during lock is rejected and audited as reason class **`locked`** but **not counted** (account counter and both IP counters unchanged).
  - Step 5: event exactly 15 min old is outside the window; lock starts at the 5th counted failure inside the window.
  - Step 6: completed reset **does not** end the lock early, does **not** clear the account count, and does **not** reset either IP counter; sign-in still refused (401 generic) until the lock ends; after the lock ends TOTP is **still required** (UX1-A03 r2).
  - Step 7: flat 30 min from lock start; sign-in succeeds after; account counter then starts from zero.
  - No unlock control exists in the public admin UI; the only early unlock is the audited server-side ops (break-glass) command (lockout note v5.3) — out of scope for client tests, checked by review.
  - Audit: failures with reason class bad password / bad 2FA; lock start event with account, keyed-HMAC IP, start time and duration; in-lock attempts as `locked`. No secrets, no raw IP.
- **Negative / abuse variants:** Burst fails; lock persists across IP changes for the same account; timing gap between locked / unknown / wrong-password responses (hash check still runs — flag large gaps); “locked” or duration leaking via headers, body, or UI copy; reset used as a lock bypass.
#### TD-ADM-051 — Per-IP throttle (20 in sliding 15 min → flat 30 min) → HTTP 429 with generic body, no Retry-After (login + reset/bootstrap)

- **Traces-to:** AC7, SPEC-S8.6, SPEC-S8.8, SPEC-SEC-5, STEP-5, PLAN-SEC-5
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Controllable client IP in test (WebApplicationFactory); valid Turnstile test token on every attempt; controllable clock; mail interface fake; test access to counters and audit store. Several synthetic accounts / unknown emails so the account lock (5) does not mask the IP throttle. Binding: UX1-A03 r2 + Chief Spec / CPM correction; lockout note v5.3 (separate reset/bootstrap IP counter; MUST 429). Core API H4 (429 **plus** Retry-After) is a separate track and is **not** the expectation here.
- **Steps:**
  1) **Login:** from one IP, 20 counted credential failures inside 15 minutes (spread across accounts/unknown emails).
  2) From the same IP: further login with wrong credentials, then with a **correct** password + TOTP for a non-locked account.
  3) Read login-IP counter after step 2.
  4) **Reset-request:** from one IP, 20 reset-request submissions (passing Turnstile; mix of real and unknown emails) inside 15 minutes; then a 21st for the real CoreOwner email.
  5) **Bootstrap / reset link:** from one IP, 20 bad or expired link submissions inside 15 minutes; then one more (bad and, separately, a valid token).
  6) Check the login IP counter and the reset/bootstrap IP counter are separate (reset traffic does not throttle login and vice versa).
  7) Advance clock 30 min from throttle start; retry each flow.
  8) Read audit.
- **Expected:**
  - Step 2 (login throttled): **HTTP 429** with a body **byte-identical** to the 401 generic sign-in body (“We couldn't sign you in. Check your details and try again later. You can also reset your password.”), for both wrong and correct credentials (correct credentials still refused while throttled). **No `Retry-After` header.** No remaining time, attempt count, or the word “locked”.
  - Step 3: attempts during the throttle are rejected and audited as reason class **`rate-limited`** but **not counted**; 30 min not extended.
  - Step 4 (reset throttled): **HTTP 429** with the same “If that account exists, we've sent instructions” reply as a non-throttled request; **no email sent** (mail fake records zero sends); no `Retry-After`.
  - Step 5 (bootstrap/reset link throttled): **HTTP 429** with the **same generic sign-in body** as the 401 (lockout note v5.3); no `Retry-After`; a valid token is not consumed while throttled. Before the throttle starts, bad/expired links get the generic invalid-or-expired link reply (TD-ADM-010 / TD-ADM-031).
  - Step 6: counters independent per lockout note.
  - Step 7: throttle lifts after a flat 30 min; that counter starts from zero. A completed password reset does **not** reset either IP counter.
  - Audit: `rate-limited` rows with keyed-HMAC IP only; no raw IP, no secrets.
- **Negative / abuse variants:** `Retry-After` present on any admin.core auth response (FAIL); 429 body differing from the 401 generic body or the generic reset / invalid-link reply (FAIL); account information in 429 responses; cross-IP account lock still applies; process-global limiter must be absent unless Chief Security approved (TD-ADM-054).
- **Note:** UX1-A03 r2 accepts 429 or 401 for the IP throttle; lockout note v5.3 and Chief Spec / CPM lock **MUST 429**. This design asserts 429.
#### TD-ADM-052 — Session idle 30m sliding + absolute 8h; cookie flags

- **Traces-to:** AC7, SPEC-S8.7, SPEC-SEC-5, STEP-5, PLAN-SEC-5
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Authenticated CoreOwner session.
- **Steps:** 1) Inspect Set-Cookie: HttpOnly, Secure, SameSite=Strict on admin.core. 2) Idle >30m → denied. 3) Activity within idle renews sliding window. 4) Absolute >8h from login → denied even if active. 5) After expiry require full password+TOTP.
- **Expected:** Cookie flags correct; idle/absolute enforced; post-expiry full re-auth.
- **Negative / abuse variants:** Cookie without Secure/HttpOnly; SameSite=None invent.

#### TD-ADM-053 — Raw IP only in short-lived rate counters; audit IP = keyed HMAC-SHA256 only

- **Traces-to:** AC7, SPEC-S8.6, SPEC-S10, SPEC-SEC-6, STEP-5, STEP-8, PLAN-SEC-6, PLAN-SEC-8
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Failed login producing audit row; access to counters/audit store in test.
- **Steps:** 1) Trigger login_failure. 2) Inspect audit row IP field — keyed HMAC form, not raw IP, not unkeyed hash. 3) Inspect rate-limit counters — may hold raw IP briefly. 4) Inspect other logs/metrics — no raw IP.
- **Expected:** Audit IP = keyed HMAC-SHA256 only + reason class; raw IP never in audit/logs/metrics; do not invent HMAC key value in docs.
- **Negative / abuse variants:** Unkeyed SHA of IP; raw IP in audit.

#### TD-ADM-054 — Process-global auth flood limiter absent (held until Chief Security approves)

- **Traces-to:** SPEC-SEC-5, SPEC-SEC-13, STEP-5, STEP-12, PLAN-SEC-5, PLAN-SEC-13
- **Layer:** review+API
- **Priority:** P1
- **Automation:** manual+auto
- **Preconditions:** Auth limiter config under review.
- **Steps:** 1) Confirm only per-account + per-IP budgets. 2) Confirm no shared process-global auth budget across all IPs.
- **Expected:** No process-global flood limiter scheduled/implemented without Chief Security approval.
- **Negative / abuse variants:** N/A

### 4.7 Lists / search / paging

#### TD-ADM-060 — Participants list/view/edit/delete + name search

- **Traces-to:** AC2, LOCK-3, SPEC-S4.1, SPEC-S4.6, SPEC-S5, SPEC-SEC-11, SPEC-SEC-12, STEP-7, STEP-9, STEP-10, PLAN-SEC-11, PLAN-SEC-12
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner session; synthetic Participants with searchable display names.
- **Steps:** 1) List. 2) View detail. 3) Edit FieldPolicy-allowed field. 4) Search by display name (case-insensitive contains). 5) Delete via confirm path.
- **Expected:** CRUD works; search matches Product name definition; FieldPolicy enforced.
- **Negative / abuse variants:** Search injection; denied-field dump in search hits.

#### TD-ADM-061 — Artifacts list/view/edit/delete + name search

- **Traces-to:** AC3, LOCK-3, SPEC-S4.2, SPEC-S4.6, SPEC-SEC-11, STEP-7, STEP-9, STEP-10, PLAN-SEC-11
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic Artifacts.
- **Steps:** Same CRUD + search by Artifact name/title/subject.
- **Expected:** CRUD + case-insensitive contains search per Product.
- **Negative / abuse variants:** Injection; full-table client dump.

#### TD-ADM-062 — Negotiations all-status list + sort/filter/paging + name search

- **Traces-to:** AC4, LOCK-3, SPEC-S4.3, SPEC-S4.5, SPEC-S4.6, SPEC-SEC-11, STEP-7, PLAN-SEC-11
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic negotiations covering Open/Closed/Expired/Withdrawn and soft-deleted.
- **Steps:** 1) Default list excludes soft-deleted, includes all non-deleted statuses incl. Withdrawn. 2) Sort/filter status, created/updated, participant, artifact, value/price, negotiation id. 3) Page offset/limit default 50 max 200. 4) Search composite artifact+participant names. 5) Toggle include soft-deleted (CoreOwner).
- **Expected:** Server-side only; parameterized; FieldPolicy on payloads; Withdrawn first-class; soft-deleted only via toggle.
- **Negative / abuse variants:** Client-side full dump; page size >200 rejected; injection.

#### TD-ADM-063 — Offers all-status list + sort/filter/paging + name search

- **Traces-to:** AC4, LOCK-3, SPEC-S4.4, SPEC-S4.5, SPEC-S4.6, SPEC-SEC-11, STEP-7, PLAN-SEC-11
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic offers covering Open/Accepted/Declined/Superseded/Cancelled/Withdrawn + soft-deleted.
- **Steps:** Analogous to TD-ADM-062; search composite includes offering participant + negotiation id; negotiation id also filterable separately.
- **Expected:** All statuses; soft-deleted via CoreOwner toggle; server-side paging 50/200; parameterized search.
- **Negative / abuse variants:** Same as negotiations.

#### TD-ADM-064 — Soft-deleted toggle CoreOwner-only and not audited

- **Traces-to:** AC4, SPEC-S4.5, SPEC-S10, SPEC-S11.1, SPEC-SEC-6, SPEC-SEC-10, STEP-7, STEP-8, PLAN-SEC-8, PLAN-SEC-10, PLAN-SEC-11
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Soft-deleted rows exist; CoreOwner session; non-CoreOwner attempt.
- **Steps:** 1) CoreOwner toggles include soft-deleted — rows appear. 2) Inspect audit — no toggle audit row. 3) Non-CoreOwner cannot use toggle.
- **Expected:** Toggle CoreOwner-only; use is a read; not written to audit.
- **Negative / abuse variants:** Non-CoreOwner toggle; invent toggle audit.

#### TD-ADM-065 — Server-side paging defaults and caps; no client full-table dump

- **Traces-to:** AC4, SPEC-S4.5, SPEC-SEC-11, STEP-7, PLAN-SEC-11
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** >200 synthetic rows.
- **Steps:** 1) Default page size = 50. 2) Request limit=200 OK. 3) Request limit>200 rejected/clamped per Spec max. 4) Confirm responses are pages not full dumps.
- **Expected:** offset/limit server-side; default 50; max 200; no client-only paging that loads whole tables.
- **Negative / abuse variants:** limit=10000 dump attempt.

#### TD-ADM-066 — Parameterized sort/filter/search — injection blocked

- **Traces-to:** AC4, SPEC-S4.5, SPEC-S4.6, SPEC-SEC-11, STEP-7, PLAN-SEC-11
- **Layer:** security
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner session.
- **Steps:** 1) Inject SQL/meta characters into sort, filter, search params. 2) Observe query behavior and responses.
- **Expected:** Parameterized handling; no injection; no denied FieldClass dump.
- **Negative / abuse variants:** OR 1=1; comment injection; union select.

### 4.8 Stats

#### TD-ADM-070 — Overall stats five counts exclude soft-deleted

- **Traces-to:** AC5, LOCK-4, SPEC-S6, SPEC-SEC-11, STEP-7, PLAN-SEC-11
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Known synthetic counts including soft-deleted and open negotiations.
- **Steps:** 1) GET stats. 2) Soft-delete one Participant / open negotiation / offer. 3) Re-GET stats.
- **Expected:** Counts: Participants, open negotiations (Status=Open AND DeletedAt IS NULL), offers, accepts, declines — all exclude soft-deleted. No charts/warehouse.
- **Negative / abuse variants:** Soft-deleted included incorrectly; charts endpoints absent.

### 4.9 Edit / concurrency

#### TD-ADM-080 — Edit FieldPolicy-allowed fields only; before/after audit

- **Traces-to:** AC2, AC3, AC4, AC7, SPEC-S5, SPEC-S10, SPEC-SEC-12, STEP-9, STEP-8, PLAN-SEC-12, PLAN-SEC-8
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner; entity with Version.
- **Steps:** 1) Edit allowed field. 2) Read audit — before/after FieldPolicy-allowed only. 3) Attempt secret/password field in edit form.
- **Expected:** Success audited with before/after; no secrets in form/error/audit snapshots.
- **Negative / abuse variants:** Secret dump; denied field write.

#### TD-ADM-081 — Optimistic concurrency stale write → 409 Conflict

- **Traces-to:** SPEC-S5, SPEC-SEC-12, STEP-9, PLAN-SEC-12
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Two concurrent editors / stale Version.
- **Steps:** 1) Read entity Version V. 2) Update to V+1 externally. 3) PATCH with stale V.
- **Expected:** 409 Conflict with safe message; no silent overwrite.
- **Negative / abuse variants:** Lost update.

### 4.10 Confirm-delete / soft-delete / cascades

#### TD-ADM-090 — Confirm-before-delete UI modal + API confirm token

- **Traces-to:** AC7, LOCK-5, SPEC-S9, SPEC-SEC-9, STEP-10, PLAN-SEC-10
- **Layer:** API+UI-smoke
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner; entity to delete.
- **Steps:** 1) Blind DELETE without token → reject. 2) POST delete-intent → confirmToken + identity + cascade summary. 3) Delete with token within 5 min. 4) Cancel/dismiss → unchanged + no delete audit. 5) Replay token.
- **Expected:** Blind DELETE rejected; token single-use 5 min bound to actor+entity+cascade set; cancel no mutation/no delete audit; UI modal names type+identity+cascade summary.
- **Negative / abuse variants:** Expired token; stolen token wrong actor; blind DELETE.

#### TD-ADM-091 — Soft-delete default DeletedAt; hard delete deferred

- **Traces-to:** AC7, SPEC-S11.1, SPEC-SEC-10, STEP-10, PLAN-SEC-10
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner.
- **Steps:** 1) Delete entity via confirm. 2) Assert DeletedAt set. 3) Probe hard-delete API/UI.
- **Expected:** Soft-delete only; hard delete absent this slice.
- **Negative / abuse variants:** Hard delete invent.

#### TD-ADM-092 — Cascade: delete offer → soft-delete offer only

- **Traces-to:** AC7, SPEC-S11.2, SPEC-SEC-10, STEP-10, PLAN-SEC-10
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Offer with parent negotiation.
- **Steps:** Confirm-delete offer; check parent negotiation unchanged; audit rows.
- **Expected:** Only offer soft-deleted; parent stays; audit for offer.
- **Negative / abuse variants:** Parent wiped.

#### TD-ADM-093 — Cascade: delete negotiation → soft-delete negotiation + child offers

- **Traces-to:** AC7, SPEC-S11.2, SPEC-S11.3, SPEC-SEC-10, STEP-10, PLAN-SEC-10
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Open negotiation with N child offers.
- **Steps:** 1) Confirm warns open + child-offer count. 2) Delete. 3) Assert negotiation+offers soft-deleted. 4) Stats open count drops. 5) Cascade audit rows share correlation id.
- **Expected:** Cascade per Spec; open warning; Artifacts/Participants unchanged; audited.
- **Negative / abuse variants:** Artifact auto-deleted.

#### TD-ADM-094 — Cascade: delete Artifact blocked while referenced by non-deleted negotiation

- **Traces-to:** AC7, SPEC-S11.2, SPEC-SEC-10, STEP-10, PLAN-SEC-10
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Artifact referenced by non-deleted negotiation.
- **Steps:** 1) Attempt delete Artifact. 2) Expect block listing negotiation ids. 3) Soft-delete referencing negotiations. 4) Retry Artifact delete.
- **Expected:** Blocked while referenced; allowed when no non-deleted negotiation references remain.
- **Negative / abuse variants:** Force wipe with references.

#### TD-ADM-095 — Cascade: delete Participant → negotiations+offers; not auto Artifacts

- **Traces-to:** AC7, SPEC-S11.2, SPEC-S11.3, SPEC-SEC-10, STEP-10, PLAN-SEC-10
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Participant in negotiations with offers; related Artifacts exist.
- **Steps:** Confirm states negotiation+offer counts; delete; assert Participant+negotiations+offers soft-deleted; Artifacts remain; human-user delete absent.
- **Expected:** Per Spec §11.2; Artifacts not auto-deleted; settlement cascades absent.
- **Negative / abuse variants:** Artifact wipe; human-user delete.

### 4.11 Audit

#### TD-ADM-100 — Auth events audited (login/reset/TOTP/recovery/captcha/lock/throttle)

- **Traces-to:** AC7, SPEC-S8.8, SPEC-S10, SPEC-SEC-6, STEP-8, PLAN-SEC-8
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Mail/TOTP harness.
- **Steps:** Trigger login_success, login_failure (bad password, bad 2FA, `captcha-failed`, `locked`, `rate-limited`), reset_request, reset_complete, totp_enroll, totp_change, recovery_code_use; read audit.
- **Expected:** Each event present with reason class where applicable (incl. `captcha-failed` added to §8.8 by the lockout note); lock events carry account, keyed-HMAC IP, start time and duration. Reason classes stay in the audit / superadmin view only and are **never** echoed to the admin.core client. No password/TOTP secret/recovery plaintext/raw IP/Turnstile token.
- **Negative / abuse variants:** Secret material in audit; reason class leaked to client response.
#### TD-ADM-101 — Edit/delete audit append-only; cannot edit/delete audit from admin

- **Traces-to:** AC7, SPEC-S10, SPEC-SEC-6, STEP-8, PLAN-SEC-8
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner; existing audit rows.
- **Steps:** 1) Attempt PATCH/DELETE audit via admin API/UI. 2) Confirm CoreOwner can READ audit. 3) Snapshots FieldPolicy-only; >4KiB truncated + length + hash.
- **Expected:** No update/delete path; read OK; truncation rules; retain indefinitely this slice.
- **Negative / abuse variants:** Audit wipe; secret in snapshot.

#### TD-ADM-102 — Same-txn pairing: audit insert fail rolls back mutation

- **Traces-to:** SPEC-S10, SPEC-SEC-6, STEP-8, PLAN-SEC-8
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Test double that fails audit insert.
- **Steps:** 1) Attempt edit/delete while audit insert fails. 2) Assert entity unchanged.
- **Expected:** Whole mutation rolled back; fail-closed.
- **Negative / abuse variants:** Orphan mutation without audit.

### 4.12 Mail / SES

#### TD-ADM-110 — Mail via interface only; no AWS SES SDK in Core app

- **Traces-to:** AC7, AC9, SPEC-S8.9, SPEC-SEC-13, STEP-6, PLAN-SEC-7
- **Layer:** review+integration
- **Priority:** P0
- **Automation:** manual+auto
- **Preconditions:** Codebase / package refs; fake mail adapter in tests.
- **Steps:** 1) Bootstrap/reset use mail interface. 2) Scan Core app for direct SES SDK PackageReference/types. 3) Confirm no invented AWS account IDs/region/identity/from-address/provision/spend.
- **Expected:** Mail interface only; no SES SDK in Core; no account details in docs/plan/tests; CFO cost cite only ($0 Turnstile; SES under $0.01/mo assumed) — not spend approval.
- **Negative / abuse variants:** Hard-coded account IDs; direct SES calls.

### 4.13 OUT / gates / process

#### TD-ADM-120 — OUT pack absent from delivery (SSO, settlement, charts, MM/DC4, inbound connector, platform admin)

- **Traces-to:** AC8, AC9, SPEC-S12-OUT, SPEC-SEC-14, STEP-12, PLAN-SEC-13
- **Layer:** review
- **Priority:** P1
- **Automation:** manual
- **Preconditions:** Delivery notes / routes.
- **Steps:** Confirm OUT items not delivered: platform admin hosts as targets, human-user list, Participant UI-as-admin, multi-admin, email OTP, WAF/reCAPTCHA, SSO/IdP as delivered, inbound bot connector, settlement/escrow/checkout, charts/warehouse, MotorMarket/DC4, .NET 10 retarget as this Story.
- **Expected:** All OUT; gate holds stated.
- **Negative / abuse variants:** N/A

#### TD-ADM-121 — Gate holds respected: Stories/build until Test design PASS; A7 until H4 live PASS; deploy until Ivan OK

- **Traces-to:** AC9, LOCK-7, SPEC-S12-OUT, SPEC-SEC-14, SPEC-SEC-15, STEP-12, STEP-13, PLAN-SEC-13, PLAN-SEC-14
- **Layer:** process-review
- **Priority:** P0
- **Automation:** manual
- **Preconditions:** CPM/BM gate state.
- **Steps:** 1) Confirm no Stories/code started before this Test design PASS. 2) A7 held until H4 live PASS (cite separate harden evidence; do not invent H4 steps). 3) Deploy held until Ivan OK. 4) Do not invent password/AWS values. 5) PoC $0.
- **Expected:** Gates documented and held; not a build unlock from this design alone.
- **Negative / abuse variants:** Premature Stories/build/deploy.

#### TD-ADM-122 — Traceability handshake: Spec tip sha256 + Security SoR cited

- **Traces-to:** SPEC-SEC-15, PLAN-SEC-14, STEP-13
- **Layer:** process-review
- **Priority:** P1
- **Automation:** manual
- **Preconditions:** This test design + binding sources.
- **Steps:** Verify Spec sha256 MATCH; Spec Security PASS 15/15; Dev Plan Security PASS 14/14; SA SoR cited; host admin.core only.
- **Expected:** Trace complete; handshake qa-confirm only for Security SoR.
- **Negative / abuse variants:** Invent points-review SoR.

#### TD-ADM-160 — Inbound bot connector held until after this track (sequence lock)

- **Traces-to:** LOCK-6, AC8, SPEC-S12-OUT, STEP-12, PLAN-SEC-13
- **Layer:** process-review
- **Priority:** P2
- **Automation:** manual
- **Preconditions:** Scope notes.
- **Steps:** Confirm inbound bot connector not in this delivery; sequenced after.
- **Expected:** OUT / after this.
- **Negative / abuse variants:** N/A

#### TD-ADM-161 — Must-cover pack present: auth, confirm-delete, audit, soft-delete cascades, lists/search

- **Traces-to:** LOCK-5, AC7, SPEC-SEC-15, STEP-13, PLAN-SEC-14
- **Layer:** process-review
- **Priority:** P0
- **Automation:** manual
- **Preconditions:** This matrix.
- **Steps:** Confirm P0 cases exist for §8 auth, §9 confirm, §10 audit, §11 delete, §4.5–4.6 lists/search.
- **Expected:** All must-covers mapped to ≥1 P0 automated or auto+manual case.
- **Negative / abuse variants:** N/A

#### TD-ADM-162 — IN pack delivered: host, four entities CRUD, stats, CoreOwner auth, confirm, audit, soft-delete

- **Traces-to:** SPEC-S12-IN, LOCK-3, LOCK-4, LOCK-5, AC1, AC2, AC3, AC4, AC5, AC7
- **Layer:** process-review
- **Priority:** P1
- **Automation:** manual
- **Preconditions:** Delivery checklist after Active holds lift.
- **Steps:** Walk Spec §12 IN against implemented surface.
- **Expected:** All IN items present; no OUT items slipped in.
- **Negative / abuse variants:** N/A

### 4.14 UI smoke

#### TD-ADM-130 — UI smoke: sign-in → stats → list → search → edit → confirm-delete cancel

- **Traces-to:** AC1, AC2, AC3, AC4, AC5, AC7, SPEC-S3, SPEC-S4.5, SPEC-S6, SPEC-S9, STEP-7, STEP-9, STEP-10
- **Layer:** E2E-UI-smoke
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Playwright against admin.core (local/CI; live only after Ivan deploy OK). Synthetic CoreOwner via env. Never MotorMarket/Arctic Circle.
- **Steps:** 1) Open admin.core. 2) Login password+TOTP+Turnstile. 3) See stats. 4) Open each entity list, search, open detail, edit allowed field, open delete confirm and cancel.
- **Expected:** Happy path smoke green; cancel leaves data unchanged.
- **Negative / abuse variants:** N/A (smoke).

#### TD-ADM-131 — UI smoke: unauthenticated redirect/deny on admin pages

- **Traces-to:** AC1, AC6, SPEC-S8.1, SPEC-SEC-7, STEP-11
- **Layer:** E2E-UI-smoke
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Logged out browser.
- **Steps:** Navigate admin entity URLs without session.
- **Expected:** Sign-in required / safe deny; no data flash.
- **Negative / abuse variants:** Cached HTML entity dump.

### 4.15 Empty / validation / secrets

#### TD-ADM-140 — Empty states for lists and search misses

- **Traces-to:** AC2, AC3, AC4, SPEC-S4.5, SPEC-S4.6, STEP-7
- **Layer:** API+UI-smoke
- **Priority:** P2
- **Automation:** auto
- **Preconditions:** Empty dataset or unmatched search.
- **Steps:** List empty tables; search with no hits.
- **Expected:** Safe empty payload/UI; no errors dumping internals.
- **Negative / abuse variants:** N/A

#### TD-ADM-141 — Validation errors safe; denied FieldClass values not echoed

- **Traces-to:** SPEC-S5, SPEC-SEC-7, SPEC-SEC-12, STEP-9, PLAN-SEC-9, PLAN-SEC-12
- **Layer:** API
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** CoreOwner.
- **Steps:** Submit invalid field values; submit denied FieldClass.
- **Expected:** Field-level safe messages; denied values not echoed (SA residual copy — assert no dump).
- **Negative / abuse variants:** Error reflects secret/denied value.

#### TD-ADM-150 — No password/TOTP/recovery/HMAC/Turnstile/SES secrets in logs or docs

- **Traces-to:** AC7, AC9, SPEC-S8.10, SPEC-S12-OUT, SPEC-SEC-2, SPEC-SEC-13, SPEC-SEC-14, STEP-2, STEP-6, STEP-12, PLAN-SEC-2, PLAN-SEC-7, PLAN-SEC-13
- **Layer:** security-review
- **Priority:** P0
- **Automation:** manual+auto
- **Preconditions:** Test run logs; KB docs; plan; this design.
- **Steps:** Scan logs and docs for password values, TOTP secrets, recovery plaintext, HMAC keys, Turnstile site keys, AWS account IDs, SES identities.
- **Expected:** None present; secrets referenced by env/secret-store name only.
- **Negative / abuse variants:** N/A

---

## 5. Entry / exit criteria and evidence

| Phase | Entry | Exit | Evidence location |
|-------|-------|------|-------------------|
| Story-level (after Active holds lift) | A6 PASS; A7 unlocked after H4; Story assigned | Story P0 API/integration green for mapped TD-ADM IDs | `/workspace/qa/YYYY-MM-DD__qa__qa-report__core-admin-*.md` |
| PR CI | PR touches admin surface | CI: all P0 automated cases for changed area PASS; no secret invent in diff | CI logs + `/workspace/qa/...` report path |
| Pre-deploy | CI green; CQ gate; deploy held until Ivan OK | Pre-deploy checklist: TD-ADM-121 gates + TD-ADM-150 no-secret scan | `/workspace/qa/...__predeploy__...md` |
| Post-deploy live smoke | **Ivan OK** deploy; host `admin.core.dealoware.com` | Live smoke: TD-ADM-001/003/070/130/131 (+ subset P0 auth if secrets available via env) PASS | `/workspace/qa/YYYY-MM-DD__qa__qa-report__core-admin-live-smoke.md` |

KB twin of design: `dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`. Runtime evidence under `/workspace/qa/` (do not invent AWS account IDs in evidence).

---

## 6. Open questions / assumptions

| # | Item | Source | Disposition |
|---|------|--------|-------------|
| OQ1 | Do CAPTCHA (Turnstile) failures count toward the same per-account / per-IP lockout budgets? | Spec §8.6 “SA to confirm” | **Closed** by lockout note (`specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md`, OQ1) + Architecture Turnstile note: Turnstile reject happens before any credential check, counts toward no counter, audited `captcha-failed`. Tested in TD-ADM-040 |
| OQ2 | TOTP digits/period and recovery-code count (Spec recommends 10) | Spec §8.3 “SA to confirm” | Open — do not invent; tests assert hashed single-use + show-once; exact count/params from SA when locked |
| OQ3 | Sliding vs fixed window for lockout counters | Spec §8.6 “SA to confirm” | **Closed** by lockout note OQ3: sliding 15-min window (event exactly 15 min old is outside), 5 per account / 20 per IP, flat 30-min lock/throttle, in-lock attempts rejected + audited but not counted, wrong recovery code = bad 2FA, separate reset/bootstrap per-IP counter. Tested in TD-ADM-050 / TD-ADM-051 |
| OQ4 | Admin Host-header reject for non-`admin.core` at app vs edge | Spec §3 “served only as” | Open — do not invent edge details; TD-ADM-001 tests app binding if present; do not target platform hosts as positive cases |
| OQ5 | Retry-After on admin auth 429 | Spec §8.6 “HTTP 429 / equivalent” | **Closed** by UX1-A03 r2: admin.core IP throttle may return **429** with the generic body; **no admin.core auth response ever carries `Retry-After`** (nor remaining time / attempt count / “locked”). TD-ADM-051 asserts 429 + absence of Retry-After. Core API H4 (429 + Retry-After) is separate and unchanged |
| OQ6 | Validation error copy exact wording | Spec §5 SA residual | Open — do not invent copy; TD-ADM-141 asserts no denied-field/secret echo |
| OQ7 | Lockout note vs UX1-A03 r2 on IP-throttle status, reset-unlock and copy | Lockout note v4 (`e45ffeac…`) conflicted; v5.3 (`67686ce5…`) aligns | **Aligned** on disk with v5.3 (MUST 429, reset does not lift lock, r2 copy). Active hold: v5.3 is still Draft for Spec QA + Security QA; re-cite final hash after their PASS |
| OQ8 | New v5.3 scope: break-glass ops unlock, S-A12 recent sign-in activity panel, lock-notice email (SHOULD) | Lockout note v5.3 / Chief Security answers items 3–4 | Open — not covered by a dedicated case in r2 (53 cases kept). Proposed follow-up cases for the next revision once v5.3 passes Spec QA; do not invent behavior now |

**Assumptions (from locked sources — not invent):** soft-delete marker `DeletedAt`; paging offset/limit 50/200; Withdrawn first-class; session cookie HttpOnly Secure SameSite=Strict; audit IP keyed HMAC-SHA256; Turnstile only; SES behind mail interface; email OTP OUT; hard delete deferred.

---

## 7. Self-check

- [x] Every Product AC1–AC9 mapped
- [x] Every Spec Locked #1–#7 mapped
- [x] Every listed Spec § requirement section mapped
- [x] Spec Security pts **15/15** mapped
- [x] Dev Plan Steps **13/13** mapped
- [x] Dev Plan Security pts **14/14** mapped
- [x] Security points total **29/29**
- [x] No password values, AWS account IDs, SES identities, Turnstile keys, or HMAC keys in this document
- [x] Host under test / positive target: `admin.core.dealoware.com` only
- [x] Platform hosts not used as positive targets; no CDK / bot-platform / App Runner invent
- [x] No MotorMarket / Arctic Circle test data
- [x] UX1-A03 r2 applied: account-level failures 401 identical; IP throttle 429 generic body; no Retry-After on admin.core; never “locked”; reset does not lift lock; TOTP after reset
- [x] OQ1 / OQ3 / OQ5 closed with source refs; Core API H4 429 + Retry-After unchanged
- [x] Active holds: Stories/build until Test design QA confirms this revision; A7 until H4 live PASS; deploy until Ivan OK
- [x] PoC $0

---

## 8. Programmatic self-verification

Method: extract Product `AC[1-9]`, Spec Locked table rows `1–7`, Spec §13 MET rows `1–15`, Spec checklist numbered pts `1–15`, Dev Plan `### Step N`, Dev Plan checklist pts `1–14`, plus Spec section IDs listed in §3.3; diff against traces in this design.

| Check | Result |
|-------|--------|
| Spec tip sha256 | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` MATCH |
| Plan tip sha256 | `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf` MATCH |
| Extracted source IDs | 84 |
| IDs covered by ≥1 TD-ADM | 84 / 84 |
| Gaps | **none** |
| Test cases | 53 (P0=45, P1=6, P2=2) |
| Secret/account-ID invent scan (this file) | PASS — no password values; no AWS account IDs; no SES/Turnstile/HMAC secret values |

**Coverage: 0 gaps** (Chief QA self-check on r2; not a Test design QA stamp). Active holds: Stories/build until Test design QA confirms this tip; A7 until H4 live PASS; deploy until Ivan OK; lockout note v5.3 hash to re-cite after Spec QA. Do not invent password/AWS values. PoC $0.

---

## 9. Confirm

**A6 Test design r2 — revised, awaiting Test design QA re-verify.** Path `qa/2026-10-05__qa__test-design__core-admin-dashboard.md`. Cases **53** (P0 **45**). Sources: AC **9/9**, Spec Locked **7/7**, Spec sections **26/26**, Spec Security **15/15**, Plan Steps **13/13**, Plan Security **14/14**, Security points **29/29**. Host `admin.core.dealoware.com` only. No secrets / account IDs invented. Active holds: Stories/build until Test design QA confirms; A7 until H4; deploy until Ivan OK; lockout note v5.3 final hash. PoC $0.
