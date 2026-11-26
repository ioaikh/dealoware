import type { Page, Request } from '@playwright/test';

/**
 * Collects the page's errors for a test to assert there are none: call it in
 * beforeEach, and expect what the returned function gives to be [].
 *
 * One WebKit message is left out, and only in one case. When a fetch is still
 * in flight as the page navigates away (a sign-out, a goto, a reload), WebKit
 * may fail it and write "Fetch API cannot load <address> due to access control
 * checks." to the console as a JavaScript error, and Playwright's WebKit
 * backend reports every JavaScript error written to the console as a page
 * error (cut at the first colon, so the message arrives as
 * "/<host><path> due to access control checks."). It is not an exception in
 * the page: every request the admin scripts make handles its own failure.
 *
 * So that message is dropped only when it names a request that failed after
 * the document that sent it had started to navigate away (a main-frame
 * navigation request after that document arrived). The same message for a
 * request that failed while its document stayed, and every other error, is
 * kept.
 */
export function collectPageErrors(page: Page): () => string[] {
  const errors: { name: string; message: string }[] = [];
  const abandoned = new Set<string>();
  // How many main-frame navigation requests had been made when the current document arrived.
  let navigations = 0;
  let documentArrivedAt = 0;
  const sentBy = new Map<Request, number>();

  page.on('pageerror', (error) => errors.push({ name: error.name, message: error.message }));
  page.on('framenavigated', (frame) => {
    if (frame === page.mainFrame()) documentArrivedAt = navigations;
  });
  page.on('request', (request) => {
    if (request.isNavigationRequest() && request.frame() === page.mainFrame()) navigations += 1;
    else sentBy.set(request, documentArrivedAt);
  });
  page.on('requestfailed', (request) => {
    const arrivedAt = sentBy.get(request);
    if (arrivedAt !== undefined && navigations > arrivedAt) abandoned.add(request.url());
  });

  return () => errors
    .filter((error) => !isAbandonedFetch(error, abandoned))
    .map((error) => error.message);
}

/** WebKit's console line for a fetch to one of these addresses, as Playwright splits it. */
function isAbandonedFetch(error: { name: string; message: string }, abandoned: Set<string>): boolean {
  for (const url of abandoned) {
    const line = `Fetch API cannot load ${url} due to access control checks.`;
    const colon = line.indexOf(':');
    if (error.name === line.slice(0, colon) && error.message === line.substring(colon + 2)) return true;
  }
  return false;
}
