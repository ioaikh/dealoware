# Decision — UX1-X08 / UXR-D15 headless admin UI cases in CI

| Field | Value |
|-------|--------|
| Written by | Dealoware Chief QA + Chief DevOps (co-own) |
| Date | 2026-10-05 ~9:28pm ET; **AGREED** ~9:29pm ET |
| Status | **AGREED** (incl. ~9:31pm ET scope amendment) — DevOps may wire the workflow. Catalog tip `7bb1dd3b…` case `TD-ADM-UI-ci-01`. |
| Catalog | `/workspace/dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md` sha256 `7bb1dd3b24a630dfca10c3b96969aa1f7d4815108d83fbc497475e1a1864a26f` |
| DevOps proposal | `/workspace/devops-out/ux1-x08-headless-ui-ci-proposal-2026-10-05.md` |
| Harness | `/workspace/qa/harness/admin-dashboard/` |
| PoC | $0 |

## Decision

**Yes — headless UI cases run in CI** on every PR that touches admin UI files; a failing non-SKIP UI case fails the check and blocks merge. Brief 4 “Playwright optional” is overruled for the **PR gate** by UX1-X08 / UXR-D15 / TD-ADM-UI-ci-01 (optional only means unrelated non-admin PRs skip the job). **No live `admin.core.dealoware.com`.**

Ownership: **DevOps** = workflow YAML, path filters, deny-live guard, secret-name wiring, artifacts. **QA** = case selection, harness stubs, PASS/SKIP criteria. No deploy / SQL / adminHost from this decision.

## Agreed job shape (from DevOps proposal)

1. **Triggers:** `pull_request` on `ioaikh/dealoware` with path filters for admin UI (exact globs with Chief Dev once Story paths land — roughly `**/Admin*/**`, `**/admin*/**`, `**/Areas/Admin/**`, plus Playwright/harness under admin-dashboard). Also `workflow_dispatch`. Skip non-admin PRs.
2. **Runner / browsers:** GitHub Actions `ubuntu-latest`; Playwright **Chromium only** on PR. Firefox/WebKit optional later nightly (out of tonight).
3. **No live admin.core:** `ADMIN_BASE_URL` / `PLAYWRIGHT_BASE_URL` = ephemeral local stub or in-job preview. Job **fails closed** if host is `admin.core.dealoware.com` (or admin `*.dealoware.com`) unless `ALLOW_LIVE_ADMIN_CORE=1` — and that flag must **never** appear in the PR workflow YAML.
4. **Secrets by env name only** (GH Actions secrets → env; never values in logs/YAML):
   - `ADMIN_BASE_URL` (stub/local only; alias `PLAYWRIGHT_BASE_URL` / harness `ADMIN_UI_BASE_URL`)
   - `ADMIN_USER` (CoreOwner email; harness also accepts `COREOWNER_EMAIL`)
   - `ADMIN_PASSWORD` (harness also accepts `COREOWNER_PASSWORD`)
   - `COREOWNER_PASSWORD_ENV` / `TOTP_SEED_ENV` if harness uses indirection (`COREOWNER_TOTP_SEED`)
   - `TURNSTILE_TEST_SITE_KEY` / `TURNSTILE_TEST_SECRET_KEY` (Cloudflare **test** keys only)
5. **Artifacts:** upload `playwright-report/`, `test-results/` (+ traces on failure); retention ~7–14d. Fail the check on any failed case in the CI subset.
6. **Binding scope (design §4.19 / TD-ADM-UI-ci-01):** every non-SKIP `TD-ADM-UI-*` case plus `TD-ADM-130` and `TD-ADM-131` runs headless on every admin-UI-touching PR; fail CI on fail; SKIP stubs stay SKIP (not PASS) with Active hold reason; axe JSON in artifacts; **no live admin.core**.
7. **Interim until Playwright specs exist:** a smoke subset tagged `ui-ci` / gate `TD-ADM-UI-ci-01` may run first (harness is stubs-only today). Once specs land, the PR gate **must expand** to the full non-SKIP UI catalog — smoke is not permanent.

## Phase 0 interim subset (wire now — expands as specs land)

| ID | Intent |
|----|--------|
| TD-ADM-UI-ci-01 | Job triggers; broken case → red; SKIP ≠ PASS; artifacts retained; path-filter + throwaway red acceptance |
| TD-ADM-131 | Unauthenticated admin pages deny / sign-in |
| TD-ADM-130 | End-to-end UI smoke (when screens + specs exist) |
| TD-ADM-UI-auth-01 | Failure copy parity (401 vs 429); no lock logic in UI |
| TD-ADM-UI-auth-21 | Two-step sign-in layout; pending token never in URL |
| TD-ADM-UI-auth-22 | Same generic sentence on both steps (deny path) |
| TD-ADM-UI-auth-17 | Sign out POST; GET `/sign-out` no-op; Back shows no entity data |
| TD-ADM-UI-auth-20 | axe-core on S-A screens: zero serious/critical |

Until those Playwright specs exist, the job may run a harness smoke that **fails closed** if the suite is empty or if the live-host guard trips — never a green empty job.

## Acceptance for the DevOps wiring PR

1. **Path-filter negative control:** a PR that touches only non-UI files does **not** need the UI job.
2. **Throwaway broken-case red check:** break one UI case target on a throwaway branch → job red / merge-blocking (design steps 2–3).
3. SKIP ≠ PASS; axe JSON retained; deny-live host guard.

## Phase 1–2 (as UI PRs + Playwright specs land)

Every remaining non-SKIP `TD-ADM-UI-auth-*`, `TD-ADM-UI-del-*`, `TD-ADM-UI-na-*` plus TD-ADM-130/131 per catalog. SKIP stubs stay SKIP with Active-hold reason. Gate must not stay smoke-only after specs exist.

## Amendments from QA (folded into AGREE)

1. Env aliases above so harness and workflow names both work.
2. Phase 0 case IDs listed explicitly (not only “auth gate + happy + deny”).
3. Empty green job forbidden.
4. **2026-10-05 ~9:31pm ET amendment (Chief DevOps + Chief QA AGREE):** binding end-state = full non-SKIP UI catalog + 130/131; smoke interim only; path-filter negative + throwaway red as wiring-PR acceptance; OQ10 Brief 4 optional overruled for admin-UI PRs — Chief QA asks Chief Dev/CPM to align the SD brief.

## Active holds

- Workflow file lands after this AGREE (DevOps owns).
- Screens not built → those cases SKIP.
- Deploy / live smoke waits Ivan OK.

