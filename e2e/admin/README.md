# Admin UI CI — what this folder holds

This folder holds the "SKIP is not PASS" check of Admin UI CI and its
negative control. The browser tests themselves are not here: they live in one
folder per suite next to this one (such as `e2e/bootstrap`, `e2e/sign-in` and
`e2e/enrol`), built on the shared runner in `e2e/signed-out`.

## References

- **Epic:** UX1-X08
- **Task:** TD-ADM-UI-ci-01
- **Decision:** `docs/qa/2026-10-05__qa__decision__ux1-x08-headless-ui-ci.md`
- **Workflow:** `.github/workflows/admin-ui-ci.yml`

## What CI runs

On every pull request to `main` (its jobs are required checks there), one job
per browser engine (Chromium, Firefox, WebKit) builds the application
and runs:

```bash
bash e2e/run-signed-out.sh --forbid-only
```

That script finds every suite built on the shared runner (each
`e2e/<suite>/run.sh` that sources `e2e/signed-out/harness.sh`) and runs them
one after the other. A new suite folder is picked up without a workflow edit.

By hand, from the repository root:

```bash
./e2e/run-signed-out.sh                           # every suite, three engines
E2E_ENGINES="chromium" ./e2e/run-signed-out.sh    # every suite, one engine
./e2e/sign-in/run.sh                              # one suite
```

## Fail closed

1. **A failing test fails the job.** So does a suite that collected no test,
   and a run that found no suite at all.
2. **SKIP is not PASS (AC-06).** After each suite, `ci/assert-executed.js`
   reads Playwright's JSON report of each engine's run (never what was
   printed) and fails the run unless at least one test was executed (passed +
   failed + flaky). Red: zero tests collected, and every test skipped
   (`test.skip`, `test.fixme`, `test.skip(condition)`). Green: a normal
   passing run, and a mixed run with some skips if at least one test executed.
3. **The check is itself checked.** In every job the negative control
   recaptures real reports from `ci/fixture-src/` and re-runs the script on
   them (`ci/capture-fixtures.js`, `ci/assert-executed.control.js`).
4. No `continue-on-error`, `|| true` or `if: always()` can turn a red run
   green.

## Security — public repository

**This repository is public.** GitHub Actions logs and artifacts are
world-readable.

- **No secrets.** The workflow reads none. The passwords, keys and tokens of
  a run are made up when it starts and are gone when it ends. Do NOT put real
  CoreOwner or admin passwords, TOTP seeds or Turnstile keys into this
  repository's Actions secrets.
- **Nothing leaves the machine.** The pages are opened under the admin host's
  name, but every browser connection goes through a proxy on `127.0.0.1` to
  the application the run started, and the suites answer Cloudflare's
  requests themselves. Each suite's Playwright configuration refuses to load
  with a proxy that is not `http://127.0.0.1:<port>`
  (`e2e/signed-out/require-proxy.cjs`, checked by its `require-proxy.check.cjs`
  and `playwright.base.check.cjs` before any browser starts).
- **No uploads.** Playwright traces, screenshots and video are off
  (`e2e/signed-out/playwright.base.cjs`). The screenshots and axe reports the
  suites save stay in each suite's `test-results/` folder; the workflow
  uploads nothing.

## Files

| Path | What it is |
|---|---|
| `ci/assert-executed.js` | Reads a Playwright JSON report; exits 1 unless a test was executed. Prints counts only. |
| `ci/assert-executed.control.js` | The negative control (`node --test`), run in CI. |
| `ci/capture-fixtures.js` | Recaptures the fixture reports with the pinned Playwright. |
| `ci/fixture-src/`, `ci/fixtures/` | The throwaway specs and the reports captured from them. |
