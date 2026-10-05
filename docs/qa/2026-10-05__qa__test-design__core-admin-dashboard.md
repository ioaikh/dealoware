# Test design — Core admin dashboard (A6)

| Field | Value |
|-------|--------|
| Written by | Dealoware Chief QA |
| Date | 2026-10-05 (~2:06–2:15pm ET) |
| Status | **PASS** — A6 Test design complete; Stories/build Soft HOLD until this PASS; Soft HOLD A7 until H4 live PASS; Soft HOLD deploy until Ivan OK |
| Host | `admin.core.dealoware.com` **only** (Core admin). Platform hosts are not Core and are not positive test targets. |
| Principal | **CoreOwner** = single system superadmin `io@aiknowhow.com` |
| PoC | **$0** |
| DOC-FLOW | `qa/2026-10-05__qa__test-design__core-admin-dashboard.md` |
| Constraints | Test design ONLY. Soft HOLD invent Stories / code / PRs / CDK / spend / provision / deploy. Soft HOLD invent passwords / AWS account IDs / region / SES identity / Turnstile site keys / HMAC key values. Soft HOLD A7 until H4 live PASS. Soft HOLD deploy until Ivan OK. Never MotorMarket buyer/seller sessions or Arctic Circle inventory. |

---

## 1. Binding inputs

| Source | Path | sha256 / note |
|--------|------|---------------|
| Spec v2.2 | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — **MATCH** |
| Spec QA PASS | `verification/2026-10-05__spec__verification__core-admin-dashboard.md` | Formal Spec gate PASS |
| Dev Plan A5 | `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` | `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf` — **MATCH** |
| Dev Plan QA PASS | `verification/2026-10-05__devplan__verification__core-admin-dashboard.md` | Dev Plan gate PASS |
| Spec Security Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` | **PASS 15/15** |
| Spec Security checklist v2 | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` | Pts **1–15** |
| Dev Plan Security Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` | **PASS 14/14** |
| Dev Plan Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` | Pts **1–14** |
| SA Soft HOLD SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` | Option A + §§3.1–3.9 |
| Product scope 13:31 | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | AC1–AC9 binding |
| CFO Soft HOLD estimate | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` | Turnstile $0; SES under $0.01/mo assumed — estimate only |

### Gates (binding)

| Gate | Rule |
|------|------|
| Stories / build | Soft HOLD until **this Test design PASS** |
| A7 admin build | Soft HOLD until **H4 live PASS** (separate harden track; do not invent H4 steps here) |
| Deploy | Soft HOLD until **Ivan OK** |
| Secrets / AWS | Soft HOLD invent password values and AWS account details |
| PoC | **$0** — not a spend/provision unlock |

### Scope

- Test design for Core admin dashboard at **`admin.core.dealoware.com`** / Core admin API only.
- Cover Product AC1–AC9, Spec Locked #1–#7 + §§3–12 requirement surface, Spec Security pts 1–15, Dev Plan Steps 1–13, Dev Plan Security pts 1–14.
- Automated API / integration first; minimal UI smoke (Playwright).

### Out of scope

- Writing Stories, code, PRs, CDK, provision, deploy, or spend.
- Inventing passwords, AWS account IDs, SES identity/from-address, Turnstile keys, HMAC keys.
- Platform admin hosts as positive targets; MotorMarket / DC4 / Arctic Circle inventory.
- H1–H6 harden live re-verify (separate track). Note only: A7 Soft HOLD until H4 live PASS; admin Spec §8.6 requires HTTP **429 / equivalent** (Retry-After **not** mandated by Spec — Soft HOLD invent Retry-After as Spec requirement).
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
| Process-review | Soft HOLD / OUT / gate pack / no-secret scan | Manual checklist |

### Environments

| Env | When | Notes |
|-----|------|-------|
| Local | Story / PR development after Soft HOLDs lift | Fake mail adapter; Turnstile test keys via env name only |
| CI | Every admin PR | Automated P0 API/integration; UI smoke optional in CI if headed runners available |
| Live `admin.core.dealoware.com` | **Only after** Ivan deploy OK | Smoke subset; Soft HOLD invent AWS details in evidence |

### Test data rules

- **Synthetic only** — no real customer data; no MotorMarket buyer/seller sessions; no Arctic Circle inventory.
- CoreOwner login email under test is the Spec lock `io@aiknowhow.com`; **password / TOTP seed / recovery codes** live only in env or secret store, referenced **by name** (e.g. `COREOWNER_PASSWORD_ENV`, `TOTP_SEED_ENV`). **Never** write values into this design, fixtures committed to git, chat, or reports.
- Mail: fake/in-memory mail interface adapter in tests; Soft HOLD invent SES account/region/identity.
- CAPTCHA: Turnstile **test** keys via env; Soft HOLD invent production site keys in docs.

