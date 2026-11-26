import { AxeBuilder } from '@axe-core/playwright';
import { expect } from '@playwright/test';
import { seriousAxeFindingsCheck } from '../signed-out/axe';

/**
 * Runs axe-core against the page as it stands and fails on any serious or
 * critical finding. The check itself is shared (e2e/signed-out/axe.ts); this
 * suite hands it its own packages and its artifacts folder.
 */
export const expectNoSeriousAxeFindings = seriousAxeFindingsCheck({
  AxeBuilder,
  expect,
  artifacts: process.env.RESET_E2E_ARTIFACTS,
});
