# Chief UI/UX sign-off: S-A12 password change

Date: 2026-10-05, 9:49pm ET
Owner: Dealoware Chief UI/UX
Points back to: UX1 approval `ux/2026-10-05__ux__approval__ux1-admin-requirements.md` (frozen, sha256 15092fc1). That file is not changed. This file records the S-A12 current-password answer the approval was waiting on.

## Decision
I approve the S-A12 password-change UI requirements. Change password asks for the current password plus an authenticator or recovery code on submit. It uses one generic failure message, and my additions (a) to (c) cover the lock, the form reset after a failure, and show/hide on every password field.

## Evidence
- Spec note v1.4: `specs/2026-10-05__spec__spec__core-admin-sa12-password-change-note.md` (df797fef), which passed Spec QA (`verification/2026-10-05__spec__verification__core-admin-sa12-password-change-note.md`) and Security QA (`verification/2026-10-05__security__verification__core-admin-sa12-password-change-note-df797fef-confirm.md`).
- UXR-A42 in addendum `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` (c9cf35d0).
- UI/UX QA cross-check PASS: `verification/2026-10-05__ux-qa__verification__sa12-password-change-note-v1-4-and-uxr-a42.md` (fc90ab42).

## Effect
- The requirements no longer hold the S-A12 password-change PR, and code can start.
- Its merge still follows the usual path: a Dev Code QA PASS, Security, a UI/UX QA PASS on the built UI, my approval, and then the Bot Manager merge.
- The CPM can now let step-14.md keep the A40 "Manage recovery codes" link, which waits on this PR.

## Active holds
- The S-A12 password PR needs Chief QA's test design amendment, which closes OQ16 and adds the password-change cases, to pass Test design QA before UI/UX QA can give a UI verdict on the built PR.
- A record fix that holds nothing: Spec note v1.4's Status row still says "Draft for Spec QA + Security QA". I've sent it to Chief Spec.
