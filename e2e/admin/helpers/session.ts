import fs from 'fs';
import os from 'os';
import path from 'path';
import { test as base, expect } from '@playwright/test';

export { expect };

export const SESSION_FILE =
  process.env.DEALOWARE_ADMIN_UI_TEST_SESSION_FILE
  ?? path.join(os.tmpdir(), 'dealoware-admin-ui-e2e-session.txt');

export const DB_FILE =
  process.env.DEALOWARE_ADMIN_UI_E2E_DB
  ?? path.join(os.tmpdir(), 'dealoware-admin-ui-e2e.db');

export function readSessionId(): string {
  expect(fs.existsSync(SESSION_FILE), `Admin UI test session file missing: ${SESSION_FILE}`).toBeTruthy();
  const id = fs.readFileSync(SESSION_FILE, 'utf8').trim();
  expect(id.length, 'Admin UI test session id is empty.').toBeGreaterThan(0);
  return id;
}

export const test = base.extend({
  context: async ({ browser, baseURL }, use) => {
    const host = new URL(baseURL ?? 'http://127.0.0.1:4173').hostname;
    const context = await browser.newContext();
    await context.addCookies([
      {
        name: 'dw_admin_session',
        value: readSessionId(),
        domain: host,
        path: '/admin',
        httpOnly: true,
        secure: false,
        sameSite: 'Strict',
      },
    ]);
    await use(context);
    await context.close();
  },
});