### Prefer

1. Automated API / integration covering auth, RBAC, lists, edit, delete, audit.
2. Minimal UI smoke for AC1 shell + confirm modal cancel.
3. Manual process-review for Soft HOLD / OUT / no-secret scans.

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
| AC9 | Build/deploy Soft HOLD; invent AWS Soft HOLD; PoC $0 | TD-ADM-110, TD-ADM-120, TD-ADM-121, TD-ADM-150 |

### 3.2 Spec Locked decisions #1–#7

| ID | Summary | Test cases |
|----|---------|------------|
| LOCK-1 | Host admin.core only | TD-ADM-001 |
| LOCK-2 | One CoreOwner; FieldPolicy; no multi-admin ACL | TD-ADM-002, TD-ADM-005, TD-ADM-007 |
| LOCK-3 | Four entities list/view/edit/delete + lists/search | TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-162 |
| LOCK-4 | Overall stats | TD-ADM-070, TD-ADM-162 |
| LOCK-5 | Must-cover auth/confirm/audit/delete/lists | TD-ADM-090, TD-ADM-161, TD-ADM-162 |
| LOCK-6 | Inbound bot connector after this | TD-ADM-160 |
| LOCK-7 | Build/deploy/Stories Soft HOLD; no password invent | TD-ADM-121 |

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
| SPEC-S8.6 | §8.6 Lockout/rate + raw IP | TD-ADM-050, TD-ADM-051, TD-ADM-053 |
| SPEC-S8.7 | §8.7 Session lifetime | TD-ADM-003, TD-ADM-052 |
| SPEC-S8.8 | §8.8 Auth audit events | TD-ADM-100 |
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
| 14 | OUT / Soft HOLD pack | TD-ADM-006, TD-ADM-007, TD-ADM-011, TD-ADM-021, TD-ADM-041, TD-ADM-120, TD-ADM-121, TD-ADM-150 |
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
| 12 | Explicit Soft HOLD / OUT / gates | TD-ADM-006, TD-ADM-007, TD-ADM-011, TD-ADM-021, TD-ADM-041, TD-ADM-054, TD-ADM-120, TD-ADM-121, TD-ADM-150, TD-ADM-160 |
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
| 7 | SES mail interface; Soft HOLD invent AWS | TD-ADM-110, TD-ADM-150 |
| 8 | Audit auth+edit/delete; toggle not audited | TD-ADM-053, TD-ADM-064, TD-ADM-080, TD-ADM-100, TD-ADM-101, TD-ADM-102 |
| 9 | Fail-closed FieldPolicy dual wall | TD-ADM-002, TD-ADM-003, TD-ADM-004, TD-ADM-005, TD-ADM-007, TD-ADM-141 |
| 10 | Confirm-delete + soft-delete cascades | TD-ADM-064, TD-ADM-090, TD-ADM-091, TD-ADM-092, TD-ADM-093, TD-ADM-094, TD-ADM-095 |
| 11 | Lists/search/paging safe | TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-065, TD-ADM-066, TD-ADM-070 |
| 12 | Edit + concurrency | TD-ADM-005, TD-ADM-060, TD-ADM-080, TD-ADM-081, TD-ADM-141 |
| 13 | OUT / Soft HOLD / A7 gate pack | TD-ADM-006, TD-ADM-007, TD-ADM-011, TD-ADM-021, TD-ADM-041, TD-ADM-054, TD-ADM-120, TD-ADM-121, TD-ADM-150, TD-ADM-160 |
| 14 | Traceability + handshake Soft HOLD SoR | TD-ADM-121, TD-ADM-122, TD-ADM-161 |

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

#### TD-ADM-010 — Bootstrap link single-use and time-limited (≤24h)

- **Traces-to:** AC7, SPEC-S8.2, SPEC-SEC-2, STEP-2, PLAN-SEC-2
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Test harness can mint bootstrap token via mail interface fake (no real SES).
- **Steps:** 1) Consume bootstrap link — set first password + enroll TOTP. 2) Reuse same link. 3) Use expired (>24h) link.
- **Expected:** First use succeeds; reuse fails; expired fails. Password never in email body/docs/fixtures.
- **Negative / abuse variants:** Replay; expired; truncated token.

