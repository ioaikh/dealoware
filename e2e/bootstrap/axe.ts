import { AxeBuilder } from '@axe-core/playwright';
import { expect, type Page } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';

/**
 * Runs axe-core against the page as it stands (WCAG 2.0, 2.1 and 2.2, levels
 * A and AA) and fails on any serious or critical finding. The full report is
 * kept next to the screenshots when an artifacts folder is set.
 */
export async function expectNoSeriousAxeFindings(page: Page, name: string): Promise<void> {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();

  const artifacts = process.env.BOOTSTRAP_E2E_ARTIFACTS;
  if (artifacts) {
    const dir = path.join(artifacts, 'axe');
    fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(path.join(dir, `${name}.json`), JSON.stringify(results, null, 2));
  }

  // The scan must have looked at the page: a run that checked nothing is not a pass.
  expect(results.passes.length, `axe checked nothing for ${name}`).toBeGreaterThan(0);
  const blocking = results.violations
    .filter((v) => v.impact === 'serious' || v.impact === 'critical')
    .map((v) => ({ id: v.id, impact: v.impact, nodes: v.nodes.map((n) => n.target.join(' ')) }));
  expect(blocking, `axe serious/critical findings for ${name}`).toEqual([]);
}
