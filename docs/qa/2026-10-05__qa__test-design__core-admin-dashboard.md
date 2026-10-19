# Test design — Core admin dashboard (A6)

| Field | Value |
|-------|--------|
| Written by | Dealoware Chief QA |
| Date | 2026-10-05 (~2:06–2:15pm ET); revised r2 ~8:15pm ET (UX1-A03 r2 + lockout note OQ1/OQ3); **revised r3 ~8:30pm ET** (UX1 A6 redlines A12, C08, X07, X08 + addendum Part A §A.5 UI cases; lockout note v5.3 final); **revised r4 ~8:40pm ET** (UX1-A16 two-step sign-in cases; final cites: lockout note v5.5, password-rules note v2.3, sign-in steps note v1); **revised r5 ~9:20pm ET** (TD-ADM-062 status fix; UX1 approval condition 3 fill of B/C/X cases; Spec UX1 redlines note v2 cases) |
| Revision | **r5** — prior tip sha256 `8df392554ed887322bd7dc3f6633933bca26209c3bcfbb29321bf055f89814c9` (r4; archived `/workspace/qa/archive/2026-10-05__qa__test-design__core-admin-dashboard__8df39255.md`; not yet Test-design-QA verified). Earlier: `31095cbb…` (r3), `110e075f…` (r2, Test design QA PASS), `870f348c…` (r1, A6 PASS) |
| Status | **Revised r5 — awaiting Test design QA re-verify (Senior Product QA).** No earlier PASS covers this tip. Gates and holds: §0 Active holds (single list) |
| Host | `admin.core.dealoware.com` **only** (Core admin). Platform hosts are not Core and are not positive test targets. |
| Principal | **CoreOwner** = single system superadmin `io@aiknowhow.com` |
| PoC | **$0** |
| DOC-FLOW | `qa/2026-10-05__qa__test-design__core-admin-dashboard.md` |
| Constraints | Test design only. Do not invent Stories, code, PRs, CDK, spend, provision or deploy. Do not invent passwords, AWS account IDs, region, SES identity, Turnstile site keys or HMAC key values. A7 held until H4 live PASS. Deploy held until Ivan OK. Never MotorMarket buyer/seller sessions or Arctic Circle inventory. |

---

## 0. Revision r5 (2026-10-05 ~9:20pm ET)

| Item | Change |
|------|--------|
| Why | (1) TD-ADM-062 wrongly listed Withdrawn as a negotiation status. (2) UX1 approval condition 3: fill the B, C and X UI cases (incl. UXR-B14 Expire and UXR-B15) now that addendum Parts B–D are locked. (3) New test surface in Spec UX1 redlines note v2 (Spec QA + Security QA PASS) |
| Status fix | **TD-ADM-062** now uses **Open, Closed, Expired** only (status-list note `0a5ff57b2c55059373494e8452ad2d0931f055aae830c1704336859bbcfaa910`); value/price removed from the negotiation sort/filter (UXR-B02). **TD-ADM-063** keeps **Withdrawn as an offer-only, read-only status** the admin can't set (Product decision `6ea05ddb799c44bddb52273214b9ccb3e4865e521b1a26d9edbb302ee3819d2e`). §6 Assumptions line fixed. Whole-design check: no other negotiation + Withdrawn wording (§7) |
| B/C/X fill | All 22 non-auth stubs are now full cases with preconditions, steps and expected results from addendum Parts B and D; delete cases cite Part C copy. New **TD-ADM-UI-na-expire** (UXR-B14 Expire dialog). UXR-B15 → TD-ADM-UI-na-B11. Every UXR-B01–B18, C01–C08, D01–D16 row maps to ≥1 case (§3.13). Case IDs keep the UX1 findings numbers from r3 (for example na-B14 = UX1-B14 = UXR-B09); each title names its UXR rows |
| Redlines v2 cases | New API / integration **TD-ADM-170…180** (§4.3b): TOTP parameters and replay guard, recovery codes, failure counting, no-store, S-A12 step-up, sign-out POST, Stay signed in, return path, server-side transitions, S-D3/S-D4, link hashing |
| Part A updates | New **TD-ADM-UI-auth-24** (UXR-A45 absolute-cap warning). Changed auth-04, -06, -07, -08, -10, -13, -14, -15, -17, -18, -19, -20, -21, -22, -23 (UXR-A08 focus order, A18, A42 step-up, A38 idle only, X01 routes, A05 counts) |
| Unchanged (locked) | UX1-A03 r2 rules (401 identical; 429 generic body; no `Retry-After`; never “locked”); Core API H4 (429 + Retry-After). Lockout v5.5, password-rules v2.3, sign-in steps v1, A16 cites unchanged |
| Closed | OQ2 (redlines A05: 6 digits, 30 s, HMAC-SHA1, 10 codes). OQ13 narrowed to the activity event label only |
| New OQs | OQ14 Offers stat statuses; OQ15 audit-log search; OQ16 S-A12 current-password field; OQ17 Turnstile on S-A10; OQ18 Withdrawn writer; OQ19 deleted-ID page with 'Show deleted' on |
| Case count | **127** (was 114, +13): P0 **88** / P1 **34** / P2 **5** |
| Routes | Only Spec X01 inventory routes (redlines note v2) are used |

### Active holds (single list for this design)

1. **Test design QA and UX1 condition 3** — Senior Product QA re-verifies this tip. Condition 3 (B/C/X fill incl. UXR-B14 Expire and B15) counts as met only after that PASS; until then the Step 7, 9, 10 and 15 UI PRs can't merge on this condition. Chief QA claims no PASS.
2. **UI PR gates** — UX1 approved 9:05pm ET (`ux/2026-10-05__ux__approval__ux1-admin-requirements.md`, sha256 `8accd894ac909f89f8a82df008eefc310ac3a73951cc8e0bb2a060e4b2e5411f`); UI lanes 8, 11, 14, 15 released and Stories 2, 3, 4, 5, 7, 9, 10, 14, 15 released for code. UI cases stay SKIP until their screens land; each UI PR merges through Dev Code QA, Security and the UI/UX gate (UXR-D16). Condition 1 (Dev Plan record fixes) is met; its docs PR still needs Docs QA.
3. **Condition 2 leftover** — the addendum follow-up is met (UI/UX QA PASS ~9:13pm ET on addendum `7aa374e9…` and findings `4b612834…`). Spec still has to confirm whether the S-A12 password change asks for the current password (UXR-A42, OQ16). This holds only the S-A12 password PR and the password-change parts of TD-ADM-UI-auth-10 / -13.
4. **Condition 4 and Withdrawn** — Withdrawn (Step 7 PR) and the edit paths plus Expire (Step 9 PR) each need their own Dev Code QA PASS. Nothing writes Withdrawn until a maker withdraw endpoint exists (Chief Product deciding, OQ18); meanwhile TD-ADM-063 seeds Withdrawn rows in the test store and TD-ADM-UI-na-B03 expects the filter-miss state.
5. **Audit viewer** — UXR-D12, D13, D16 follow the step-15 Story's own UI/UX gate (TD-ADM-UI-na-X02, -X09).
6. **Spec decisions still open** — Offers stat statuses (OQ14), audit-log search (OQ15), Turnstile on S-A10 (OQ17), deleted-ID page with 'Show deleted' on (OQ19). Cases assert the interim behaviour the addendum names.
7. **X08 CI wiring (DevOps)** — Brief 4 makes Playwright optional; until DevOps wires it, UI cases run locally and block merge by review (UXR-D15). This design still asks for headless CI (OQ10, TD-ADM-UI-ci-01).
8. **Deploy** — held until Ivan OK; admin.core deploy / attach also waits on A11 and the PR #11 conditions. No live admin.core calls from tests or stubs.
9. **No secrets** — no password values, keys, AWS account IDs or SES identities in the design, stubs or notes.

---

## 0a. Revision r4 (2026-10-05 ~8:40pm ET)

| Item | Change |
|------|--------|
| Why | UX1-A16 decided (two steps) by Chief Security and bound in the sign-in steps note; final Spec cites for the lockout and password-rules notes |
| Final cites | Lockout note **v5.5** `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` (replaces v5.3 `67686ce5…`); password-rules note **v2.3** `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` (replaces v2.1 `1ce5e7f3…` and v2.2 `7f73db9b…`); sign-in steps note **v1** `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4`; A16 decision `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34`. Spec QA + Security QA PASS on each tip (§1) |
| Added cases | **10**: API / integration `TD-ADM-023…029` (§4.3a); UI `TD-ADM-UI-auth-21…23` (§4.16) |
| Changed cases | TD-ADM-004, TD-ADM-011, TD-ADM-020, TD-ADM-050, TD-ADM-051 (r4 notes / cite v5.5; expectations unchanged); TD-ADM-UI-auth-01, -04, -07, -08, -10, -11, -13, -19 (sign-in steps note, password-rules v2.3, lockout v5.5) |
| Unchanged (locked) | UX1-A03 r2 rules: account failures **401** identical; IP throttle **429** generic body; **no `Retry-After`**; never “locked”; reset does not lift a lock. Core API H4 (429 + Retry-After) unchanged and separate |
| Closed | **OQ9** (step shape: two steps), **OQ11** (password-rules final at v2.3). OQ2 partly (6 digits locked) |
| New OQs | OQ12 (step-2 cap and account budget both 5 — cap only visible with a test-only threshold), OQ13 (addendum wording drift vs Spec: 'Start over' vs 'Back to sign in'; activity event names) |
| Case count | **114** (was 104): P0 **76** / P1 **33** / P2 **5** |
| Route names | The sign-in steps note names screen **S-A01** and no route strings; no route strings are invented here |

r4 holds are folded into the single **Active holds** list in §0 (r5). Closed at r5: UI build lanes and addendum Part A Draft (UX1 approved), Parts B–D not written (locked and filled here), delete UI copy (Part C locked), S-A12 and idle-warning adopt and route names (Spec UX1 redlines note v2 A20 / X01), SA TOTP period and code count (redlines A05), UI/UX QA cross-checks (done).

---

## 0b. Revision r3 (2026-10-05 ~8:30pm ET)

| Item | Change |
|------|--------|
| Why | UX1 redline routing r2 (`/workspace/dealoware-kb/ux/2026-10-05__ux__redlines__ux1-routing.md`, sha256 `fde55fba…0854b4d`): A6 owns **UX1-A12, UX1-C08, UX1-X07, UX1-X08** plus the minimum UI cases at the end of addendum Part A (§A.5). Steering from CPM + Chief Spec: lockout window note **v5.3 is final** — re-cite it here |
| Added cases | **51** UI cases: `TD-ADM-UI-auth-01…20` (A12 + Part A §A.5), `TD-ADM-UI-del-01…06` (C08), `TD-ADM-UI-na-B01…B15`, `-X01…X06`, `-X09`, `-axe`, `-kbd` (X07, stubs with Active holds), `TD-ADM-UI-ci-01` (X08) |
| Changed cases | TD-ADM-052, TD-ADM-090, TD-ADM-093, TD-ADM-094, TD-ADM-095, TD-ADM-102, TD-ADM-130, TD-ADM-131, TD-ADM-140 (UI-sibling cross-references only; API expectations unchanged) |
| Unchanged (locked) | UX1-A03 r2 auth API rules in §0 r2 table and TD-ADM-010/020/030/031/040/041/050/051/100: account failures **401** identical; IP throttle **429** generic body; **no `Retry-After`** on admin.core; never “locked”; reset does not lift a lock; TOTP after reset. Core API H4 (429 + Retry-After) unchanged and separate |
| Closed | **OQ7** (lockout note v5.3 final at r3: Spec QA PASS + Security QA point 5 PASS on `67686ce5…`; superseded by v5.5 in r4). **OQ8** partly: S-A12 activity panel → TD-ADM-UI-auth-19; break-glass stays review in TD-ADM-050 |
| New OQs | OQ9 (step shape UX1-A16), OQ10 (Playwright optional in Brief 4 vs X08 required), OQ11 (password-rules note v2.1 not final) — OQ9 and OQ11 closed in r4 |
| Case count | **104** (was 53): P0 **66** / P1 **33** / P2 **5** |
| UXR coverage | UXR-A01…A44 → **44/44** mapped to ≥1 TD-ADM-UI-auth case (§3.9). UI detail is cited by UXR ID, not copied |
| Wording | Plain English and **Active holds** only |

r3 holds are folded into the single **Active holds** list in §0 (r4). At r3, lockout note v5.3 (`67686ce5…`) was final; r4 re-cites v5.5.

---

## 0c. Revision r2 (2026-10-05 ~8:15pm ET)

| Item | Change |
|------|--------|
| Why | Chief Security **UX1-A03 r2** decision (admin.core lockout message) + Chief Spec / CPM correction + lockout window note closing **OQ1 / OQ3** |
| Changed cases | TD-ADM-010, TD-ADM-020, TD-ADM-030, TD-ADM-031, TD-ADM-040, TD-ADM-041, TD-ADM-050, TD-ADM-051, TD-ADM-100 |
| Closed OQs | OQ1, OQ3 (lockout note); OQ5 (UX1-A03 r2: no Retry-After on admin.core). New at r2: OQ7 (aligned, hash re-cite pending — **closed in r3**, v5.3 final), OQ8 (v5.3 new scope — partly closed in r3) |
| Case count | **53** at r2 (no split needed; audit-only throttle proof folded into TD-ADM-051). r3: 104, see §0 |
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
| Lockout window note **v5.5** (OQ1 / OQ3 / OQ7 / S-A12 panel / `signin.second_factor_failed`) | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` | **FINAL** sha256 `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` — Spec QA **PASS** on this tip (`verification/2026-10-05__spec__verification__core-admin-lockout-window-note.md`, “v5.5 PASS” entry ~8:30pm ET) and Security QA point 5 **PASS** on this tip (`verification/2026-10-05__security__verification__core-admin-lockout-window-note-pt5-confirm.md`). Replaces v5.3 `67686ce5…` (rules unchanged per Spec QA diff). Binding: IP throttles MUST 429 + generic body (both sign-in steps), no Retry-After; reset does not lift a lock or clear counts; break-glass ops-only; step-2 cap on top of the shared account counter; S-A12 activity closed event list incl. 'code failed after correct password'; lock-notice email SHOULD |
| Password-rules note **v2.3** (UX1-A17) | `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` | **FINAL** sha256 `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` — Spec QA **PASS** (`verification/2026-10-05__spec__verification__core-admin-password-rules-note.md`, “v2.3 PASS” entry ~8:31pm ET) and Security QA **PASS** (`verification/2026-10-05__security__verification__core-admin-password-rules-note-confirm.md`). Replaces v2.2 `7f73db9b…` and v2.1 `1ce5e7f3…`. Context-word rule v1.2 (NFKC → Unicode default full case folding; equals / contains ≥4 / equals after stripping N, P, S, Z) |
| Sign-in steps note **v1** (UX1-A16) | `specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` | sha256 `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` — Spec QA **PASS** on this tip only (`verification/2026-10-05__spec__verification__core-admin-signin-steps-note.md`) and Security QA **PASS** (`verification/2026-10-05__security__verification__core-admin-signin-steps-note-confirm.md`). Conditions 1–8 + C1–C4 → §3.11. Header on disk still reads Draft; verifications match the tip |
| UX1-A16 Chief Security decision | `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` | sha256 `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34` — **binding** (two steps; conditions 1–8) |
| Chief Security auth-UI answers | `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` | `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` on disk at r4 — matches the cite in lockout v5.5 and password-rules v2.3 |
| UX1 redline routing r2 | `ux/2026-10-05__ux__redlines__ux1-routing.md` | `fde55fbae19ee3a0f97d27734f902ba3694cb2d6e4f7cd42a2742e0ec0854b4d` — A6 rows A12, C08, X07, X08 + Part A §A.5 |
| UX1 findings | `ux/2026-10-05__ux__findings__ux1-admin-requirements.md` | sha256 `4b612834bb5b0d79f37fb0e3ba7419734707b62aafe359b29d11ccf6486fd791` on disk at r5 (UI/UX QA condition 2 re-check PASS on this tip; the approval cites the earlier `8efb1d30`) |
| UX1 UI requirements addendum | `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` | sha256 `7aa374e9de0ae6c0d2999122f47e7b28e04a66a2cce4084f0e7314bffad5419e` on disk at r5 — Parts A–D. The approval cites `599d6a18`; this tip is the follow-up the approval asked for and has UI/UX QA PASS (condition 2 re-check ~9:13pm ET, `verification/2026-10-05__ux-qa__verification__ux1-admin-requirements.md`). Its header still reads Draft. Cited by UXR ID; B/C/D rows → §3.13 |
| UX1 approval | `ux/2026-10-05__ux__approval__ux1-admin-requirements.md` | sha256 `8accd894ac909f89f8a82df008eefc310ac3a73951cc8e0bb2a060e4b2e5411f` on disk at r5 — approved 9:05pm ET with conditions 1–4; condition 3 = this fill |
| Spec status-list note | `specs/2026-10-05__spec__spec__core-admin-status-list-note.md` | sha256 `0a5ff57b2c55059373494e8452ad2d0931f055aae830c1704336859bbcfaa910` — Spec QA **PASS** (`verification/2026-10-05__spec__verification__core-admin-status-list-note.md`). Negotiations Open, Closed, Expired; offers Open, Accepted, Declined, Withdrawn, Superseded (countered), Cancelled; Deleted is a badge, never a status → §3.15 |
| Product decision: Withdrawn and edit paths | `product/2026-10-05__product__decision__ux1-withdrawn-and-edit-paths.md` | sha256 `6ea05ddb799c44bddb52273214b9ccb3e4865e521b1a26d9edbb302ee3819d2e` — Product QA **PASS** (`verification/2026-10-05__product__verification__ux1-withdrawn-and-edit-paths.md`). Withdrawn offer-only, maker-only while Open, never admin-set; admin offer change Open→Cancelled only; SA §4 edit fields and admin Expire in scope |
| Spec UX1 redlines note v2 | `specs/2026-10-05__spec__spec__core-admin-ux1-redlines-note.md` | sha256 `eb63276fc51217c3c82772c0b8de26ead1337f6e382938a1d736a05d13f6f2d0` — Security QA **PASS** 10/10 (`verification/2026-10-05__security__verification__core-admin-ux1-redlines-spec-note-confirm.md`) and Spec QA **PASS** (`verification/2026-10-05__spec__verification__core-admin-ux1-redlines-note.md`, v1 `73b86ba6…` void). A05, A15, A20, return path, B10, B01, X01 → §3.14 |
| SA UX1 redlines note (cite only) | `architecture/2026-10-05__sa__architecture__core-admin-ux1-redlines-note.md` | sha256 `0286c68eb3ab372db91a6afe7e2fb359c063447d5ba3e5d6786cfab82ee5f72b` as cited by the Spec note (SA §4 edit fields) |
| Architecture Turnstile vs lockout | `architecture/2026-10-05__sa__architecture__core-admin-turnstile-lockout-note.md` | Binding for OQ1 (Turnstile reject counts toward nothing); Arch QA PASS `verification/2026-10-05__sa__verification__core-admin-turnstile-lockout-note.md` |
| CFO estimate | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` | Turnstile $0; SES under $0.01/mo assumed — estimate only |

### Gates (binding)

| Gate | Rule |
|------|------|
| Stories / build | held until **this Test design PASS** |
| Admin UI build lanes | **released** — UX1 approved 9:05pm ET with conditions 1–4 (§0 Active holds 1–4); UI cases run as each screen lands |
| A7 admin build | held until **H4 live PASS** (separate harden track; do not invent H4 steps here) |
| Deploy | held until **Ivan OK** |
| Secrets / AWS | Do not invent password values and AWS account details |
| PoC | **$0** — not a spend/provision unlock |

### Scope

- Test design for Core admin dashboard at **`admin.core.dealoware.com`** / Core admin API only.
- Cover Product AC1–AC9, Spec Locked #1–#7 + §§3–12 requirement surface, Spec Security pts 1–15, Dev Plan Steps 1–13, Dev Plan Security pts 1–14.
- r3: cover UX1 rows owned by A6 (A12, C08, X07, X08) and addendum Part A UXR-A01…A44 with UI cases; B/X rows as stubs until locked.
- r5: full UI cases for every UXR-B, C and D row (UX1 approval condition 3), UXR-A45, and the Spec UX1 redlines note v2 surface; negotiation statuses per the status-list note.
- Automated API / integration first; UI layer per UX1 (r3): Playwright headless UI cases + axe-core, run in CI on admin UI PRs (TD-ADM-UI-ci-01).

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
| E2E UI (r3) | Auth screens (UXR-A), delete modal (UX1-C), non-auth screens (UX1-B/X, stubs) | Playwright headless; keyboard-only runs; clock control |
| Accessibility (r3) | axe-core WCAG 2.2 A/AA tags per screen + manual WCAG 2.2 checks | axe-core via Playwright; manual checklist |
| Security / negative | Injection, IDOR/deny-by-default, secret-dump probes, CAPTCHA fail | API + review |
| Process-review | Active holds / OUT / gate pack / no-secret scan | Manual checklist |

### Environments

| Env | When | Notes |
|-----|------|-------|
| Local | Story / PR development after Active holds lift | Fake mail adapter; Turnstile test keys via env name only |
| CI | Every admin PR | Automated P0 API/integration. **r3 (UX1-X08): headless UI cases run on every PR touching admin UI files; a failing UI case fails CI** (TD-ADM-UI-ci-01). DevOps owns wiring; QA owns catalog |
| Live `admin.core.dealoware.com` | **Only after** Ivan deploy OK | Smoke subset; do not invent AWS details in evidence |

### Test data rules

- **Synthetic only** — no real customer data; no MotorMarket buyer/seller sessions; no Arctic Circle inventory.
- CoreOwner login email under test is the Spec lock `io@aiknowhow.com`; **password / TOTP seed / recovery codes** live only in env or secret store, referenced **by name** (e.g. `COREOWNER_PASSWORD_ENV`, `TOTP_SEED_ENV`). **Never** write values into this design, fixtures committed to git, chat, or reports.
- Mail: fake/in-memory mail interface adapter in tests; do not invent SES account/region/identity.
- CAPTCHA: Turnstile **test** keys via env; do not invent production site keys in docs.

### Prefer

1. Automated API / integration covering auth, RBAC, lists, edit, delete, audit.
2. UI cases (r3): Part A auth UI, delete modal UI, non-auth UI stubs, axe-core, keyboard-only — headless in CI.
3. Manual process-review for Active holds / OUT / no-secret scans.

---

## 3. Traceability matrix

Coverage rule: every source ID → ≥1 `TD-ADM-###`. Self-verify §8 confirms **0 gaps**.

### 3.1 Product AC1–AC9