#### TD-ADM-011 — First password set only via bootstrap link; no seeded password

- **Traces-to:** AC7, SPEC-S8.1, SPEC-S8.2, SPEC-S8.10, SPEC-SEC-2, SPEC-SEC-14, STEP-2, STEP-12, PLAN-SEC-2, PLAN-SEC-13
- **Layer:** integration+review
- **Priority:** P0
- **Automation:** auto+manual
- **Preconditions:** Fresh CoreOwner identity without password.
- **Steps:** 1) Attempt login before bootstrap. 2) Set password only via bootstrap page with Turnstile. 3) Scan fixtures/docs/repo for password values (grep Soft HOLD invent).
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
- **Steps:** 1) Password OK, omit TOTP. 2) Password OK + wrong TOTP. 3) Password OK + valid TOTP.
- **Expected:** Omit/wrong → fail (reason class bad_2FA); valid → session issued.
- **Negative / abuse variants:** Reuse old TOTP code; clock skew beyond window.

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

#### TD-ADM-030 — Password reset requires email link AND 2FA

- **Traces-to:** AC7, SPEC-S8.4, SPEC-SEC-4, STEP-4, PLAN-SEC-4
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Enrolled CoreOwner; mail interface fake captures reset link.
- **Steps:** 1) Request reset (+ Turnstile). 2) Open link without 2FA — fail. 3) Open link + valid TOTP — set new password. 4) Attempt reset with password-only / skip-2FA path.
- **Expected:** Both factors required; skip-2FA path absent; reset email carries link only (no password value).
- **Negative / abuse variants:** Expired link (>1h); reused link; skip-2FA.

#### TD-ADM-031 — Reset link single-use and expires in 1 hour

- **Traces-to:** AC7, SPEC-S8.4, SPEC-SEC-4, STEP-4, PLAN-SEC-4
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Mail interface fake.
- **Steps:** 1) Complete reset once. 2) Replay link. 3) Use link aged >1h.
- **Expected:** Replay and expired fail safely.
- **Negative / abuse variants:** Replay; expired.

### 4.5 Turnstile

#### TD-ADM-040 — Turnstile required on login, reset, and bootstrap password-set

- **Traces-to:** AC7, SPEC-S8.5, SPEC-SEC-5, SPEC-SEC-13, STEP-2, STEP-5, PLAN-SEC-5
- **Layer:** API+UI-smoke
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Turnstile test keys via env (Soft HOLD invent site keys in docs).
- **Steps:** 1) Submit login/reset/bootstrap without Turnstile token. 2) Submit with invalid token. 3) Submit with valid test token.
- **Expected:** Missing/invalid CAPTCHA rejected; valid proceeds to next auth step. Provider is Turnstile only.
- **Negative / abuse variants:** Missing token; forged token.

#### TD-ADM-041 — Non-Turnstile CAPTCHA providers absent (WAF CAPTCHA / reCAPTCHA OUT)

- **Traces-to:** AC7, SPEC-S8.5, SPEC-S12-OUT, SPEC-SEC-5, SPEC-SEC-14, STEP-5, STEP-12, PLAN-SEC-5, PLAN-SEC-13
- **Layer:** review+API
- **Priority:** P0
- **Automation:** manual+auto
- **Preconditions:** Code/config under review; auth pages available.
- **Steps:** 1) Confirm only Turnstile widget/integration. 2) Grep/config scan for WAF CAPTCHA / reCAPTCHA providers.
- **Expected:** Turnstile only; other CAPTCHA providers absent from delivery path.
- **Negative / abuse variants:** N/A

### 4.6 Lockout / rate / session / IP

#### TD-ADM-050 — Per-account lockout 5 fails / 15 min → 30 min lock

- **Traces-to:** AC7, SPEC-S8.6, SPEC-SEC-5, STEP-5, PLAN-SEC-5
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic CoreOwner; Turnstile satisfied or bypassed only via test harness approved for lockout tests.
- **Steps:** 1) Submit 5 failed password or 2FA attempts within 15 min. 2) Attempt further login. 3) Wait/simulate 30 min or complete reset unlock.
- **Expected:** After 5 fails, account locked 30 min (or unlock via completed reset). Failures audited with reason class locked — no secrets.
- **Negative / abuse variants:** Burst fails; lock persists across IP changes for same account.

#### TD-ADM-051 — Per-IP throttle 20 fails / 15 min → 30 min HTTP 429 (login + reset-request)

