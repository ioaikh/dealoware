import fs from 'fs';
import path from 'path';
import { expect, type Page } from '@playwright/test';

const AXE_PATH = path.resolve(__dirname, '../../../tests/Dealoware.Api.UiTests/assets/axe.min.js');
const ARTIFACT_DIR = path.resolve(__dirname, '../../../test-results/axe');

export async function scanAxe(page: Page, caseId: string): Promise<void> {
  expect(fs.existsSync(AXE_PATH), 'Vendored axe-core is required; SKIP is not a pass.').toBeTruthy();
  await page.addScriptTag({ path: AXE_PATH });
  const json = await page.evaluate(async () => {
    const axe = (window as unknown as { axe: { run: (opts: unknown) => Promise<unknown> } }).axe;
    const results = await axe.run({
      runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'] },
    });
    return JSON.stringify(results);
  });

  fs.mkdirSync(ARTIFACT_DIR, { recursive: true });
  const artifact = path.join(ARTIFACT_DIR, `${caseId}.json`);
  fs.writeFileSync(artifact, json);

  const parsed = JSON.parse(json) as { violations: { id: string }[] };
  const ids = parsed.violations.map((v) => v.id);
  expect(ids, `axe failed for ${caseId} (${ids.join(', ')}). Artifact: ${artifact}`).toEqual([]);
}
