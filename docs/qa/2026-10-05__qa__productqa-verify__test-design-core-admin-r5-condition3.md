# Product QA — Test design re-verify (Core admin dashboard r5 / UX1 condition 3)

| Field | Value |
|-------|--------|
| Role | Senior Product QA (Test design QA) — independent of Chief QA author |
| Date | 2026-10-05 ~9:27pm ET |
| Scope | r5: TD-ADM-062 Withdrawn fix; UX1 approval condition 3 B/C/X fill; Spec UX1 redlines note v2 surface. Gates Steps 7, 9, 10, 15 UI merges on condition 3. Read-only; no merge/deploy/push |
| Design | `/workspace/dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md` |
| Design sha256 | `7bb1dd3b24a630dfca10c3b96969aa1f7d4815108d83fbc497475e1a1864a26f` — **MATCH** expected |
| Revision note | `/workspace/qa/2026-10-05__qa__revision-note__test-design-core-admin-r5-condition3.md` |
| Revision note sha256 | `08dd301384eea2e780e6726f16517ab08a7731514d13489109130c81da2f2bc5` |
| Spec UX1 redlines note v2 | `/workspace/dealoware-kb/specs/2026-10-05__spec__spec__core-admin-ux1-redlines-note.md` |
| Redlines v2 sha256 | `eb63276fc51217c3c82772c0b8de26ead1337f6e382938a1d736a05d13f6f2d0` |
| UXR-B/C/D source (addendum Parts B–D) | `/workspace/dealoware-kb/ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` sha256 `7aa374e9de0ae6c0d2999122f47e7b28e04a66a2cce4084f0e7314bffad5419e` |
| Prior PASS (r4) | `/workspace/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-ux1-a16-final-cites.md` |
| Overall | **PASS** |
| PoC | $0 |

## Verdict table

