# A7 Step 2 — Core admin first-time setup (bootstrap): code and security review

| Field | Value |
|-------|-------|
| Story | [A7 Step 2](../plans/a7-stories/2026-10-05__pm__note__core-admin-dashboard-a7-step-02.md) |
| Branch | `claude/t02-admin-bootstrap`, three commits on `main` at `db49f16` |
| Replaces | Draft pull request #36, which conflicted with `main` in 16 files and was partly superseded |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below |

## How this was reviewed

The agent teams named in the Story's gates are not running. The owner asked for the work to be finished another way, so the gates were met as follows.

| Gate in the Story | How it was met |
|-------------------|----------------|
| Dev Code QA | A reviewer who did not write the code read the whole change against `main`, built it, and ran 90 bootstrap, command and migration tests. The builder and the reviewer were separate Claude Code agents. A third pass re-read the password-set handler and re-ran the full suite. |
| Security | The same independent review was adversarial: account takeover, oracles, order of checks, throttling, session and CSRF, the one-shot command, logging, the migration. |
| UI/UX | Not applicable. This step has no screen. The setup screen (S-A4) is Step 14. |
| CI | The pull request's own run, which includes the PostgreSQL job. |

## What the change does

- Adds the bootstrap link store (hash only, 24 hours, single use) and migration `20261006000390`.
- Adds a one-shot command, `bootstrap-link`, beside `migrate`. It is the only way a link is issued. It mails the configured owner, and does nothing when the owner already has a password.
- Adds the password-set request at `/admin/bootstrap` and `/admin/api/auth/bootstrap`, and opens exactly those signed-out exemptions.
- Creates the owner's account and credential rows. Before this change no production code created them, so nobody could sign in.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important | Sign-in verified the password as typed, while bootstrap and reset store the hash of its NFKC form. A password with an ellipsis, a no-break space or a full-width letter was accepted and then refused at every sign-in, with no way back. This fault was already on `main` for reset. | Fixed. Every verification site now uses the NFKC form. Tests cover bootstrap then sign-in, and reset then sign-in, with such a password. |
| 2 | Minor | Two usable links could coexist if the command ran twice at once. | Fixed. Completing setup invalidates every other unused link, in the same transaction. |
| 3 | Minor | Any database error at the final write was answered as "link expired". | Fixed. Only a unique-key conflict is answered that way. Any other fault is a server error, and the link survives. |
| 4 | Minor | The audit row for an issued link carries the word `command` where a request would carry an address hash. | Kept, and the column's comment now says so. |
| 5 | Minor | Missing tests: a reset token at the bootstrap route and the reverse, an unconfigured owner, a start barrier for the parallel tests. | Added. |
| 6 | Minor | Failed Turnstile submissions are not counted and each writes an audit row. | Not changed. Test design TD-ADM-040 requires it, and sign-in on `main` behaves the same. An edge rate limit is the right place for it. |

No way was found to set or change the owner's password without a valid, unused link. Unknown, expired, used and superseded links, and a setup that is already complete, all get the same answer. No token, link, password or hash is logged or audited.

## Tests

- Full suite on Windows: 1,292 tests, 1,286 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on that machine: four need `/bin/chmod`, and one hits a locked temporary file.
- 56 tests are new.
- The PostgreSQL tests, including two parallel consumes of one link, run in CI only.

## Limits to know about

1. **Do not run `bootstrap-link` in production yet.** There is no setup screen, so a mailed link cannot be used in a browser. The screen is Step 14.
2. After setting the password the owner signs in and then enrols TOTP. Going straight from setup to enrolment (TD-ADM-UI-auth-08) belongs with the screen.
3. A rejected password takes one slot of the shared reset and bootstrap budget, as reset does on `main`. TD-ADM-UI-auth-10 says it should not.
4. The password-rules note the plan cites is not in the repository. The code follows `AdminPasswordRules`.