- **Traces-to:** AC7, SPEC-S8.6, SPEC-SEC-5, STEP-5, PLAN-SEC-5
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Controllable client IP in test (or WebApplicationFactory). Note: live Core API H4 429+Retry-After Soft HOLD FAIL is a separate harden track; admin Spec requires HTTP 429 / equivalent — Retry-After not mandated by Spec §8.6.
- **Steps:** 1) From one IP, 20 failed logins in 15 min. 2) Further login → 429. 3) Same window on reset-request → 429. 4) After 30 min throttle lifts.
- **Expected:** HTTP 429 (or Spec-equivalent) for 30 min on login and reset-request. Soft HOLD invent process-global flood limiter.
- **Negative / abuse variants:** Cross-IP account lock still applies; process-global limiter must be absent unless Chief Security approved.

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
- **Expected:** Audit IP = keyed HMAC-SHA256 only + reason class; raw IP never in audit/logs/metrics; Soft HOLD invent HMAC key value in docs.
- **Negative / abuse variants:** Unkeyed SHA of IP; raw IP in audit.

#### TD-ADM-054 — Process-global auth flood limiter absent (Soft HOLD until Chief Security approves)

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

#### TD-ADM-066 — Parameterized sort/filter/search — injection Soft HOLD

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

#### TD-ADM-100 — Auth events audited (login/reset/TOTP/recovery)

- **Traces-to:** AC7, SPEC-S8.8, SPEC-S10, SPEC-SEC-6, STEP-8, PLAN-SEC-8
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Mail/TOTP harness.
- **Steps:** Trigger login_success, login_failure, reset_request, reset_complete, totp_enroll, totp_change, recovery_code_use; read audit.
- **Expected:** Each event present with reason class where applicable; no password/TOTP secret/recovery plaintext/raw IP.
- **Negative / abuse variants:** Secret material in audit.

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

### 4.12 Mail / SES Soft HOLD

#### TD-ADM-110 — Mail via interface only; no AWS SES SDK in Core app

- **Traces-to:** AC7, AC9, SPEC-S8.9, SPEC-SEC-13, STEP-6, PLAN-SEC-7
- **Layer:** review+integration
- **Priority:** P0
- **Automation:** manual+auto
- **Preconditions:** Codebase / package refs; fake mail adapter in tests.
- **Steps:** 1) Bootstrap/reset use mail interface. 2) Scan Core app for direct SES SDK PackageReference/types. 3) Confirm Soft HOLD invent AWS account IDs/region/identity/from-address/provision/spend.
- **Expected:** Mail interface only; no SES SDK in Core; no account details in docs/plan/tests; CFO Soft HOLD cost cite only ($0 Turnstile; SES under $0.01/mo assumed) — not spend approval.
- **Negative / abuse variants:** Hard-coded account IDs; direct SES calls.

### 4.13 OUT / gates / process

#### TD-ADM-120 — OUT pack absent from delivery (SSO, settlement, charts, MM/DC4, inbound connector, platform admin)

- **Traces-to:** AC8, AC9, SPEC-S12-OUT, SPEC-SEC-14, STEP-12, PLAN-SEC-13
- **Layer:** review
- **Priority:** P1
- **Automation:** manual
- **Preconditions:** Delivery notes / routes.
- **Steps:** Confirm OUT items not delivered: platform admin hosts as targets, human-user list, Participant UI-as-admin, multi-admin, email OTP, WAF/reCAPTCHA, SSO/IdP as delivered, inbound bot connector, settlement/escrow/checkout, charts/warehouse, MotorMarket/DC4, .NET 10 retarget as this Story.
- **Expected:** All OUT; Soft HOLD gates stated.
- **Negative / abuse variants:** N/A

#### TD-ADM-121 — Gate Soft HOLDs respected: Stories/build until Test design PASS; A7 until H4 live PASS; deploy until Ivan OK

- **Traces-to:** AC9, LOCK-7, SPEC-S12-OUT, SPEC-SEC-14, SPEC-SEC-15, STEP-12, STEP-13, PLAN-SEC-13, PLAN-SEC-14
- **Layer:** process-review
- **Priority:** P0
- **Automation:** manual
- **Preconditions:** CPM/BM gate state.
- **Steps:** 1) Confirm no Stories/code started before this Test design PASS. 2) Soft HOLD A7 until H4 live PASS (cite separate harden evidence; do not invent H4 steps). 3) Soft HOLD deploy until Ivan OK. 4) Soft HOLD invent password/AWS. 5) PoC $0.
- **Expected:** Gates documented and held; not a build unlock from this design alone.
- **Negative / abuse variants:** Premature Stories/build/deploy.