| ID | Summary | Test cases |
|----|---------|------------|
| AC1 | Open admin at admin.core only | TD-ADM-001, TD-ADM-130, TD-ADM-131, TD-ADM-162, TD-ADM-029 |
| AC2 | Participants CRUD + name search | TD-ADM-006, TD-ADM-060, TD-ADM-080, TD-ADM-130, TD-ADM-140, TD-ADM-162, TD-ADM-178 |
| AC3 | Artifacts CRUD + name search | TD-ADM-061, TD-ADM-080, TD-ADM-130, TD-ADM-140, TD-ADM-162, TD-ADM-178 |
| AC4 | Negotiations/offers all-status + sort/filter/paging + search | TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-065, TD-ADM-066, TD-ADM-080, TD-ADM-130, TD-ADM-140, TD-ADM-162, TD-ADM-178 |
| AC5 | Overall stats five counts | TD-ADM-070, TD-ADM-130, TD-ADM-162 |
| AC6 | Not Participant UI; FieldPolicy; one superadmin | TD-ADM-002, TD-ADM-003, TD-ADM-005, TD-ADM-007, TD-ADM-131 |
| AC7 | Required auth/confirm/audit/delete/lists designs | TD-ADM-002, TD-ADM-004, TD-ADM-010, TD-ADM-011, TD-ADM-012, TD-ADM-020, TD-ADM-021, TD-ADM-022, TD-ADM-030, TD-ADM-031, TD-ADM-040, TD-ADM-041, TD-ADM-050, TD-ADM-051, TD-ADM-052, TD-ADM-053, TD-ADM-080, TD-ADM-090, TD-ADM-091, TD-ADM-092, TD-ADM-093, TD-ADM-094, TD-ADM-095, TD-ADM-100, TD-ADM-101, TD-ADM-110, TD-ADM-130, TD-ADM-150, TD-ADM-161, TD-ADM-162, TD-ADM-023, TD-ADM-024, TD-ADM-025, TD-ADM-026, TD-ADM-027, TD-ADM-028, TD-ADM-170, TD-ADM-171, TD-ADM-172, TD-ADM-173, TD-ADM-174, TD-ADM-175, TD-ADM-176, TD-ADM-177, TD-ADM-178, TD-ADM-180 |
| AC8 | Does not deliver inbound connector / platform admin / payments / tokens | TD-ADM-006, TD-ADM-007, TD-ADM-120, TD-ADM-160 |
| AC9 | Build/deploy held; AWS details not invented; PoC $0 | TD-ADM-110, TD-ADM-120, TD-ADM-121, TD-ADM-150 |

### 3.2 Spec Locked decisions #1–#7

| ID | Summary | Test cases |
|----|---------|------------|
| LOCK-1 | Host admin.core only | TD-ADM-001, TD-ADM-029 |
| LOCK-2 | One CoreOwner; FieldPolicy; no multi-admin ACL | TD-ADM-002, TD-ADM-005, TD-ADM-007 |
| LOCK-3 | Four entities list/view/edit/delete + lists/search | TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-162 |
| LOCK-4 | Overall stats | TD-ADM-070, TD-ADM-162 |
| LOCK-5 | Must-cover auth/confirm/audit/delete/lists | TD-ADM-090, TD-ADM-161, TD-ADM-162 |
| LOCK-6 | Inbound bot connector after this | TD-ADM-160 |
| LOCK-7 | Build/deploy/Stories held; no password invent | TD-ADM-121 |

### 3.3 Spec requirement sections

| ID | Spec section | Test cases |
|----|--------------|------------|
| SPEC-S3 | §3 Host and role | TD-ADM-001, TD-ADM-002, TD-ADM-007, TD-ADM-130, TD-ADM-029, TD-ADM-179 |
| SPEC-S4.1 | §4.1 Participants | TD-ADM-006, TD-ADM-060, TD-ADM-178 |
| SPEC-S4.2 | §4.2 Artifacts | TD-ADM-061, TD-ADM-178 |
| SPEC-S4.3 | §4.3 Negotiations | TD-ADM-062, TD-ADM-178 |
| SPEC-S4.4 | §4.4 Offers | TD-ADM-063, TD-ADM-178 |
| SPEC-S4.5 | §4.5 Lists/sort/filter/paging | TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-065, TD-ADM-066, TD-ADM-130, TD-ADM-140 |
| SPEC-S4.6 | §4.6 Name search | TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-066, TD-ADM-140 |
| SPEC-S5 | §5 Edit rules | TD-ADM-005, TD-ADM-060, TD-ADM-080, TD-ADM-081, TD-ADM-141, TD-ADM-178 |
| SPEC-S6 | §6 Overall stats | TD-ADM-070, TD-ADM-130 |
| SPEC-S8.1 | §8.1 Identity | TD-ADM-002, TD-ADM-003, TD-ADM-011, TD-ADM-131, TD-ADM-023, TD-ADM-029 |
| SPEC-S8.2 | §8.2 Bootstrap | TD-ADM-004, TD-ADM-010, TD-ADM-011, TD-ADM-012, TD-ADM-180 |
| SPEC-S8.3 | §8.3 TOTP 2FA | TD-ADM-004, TD-ADM-012, TD-ADM-020, TD-ADM-021, TD-ADM-022, TD-ADM-023, TD-ADM-024, TD-ADM-025, TD-ADM-027, TD-ADM-170, TD-ADM-171, TD-ADM-172, TD-ADM-173, TD-ADM-174 |
| SPEC-S8.4 | §8.4 Password reset | TD-ADM-030, TD-ADM-031, TD-ADM-180 |
| SPEC-S8.5 | §8.5 CAPTCHA Turnstile | TD-ADM-040, TD-ADM-041 |
| SPEC-S8.6 | §8.6 Lockout/rate + raw IP (as amended by lockout note + UX1-A03 r2) | TD-ADM-040, TD-ADM-050, TD-ADM-051, TD-ADM-053, TD-ADM-024, TD-ADM-025, TD-ADM-026, TD-ADM-172 |
| SPEC-S8.7 | §8.7 Session lifetime | TD-ADM-003, TD-ADM-052, TD-ADM-027, TD-ADM-173, TD-ADM-174, TD-ADM-175, TD-ADM-176, TD-ADM-177 |
| SPEC-S8.8 | §8.8 Auth audit events (+ `captcha-failed`, lockout note) | TD-ADM-040, TD-ADM-050, TD-ADM-051, TD-ADM-100, TD-ADM-028, TD-ADM-175 |
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
| 1 | Host + single superadmin | TD-ADM-001, TD-ADM-002, TD-ADM-006, TD-ADM-007, TD-ADM-029 |
| 2 | Email+password; bootstrap | TD-ADM-010, TD-ADM-011, TD-ADM-012, TD-ADM-150, TD-ADM-180 |
| 3 | TOTP; no email OTP; hashed recovery | TD-ADM-004, TD-ADM-012, TD-ADM-020, TD-ADM-021, TD-ADM-022, TD-ADM-023, TD-ADM-027, TD-ADM-170, TD-ADM-171, TD-ADM-173, TD-ADM-174 |
| 4 | Reset = link + 2FA | TD-ADM-030, TD-ADM-031, TD-ADM-180 |
| 5 | Turnstile + lockout/rate + session | TD-ADM-040, TD-ADM-041, TD-ADM-050, TD-ADM-051, TD-ADM-052, TD-ADM-054, TD-ADM-024, TD-ADM-025, TD-ADM-026, TD-ADM-172, TD-ADM-176 |
| 6 | Login/reset + edit/delete audit; HMAC IP | TD-ADM-053, TD-ADM-064, TD-ADM-100, TD-ADM-101, TD-ADM-102, TD-ADM-028, TD-ADM-175 |
| 7 | Fail-closed non-superadmin | TD-ADM-002, TD-ADM-003, TD-ADM-004, TD-ADM-005, TD-ADM-131, TD-ADM-141, TD-ADM-174, TD-ADM-175, TD-ADM-177, TD-ADM-178, TD-ADM-179 |
| 8 | FieldPolicy only; no parallel ACL | TD-ADM-005, TD-ADM-007 |
| 9 | Confirm before delete | TD-ADM-090 |
| 10 | Soft-delete + cascades + toggle | TD-ADM-064, TD-ADM-091, TD-ADM-092, TD-ADM-093, TD-ADM-094, TD-ADM-095 |
| 11 | Lists/search/paging safe | TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-065, TD-ADM-066, TD-ADM-070 |
| 12 | Edit write FieldPolicy-only | TD-ADM-005, TD-ADM-060, TD-ADM-080, TD-ADM-081, TD-ADM-141, TD-ADM-178 |
| 13 | SES + Turnstile dependency+cost only | TD-ADM-040, TD-ADM-054, TD-ADM-110, TD-ADM-150 |
| 14 | OUT / Active holds pack | TD-ADM-006, TD-ADM-007, TD-ADM-011, TD-ADM-021, TD-ADM-041, TD-ADM-120, TD-ADM-121, TD-ADM-150 |
| 15 | Traceability + re-QA handshake | TD-ADM-121, TD-ADM-122, TD-ADM-161 |

### 3.5 Dev Plan Steps 1–13

| Step | Title (short) | Test cases |
|------|---------------|------------|
| 1 | Host / CoreOwner / FieldPolicy / TFM | TD-ADM-001, TD-ADM-002, TD-ADM-005, TD-ADM-007, TD-ADM-029 |
| 2 | Bootstrap + Turnstile on bootstrap | TD-ADM-004, TD-ADM-010, TD-ADM-011, TD-ADM-012, TD-ADM-040, TD-ADM-150, TD-ADM-180 |
| 3 | TOTP + hashed recovery; no email OTP | TD-ADM-004, TD-ADM-012, TD-ADM-020, TD-ADM-021, TD-ADM-022, TD-ADM-023, TD-ADM-027, TD-ADM-170, TD-ADM-171, TD-ADM-173, TD-ADM-174 |
| 4 | Password reset = link + 2FA | TD-ADM-030, TD-ADM-031, TD-ADM-180 |
| 5 | Turnstile + lockout/rate + session | TD-ADM-040, TD-ADM-041, TD-ADM-050, TD-ADM-051, TD-ADM-052, TD-ADM-053, TD-ADM-054, TD-ADM-024, TD-ADM-025, TD-ADM-026, TD-ADM-172, TD-ADM-175, TD-ADM-176, TD-ADM-177 |
| 6 | SES behind mail interface | TD-ADM-110, TD-ADM-150 |
| 7 | Lists / search / paging / stats | TD-ADM-006, TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-065, TD-ADM-066, TD-ADM-070, TD-ADM-130, TD-ADM-140 |
| 8 | Audit + HMAC IP | TD-ADM-053, TD-ADM-064, TD-ADM-080, TD-ADM-100, TD-ADM-101, TD-ADM-102, TD-ADM-028, TD-ADM-175 |
| 9 | Edit + concurrency | TD-ADM-005, TD-ADM-060, TD-ADM-061, TD-ADM-080, TD-ADM-081, TD-ADM-130, TD-ADM-141, TD-ADM-178 |
| 10 | Confirm-delete + soft-delete cascades | TD-ADM-060, TD-ADM-061, TD-ADM-090, TD-ADM-091, TD-ADM-092, TD-ADM-093, TD-ADM-094, TD-ADM-095, TD-ADM-130 |
| 11 | Fail-closed non-CoreOwner | TD-ADM-002, TD-ADM-003, TD-ADM-004, TD-ADM-005, TD-ADM-131, TD-ADM-177, TD-ADM-179 |
| 12 | Explicit Active holds / OUT / gates | TD-ADM-006, TD-ADM-007, TD-ADM-011, TD-ADM-021, TD-ADM-041, TD-ADM-054, TD-ADM-120, TD-ADM-121, TD-ADM-150, TD-ADM-160 |
| 13 | Self-verify before handoff | TD-ADM-121, TD-ADM-122, TD-ADM-161 |

### 3.6 Dev Plan Security points 1–14

| # | Point (short) | Test cases |
|---|---------------|------------|
| 1 | Host + CoreOwner gate | TD-ADM-001, TD-ADM-002, TD-ADM-006, TD-ADM-007, TD-ADM-029 |
| 2 | Bootstrap; no invent password | TD-ADM-010, TD-ADM-011, TD-ADM-012, TD-ADM-150, TD-ADM-180 |
| 3 | TOTP; no email OTP; hashed recovery | TD-ADM-004, TD-ADM-012, TD-ADM-020, TD-ADM-021, TD-ADM-022, TD-ADM-023, TD-ADM-027, TD-ADM-170, TD-ADM-171, TD-ADM-174 |
| 4 | Reset = link + 2FA | TD-ADM-030, TD-ADM-031, TD-ADM-180 |
| 5 | Turnstile + lockout + session; no process-global limiter | TD-ADM-040, TD-ADM-041, TD-ADM-050, TD-ADM-051, TD-ADM-052, TD-ADM-054, TD-ADM-024, TD-ADM-025, TD-ADM-026, TD-ADM-172, TD-ADM-176 |
| 6 | Raw IP counters; audit HMAC IP | TD-ADM-053 |
| 7 | SES mail interface; no invented AWS details | TD-ADM-110, TD-ADM-150 |
| 8 | Audit auth+edit/delete; toggle not audited | TD-ADM-053, TD-ADM-064, TD-ADM-080, TD-ADM-100, TD-ADM-101, TD-ADM-102, TD-ADM-028, TD-ADM-175 |
| 9 | Fail-closed FieldPolicy dual wall | TD-ADM-002, TD-ADM-003, TD-ADM-004, TD-ADM-005, TD-ADM-007, TD-ADM-141, TD-ADM-177, TD-ADM-178, TD-ADM-179 |
| 10 | Confirm-delete + soft-delete cascades | TD-ADM-064, TD-ADM-090, TD-ADM-091, TD-ADM-092, TD-ADM-093, TD-ADM-094, TD-ADM-095 |
| 11 | Lists/search/paging safe | TD-ADM-060, TD-ADM-061, TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-065, TD-ADM-066, TD-ADM-070 |
| 12 | Edit + concurrency | TD-ADM-005, TD-ADM-060, TD-ADM-080, TD-ADM-081, TD-ADM-141, TD-ADM-178 |
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
| UX1 rows owned by A6 (A12, C08, X07, X08) | 4 | 4 | **0** |
| Addendum Part A UXR-A01…A45 (r5 adds A45) | 45 | 45 | **0** |
| Part A §A.5 minimum set items (r5: 18, was 14) | 18 | 18 | **0** |
| UX1-C01…C07 (via C08) | 7 | 7 | **0** |
| UX1-B01…B15, X01…X06, X09 (via X07) | 22 | 22 (full cases, r5) | **0** |
| Addendum UXR-B01…B18, UXR-C01…C08, UXR-D01…D16 (r5, §3.13) | 42 | 42 | **0** |
| Sign-in steps note v1 conditions 1–8 + C1–C4 (r4) | 12 | 12 | **0** |
| Password-rules note v2.3 rules 1–9 + UI table (r4) | 10 | 10 | **0** |
| Spec UX1 redlines note v2 items (r5, §3.14) | 13 | 13 | **0** |
| Status-list note + Product decision items (r5, §3.15) | 4 | 4 | **0** |
| Test cases total | **127** (53 r2 + 51 r3 UI + 10 r4 + 13 r5) | — | — |
| P0 / P1 / P2 | **88 / 34 / 5** | — | — |
| By group | API/integration 71 (incl. sign-in steps 7, redlines v2 11) · UI auth 24 · delete 6 · non-auth 25 (22 rows + Expire + axe + kbd) · CI 1 | — | — |

**Zero gaps** (every source, UX1 row and UXR row maps to ≥1 case). UI cases stay SKIP until their screens land (Active holds 2); their content is complete. Residual ambiguities are listed in §6 as open questions (do not invent behaviour).

### 3.8 UX1 rows owned by A6 (routing r2)

| UX1 row | Ask | Test cases |
|---------|-----|------------|
| UX1-A12 | Auth UI coverage for UX1-A01–A10 criteria; TD-ADM-UI-auth-* IDs | TD-ADM-UI-auth-01…20 (and §3.9 UXR map) |
| UX1-C08 | UI cases for C01–C07 | TD-ADM-UI-del-01…06 (§3.10) |
| UX1-X07 | UI case per B, C, X row; axe-core per screen; keyboard list + edit + delete | B01–B15, X01–X06, X09 → TD-ADM-UI-na-<row> (full cases r5) + TD-ADM-UI-na-expire; C01–C07 → TD-ADM-UI-del-*; X08 → TD-ADM-UI-ci-01; TD-ADM-UI-na-axe; TD-ADM-UI-na-kbd; UXR rows → §3.13 |
| UX1-X08 | Headless UI cases in CI on every admin UI PR; broken UI case fails CI | TD-ADM-UI-ci-01 |
| Part A §A.5 minimum set | 18 items (addendum `7aa374e9…`) | parity on both steps → auth-01, -22; step-2 cap and 5-min expiry back to S-A1 → auth-23; locked account with correct password stays on S-A1 → auth-22; Turnstile four states → auth-02; attributes + paste → auth-03; keyboard-only + 2FA → auth-04, -21; bootstrap → enrolment → codes → auth-08; expired/used links → auth-09; reset with TOTP → auth-11; reset with recovery code → auth-12; idle warning + return path → auth-14, -15; banner count → auth-16; sign-out + Back → auth-17; helper text / length / server reason → auth-10; other sessions end → auth-13; activity panel + 24h notice → auth-19; axe-core every S-A screen → auth-20; UXR-A45 absolute cap → auth-24 |

UX1 auth criteria A01–A10 reach TD-ADM-UI-auth as follows: A01 → auth-04, -06; A02 → auth-01, -05; A03 → auth-01, -19; A04 → auth-02; A05 → auth-08; A06 → auth-07, -08, -12; A07 → auth-09, -11, -12; A08 → auth-14; A09 → auth-04, -05; A10 → auth-03, -06, -20. Related rows: A13 → auth-02; A15 → auth-17; A16 → auth-04, -07; A17 → auth-10, -13; A18 → auth-11; A19 → auth-06, -08, -09; A20 → auth-14, -15; A21 → auth-16, -18.

### 3.9 Addendum Part A requirements (UXR-A01…A45)

Rule (addendum §A.5): every UXR-A row → ≥1 TD-ADM UI case.

| UXR | Test cases |
|-----|------------|
| UXR-A01 | TD-ADM-UI-auth-01, TD-ADM-UI-auth-22 |
| UXR-A02 | TD-ADM-UI-auth-01 |
| UXR-A03 | TD-ADM-UI-auth-02, TD-ADM-UI-auth-23 |
| UXR-A04 | TD-ADM-UI-auth-03 |
| UXR-A05 | TD-ADM-UI-auth-03, TD-ADM-UI-auth-20 |
| UXR-A06 | TD-ADM-UI-auth-01, TD-ADM-UI-auth-05 |
| UXR-A07 | TD-ADM-UI-auth-04, TD-ADM-UI-auth-20 |
| UXR-A08 | TD-ADM-UI-auth-04, TD-ADM-UI-auth-21 |
| UXR-A09 | TD-ADM-UI-auth-06, TD-ADM-UI-auth-20 |
| UXR-A10 | TD-ADM-UI-auth-06, TD-ADM-UI-auth-21 |
| UXR-A11 | TD-ADM-UI-auth-15 |
| UXR-A12 | TD-ADM-UI-auth-06 |
| UXR-A13 | TD-ADM-UI-auth-04 |
| UXR-A14 | TD-ADM-UI-auth-04, TD-ADM-UI-auth-05 |
| UXR-A15 | TD-ADM-UI-auth-07, TD-ADM-UI-auth-21 |
| UXR-A16 | TD-ADM-UI-auth-04, TD-ADM-UI-auth-07, TD-ADM-UI-auth-21 |
| UXR-A17 | TD-ADM-UI-auth-04 |
| UXR-A18 | TD-ADM-UI-auth-01, TD-ADM-UI-auth-07, TD-ADM-UI-auth-22, TD-ADM-UI-auth-23 |
| UXR-A19 | TD-ADM-UI-auth-07 |
| UXR-A20 | TD-ADM-UI-auth-07, TD-ADM-UI-auth-16 |
| UXR-A21 | TD-ADM-UI-auth-08, TD-ADM-UI-auth-10 |
| UXR-A22 | TD-ADM-UI-auth-10 |
| UXR-A23 | TD-ADM-UI-auth-08, TD-ADM-UI-auth-10 |
| UXR-A24 | TD-ADM-UI-auth-08 |
| UXR-A25 | TD-ADM-UI-auth-09 |
| UXR-A26 | TD-ADM-UI-auth-09 |
| UXR-A27 | TD-ADM-UI-auth-08 |
| UXR-A28 | TD-ADM-UI-auth-08 |
| UXR-A29 | TD-ADM-UI-auth-08 |
| UXR-A30 | TD-ADM-UI-auth-08 |
| UXR-A31 | TD-ADM-UI-auth-08 |
| UXR-A32 | TD-ADM-UI-auth-08 |
| UXR-A33 | TD-ADM-UI-auth-11 |
| UXR-A34 | TD-ADM-UI-auth-11 |
| UXR-A35 | TD-ADM-UI-auth-11 |
| UXR-A36 | TD-ADM-UI-auth-11, TD-ADM-UI-auth-12 |
| UXR-A37 | TD-ADM-UI-auth-11, TD-ADM-UI-auth-12, TD-ADM-UI-auth-13 |
| UXR-A38 | TD-ADM-UI-auth-14, TD-ADM-UI-auth-15 |
| UXR-A39 | TD-ADM-UI-auth-13, TD-ADM-UI-auth-14, TD-ADM-UI-auth-17, TD-ADM-UI-auth-24 |
| UXR-A40 | TD-ADM-UI-auth-16 |
| UXR-A41 | TD-ADM-UI-auth-17 |
| UXR-A42 | TD-ADM-UI-auth-13, TD-ADM-UI-auth-18 |
| UXR-A43 | TD-ADM-UI-auth-19 |
| UXR-A44 | TD-ADM-UI-auth-19 |
| UXR-A45 | TD-ADM-UI-auth-24 |

### 3.10 Delete UI (UX1-C01…C07, UXR-C01…C07) → UI case → API sibling

| UX1 row | UXR row (Part C) | UI case | Existing API / integration sibling |
|---------|------------------|---------|-------------------------------------|
| C01 undo statement | UXR-C01 | TD-ADM-UI-del-01 | TD-ADM-091 |
| C02 modal copy per entity | UXR-C02 | TD-ADM-UI-del-01 | TD-ADM-090, TD-ADM-092, TD-ADM-093, TD-ADM-095 |
| C03 keyboard / focus | UXR-C03 | TD-ADM-UI-del-02, TD-ADM-UI-na-kbd, TD-ADM-UI-na-expire | TD-ADM-090 (step 4, cancel no audit) |
| C04 token expiry / double click | UXR-C04 | TD-ADM-UI-del-03, TD-ADM-UI-na-expire | TD-ADM-090 (expired / replay) |
| C05 Artifact block | UXR-C05 | TD-ADM-UI-del-04 | TD-ADM-094 |
| C06 typed confirmation | UXR-C06 | TD-ADM-UI-del-05 | TD-ADM-093, TD-ADM-095 |
| C07 post-delete routing / failure | UXR-C07 | TD-ADM-UI-del-06, TD-ADM-UI-na-expire | TD-ADM-064, TD-ADM-091, TD-ADM-102 |

