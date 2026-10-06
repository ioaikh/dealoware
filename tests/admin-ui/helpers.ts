import { AxeBuilder } from "@axe-core/playwright";
import { expect, type Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

export const AXE_DIR = path.resolve(__dirname, "axe-results");
export const SCREEN_DIR = path.resolve(__dirname, "screenshots");

const TURNSTILE_INIT = `
window.__ADMIN_AUTH_TEST__ = {
  siteKey: "test-site-key",
  renderTurnstile: function (host, api) {
    window.__turnstileApi = api;
    api.setReady("test-turnstile-token");
  }
};
`;

export async function openAdmin(page: Page, adminPath: string, cookies: { name: string; value: string }[] = []) {
  if (!adminPath.startsWith("/admin/")) {
    throw new Error("Playwright must use /admin/ paths: " + adminPath);
  }
  await page.addInitScript(TURNSTILE_INIT);
  if (adminPath.startsWith("/admin/setup/recovery-codes")) {
    await page.addInitScript(() => {
      sessionStorage.setItem("dw-admin-recovery-once", JSON.stringify(["alpha-one", "bravo-two"]));
    });
  }
  if (cookies.length) {
    await page.context().addCookies(
      cookies.map((c) => ({
        name: c.name,
        value: c.value,
        domain: "127.0.0.1",
        path: "/admin",
        httpOnly: true,
        secure: false
      }))
    );
  }
  await page.goto(adminPath, { waitUntil: "domcontentloaded" });
}

export async function serveStatsLanding(page: Page) {
  const html = fs.readFileSync(
    path.resolve(__dirname, "../../src/Dealoware.Api/AdminUi/pages/stats.html"),
    "utf8"
  );
  await page.route((url) => url.pathname === "/admin/" || url.pathname === "/admin", async (route) => {
    if (route.request().method() === "GET") {
      await route.fulfill({
        status: 200,
        contentType: "text/html; charset=utf-8",
        body: html
      });
      return;
    }
    await route.fallback();
  });
}

export async function openSecuritySettings(page: Page) {
  const html = fs.readFileSync(
    path.resolve(__dirname, "../../src/Dealoware.Api/AdminUi/pages/security.html"),
    "utf8"
  );
  await page.route((url) => url.pathname === "/admin/settings/security", async (route) => {
    if (route.request().method() === "GET") {
      await route.fulfill({
        status: 200,
        contentType: "text/html; charset=utf-8",
        headers: { "Cache-Control": "no-store" },
        body: html
      });
      return;
    }
    await route.fallback();
  });
  await page.goto("/admin/settings/security", { waitUntil: "domcontentloaded" });
}

export function watchLinkTokenRequests(page: Page, apiPath: string) {
  const bodies: { token?: string }[] = [];
  const leaks: string[] = [];
  page.on("request", (request) => {
    const url = request.url();
    const headers = request.headers();
    const referer = headers.referer || headers.referrer || "";
    const headerValues = Object.entries(headers)
      .filter(([name]) => name.toLowerCase() !== "cookie")
      .map(([, value]) => value)
      .join("\n");
    const isApiPost = request.method() === "POST" && url.includes(apiPath);
    if (isApiPost) bodies.push(request.postDataJSON() as { token?: string });
    const urlHasToken = /[?#]token=/i.test(url) || /\/opaque(?:[/?#]|$)/i.test(url);
    const refererHasToken = /[?#]token=|token=opaque/i.test(referer);
    const headerHasToken = /token=opaque/i.test(headerValues);
    if (urlHasToken || refererHasToken || headerHasToken)
      leaks.push(`${request.method()} ${url} referer=${referer}`);
  });
  return { bodies, leaks };
}

export async function expectAxe(page: Page, caseId: string) {
  fs.mkdirSync(AXE_DIR, { recursive: true });
  const results = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag22aa"])
    .analyze();
  fs.writeFileSync(path.join(AXE_DIR, `${caseId}.json`), JSON.stringify(results, null, 2));
  const blocking = results.violations.filter(
    (v) => v.impact === "serious" || v.impact === "critical"
  );
  expect(blocking, `axe serious/critical for ${caseId}`).toEqual([]);
}

export async function screenshotScreen(page: Page, name: string) {
  fs.mkdirSync(SCREEN_DIR, { recursive: true });
  await page.screenshot({ path: path.join(SCREEN_DIR, `${name}.png`), fullPage: true });
}

export async function mockAuthApi(page: Page, options: { passwordDelayMs?: number } = {}) {
  await page.route("**/admin/api/me", async (route) => {
    if (route.request().method() !== "GET") {
      await route.fallback();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ principal: "CoreOwner", remainingRecoveryCodes: 10 })
    });
  });

  await page.route("**/admin/api/settings/password", async (route) => {
    if (route.request().method() !== "POST") {
      await route.fallback();
      return;
    }
    if (options.passwordDelayMs)
      await new Promise((resolve) => setTimeout(resolve, options.passwordDelayMs));
    const body = route.request().postDataJSON() as {
      currentPassword?: string;
      newPassword?: string;
      confirmPassword?: string;
      code?: string;
      recoveryCode?: string;
    };
    const stepUp = JSON.stringify({
      error: "We couldn't confirm it's you. Check your current password and code."
    });
    if (body?.currentPassword === "incorrect-value-aaa" || body?.code === "000000" || body?.recoveryCode === "usedcode") {
      await route.fulfill({ status: 401, contentType: "application/json", body: stepUp });
      return;
    }
    if (body?.currentPassword === "trigger-hold-value") {
      await route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({
          error: "We couldn't sign you in. Check your details and try again later. You can also reset your password.",
          signedOut: true
        })
      });
      return;
    }
    if (body?.newPassword === "passwordpassword1") {
      await route.fulfill({
        status: 400,
        contentType: "application/json",
        body: JSON.stringify({ error: "This password is too common. Choose another.", reason: "common" })
      });
      return;
    }
    if (body?.newPassword && body.newPassword === body.currentPassword) {
      await route.fulfill({
        status: 400,
        contentType: "application/json",
        body: JSON.stringify({
          error: "Choose a password that is different from your current one.",
          reason: "same_as_current"
        })
      });
      return;
    }
    if (body?.newPassword && body.confirmPassword && body.newPassword !== body.confirmPassword) {
      await route.fulfill({
        status: 400,
        contentType: "application/json",
        body: JSON.stringify({ error: "These passwords don't match.", reason: "mismatch" })
      });
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ next: "signin", status: "password-changed" })
    });
  });

  await page.route("**/admin/api/auth/**", async (route) => {
    const url = new URL(route.request().url());
    const method = route.request().method();
    const pathname = url.pathname;
    const failBody = JSON.stringify({
      error: "We couldn't sign you in. Check your details and try again later. You can also reset your password."
    });

    if (pathname === "/admin/api/auth/sign-in" && method === "POST") {
      const body = route.request().postDataJSON() as { email?: string; password?: string };
      const unknown = body?.email === "unknown@example.com";
      const throttle = body?.email === "throttle@example.com";
      await route.fulfill({
        status: throttle ? 429 : unknown || !body?.password ? 401 : 200,
        contentType: "application/json",
        body: throttle || unknown || !body?.password ? failBody : JSON.stringify({ next: "code" }),
        headers: throttle || unknown || !body?.password
          ? { "Content-Type": "application/json" }
          : {
              "Content-Type": "application/json",
              "Set-Cookie": "dw_admin_pending=valid; Path=/admin; SameSite=Strict"
            }
      });
      return;
    }
    if (pathname === "/admin/api/auth/sign-in/code" && method === "POST") {
      const body = route.request().postDataJSON() as { code?: string };
      await route.fulfill({
        status: body?.code === "000000" ? 401 : 200,
        contentType: "application/json",
        body: body?.code === "000000" ? failBody : JSON.stringify({ next: "home" })
      });
      return;
    }
    if (pathname === "/admin/api/auth/sign-in/recovery" && method === "POST") {
      const body = route.request().postDataJSON() as { recoveryCode?: string };
      if (body?.recoveryCode === "usedcode") {
        await route.fulfill({ status: 401, contentType: "application/json", body: failBody });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ next: "home", remainingRecoveryCodes: 9 })
      });
      return;
    }
    if (pathname === "/admin/api/auth/bootstrap" && method === "POST") {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ next: "authenticator" }),
        headers: {
          "Content-Type": "application/json",
          "Set-Cookie": "dw_admin_pending=enrol; Path=/admin; SameSite=Strict"
        }
      });
      return;
    }
    if (pathname === "/admin/api/auth/totp/enroll" && method === "POST") {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ secret: "ABCDEFGHIJKLMNOP", otpauthUri: "otpauth://totp/Dealoware", digits: 6 })
      });
      return;
    }
    if (pathname === "/admin/api/auth/totp/enroll/confirm" && method === "POST") {
      const body = route.request().postDataJSON() as { code?: string };
      await route.fulfill({
        status: body?.code === "000000" ? 400 : 200,
        contentType: "application/json",
        body: body?.code === "000000"
          ? JSON.stringify({ error: "That code did not work. Try again." })
          : JSON.stringify({ recoveryCodes: ["alpha-one", "bravo-two"] })
      });
      return;
    }
    if (pathname === "/admin/api/auth/reset" && method === "POST") {
      await route.fulfill({ status: 200, contentType: "application/json", body: "{}" });
      return;
    }
    if (pathname === "/admin/api/auth/reset/confirm" && method === "POST") {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ next: "signin", status: "password-set" })
      });
      return;
    }
    if (pathname === "/admin/api/auth/sign-out" && method === "POST") {
      await route.fulfill({ status: 204, body: "" });
      return;
    }
    await route.fulfill({ status: 404, contentType: "application/json", body: "{\"error\":\"Unauthorized\"}" });
  });
}
