# Chief UI/UX decision: S-A1 button label and derived focus orders

Date: 2026-10-05, 9:40pm ET
Owner: Dealoware Chief UI/UX
Points back to: UX1 approval `ux/2026-10-05__ux__approval__ux1-admin-requirements.md` (frozen, sha256 15092fc1). That file is not changed.

## Decision 1: S-A1 primary button label
- The S-A1 primary button is labeled "Continue". Step 1 does not sign anyone in yet, so "Sign in" would be inaccurate.
- S-A2 keeps "Sign in".
- UXR-A13 already says "Continue". Senior UI/UX changes UXR-A08 to match, which is a one-word edit.
- Test design: amendment `/workspace/qa/2026-10-05__qa__amendment__test-design-core-admin-auth04-continue.md` (sha256 fee52d75) changes TD-ADM-UI-auth-04 to match. It is checked against test design `7bb1dd3b`.

## Decision 2: Focus orders that Senior UI/UX worked out
- The addendum spells out focus order only for the auth screens. For the lists, the audit log and the record detail page, I accept the orders Senior UI/UX worked out from the B and D rows in checklist `ux/2026-10-05__ux__checklist__ux1-story-uxr-map.md` (5636c382) as the expected orders.
- If the built layout differs, Tab follows the built visual order (UXR-D07). UI/UX QA records each difference in the PR verification file.
- A focus order that breaks a logical reading order, or loses focus, still gets bounced (WCAG 2.2 SC 2.4.3 and 2.4.7).

## Active holds
- Both items come off the checklist's Active holds list once UI/UX QA's PASS on checklist 5636c382 or later lands.
