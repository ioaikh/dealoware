# UI/UX QA verification: test design r5 UI coverage (UX1 approval condition 3)

| Field | Value |
|---|---|
| Verifier | Dealoware UI/UX QA (independent of the author, Chief QA, and of the Test design QA reviewer) |
| Date | 2026-10-05, checked 9:31pm to 9:36pm ET |
| Asked by | Dealoware CPM, item 1 (9:31pm ET) |
| Scope | UX1 approval condition 3: the B, C and X cases in the test design, including UXR-B14 and UXR-B15, plus UXR-A45, the UXR-B01 follow-up details and Part A coverage. Read only. |
| Test design (r5) | `qa/2026-10-05__qa__test-design__core-admin-dashboard.md`, sha256 `7bb1dd3b24a630dfca10c3b96969aa1f7d4815108d83fbc497475e1a1864a26f` (matches the expected `7bb1dd3b`) |
| Test design QA PASS | `/workspace/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-r5-condition3.md`, sha256 `7a2e06a4bb05f270ad4a9d97764f6d260cc2ebe17b6819c5538b129e64a44a30` (Senior Product QA, about 9:27pm ET, overall PASS) |
| Addendum (requirement source) | `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md`, sha256 `7aa374e9de0ae6c0d2999122f47e7b28e04a66a2cce4084f0e7314bffad5419e` on disk. The test design cites this same hash (lines 36, 130, 359, 1452). No mismatch. |
| UX1 approval | `ux/2026-10-05__ux__approval__ux1-admin-requirements.md`, sha256 `15092fc1423f02dc9da19c96e325796eac26a75372108f712c948e9f9cffcfe3`, condition 3 at line 20 |

## Verdict

**PASS.** Test design r5 meets UX1 approval condition 3 for UI coverage. Every UXR-B, UXR-C and UXR-D row in addendum `7aa374e9` (42 rows) maps to at least one real case. Each mapped case has its own `####` heading with Preconditions, Steps and Expected results, and none of them is a stub. UXR-A45, the UXR-B01 Created fallback sort and Deleted badge position, and all 45 Part A rows are covered. No fixes are needed. Three record notes below are optional and don't hold anything.

## How I checked

1. Hash and count. The sha256 starts with `7bb1dd3b`. A script counted 127 unique `#### TD-ADM-*` headings with no duplicates, which matches the r5 revision table (line 29) and the coverage counts (line 345).
2. Mapping. A script read the §3.13 table (lines 461 to 508) and the §3.9 table (lines 363 to 413) and compared them with the UXR rows in the addendum. All 87 addendum rows (A01 to A45, B01 to B18, C01 to C08, D01 to D16) are mapped. Every mapped case ID exists as a heading. No addendum row is unmapped and no mapped ID is an orphan.
3. Real and runnable. I read every non-auth case (lines 1702 to 2094) and every auth case (lines 1410 to 1700). All 56 UI cases have Steps and Expected lines (56 of 56). Expected results quote the addendum copy for the rows they cover. The word "stub" appears once, at line 1379, in a stale sibling note on TD-ADM-140 (see note 2), and not in any UI case body. The "N/A" hits (lines 1036 to 1408) are in the Negative variants field of backend and smoke cases, not in UI cases.
4. No row in Parts B to D is marked untestable by the addendum. UXR-C08, D14, D15 and D16 are process rows, and the design covers them with del-01 to del-06, the full non-auth set, UI-ci-01 and a manual process-review case UI-na-X09 that has its own steps.

## Row table: UXR-B, UXR-C, UXR-D

Case IDs drop the `TD-ADM-` prefix. Line numbers point to the §3.13 table in the test design.

