import type { Page } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';

/**
 * The accessibility gate of the signed-out admin page suites (e2e/bootstrap,
 * e2e/sign-in, e2e/enrol).
 *
 * It runs axe-core against the page as it stands (WCAG 2.0, 2.1 and 2.2,
 * levels A and AA) and fails on any serious or critical finding. The full
 * report is kept next to the screenshots when an artifacts folder is set.
 *
 * Each suite installs its own packages, so the suite hands in its own
 * AxeBuilder and expect; this file imports only Node's own modules at run
 * time (the Playwright import above is a type).
 */
export function seriousAxeFindingsCheck(suite: {
  AxeBuilder: new (options: { page: Page }) => any;
  expect: (value: unknown, message?: string) => any;
  artifacts?: string;
}): (page: Page, name: string) => Promise<void> {
  return async (page, name) => {
    const results = await new suite.AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
      .analyze();

    if (suite.artifacts) {
      const dir = path.join(suite.artifacts, 'axe');
      fs.mkdirSync(dir, { recursive: true });
      fs.writeFileSync(path.join(dir, `${name}.json`), JSON.stringify(results, null, 2));
    }

    // The scan must have looked at the page: a run that checked nothing is not a pass.
    suite.expect(results.passes.length, `axe checked nothing for ${name}`).toBeGreaterThan(0);
    const blocking = results.violations
      .filter((v: any) => v.impact === 'serious' || v.impact === 'critical')
      .map((v: any) => ({ id: v.id, impact: v.impact, nodes: v.nodes.map((n: any) => n.target.join(' ')) }));
    suite.expect(blocking, `axe serious/critical findings for ${name}`).toEqual([]);
  };
}
