import { AxeBuilder } from "@axe-core/playwright";
import type { Page } from "@playwright/test";
import { expect } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

export async function runAxeAndSave(page: Page, caseId: string): Promise<void> {
  const results = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"])
    .analyze();

  const outDir = path.join(__dirname, "..", "test-results", "axe");
  fs.mkdirSync(outDir, { recursive: true });
  const outFile = path.join(outDir, `${caseId}.json`);
  fs.writeFileSync(outFile, JSON.stringify(results, null, 2));

  const serious = results.violations.filter(
    (v) => v.impact === "serious" || v.impact === "critical",
  );
  expect(
    serious,
    `${caseId} axe serious/critical: ${serious.map((v) => v.id).join(", ")}`,
  ).toEqual([]);
}
