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
  if (cookies.length) {
    await page.context().addCookies(
      cookies.map((c) => ({
        name: c.name,
        value: c.value,
        url: "http://127.0.0.1:5088",
        path: "/admin",
        httpOnly: true
      }))
    );
  }
  await page.goto(adminPath, { waitUntil: "domcontentloaded" });
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

export async function mockAuthApi(page: Page) {
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
        body: throttle || unknown || !body?.password ? failBody : JSON.stringify({ next: "code" })
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
        body: JSON.stringify({ next: "authenticator" })
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
