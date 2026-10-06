# Admin edit UI Playwright specs (Step 9)

Chromium, headless, local instance only. Never `admin.core.dealoware.com`.

Signed-in shell assets (`/admin/ui/edit.js`, `/admin/ui/edit.css`) and edit pages (`/admin/{type}/{id}/edit`) require a CoreOwner session (route note r3 `b079a814`). Only `/admin/auth/` static files may load signed out.

## One command

```bash
dotnet test tests/Dealoware.AdminUi.Tests/Dealoware.AdminUi.Tests.csproj
```

The first run installs Playwright Chromium if needed. Axe JSON is written to `test-results/axe/` (and `/opt/cursor/artifacts/axe/` when that directory exists). Any axe violation fails the run. SKIP is never treated as PASS.

Do not set `PLAYWRIGHT_BASE_URL` or `ADMIN_BASE_URL` to a live admin host. The suite fails closed if those point at `admin.core.dealoware.com`.