| # | Item | Verdict | Evidence |
|---|------|---------|----------|
| 1 | Hashes + case count **127** unique; delta from 114 | **PASS** | Design MATCH `7bb1dd3b…`. Rev note recorded `08dd3013…`. Exactly **127** unique `#### TD-ADM-*` headings (no dups). Delta **+13** from r4's 114: API `TD-ADM-170…180` (11) + `TD-ADM-UI-auth-24` (1) + `TD-ADM-UI-na-expire` (1). Prior B/C/X stubs filled in place (no ID churn). Coverage §3.7 states 127 = 53+51+10+13 |
| 2 | TD-ADM-062 Withdrawn **offer-only**; no negotiation-level Withdrawn status/transition | **PASS** | TD-ADM-062 r5 note + Expected: negotiations **Open, Closed, Expired** only; filter rejects `Withdrawn`/`Accepted`/`Deleted`; negatives forbid negotiation rows carrying Withdrawn. TD-ADM-063 keeps Withdrawn as offer-only (seeded; admin can't set). TD-ADM-178 asserts Open→Withdrawn **denied** for negotiations and admin never sets Withdrawn on offers. Whole-design `Withdrawn` hits are fix notes, offer-only asserts, deny tests, or OQ18 — none introduce a negotiation Withdrawn status. Consistent with status-list note `0a5ff57b…` and Product decision `6ea05ddb…` |
| 3 | UXR-B14 (Expire) and UXR-B15 covered with executable asserts | **PASS** | **UXR-B14:** `TD-ADM-UI-na-B10` (edit fields / status choices; Steps+Expected), `TD-ADM-UI-na-expire` (Expire dialog: title/body with server count, focus on Cancel, Escape/backdrop no-op, Expiring… single submit, success cancels open offers, failure Nothing was changed + Retry), `TD-ADM-178` (server Open→Expired + open offers→Cancelled). **UXR-B15:** `TD-ADM-UI-na-B11` (Save disabled until change / Saving… / single request / Discard guard / Changes saved / inline+summary errors) |
| 4 | Full UXR-B/C/D traceability vs addendum (redlines v2 Spec is separate RL-* surface) | **PASS** | Addendum has **42** UXR-B01…B18 + C01…C08 + D01…D16; design §3.13 maps **42/42**; unmapped **none**; orphan UXR IDs **none**; every mapped case ID has a `####` definition. Spec redlines v2 (`eb63276f…`) carries Spec locks (A05/A15/A20/return/B10/B01/X01/links/S-D) in §3.14 (13/13) — not UXR-B/C/D IDs. Matrix below |
| 5 | Condition 3 B/C/X fill present and matches rev note | **PASS** | §0: all 22 non-auth stubs now full cases; new na-expire (B14 Expire); B15→na-B11; every UXR-B/C/D → ≥1 case. Matches rev note bullets. Active holds 1: condition 3 met only after this SPQA PASS |
| 6 | No regression vs r4 PASS | **PASS** | TD-ADM-050 still identical **401** + r2 generic body; no locked/duration/count; reset does not unlock/clear; TOTP still required. TD-ADM-051 still **429** + generic body; **no Retry-After**; throttled reset → exists reply + no email. §6: OQ1 / OQ3 / OQ5 / OQ7 still **Closed** (OQ7 closed r3; re-cited v5.5). Core API H4 still 429+Retry-After, separate. A16 cases TD-ADM-023…029 + UI-auth-21…23 intact. Cites unchanged: lockout v5.5 `8a194eb9…`, password-rules v2.3 `1a7b342c…`, sign-in steps v1 `0a3db5f4…`, A16 decision `bb0bcaa2…` |
| 7 | Retired hold phrase absent; no >5× repeats | **PASS** | Scan on design + rev note: **0** hits for the retired two-word hold phrase. Consecutive same-token >5: **none** |
| 8 | Internal consistency; one Active holds list | **PASS** | Single §0 Active holds list (9 items). Per-case Waits on → that list. §3.13/§3.14/§3.15 align with cases. No AWS account IDs invented. Host admin.core only |

## Full UXR-B / UXR-C / UXR-D traceability matrix (§3.13)

| UXR | Case IDs |
|-----|----------|
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
| UXR-C08 | TD-ADM-UI-del-01…06 |
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
| UXR-D14 | TD-ADM-UI-na-B01…B15, na-expire, na-X01…X06, na-X09, na-axe, na-kbd (25 IDs as in §3.13) |
| UXR-D15 | TD-ADM-UI-ci-01 |
| UXR-D16 | TD-ADM-UI-na-X09 |

Unmapped addendum UXR items: **none**. Orphan design UXR IDs: **none**.

## Gaps

| Sev | Gap | Fix |
|-----|-----|-----|
| — | None blocking | — |
| Info | Harness `cases.json` still reports **114** while design is **127** | Sync A8 harness stubs/index to r5 (+170…180, auth-24, na-expire) before CI relies on stub count |
| Info | Spec redlines note v2 / some Spec notes still show Draft in Status headers while Spec QA + Security QA PASS on tip | Optional Status flip on next Spec edit; cites already treat as final |

## Active holds (from design §0)

1. This SPQA PASS is what unlocks UX1 approval **condition 3** for Step 7 / 9 / 10 / 15 UI PR merges on that condition.
2. UI cases stay SKIP until screens land; each UI PR still needs Dev Code QA, Security, and the UI/UX gate (UXR-D16).
3. S-A12 current-password field still open (OQ16) — holds only the S-A12 password PR and password-change parts of auth-10 / -13.
4. Condition 4: Withdrawn (Step 7) and edit/Expire (Step 9) need their own Dev Code QA PASS; nothing writes Withdrawn until a maker withdraw endpoint exists (OQ18).
5. Audit viewer UXR-D12/D13/D16 follow the step-15 Story UI/UX gate.
6. Spec decisions still open: OQ14 Offers stat, OQ15 audit-log search, OQ17 Turnstile on S-A10, OQ19 deleted-ID page.
7. X08 CI wiring still with DevOps (OQ10).
8. Deploy held until Ivan OK; no live admin.core calls.
9. No secrets / AWS account IDs invented.

PoC $0.

## Stamp

**PASS** — design tip `7bb1dd3b24a630dfca10c3b96969aa1f7d4815108d83fbc497475e1a1864a26f` meets UX1 approval condition 3 (B/C/X fill incl. UXR-B14 Expire and UXR-B15), TD-ADM-062 offer-only Withdrawn, Spec redlines v2 surface, full 42/42 UXR-B/C/D traceability, and no regression vs the r4 Product QA PASS. Condition 3 may be treated as met for Steps 7, 9, 10 and 15 UI merge gating once Chief QA / CPM accept this stamp.

Report: `/workspace/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-r5-condition3.md`
