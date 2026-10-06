import fs from "node:fs";
import type { BrowserContext } from "@playwright/test";

export type SeedManifest = Record<string, string>;

export function readSeed(): SeedManifest {
  const seedFile =
    process.env.ADMIN_UI_TEST_SEED_FILE ||
    "/tmp/dealoware-admin-ui-seed.json";
  if (!fs.existsSync(seedFile)) {
    throw new Error(`Seed file missing: ${seedFile}. Start the local admin host with ADMIN_UI_TEST_SEED=1.`);
  }
  return JSON.parse(fs.readFileSync(seedFile, "utf8")) as SeedManifest;
}

export async function applyAdminSession(context: BrowserContext, sessionId: string): Promise<void> {
  await context.addCookies([
    {
      name: "dw_admin_session",
      value: sessionId,
      domain: "127.0.0.1",
      path: "/admin",
      httpOnly: true,
      secure: false,
      sameSite: "Strict",
    },
  ]);
}
