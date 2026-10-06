import { test, expect } from "@playwright/test";
import { expectAxe, mockAuthApi, openAdmin, screenshotScreen, serveStatsLanding } from "../helpers";

const pending = [{ name: "dw_admin_pending", value: "valid" }];
const enrol = [{ name: "dw_admin_pending", value: "enrol" }];

const screens: { id: string; path: string; title: string; cookies?: { name: string; value: string }[]; query?: string }[] = [
  { id: "S-A1", path: "/admin/sign-in", title: "Sign in · Dealoware admin" },
  { id: "S-A11", path: "/admin/sign-in", title: "Sign in · Dealoware admin", query: "?status=expired" },
  { id: "S-A2", path: "/admin/sign-in/code", title: "Enter code · Dealoware admin", cookies: pending },
  { id: "S-A3", path: "/admin/sign-in/recovery", title: "Recovery code · Dealoware admin", cookies: pending },
  { id: "S-A4", path: "/admin/bootstrap", title: "Set password · Dealoware admin", query: "?token=opaque" },
  { id: "S-A5", path: "/admin/link-expired", title: "Link no longer works · Dealoware admin" },
  { id: "S-A6", path: "/admin/setup/authenticator", title: "Authenticator setup · Dealoware admin", cookies: enrol },
  { id: "S-A7", path: "/admin/setup/recovery-codes", title: "Recovery codes · Dealoware admin", cookies: enrol },
  { id: "S-A8", path: "/admin/reset", title: "Reset password · Dealoware admin" },
  { id: "S-A9", path: "/admin/reset/sent", title: "Check your email · Dealoware admin" },
  { id: "S-A10", path: "/admin/reset/confirm", title: "Set a new password · Dealoware admin", query: "?token=opaque" }
];

test.describe("TD-ADM-UI-auth screens @TD-ADM-UI-auth-06 @TD-ADM-UI-auth-20", () => {
  for (const screen of screens) {
    test(`${screen.id} title, h1, axe, screenshot`, async ({ page }) => {
      await mockAuthApi(page);
      await openAdmin(page, `${screen.path}${screen.query ?? ""}`, screen.cookies ?? []);
      await expect(page).toHaveTitle(screen.title);
      await expect(page.locator("h1")).toHaveCount(1);
      await expect(page.locator("nav")).toHaveCount(0);
      await expectAxe(page, `TD-ADM-UI-auth-20-${screen.id}`);
      await screenshotScreen(page, screen.id);
    });
  }
});

test("TD-ADM-UI-auth-01 failure copy and reset link @TD-ADM-UI-auth-01 @TD-ADM-UI-auth-22", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/sign-in");
  await page.locator("#email").fill("unknown@example.com");
  await page.locator("#password").fill("x".repeat(16));
  await page.getByRole("button", { name: "Sign in" }).click();
  const alert = page.locator("#form-alert");
  await expect(alert).toBeVisible();
  await expect(alert).toContainText("We couldn't sign you in.");
  await expect(alert.locator("#reset-password-link")).toHaveAttribute("href", "/admin/reset");
  await expect(page.locator("body")).not.toContainText("locked");
  await expectAxe(page, "TD-ADM-UI-auth-01");
});

test("TD-ADM-UI-auth-02 Turnstile states @TD-ADM-UI-auth-02 @TD-ADM-UI-auth-23", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/sign-in");
  await expect(page.getByRole("button", { name: "Sign in" })).toBeEnabled();
  await page.evaluate(() => window.__turnstileApi.setFailed());
  await expect(page.locator("#turnstile-status")).toContainText("failed");
  await expect(page.getByRole("button", { name: "Sign in" })).toBeDisabled();
  await page.getByRole("button", { name: "Retry" }).click();
  await page.evaluate(() => window.__turnstileApi.setUnavailable());
  await expect(page.locator("#turnstile-status")).toContainText("unavailable");
  await expect(page.locator("#turnstile-status")).toContainText("network");
  await expectAxe(page, "TD-ADM-UI-auth-02");
});

test("TD-ADM-UI-auth-03 field attributes @TD-ADM-UI-auth-03", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/sign-in");
  await expect(page.locator("#email")).toHaveAttribute("autocomplete", "username");
  await expect(page.locator("#password")).toHaveAttribute("autocomplete", "current-password");
  await page.locator("#password").fill("paste-ok");
  await page.locator("#password-toggle").click();
  await expect(page.locator("#password-toggle")).toHaveAttribute("aria-pressed", "true");
  await openAdmin(page, "/admin/sign-in/code", pending);
  await expect(page.locator("#code")).toHaveAttribute("autocomplete", "one-time-code");
  await expect(page.locator("#code")).toHaveAttribute("inputmode", "numeric");
  await openAdmin(page, "/admin/sign-in/recovery", pending);
  await expect(page.locator("#recovery-code")).not.toHaveAttribute("inputmode");
  await expectAxe(page, "TD-ADM-UI-auth-03");
});

