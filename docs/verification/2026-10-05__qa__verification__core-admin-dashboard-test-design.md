# Verification — Core admin dashboard Test design (A6) — independent QAQA

| Field | Value |
|-------|--------|
| Author | Dealoware QAQA (independent meta-QA) |
| Date | 2026-10-05 |
| Scope | Independent review of Chief QA Test design for Core admin dashboard (A6). Not a rubber-stamp of Chief QA self-score. |
| Overall verdict | **PASS** |
| A6 consequence | A6 may close on this PASS. Soft HOLD invent A6 docs PR was held until this PASS; Soft HOLD invent Stories/code Soft HOLD A7 until H4 live PASS remains. |
| Independence | **Independent of Chief QA self-score; not a rubber-stamp.** Every AC, Dev Plan step, checklist A6 requirement, and security point was re-checked against source documents. |

---

## Inputs (paths + sha256 computed this review)

| Input | Path | sha256 (computed) | Note |
|-------|------|-------------------|------|
| Test design (under review) | `/workspace/dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md` | `870f348c61e2b3d88c2cd6a99e6ae4398186e96c1a7169a16c0b52e2bdd7bca7` | **MATCH** expected `870f348c…bdd7bca7` |
| Spec v2.2 | `/workspace/dealoware-kb/specs/2026-10-05__spec__spec__core-admin-dashboard.md` | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` | Header Status: **v2.2, Chief Spec CLEAR.** Revision field cites v2.2. Confirmed. |
| Dev Plan A5 | `/workspace/dealoware-kb/plans/2026-10-05__devplan__plan__core-admin-dashboard.md` | `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf` | Steps 1–13 present |
| PM checklist (A6) | `/workspace/pm/admin-dashboard-checklist.md` | `a945661eca5d3a1f521d9c09119c0b6afd81a37c93597bcc5515501bfda0dd10` | A6 row REOPENED pending this PASS (CPM cites “checklist v2”) |
| Spec Security points (authoritative) | `/workspace/dealoware-kb/verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` | (checklist ISSUED v2 pts **1–15**) | Also mirrored MET in Spec §13 |
| Dev Plan Security points (authoritative) | `/workspace/dealoware-kb/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` | (checklist ISSUED pts **1–14**) | Binding Dev Plan-step handshake |
| Spec Security Soft HOLD SoR (cite) | `/workspace/dealoware-kb/verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` | PASS 15/15 (cited by design; not re-scored here) | |
| Dev Plan Security Soft HOLD SoR (cite) | `/workspace/dealoware-kb/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` | PASS 14/14 (cited by design; not re-scored here) | |

**Security-points source decision:** Authoritative itemized points are the two Chief Security checklists above (**15 + 14 = 29**). Spec §13 binds Spec checklist v2 pts 1–15. No missing source — FAIL-if-absent does not apply.

**Case inventory (independent count):** **53** `TD-ADM-###` cases (P0=45, P1=6, P2=2) — matches design §3.7 / §9. Of these, **50** carry at least one `SPEC-SEC-*` or `PLAN-SEC-*` trace. The **29** figure is the **security-points universe** (Spec 1–15 + Plan 1–14), not a separate list of 29 solitary cases.

---

## Overall verdict: **PASS**