### 3.11 Sign-in steps note v1 (UX1-A16) conditions → cases

| Condition | Short | Test cases |
|-----------|-------|------------|
| Shape | Step 1 email + password + Turnstile; step 2 6-digit TOTP + recovery link | TD-ADM-UI-auth-21, TD-ADM-UI-auth-07, TD-ADM-UI-auth-04 |
| SIGNIN-1 | Pending-auth token (≥128 bits, no session, ≤5 min, single use, cleared on success/expiry/lock, host-only Secure/HttpOnly/SameSite=Strict cookie, never in URL, step 2 only) | TD-ADM-023, TD-ADM-026, TD-ADM-UI-auth-21, TD-ADM-UI-auth-23 |
| SIGNIN-2 | One failure counter across both steps | TD-ADM-024, TD-ADM-050 |
| SIGNIN-3 | ≤5 code attempts per token, then restart step 1 with new Turnstile | TD-ADM-025, TD-ADM-UI-auth-23 |
| SIGNIN-4 | Same generic sentence on both steps; locked → identical 401 at step 1 even with correct password; never step 2 while locked | TD-ADM-026, TD-ADM-UI-auth-22, TD-ADM-UI-auth-01 |
| SIGNIN-5 | No Retry-After / duration / count / 'locked'; unknown email vs wrong password match incl. timing | TD-ADM-026, TD-ADM-UI-auth-22, TD-ADM-051 |
| SIGNIN-6 | `signin.second_factor_failed` audit + S-A12 activity; email SHOULD | TD-ADM-028, TD-ADM-UI-auth-19 (email SHOULD: OQ8) |
| SIGNIN-7 | Session only after step 2; rotate session ID; recovery code spent at once | TD-ADM-027, TD-ADM-UI-auth-21 |
| SIGNIN-8 | Step endpoints only on admin.core | TD-ADM-029, TD-ADM-023 |
| SIGNIN-C1 | IP throttle 429 + generic body, no Retry-After, on both steps | TD-ADM-024, TD-ADM-UI-auth-22 |
| SIGNIN-C2 | Step 2 failures count toward the login IP throttle | TD-ADM-024 |
| SIGNIN-C3 | Hash still runs when locked (timing) | TD-ADM-026 |
| SIGNIN-C4 | `signin.second_factor_failed` mapped in lockout note §8.8 / S-A12 list | TD-ADM-028, TD-ADM-UI-auth-19 |
| UX impact | Focus order step 1 / step 2; expired token → same sentence, back to step 1 | TD-ADM-UI-auth-04, TD-ADM-UI-auth-21, TD-ADM-UI-auth-23 |

### 3.12 Password-rules note v2.3 → cases

| Rule | Test cases |
|------|------------|
| 1 Length 15–128 code points after NFKC, same count client/server | TD-ADM-UI-auth-10 |
| 2 Characters / NFKC | TD-ADM-UI-auth-10 |
| 3 No composition / expiry / hints | TD-ADM-UI-auth-10 |
| 4 Bundled blocklist + context-word v1.2, local only | TD-ADM-UI-auth-10 |
| 5 Reuse = current only | TD-ADM-UI-auth-10 |
| 6 Hashing floor (Argon2id preferred, or PBKDF2-HMAC-SHA512 ≥220,000), per-user salt, never logged | TD-ADM-011 (review), TD-ADM-150 |
| 7 Server authority; client length only | TD-ADM-UI-auth-10 |
| 8 Paste / managers; `new-password` | TD-ADM-UI-auth-03 |
| 9 Rule reject doesn't count toward lockout / throttle; link stays usable | TD-ADM-UI-auth-10, TD-ADM-UI-auth-13 |
| UI table (helper text, named inline error, sessions end on success, TOTP unchanged) | TD-ADM-UI-auth-10, TD-ADM-UI-auth-13, TD-ADM-UI-auth-08 |

### 3.13 Addendum Parts B–D (UXR-B01…B18, UXR-C01…C08, UXR-D01…D16) → cases (UX1 approval condition 3)

Rule (UXR-C08, UXR-D14): every UXR-B, C and D row → ≥1 TD case ID. Result **42 / 42**. Generated from case traces.

| UXR | Test cases |
|-----|------------|
| UXR-B01 | TD-ADM-UI-na-B01, TD-ADM-UI-na-B08 |
| UXR-B02 | TD-ADM-UI-na-B02, TD-ADM-UI-na-B04 |
| UXR-B03 | TD-ADM-UI-na-B02, TD-ADM-UI-na-B03, TD-ADM-UI-na-B05 |
| UXR-B04 | TD-ADM-UI-na-B07 |
| UXR-B05 | TD-ADM-UI-na-B06 |
| UXR-B06 | TD-ADM-UI-na-B06, TD-ADM-177 |
| UXR-B07 | TD-ADM-UI-na-B03 |
| UXR-B08 | TD-ADM-UI-del-06, TD-ADM-UI-na-B08 |
| UXR-B09 | TD-ADM-UI-na-B14 |
| UXR-B10 | TD-ADM-UI-na-X04 |
| UXR-B11 | TD-ADM-UI-na-B15 |
| UXR-B12 | TD-ADM-UI-na-B09 |
| UXR-B13 | TD-ADM-UI-na-B09, TD-ADM-UI-na-X04 |
| UXR-B14 | TD-ADM-UI-na-B10, TD-ADM-UI-na-expire, TD-ADM-178 |
| UXR-B15 | TD-ADM-UI-na-B11 |
| UXR-B16 | TD-ADM-UI-na-B11 |
| UXR-B17 | TD-ADM-UI-na-B12 |
| UXR-B18 | TD-ADM-UI-auth-15, TD-ADM-UI-na-B13, TD-ADM-177, TD-ADM-179 |
| UXR-C01 | TD-ADM-UI-del-01 |
| UXR-C02 | TD-ADM-UI-del-01 |
| UXR-C03 | TD-ADM-UI-del-02, TD-ADM-UI-na-expire, TD-ADM-UI-na-kbd |
| UXR-C04 | TD-ADM-UI-del-03, TD-ADM-UI-na-expire |
| UXR-C05 | TD-ADM-UI-del-04 |
| UXR-C06 | TD-ADM-UI-del-05 |
| UXR-C07 | TD-ADM-UI-del-06, TD-ADM-UI-na-expire |
| UXR-C08 | TD-ADM-UI-del-01, TD-ADM-UI-del-02, TD-ADM-UI-del-03, TD-ADM-UI-del-04, TD-ADM-UI-del-05, TD-ADM-UI-del-06 |
| UXR-D01 | TD-ADM-UI-na-X01 |
| UXR-D02 | TD-ADM-UI-na-X03 |
| UXR-D03 | TD-ADM-UI-na-B13, TD-ADM-UI-na-X01, TD-ADM-179 |
| UXR-D04 | TD-ADM-UI-na-X04 |
| UXR-D05 | TD-ADM-UI-na-X04 |
| UXR-D06 | TD-ADM-UI-na-X05 |
| UXR-D07 | TD-ADM-UI-na-X05, TD-ADM-UI-na-kbd |
| UXR-D08 | TD-ADM-UI-na-X05, TD-ADM-UI-na-axe |
| UXR-D09 | TD-ADM-UI-na-X05, TD-ADM-UI-na-X06 |
| UXR-D10 | TD-ADM-UI-na-X05, TD-ADM-UI-na-axe, TD-ADM-UI-na-kbd |
| UXR-D11 | TD-ADM-UI-na-X06 |
| UXR-D12 | TD-ADM-UI-na-X02 |
| UXR-D13 | TD-ADM-UI-na-X02 |
| UXR-D14 | TD-ADM-UI-na-B01, TD-ADM-UI-na-B02, TD-ADM-UI-na-B03, TD-ADM-UI-na-B04, TD-ADM-UI-na-B05, TD-ADM-UI-na-B06, TD-ADM-UI-na-B07, TD-ADM-UI-na-B08, TD-ADM-UI-na-B09, TD-ADM-UI-na-B10, TD-ADM-UI-na-expire, TD-ADM-UI-na-B11, TD-ADM-UI-na-B12, TD-ADM-UI-na-B13, TD-ADM-UI-na-B14, TD-ADM-UI-na-B15, TD-ADM-UI-na-X01, TD-ADM-UI-na-X02, TD-ADM-UI-na-X03, TD-ADM-UI-na-X04, TD-ADM-UI-na-X05, TD-ADM-UI-na-X06, TD-ADM-UI-na-X09, TD-ADM-UI-na-axe, TD-ADM-UI-na-kbd |
| UXR-D15 | TD-ADM-UI-ci-01 |
| UXR-D16 | TD-ADM-UI-na-X09 |

### 3.14 Spec UX1 redlines note v2 (`eb63276f…`) → cases

| Item | Spec lock (short) | Test cases |
|------|-------------------|------------|
| RL-A05-TOTP | A05 TOTP: 6 digits, 30 s, HMAC-SHA1, ±1 step, last-accepted-step replay guard, secret encrypted at rest | TD-ADM-170 |
| RL-A05-RC | A05 recovery codes: 10, CSPRNG ≥50 bits, hashed, constant-time, burned on use, audited, regenerate replaces set | TD-ADM-UI-auth-08, TD-ADM-171 |
| RL-A05-COUNT | A05 failure counting: enrolment-confirm → shared account counter, not IP throttle; step-2 recovery → account + IP | TD-ADM-172 |
| RL-A05-NOSTORE | A05 no-store on S-A6 / S-A7; secret and codes never in a URL | TD-ADM-UI-auth-08, TD-ADM-173 |
| RL-A05-STEPUP | A05 S-A12 step-up within 5 minutes; all other sessions end; notice email SHOULD | TD-ADM-UI-auth-18, TD-ADM-174 |
| RL-A15 | A15 sign-out POST + anti-forgery; GET `/sign-out` does nothing; cookie clear; Back shows no data; audited | TD-ADM-UI-auth-17, TD-ADM-173, TD-ADM-175 |
| RL-A20 | A20 idle and absolute warnings; Stay signed in anti-forgery, idle only, ignores client expiry | TD-ADM-UI-auth-14, TD-ADM-UI-auth-24, TD-ADM-176 |
| RL-RETURN | Return path: server checks, exact allowlist, fixed origin, server-side carry; list query restored; failure → Stats | TD-ADM-UI-auth-15, TD-ADM-177 |
| RL-B10 | B10 FieldPolicy inputs only; transitions server-enforced; offers Open→Cancelled only; generic deny; old/new audit | TD-ADM-UI-na-B10, TD-ADM-UI-na-expire, TD-ADM-178 |
| RL-B01 | B01 default columns and sort; Created fallback; Deleted badge beside status | TD-ADM-UI-na-B01 |
| RL-X01 | X01 screen inventory and routes | TD-ADM-UI-auth-06, TD-ADM-UI-na-X01 |
| RL-LINKS | Bootstrap / reset links single use, hashed, invalidated by use or a newer link | TD-ADM-180 |
| RL-SD | S-D3 / S-D4 show no entity data and look the same | TD-ADM-UI-na-X01, TD-ADM-179 |

### 3.15 Status-list note (`0a5ff57b…`) and Product decision (`6ea05ddb…`) → cases

| Item | Rule | Test cases |
|------|------|------------|
| STATUS-N | Negotiations: Open, Closed, Expired only (no Withdrawn) | TD-ADM-062, TD-ADM-UI-na-B03, TD-ADM-UI-na-expire, TD-ADM-178 |
| STATUS-O | Offers: Open, Accepted, Declined, Withdrawn, Superseded (countered), Cancelled | TD-ADM-063, TD-ADM-UI-na-B03, TD-ADM-178 |
| STATUS-D | Deleted is a badge beside the status, never a filter status | TD-ADM-062, TD-ADM-063, TD-ADM-064, TD-ADM-UI-na-B01, TD-ADM-UI-na-B03, TD-ADM-UI-na-B08 |
| PROD-W | Withdrawn offer-only, maker-only; admin can't set it; admin offer change Open→Cancelled only | TD-ADM-063, TD-ADM-UI-na-B03, TD-ADM-UI-na-B10, TD-ADM-178 |

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
- **r4 note:** With two steps (sign-in steps note v1) the partial-auth state is the pending-auth token, which grants only the step 2 endpoint — detailed in TD-ADM-023.
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
- **r5 note:** Links are stored hashed and a newer link invalidates the older one (redlines note v2) — TD-ADM-180.
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Test harness can mint bootstrap token via mail interface fake (no real SES).
- **Steps:** 1) Consume bootstrap link — set first password + enroll TOTP. 2) Reuse same link. 3) Use expired (>24h) link. 4) Use truncated / random token.
- **Expected:** First use succeeds. Reuse, expired and bad tokens all get the **same** generic invalid-or-expired link reply (same status, same body) — no hint which case applied (UX1-A03 r2). Each bad/expired submission that passes Turnstile counts toward the reset/bootstrap per-IP counter (lockout note OQ3). Password never in email body/docs/fixtures.
- **Negative / abuse variants:** Replay; expired; truncated token; response differences between reused vs expired vs random token (must be none).
#### TD-ADM-011 — First password set only via bootstrap link; no seeded password

- **Traces-to:** AC7, SPEC-S8.1, SPEC-S8.2, SPEC-S8.10, SPEC-SEC-2, SPEC-SEC-14, STEP-2, STEP-12, PLAN-SEC-2, PLAN-SEC-13
- **r4 note:** Password stored per password-rules note v2.3 rule 6 (Argon2id preferred at or above the floor, or PBKDF2-HMAC-SHA512 ≥220,000 iterations; per-user salt; never logged) — review item; acceptance rules in TD-ADM-UI-auth-10.
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
- **r5 note:** TOTP parameters locked by redlines note v2 A05 (6 digits, 30 s, HMAC-SHA1, ±1 step, last-accepted-step replay guard) — TD-ADM-170.
- **r4 note:** Step 1 = password, step 2 = code (sign-in steps note v1). 'Omit TOTP' means stopping after step 1: no session (TD-ADM-027). Shared counter, cap and audit for step 2 failures: TD-ADM-024, TD-ADM-025, TD-ADM-028.
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
- **r5 note:** 10 codes from a CSPRNG, keyed-hash or Argon2id at rest, burned on use, regenerate replaces the set (redlines A05) — TD-ADM-171; failure counting TD-ADM-172.
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** TOTP enrollment flow.
- **Steps:** 1) Enroll TOTP — capture recovery codes once. 2) Re-fetch enrollment — codes not re-shown. 3) Use one recovery code for 2FA. 4) Reuse same code. 5) Inspect DB/store for plaintext codes.
- **Expected:** Shown once; stored hashed; single-use success then reject; no plaintext in DB/logs/audit/UI dump.
- **Negative / abuse variants:** Reuse; dump recovery codes via error payloads.

### 4.3a Two-step sign-in (UX1-A16, sign-in steps note v1)

Binding: sign-in steps note v1 `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` and A16 decision `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34`; counters per lockout note v5.5. Trace tokens `SIGNIN-1…8`, `SIGNIN-C1…C4` = note conditions (§3.11). Local / CI test host only; no live admin.core calls.

#### TD-ADM-023 — Pending-auth token after correct step 1: opaque ≥128 bits, no session, ≤5 min, single use, cleared on success/expiry/lock, host-only Secure/HttpOnly/SameSite=Strict cookie, never in URL, grants only step 2

- **Traces-to:** AC7, SPEC-S8.1, SPEC-S8.3, SPEC-SEC-3, SPEC-SEC-7, STEP-3, STEP-11, PLAN-SEC-3, PLAN-SEC-9, SIGNIN-1, SIGNIN-7, SIGNIN-8, UX1-A16
- **Layer:** integration+API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** WebApplicationFactory host bound to admin.core; Turnstile test keys by env name; controllable clock; read access to the pending-token store in test.
- **Steps:** 1) Correct step 1; capture all Set-Cookie headers and the body. 2) With only the pending cookie, call every admin API (lists, detail, edit, delete-intent, stats, audit, security settings). 3) Sample 1,000 tokens: length, randomness, no relation to email/time; confirm the server-side record binds account + Turnstile result. 4) Present the token after 5 min. 5) Reuse it after a successful step 2. 6) Present it with a different account or a different Turnstile result. 7) Lock the account while the token is outstanding, then present it. 8) Check redirects (Location), URLs, logs and audit for the token. 9) Read cookie attributes. 10) Send the cookie with a non-admin.core Host.
- **Expected:** No session cookie at step 1; the pending cookie grants only the step 2 endpoint (everything else denies as TD-ADM-003/004); token opaque with ≥128 bits of randomness; expires ≤5 min; single use; cleared on success, expiry and lock; binding mismatches refused; token never in URL, Location, logs or audit; cookie Secure, HttpOnly, SameSite=Strict and host-only (no Domain attribute); not honoured on any other host. Expired / used / cleared token gets the same generic body as a step-2 failure.
- **Negative / abuse variants:** Session or partial-session cookie at step 1; token in query string; token accepted twice or after 5 min; Domain attribute widening scope.

#### TD-ADM-024 — One shared account failure counter across step 1 password and step 2 TOTP/recovery failures; step 2 failures also count toward the login IP throttle; 429 on both steps

- **Traces-to:** AC7, SPEC-S8.3, SPEC-S8.6, SPEC-SEC-5, STEP-5, PLAN-SEC-5, SIGNIN-2, SIGNIN-C1, SIGNIN-C2, UX1-A16
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Controllable clock and client IP; valid Turnstile test token on every attempt; counters readable in test; several synthetic accounts.
- **Steps:** 1) Inside 15 min: 2 wrong passwords at step 1, then correct step 1 and 3 wrong codes at step 2 (mix TOTP and recovery). 2) Read the account counter after each attempt. 3) Spread failures over several pending tokens. 4) From one IP, 20 counted failures mixing step 1 and step 2 across accounts; then submit step 1 and step 2. 5) After a lock ends, sign in fully; read account and IP counters.
- **Expected:** Every counted failure on either step adds to the one account counter and window (lockout note v5.5 (`8a194eb9…`): 5 / 15 min → 30 min); the account locks at the 5th, whichever step it came from; no separate or higher step 2 budget; step 2 failures add to the login IP counter (C2); throttled step 1 and step 2 both return 429 with the generic body and no `Retry-After` (C1); a full sign-in clears the account count only, not the IP counter.
- **Negative / abuse variants:** Step 2 failures uncounted; per-token counter instead of per-account; step 2 escaping the IP throttle.

#### TD-ADM-025 — At most 5 code attempts per pending token, then the token is void and sign-in restarts at step 1 with a new Turnstile (on top of the shared counter)

- **Traces-to:** AC7, SPEC-S8.3, SPEC-S8.6, SPEC-SEC-5, STEP-5, PLAN-SEC-5, SIGNIN-3, UX1-A16
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Two test hosts: (A) test-only raised account threshold so the cap can be seen without a lock — set in the local test host only, never in deployed config; (B) default config.
- **Steps:** A1) Correct step 1; 5 wrong codes; 6th attempt with a valid TOTP on the same token. A2) Restart step 1 reusing the old Turnstile result. A3) New Turnstile + correct step 1 + valid code. B1) Default config: 5 wrong codes on one token. B2) 4 wrong codes, then a valid code.
- **Expected:** A1: 6th attempt refused with the generic body, token void. A2: old Turnstile result refused. A3: signs in. B1: the 5th wrong code locks the account through the shared counter and clears the token (TD-ADM-024 / lockout note v5.5). B2: signs in; account count cleared. The cap never raises or replaces the account budget.
- **Negative / abuse variants:** More than 5 attempts on one token; token survives the cap; raised threshold leaking into deployed config.

#### TD-ADM-026 — Locked account at step 1 returns the identical 401 even with the correct password, issues no pending token, never reaches step 2; hash still runs (timing)

- **Traces-to:** AC7, SPEC-S8.6, SPEC-SEC-5, STEP-5, PLAN-SEC-5, SIGNIN-1, SIGNIN-4, SIGNIN-5, SIGNIN-C3, UX1-A16, UX1-A03
- **Layer:** integration+API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Lock hook (TD-ADM-050); timing harness with repeated samples; threshold for a timing gap set in harness config and reported with results.
- **Steps:** 1) Lock the account. 2) Step 1 with correct password, wrong password and an unknown email; ≥30 samples each. 3) Check Set-Cookie for a pending token. 4) Present a pending token issued before the lock at step 2. 5) Compare status, body, headers and timing.
- **Expected:** All three return 401 with the byte-identical UX1-A03 r2 body; no pending token issued while locked; a token issued before the lock was cleared and step 2 refuses it with the generic body; step 2 never reveals a correct password during a lock; password hash runs when locked (C3) and against a dummy hash for an unknown email (condition 5), so timing shows no gap above the harness threshold; no `Retry-After`, duration, count or 'locked'.
- **Negative / abuse variants:** Pending token issued to a locked account; measurable fast path for locked or unknown accounts.

#### TD-ADM-027 — Session issued only after a valid TOTP or recovery code at step 2; session ID rotated; pending token invalidated; recovery code spent immediately

- **Traces-to:** AC7, SPEC-S8.3, SPEC-S8.7, SPEC-SEC-3, SPEC-SEC-7, STEP-3, PLAN-SEC-3, SIGNIN-7, UX1-A16
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Enrolled CoreOwner; recovery codes in a test-only store; any pre-auth cookie captured before step 1.
- **Steps:** 1) After step 1, confirm no session. 2) Valid TOTP at step 2; compare any pre-existing session identifier with the new one. 3) Reuse the pending token. 4) Repeat with a valid unused recovery code. 5) Race: two pending tokens submit the same recovery code concurrently. 6) Read the store after each.
- **Expected:** Session only after step 2; session ID new (rotated); pending token unusable after success; recovery code marked spent at once — exactly one of the concurrent requests succeeds; audit `recovery_code_use` once.
- **Negative / abuse variants:** Session before step 2; fixed session ID; recovery code usable twice under race.

#### TD-ADM-028 — Audit `signin.second_factor_failed` when a TOTP or recovery code fails after a correct password; shown in S-A12 activity as 'code failed after correct password'; never echoed to the client

