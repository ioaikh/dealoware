# Product QA: test design QA on the TD-ADM-UI-auth-04 "Continue" amendment

| Field | Value |
|-------|-------|
| Role | Senior Product QA (test design QA), independent of the Chief QA author |
| Date | 2026-10-05, about 9:40pm ET |
| Amendment | `/workspace/qa/2026-10-05__qa__amendment__test-design-core-admin-auth04-continue.md` |
| Amendment sha256 | `fee52d75231304f41d7b3f6be7b3f87c48dca80117824b9fbccd20dc6becbb7b` (matches) |
| Frozen tip | `/workspace/dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md` sha256 `7bb1dd3b24a630dfca10c3b96969aa1f7d4815108d83fbc497475e1a1864a26f` (matches, unchanged) |
| Playwright seed | `/workspace/qa/harness/admin-dashboard/playwright/td-adm-ui-auth-04.spec.ts` sha256 `d9f9983f608db79ac4b033a50855c3b1ee6f3aa2d3a80a160888253b03e3ca48` |
| UX addendum | `/workspace/dealoware-kb/ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` sha256 `7aa374e9…` |
| Overall | **PASS** |
| PoC | $0 |

## Checks

| # | Check | Verdict | Evidence |
|---|-------|---------|----------|
| 1 | Hashes | PASS | Amendment matches `fee52d75…`. The tip still hashes to `7bb1dd3b…`, so the tip file was not rewritten. |
| 2 | Only the S-A1 primary label changes | PASS | The amendment changes one Expected phrase in TD-ADM-UI-auth-04: S-A1 tab order now ends in 'Continue' before 'Forgot your password?'. Step 6 ("without pressing Sign in") is S-A2 and correctly stays. S-A2 tab order stays code field, 'Sign in', 'Use a recovery code instead', 'Back to sign in'. No other case IDs change, and TD-ADM-UI-auth-21 is untouched. |
| 3 | Matches the binding UX copy | PASS | Addendum UXR-A13 says: Primary button "Continue" on S-A1. The UX story map row for UXR-A13 also says "Continue" and maps to TD-ADM-UI-auth-04. S-A2 "Sign in" matches UXR-A16. |
| 4 | Playwright seed asserts the change | PASS | It finds the role button with the exact name "Continue" and asserts it is visible. It asserts that no button named exactly "Sign in" exists on S-A1. It runs only on a local or CI host and never calls admin.core. The S-A1 heading "Sign in" is a heading, not a button, so the zero-count assert won't false-fail on it. |
| 5 | Harness stub cites the amendment | PASS | `cases/p0/td-adm-ui-auth-04__keyboard-only-sign-in-2fa.md` has an "Amendment 2026-10-05 (Continue)" section that points to this amendment and the tip. |
| 6 | Earlier passes unaffected | PASS | TD-ADM-050 (401), TD-ADM-051 (429, no Retry-After), OQ1, OQ3, OQ5 and OQ7, Core API H4, the A16 two-step cases and the 127-case count are all outside the amendment and unchanged. |
| 7 | Wording rule | PASS | The retired hold phrase is absent from the amendment and the seed. A PCRE scan found no word repeated more than 5 times in a row. |

## Gaps (none blocking)

| Severity | Gap | Fix |
|----------|-----|-----|
| Minor | Addendum UXR-A08 (focus order) still says S-A1 tab order ends in "Sign in". The source therefore still conflicts with UXR-A13 and with this amendment. A developer who follows A08 would fail the new Playwright assert. | Chief UI/UX makes a one-word fix in UXR-A08 ("Sign in" becomes "Continue" for S-A1 only), then the addendum is re-hashed. The amendment already assigns this to Spec and UI/UX. |
| Info | Chief UI/UX's decision arrived as a CPM relay. I found no written decision file. | Once Chief UI/UX writes the decision down, cite its path and hash in the Docs PR. |

## Active holds

- Chief QA sends this path and hash to CPM for the Docs PR. Chief Docs then cites the amendment path and hash.
- UXR-A08 still needs the one-word fix from Chief UI/UX before the auth UI PR is checked against the addendum.
- The UI cases stay skipped until the auth screens land.
- Deploy waits for Ivan's OK. There are no live admin.core calls.

## Stamp

**PASS.** Amendment `fee52d75…` against frozen tip `7bb1dd3b…` correctly sets the S-A1 primary button to "Continue" and keeps S-A2 as "Sign in". It matches UXR-A13, and the Playwright seed asserts it.