Checks A–E all **PASS**. Section F locks noted (PoC $0 present; Soft HOLD multi-provider / Gate #26/#27 not labeled in this design — optional polish only; does not fail A–E).

---

## A. AC coverage (Spec §14 / Product AC1–AC9)

Spec §14 (lines ~507–515) confirms AC1–AC9. Coverage checked against case bodies (preconditions / steps / expected / negatives), not only the matrix.

| AC | Spec §14 summary (short) | Case IDs | Positive + negative/edge | Verdict |
|----|--------------------------|----------|--------------------------|---------|
| AC1 | Open Core admin at `admin.core.dealoware.com` (not `admin.platform`) | TD-ADM-001, TD-ADM-130, TD-ADM-131, TD-ADM-162 | Positive host serve; unauth deny (131); Host-deny conditional per OQ4 Soft HOLD invent app vs edge | **PASS** |
| AC2 | Participants CRUD + name search (not human users) | TD-ADM-006, TD-ADM-060, TD-ADM-080, TD-ADM-130, TD-ADM-140, TD-ADM-162 | CRUD+search (060); human-user list OUT (006); injection/denied-field negatives | **PASS** |
| AC3 | Artifacts CRUD + name search | TD-ADM-061, TD-ADM-080, TD-ADM-130, TD-ADM-140, TD-ADM-162 | CRUD+search (061); edit audit (080); empty/miss (140) | **PASS** |
| AC4 | Negotiations/offers all-status + sort/filter/paging + search | TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-065, TD-ADM-066, TD-ADM-080, TD-ADM-130, TD-ADM-140, TD-ADM-162 | All-status+Withdrawn; soft-deleted toggle; page 50/200; injection Soft HOLD | **PASS** |
| AC5 | Overall stats five counts | TD-ADM-070, TD-ADM-130, TD-ADM-162 | Five counts exclude soft-deleted; charts/warehouse absent | **PASS** |
| AC6 | Not Participant UI; FieldPolicy; one superadmin | TD-ADM-002, TD-ADM-003, TD-ADM-005, TD-ADM-007, TD-ADM-131 | CoreOwner-only; session deny; FieldPolicy dual wall; no multi-admin ACL | **PASS** |
| AC7 | Required auth/confirm/audit/delete/lists designs | TD-ADM-002,004,010–012,020–022,030–031,040–041,050–053,080,090–095,100–101,110,130,150,161–162 (+ related) | Bootstrap/TOTP/recovery/reset/Turnstile/lockout/session/confirm/cascades/audit/mail — with negatives | **PASS** |
| AC8 | Does not deliver inbound connector / platform admin / payments / tokens | TD-ADM-006, TD-ADM-007, TD-ADM-120, TD-ADM-160 | OUT pack review; inbound Soft HOLD after; platform hosts not positive targets | **PASS** |
| AC9 | Build/deploy Soft HOLD; invent AWS Soft HOLD; PoC $0 | TD-ADM-110, TD-ADM-120, TD-ADM-121, TD-ADM-150 | Gate Soft HOLDs; no AWS invent; secret scan; PoC $0 | **PASS** |

**Section A verdict: PASS** — 9/9 AC covered with executable cases and implied negatives/edges. No Spec invent to fill gaps.

---

## B. Dev Plan alignment

Dev Plan Steps 1–13 (`plans/…core-admin-dashboard.md` §4) mapped in design §3.5; spot-checked against case bodies.

| Step | Title (short) | Covering cases (representative) | Align? |
|------|---------------|---------------------------------|--------|
| 1 | Host / CoreOwner / FieldPolicy / TFM | TD-ADM-001,002,005,007 | Yes |
| 2 | Bootstrap + Turnstile on bootstrap | TD-ADM-004,010–012,040,150 | Yes |
| 3 | TOTP + hashed recovery; no email OTP | TD-ADM-004,012,020–022 | Yes |
| 4 | Password reset = link + 2FA | TD-ADM-030,031 | Yes |
| 5 | Turnstile + lockout/rate + session | TD-ADM-040–054 | Yes (incl. process-global Soft HOLD 054) |
| 6 | SES behind mail interface | TD-ADM-110,150 | Yes |
| 7 | Lists / search / paging / stats | TD-ADM-060–070,130,140 | Yes |
| 8 | Audit + HMAC IP | TD-ADM-053,064,080,100–102 | Yes |
| 9 | Edit + concurrency | TD-ADM-005,060,061,080,081,141 | Yes |
| 10 | Confirm-delete + soft-delete cascades | TD-ADM-090–095 | Yes (all four cascade directions) |
| 11 | Fail-closed non-CoreOwner | TD-ADM-002–005,131 | Yes |
| 12 | Explicit Soft HOLD / OUT / gates | TD-ADM-006,007,011,021,041,054,120,121,150,160 | Yes |
| 13 | Self-verify before handoff | TD-ADM-121,122,161 | Yes |

**Levels / tooling:** Design proposes Unit (xUnit), Integration (xUnit + WebApplicationFactory), API (Newman/Postman + WebApplicationFactory), E2E UI smoke (Playwright). Dev Plan assumes Negotiation Core modular monolith / TFM `net10.0` and does **not** lock specific `*.Tests` project folder names — design tooling is consistent (no invent conflict). No tests for out-of-plan deliverables (no MotorMarket, no platform admin build, no CDK/provision as positive work).

**Section B verdict: PASS**

---

## C. Checklist v2 / A6 alignment

**A6 row (quoted from `/workspace/pm/admin-dashboard-checklist.md`):**

> | A6 | Test design (cases before code) | Chief QA | **REOPENED** — Chief QA wrote the design + own PASS only. Soft HOLD invent A6 CLOSED until independent QAQA PASS vs Spec v2.2 AC1–AC9 + Dev Plan + checklist v2 + 29 security cases ↔ security points. Soft HOLD invent A6 docs PR until that PASS. Soft HOLD invent Stories/code Soft HOLD A7 until H4 live PASS | Design `…test-design__core-admin-dashboard.md` (sha256 `870f348c…`); Soft HOLD invent independent verification PASS path pending | |

| A6 requirement | Evidence in design | Verdict |
|----------------|--------------------|---------|
| Cases before code | 53 executable `TD-ADM-*` cases; Soft HOLD invent Stories/code until this PASS | **PASS** |
| Cover Spec v2.2 AC1–AC9 | §3.1 + case bodies (Section A) | **PASS** |
| Cover Dev Plan | §3.5 Steps 1–13 (Section B) | **PASS** |
| 29 security cases ↔ security points | Spec SEC 1–15 + Plan SEC 1–14 = 29; §3.4 + §3.6 + Section D table | **PASS** |
| Soft HOLD invent A6 docs PR until independent PASS | This verification file is that PASS artifact | **PASS** (closes that Soft HOLD) |
| Soft HOLD invent Stories/code Soft HOLD A7 until H4 | Design header + TD-ADM-121 | **PASS** (labeled; not lifted) |

**Section C verdict: PASS**

---

## D. Security cases / points 29/29

**Authoritative sources:**
- Spec pts 1–15: `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md`
- Plan pts 1–14: `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md`

**Count found:** **29** security points (15 + 14). Design claim 29/29 confirmed by reverse-trace below.

### Explicit 29/29 security point → case table

| # | Case ID (primary) | Case title (short) | Security point ID | Security point source path | Trace verdict |
|---|-------------------|--------------------|-------------------|----------------------------|---------------|
| 1 | TD-ADM-001 | Admin only at admin.core | SPEC-SEC-1 | `…spec-checklist.md` pt 1 | **OK** |
| 2 | TD-ADM-010 | Bootstrap single-use ≤24h | SPEC-SEC-2 | `…spec-checklist.md` pt 2 | **OK** |
| 3 | TD-ADM-020 | TOTP every login; no email OTP (021) | SPEC-SEC-3 | `…spec-checklist.md` pt 3 | **OK** |
| 4 | TD-ADM-030 | Reset = link + 2FA | SPEC-SEC-4 | `…spec-checklist.md` pt 4 | **OK** |
| 5 | TD-ADM-040 | Turnstile + lockout/session (050–052) | SPEC-SEC-5 | `…spec-checklist.md` pt 5 | **OK** |
| 6 | TD-ADM-100 | Auth+edit/delete audit; HMAC IP (053) | SPEC-SEC-6 | `…spec-checklist.md` pt 6 (+ Spec §13 MET HMAC) | **OK** |
| 7 | TD-ADM-003 | Fail-closed missing session | SPEC-SEC-7 | `…spec-checklist.md` pt 7 | **OK** |
| 8 | TD-ADM-005 | FieldPolicy only; no parallel ACL (007) | SPEC-SEC-8 | `…spec-checklist.md` pt 8 | **OK** |
| 9 | TD-ADM-090 | Confirm before delete | SPEC-SEC-9 | `…spec-checklist.md` pt 9 | **OK** |
| 10 | TD-ADM-091 | Soft-delete + cascades (092–095) + toggle (064) | SPEC-SEC-10 | `…spec-checklist.md` pt 10 | **OK** |
| 11 | TD-ADM-062 | Lists/search/paging safe (060–066,070) | SPEC-SEC-11 | `…spec-checklist.md` pt 11 | **OK** |
| 12 | TD-ADM-080 | Edit FieldPolicy-only + before/after | SPEC-SEC-12 | `…spec-checklist.md` pt 12 | **OK** |
| 13 | TD-ADM-110 | SES+Turnstile dependency/cost only | SPEC-SEC-13 | `…spec-checklist.md` pt 13 | **OK** |
| 14 | TD-ADM-120 | OUT / Soft HOLD pack | SPEC-SEC-14 | `…spec-checklist.md` pt 14 | **OK** |
| 15 | TD-ADM-122 | Traceability + handshake | SPEC-SEC-15 | `…spec-checklist.md` pt 15 | **OK** |
| 16 | TD-ADM-001 | Host + CoreOwner gate | PLAN-SEC-1 | `…devplan-checklist.md` pt 1 | **OK** |
| 17 | TD-ADM-011 | Bootstrap; no invent password | PLAN-SEC-2 | `…devplan-checklist.md` pt 2 | **OK** |
| 18 | TD-ADM-021 | TOTP; no email OTP; hashed recovery (022) | PLAN-SEC-3 | `…devplan-checklist.md` pt 3 | **OK** |
| 19 | TD-ADM-030 | Reset = link + 2FA | PLAN-SEC-4 | `…devplan-checklist.md` pt 4 | **OK** |
| 20 | TD-ADM-051 | Turnstile + lockout + session; no process-global (054) | PLAN-SEC-5 | `…devplan-checklist.md` pt 5 | **OK** |
| 21 | TD-ADM-053 | Raw IP counters; audit IP = keyed HMAC-SHA256 | PLAN-SEC-6 | `…devplan-checklist.md` pt 6 | **OK** |
| 22 | TD-ADM-110 | SES mail interface; Soft HOLD invent AWS | PLAN-SEC-7 | `…devplan-checklist.md` pt 7 | **OK** |
| 23 | TD-ADM-101 | Audit auth+edit/delete; toggle not audited | PLAN-SEC-8 | `…devplan-checklist.md` pt 8 | **OK** |
| 24 | TD-ADM-005 | Fail-closed FieldPolicy dual wall | PLAN-SEC-9 | `…devplan-checklist.md` pt 9 | **OK** |
| 25 | TD-ADM-090 | Confirm-delete + soft-delete cascades | PLAN-SEC-10 | `…devplan-checklist.md` pt 10 | **OK** |
| 26 | TD-ADM-065 | Lists/search/paging safe | PLAN-SEC-11 | `…devplan-checklist.md` pt 11 | **OK** |
| 27 | TD-ADM-081 | Edit + concurrency 409 | PLAN-SEC-12 | `…devplan-checklist.md` pt 12 | **OK** |
| 28 | TD-ADM-121 | OUT / Soft HOLD / A7 gate pack | PLAN-SEC-13 | `…devplan-checklist.md` pt 13 | **OK** |
| 29 | TD-ADM-122 | Traceability + handshake Soft HOLD SoR | PLAN-SEC-14 | `…devplan-checklist.md` pt 14 | **OK** |

**Uncovered in-scope security points:** **None** (0).  
**Weak traces:** None that fail. Spec-checklist pt 6 text emphasizes audit events; HMAC detail is in Spec §8.6/§10 and Spec §13 MET row for pt 6 — design mapping SPEC-SEC-6 → TD-ADM-053/100–102 is **OK** (not invent).

**Section D verdict: PASS — 29/29 OK**

---

## E. Design quality / no invent

| Check | Evidence | Verdict |
|-------|----------|---------|
| Executable cases | Each `TD-ADM-*` has Traces-to, Layer, Priority, Automation, Preconditions, Steps, Expected, Negative/abuse | **PASS** |
| Deterministic | Concrete numbers from Spec (5/15→30; 20/15→30; idle 30m / abs 8h; page 50/200; bootstrap ≤24h; reset 1h; confirm token 5 min) | **PASS** |
| PoC $0 | Header; TD-ADM-121/150; no paid/live LLM; fake mail adapter; Turnstile test keys via env name only | **PASS** |
| No Spec invent | Open questions OQ1–OQ6 Soft HOLD invent; Retry-After not mandated (correct vs Spec §8.6) | **PASS** |
| No MotorMarket / Arctic Circle | Constraints; TD-ADM-120/130; synthetic data only | **PASS** |
| Host naming (CEO standing rule) | Positive target **`admin.core.dealoware.com` only**; platform hosts (`admin.platform…`, `platform…`, `api.platform…`) explicitly **not** positive targets / OUT | **PASS** — no core↔platform confusion |
| AWS account information | Soft HOLD invent AWS account IDs/region/SES identity throughout; scan found **no** account IDs / ARNs / AKIA keys in the design | **PASS** — none present; none copied here |

**Section E verdict: PASS**

---

## F. Locks

| Lock | In design? | Note |
|------|------------|------|
| Soft HOLD multi-provider | **Not labeled** | Standing PoC org lock; not admin-dashboard-specific. Optional polish to add a Locks line. |
| Gate #26 / #27 CLOSED | **Not labeled** | Same — optional polish. Do not re-score other Stories. |
| PoC $0 | **Labeled** | Header, gates, TD-ADM-121, Confirm §9 | 

**Section F:** PoC $0 OK. Multi-provider / Gate #26/#27 absent from this file — **optional polish only** (does not fail A–E).

---

## FAIL defects

**None.**

---

## Optional polish (not FAIL)

1. Add an explicit Locks line to the test design header: `Soft HOLD multi-provider; Gate #26 CLOSED; Gate #27 CLOSED; PoC $0` (org continuity; A6 content does not depend on it).
2. TD-ADM-001 Host-deny negative is marked optional pending OQ4 — acceptable Soft HOLD invent; when SA/edge lock lands, promote Host-reject to required P0 assertion.
3. Consider naming intended test project path(s) once Stories unlock (Dev Plan does not lock folder names today).

---

## Independence + A6 closure

**Independent of Chief QA self-score; not a rubber-stamp.**

- Chief QA stamped Status **PASS** on the design itself; this file is the **independent** QAQA verification required by Ivan’s 1:32pm stage QA rule and the A6 checklist row.
- **A6 may close on this PASS.** Soft HOLD invent A6 docs PR lifts on this PASS. Soft HOLD invent Stories/code Soft HOLD A7 until H4 live PASS **remains**. Soft HOLD deploy until Ivan OK **remains**. Soft HOLD invent password/AWS **remains**.

**Locks line (this verification):** Soft HOLD multi-provider (org; not re-scored here) · Gate #26/#27 CLOSED (org; not re-scored here) · Soft HOLD Spec Steps 2–4 (unchanged; not re-scored) · Soft HOLD invent Stories/code Soft HOLD A7 until H4 live PASS · Soft HOLD deploy until Ivan OK · Soft HOLD invent password/AWS · **PoC $0**.

---

## Confirm

**QAQA verification PASS.** Path `verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md`. Test design sha256 `870f348c…bdd7bca7` **MATCH**. Spec **v2.2** confirmed. AC **9/9 PASS**. Dev Plan Steps **13/13 PASS**. Checklist A6 requirements **PASS**. Security points **29/29 OK** (Spec 15 + Plan 14). Host `admin.core.dealoware.com` only; no AWS account info; no MotorMarket. Independent of Chief QA self-score.