#### TD-ADM-122 — Traceability handshake: Spec tip sha256 + Security Soft HOLD SoR cited

- **Traces-to:** SPEC-SEC-15, PLAN-SEC-14, STEP-13
- **Layer:** process-review
- **Priority:** P1
- **Automation:** manual
- **Preconditions:** This test design + binding sources.
- **Steps:** Verify Spec sha256 MATCH; Spec Security PASS 15/15; Dev Plan Security PASS 14/14; SA Soft HOLD SoR cited; host admin.core only.
- **Expected:** Trace complete; handshake qa-confirm only for Security Soft HOLD SoR.
- **Negative / abuse variants:** Invent points-review Soft HOLD SoR.

#### TD-ADM-160 — Inbound bot connector Soft HOLD after this track (sequence lock)

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
- **Preconditions:** Delivery checklist after Soft HOLDs lift.
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
| Story-level (after Soft HOLDs lift) | A6 PASS; A7 unlocked after H4; Story assigned | Story P0 API/integration green for mapped TD-ADM IDs | `/workspace/qa/YYYY-MM-DD__qa__qa-report__core-admin-*.md` |
| PR CI | PR touches admin surface | CI: all P0 automated cases for changed area PASS; no secret invent in diff | CI logs + `/workspace/qa/...` report path |
| Pre-deploy | CI green; CQ gate; Ivan deploy Soft HOLD still until Ivan OK | Pre-deploy checklist: TD-ADM-121 gates + TD-ADM-150 no-secret scan | `/workspace/qa/...__predeploy__...md` |
| Post-deploy live smoke | **Ivan OK** deploy; host `admin.core.dealoware.com` | Live smoke: TD-ADM-001/003/070/130/131 (+ subset P0 auth if secrets available via env) PASS | `/workspace/qa/YYYY-MM-DD__qa__qa-report__core-admin-live-smoke.md` |

KB twin of design: `dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`. Runtime evidence under `/workspace/qa/` (not invent AWS account IDs in evidence).

---

## 6. Open questions / assumptions

| # | Item | Source | Disposition |
|---|------|--------|-------------|
| OQ1 | Do CAPTCHA (Turnstile) failures count toward the same per-account / per-IP lockout budgets? | Spec §8.6 “SA to confirm” | Soft HOLD invent — ask SA/CPM; tests treat CAPTCHA fail separately until answered |
| OQ2 | TOTP digits/period and recovery-code count (Spec recommends 10) | Spec §8.3 “SA to confirm” | Soft HOLD invent — tests assert hashed single-use + show-once; exact count/params from SA Soft HOLD SoR when locked |
| OQ3 | Sliding vs fixed window for lockout counters | Spec §8.6 “SA to confirm” | Soft HOLD invent — tests use Spec numbers (5/15→30; 20/15→30); window shape per SA |
| OQ4 | Admin Host-header reject for non-`admin.core` at app vs edge | Spec §3 “served only as” | Soft HOLD invent edge details; TD-ADM-001 tests app binding if present; do not target platform hosts as positive cases |
| OQ5 | Retry-After on admin auth 429 | Spec §8.6 says “HTTP 429 / equivalent”; harden H4 Soft HOLD FAIL is separate Core API track | Soft HOLD invent Retry-After as Spec must; TD-ADM-051 requires **429**; Retry-After asserted only if Spec/SA later locks it |
| OQ6 | Validation error copy exact wording | Spec §5 SA residual | Soft HOLD invent copy; TD-ADM-141 asserts no denied-field/secret echo |

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
- [x] Soft HOLD Stories/build until this PASS; Soft HOLD A7 until H4; Soft HOLD deploy until Ivan OK
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

**Status: PASS** — programmatic coverage **0 gaps**. Soft HOLD invent Stories/build until this PASS is consumed by CPM. Soft HOLD A7 until H4 live PASS. Soft HOLD deploy until Ivan OK. Soft HOLD invent password/AWS. PoC $0.

---

## 9. Confirm

**A6 Test design PASS.** Path `qa/2026-10-05__qa__test-design__core-admin-dashboard.md`. Cases **53** (P0 **45**). Sources: AC **9/9**, Spec Locked **7/7**, Spec sections **26/26**, Spec Security **15/15**, Plan Steps **13/13**, Plan Security **14/14**, Security points **29/29**. Host `admin.core.dealoware.com` only. No secrets / account IDs invented. Soft HOLD Stories/build; Soft HOLD A7 until H4; Soft HOLD deploy until Ivan OK. PoC $0.