- **Traces-to:** AC7, SPEC-S8.8, SPEC-S10, SPEC-SEC-6, STEP-8, PLAN-SEC-8, SIGNIN-6, SIGNIN-C4, UX1-A16
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Audit store and S-A12 activity data readable in test.
- **Steps:** 1) Correct step 1, wrong TOTP. 2) Correct step 1, wrong recovery code. 3) Control: wrong password at step 1. 4) Read audit rows and the S-A12 activity feed. 5) Compare client responses for 1–3.
- **Expected:** Steps 1–2 write `signin.second_factor_failed` under reason class bad 2FA with the after-correct-password flag (lockout note v5.5 (`8a194eb9…`) §8.8), keyed-HMAC IP prefix only, no code value; step 3 does not write it; the S-A12 feed lists it with the label 'code failed after correct password' (UI in TD-ADM-UI-auth-19); client responses identical across 1–3 and never name the factor.
- **Negative / abuse variants:** Event missing or written for a wrong password; raw IP or code value in audit; reason class echoed to client.

#### TD-ADM-029 — Both sign-in step endpoints exist only on admin.core.dealoware.com — never on api.core or the raw host

- **Traces-to:** AC1, LOCK-1, SPEC-S3, SPEC-S8.1, SPEC-SEC-1, STEP-1, PLAN-SEC-1, SIGNIN-8, UX1-A16
- **Layer:** API+review
- **Priority:** P0
- **Automation:** auto+manual
- **Preconditions:** Local WebApplicationFactory / test host with Host-header binding; no live calls (live only after Ivan deploy OK and the PR #11 conditions).
- **Steps:** 1) Call step 1 and step 2 with Host admin.core.dealoware.com. 2) Same with Host api.core.dealoware.com and with a raw-host / IP Host header. 3) Review route registration.
- **Expected:** Endpoints answer only for admin.core; other hosts get not-found / deny with no auth behaviour; a pending cookie is never honoured on other hosts.
- **Negative / abuse variants:** Step endpoints reachable via api.core or raw host.

### 4.3b Spec UX1 redlines note v2 (A05, A15, A20, return path, B10, X01)

Binding: Spec UX1 redlines note v2 `eb63276fc51217c3c82772c0b8de26ead1337f6e382938a1d736a05d13f6f2d0` (Security QA + Spec QA PASS). Trace tokens `RL-*` = §3.14; `STATUS-*` / `PROD-W` = §3.15. Routes only from Spec X01. Local / CI test host only; no live admin.core calls. UX1-A03 r2 client rules (§0c) still apply to every auth response here.

#### TD-ADM-170 — TOTP is 6 digits, 30 s, HMAC-SHA1; accepts current ±1 step only; per-account last-accepted step blocks replay; secret encrypted at rest and never shown after enrolment

- **Traces-to:** AC7, SPEC-S8.3, SPEC-SEC-3, STEP-3, PLAN-SEC-3, RL-A05-TOTP, UX1-A05
- **Layer:** integration+API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Enrolled CoreOwner; codes generated in the harness from `TOTP_SEED_ENV`; controllable clock; test read of the per-account last-accepted step and the secret column.
- **Steps:** 1) Read enrolment parameters on S-A6 (digits, period, algorithm) without logging the secret. 2) At step T submit codes for T-2, T-1, T, T+1, T+2, each on a fresh pending token. 3) Sign in with the code for T; reuse the same code on a new pending token inside its window. 4) After success at T, submit a valid code for T-1. 5) Submit 5-digit, 7-digit and non-numeric codes. 6) Read the secret at rest, audit, logs, error bodies and API responses after enrolment. 7) Review the compare (constant time) and run a timing smoke of right vs wrong codes.
- **Expected:** 6 digits, 30-second period, HMAC-SHA1; T-1, T and T+1 accepted, T-2 and T+2 refused with the generic step-2 body; the last-accepted step is stored per account and any code at that step or earlier is refused, so the same code twice and a valid older-step code both fail with the generic body; malformed codes refused the same way; the secret is encrypted at rest and never appears in audit, logs, errors or API bodies after enrolment; compare is constant time.
- **Negative / abuse variants:** Same code accepted twice in its window; ±2 drift accepted; plaintext secret in the store or logs; another algorithm or period.

#### TD-ADM-171 — Recovery codes: 10 from a CSPRNG, ≥50 bits each, stored hashed, constant-time compare, burned on use, each use audited, regenerate replaces the set, shown once and never in a URL

- **Traces-to:** AC7, SPEC-S8.3, SPEC-SEC-3, STEP-3, PLAN-SEC-3, RL-A05-RC, UX1-A05, UXR-A30, UXR-A31
- **Layer:** integration+review
- **Priority:** P0
- **Automation:** auto+manual
- **Preconditions:** Fresh enrolment in the test host; test read of the recovery-code store; audit read.
- **Steps:** 1) Complete enrolment; count codes on S-A7. 2) Read the store for each code. 3) From the alphabet and length compute bits per code; review the generator. 4) Use one code at sign-in step 2 (S-A3); reuse it. 5) Regenerate from S-A12 with step-up; try an old unused code. 6) Read audit after each use. 7) Check URLs, history and Referer around S-A7.
- **Expected:** Exactly 10 codes; each ≥50 bits from a CSPRNG; stored only as keyed HMAC-SHA256 (server key) or Argon2id hashes, compared in constant time; burned on first use (reuse refused with the generic body); each use audited without the code value; regenerate replaces the whole set so old unused codes fail; codes shown once on S-A7 and never in a URL.
- **Negative / abuse variants:** Wrong count; plaintext or unkeyed hash; code usable twice; part of the old set still valid.

#### TD-ADM-172 — Failure counting: enrolment-confirm failures count on the one shared account counter but never on the login IP throttle; recovery-code failures at sign-in step 2 count on both

- **Traces-to:** AC7, SPEC-S8.3, SPEC-S8.6, SPEC-SEC-5, STEP-5, PLAN-SEC-5, RL-A05-COUNT, SIGNIN-2, SIGNIN-C2, UX1-A05
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Counters readable in test; controllable client IP and clock; first-enrolment and S-A12 re-enrol paths in the test host.
- **Steps:** 1) During first enrolment (S-A6) submit wrong confirm codes; read the account counter and the login IP counter after each. 2) Repeat during S-A12 re-enrol (after step-up). 3) At sign-in step 2 submit wrong recovery codes on S-A3; read both counters. 4) Reach the account threshold with enrolment-confirm failures only; then sign in with the correct password. 5) From one IP send more than 20 enrolment-confirm failures across accounts; then sign in from that IP.
- **Expected:** Enrolment-confirm failures add to the one shared account counter (A16 condition 2, lockout note v5.5) and never to the login IP throttle; recovery-code failures at step 2 add to both the account counter and the login IP throttle; reaching the threshold by enrolment-confirm failures locks the account, which shows only the identical 401 (UX1-A03 r2); enrolment-confirm failures alone never throttle the IP; no separate enrolment counter exists.
- **Negative / abuse variants:** Enrolment failures uncounted or counted on the IP throttle; recovery failures skipping the IP counter.

#### TD-ADM-173 — `Cache-Control: no-store` on S-A6, S-A7 and every admin page so Back after sign-out shows no entity data; secret and codes never in a URL

- **Traces-to:** SPEC-S8.3, SPEC-S8.7, SPEC-SEC-3, STEP-3, STEP-5, RL-A05-NOSTORE, RL-A15, UX1-A05, UX1-A15, UXR-A41
- **Layer:** integration+E2E-UI
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Signed-in CoreOwner; browser with HTTP cache and back-forward cache on.
- **Steps:** 1) Fetch S-A6 and S-A7 and the API calls behind them; read Cache-Control. 2) Fetch S-B1–S-B6, S-D1, S-D2 and their API calls; read Cache-Control. 3) Sign out (POST); press Back; reload cached URLs. 4) Search URLs, history and Referer for the TOTP secret and recovery codes. 5) Record whether sign-out sends `Clear-Site-Data: "cache"` (SHOULD).
- **Expected:** S-A6, S-A7 and every admin page carry `Cache-Control: no-store`; after sign-out Back and reload show no entity data; the secret and codes never appear in a URL; `Clear-Site-Data` presence recorded as a SHOULD observation (not a fail).
- **Negative / abuse variants:** `private, max-age=…` or no header; entity HTML from cache after sign-out.

#### TD-ADM-174 — S-A12 re-enrol and regenerate need step-up (current password + current TOTP or recovery code within 5 minutes); a live session alone is not enough; success ends every other session

- **Traces-to:** AC7, SPEC-S8.3, SPEC-S8.7, SPEC-SEC-3, SPEC-SEC-7, STEP-3, PLAN-SEC-3, RL-A05-STEPUP, UX1-A21, UXR-A42
- **Layer:** integration+API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Signed-in CoreOwner in contexts A (acting), B and C; controllable clock; mail interface fake; audit read.
- **Steps:** 1) From A call re-enrol and regenerate with only the session. 2) Step-up with wrong password + valid code; valid password + wrong code; valid password + valid recovery code. 3) Pass step-up, advance 5 min 1 s, call re-enrol. 4) Pass step-up; re-enrol, then regenerate within 5 minutes. 5) Make a request in B and C after each success. 6) Read the mail fake. 7) Read audit.
- **Expected:** Session-only calls refused; step-up needs the current password plus a current TOTP or recovery code entered within the last 5 minutes, otherwise refused with no factor named; after 5 minutes step-up is asked again; after each success every other session (B, C) ends and lands on S-A1 while A stays signed in; a notice email through the mail interface is a SHOULD (record it, don't fail); audit `totp_change` and regenerate entries carry no secret.
- **Negative / abuse variants:** Step-up older than 5 minutes accepted; other sessions survive; session cookie alone passes.

#### TD-ADM-175 — Sign out is POST with anti-forgery; GET /sign-out signs nobody out; POST clears session row, pending token and cookie (same attributes, Max-Age=0); sign-out audited

- **Traces-to:** SPEC-S8.7, SPEC-S8.8, SPEC-SEC-6, SPEC-SEC-7, STEP-5, STEP-8, PLAN-SEC-8, RL-A15, UX1-A15, UXR-A41
- **Layer:** integration+security
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Signed-in CoreOwner; a second tab holding a pending sign-in token; cross-site test origin; audit read.
- **Steps:** 1) Send GET `/sign-out` (navigation, prefetch, image tag); call an admin API. 2) POST sign-out without the anti-forgery token, with another session's token, and from the cross-site origin. 3) POST with a valid token. 4) Read the session store, pending-token store and Set-Cookie. 5) Use the pending token in the second tab. 6) Replay the old cookie. 7) Read audit.
- **Expected:** GET `/sign-out` leaves the session valid; POSTs without a valid token or from another site are refused and the session survives; the valid POST deletes the session row, clears any pending sign-in token, and clears the cookie with the same name, host-only scope, Path, Secure, HttpOnly, SameSite=Strict plus `Max-Age=0`; next screen S-A1/S-A11 with 'You've been signed out.'; the pending token and old cookie are refused; one sign-out audit event with no secret material.
- **Negative / abuse variants:** Sign-out by GET; sign-out CSRF; cookie cleared with different attributes; no audit.

#### TD-ADM-176 — Stay signed in: authenticated + anti-forgery; renews only the idle window; never moves or re-issues the 8-hour cap; server ignores client-sent expiry; client timers never keep a session alive

- **Traces-to:** SPEC-S8.7, SPEC-SEC-5, STEP-5, PLAN-SEC-5, RL-A20, UX1-A20, UXR-A38, UXR-A45
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Signed-in CoreOwner; controllable clock; test read of idle and absolute expiry.
- **Steps:** 1) At 28 min idle call Stay signed in with a valid token; read idle and absolute expiry. 2) Call it without the token and from another site. 3) Send a far-future expiry in the body, a header and a cookie. 4) From 7h55m call it every minute past 8h. 5) Run the client timers with no server call past 30 min idle; then make a request. 6) After expiry, make a request.
- **Expected:** Valid call renews only the idle window; absolute expiry unchanged and never re-issued; calls without anti-forgery or from another site refused; client expiry values ignored; no call extends past 8h; client timers alone never keep the session; after expiry the next request goes to sign-in with 'Your session ended. Sign in again to continue.' and needs password + TOTP.
- **Negative / abuse variants:** Cap extended; client expiry honoured; renew without anti-forgery.

#### TD-ADM-177 — Return path checked by the server only: one decode, reject rules, 2048 max, exact allowlist, fixed origin, carried server-side; known list query restored and re-validated, unknown dropped; any failure → Stats

