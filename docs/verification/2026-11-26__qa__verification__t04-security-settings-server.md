# T04 piece 3 — security settings server actions (S-A12): review

| Field | Value |
|-------|-------|
| Scope | Server actions for the security settings page S-A12, as JSON under `/admin/api/settings/security`, and a new table `AdminSessionStepUps` (migration `20261126000400`) |
| Branch | `claude/t04-security-settings-server` |
| Rules | S-A12 password-change note v1.4; test design TD-ADM-170, 171, 172, 174 and TD-ADM-UI-auth-13, 16, 18, 19; Spec §8 |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below |

## How this was reviewed

A reviewer who did not write the code read the change adversarially, in this order:
1. takeover paths;
2. failure counting and lockout;
3. transactions and races;
4. secrets;
5. the migration;
6. the session gate.

The builder and the reviewer were separate Claude Code agents. A third pass read the fixes and ran the full suite.

## What the change does

- **`POST /password`:** checks the current password and an authenticator or recovery code at submit, then the new-password rules. Success ends every session, this one included.
- **`POST /step-up`:** records a step-up on this session that lasts 5 minutes (TD-ADM-174 steps 3 and 4). It does not carry to another session, and it goes when the session goes.
- **`POST /recovery-codes`:** needs the step-up, or the proof sent with the request. It replaces the whole set with 10 new codes of 60 bits each, stored as keyed hashes, and returns them once in the reply. One set per step-up. Every other session ends.
- **`POST /authenticator` and `/authenticator/confirm`:** each start makes a new key, kept encrypted on this session's step-up row. Confirm replaces the old key at once and issues a new set of codes. The key is taken once, and every other session ends.
- **`GET /` and `GET /activity`:**
  - `GET /` returns the page state.
  - `GET /activity` returns the owner's last 20 sign-in events and the 24-hour lock notice.
  - Neither returns secrets or IP addresses.
- **Failure counting:** every failure counts on the account counter that sign-in uses, and never on the IP throttle. A lock ends every session.
- **One transaction for each change:** the recovery-code burn or the accepted authenticator step, together with the change itself.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important | The tests that two requests at once are safe ran only on SQLite. SQLite runs whole transactions one at a time, so they proved nothing about PostgreSQL's row locks. | Fixed. The PostgreSQL CI job now runs the whole `AdminSecuritySettingsTests` class. |
| 2 | Minor | A password change spending a recovery code took the code row lock before the account row. New codes and a re-enrolment confirm took them the other way round, so on PostgreSQL the two could deadlock and one would answer 500. | Fixed. Every transaction here takes the account row lock first. A new test runs both at once for five rounds and finds no 500 and exactly one winner. |
| 3 | Note | The activity feed showed whether a failed step-up had the right password, which someone holding a stolen session could use to check guesses. The 5-failure lock limits this. | Fixed. The feed shows no reason for those two events; the audit log keeps it. |
| 4 | Note | A possible 500 if two first step-ups hit the same session at once. | Judged very unlikely, because EF Core rolls back to a savepoint. The PostgreSQL job now runs this class. |
| 5 | Note | A request cancelled after its failure slot was reserved keeps the slot and writes no audit row. | Accepted. Sign-in uses the same pattern, and it fails closed. |
| 6 | Note | No notice email is sent after these changes. TD-ADM-174 marks it SHOULD. | Open. Mail sending arrives with the email work. |

The reviewer found:
- A stolen session cookie alone cannot change a factor, read codes or a pending key, or forge or carry a step-up.
- The password alone is never enough.
- Replayed authenticator steps and spent recovery codes are refused.
- Each failure counts exactly once.
- Secrets never reach logs, audit rows, URLs, headers or GET replies.
- Every POST passes the Origin and anti-forgery checks, with no exemption.

## Known limit

The reset-confirm transaction, from an earlier step, still takes the recovery-code row before the account row. A reset confirm at the same moment as a new-codes request could therefore still deadlock. One of them would answer 500 and nothing would be written. It needs the owner to reset their password and make new codes at the same moment. It is left for a later change to the reset flow.

## Tests

- Full suite on Windows after the fixes: 1,567 tests, 1,561 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on this machine.
- The security settings class: 43 tests, all passed. The PostgreSQL job on the pull request is their first PostgreSQL run.
- The migration tests show that existing rows survive and that the previous release's queries still run.
