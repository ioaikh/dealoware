# Admin dashboard UI tests (Step 10 delete)

Playwright Chromium, headless. Local instance only — never `admin.core.dealoware.com`.

Signed-in shells (HTML/JS/CSS) are served after the session gate (route note r3 `b079a814`). Only `/admin/auth/` static files may load signed out.

## One command

From this directory:

```bash
npm install
npx playwright install chromium
npm test
```

`playwright.config.ts` starts a local API on `http://127.0.0.1:5055` with `ADMIN_UI_TEST_SEED=1`. Specs read `/tmp/dealoware-admin-ui-seed.json`.

Every spec runs axe-core and writes `test-results/axe/<case-id>.json`. Serious or critical violations fail the run. SKIP is never treated as PASS.

## Case IDs

`TD-ADM-UI-del-01` … `TD-ADM-UI-del-06`, `TD-ADM-130`, `TD-ADM-131`.
