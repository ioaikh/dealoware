# A7 Step 14, piece 5 — the admin browser tests in CI: review

| Field | Value |
|-------|-------|
| Story | A7 Step 14, Core admin auth screens. This piece meets the Story's rule that headless UI cases run in CI (UX1-X08). |
| Branch | `claude/t03-admin-ui-ci` |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below |

## How this was reviewed

A reviewer who did not write the change read it adversarially against `main`, and read the logs of its first run on GitHub. The builder and the reviewer were separate Claude Code agents. A third pass made the fixes and re-ran the checks.

## What the change does

- `e2e/run-signed-out.sh` finds every suite on the shared runner and runs them one after another. It fails if any suite fails, if no suite is found, if a suite collects no tests, or if every test is skipped.
- `.github/workflows/admin-ui-ci.yml` runs it on every pull request that touches `src/**`, `e2e/**` or the workflow. There is one job per engine (Chromium, Firefox, WebKit), on `ubuntu-24.04`.
- The old always-skipped stub spec and its configuration are removed. What the stub would have checked is asserted in `e2e/sign-in`.

## First run on Linux

| Engine | Result | Setup page | Enrolment pages | Sign-in pages |
|--------|--------|------------|-----------------|---------------|
| Chromium | passed, 3 min 22 s | 30 run | 25 run | 41 run |
| Firefox | passed, 3 min 54 s | 30 run | 25 run | 41 run |
| WebKit | passed, 4 min 00 s | 30 run | 25 run | 41 run |

No test was skipped, failed or flaky. The check that "skipped is not passed" ran in every job and gave the expected results. No application process was left behind.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Low | An engine list of only spaces ran no engine and passed. CI cannot send one, but the script failed open. | Fixed. The entry point and the shared runner refuse it. |
| 2 | Low | A port that answered was taken as ours even if our own process had died and another one held the port. | Fixed. It counts only while our process is alive. |
| 3 | Low | If the runner image lacked .NET 10, the workflow downloaded an install script without checking it, and took the latest patch. | Fixed. The job fails instead. |
| 4 | Low | The older confirm-page suite, `e2e/reset-confirm`, is not run in CI. | Open. The reset-pages piece replaces or moves it. |
| 5 | Note | No status check is required on `main`, so a red run does not block a merge. | Changing that is a repository setting for the owner. Until then a pull request is merged only when this job passes. |
| 6 | Note | The pinned `checkout` and `setup-node` commits run on Node 24 with a deprecation warning. | To move to newer pinned commits later. |

The reviewer found:
- no use of secrets;
- no `pull_request_target`;
- no uploads;
- no way for a fork's pull request to get more than read access;
- no cache-poisoning path;
- no generated key or password in the logs.

## Tests

- `AdminSignedOutBrowserRunTests` and `AdminPasswordResetTests`: 106 of 106 passed after the fixes.
- On Windows, the setup-page suite passed through the changed runner: 30 of 30, Chromium.
- The workflow YAML parses, and both scripts pass `bash -n`.
