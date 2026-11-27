import type { Page } from '@playwright/test';

/**
 * Collects the page's errors for a test to assert there are none: call it in
 * beforeEach, and expect what the returned function gives to be [].
 *
 * Every error is kept. On WebKit that includes "Fetch API cannot load <address>
 * due to access control checks.": WebKit writes it to the console when a page
 * starts a request after it has begun to navigate away (the request is
 * refused before it is sent, so no request or requestfailed event names it),
 * and Playwright reports it as a page error. A request already out that the
 * navigation cancels is not reported. So the line means the page asked for
 * something as it was going (console.js's sign-out did, when the session
 * timer moved the page under it).
 */
export function collectPageErrors(page: Page): () => string[] {
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  return () => [...errors];
}