- **Traces-to:** SPEC-S8.7, SPEC-SEC-7, STEP-5, STEP-11, PLAN-SEC-9, RL-RETURN, UX1-A20, UX1-B13, UXR-A11, UXR-B06, UXR-B18
- **Layer:** integration+security
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Signed-out API client and browser; Negotiations and Offers views with known sort, filter, page, page-size and deleted-toggle parameters (names from the built query schema).
- **Steps:** For each value: start at it signed out (or expire the session there), finish both sign-in steps, read Location and the page. 1) Allowlist: `/`, `/participants`, `/artifacts`, `/negotiations`, `/offers`, `/{type}/{id}` for each of the four types, `/audit`, `/audit/{id}`. 2) Rejects: `//host`, `/\host`, any `\`, schemes (`https:`, `javascript:`), a `..` segment, `%2f` / `%5c` / `%2e%2e` / `%25` left after one decode, Unicode Cc and Cf characters, 2049 characters, S-A routes (`/sign-in`, `/sign-in/code`, `/settings/security`), `/setup/*`, `/reset*`, the bootstrap link path, an unknown route (S-D3), `/{type}/{id}` with a non-entity type. 3) A list URL with known parameters plus one unknown parameter and one invalid known value. 4) Read the step 1 and step 2 URLs. 5) Compare each Location with the raw input.
- **Expected:** Allowlisted paths return there after sign-in; known list parameters restored and re-validated server-side, unknown ones dropped; every reject and every failure lands on Stats (S-B1); Location is built on `https://admin.core.dealoware.com` plus the checked path and never echoes the raw value; the return value is carried server-side with the pending sign-in token and never in the sign-in step query strings.
- **Negative / abuse variants:** Open redirect; return to an S-A, setup, reset or S-D route; unknown parameter carried; return value in a query string.

#### TD-ADM-178 — Server enforces SA §4 edit fields and status transitions (UI bypassed): negotiations Open→Closed / Open→Expired only; admin offers Open→Cancelled only; Withdrawn never admin-set; generic deny; old/new audit

- **Traces-to:** AC2, AC3, AC4, SPEC-S4.1, SPEC-S4.2, SPEC-S4.3, SPEC-S4.4, SPEC-S5, SPEC-SEC-7, SPEC-SEC-12, STEP-9, PLAN-SEC-9, PLAN-SEC-12, RL-B10, STATUS-N, STATUS-O, PROD-W, UX1-B10, UXR-B14
- **Layer:** API (direct, bypassing UI)
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic records in every status; API client with session + anti-forgery; FieldPolicy fixtures; fault hook inside the close/expire transaction; audit read.
- **Steps:** 1) Negotiation status: Open→Closed, Open→Expired; then Closed→Open, Expired→Open, Closed→Expired, Open→Withdrawn, Open→Accepted. 2) Close and expire negotiations holding 3 open offers + 1 accepted; repeat with the fault hook after the offer update. 3) EndsAt: valid while Open; before StartsAt; on a Closed negotiation. 4) Offer status: Open→Cancelled; Open→Accepted, Declined, Superseded, Withdrawn; any change from each final status. 5) Offer Amount/Terms on Open vs non-Open; Amount -1, 3 decimals, Amount without Currency, 4-letter Currency, Terms 2001 characters, both cleared. 6) Participant DisplayName 256 / 257; IsActive both ways; writes to Id, Sub, LoginEmail, ContactEmail, CreatedAt. 7) Artifact Name 4096 / 4097 / empty; Description empty; Owner set to a soft-deleted or unknown participant; writes to Intent or values. 8) Compare deny bodies. 9) Read audit for allowed edits.
- **Expected:** The server enforces every rule whatever the UI shows: negotiations only Open→Closed and Open→Expired, Closed and Expired final; close or expire moves every open offer to Cancelled in the same transaction (the fault leaves both unchanged); admin offer change only Open→Cancelled; Accepted, Declined, Withdrawn, Superseded and Cancelled final; the admin can never set Withdrawn; Amount, Terms and EndsAt editable only while Open; limits and required rules per SA §4 / redlines B10; read-only fields never writable; every disallowed change or denied field returns the same generic deny with no state detail; each allowed edit audits old and new values for non-secret fields only; stale versions get 409 (TD-ADM-081).
- **Negative / abuse variants:** API allows what the UI hides; deny names the current state; admin sets Withdrawn; offers cancelled outside the transaction.

#### TD-ADM-179 — S-D3 and S-D4 show no entity data and look the same wherever FieldPolicy hides whether a record exists

- **Traces-to:** SPEC-S3, SPEC-SEC-7, STEP-11, PLAN-SEC-9, RL-SD, UX1-X01, UX1-B13, UXR-D03, UXR-B18
- **Layer:** integration+E2E-UI
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Signed-in CoreOwner; missing IDs, a soft-deleted ID (toggle off), and a record whose existence FieldPolicy hides; a 403 action; timing harness.
- **Steps:** 1) Signed in: open an unknown route, a missing ID per type, the soft-deleted ID, the hidden record, and trigger the 403. 2) Signed out: open an unknown route. 3) Capture status, headers, body, DOM, title, size and timing. 4) Compare missing vs hidden vs deleted.
- **Expected:** S-D3 'We can't find that page.' with a Stats link (signed out: S-A1); S-D4 'You can't do that.' with a Stats link; neither shows entity data in DOM, title or API body; where FieldPolicy hides existence the missing, hidden and deleted cases return the same status, body and size with no timing gap above the harness threshold.
- **Negative / abuse variants:** Record name or field in title or body; 403 vs 404 that reveals a hidden record.

#### TD-ADM-180 — Bootstrap and reset links are single use, stored hashed, and invalidated when used or when a newer link is issued

- **Traces-to:** SPEC-S8.2, SPEC-S8.4, SPEC-SEC-2, SPEC-SEC-4, STEP-2, STEP-4, PLAN-SEC-2, PLAN-SEC-4, RL-LINKS, UXR-A25, UXR-A26
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Mail interface fake; test read of the link store; controllable clock.
- **Steps:** 1) Issue a bootstrap link; read the store. 2) Issue a second one; open the first, then the second. 3) Request a reset link twice; open the first, then the second. 4) Use a link; open it again. 5) Search logs and audit for link tokens.
- **Expected:** Only a hash is stored (no plaintext token in the store, logs or audit); each link works once; a newer link invalidates the older one; used, superseded and expired links get the generic invalid-link reply and the same S-A5 page per link type (TD-ADM-UI-auth-09).
- **Negative / abuse variants:** Older link still valid; plaintext token at rest.

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
- **r5 note:** Reset links stored hashed; a newer link invalidates the older one — TD-ADM-180.
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
- **r4 note:** Lockout note re-cited at v5.5 (rules unchanged). Step 2 code failures count toward the same account counter (TD-ADM-024); a locked account at step 1 never reaches step 2 (TD-ADM-026).
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic CoreOwner; valid Turnstile test token on every attempt (so attempts reach the credential check); controllable clock; test access to counters and audit store. Binding: UX1-A03 r2; lockout note v5.5 OQ3 + REPLACED unlock-by-reset.
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
  - No unlock control exists in the public admin UI; the only early unlock is the audited server-side ops (break-glass) command (lockout note v5.5) — out of scope for client tests, checked by review.
  - Audit: failures with reason class bad password / bad 2FA; lock start event with account, keyed-HMAC IP, start time and duration; in-lock attempts as `locked`. No secrets, no raw IP.
- **Negative / abuse variants:** Burst fails; lock persists across IP changes for the same account; timing gap between locked / unknown / wrong-password responses (hash check still runs — flag large gaps); “locked” or duration leaking via headers, body, or UI copy; reset used as a lock bypass.
#### TD-ADM-051 — Per-IP throttle (20 in sliding 15 min → flat 30 min) → HTTP 429 with generic body, no Retry-After (login + reset/bootstrap)

- **Traces-to:** AC7, SPEC-S8.6, SPEC-S8.8, SPEC-SEC-5, STEP-5, PLAN-SEC-5
- **r4 note:** Lockout note re-cited at v5.5. Login IP throttle applies to both sign-in steps, and step 2 failures count toward it (TD-ADM-024, sign-in steps note C1–C2).
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Controllable client IP in test (WebApplicationFactory); valid Turnstile test token on every attempt; controllable clock; mail interface fake; test access to counters and audit store. Several synthetic accounts / unknown emails so the account lock (5) does not mask the IP throttle. Binding: UX1-A03 r2 + Chief Spec / CPM correction; lockout note v5.5 (separate reset/bootstrap IP counter; MUST 429). Core API H4 (429 **plus** Retry-After) is a separate track and is **not** the expectation here.
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
  - Step 5 (bootstrap/reset link throttled): **HTTP 429** with the **same generic sign-in body** as the 401 (lockout note v5.5); no `Retry-After`; a valid token is not consumed while throttled. Before the throttle starts, bad/expired links get the generic invalid-or-expired link reply (TD-ADM-010 / TD-ADM-031).
  - Step 6: counters independent per lockout note.
  - Step 7: throttle lifts after a flat 30 min; that counter starts from zero. A completed password reset does **not** reset either IP counter.
  - Audit: `rate-limited` rows with keyed-HMAC IP only; no raw IP, no secrets.
- **Negative / abuse variants:** `Retry-After` present on any admin.core auth response (FAIL); 429 body differing from the 401 generic body or the generic reset / invalid-link reply (FAIL); account information in 429 responses; cross-IP account lock still applies; process-global limiter must be absent unless Chief Security approved (TD-ADM-054).
- **Note:** UX1-A03 r2 accepts 429 or 401 for the IP throttle; lockout note v5.5 and Chief Spec / CPM lock **MUST 429**. This design asserts 429.
#### TD-ADM-052 — Session idle 30m sliding + absolute 8h; cookie flags

- **Traces-to:** AC7, SPEC-S8.7, SPEC-SEC-5, STEP-5, PLAN-SEC-5
- **r5 note:** Idle and absolute warnings, Stay signed in rules (redlines A20) — TD-ADM-176, TD-ADM-UI-auth-14, TD-ADM-UI-auth-24; no-store and sign-out — TD-ADM-173, TD-ADM-175.
- **UI siblings (r3):** TD-ADM-UI-auth-14 (idle warning, expiry message), TD-ADM-UI-auth-13 (sessions end after reset), TD-ADM-UI-auth-17 (sign-out)
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

#### TD-ADM-062 — Negotiations list (Open, Closed, Expired) + sort/filter/paging + name search

- **Traces-to:** AC4, LOCK-3, SPEC-S4.3, SPEC-S4.5, SPEC-S4.6, SPEC-SEC-11, STEP-7, PLAN-SEC-11, STATUS-N, STATUS-D
- **r5 note:** Status values per the status-list note `0a5ff57b…`: negotiations have **Open, Closed, Expired** only; there is no negotiation-level Withdrawn, Accepted or Declined. Withdrawn is an offer status (TD-ADM-063). No amount or price sort/filter on negotiations (UXR-B02). UI side: TD-ADM-UI-na-B03, -B04.
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic negotiations in each negotiation status — Open, Closed, Expired — plus soft-deleted rows.
- **Steps:** 1) Default list excludes soft-deleted and includes Open, Closed and Expired. 2) Sort by Created, Updated, Status; filter by status (Open, Closed, Expired), created/updated range, participant, artifact and exact negotiation id. 3) Page offset/limit default 50 max 200. 4) Search composite artifact+participant names. 5) Toggle include soft-deleted (CoreOwner). 6) Send status filter values outside the negotiation list (`Withdrawn`, `Accepted`, `Deleted`) and an amount/price sort.
- **Expected:** Server-side only; parameterized; FieldPolicy on payloads; status values exactly Open, Closed, Expired; out-of-list status values and amount/price sorts match no rows and are not applied (response shape per Spec §4.5; no state detail); soft-deleted only via toggle, where Deleted is a badge beside the status, never a status value.
- **Negative / abuse variants:** Client-side full dump; page size >200 rejected; injection; any negotiation row or filter option carrying Withdrawn.

#### TD-ADM-063 — Offers all-status list + sort/filter/paging + name search

- **Traces-to:** AC4, LOCK-3, SPEC-S4.4, SPEC-S4.5, SPEC-S4.6, SPEC-SEC-11, STEP-7, PLAN-SEC-11, STATUS-O, STATUS-D, PROD-W
- **r5 note:** Offer statuses per the status-list note: Open, Accepted, Declined, **Withdrawn**, Superseded (UI label 'Superseded (countered)'), Cancelled. Withdrawn is offer-only and read-only: only the maker sets it while Open; the admin can't (Product decision `6ea05ddb…`; server check in TD-ADM-178). Nothing writes Withdrawn yet (Active holds 4), so Withdrawn rows are seeded in the test store.
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic offers in all six offer statuses (Open, Accepted, Declined, Withdrawn, Superseded, Cancelled; Withdrawn seeded directly) + soft-deleted.
- **Steps:** Analogous to TD-ADM-062, plus Amount sort and min/max filter; search composite includes offering participant + negotiation id; negotiation id also filterable separately; filter on Withdrawn alone with and without seeded rows.
- **Expected:** All six statuses listed and filterable; Withdrawn alone with no rows returns an empty page, not an error; soft-deleted via CoreOwner toggle; server-side paging 50/200; parameterized search.
- **Negative / abuse variants:** Same as negotiations; Withdrawn mapped to Cancelled.

#### TD-ADM-064 — Soft-deleted toggle CoreOwner-only and not audited

- **Traces-to:** AC4, SPEC-S4.5, SPEC-S10, SPEC-S11.1, SPEC-SEC-6, SPEC-SEC-10, STEP-7, STEP-8, PLAN-SEC-8, PLAN-SEC-10, PLAN-SEC-11, STATUS-D
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
- **r5 note:** Editable fields and allowed transitions per redlines B10 / SA §4, enforced server-side — TD-ADM-178; UI TD-ADM-UI-na-B10, -na-expire, -na-B11.
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
- **UI siblings (r3):** TD-ADM-UI-del-01 (copy per entity), TD-ADM-UI-del-02 (keyboard/focus), TD-ADM-UI-del-03 (token expiry / double click) — UX1-C08
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
- **UI siblings (r3):** TD-ADM-UI-del-01, TD-ADM-UI-del-05 (open-negotiation warning + typed confirmation)
- **Layer:** integration
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Open negotiation with N child offers.
- **Steps:** 1) Confirm warns open + child-offer count. 2) Delete. 3) Assert negotiation+offers soft-deleted. 4) Stats open count drops. 5) Cascade audit rows share correlation id.
- **Expected:** Cascade per Spec; open warning; Artifacts/Participants unchanged; audited.
- **Negative / abuse variants:** Artifact auto-deleted.

#### TD-ADM-094 — Cascade: delete Artifact blocked while referenced by non-deleted negotiation

- **Traces-to:** AC7, SPEC-S11.2, SPEC-SEC-10, STEP-10, PLAN-SEC-10
- **UI siblings (r3):** TD-ADM-UI-del-04 (Artifact block state)
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Artifact referenced by non-deleted negotiation.
- **Steps:** 1) Attempt delete Artifact. 2) Expect block listing negotiation ids. 3) Soft-delete referencing negotiations. 4) Retry Artifact delete.
- **Expected:** Blocked while referenced; allowed when no non-deleted negotiation references remain.
- **Negative / abuse variants:** Force wipe with references.

#### TD-ADM-095 — Cascade: delete Participant → negotiations+offers; not auto Artifacts

- **Traces-to:** AC7, SPEC-S11.2, SPEC-S11.3, SPEC-SEC-10, STEP-10, PLAN-SEC-10
- **UI siblings (r3):** TD-ADM-UI-del-01, TD-ADM-UI-del-05 (typed confirmation)
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
- **r5 note:** Step 2 (viewer has no edit/delete control) runs through the audit viewer S-D2 — TD-ADM-UI-na-X02 (UXR-D13).
- **Layer:** API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner; existing audit rows.
- **Steps:** 1) Attempt PATCH/DELETE audit via admin API/UI. 2) Confirm CoreOwner can READ audit. 3) Snapshots FieldPolicy-only; >4KiB truncated + length + hash.
- **Expected:** No update/delete path; read OK; truncation rules; retain indefinitely this slice.
- **Negative / abuse variants:** Audit wipe; secret in snapshot.

#### TD-ADM-102 — Same-txn pairing: audit insert fail rolls back mutation

- **Traces-to:** SPEC-S10, SPEC-SEC-6, STEP-8, PLAN-SEC-8
- **UI siblings (r3):** TD-ADM-UI-del-06 ('Nothing was deleted' + Retry UI)
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
- **UI siblings (r3):** Full UI coverage moved to TD-ADM-UI-* (r3); this stays the end-to-end smoke. Step 2 sign-in detail: TD-ADM-UI-auth-04; delete modal: TD-ADM-UI-del-*; runs in CI per TD-ADM-UI-ci-01
- **Layer:** E2E-UI-smoke
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Playwright against admin.core (local/CI; live only after Ivan deploy OK). Synthetic CoreOwner via env. Never MotorMarket/Arctic Circle.
- **Steps:** 1) Open admin.core. 2) Login password+TOTP+Turnstile. 3) See stats. 4) Open each entity list, search, open detail, edit allowed field, open delete confirm and cancel.
- **Expected:** Happy path smoke green; cancel leaves data unchanged.
- **Negative / abuse variants:** N/A (smoke).

#### TD-ADM-131 — UI smoke: unauthenticated redirect/deny on admin pages

- **Traces-to:** AC1, AC6, SPEC-S8.1, SPEC-SEC-7, STEP-11
- **r5 note:** S-D3 / S-D4 show no entity data and look the same where FieldPolicy hides existence — TD-ADM-179.
- **UI siblings (r3):** TD-ADM-UI-auth-06 (no chrome before full sign-in), TD-ADM-UI-auth-15 (return path)
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
- **r5 note:** UI copy locked in UXR-B09 — TD-ADM-UI-na-B14.
- **UI siblings (r3):** TD-ADM-UI-na-B14 (three empty-state kinds; stub until Part B)
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

### 4.16 Auth screens UI (UX1-A12 + addendum Part A §A.5 + UX1-A16)

Common rules for 4.16–4.19: Playwright headless against local/CI host binding for `admin.core.dealoware.com` (live only after Ivan deploy OK); synthetic CoreOwner and Turnstile test keys by env name only; UI copy and attributes asserted **as cited by UXR ID** in the addendum, or by the final Spec notes where they lock wording (sign-in steps note, password-rules note v2.3, lockout note v5.5, redlines note v2 — Spec wins over the addendum where they differ, OQ13); server-side auth rules stay as locked in §0b (UX1-A03 r2). 'Waits on' points to the numbered §0 Active holds. Evidence under `/workspace/qa/` run folders, no secrets.

#### TD-ADM-UI-auth-01 — Sign-in failure copy parity across five causes (+ wrong recovery code); 401 vs 429; no lock/throttle logic in UI

- **Traces-to:** UX1-A12, UX1-A02, UX1-A03, UXR-A01, UXR-A02, UXR-A06, UXR-A18; API sibling TD-ADM-020, TD-ADM-050, TD-ADM-051; sign-in steps note v1 (`0a3db5f4…`) conditions 4–5
- **Layer:** E2E-UI (Playwright headless) + DOM/source scan
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic CoreOwner via env names; Turnstile test keys by env name; test hooks to put the account in lock and the test IP in throttle (TD-ADM-050/051 harness).
- **Steps:** 1) On S-A1 trigger, one at a time: wrong password, unknown email, wrong TOTP (S-A2), account locked, IP throttle; also wrong recovery code (S-A3). 2) Capture status, response headers, rendered alert text and DOM. 3) Click the 'reset your password' link. 4) Scan built UI bundle/source for countdowns, durations, attempt counters and the word 'locked'.
- **Expected:** Four account causes (+ wrong recovery code) → 401; IP throttle → 429; rendered text identical in all cases and equal to the server body (UXR-A01/A02 r2 copy) in one `role="alert"` region; 'reset your password' opens S-A8; no `Retry-After` on any response; UI source has no duration, countdown, attempt counter or 'locked'. Failure never names which factor failed (UXR-A18). Copy and status are identical on step 1 and step 2 (sign-in steps note condition 4).
- **Negative / abuse variants:** Any per-cause difference in copy, DOM structure or alert placement; client-side lockout/throttle logic; 'locked' anywhere in auth UI.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-02 — Turnstile loading / ready / failed-or-expired / unavailable states on S-A1, S-A8, S-A4

- **Traces-to:** UX1-A12, UX1-A04, UX1-A13, UXR-A03; API sibling TD-ADM-040
- **Layer:** E2E-UI
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Turnstile test keys by env name only; network interception/test double to force failed, expired and not-loaded (>10 s) widget.
- **Steps:** 1) On each of S-A1, S-A8, S-A4 force: loading, ready, failed, expired, unavailable. 2) Try to submit in every non-ready state (click and Enter). 3) Press Retry. 4) After a failed state, submit with a fresh token.
- **Expected:** Each state shows the UXR-A03 text (unavailable adds the network/extensions line); widget sits directly above the primary button; primary button disabled until a fresh token exists — no request leaves the browser without one; Retry re-renders the widget; a Turnstile failure never shows the UXR-A01 sign-in failure text (and, per TD-ADM-040, counts toward nothing).
- **Negative / abuse variants:** Submit fires without token; stale token reused after expiry; provider outage treated as pass.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-03 — Field attributes, paste allowed, Show/Hide toggle, password-manager fill (WCAG 2.2 SC 3.3.8)

- **Traces-to:** UX1-A12, UX1-A10, UXR-A04, UXR-A05
- **Layer:** E2E-UI + DOM assert + manual a11y
- **Priority:** P0
- **Automation:** auto+manual
- **Preconditions:** All S-A screens reachable in test env.
- **Steps:** 1) Assert `type`/`autocomplete`/`inputmode` on email, current-password, new-password, 2FA code and recovery-code fields per UXR-A04. 2) Paste into every field. 3) Toggle Show/Hide; read `aria-pressed`. 4) Fill sign-in via a password-manager fixture (autofill by `autocomplete`). 5) Manual: no cognitive test beyond Turnstile on any screen (UXR-A05).
- **Expected:** Attributes match UXR-A04 exactly (recovery code has no `inputmode` restriction); paste succeeds everywhere; toggle flips type and `aria-pressed`; autofill works; SC 3.3.8 manual check passes.
- **Negative / abuse variants:** `onpaste` blocking; `autocomplete="off"` on credential fields; numeric-only recovery field.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-04 — Keyboard-only sign-in with 2FA: focus order, focus-visible, control states, single request on double submit

- **Traces-to:** UX1-A12, UX1-A09, UX1-A01, UXR-A07, UXR-A08, UXR-A13, UXR-A14, UXR-A16, UXR-A17; sign-in steps note v1 (`0a3db5f4…`) UX impact (S-A01 focus order); addendum `7aa374e9…` UXR-A08 (focus order now matches the Spec)
- **Layer:** E2E-UI (keyboard only) + visual/contrast assert
- **Priority:** P0
- **Automation:** auto+manual
- **Preconditions:** Synthetic CoreOwner via env; TOTP generated from `TOTP_SEED_ENV` in the harness.
- **Steps:** 1) Load S-A1; record initial focus. 2) Tab through step 1 and step 2; record order. 3) Complete S-A1 and S-A2 with keyboard only (Enter/Space). 4) At each control assert focus ring ≥2 px and 3:1 contrast; target ≥24×24 CSS px. 5) Double-press submit while a request is in flight. 6) Fill the full code on S-A2 without pressing Sign in.
- **Expected:** Initial focus on first empty field; S-A1 Tab order email → password → Turnstile → 'Sign in' → 'Forgot your password?'; S-A2 Tab order code field → 'Sign in' → 'Use a recovery code instead' → 'Back to sign in' (UXR-A08, consistent with the sign-in steps note UX impact); S-A1 content per UXR-A13, step 2 per sign-in steps note (6-digit code) and UXR-A16; correct password lands on step 2 with focus in code field; busy state `aria-busy="true"` and exactly one request; step 2 does not auto-submit on full code (UXR-A17); client validation checks only filled + well-formed email (UXR-A14).
- **Negative / abuse variants:** Focus lost/trapped; invisible focus; duplicate requests; auto-submit on paste.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-05 — Error presentation: inline field errors, single alert region, focus to alert, email kept / secrets cleared

- **Traces-to:** UX1-A12, UX1-A02, UX1-A09, UXR-A06, UXR-A14
- **Layer:** E2E-UI + screen-reader announcement check
- **Priority:** P1
- **Automation:** auto+manual
- **Preconditions:** S-A1, S-A4, S-A8, S-A10 reachable.
- **Steps:** 1) Submit empty and malformed fields. 2) Trigger a server failure on S-A1. 3) Read DOM and focus; run a screen reader announcement check (manual or via accessibility tree).
- **Expected:** Field errors inline, linked by `aria-describedby`, `aria-invalid="true"`; server failure in one `role="alert"` region at form top with focus moved there; typed email survives; password and code fields cleared after server failure (except UXR-A22 / A37 cases where new-password fields stay filled).
- **Negative / abuse variants:** Multiple alert regions; email cleared; password retained after sign-in failure.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-06 — Page titles + single h1; no signed-in chrome before full sign-in; built auth routes = Spec X01 inventory

- **Traces-to:** UX1-A12, UX1-A01, UX1-A10, UX1-A19, UXR-A09, UXR-A10, UXR-A12; API sibling TD-ADM-004, TD-ADM-131; Spec UX1 redlines note v2 (`eb63276f…`) X01 (RL-X01)
- **Layer:** E2E-UI + PR route-list review
- **Priority:** P1
- **Automation:** auto+manual
- **Preconditions:** Route config map read from the Spec X01 inventory (S-A1 `/sign-in` … S-A12 `/settings/security`; S-A4, S-A5, S-A10 come from links).
- **Steps:** 1) For each S-A screen read `<title>` and `<h1>`. 2) Signed out and in partial-auth states (password done, 2FA not done; bootstrap done, enrolment not done), open admin URLs; capture every frame (no-flash check). 3) Diff built auth route list against the Spec X01 inventory rows S-A1–S-A12.
- **Expected:** Unique `<title>` '<Screen name> · Dealoware admin' and one matching `<h1>` per screen; no nav, entity data or stats render (even briefly) before password + 2FA/recovery succeed; auth route list equals Spec X01 one-to-one (no extra, none missing).
- **Negative / abuse variants:** Nav flash during redirect; orphan auth route; inventory row with no route.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-07 — Step 2 code entry and recovery-code sign-in (two-step shape locked; recovery link, Back to sign in, normalisation, single use)

- **Traces-to:** UX1-A12, UX1-A06, UX1-A16, UXR-A15, UXR-A16, UXR-A18, UXR-A19, UXR-A20; API sibling TD-ADM-020, TD-ADM-022; sign-in steps note v1 (`0a3db5f4…`) two-step shape + conditions 4, 7
- **Layer:** E2E-UI
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Enrolled CoreOwner with recovery codes captured once into a test-only store (never committed).
- **Steps:** 1) From S-A2 follow 'Use a recovery code instead' and back; follow 'Back to sign in' (Spec and UXR-A16 now agree; it discards the pending token). 2) Sign in with a recovery code typed in mixed case with spaces/dashes. 3) Reuse the same code. 4) Wrong code.
- **Expected:** Links per UXR-A16/A19; normalised code accepted (case-insensitive, spaces/dashes ignored); reuse and wrong code show the UXR-A01 text (no factor named); success lands on return path or Stats with banner (see TD-ADM-UI-auth-16). A used recovery code is spent immediately (TD-ADM-027).
- **Negative / abuse variants:** Recovery code accepted twice; distinct copy for used vs wrong code.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-08 — Bootstrap → set password → authenticator enrolment (QR + key + confirm) → recovery codes → Stats

- **Traces-to:** UX1-A12, UX1-A05, UX1-A06, UX1-A19, UXR-A21, UXR-A23, UXR-A24, UXR-A27, UXR-A28, UXR-A29, UXR-A30, UXR-A31, UXR-A32; API sibling TD-ADM-010, TD-ADM-011, TD-ADM-012, TD-ADM-022; password-rules note v2.3 (`1a7b342c…`); Spec UX1 redlines note v2 (`eb63276f…`) A05 (RL-A05-RC, RL-A05-NOSTORE); API sibling TD-ADM-170, TD-ADM-171, TD-ADM-173
- **Layer:** E2E-UI + network/log scan
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Fresh CoreOwner identity; bootstrap link minted via the mail interface fake; password from env name only.
- **Steps:** 1) Open bootstrap link → S-A4; set password (UXR-A21/A23 mismatch check first). 2) S-A6: assert QR alt text, grouped manual key, Copy button announcement; enter a wrong code, then a valid one. 3) Reload S-A6 after confirm. 4) S-A7: try Continue before ticking the checkbox; Copy all; Download .txt; Continue. 5) Reload and Back after Continue. 6) Scan network error bodies, titles, URLs, audit payload and logs for the secret.
- **Expected:** S-A4 success goes straight to S-A6 with no signed-in chrome; wrong code shows UXR-A28 inline text and keeps QR + key; enrolment saved only after valid confirm; secret never in errors/titles/URLs/audit/logs and not re-shown on reload (UXR-A29); S-A7 Continue disabled until ticked; downloaded file = shown set, file name has no account email; after Continue, reload/Back show Stats, never the codes (UXR-A32); S-A7 shows exactly 10 codes and the code field takes 6 digits (UXR-A30/A31, redlines A05); S-A6 and S-A7 responses carry `Cache-Control: no-store` (TD-ADM-173). S-A4 password acceptance follows password-rules note v2.3 (`1a7b342c…`) as asserted in TD-ADM-UI-auth-10; a rule reject leaves the bootstrap link usable until it expires (rule 9). Code field is 6 digits (sign-in steps note v1 (`0a3db5f4…`)).
- **Negative / abuse variants:** Enrolment committed on wrong code; codes re-fetchable; secret in logs.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-09 — Expired and used bootstrap / reset links render one identical 'link no longer works' page with no form

- **Traces-to:** UX1-A12, UX1-A07, UX1-A19, UXR-A25, UXR-A26; API sibling TD-ADM-010, TD-ADM-031
- **Layer:** E2E-UI + DOM diff
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Mail fake; controllable clock.
- **Steps:** 1) Reset link: open used, then expired (>1h), then random token. 2) Bootstrap link: used, expired (>24h), random. 3) DOM-diff pages within each link type.
- **Expected:** S-A5 per UXR-A25 (reset variant has 'Request a new link' → S-A8; bootstrap variant has out-of-band text and no resend button); expired vs used pages identical within each type; no password form ever rendered.
- **Negative / abuse variants:** Any 'expired' vs 'already used' distinction; password form visible.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-10 — New-password rules UI per password-rules note v2.3: helper text, client length check, named server reason inline, context-word v1.2, fields kept after reject

- **Traces-to:** UX1-A12, UX1-A17, UXR-A21, UXR-A22, UXR-A23; password-rules note v2.3 (`1a7b342c…`) rules 1–5, 7–9 and 'What the UI shows'; API sibling TD-ADM-011, TD-ADM-030
- **Layer:** E2E-UI
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** S-A4 and S-A10 reachable (S-A12 password change once S-A12 is adopted); server blocklist fixture referenced by fixture name; rule function reachable from a unit test; CoreOwner email local part is the context word under test.
- **Steps:** 1) Read helper text before typing. 2) Length: 14 and 15 code points after NFKC; 128 and 129; a value whose length differs before and after NFKC (compatibility characters) — client and server must count the same. 3) Blocklist: a value on the bundled list (fixture name). 4) Context-word v1.2 at unit level on the rule function (short inputs, so the length rule does not mask them): (a) equals a context word, any case, incl. Unicode full case folding; (b) contains `dealoware` or `admin` (≥4 chars); the email local part `io` is under 4 chars so (b) does not apply to it; (c) equals a context word after stripping Unicode categories N, P, S and Z. Required examples: `radio station` passes; `io2026!!!` and `io$$$2026$$$` fail. Run under two process cultures (e.g. tr-TR and en-US) — results must match. 5) End to end: ≥15-code-point padded variants of the failing patterns, built by the harness, show the common/context message. 6) Same as current password (S-A10, S-A12). 7) Mismatch confirm. 8) Observe network egress during submit. 9) After a rule reject, read lockout/IP counters and reuse the same bootstrap/reset link.
- **Expected:** Helper text exactly as in the note; optional counter; no strength meter; client checks length only, on blur and on submit; the inline error MUST name the failed rule with the note's one message per rule (too short, over 128, common or context word, same as current); 128 accepted untruncated, 129 rejected; client and server lengths agree after NFKC; context-word results match v1.2 exactly incl. the three required examples and are culture-independent; both fields stay filled and focus moves to New password; mismatch shows UXR-A23 text; no outbound call during the check (local list only); a rule reject counts toward neither account lockout nor IP throttle and leaves a valid link usable.
- **Negative / abuse variants:** Composition rules, strength meter or locale-dependent folding; silent truncation; generic reason instead of the named rule; outbound breach-API call; rule reject burning the link or counting as a failure.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2); the S-A12 password-change part also waits on Spec confirming the current-password field (Active holds 3)

#### TD-ADM-UI-auth-11 — Email-link reset with authenticator code: S-A8 → S-A9 → S-A10 → S-A1 status; locked account and throttle parity

- **Traces-to:** UX1-A12, UX1-A07, UX1-A18, UXR-A33, UXR-A34, UXR-A35, UXR-A36, UXR-A37; API sibling TD-ADM-030, TD-ADM-031, TD-ADM-050, TD-ADM-051
- **Layer:** E2E-UI
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Mail fake captures link; TOTP from env seed; lock and throttle hooks.
- **Steps:** 1) S-A8: submit CoreOwner email, unknown email, locked account's email. 2) S-A9 content. 3) Open link → S-A10; submit with wrong TOTP, then valid TOTP. 4) While account is locked, complete reset and try sign-in. 5) Throttle S-A8 (429).
- **Expected:** Step 1: same S-A9 page and copy for all three (UXR-A34); S-A9 shows server's neutral reply + 1-hour line + Back link, no resend (UXR-A35); wrong TOTP shows UXR-A01 text and keeps new-password fields filled; success → S-A1 with the UXR-A37 status message that does not say or imply a lock ended; during lock sign-in still shows the generic text (lockout note v5.5); throttled S-A8 shows server text unchanged; Turnstile absence/presence on S-A10 per Spec §8.5 decision.
- **Negative / abuse variants:** Reset reveals or changes lock state on screen; email-OTP field anywhere; resend button.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2); Turnstile-on-S-A10 awaits Spec §8.5 confirmation (UXR-A36)

#### TD-ADM-UI-auth-12 — Email-link reset using a recovery code (switch swaps field; code single-use)

- **Traces-to:** UX1-A12, UX1-A06, UX1-A07, UXR-A36, UXR-A37; API sibling TD-ADM-022, TD-ADM-030
- **Layer:** E2E-UI
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** As TD-ADM-UI-auth-11 plus a spare recovery code from the test-only store.
- **Steps:** 1) On S-A10 use 'Use a recovery code instead'. 2) Submit with the recovery code. 3) Repeat a new reset with the same (now used) code.
- **Expected:** Switch swaps the authenticator field for the recovery field (attributes per UXR-A04); success as TD-ADM-UI-auth-11; reused code shows UXR-A01 text, password not changed.
- **Negative / abuse variants:** Reset with no 2FA value; recovery code reusable.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-13 — All other sessions end after a completed reset; other tab shows S-A1 with expiry text

- **Traces-to:** UX1-A12, UX1-A17, UXR-A37, UXR-A39; API sibling TD-ADM-030, TD-ADM-052; password-rules note v2.3 (`1a7b342c…`) 'On successful reset or change'; UXR-A42 (S-A12 password section)
- **Layer:** E2E-UI (two browser contexts) + API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Two signed-in browser contexts for the CoreOwner.
- **Steps:** 1) Context A and B signed in. 2) In context C complete an email-link reset. 3) In A and B make the next request. 4) Replay A's old cookie at the API. 5) Repeat with an S-A12 password change from context A. 6) Rule-reject first, then succeed with the same link.
- **Expected:** A and B land on S-A1 with the UXR-A39 expiry text; no entity data shown; old cookies rejected by the API (TD-ADM-003); the reset link is burned on success but not by an earlier rule reject; TOTP enrolment unchanged; for an S-A12 password change every session ends, this one included, and context A shows S-A1 with the UXR-A42 success text; the authenticator stays enrolled.
- **Negative / abuse variants:** Any pre-reset session still valid.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2); the S-A12 password-change part also waits on Spec confirming the current-password field (Active holds 3)

#### TD-ADM-UI-auth-14 — Idle warning 2 min before 30-min expiry (Stay signed in / Sign out); Stay signed in renews idle only; expiry message

- **Traces-to:** UX1-A12, UX1-A08, UX1-A20, UXR-A38, UXR-A39; Spec UX1 redlines note v2 (`eb63276f…`) A20 (RL-A20); API sibling TD-ADM-052, TD-ADM-176; absolute cap is TD-ADM-UI-auth-24
- **Layer:** E2E-UI (controllable clock)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Clock control in the browser and server test host.
- **Steps:** 1) Idle 28 min: dialog appears; check focus and announcement. 2) Stay signed in → read the server idle and absolute expiry (test hook). 3) Idle again to expiry with the dialog left open and no request; then act. 4) Unsaved form input on the expired page.
- **Expected:** Dialog per UXR-A38 (title 'Still there?', text, 'Stay signed in' and 'Sign out'), focus on 'Stay signed in', announced; Stay signed in sends one authenticated anti-forgery request that renews only the idle window and leaves the 8-hour cap unchanged (TD-ADM-176); the open dialog alone does not keep the session alive; after expiry the next screen is S-A1 with UXR-A39 expiry text; unsaved input is lost as the Spec states.
- **Negative / abuse variants:** No warning; Stay signed in moves the 8-hour cap; client timer keeps the session; admin data flash after expiry.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-15 — Return path after sign-in: same-origin deep link reopens; off-origin / malformed → Stats

- **Traces-to:** UX1-A12, UX1-A20, UX1-B13, UXR-A11, UXR-A38; API sibling TD-ADM-131; UXR-B18; Spec UX1 redlines note v2 (`eb63276f…`) return path (RL-RETURN); API sibling TD-ADM-177
- **Layer:** E2E-UI + security negative
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Signed-out browser.
- **Steps:** 1) Open a Negotiations view with sort, filters, page, page size and the deleted toggle set, signed out; sign in. 2) Expire the session on that view, sign in again. 3) Crafted return values (full list in TD-ADM-177): absolute external URL, `//`, `/\`, `javascript:`, double-encoded `%2f`, `..`, Cc/Cf characters, 2049 characters, an S-A route, `/setup/…`, `/reset…`.
- **Expected:** Steps 1–2 land on the same page with the same rows, the known list parameters restored and unknown ones dropped; every crafted value lands on Stats; the return value never shows in the sign-in step URLs; no open redirect.
- **Negative / abuse variants:** Open redirect via encoded or protocol-relative values.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-16 — Recovery-code sign-in banner shows server count N and links to S-A12

- **Traces-to:** UX1-A12, UX1-A21, UXR-A20, UXR-A40
- **Layer:** E2E-UI + store read
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Enrolled CoreOwner with known remaining-code count in the test store.
- **Steps:** 1) Sign in with a recovery code. 2) Read banner. 3) Compare N with store. 4) Dismiss; follow link.
- **Expected:** Banner text per UXR-A40 on the first signed-in page; N equals the server count after consumption; dismissible; link opens S-A12.
- **Negative / abuse variants:** Client-computed N; banner on normal TOTP sign-in.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2) (link target S-A12)

#### TD-ADM-UI-auth-17 — Sign out (POST + anti-forgery) from persistent nav ends the session; GET /sign-out does nothing; Back and reload show no entity data

- **Traces-to:** UX1-A12, UX1-A15, UXR-A39, UXR-A41; API sibling TD-ADM-003; Spec UX1 redlines note v2 (`eb63276f…`) A15 (RL-A15); API sibling TD-ADM-173, TD-ADM-175
- **Layer:** E2E-UI + API
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Signed-in CoreOwner on a list and on a detail page.
- **Steps:** 1) Check Sign out present on every signed-in screen and that it submits a POST. 2) Open `/sign-out` by plain navigation (GET). 3) Sign out with the nav control. 4) Press Back; reload; open a cached entity URL. 5) Replay old cookie at the API.
- **Expected:** Step 2 leaves the session alive (no sign-out); step 3 lands on S-A1 with 'You've been signed out.' (UXR-A39); Back/reload show no entity data because admin pages are `Cache-Control: no-store`; old cookie rejected; session row deleted (TD-ADM-175).
- **Negative / abuse variants:** Cached entity HTML shown after sign-out.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-18 — S-A12: re-enrol authenticator and create new recovery codes need step-up (password + code within 5 min); every other session ends; old set stops working

- **Traces-to:** UX1-A12, UX1-A21, UXR-A42; Spec UX1 redlines note v2 (`eb63276f…`) A05 (RL-A05-STEPUP); API sibling TD-ADM-022, TD-ADM-100 (totp_change), TD-ADM-171, TD-ADM-174
- **Layer:** E2E-UI
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Signed-in CoreOwner in context A, plus a second signed-in context B; controllable clock.
- **Steps:** 1) 'Set up again': wrong password + valid code; valid password + wrong code; valid password + valid code → S-A6, S-A7. 2) 'Create new codes' with password + valid recovery code. 3) Pass step-up, wait 5 min 1 s, press a button again. 4) After each success make a request in context B. 5) Try an old recovery code.
- **Expected:** Each failed step-up is refused with no factor named; a step-up older than 5 minutes asks again; success runs S-A6/S-A7 (re-enrol) or S-A7 (new codes); context B lands on S-A1 after each success while A stays signed in; the new set of 10 replaces the old and old codes fail; 'N codes left' matches the store; audit `totp_change`.
- **Negative / abuse variants:** Re-enrol or regenerate with a live session only; other sessions survive; step-up reused after 5 minutes.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2) (S-A12, UX1-A21)

#### TD-ADM-UI-auth-19 — S-A12 Recent sign-in activity panel (empty state, last 20, no raw IP) and 24-hour lock notice

- **Traces-to:** UX1-A12, UX1-A03, UX1-A16, UXR-A43, UXR-A44; lockout note v5.5 (`8a194eb9…`) §'S-A12 Recent sign-in activity'; sign-in steps note v1 (`0a3db5f4…`) condition 6; API sibling TD-ADM-028, TD-ADM-050, TD-ADM-053, TD-ADM-100
- **Layer:** E2E-UI + DOM/a11y assert
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Fresh store (no events); then seeded auth events incl. a lock that ended <24h ago and one >24h ago; controllable clock.
- **Steps:** 1) Open S-A12 with no events. 2) Seed >20 events incl. one `signin.second_factor_failed`; reload. 3) Inspect Network column and user-agent detail. 4) Lock active / ended <24h / ended >24h. 5) Visit every signed-out screen during an active lock.
- **Expected:** Empty state 'No sign-in activity yet.'; table shows last 20 with caption and column headers, times in ET; event types only from the lockout note v5.5 closed list, incl. 'code failed after correct password' for `signin.second_factor_failed` (addendum UXR-A43 says 'Failed second factor (correct password, wrong code)' — the label alone stays OQ13; tests assert the lockout note); Network is a labelled short prefix of the keyed-HMAC value — never a raw IP; browser column, if shown, is a short family label only; lock notice one line at panel top with text-labelled icon, not dismissible, only for active or <24h locks, times from server; no lock notice or lock wording on any signed-out screen.
- **Negative / abuse variants:** Raw IP; colour-only notice; lock notice on S-A1.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-20 — axe-core scan on every S-A screen (S-A1–S-A12, incl. dialog and error states): zero serious or critical

- **Traces-to:** UX1-A12, UX1-A10, UX1-X05, UXR-A05, UXR-A07, UXR-A09
- **Layer:** a11y (axe-core via Playwright, headless)
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** All S-A screens reachable; states seeded (error, Turnstile failed, idle dialog, empty activity panel).
- **Steps:** 1) Run axe-core (WCAG 2.2 A/AA rule tags) on each S-A screen in default and error states, plus the idle dialog and the absolute-cap dialog. 2) Save results per screen under the run folder.
- **Expected:** Zero serious and zero critical violations on every screen and state; results file lists each S-A screen ID.
- **Negative / abuse variants:** Screen skipped from the scan; rule tags narrowed to hide failures.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-21 — Two-step sign-in UI: step 1 email + password + Turnstile; step 2 6-digit code + 'Use a recovery code instead' + 'Back to sign in'; pending token never visible to page or URL

- **Traces-to:** UX1-A16, UX1-A01, UXR-A08, UXR-A10, UXR-A15, UXR-A16; sign-in steps note v1 (`0a3db5f4…`) two-step shape, conditions 1, 7 and UX impact; API sibling TD-ADM-023, TD-ADM-027
- **Layer:** E2E-UI (keyboard + storage inspection)
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Synthetic CoreOwner via env names; Turnstile test keys by env name; TOTP from `TOTP_SEED_ENV`.
- **Steps:** 1) Step 1: assert fields and Tab order email → password → Turnstile → submit. 2) Correct step 1: assert step 2 shows one 6-digit code field focused, then 'Sign in', then 'Use a recovery code instead', then 'Back to sign in' in Tab order (UXR-A08). 3) On step 2 inspect URL, history entries, Referer of outgoing requests, localStorage, sessionStorage, IndexedDB, DOM and `document.cookie` for the pending token. 4) Press 'Back to sign in'; then try browser Back to step 2. 5) Complete step 2 with a valid code.
- **Expected:** Layout and order match the sign-in steps note; code field per UXR-A04 (`one-time-code`, numeric); no signed-in chrome on step 2 (UXR-A10); pending token absent from URL, history, Referer, web storage and DOM, and unreadable by script (HttpOnly); 'Back to sign in' returns to step 1 and a new step 1 (with new Turnstile) is needed; session only after the valid code.
- **Negative / abuse variants:** Token in query string or fragment; step 2 reachable by Back after leaving; nav or data on step 2.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-22 — Same generic sentence and no hints on both steps: wrong password / unknown email / locked with correct password at step 1; wrong TOTP / recovery at step 2; IP throttle on both steps

- **Traces-to:** UX1-A16, UX1-A02, UX1-A03, UXR-A01, UXR-A18; sign-in steps note v1 (`0a3db5f4…`) conditions 4, 5 and C1; lockout note v5.5 (`8a194eb9…`); API sibling TD-ADM-024, TD-ADM-026
- **Layer:** E2E-UI + response capture
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Lock and throttle hooks from TD-ADM-050/051 harness.
- **Steps:** 1) Step 1: wrong password, unknown email, locked account with the correct password. 2) Step 2: wrong TOTP, wrong recovery code (attempts 1–4 on one token). 3) IP throttle active: submit step 1 and step 2. 4) Capture rendered text, status, headers, DOM.
- **Expected:** Every case renders the UX1-A03 r2 sentence unchanged; account failures 401; throttle 429 on both steps; locked account with correct password stays on step 1 and never sees step 2; while the token is still valid a wrong code stays on S-A2 with the same sentence, the code field cleared and focus kept on it (UXR-A18); no `Retry-After`, duration, attempt count or 'locked' on either step; nothing names which factor failed.
- **Negative / abuse variants:** Step-specific wording; step 2 shown during a lock; attempt counter on step 2.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-23 — Expired pending token or exhausted code attempts on step 2 → same generic sentence, back to step 1 with a new Turnstile

- **Traces-to:** UX1-A16, UXR-A03, UXR-A18; sign-in steps note v1 (`0a3db5f4…`) conditions 1, 3 and UX impact; API sibling TD-ADM-023, TD-ADM-025
- **Layer:** E2E-UI (controllable clock)
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Clock control; test-only raised account threshold in the local test host to isolate the cap (never in deployed config); default config run as well.
- **Steps:** 1) Reach step 2; advance clock past 5 min; submit a valid code. 2) Raised-threshold host: 5 wrong codes on one token, then a valid code. 3) Default config: 5 wrong codes. 4) In each case check the Turnstile widget state on the returned step 1.
- **Expected:** Steps 1–2: the same generic sentence, user returned to S-A1 with email kept and password empty (UXR-A18), Turnstile re-rendered and submit disabled until a fresh token exists (no reuse of the old Turnstile result); valid code after expiry or cap does not sign in. Step 3: 5th wrong code also locks the account via the shared counter (lockout note v5.5); the user sees only the generic sentence.
- **Negative / abuse variants:** Valid code accepted after 5 min or after 5 attempts; old Turnstile result reused.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

#### TD-ADM-UI-auth-24 — Absolute-cap warning 2 min before 8 h: one OK, cannot extend, idle dialog suppressed, expiry lands on S-A1

- **Traces-to:** UX1-A12, UX1-A20, UXR-A45, UXR-A39; Spec UX1 redlines note v2 (`eb63276f…`) A20 (RL-A20); addendum §A.5 (A45 case); API sibling TD-ADM-052, TD-ADM-176
- **Layer:** E2E-UI (controllable clock) + network assert
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Clock control in the browser and the server test host; signed-in CoreOwner with an unsaved edit form open.
- **Steps:** 1) Advance to 7h58m since sign-in with recent activity: read the dialog. 2) Check role, announcement and focus; press Escape; reopen at the next tick if shown; press OK. 3) Record every request while the dialog is open and after OK. 4) Arrange idle and absolute expiry inside the same 2 minutes. 5) Advance to 8h and make a request.
- **Expected:** Dialog titled 'Your session is ending' with the UXR-A45 text and one button 'OK'; `role="alertdialog"`, announced; Escape acts as OK; OK returns focus to the element that had it; no request extends the session; when both warnings fall in the same 2 minutes only this dialog shows; at 8h the next request lands on S-A1 with the UXR-A39 expiry text and needs password + code; the unsaved input is lost.
- **Negative / abuse variants:** 'Stay signed in' or any extend control; idle dialog stacked over it; session alive after 8h.
- **Waits on:** Auth-screen UI PRs (UX1 approved, lanes released); SKIP until the screens land (Active holds 2)

### 4.17 Delete UI (UX1-C08, addendum Part C)

Each case is a UI-layer sibling of an existing API/integration case (§3.10). Copy is asserted as locked in addendum Part C (UXR-C01…C07); the undo statement follows Product's C01 interim decision ('This can't be restored from the admin.').

#### TD-ADM-UI-del-01 — Delete modal copy per entity (Participant, Artifact, Negotiation, Offer): name, id, cascade counts, undo statement, named destructive button

- **Traces-to:** UX1-C08, UXR-C01, UXR-C02, UXR-C08, UX1-C01, UX1-C02; UI sibling of TD-ADM-090, TD-ADM-092, TD-ADM-093, TD-ADM-095
- **Layer:** E2E-UI
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Synthetic entity of each type with known cascade counts.
- **Steps:** 1) Open delete for each type. 2) Compare title, id, cascade list and undo statement with delete-intent response and the locked copy.
- **Expected:** Undo statement 'This can't be restored from the admin.' (UXR-C01, Product C01 interim); title 'Delete {type} {display name}?'; ID shown; cascade counts as a list equal to delete-intent; destructive button names the action (for example 'Delete negotiation'); other button Cancel (UXR-C02).
- **Negative / abuse variants:** Generic 'Are you sure?'; counts differ from server.
- **Waits on:** Step 10 UI PR (delete); merge also needs Condition 3 (Active holds 1–2)

#### TD-ADM-UI-del-02 — Delete modal keyboard and focus: start on Cancel, trap, Escape/backdrop cancel, focus return, dialog semantics; cancel writes no audit

- **Traces-to:** UX1-C08, UXR-C03, UXR-C08, UX1-C03; UI sibling of TD-ADM-090 (step 4)
- **Layer:** E2E-UI (keyboard only) + audit read
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** CoreOwner; entity to delete.
- **Steps:** 1) Keyboard-open the modal. 2) Tab/Shift-Tab past both ends. 3) Escape; backdrop click; reopen and confirm by keyboard. 4) Read audit after each cancel.
- **Expected:** Focus starts on Cancel; Tab trapped; Escape and backdrop cancel; focus returns to the Delete trigger; `role="dialog"`, `aria-modal`, labelled title; cancels write no delete audit row (Spec §9). (UXR-C03)
- **Negative / abuse variants:** Focus escapes the modal; cancel writes audit.
- **Waits on:** Step 10 UI PR (delete); merge also needs Condition 3 (Active holds 1–2)

#### TD-ADM-UI-del-03 — Confirm token expiry while modal open (>5 min) and double-click Confirm

- **Traces-to:** UX1-C08, UXR-C04, UXR-C08, UX1-C04; UI sibling of TD-ADM-090 (expired / replay variants)
- **Layer:** E2E-UI (controllable clock) + network assert
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Clock control; entity with cascade.
- **Steps:** 1) Open modal; change cascade counts server-side; advance 5 min; Confirm. 2) Fresh modal; double-click Confirm.
- **Expected:** Confirm shows 'Deleting…' and is disabled while sending; double-click sends exactly one request; an expired token shows 'This confirmation expired. Review the details again.' and re-fetches delete-intent with fresh counts; nothing deleted on the expired attempt (UXR-C04).
- **Negative / abuse variants:** Expired token accepted; two DELETE requests.
- **Waits on:** Step 10 UI PR (delete); merge also needs Condition 3 (Active holds 1–2)

#### TD-ADM-UI-del-04 — Artifact referenced by non-deleted negotiations: block state lists linked ids, Close only, no Delete button

- **Traces-to:** UX1-C08, UXR-C05, UXR-C08, UX1-C05; UI sibling of TD-ADM-094
- **Layer:** E2E-UI
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Artifact referenced by ≥2 non-deleted negotiations.
- **Steps:** 1) Open delete on the Artifact. 2) Follow each negotiation link. 3) Soft-delete the references; reopen.
- **Expected:** Title 'This artifact can't be deleted'; body 'It is used by these negotiations:' with each negotiation ID as a link that opens it; Close is the only button; no Delete control in the DOM; after the references are soft-deleted the normal modal appears (UXR-C05).
- **Negative / abuse variants:** Delete button hidden by CSS only; missing ids.
- **Waits on:** Step 10 UI PR (delete); merge also needs Condition 3 (Active holds 1–2)

#### TD-ADM-UI-del-05 — Typed confirmation for Participant delete with cascade and for an open negotiation

- **Traces-to:** UX1-C08, UXR-C06, UXR-C08, UX1-C06; UI sibling of TD-ADM-093, TD-ADM-095
- **Layer:** E2E-UI
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Participant with cascades incl. an open negotiation; standalone open negotiation with child offers.
- **Steps:** 1) Open delete; try Confirm with empty, partial, wrong-case and exact text. 2) Read open warning.
- **Expected:** Labelled field 'Type {value} to confirm' (participant name or negotiation ID); destructive button disabled until the typed text matches exactly (case and whitespace included); paste allowed; an open negotiation is named as open with its child-offer count (UXR-C06).
- **Negative / abuse variants:** Confirm enabled by whitespace or partial match.
- **Waits on:** Step 10 UI PR (delete); merge also needs Condition 3 (Active holds 1–2)

#### TD-ADM-UI-del-06 — Post-delete routing (list, live-region message, row gone / Deleted badge with toggle) and failed delete ('Nothing was deleted' + Retry)

- **Traces-to:** UX1-C08, UXR-C07, UXR-B08, UXR-C08, UX1-C07, UX1-B08; UI sibling of TD-ADM-064, TD-ADM-091, TD-ADM-102
- **Layer:** E2E-UI + audit-failure test double
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Test double that fails the audit insert (TD-ADM-102).
- **Steps:** 1) Delete each entity type. 2) Turn the soft-deleted toggle on. 3) Force audit insert failure; delete; Retry after clearing the fault.
- **Expected:** Success closes the modal, returns to the list and announces '{Type} {name} deleted.' in a live region; row gone from the default view and shown with the Deleted badge beside its status when 'Show deleted' is on; forced audit failure keeps the modal open with 'Nothing was deleted.' and Retry; Retry succeeds once the fault clears (UXR-C07).
- **Negative / abuse variants:** Row removed client-side when the server rolled back.
- **Waits on:** Step 10 UI PR (delete); merge also needs Condition 3 (Active holds 1–2)

### 4.18 Non-auth UI (UX1-X07, addendum Parts B and D)

Filled in r5 for UX1 approval condition 3. Case IDs keep the UX1 findings row numbers from r3 (for example TD-ADM-UI-na-B14 = UX1-B14 = UXR-B09); each title names its UXR rows and §3.13 maps every UXR row to its cases. UXR-B14 is covered by TD-ADM-UI-na-B10 (edit fields) and TD-ADM-UI-na-expire (Expire dialog); UXR-B15 by TD-ADM-UI-na-B11. Routes are the Spec X01 inventory names. Cases stay SKIP until their screens land, but every step and expected result is final. Each screen is also scanned in TD-ADM-UI-na-axe.

#### TD-ADM-UI-na-B01 — UX1-B01 → UXR-B01: each list shows the locked columns in order and opens on the locked default sort

- **Traces-to:** UX1-X07, UX1-B01, UXR-B01, RL-B01, STATUS-D, UXR-D14; API / UI sibling TD-ADM-060–063
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Seeded rows on all four lists with different Created and Updated times; a soft-deleted row per list.
- **Steps:** 1) Open S-B2–S-B5; read the header cells in order. 2) Read the default sort parameter, the order of rows and the sort indicator; run once on a build without `UpdatedAt` (before the Step 7 PR) and once with it. 3) Turn 'Show deleted' on; read a deleted row's status cell.
- **Expected:** Participants: Name, ID, Status, Created, Updated. Artifacts: Name, Owner participant, Created, Updated. Negotiations: ID, Artifact, Participants, Status, Created, Updated. Offers: ID, Negotiation ID, Artifact, Offering participant, Amount with currency, Status, Created, Updated. Default sort Updated newest first; before `UpdatedAt` ships, Created newest first with the indicator on Created. A deleted row shows its status badge and the Deleted badge side by side (never in place of it).
- **Negative / abuse variants:** Extra or reordered columns; Deleted badge replacing the status; client-side default sort.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-B02 — UX1-B02 → UXR-B02, UXR-B03: sortable headers (server-side, `aria-sort`) and Participants / Artifacts filters

- **Traces-to:** UX1-X07, UX1-B02, UXR-B02, UXR-B03, UXR-D14; API / UI sibling TD-ADM-060, TD-ADM-061
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Seeded Participants (Active and Suspended) and Artifacts with several owners.
- **Steps:** 1) On each list, activate every header (mouse and keyboard); watch the network. 2) Read the header markup. 3) Participants: filter status Active, then Suspended, then both. 4) Artifacts: filter by owner participant with the typeahead.
- **Expected:** Sortable: Name, Created, Updated on Participants and Artifacts; Created, Updated, Status on Negotiations; Created, Updated, Amount, Status on Offers; other headers are not buttons. Each click sends one server query and the order changes; header is a button inside `th scope="col"` with direction text for screen readers; `aria-sort` sits on the sorted column only and moves with it. Status and owner filters change the server query.
- **Negative / abuse variants:** Client-side sort of one page; `aria-sort` on several columns; sort on a non-sortable column.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-B03 — UX1-B03 → UXR-B03, UXR-B07: status options match the status-list note exactly; text badges; Deleted never a status; Withdrawn offer-only

- **Traces-to:** UX1-X07, UX1-B03, UXR-B03, UXR-B07, STATUS-N, STATUS-O, STATUS-D, PROD-W, UXR-D14; API / UI sibling TD-ADM-062, TD-ADM-063
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Seeded rows in every status per status-list note (`0a5ff57b…`): negotiations Open, Closed, Expired; offers Open, Accepted, Declined, Superseded, Cancelled (Withdrawn only if seeded directly in the test store); participants Active, Suspended.
- **Steps:** 1) Open the Status multi-select on S-B4, S-B5 and S-B2; read every option. 2) Filter Offers by Withdrawn alone with no Withdrawn rows. 3) Grayscale screenshot of each list; measure badge text contrast. 4) Read the Superseded badge.
- **Expected:** Negotiations: Open, Closed, Expired only — no Withdrawn, Accepted or Declined. Offers: Open, Accepted, Declined, Withdrawn, Superseded (countered), Cancelled. Participants: Active, Suspended. 'Deleted' is never an option. Withdrawn alone shows the UXR-B09 filter-miss state ('No results match these filters.' + Clear filters), not an error. Every status is a text badge readable in grayscale with ≥4.5:1 text contrast; Superseded reads 'Superseded (countered)'.
- **Negative / abuse variants:** Withdrawn offered on Negotiations; Deleted as a status option; colour-only badges; error on an empty Withdrawn filter.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2); Withdrawn rows come from Step 7 under its own Dev Code QA, and nothing writes Withdrawn yet (Active holds 4)

#### TD-ADM-UI-na-B04 — UX1-B04 → UXR-B02: no amount or price sort or filter on Negotiations; Offers keep Amount

- **Traces-to:** UX1-X07, UX1-B04, UXR-B02, UXR-D14; API / UI sibling TD-ADM-062
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P2
- **Automation:** auto
- **Preconditions:** Seeded negotiations and offers with amounts.
- **Steps:** 1) On S-B4 look for any amount or price header button, filter or chip. 2) On S-B5 sort by Amount and filter by Amount min/max. 3) Add an amount sort to the S-B4 URL by hand (parameter name from the Offers query schema) and reload.
- **Expected:** S-B4 has no amount or price control anywhere; S-B5 Amount sort and min/max filter change the server query; the hand-made S-B4 parameter is not applied (rows keep the default order) and no error page shows.
- **Negative / abuse variants:** Price control on Negotiations; hand-made parameter reorders rows.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-B05 — UX1-B05 → UXR-B03: filter control types, server-backed typeahead pickers, chips and Clear all

- **Traces-to:** UX1-X07, UX1-B05, UXR-B03, UXR-D14; API / UI sibling TD-ADM-062, TD-ADM-063, TD-ADM-065
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** More than one page (200+) of Participants and Artifacts so a picker could over-fetch.
- **Steps:** 1) Set Created and Updated date ranges. 2) Type into Owner participant, Participant and Artifact pickers; watch the network. 3) Set Amount min and max on Offers. 4) Enter an exact Negotiation ID on Offers. 5) Remove one chip; then 'Clear all filters'.
- **Expected:** Each control changes the server query; pickers send the typed text and list at most one page of matches (no full-list preload); amount is a min/max pair; Negotiation ID is an exact match; active filters show as removable chips above the table; removing a chip drops only that filter; 'Clear all filters' resets every filter.
- **Negative / abuse variants:** Picker downloads the whole table; partial-match on Negotiation ID; chips that don't change the query.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-B06 — UX1-B06 → UXR-B05, UXR-B06: paging controls, result count, reset to page 1, clamp, URL state restore

- **Traces-to:** UX1-X07, UX1-B06, UXR-B05, UXR-B06, UXR-D14; API / UI sibling TD-ADM-065
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** 1,234+ seeded offers.
- **Steps:** 1) Read page-size options; choose 100; go to page 2. 2) Read the footer and page-control names. 3) On page 4 change a filter, a sort, then the search. 4) Open a page number beyond the last by URL. 5) Set page size 500 by URL. 6) Reload; press Back; paste the URL into a new tab; open a row (S-B6) and return.
- **Expected:** Options 50 (default), 100, 200 only; footer 'Showing 101–200 of 1,234'-style range with Previous, Next and 'Page N' accessible names; any filter, sort or search change returns to page 1; an out-of-range page shows the last page; the UI never sends a limit above 200 and the selector shows the clamped value; reload, Back, a copied link and return from S-B6 restore the same search, sort, filters, page, size and toggle.
- **Negative / abuse variants:** Limit >200 sent; page kept after a filter change; state lost on return from detail.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-B07 — UX1-B07 → UXR-B04: search label, Enter or 300 ms pause, Clear, 'N results' announcement

- **Traces-to:** UX1-X07, UX1-B07, UXR-B04, UXR-D14; API / UI sibling TD-ADM-060–063, TD-ADM-066
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Seeded names that match and miss; screen reader or live-region recorder.
- **Steps:** 1) Read the search label on each list. 2) Type quickly a 6-character term and pause; count requests. 3) Press Enter. 4) Press Clear.
- **Expected:** Visible label names what it matches (for example 'Search by artifact or participant name' on Negotiations); a burst of typing sends one request after a 300 ms pause, Enter sends at once; search runs server-side; a polite live region announces 'N results'; Clear restores the unfiltered list.
- **Negative / abuse variants:** One request per keystroke; no announcement; placeholder-only label.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-B08 — UX1-B08 → UXR-B08: 'Show deleted' toggle, Deleted badge plus deleted time, read-only deleted detail

- **Traces-to:** UX1-X07, UX1-B08, UXR-B08, UXR-B01, STATUS-D, UXR-D14; API / UI sibling TD-ADM-064
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** One soft-deleted record per type.
- **Steps:** 1) Check the toggle on all four lists (default state). 2) Turn it on; reload. 3) Read a deleted row. 4) Open the deleted record's detail. 5) Send a direct edit and delete request for that record.
- **Expected:** Toggle present on all four lists, off by default, state in the URL and kept on reload; deleted rows show a text 'Deleted' badge beside the status badge plus the deleted time in ET; deleted detail shows no Edit or Delete control; direct writes are refused with the generic deny (TD-ADM-178).
- **Negative / abuse variants:** Toggle missing on a list; Edit control on a deleted record; badge colour-only.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-B09 — UX1-B09 → UXR-B12, UXR-B13: stats tiles (label, count, 'As of'), pre-filtered links, Refresh, per-tile loading and error

- **Traces-to:** UX1-X07, UX1-B09, UXR-B12, UXR-B13, UXR-D14; API / UI sibling TD-ADM-070
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Known counts incl. soft-deleted rows; fault hook that fails one count.
- **Steps:** 1) Open S-B1; read each tile. 2) Follow each tile link; read the list total. 3) Change data; press Refresh. 4) Throttle the network. 5) Fail one count.
- **Expected:** One tile per Spec §6 count with label, count and 'As of {time} ET'; each link opens its list pre-filtered (open negotiations → Negotiations status Open; accepts → Offers status Accepted) and the list total equals the tile count; Refresh reloads all tiles; each tile shows its own `aria-busy` loading; a failed count shows 'Couldn't load' with Retry on that tile only while the others stay intact. The Offers tile follows the Spec once it states which statuses it covers (OQ14).
- **Negative / abuse variants:** Whole page blank on one failure; tile total differs from list total.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-B10 — UX1-B10 → UXR-B14: only the SA §4 fields render as inputs; allowed status changes only; required marked with text

- **Traces-to:** UX1-X07, UX1-B10, UXR-B14, RL-B10, PROD-W, UXR-D14; API / UI sibling TD-ADM-080, TD-ADM-005, TD-ADM-178
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** One record per type in each status; FieldPolicy fixtures.
- **Steps:** 1) Open edit on a Participant, Artifact, Negotiation and Offer; list every input and read-only field. 2) Check types and limits: Display name 256; Artifact Name 4096 (required) and Description 4096; Owner typeahead; Terms 2000; Amount (≥0, 2 places) with Currency (3 letters). 3) Status choices on an Open negotiation, a Closed one, an Open offer and an Accepted offer. 4) Ends at on an Open and a Closed negotiation; set it before Starts at. 5) Clear both Amount and Terms. 6) Close an Open negotiation; read the confirm text. 7) Reactivate a Suspended participant.
- **Expected:** Inputs only for: Participant Display name, Status (Active/Suspended); Artifact Name, Description, Owner participant (existing non-deleted only); Negotiation Status, Ends at; Offer Amount + Currency, Terms, Status. Everything else read-only. Negotiation Status offers only Closed or Expired from Open; Closed and Expired show Status read-only. Ends at editable only while Open and must be after Starts at. Offer Amount and Terms read-only when not Open; Offer Status offers Cancelled only, from Open — never Withdrawn, Accepted, Declined or Superseded. At least one of Amount or Terms required; Currency required with Amount. Required fields marked with text. The close confirm says open offers become Cancelled. Suspended → Active works.
- **Negative / abuse variants:** Withdrawn offered to the admin; read-only field rendered as input; status choice hidden by CSS only.
- **Waits on:** Step 9 UI PR (edit paths, Expire) under its own Dev Code QA; merge also needs Condition 3 (Active holds 1, 2, 4)

#### TD-ADM-UI-na-expire — UXR-B14 Expire negotiation dialog: server open-offer count, focus on Cancel, Escape changes nothing, 'Expiring…' single submit, success cancels every open offer, failure 'Nothing was changed.' + Retry

- **Traces-to:** UX1-X07, UX1-B10, UXR-B14, UXR-C03, UXR-C04, UXR-C07, UXR-D14, RL-B10, STATUS-N; Product decision (`6ea05ddb…`) (admin Expire, Step 9); API sibling TD-ADM-178, TD-ADM-093
- **Layer:** E2E-UI (keyboard) + API read + fault double
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Open negotiation N1 with 3 open offers and 1 accepted offer; Open negotiation N2 with no open offers; Closed negotiation N3; audit read; fault hook that fails the expire transaction.
- **Steps:** 1) On N1 detail press 'Expire negotiation' by keyboard; read the dialog. 2) Check role, label, initial focus; Tab past both ends; press Escape; read N1, its offers and audit. 3) Reopen; backdrop click. 4) Reopen; double-click 'Expire negotiation'; read requests, the live region and N1's offers. 5) On N2 open the dialog. 6) On N3 look for the action. 7) With the fault hook on, expire N2; then clear it and press Retry.
- **Expected:** Title 'Expire negotiation {ID}?'; body '3 open offers will be cancelled.' with the count from the server (N2: 'No open offers will be cancelled.'); buttons 'Expire negotiation' and Cancel; `role="dialog"`, `aria-modal="true"`, focus starts on Cancel and stays inside; Escape and backdrop close it with nothing changed and no audit row; the confirm button shows 'Expiring…', is disabled while sending and sends one request; success announces 'Negotiation {ID} expired.' in a live region, N1 is Expired (final) and all 3 open offers are Cancelled while the accepted one is unchanged; focus returns to a sensible target; no Expire action on N3; a failure keeps the dialog open with 'Nothing was changed.' and Retry, and N2 stays Open until Retry succeeds.
- **Negative / abuse variants:** Client-computed count; two expire requests; offers left Open after success; partial change on failure; Expire offered on a Closed or Expired negotiation.
- **Waits on:** Step 9 UI PR (edit paths, Expire) under its own Dev Code QA; merge also needs Condition 3 (Active holds 1, 2, 4)

#### TD-ADM-UI-na-B11 — UX1-B11 → UXR-B15, UXR-B16: Save / Cancel, 'Saving…', single request, 'Discard your changes?' guard, 'Changes saved.', inline + summary errors

- **Traces-to:** UX1-X07, UX1-B11, UXR-B15, UXR-B16, UXR-D14; API / UI sibling TD-ADM-080, TD-ADM-141
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Editable record of each type; network recorder; live-region recorder.
- **Steps:** 1) Open edit: read Save state before any change. 2) Change a field; double-click Save. 3) Change a field; press Cancel. 4) Change a field; leave by nav link, browser Back and a tile link. 5) Submit with two invalid fields. 6) Submit a value the server rejects.
- **Expected:** Save disabled until a field changes and while saving, showing 'Saving…'; a double-click sends one request; Cancel returns to the detail view; leaving with edits asks 'Discard your changes?' with 'Discard' and 'Keep editing' (Keep editing keeps the values); success announces 'Changes saved.' in a live region; errors show inline under each field and in a top summary that links to each field, and focus moves to the summary on submit; rejected or denied values are never echoed in error text.
- **Negative / abuse variants:** Two PATCH requests; silent loss of edits; focus left on the button after errors; echoed value.
- **Waits on:** Step 9 UI PR (edit paths, Expire) under its own Dev Code QA; merge also needs Condition 3 (Active holds 1, 2, 4)

#### TD-ADM-UI-na-B12 — UX1-B12 → UXR-B17: 409 keeps typed values, shows the conflict copy, Reload fetches the new version

- **Traces-to:** UX1-X07, UX1-B12, UXR-B17, UXR-D14; API / UI sibling TD-ADM-081
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Two contexts editing the same record.
- **Steps:** 1) Context A opens edit; context B saves a change. 2) Context A edits two fields and saves. 3) Press Reload.
- **Expected:** A sees 'This record changed since you opened it. Reload to see the latest version, then reapply your changes.' with a Reload button and its typed values intact; Reload fetches the new version and shows A's typed values alongside for reference until A leaves.
- **Negative / abuse variants:** Typed values lost; silent overwrite.
- **Waits on:** Step 9 UI PR (edit paths, Expire) under its own Dev Code QA; merge also needs Condition 3 (Active holds 1, 2, 4)

#### TD-ADM-UI-na-B13 — UX1-B13 → UXR-B18: mid-session 401 goes to S-A1 and back to the same view; 403 shows S-D4 or inline 'You can't do that.' with no data

- **Traces-to:** UX1-X07, UX1-B13, UXR-B18, UXR-D03, UXR-D14; API / UI sibling TD-ADM-003, TD-ADM-131, TD-ADM-177, TD-ADM-179, TD-ADM-UI-auth-15
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Filtered list view; session expiry hook; a FieldPolicy-denied action.
- **Steps:** 1) Expire the session server-side; click a list page link. 2) Sign in. 3) Expire the session during an edit save. 4) Trigger a 403 (denied field class or action).
- **Expected:** Steps 1–2: S-A1, then the same list view with its state restored (TD-ADM-177). Step 3: S-A1, then the same record (the unsaved edit is lost as the Spec states). Step 4: S-D4 'You can't do that.' with a Stats link, or the same text inline, with no entity data.
- **Negative / abuse variants:** Data shown after 401; 403 page that leaks field values.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-B14 — UX1-B14 → UXR-B09: empty table vs filter miss vs zero stat

- **Traces-to:** UX1-X07, UX1-B14, UXR-B09, UXR-D14; API / UI sibling TD-ADM-140
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P2
- **Automation:** auto
- **Preconditions:** Empty store for one type; non-empty for another.
- **Steps:** 1) Open an empty list. 2) Search or filter with no match. 3) Open Stats with a zero count.
- **Expected:** Empty table: 'No {type} yet.'; miss: 'No results match these filters.' with a Clear filters button that resets them; zero stat shows 0, never blank.
- **Negative / abuse variants:** One shared message for both; blank tile.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-B15 — UX1-B15 → UXR-B11: every timestamp in ET with a zone label; UTC in a tooltip that keyboard focus also shows

- **Traces-to:** UX1-X07, UX1-B15, UXR-B11, UXR-D14; API / UI sibling TD-ADM-101
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P2
- **Automation:** auto
- **Preconditions:** Rows with timestamps in summer (EDT) and winter (EST).
- **Steps:** 1) Read every timestamp on lists, detail, stats 'As of' and audit. 2) Hover and Tab to a timestamp. 3) Compare with the stored UTC value.
- **Expected:** Every timestamp shows America/New_York time with the 'ET' label (for example 'Oct 5, 2026, 7:55 PM ET'), correct across EDT and EST; the UTC value shows in a tooltip on hover and on keyboard focus.
- **Negative / abuse variants:** Unlabelled or browser-local times; tooltip mouse-only.
- **Waits on:** Step 7 UI PR (lists, stats); merge also needs Condition 3 = this fill after SPQA PASS (Active holds 1–2)

#### TD-ADM-UI-na-X01 — UX1-X01 → UXR-D01, UXR-D03: built routes match the Spec X01 inventory both ways; S-D3 and S-D4 pages

- **Traces-to:** UX1-X07, UX1-X01, UXR-D01, UXR-D03, RL-X01, RL-SD, UXR-D14; API / UI sibling TD-ADM-001, TD-ADM-179, TD-ADM-UI-auth-06
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Route list exported from the built app; Spec X01 table.
- **Steps:** 1) Diff built routes against Spec X01 (S-A1…S-A12, S-B1…S-B6, S-C1, Toggle, S-D1…S-D4). 2) Signed in: open an unknown route, an unknown ID per type and a soft-deleted ID with the toggle off. 3) Signed out: open an unknown route. 4) Trigger a 403.
- **Expected:** Every built route maps to one Spec row and every row to one route; S-D3 'We can't find that page.' with a link to Stats for unknown routes and for missing or deleted IDs (signed out: S-A1 per Spec X01); S-D4 'You can't do that.' with a Stats link; neither shows entity data (TD-ADM-179). Deleted-ID handling with the toggle on is OQ19.
- **Negative / abuse variants:** Orphan route; S-D3 page showing a record name.
- **Waits on:** Each screen's UI PR as it lands (Active holds 2)

#### TD-ADM-UI-na-X02 — UX1-X02 → UXR-D12, UXR-D13: audit log S-D1 columns, filters, paging, URL; S-D2 before/after allowed fields only; no mutating control

- **Traces-to:** UX1-X07, UX1-X02, UXR-D12, UXR-D13, UXR-D14; API / UI sibling TD-ADM-101 (step 2)
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Seeded audit rows (edits, deletes, auth events) incl. a FieldPolicy-denied field change; one entity since deleted.
- **Steps:** 1) Open S-D1: read columns and default order. 2) Apply action (multi), entity type and date range filters; page; reload; Back. 3) Look for a search box. 4) Open an edit entry (S-D2); read before/after. 5) Search the whole viewer DOM for edit or delete controls. 6) Follow entity ID links for an existing and a deleted entity. 7) Run TD-ADM-101 step 2 here.
- **Expected:** Columns Time (ET), Actor, Action, Entity type, Entity ID, Outcome; default Time newest first; filters change the server query; paging 50 default / 200 max with UXR-B05/B06 rules; reload restores the view; no search box until the Spec decides (OQ15); S-D2 shows before and after for FieldPolicy-allowed fields only with text 'Changed' markers; the denied field never appears; no edit or delete control anywhere; the existing entity ID links to its record and the deleted one does not open a record.
- **Negative / abuse variants:** Mutating control; denied field shown; colour-only change marker.
- **Waits on:** Step-15 audit viewer Story under its own UI/UX gate; merge also needs Condition 3 (Active holds 1, 2, 5)

#### TD-ADM-UI-na-X03 — UX1-X03 → UXR-D02: land on Stats or return path; persistent nav; `aria-current`; skip link first; page titles

- **Traces-to:** UX1-X07, UX1-X03, UXR-D02, UXR-D14; API / UI sibling TD-ADM-130
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto
- **Preconditions:** Signed-in CoreOwner.
- **Steps:** 1) Sign in with and without a return path. 2) Read nav items on every screen. 3) Press Tab once on each screen; activate the skip link. 4) Read `<title>` per screen. 5) Count steps from nav to each inventory screen.
- **Expected:** Lands on Stats or the return path; nav lists Stats, Participants, Artifacts, Negotiations, Offers, Audit log, Security settings and Sign out with `aria-current="page"` on the current item; 'Skip to main content' is the first focusable element and moves focus to main; title '{Screen} · Dealoware admin'; every inventory screen reachable in two steps or fewer.
- **Negative / abuse variants:** Missing nav item; skip link not first; duplicate titles.
- **Waits on:** Each screen's UI PR as it lands (Active holds 2)

#### TD-ADM-UI-na-X04 — UX1-X04 → UXR-D04, UXR-D05, UXR-B10: component state checklist; list loading skeleton and error; shared state rules

- **Traces-to:** UX1-X07, UX1-X04, UXR-D04, UXR-D05, UXR-B10, UXR-B13, UXR-D14; API / UI sibling TD-ADM-130
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto+manual
- **Preconditions:** Network throttle and fault hooks; component checklist from UXR-D04.
- **Steps:** 1) Fill the UXR-D04 checklist (default, hover, focus, active, disabled, error, empty, loading) per component on every screen. 2) Throttle: load each list, then re-sort. 3) Force a 500 on each list with filters set; press Retry. 4) Count live regions per page. 5) Check every disabled control for a nearby reason when not obvious.
- **Expected:** Checklist complete and passing; lists show skeleton rows with `aria-busy="true"` while loading and keep existing rows during a re-sort with a busy indicator; a failed load shows 'We couldn't load {type}.' with Retry and keeps the filters; Retry reloads; never a blank page; one polite live region per page; disabled controls stay visible with a reason.
- **Negative / abuse variants:** Blank page while loading; filters lost on error; several live regions.
- **Waits on:** Each screen's UI PR as it lands (Active holds 2)

#### TD-ADM-UI-na-X05 — UX1-X05 → UXR-D06–D10: WCAG 2.2 AA checks beyond axe (contrast per state, focus not hidden, targets, labels, table semantics, reflow, zoom)

- **Traces-to:** UX1-X07, UX1-X05, UXR-D06, UXR-D07, UXR-D08, UXR-D09, UXR-D10, UXR-D14; API / UI sibling TD-ADM-UI-na-axe, TD-ADM-UI-na-kbd
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto+manual
- **Preconditions:** All non-auth screens built; contrast tool; 320 px viewport and 200% zoom.
- **Steps:** 1) Contrast on every component state: text, UI parts, focus ring. 2) Tab through each screen with the sticky header and nav present. 3) Measure targets; check visible labels; read table header `scope` and `aria-sort`; check live regions. 4) 320 px and 200% zoom per screen. 5) Attach axe (na-axe) and walkthrough (na-kbd) results.
- **Expected:** Text ≥4.5:1, UI parts and focus ≥3:1 (1.4.3, 1.4.11); every action by keyboard, focus order follows visual order, focus always visible and never hidden by sticky elements (2.1.1, 2.4.3, 2.4.7, 2.4.11); targets ≥24×24 px, visible labels, `scope` + `aria-sort`, live regions (2.5.8, 3.3.2, 1.3.1, 4.1.2, 4.1.3); reflow at 320 px and 200% with no loss (tables may scroll inside); pass bar UXR-D10 met.
- **Negative / abuse variants:** Focus hidden under the sticky header; placeholder-only labels; page-level horizontal scroll.
- **Waits on:** Each screen's UI PR as it lands (Active holds 2)

#### TD-ADM-UI-na-X06 — UX1-X06 → UXR-D11: layout at 1280 / 768 / 320 on every screen

- **Traces-to:** UX1-X07, UX1-X06, UXR-D11, UXR-D09, UXR-D14; API / UI sibling TD-ADM-130
- **Layer:** E2E-UI (Playwright headless)
- **Priority:** P1
- **Automation:** auto+manual
- **Preconditions:** All screens built; viewport presets.
- **Steps:** 1) Screenshot every screen at 1280, 768 and 320 px. 2) At 768 operate the nav. 3) Scroll a wide table at 768 and 320. 4) Open the delete modal and the Expire dialog at 320.
- **Expected:** 1280+ primary layout; at 768 the nav collapses to a 'Menu' button with `aria-expanded` that toggles correctly; tables scroll inside their container with the first column kept visible; at 320 no page-level horizontal scroll except inside data tables; modals fit the viewport and scroll inside.
- **Negative / abuse variants:** Nav unusable at 768; modal off-screen at 320.
- **Waits on:** Each screen's UI PR as it lands (Active holds 2)

#### TD-ADM-UI-na-X09 — UX1-X09 → UXR-D16: every Story with a UI part carries the UI/UX gate (process-review)

- **Traces-to:** UX1-X07, UX1-X09, UXR-D16, UXR-D14; API / UI sibling TD-ADM-101, TD-ADM-131, TD-ADM-121
- **Layer:** process-review
- **Priority:** P1
- **Automation:** manual
- **Preconditions:** A7 Story files and the CPM checklist.
- **Steps:** 1) List Stories with a UI part (incl. Steps 8 and 11 UI parts and the step-15 audit viewer). 2) For each, read the gate lines. 3) At merge, check for the Chief UI/UX approval and UI/UX QA PASS file path.
- **Expected:** Each UI-bearing Story names the UI/UX gate (Chief UI/UX approval + UI/UX QA PASS file); merged UI PRs show both; TD-ADM-101 step 2 and TD-ADM-131 run under that gate.
- **Negative / abuse variants:** UI PR merged without the gate.
- **Waits on:** Step-15 audit viewer Story under its own UI/UX gate; merge also needs Condition 3 (Active holds 1, 2, 5)

#### TD-ADM-UI-na-axe — axe-core scan on every non-auth screen and state (S-B1–S-B6, S-C1, Expire dialog, S-D1–S-D4): zero serious or critical

- **Traces-to:** UX1-X07, UX1-X05, UXR-D08, UXR-D10, UXR-D14; existing sibling TD-ADM-130
- **Layer:** a11y (axe-core via Playwright, headless)
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** Screens per Spec X01 inventory; states seeded: default, loading, empty, filter miss, error, modal open (delete, Expire), 'Show deleted' on.
- **Steps:** 1) Run axe-core with WCAG 2.2 A/AA rule tags on each screen in each state. 2) Save one JSON result per screen and state under the run folder.
- **Expected:** Zero serious and zero critical violations on every screen and state; the results file lists every Spec X01 non-auth screen ID.
- **Negative / abuse variants:** Screen or state skipped; rule tags narrowed.
- **Waits on:** Each screen's UI PR as it lands (Active holds 2)

#### TD-ADM-UI-na-kbd — Keyboard-only walkthrough: list → search / sort / filter / page → detail → edit + save → Expire dialog → delete modal → confirm

- **Traces-to:** UX1-X07, UX1-X05, UX1-C03, UXR-D07, UXR-D10, UXR-D14, UXR-C03; existing sibling TD-ADM-130, TD-ADM-UI-del-02
- **Layer:** E2E-UI (keyboard only)
- **Priority:** P0
- **Automation:** auto+manual
- **Preconditions:** Signed-in CoreOwner; seeded data; no mouse input.
- **Steps:** 1) Skip link → nav → Negotiations. 2) Search, sort a header, add and remove a filter chip, change page. 3) Open a row; edit Ends at; Save. 4) Open the Expire dialog; cancel with Escape. 5) Open delete; Tab to the destructive button; confirm (type the ID where asked).
- **Expected:** Every step works by keyboard; focus always visible and never hidden by sticky elements; no trap outside dialogs; focus returns to the trigger after Escape and to a sensible target after save and delete; walkthrough notes attached per screen (UXR-D10).
- **Negative / abuse variants:** Mouse-only control; focus lost after save or delete.
- **Waits on:** Each screen's UI PR as it lands (Active holds 2)

### 4.19 UI cases in CI (UX1-X08, QA + DevOps co-own)

#### TD-ADM-UI-ci-01 — Headless UI cases run in CI on every PR touching admin UI files; a broken UI case fails CI

- **Traces-to:** UX1-X08, UXR-D15 (QA + DevOps co-own); applies to every TD-ADM-UI-* case and TD-ADM-130/131
- **Layer:** CI process + negative control
- **Priority:** P0
- **Automation:** auto
- **Preconditions:** DevOps-wired CI job (pipeline wiring is DevOps-owned); QA-owned case catalog (`cases.json` entries tagged group `ui-*`); Turnstile test keys and synthetic CoreOwner by env name only; no live admin.core calls.
- **Steps:** 1) Open a PR that touches an admin UI file; confirm the UI job triggers and runs every non-SKIP TD-ADM-UI-* case headless. 2) Open a PR that touches only non-UI files; record whether the UI job runs (path rule owned by DevOps). 3) Negative control: on a throwaway branch, break one UI case's target (e.g. remove a required attribute); confirm the job fails and the PR check is red / merge-blocking. 4) Confirm results (incl. axe JSON) are kept as CI artifacts.
- **Expected:** UI job runs on every PR touching admin UI files; any failing UI case fails the job and blocks merge; SKIP stubs reported as SKIP (not PASS) with their Active hold reason; no secrets in logs.
- **Negative / abuse variants:** UI job optional or `continue-on-error`; failures reported as warnings; job runs against live admin.core.
- **Waits on:** DevOps owns pipeline wiring (trigger paths, runner, browsers, artifact retention); QA owns the case catalog. Brief 4 D4.2/D4.3/Q15 made Playwright optional — this design requires it; Chief Dev / CPM to align the SD brief. No CDK names or account IDs invented here. Until wired, UI cases run locally and block merge by review (UXR-D15, addendum D.7) (Active holds 7)

**Requirement (binding in this design):** every non-SKIP TD-ADM-UI-* case, plus TD-ADM-130 and TD-ADM-131, runs headless in CI on every PR that touches admin UI files; a failing UI case fails the CI check and blocks merge. **Ownership:** DevOps owns pipeline wiring (trigger paths, runners, browsers, artifacts); QA owns the case catalog and tags (`/workspace/qa/harness/admin-dashboard/cases.json`, groups `ui-auth`, `ui-delete`, `ui-nonauth`, `ui-ci`). No CDK names, AWS account IDs or key values in this design (Active holds 7).

---

## 5. Entry / exit criteria and evidence

| Phase | Entry | Exit | Evidence location |
|-------|-------|------|-------------------|
| Story-level (after Active holds lift) | A6 PASS; A7 unlocked after H4; Story assigned | Story P0 API/integration green for mapped TD-ADM IDs | `/workspace/qa/YYYY-MM-DD__qa__qa-report__core-admin-*.md` |
| PR CI | PR touches admin surface | CI: all P0 automated cases for changed area PASS; **PR touching admin UI files: all non-SKIP TD-ADM-UI-* cases PASS headless, else CI fails** (TD-ADM-UI-ci-01); no secret invent in diff | CI logs + axe JSON artifacts + `/workspace/qa/...` report path |
| Pre-deploy | CI green; CQ gate; deploy held until Ivan OK | Pre-deploy checklist: TD-ADM-121 gates + TD-ADM-150 no-secret scan | `/workspace/qa/...__predeploy__...md` |
| Post-deploy live smoke | **Ivan OK** deploy; host `admin.core.dealoware.com` | Live smoke: TD-ADM-001/003/070/130/131 (+ subset P0 auth if secrets available via env) PASS | `/workspace/qa/YYYY-MM-DD__qa__qa-report__core-admin-live-smoke.md` |

KB twin of design: `dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`. Runtime evidence under `/workspace/qa/` (do not invent AWS account IDs in evidence).

---

## 6. Open questions / assumptions

| # | Item | Source | Disposition |
|---|------|--------|-------------|
| OQ1 | Do CAPTCHA (Turnstile) failures count toward the same per-account / per-IP lockout budgets? | Spec §8.6 “SA to confirm” | **Closed** by lockout note (`specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md`, OQ1) + Architecture Turnstile note: Turnstile reject happens before any credential check, counts toward no counter, audited `captcha-failed`. Tested in TD-ADM-040 |
| OQ2 | TOTP digits/period and recovery-code count (Spec recommends 10) | Spec §8.3 “SA to confirm”; sign-in steps note v1; redlines note v2 A05 | **Closed (r5):** 6 digits, 30-second period, HMAC-SHA1, 10 recovery codes — Spec UX1 redlines note v2 A05 (MUST). Tested in TD-ADM-170, TD-ADM-171, TD-ADM-UI-auth-08 |
| OQ3 | Sliding vs fixed window for lockout counters | Spec §8.6 “SA to confirm” | **Closed** by lockout note OQ3: sliding 15-min window (event exactly 15 min old is outside), 5 per account / 20 per IP, flat 30-min lock/throttle, in-lock attempts rejected + audited but not counted, wrong recovery code = bad 2FA, separate reset/bootstrap per-IP counter. Tested in TD-ADM-050 / TD-ADM-051 |
| OQ4 | Admin Host-header reject for non-`admin.core` at app vs edge | Spec §3 “served only as” | Open — do not invent edge details; TD-ADM-001 tests app binding if present; do not target platform hosts as positive cases |
| OQ5 | Retry-After on admin auth 429 | Spec §8.6 “HTTP 429 / equivalent” | **Closed** by UX1-A03 r2: admin.core IP throttle may return **429** with the generic body; **no admin.core auth response ever carries `Retry-After`** (nor remaining time / attempt count / “locked”). TD-ADM-051 asserts 429 + absence of Retry-After. Core API H4 (429 + Retry-After) is separate and unchanged |
| OQ6 | Validation error copy exact wording | Spec §5 SA residual | Open — do not invent copy; TD-ADM-141 asserts no denied-field/secret echo |
| OQ7 | Lockout note vs UX1-A03 r2 on IP-throttle status, reset-unlock and copy | Lockout note v4 (`e45ffeac…`) conflicted; v5.3 aligns | **Closed (r3)**: v5.3 `67686ce5044728db5cb754ffb65c68562e5ed38aea26161bf90988a145972b8b` is final — Spec QA PASS + Security QA point 5 PASS. MUST 429, reset does not lift lock, r2 copy. Active hold removed r4: re-cited at v5.5 `8a194eb9…` (rules unchanged) |
| OQ8 | v5.3+ scope: break-glass ops unlock, S-A12 recent sign-in activity panel, lock-notice email (SHOULD) | Lockout note v5.5 (final) / Chief Security answers items 3–4 | **Partly closed (r3)**: S-A12 panel + 24h lock notice → TD-ADM-UI-auth-19; break-glass (ops-only, audited, no public-UI unlock) → review in TD-ADM-050. **Open:** lock-notice email (SHOULD) has no dedicated case yet — propose an integration case in the next revision; do not invent copy r4: the email SHOULD also cover 'correct password, then repeated code failures' (sign-in steps note condition 6); still no dedicated case |
| OQ9 | Sign-in step shape: two steps vs one form | UX1-A16 | **Closed (r4):** two steps — Chief Security decision `bb0bcaa2…` + sign-in steps note v1 `0a3db5f4…` (Spec QA + Security QA PASS). Cases TD-ADM-023…029, TD-ADM-UI-auth-21…23 |
| OQ10 | Playwright optional (Brief 4 D4.2/D4.3/Q15, SD decision) vs UX1-X08 UI cases required in CI | UX1-X08; routing r2 (QA + DevOps co-own) | Open — this design **requires** headless UI cases in CI (TD-ADM-UI-ci-01). DevOps wires; Chief Dev / CPM align Brief 4 |
| OQ11 | Password-rules note final hash | `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` | **Closed (r4):** v2.3 `1a7b342c…` final (Spec QA + Security QA PASS). TD-ADM-UI-auth-08 / -10 / -13 assert it |
| OQ12 | Step-2 cap (5 per token) and account budget (5 / 15 min) are both 5 | Sign-in steps note condition 3; lockout note v5.5 | Open, for Spec / Security awareness only — under default config the 5th wrong code locks the account before the cap can be seen alone. TD-ADM-025 / UI-auth-23 prove the cap with a test-only raised threshold in the local test host and assert lock + token clear in default config. No rule change proposed |
| OQ13 | Addendum Part A wording vs final Spec notes | Addendum UXR-A43 activity event label vs lockout note v5.5 closed event list | **Narrowed (r5):** 'Back to sign in' and the UXR-A08 focus order now match the Spec. Left: UXR-A43 says 'Failed second factor (correct password, wrong code)', lockout note v5.5 says 'code failed after correct password'. For Chief UI/UX — tests assert the Spec label |
| OQ14 | Which statuses the Offers stat tile covers | UXR-B12 ('Spec states which statuses Offers covers') | Open (Spec). TD-ADM-UI-na-B09 asserts tile total = linked list total; Offers link per Spec once stated |
| OQ15 | Free-text search on the audit log | UXR-D12, addendum D.7 | Open (Spec). Interim: no search box — TD-ADM-UI-na-X02 asserts none |
| OQ16 | Does the S-A12 password change ask for the current password? | UXR-A42; addendum Active holds | Open (Spec). Holds only the S-A12 password PR and the password parts of TD-ADM-UI-auth-10 / -13 (Active holds 3) |
| OQ17 | Turnstile on S-A10 (reset submit) | Spec §8.5; TD-ADM-UI-auth-11 | Open (Spec) — unchanged from r3/r4; auth-11 asserts the Spec once confirmed |
| OQ18 | Who writes Withdrawn (maker withdraw endpoint / Story) | Product decision `6ea05ddb…`; addendum Active holds | Open (Chief Product). Admin never writes it (TD-ADM-178). Until a writer exists TD-ADM-063 seeds rows and TD-ADM-UI-na-B03 expects the filter-miss state |
| OQ19 | A deleted record's ID: S-D3 (UXR-D03) or read-only detail (UXR-B08) when 'Show deleted' is on | UXR-D03 vs UXR-B08 | Open (Chief UI/UX + Spec). Tests: toggle off or direct URL → S-D3 (TD-ADM-179, na-X01); opened from the deleted list → read-only detail (na-B08). Confirm the rule |

**Assumptions (from locked sources — not invent):** soft-delete marker `DeletedAt`; paging offset/limit 50/200; Withdrawn is a first-class **offer** status only, never a negotiation status, and the admin can't set it (status-list note `0a5ff57b…`, Product decision `6ea05ddb…`); negotiation statuses Open, Closed, Expired; session cookie HttpOnly Secure SameSite=Strict; audit IP keyed HMAC-SHA256; Turnstile only; SES behind mail interface; email OTP OUT; hard delete deferred.

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
- [x] r3: UX1-A12, C08, X07, X08 mapped (§3.8); UXR-A01…A44 44/44 (§3.9); C01–C07 7/7 (§3.10); B/X rows 22/22 as stubs with Active holds
- [x] r3: UI detail cited by UXR ID, not copied; no B/C/X UI detail invented
- [x] r3: lockout note v5.3 re-cited as final (`67686ce5…`); password-rules note v2.1 held — both superseded by r4 cites
- [x] r3: X08 CI requirement stated; DevOps owns wiring, QA owns catalog; no CDK/account IDs
- [x] r4: lockout note v5.5 `8a194eb9…`, password-rules v2.3 `1a7b342c…`, sign-in steps v1 `0a3db5f4…`, A16 decision `bb0bcaa2…` cited with full sha256 in §1; verifications on disk match
- [x] r4: UX1-A16 conditions 1–8 + C1–C4 mapped 12/12 (§3.11); password-rules rules + UI 10/10 (§3.12); no route strings invented
- [x] r4: UX1-A03 r2 401/429 rules and Core API H4 unchanged
- [x] r5: TD-ADM-062 lists Open, Closed, Expired only; no negotiation + Withdrawn wording anywhere (checked by script); Withdrawn kept on TD-ADM-063 as offer-only and read-only
- [x] r5: UXR-B01…B18, C01…C08, D01…D16 → 42/42 (§3.13); UXR-A45 → auth-24; UXR-B14 Expire → na-expire; UXR-B15 → na-B11
- [x] r5: redlines note v2 `eb63276f…` items 13/13 (§3.14); status-list + Product items 4/4 (§3.15); routes only from Spec X01; no route strings invented
- [x] r5: UX1-A03 r2 401/429 rules, Core API H4, lockout v5.5, password-rules v2.3, sign-in steps v1, A16 cites unchanged
- [x] One Active holds list (§0); per-case 'Waits on' points to it
- [x] PoC $0

---

## 8. Programmatic self-verification

Method: extract Product `AC[1-9]`, Spec Locked table rows `1–7`, Spec §13 MET rows `1–15`, Spec checklist numbered pts `1–15`, Dev Plan `### Step N`, Dev Plan checklist pts `1–14`, plus Spec section IDs listed in §3.3; diff against traces in this design.

| Check | Result |
|-------|--------|
| Spec tip sha256 | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` MATCH |
| Plan tip sha256 | `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf` MATCH |
| Extracted source IDs | 84 (r2) + 77 (r3) + 22 (r4) + 64 (r5: UXR-A45 1 + §A.5 growth 4 + UXR-B/C/D 42 + redlines items 13 + status/Product items 4) = **247** |
| IDs covered by ≥1 TD-ADM | **247 / 247** |
| Gaps | **none** |
| Test cases | **127** (P0=88, P1=34, P2=5) |
| Secret/account-ID invent scan (this file) | PASS — no password values; no AWS account IDs; no SES/Turnstile/HMAC secret values |

**Coverage: 0 gaps** (Chief QA self-check on r5; not a Test design QA stamp). Active holds: see the single list in §0. Do not invent password/AWS values. PoC $0.

---

## 9. Confirm

**A6 Test design r5 — revised, awaiting Test design QA re-verify (Senior Product QA).** Path `qa/2026-10-05__qa__test-design__core-admin-dashboard.md`. Cases **127** (P0 **88** / P1 **34** / P2 **5**): 114 at r4 + 13 r5 (TD-ADM-170…180, TD-ADM-UI-auth-24, TD-ADM-UI-na-expire). TD-ADM-062 fixed (negotiations Open, Closed, Expired). B/C/X cases filled for UX1 approval condition 3: UXR-B/C/D **42/42**. Redlines note v2 items **13/13**. Sources **247/247**. UX1-A03 r2 401/429 unchanged; Core API H4 unchanged. Host `admin.core.dealoware.com` only. No secrets / account IDs invented. Active holds: §0. PoC $0.
