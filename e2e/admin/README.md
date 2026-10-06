# Admin UI End-to-End Tests

End-to-end tests for the Dealoware Admin UI using Playwright.

## References

- **Epic:** UX1-X08
- **Task:** TD-ADM-UI-ci-01
- **Decision:** Chief QA AGREED — Brief 4 Playwright optional overruled for admin-UI PR gate
- **Route note r3** (`b079a814`; r2 is void): admin pages live under `/admin/...`. Spec §5 allowed return paths for this viewer are `/admin/audit` and `/admin/audit/{id}` (GUID). Signed-in shell assets stay behind the session gate.

## Specs in this PR

`audit-viewer.spec.ts` covers the CoreOwner audit viewer (S-D1 / S-D2):

| Case ID | What it asserts |
| --- | --- |
| TD-ADM-UI-na-X02 | S-D1 columns, action/entityType/date filters, paging, URL state, S-D2 snapshots, no mutate |
| TD-ADM-UI-na-X01 | Built audit routes are `/admin/audit` and `/admin/audit/{guid}` |
| TD-ADM-UI-na-axe | axe-core on S-D1/S-D2 states; JSON written under `e2e/artifacts/axe/` |
| TD-ADM-UI-na-kbd | Keyboard walkthrough list → filter → detail → back |
| TD-ADM-UI-na-B15 | ET timestamp label + UTC tooltip |
| TD-ADM-101 step 2 | Read-only S-D2 inspection; secrets and raw IPs absent |
| TD-ADM-131 | Unauthenticated `/admin/audit` and shell assets are 401 with no entity data |

Every Playwright UI spec runs axe and writes a JSON file. Any violation fails the run. SKIP is not used (SKIP never counts as PASS).

The local host is `http://127.0.0.1:4173` with `Host: admin.core.dealoware.com`. Specs never call `https://admin.core.dealoware.com`.

`GET /admin/api/audit` is a test-host stub bound to Step 8 PR #32 @ `db22afba`
(`items`, `total`, `offset`, `limit` default 50 / max 200, `ipHmacPrefix` only,
no mutating controls). After #32 merges, rebase onto main and drop the stub.

## CI Behavior

The `admin-ui-ci.yml` workflow enforces:

1. **Fail-closed on pull_request:** If no Playwright specs exist (no `*.spec.*` or `*.e2e.*` files), the CI job fails.

2. **Allow-list deny-live guard:** Tests can ONLY run against `localhost`, `127.0.0.1`, or `::1`. Any other hostname — including `*.dealoware.com`, live IPs, or external hosts — is rejected.

3. **Secrets scoping:** Credentials are only exposed to the test step, never to `npm install` or other steps.

## Security — Public Repository

**This repository is public.** GitHub Actions artifacts and logs are world-readable.

**Do NOT put real CoreOwner/admin passwords or TOTP seeds into this repo's Actions secrets.**

Playwright traces and screenshots stay **off**. The workflow does **not** upload reports.

## Local Development

```bash
cd e2e
npm ci
npx playwright install chromium
npm test
```

The Playwright `webServer` starts `tests/Dealoware.AdminUi.Host` on `http://127.0.0.1:4173`.
