import type { Page } from '@playwright/test';

/**
 * Stand-in for Cloudflare's Turnstile widget, shared by the signed-out admin
 * page suites (e2e/bootstrap, e2e/sign-in).
 *
 * The browser never reaches Cloudflare: every request to
 * challenges.cloudflare.com is answered here with a stand-in widget, loaded
 * under the page's real Content-Security-Policy. The server checks the
 * widget's token with the stand-in verifier that e2e/signed-out/harness.sh
 * starts, which accepts exactly one run-time token.
 *
 * This file imports nothing at run time (the Playwright import above is a
 * type), so each suite can use it with its own installed packages.
 */

export const WIDGET_HOST = 'challenges.cloudflare.com';
export const WIDGET_SCRIPT = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';

/**
 * The stand-in for Cloudflare's api.js. Like the real script it draws a frame
 * from Cloudflare's origin, and like the real script may, it does not install
 * itself over something that already stands at window.turnstile. An element
 * with id="turnstile" would be exactly that, and the form could then never be
 * submitted; with this guard such a page fails the tests.
 */
const WIDGET_STAND_IN = `
(function () {
  if (window.turnstile) {
    window.__e2eWidgetRefused = true;
    return;
  }
  var cfg = window.__e2eWidget || {};
  var state = window.__e2eWidgetState = { renders: 0, resets: 0, options: null };
  function solve() {
    if (cfg.auto && state.options) setTimeout(function () { state.options.callback(cfg.token); }, 0);
  }
  window.turnstile = {
    render: function (host, options) {
      state.renders += 1;
      state.options = options;
      state.sitekey = options.sitekey;
      state.size = options.size;
      var frame = document.createElement('iframe');
      frame.src = 'https://${WIDGET_HOST}/e2e-widget';
      frame.title = 'Security check';
      frame.width = options.size === 'compact' ? '150' : '300';
      frame.height = options.size === 'compact' ? '140' : '65';
      host.appendChild(frame);
      solve();
      return 'e2e-widget';
    },
    reset: function () { state.resets += 1; solve(); },
    remove: function () {}
  };
})();
`;

const WIDGET_FRAME =
  '<!DOCTYPE html><html lang="en"><head><meta charset="utf-8"><title>Security check</title></head>'
  + '<body><main><p>Security check stand-in</p></main></body></html>';

/** 'ok': the stand-in at once. 'abort': the request fails. A promise: the stand-in once it resolves. */
export type ScriptReply = 'ok' | 'abort' | Promise<void>;

export type WidgetSetup = {
  /** Solve at once (default), or wait for the test to call widgetSays. */
  auto?: boolean;
  /** The token the widget hands the page when it solves. */
  token?: string;
  /** What the n-th request for Cloudflare's script gets (default: the stand-in). */
  script?: (n: number) => ScriptReply;
};

/** Answers everything addressed to Cloudflare. Returns the script request count. */
export async function answerCloudflare(page: Page, setup: WidgetSetup): Promise<{ scripts: number }> {
  const counts = { scripts: 0 };
  await page.addInitScript((cfg) => {
    (window as any).__e2eWidget = cfg;
  }, { auto: setup.auto ?? true, token: setup.token ?? '' });
  await page.route((url) => url.hostname === WIDGET_HOST, async (route) => {
    const url = new URL(route.request().url());
    if (url.pathname === '/turnstile/v0/api.js') {
      counts.scripts += 1;
      const reply = setup.script ? setup.script(counts.scripts) : 'ok';
      if (reply === 'abort') {
        await route.abort('failed');
        return;
      }
      if (reply !== 'ok') await reply;
      await route.fulfill({ status: 200, contentType: 'text/javascript', body: WIDGET_STAND_IN });
      return;
    }
    if (url.pathname === '/e2e-widget') {
      await route.fulfill({ status: 200, contentType: 'text/html; charset=utf-8', body: WIDGET_FRAME });
      return;
    }
    await route.fulfill({ status: 404, body: '' });
  });
  return counts;
}

/** Calls one of the callbacks the page gave the widget, as Cloudflare would. */
export async function widgetSays(page: Page, callback: string, value?: string): Promise<void> {
  await page.evaluate(([name, arg]) => {
    (window as any).__e2eWidgetState.options[name as string](arg);
  }, [callback, value]);
}

export async function widgetState(page: Page): Promise<{ renders: number; resets: number; sitekey: string; size: string }> {
  return await page.evaluate(() => {
    const s = (window as any).__e2eWidgetState;
    return { renders: s.renders, resets: s.resets, sitekey: s.sitekey, size: s.size };
  });
}
