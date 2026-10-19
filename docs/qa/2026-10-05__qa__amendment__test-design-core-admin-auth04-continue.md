# Amendment — TD-ADM-UI-auth-04 S-A1 primary button "Continue"

| Field | Value |
|-------|--------|
| Written by | Dealoware Chief QA |
| Date | 2026-10-05 ~9:37pm ET |
| Kind | Dated one-word amendment against frozen tip (not a full r6) |
| Frozen tip | `/workspace/dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md` sha256 `7bb1dd3b24a630dfca10c3b96969aa1f7d4815108d83fbc497475e1a1864a26f` |
| Trigger | CPM relay: Chief UI/UX decided S-A1 step 1 primary button label is **Continue**, not "Sign in" |
| Binding copy | UX addendum UXR-A13: Primary button "Continue" on S-A1 |
| PoC | $0 |

## Change (only TD-ADM-UI-auth-04)

Against tip `7bb1dd3b…`, treat the S-A1 primary control label in **TD-ADM-UI-auth-04** as follows.

### Steps (tip line)

Tip: `6) Fill the full code on S-A2 without pressing Sign in.`

**Unchanged** — that sentence is S-A2; S-A2 primary remains **"Sign in"** (UXR-A16).

### Expected (tip line — amend)

Tip said:

> S-A1 Tab order email → password → Turnstile → **'Sign in'** → 'Forgot your password?'

**Amend to:**

> S-A1 Tab order email → password → Turnstile → **'Continue'** → 'Forgot your password?'

S-A2 Tab order stays: code field → **'Sign in'** → 'Use a recovery code instead' → 'Back to sign in'.

### Out of scope for this amendment

- Tip file `7bb1dd3b…` is **not** rewritten (keeps UI/UX QA condition 3 cross-check and Docs PR stable).
- No other case IDs. If UXR-A08 focus-order prose still says "Sign in" for S-A1, Spec/UI/UX own that note; this amendment binds **test expected text** to UXR-A13 / Chief UI/UX.
- TD-ADM-UI-auth-21 S-A2 "Sign in" unchanged.

## Playwright

Seed spec (QA harness): `/workspace/qa/harness/admin-dashboard/playwright/td-adm-ui-auth-04.spec.ts` — asserts S-A1 primary accessible name **Continue**.

## Harness stub

`/workspace/qa/harness/admin-dashboard/cases/p0/td-adm-ui-auth-04__keyboard-only-sign-in-2fa.md` updated to cite this amendment.

## Active holds

- Test design QA (Senior Product QA) on this amendment.
- Docs PR cite of this path+hash (Chief Docs, after CPM).
