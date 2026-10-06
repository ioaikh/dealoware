# Admin UI End-to-End Tests

> **Active Hold:** TD-ADM-UI-ci-01 — Admin UI Playwright specs pending

## Overview

This directory will contain Playwright end-to-end tests for the Dealoware Admin UI.

## References

- **Epic:** UX1-X08
- **Task:** TD-ADM-UI-ci-01
- **Decision:** Chief QA AGREED — Brief 4 Playwright optional overruled for admin-UI PR gate

## CI Behavior

The `admin-ui-ci.yml` workflow enforces:

1. **Fail-closed on pull_request:** If no Playwright specs exist (no `*.spec.*` or `*.e2e.*` files), the CI job fails. This ensures PRs touching admin UI files cannot merge without test coverage.

2. **Deny-live guard:** Tests will never run against `admin.core.dealoware.com` or any `admin*.dealoware.com` host. The workflow exits non-zero if a live admin host is detected.

3. **SKIP stubs:** When specs are added but marked as skipped, they report SKIP with Active hold reason (specs pending). Skipped-only suites still fail on pull_request.

## Adding Tests

Place Playwright spec files in this directory with one of these naming patterns:
- `*.spec.ts` / `*.spec.js`
- `*.e2e.ts` / `*.e2e.js`

Example:
```typescript
// e2e/admin/login.spec.ts
import { test, expect } from '@playwright/test';

test('admin login page loads', async ({ page }) => {
  await page.goto('/admin/login');
  await expect(page.locator('h1')).toContainText('Admin Login');
});
```

## Environment Variables

The CI workflow maps these secrets (configure in GitHub repository settings):

| Variable | Description |
|----------|-------------|
| `ADMIN_BASE_URL` | Base URL for admin UI (default: `http://127.0.0.1:4173`) |
| `ADMIN_USER` | Admin username for tests |
| `ADMIN_PASSWORD` | Admin password for tests |
| `COREOWNER_PASSWORD_ENV` | Core owner password |
| `TOTP_SEED_ENV` | TOTP seed for 2FA |
| `TURNSTILE_TEST_SITE_KEY` | Turnstile test site key |
| `TURNSTILE_TEST_SECRET_KEY` | Turnstile test secret key |

## Local Development

```bash
# Install Playwright
npm install -D @playwright/test

# Install Chromium browser
npx playwright install chromium

# Run tests
npx playwright test e2e/admin/
```
