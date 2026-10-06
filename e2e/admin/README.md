# Admin UI End-to-End Tests

End-to-end tests for the Dealoware Admin UI using Playwright.

## References

- **Epic:** UX1-X08
- **Task:** TD-ADM-UI-ci-01
- **Decision:** Chief QA AGREED — Brief 4 Playwright optional overruled for admin-UI PR gate

## Status

Step 14 auth-screen specs live here (`td-adm-ui-auth*.spec.ts`). The workflow starts the local API (`webServer` in `playwright.config.ts`) and runs Chromium headless against `127.0.0.1` only.

## CI Behavior

The `admin-ui-ci.yml` workflow enforces:

1. **Fail-closed on pull_request:** If no Playwright specs exist (no `*.spec.*` or `*.e2e.*` files), the CI job fails.

2. **Allow-list deny-live guard:** Tests can ONLY run against `localhost`, `127.0.0.1`, or `::1`. Any other hostname — including `*.dealoware.com`, live IPs, or external hosts — is rejected. The guard uses Node.js URL parsing and never echoes URL values.

3. **Secrets scoping:** Credentials are only exposed to the test step, never to `npm install` or other steps.

## Security — Public Repository

**This repository is public.** GitHub Actions artifacts and logs are world-readable.

### Credentials Policy

**Do NOT put real CoreOwner/admin passwords or TOTP seeds into this repo's Actions secrets.**

The workflow uses test-only placeholder values:
- `ci-test-admin` / `ci-test-placeholder-not-real` for admin credentials
- Cloudflare's public Turnstile test keys
- A dummy TOTP seed

These placeholders are intentional. Real credentials would be exposed in logs or artifacts.

### Traces and Screenshots

Playwright traces and screenshots are **disabled** in `playwright.config.ts`:
- `trace: 'off'`
- `screenshot: 'off'`

The workflow does **not** upload `playwright-report/`, `test-results/`, or traces. These could contain login screens, session cookies, or other sensitive data.

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

The CI workflow uses these test-only placeholder values. **Do not override with real credentials in a public repo.**

| Variable | Description | CI Placeholder |
|----------|-------------|----------------|
| `ADMIN_BASE_URL` | Base URL for admin UI | `http://127.0.0.1:4173` |
| `ADMIN_USER` | Admin username | `ci-test-admin` |
| `ADMIN_PASSWORD` | Admin password | `ci-test-placeholder-not-real` |
| `COREOWNER_PASSWORD_ENV` | Core owner password | `ci-test-placeholder-not-real` |
| `TOTP_SEED_ENV` | TOTP seed for 2FA | dummy test seed |
| `TURNSTILE_TEST_SITE_KEY` | Turnstile test site key | Cloudflare public test key |
| `TURNSTILE_TEST_SECRET_KEY` | Turnstile test secret | Cloudflare public test secret |

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

# View report (local only — not generated in CI)
npm run report
```
