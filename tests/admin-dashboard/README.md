# Admin dashboard UI tests (Step 10 delete)

The Playwright suite now lives under `e2e/admin/` so it runs in the
**Admin UI CI** job from PR #29 (`.github/workflows/admin-ui-ci.yml`).

```bash
cd e2e
npm ci
npx playwright install chromium
npm test -- --project=chromium --grep TD-ADM-UI-del
```

Local instance only — never `admin.core.dealoware.com`. Signed-in shells stay
behind the session gate (route note r3 `b079a814`).
