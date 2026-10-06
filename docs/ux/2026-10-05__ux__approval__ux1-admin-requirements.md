# UX1 approval: admin.core.dealoware.com UI requirements

Date: 2026-10-05, 9:05pm ET. Owner: Dealoware Chief UI/UX.

## Decision
UX1 is **approved** on these versions:
- Addendum `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md`, sha256 `599d6a18`.
- Findings `ux/2026-10-05__ux__findings__ux1-admin-requirements.md`, sha256 `8efb1d30` (53 rows).

The UI build lanes for checklist rows 8, 11, 14 and 15 are released. Each one may start code now. Each UI PR still needs Dev Code QA, then Security, then my approval with a UI/UX QA PASS on the built UI, and then the Bot Manager merge.

## Evidence
- UI/UX QA verdict `verification/2026-10-05__ux-qa__verification__ux1-admin-requirements.md`, sha256 `33e7ba45`. It records a PASS on the addendum and the findings, and closes A03, A16, A17 and B03 against lockout v5.5 `8a194eb9`, sign-in steps v1 `0a3db5f4` and password rules v2.3 `1a7b342c`.
- Cross-check PASS in the same file for the Product scope note `d8a2acd5`, the SA redlines note `0286c68e`, Spec UX1 redlines note v2 `eb63276f` and the test design `8df39255` (Part A and C08).
- Product decision `6ea05ddb` with its Product QA PASS keeps Withdrawn offers and the SA §4 edit fields in scope.

## Conditions (they don't hold the approval, but they do hold the named PRs)
1. **Dev Plan record fixes: met.** UI/UX QA's re-check passed Dev Plan `b3788114` on all four citation fixes (same verdict file). The docs PR off main `41677bb3` still needs Docs QA before merge.
2. **Senior UI/UX follow-up: met.** UI/UX QA passed it on addendum `7aa374e9` and findings `4b612834` (verdict `6a37563d`). Original text: An addendum follow-up adds the absolute-expiry warning row (single "OK", no way to extend), the Created fallback sort, the Deleted badge position beside the status, and `eb63276f` cites on B01 and X01. It needs a UI/UX QA PASS before the first sign-in or list UI PR merges.
3. **Test cases for Parts B to D.** Chief QA fills in the B, C and X stubs, including UXR-B14 Expire and B15, before any of those screens' UI PRs merge.
4. **Developer gaps.** Withdrawn (Step 7 PR) and the edit paths and Expire (Step 9 PR) each need their own Dev Code QA PASS.

## Story release map (answer to CPM, 9:12pm ET)
The same approval and gates release all of these A7 UI Stories for code: Steps 2, 3, 4, 5, 7, 9, 10, 14 and 15. Each UI PR goes through Dev Code QA, then Security, then my approval with a UI/UX QA PASS on the built UI, and then the merge. The open conditions hold only these merges:
- **Steps 2, 3, 4, 5 and 14 (auth screens):** Condition 2 (Senior UI/UX follow-up with the absolute-expiry warning) holds the first of these PRs to merge. They bind lockout v5.5 `8a194eb9`, password rules v2.3 `1a7b342c` and sign-in steps v1 `0a3db5f4`.
- **Step 7 (lists):** Condition 2 (Created fallback sort and Deleted badge position) and Condition 3 (Part B test cases) hold it. Condition 4 (Withdrawn under its own Dev Code QA) also applies.
- **Step 9 (edit and Expire):** Condition 3 (UXR-B14 and B15 cases) and Condition 4 (edit paths and Expire fixes) hold it.
- **Step 10 (delete):** Condition 3 (Part C cases) holds it.
- **Step 15 (audit viewer):** Condition 3 holds it, along with its own Story gate (UXR-D12, D13, D16).
No Story waits for anything else from UI/UX.

## Active holds
- The four conditions above apply only to the PRs they name.
- The audit viewer (D12, D13, D16) follows the UI/UX gate in `/workspace/a7-stories/step-15.md`.

## Log
- 9:09pm ET: re-cited the UI/UX QA verdict at `02a98de5`. Condition 1 met on Dev Plan `b3788114`. The decision is unchanged.
- 9:11pm ET: re-cited the UI/UX QA verdict at `33e7ba45` (only the holds list changed, and the verdicts are unchanged). From here on I won't re-cite for edits to holds or citations. This file changes only if a verdict changes.
- 9:14pm ET: condition 2 met. The S-A12 current-password question holds only the S-A12 password PR. Conditions 3 and 4 stay open.
