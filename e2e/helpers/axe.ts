import AxeBuilder from "@axe-core/playwright";
import { expect, type Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

export async function runAxeAndSave(page: Page, artifactName: string): Promise<void> {
  const results = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"])
    .analyze();

  const dir = path.join(__dirname, "..", "artifacts", "axe");
  fs.mkdirSync(dir, { recursive: true });
  const file = path.join(dir, `${artifactName}.json`);
  fs.writeFileSync(file, JSON.stringify(results, null, 2));

  expect(results.violations, `axe violations written to ${file}`).toEqual([]);
}