| UXR | TD cases | Result |
|---|---|---|
| UXR-B01 | UI-na-B01, UI-na-B08 (§3.13 line 467) | PASS |
| UXR-B02 | UI-na-B02, UI-na-B04 (§3.13 line 468) | PASS |
| UXR-B03 | UI-na-B02, UI-na-B03, UI-na-B05 (§3.13 line 469) | PASS |
| UXR-B04 | UI-na-B07 (§3.13 line 470) | PASS |
| UXR-B05 | UI-na-B06 (§3.13 line 471) | PASS |
| UXR-B06 | UI-na-B06, 177 (§3.13 line 472) | PASS |
| UXR-B07 | UI-na-B03 (§3.13 line 473) | PASS |
| UXR-B08 | UI-del-06, UI-na-B08 (§3.13 line 474) | PASS |
| UXR-B09 | UI-na-B14 (§3.13 line 475) | PASS |
| UXR-B10 | UI-na-X04 (§3.13 line 476) | PASS |
| UXR-B11 | UI-na-B15 (§3.13 line 477) | PASS |
| UXR-B12 | UI-na-B09 (§3.13 line 478) | PASS |
| UXR-B13 | UI-na-B09, UI-na-X04 (§3.13 line 479) | PASS |
| UXR-B14 | UI-na-B10, UI-na-expire, 178 (§3.13 line 480) | PASS |
| UXR-B15 | UI-na-B11 (§3.13 line 481) | PASS |
| UXR-B16 | UI-na-B11 (§3.13 line 482) | PASS |
| UXR-B17 | UI-na-B12 (§3.13 line 483) | PASS |
| UXR-B18 | UI-auth-15, UI-na-B13, 177, 179 (§3.13 line 484) | PASS |
| UXR-C01 | UI-del-01 (§3.13 line 485) | PASS |
| UXR-C02 | UI-del-01 (§3.13 line 486) | PASS |
| UXR-C03 | UI-del-02, UI-na-expire, UI-na-kbd (§3.13 line 487) | PASS |
| UXR-C04 | UI-del-03, UI-na-expire (§3.13 line 488) | PASS |
| UXR-C05 | UI-del-04 (§3.13 line 489) | PASS |
| UXR-C06 | UI-del-05 (§3.13 line 490) | PASS |
| UXR-C07 | UI-del-06, UI-na-expire (§3.13 line 491) | PASS |
| UXR-C08 | UI-del-01, UI-del-02, UI-del-03, UI-del-04, UI-del-05, UI-del-06 (§3.13 line 492) | PASS |
| UXR-D01 | UI-na-X01 (§3.13 line 493) | PASS |
| UXR-D02 | UI-na-X03 (§3.13 line 494) | PASS |
| UXR-D03 | UI-na-B13, UI-na-X01, 179 (§3.13 line 495) | PASS |
| UXR-D04 | UI-na-X04 (§3.13 line 496) | PASS |
| UXR-D05 | UI-na-X04 (§3.13 line 497) | PASS |
| UXR-D06 | UI-na-X05 (§3.13 line 498) | PASS |
| UXR-D07 | UI-na-X05, UI-na-kbd (§3.13 line 499) | PASS |
| UXR-D08 | UI-na-X05, UI-na-axe (§3.13 line 500) | PASS |
| UXR-D09 | UI-na-X05, UI-na-X06 (§3.13 line 501) | PASS |
| UXR-D10 | UI-na-X05, UI-na-axe, UI-na-kbd (§3.13 line 502) | PASS |
| UXR-D11 | UI-na-X06 (§3.13 line 503) | PASS |
| UXR-D12 | UI-na-X02 (§3.13 line 504) | PASS |
| UXR-D13 | UI-na-X02 (§3.13 line 505) | PASS |
| UXR-D14 | all 25 non-auth cases: UI-na-B01 to B15, UI-na-expire, UI-na-X01 to X06, UI-na-X09, UI-na-axe, UI-na-kbd (§3.13 line 506) | PASS |
| UXR-D15 | UI-ci-01 (§3.13 line 507) | PASS |
| UXR-D16 | UI-na-X09 (§3.13 line 508) | PASS |

## Focus rows