test("TD-ADM-UI-auth-04 keyboard order and no auto-submit @TD-ADM-UI-auth-04 @TD-ADM-UI-auth-21", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/sign-in");
  await expect(page.locator("#email")).toBeFocused();
  await page.keyboard.press("Tab");
  await expect(page.locator("#password")).toBeFocused();
  await openAdmin(page, "/admin/sign-in/code", pending);
  await expect(page.locator("#code")).toBeFocused();
  await page.locator("#code").fill("123456");
  await expect(page).toHaveURL(/\/admin\/sign-in\/code/);
  await expectAxe(page, "TD-ADM-UI-auth-04");
});

test("TD-ADM-UI-auth-05 inline errors and alert @TD-ADM-UI-auth-05", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/sign-in");
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.locator("#email")).toHaveAttribute("aria-invalid", "true");
  await expect(page.locator("#email-error")).toBeVisible();
  await expectAxe(page, "TD-ADM-UI-auth-05");
});

test("TD-ADM-UI-auth-07 recovery switch @TD-ADM-UI-auth-07", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/sign-in/code", pending);
  await expect(page.getByRole("link", { name: "Use a recovery code instead" })).toHaveAttribute(
    "href",
    "/admin/sign-in/recovery"
  );
  await expect(page.getByRole("link", { name: "Back to sign in" })).toHaveAttribute("href", "/admin/sign-in");
  await expectAxe(page, "TD-ADM-UI-auth-07");
});

test("TD-ADM-UI-auth-09 link expired variants @TD-ADM-UI-auth-09", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/link-expired?variant=reset");
  await expect(page.locator("h1")).toHaveText("Link no longer works");
  await expect(page.locator("#request-new-link")).toBeVisible();
  await openAdmin(page, "/admin/link-expired?variant=bootstrap");
  await expect(page.locator("#request-new-link")).toBeHidden();
  await expect(page.locator("#bootstrap-note")).toBeVisible();
  await expect(page.locator("form")).toHaveCount(0);
  await expectAxe(page, "TD-ADM-UI-auth-09");
});

test("TD-ADM-UI-auth-10 password length only @TD-ADM-UI-auth-10 @TD-ADM-UI-auth-08", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/bootstrap?token=opaque");
  await expect(page.locator("#password-helper")).toContainText("15 to 128");
  await page.locator("#new-password").fill("x".repeat(14));
  await page.locator("#new-password").blur();
  await expect(page.locator("#new-password-error")).toContainText("15");
  await expectAxe(page, "TD-ADM-UI-auth-10");
});

test("TD-ADM-UI-auth-11 reset request lands on sent @TD-ADM-UI-auth-11 @TD-ADM-UI-auth-12", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/reset");
  await page.locator("#email").fill("io@example.com");
  await page.getByRole("button", { name: "Send reset link" }).click();
  await expect(page).toHaveURL(/\/admin\/reset\/sent/);
  await expect(page.locator("#reset-neutral")).toContainText("1 hour");
  await expect(page.getByRole("link", { name: "Back to sign in" })).toBeVisible();
  await expectAxe(page, "TD-ADM-UI-auth-11");
});

test("TD-ADM-UI-auth-15 return path allowlist @TD-ADM-UI-auth-15", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/sign-in?return=https://evil.example/");
  await page.locator("#email").fill("ok@example.com");
  await page.locator("#password").fill("x".repeat(16));
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/admin\/sign-in\/code/);
  await expectAxe(page, "TD-ADM-UI-auth-15");
});

test("TD-ADM-UI-auth-16 recovery banner with S-A12 link @TD-ADM-UI-auth-16", async ({ page }) => {
  await mockAuthApi(page);
  await serveStatsLanding(page);
  await openAdmin(page, "/admin/sign-in/recovery", pending);
  await page.locator("#recovery-code").fill("ALPHA-ONE");
  await page.getByRole("button", { name: "Sign in" }).click();
  await page.waitForURL(/recoveryRemaining=9/);
  await expect(page.locator("#recovery-banner-text")).toContainText("9");
  await expect(page.getByRole("link", { name: "Manage recovery codes" })).toHaveAttribute(
    "href",
    "/admin/settings/security"
  );
  await expectAxe(page, "TD-ADM-UI-auth-16");
});

test("TD-ADM-UI-auth-21 pending token not in URL @TD-ADM-UI-auth-21", async ({ page }) => {
  await mockAuthApi(page);
  await openAdmin(page, "/admin/sign-in/code", pending);
  expect(page.url()).not.toContain("pending");
  const leaked = await page.evaluate(() => ({
    href: location.href,
    ls: Object.keys(localStorage),
    ss: Object.keys(sessionStorage)
  }));
  expect(leaked.href).not.toMatch(/pending/i);
  await expectAxe(page, "TD-ADM-UI-auth-21");
});
