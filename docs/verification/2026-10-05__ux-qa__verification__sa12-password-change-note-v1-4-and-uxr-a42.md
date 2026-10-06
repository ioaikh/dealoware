# UI/UX QA cross-check: S-A12 password-change Spec note v1.4 and UXR-A42

| Field | Value |
|---|---|
| Verifier | Dealoware UI/UX QA (independent of Senior UI/UX, Chief UI/UX and the Spec team) |
| Date | 2026-10-05, checked 9:46pm to 9:48pm ET |
| Spec note | `specs/2026-10-05__spec__spec__core-admin-sa12-password-change-note.md`, sha256 `df797fefa331…` (v1.4, Spec QA and Security QA PASS per Spec QA) |
| Addendum | `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md`, sha256 `c9cf35d00d31…` (the uiux-out mirror matches) |
| Asked by | Chief UI/UX, in the Dealoware UI/UX Team room |

## Verdict

**PASS.** UXR-A42 in `c9cf35d0` matches note v1.4 on every UI point, and it includes Chief UI/UX additions (a) to (c). Chief UI/UX can write the S-A12 sign-off.

## Point by point

| Point | Note v1.4 | UXR-A42 in `c9cf35d0` | Result |
|---|---|---|---|
| Field order and autocomplete | Current (`current-password`), New and Confirm (`new-password`), then code (`one-time-code`) with "Use a recovery code instead" | Same order and attributes. The code field follows UXR-A04, which covers `inputmode`. | Match |
| Step-up | Current password plus a TOTP or recovery code, checked together on submit. A session alone is never enough. | Same | Match |
| Show/Hide, paste, Turnstile | Show/Hide on all three password fields, paste never blocked, no Turnstile | Same | Match |
| Generic failure copy | "We couldn't confirm it's you. Check your current password and code." It never names the failed factor. | Identical text, same rule | Match |
| (b) After a failure | Clear Current and the code, keep New and Confirm, focus Current, polite live region with `aria-describedby`, one submit in flight | Same | Match |
| (c) Mismatch | UXR-A23 "These passwords don't match." on blur and on submit | Cites A23, and A23 has that exact text on blur and on submit | Match |
| What isn't a step-up failure | A rule reject or a mismatch doesn't count toward lockout | Shows the A22 or A23 message and is not a step-up failure | Match |
| (a) Lock | Every session ends, this one included, then S-A1 shows the UX1-A03 text with no duration and no "locked" | Same. The UX1-A03 text appears in the addendum word for word as in the note. | Match |
| Success | Every session ends, this one included. S-A1 shows "Your password has been changed. Sign in with your new password and authenticator code." The authenticator stays enrolled. | Identical text, same behavior | Match |
| Server-only points | POST with anti-forgery, `no-store`, audit event names, TOTP replay and recovery burn in the same transaction | Not UI copy, so they stay in the note. The A42 row doesn't contradict any of them. | No UI change needed |
| Addendum records | | A.4 has a "decided" row for the S-A12 password change. The one Active holds item says only the S-A12 password PR waits, on the S-A12 sign-off. The retired hold phrase appears 0 times. | OK |

## Notes (they hold nothing)

1. **For Chief QA, through the CPM:** the test design `7bb1dd3b` still lists OQ16 (whether the current password is asked) as open, and only auth-13 and auth-18 trace UXR-A42. Before the S-A12 password PR can get its UI verdict, the test design needs cases for these: the generic step-up failure with the clear, keep and focus behavior and the live region; the single submit in flight; the lock on failure landing on S-A1 with the UX1-A03 text; and success ending this session too. A small dated amendment, like `fee52d75`, would do.
2. The `/settings/security` route gets the `/admin/` prefix once that Spec note lands (CA note `f03a8c68`). That doesn't change any UI requirement here.

## Active holds

- The S-A12 password-change PR waits on Chief UI/UX's S-A12 sign-off. Its UI verdict also needs the test cases in note 1.