| Item | Evidence | Result |
|---|---|---|
| UXR-B14 edit fields | UI-na-B10 (lines 1890 to 1900): inputs only for the SA §4 fields, Negotiation status Open to Closed or Expired only, offer status Cancelled only, Amount and Terms read-only when not Open, required marked with text, close confirm names the Cancelled offers, Suspended to Active works. TD-ADM-178 checks the same rules on the server. | PASS |
| UXR-B14 Expire negotiation dialog | UI-na-expire (lines 1902 to 1912): title "Expire negotiation {ID}?", body "3 open offers will be cancelled." with the server count and "No open offers will be cancelled." when zero, `role="dialog"`, focus starts on Cancel, focus trap, Escape and backdrop change nothing and write no audit row, "Expiring…" single submit, success live region "Negotiation {ID} expired.", open offers become Cancelled while the accepted offer stays, failure keeps the dialog with "Nothing was changed." and Retry, no Expire on a Closed negotiation. | PASS |
| UXR-B15 Save, Cancel and discard guard | UI-na-B11 (lines 1914 to 1924): Save disabled until a change and while saving, "Saving…", one request on double click, Cancel returns to detail, "Discard your changes?" with "Discard" and "Keep editing" on nav link, Back and tile link, "Changes saved." in a live region. | PASS |
| UXR-A45 absolute-cap warning | TD-ADM-UI-auth-24 (lines 1690 to 1700): at 7h58m the dialog "Your session is ending" shows with one "OK", `role="alertdialog"`, Escape acts as OK, focus returns, no request extends the session, the idle dialog is suppressed in the same 2 minutes, and at 8h the next request lands on S-A1 with the UXR-A39 text. §3.9 line 413 maps A45 to auth-24. TD-ADM-UI-auth-14 (line 1570) now covers only the idle warning. | PASS |
| UXR-B01 Created fallback sort | UI-na-B01 step 2 and Expected (lines 1789 to 1790): runs once on a build without `UpdatedAt` and once with it; before Step 7 the default is Created, newest first, with the indicator on Created. | PASS |
| UXR-B01 Deleted badge beside status | UI-na-B01 step 3 and Expected (line 1790): the status badge and the Deleted badge show side by side, never in place of each other. Also UI-na-B08 (line 1874) and del-06 (line 1774). | PASS |
| UXR-B01 columns | UI-na-B01 Expected lists all four column sets in addendum order, including "Amount with currency" on Offers. | PASS |
| Part A coverage (UXR-A01 to A45) | §3.9 (lines 367 to 413) maps all 45 rows; the §A.5 minimum set of 18 items maps at line 359. All 24 auth cases exist with Steps and Expected. The A16 and redlines v2 updates (auth-04 focus order, auth-14 idle only, auth-18 step-up) match the addendum. The Part A coverage from my cross-check on `8df39255` still holds. | PASS |

## Record notes (optional, they hold nothing)

1. The test design cites the UX1 approval at sha256 `8accd894…` (line 35 and line 131), and `15092fc1` is on disk. The decision and the released Story list are the same in both, so this is a citation lag only. Owner: Chief QA, on the next edit.
2. TD-ADM-140 line 1379 still says "stub until Part B" for TD-ADM-UI-na-B14, and that case is now complete (line 1950). Owner: Chief QA, on the next edit.
3. UI-na-B09 (line 1886) says the Offers tile follows the Spec once OQ14 is answered. It doesn't yet test the interim the addendum names (B.5: the tile links to Offers unfiltered). This is the same open Spec question as Active holds 6 in the design, and the rest of UXR-B12 is covered. Owner: Chief QA, if Spec hasn't answered OQ14 before the Step 7 UI PR.

The Test design QA PASS also notes that the harness `cases.json` still lists 114 cases. That doesn't affect UI coverage, but the CI job for UI cases needs the 127-case catalog.

## Active holds

- Condition 3 can be recorded as met by Chief UI/UX in a new dated file. This file is the UI/UX QA cross-check that the approval and the CPM checklist (line 100) ask for.
- UI cases stay at SKIP until their screens land. Each UI PR still needs Dev Code QA, then Security, then Chief UI/UX approval with a UI/UX QA PASS on the built UI, then the Bot Manager merge.
- Condition 4 still holds the Step 7 PR (Withdrawn) and the Step 9 PR (edit paths and Expire), each under its own Dev Code QA PASS.
- The S-A12 current-password question (OQ16) holds only the S-A12 password PR and the password-change parts of auth-10 and auth-13.
- The audit viewer rows (UXR-D12, D13, D16) follow the step-15 Story gate.
