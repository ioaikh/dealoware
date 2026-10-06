# Admin UI End-to-End Tests

End-to-end tests for the Dealoware Admin UI using Playwright.

## References

- **Epic:** UX1-X08
- **Task:** TD-ADM-UI-ci-01
- **Decision:** Chief QA AGREED — Brief 4 Playwright optional overruled for admin-UI PR gate

## Phase 0 Status

No admin UI frontend exists yet. This directory contains the pipeline wiring and configuration for when specs are added. The workflow:

- Runs on PRs that touch `e2e/admin/**`, `e2e/playwright.config.ts`, `e2e/package.json`, or the workflow file
- Fails closed when no specs exist (required for merge-blocking)
- Is ready to run real tests once Playwright specs are added

## CI Behavior

The `admin-ui-ci.yml` workflow enforces:

1. **Fail-closed on pull_request:** If no Playwright specs exist (no `*.spec.*` or `*.e2e.*` files), the CI job fails.

2. **Deny-live guard:** Tests will never run against `admin.core.dealoware.com` or any `admin*.dealoware.com` host. The workflow exits non-zero if a live admin host is detected.

3. **Secrets scoping:** Credentials are only exposed to the test step, never to `npm install` or other steps.

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

## App Start

### When Admin UI Exists

When a buildable admin UI frontend is added:

1. Add a build/serve script to `e2e/package.json` or the admin UI package
2. Configure `webServer` in `e2e/playwright.config.ts`:

```typescript
webServer: {
  command: 'npm run preview',
  url: 'http://127.0.0.1:4173',
  reuseExistingServer: !process.env.CI,
  timeout: 120000,
},
```

3. Tests will auto-start the server before running

### Local Development (Phase 0)

Until webServer is configured, start the app manually:

```bash
# Terminal 1: Start the API
cd src/Dealoware.Api && dotnet run

# Terminal 2: Run tests
cd e2e && npm test
```

## Environment Variables

The CI workflow maps these secrets (configure in GitHub repository settings). Test-only placeholder values are used if secrets are not configured:

| Variable | Description | CI Default |
|----------|-------------|------------|
| `ADMIN_BASE_URL` | Base URL for admin UI | `http://127.0.0.1:4173` |
| `ADMIN_USER` | Admin username | `ci-test-admin` |
| `ADMIN_PASSWORD` | Admin password | placeholder |
| `COREOWNER_PASSWORD_ENV` | Core owner password | placeholder |
| `TOTP_SEED_ENV` | TOTP seed for 2FA | test seed |
| `TURNSTILE_TEST_SITE_KEY` | Turnstile test site key | Cloudflare test key |
| `TURNSTILE_TEST_SECRET_KEY` | Turnstile test secret | Cloudflare test secret |

## Local Development

```bash
cd e2e

# Install dependencies
npm ci

# Install Chromium browser
npx playwright install chromium

# Run tests (start app first, see above)
npm test

# Run with headed browser
npm run test:headed

# View report
npm run report
```
