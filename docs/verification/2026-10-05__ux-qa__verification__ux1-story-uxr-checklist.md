# UI/UX QA verification: Story-to-UXR build review checklist

| Field | Value |
|---|---|
| Verifier | Dealoware UI/UX QA (independent of the author, Senior UI/UX, and of Chief UI/UX) |
| Date | 2026-10-05, checked 9:34pm to 9:37pm ET |
| Checklist | `ux/2026-10-05__ux__checklist__ux1-story-uxr-map.md`, sha256 `5636c382` (the uiux-out mirror matches byte for byte) |
| Addendum | `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md`, sha256 `7aa374e9` |
| Test design r5 | `qa/2026-10-05__qa__test-design__core-admin-dashboard.md`, sha256 `7bb1dd3b`, 127 case headings (UI coverage PASS in `verification/2026-10-05__ux-qa__verification__test-design-r5-ui-coverage.md`) |
| UX1 approval | `ux/2026-10-05__ux__approval__ux1-admin-requirements.md`, sha256 `15092fc1`, Story release map |

## Verdict

**BOUNCE, one record fix only. The content passes.** Chief UI/UX can accept the checklist as soon as fix 1 lands. I'll re-check that line only.

## Results

| Check | How | Result |
|---|---|---|
| Every addendum UXR row is placed | A script compared the 86 `UXR-` rows in the addendum with the checklist. None is missing. Step 8 is thin (TD-ADM-101 step 2 plus D16), and the audit viewer sits in Step 15, as the Chief briefed. | PASS |
| TD case IDs are real | A script resolved all 60 full `TD-ADM-` IDs and every short `UI-na`, `UI-auth`, `UI-del` and `UI-ci` ID cited against the 127 `####` headings in `7bb1dd3b`. All exist. B14 cites na-B10 and na-expire, B15 cites na-B11, A45 cites auth-24, and Part C cites del-01 to del-06. | PASS |
| Screen rows | Every screen section has a focus-order row naming a specific Tab order, a states row covering default, hover, focus, disabled, error, empty and loading, and a WCAG 2.2 AA row covering contrast, focus visible, target size and announcements. Each row cites its own TD cases. Where a state doesn't apply, the row says so in plain words (for example, Empty on S-B6). | PASS |
| Copy is verbatim | A script compared 77 quoted strings with the addendum. 74 match exactly. The other 3 are the UXR-A09 title template filled in ("Sign in · Dealoware admin", "Set your admin password · Dealoware admin") and one row fragment with no copy in it. | PASS |
| Merge holds match the release map | Each Story section's holds match `15092fc1`. Condition 4 is on Steps 7 and 9, condition 3 on Steps 7, 9, 10 and 15, the Story gate on Step 15, and the S-A12 current-password question only on the S-A12 password PR in Step 14. | PASS |
| Retired wording | grep for the retired hold phrase finds 0. No word repeats more than 5 times in a row. | PASS |

## Fix

1. **The Log times are in the future.** The Log entries read "9:40pm ET" (draft) and "9:55pm ET" (revision 2), but the file was on disk at `5636c382` by 9:34pm ET. Please use the real save times. Owner: Senior UI/UX.

## Notes (they hold nothing)

- Chief UI/UX decided both open items at about 9:36pm ET. The S-A1 button label is "Continue", so Senior changes UXR-A08 in the addendum and Chief QA changes TD-ADM-UI-auth-04 through the CPM. The focus orders Senior worked out for the lists, audit log and record detail are accepted. If the built layout differs, Tab follows the built visual order (D07), and each PR verification records the difference. On the next edit, the checklist should drop those two items from its Active holds and cite the new addendum hash.
- In two UXR rows (A16 and A27), the second column is a cut-off copy of the requirement ("Numbered steps:", "Helper text "Open your…"). The check column is complete, so this is cosmetic.
- All admin pages are moving under `/admin/` (CA note `architecture/2026-10-05__ca__architecture__core-admin-route-prefix-note.md`, `f03a8c68`). The checklist names screens by ID, not path, so it doesn't change. UXR-A12 and D01 check built routes against the inventory.

## Re-check, 2026-10-05 9:41pm ET

**PASS.** Revision 3 of the checklist, `d9418cab` (the uiux-out mirror matches), fixes the one record item.
- The Log now gives the real write times, about 9:30pm and 9:32pm ET. The 9:39pm line says why they changed.
- Active holds no longer list the two decided items. The "Decided" line cites addendum `f83484da`, amendment `fee52d75` and decision `8a92c820`, and all three are on disk at those hashes.
- The S-A1 focus row reads "Continue". The remaining "Sign in" mentions are correct: the S-A1 heading (A13), the S-A2 and S-A3 buttons (A08, A16, A19), and nothing else.
- The header cites `f83484da` and `8a92c820`. The retired hold phrase appears 0 times. I found no other content changes.
The checklist is ready for Chief UI/UX to accept.

## Active holds

- Chief UI/UX acceptance of checklist `d9418cab` (fix 1 is resolved and the re-check passed).
- The S-A12 current-password question holds only the S-A12 password PR.
- The audit viewer columns, filters and paging wait on Spec, and that holds only the Step 15 PR.
