import { test, expect } from "@playwright/test";
import { expectAxe, mockAuthApi, openSecuritySettings } from "./helpers";

const CURRENT = "current-value-aaaa";
const NEXT = "replacement-value-aaaa";

async function fillChangePassword(
  page: import("@playwright/test").Page,
  fields: { current: string; next: string; confirm?: string; code?: string; recovery?: string }
) {
  await page.locator("#current-password").fill(fields.current);
  await page.locator("#new-password").fill(fields.next);
  await page.locator("#confirm-password").fill(fields.confirm ?? fields.next);
  if (fields.recovery !== undefined) {
    await page.getByRole("link", { name: "Use a recovery code instead" }).click();
    await page.locator("#recovery-code").fill(fields.recovery);
  } else {
    await page.locator("#code").fill(fields.code ?? "123456");
  }
}

test("TD-ADM-UI-auth-25 step-up failure clears current and code, keeps new, focuses current @TD-ADM-UI-auth-25", async ({
  page
}) => {
  await mockAuthApi(page);
  await openSecuritySettings(page);
  await expect(page).toHaveTitle("Security settings · Dealoware admin");
  await expect(page.locator("h1")).toHaveText("Security settings");
  await expect(page.locator("#current-password")).toHaveAttribute("autocomplete", "current-password");
  await expect(page.locator("#new-password")).toHaveAttribute("autocomplete", "new-password");
  await expect(page.locator("#confirm-password")).toHaveAttribute("autocomplete", "new-password");
  await expect(page.locator("#code")).toHaveAttribute("autocomplete", "one-time-code");
  await expect(page.locator("#code")).toHaveAttribute("inputmode", "numeric");
  await expect(page.locator("#change-password-form")).toHaveAttribute("aria-describedby", "stepup-status");
  await expect(page.locator("#stepup-status")).toHaveAttribute("aria-live", "polite");

  await fillChangePassword(page, { current: "incorrect-value-aaa", next: NEXT, code: "123456" });
  await page.getByRole("button", { name: "Change password" }).click();
  await expect(page.locator("#stepup-status")).toHaveText(
    "We couldn't confirm it's you. Check your current password and code."
  );
  await expect(page.locator("#current-password")).toHaveValue("");
  await expect(page.locator("#code")).toHaveValue("");
  await expect(page.locator("#new-password")).toHaveValue(NEXT);
  await expect(page.locator("#confirm-password")).toHaveValue(NEXT);
  await expect(page.locator("#current-password")).toBeFocused();
  await expect(page.locator("#form-alert")).toHaveCount(0);

  await fillChangePassword(page, { current: CURRENT, next: NEXT, code: "000000" });
  await page.getByRole("button", { name: "Change password" }).click();
  await expect(page.locator("#stepup-status")).toHaveText(
    "We couldn't confirm it's you. Check your current password and code."
  );
  await expect(page.locator("#current-password")).toHaveValue("");
  await expect(page.locator("#code")).toHaveValue("");

  await fillChangePassword(page, { current: CURRENT, next: NEXT, recovery: "usedcode" });
  await page.getByRole("button", { name: "Change password" }).click();
  await expect(page.locator("#stepup-status")).toHaveText(
    "We couldn't confirm it's you. Check your current password and code."
  );
  await expect(page.locator("#recovery-code")).toHaveValue("");
  await expect(page.locator("#current-password")).toBeFocused();
  await expect(page.locator("body")).not.toContainText("Turnstile");
  await expectAxe(page, "TD-ADM-UI-auth-25");
});

test("TD-ADM-UI-auth-26 only one submit in flight @TD-ADM-UI-auth-26", async ({ page }) => {
  await mockAuthApi(page, { passwordDelayMs: 800 });
  await openSecuritySettings(page);
  const posts: string[] = [];
  page.on("request", (request) => {
    if (request.method() === "POST" && request.url().includes("/admin/api/settings/password"))
      posts.push(request.url());
  });
  await fillChangePassword(page, { current: CURRENT, next: NEXT, code: "123456" });
  const submit = page.getByRole("button", { name: "Change password" });
  await submit.click();
  await expect(submit).toBeDisabled();
  await submit.click({ force: true }).catch(() => undefined);
  await page.keyboard.press("Enter");
  await page.waitForURL(/\/admin\/sign-in\?status=password-changed/);
  expect(posts).toHaveLength(1);
  await expectAxe(page, "TD-ADM-UI-auth-26");
});

test("TD-ADM-UI-auth-27 rule reject or mismatch is not a step-up failure @TD-ADM-UI-auth-27", async ({
  page
}) => {
  await mockAuthApi(page);
  await openSecuritySettings(page);

  await fillChangePassword(page, { current: CURRENT, next: "short", confirm: "short", code: "123456" });
  await page.locator("#new-password").blur();
  await expect(page.locator("#new-password-error")).toContainText("Use at least 15 characters.");
  await page.getByRole("button", { name: "Change password" }).click();
  await expect(page.locator("#stepup-status")).toHaveText("");
  await expect(page.locator("#new-password")).toBeFocused();

  await fillChangePassword(page, { current: CURRENT, next: "passwordpassword1", code: "123456" });
  await page.getByRole("button", { name: "Change password" }).click();
  await expect(page.locator("#new-password-error")).toHaveText("This password is too common. Choose another.");
  await expect(page.locator("#stepup-status")).toHaveText("");
  await expect(page.locator("#new-password")).toHaveValue("passwordpassword1");
  await expect(page.locator("#new-password")).toBeFocused();

  await fillChangePassword(page, { current: CURRENT, next: CURRENT, code: "123456" });
  await page.getByRole("button", { name: "Change password" }).click();
  await expect(page.locator("#new-password-error")).toHaveText(
    "Choose a password that is different from your current one."
  );
  await expect(page.locator("#stepup-status")).toHaveText("");

  await fillChangePassword(page, { current: CURRENT, next: NEXT, confirm: `${NEXT}x`, code: "123456" });
  await page.locator("#confirm-password").blur();
  await expect(page.locator("#confirm-password-error")).toHaveText("These passwords don't match.");
  await page.getByRole("button", { name: "Change password" }).click();
  await expect(page.locator("#confirm-password-error")).toHaveText("These passwords don't match.");
  await expect(page.locator("#stepup-status")).toHaveText("");
  await expect(page).toHaveURL(/\/admin\/settings\/security/);
  await expectAxe(page, "TD-ADM-UI-auth-27");
});

test("TD-ADM-UI-auth-28 hold that ends the session lands on S-A1 with A03 text @TD-ADM-UI-auth-28", async ({
  page
}) => {
  await mockAuthApi(page);
  await openSecuritySettings(page);
  await fillChangePassword(page, { current: "trigger-hold-value", next: NEXT, code: "123456" });
  await page.getByRole("button", { name: "Change password" }).click();
  await page.waitForURL(/\/admin\/sign-in\?status=auth-failed/);
  await expect(page.locator("#form-alert")).toContainText(
    "We couldn't sign you in. Check your details and try again later. You can also reset your password."
  );
  await expect(page.locator("body")).not.toContainText(/lock/i);
  await expect(page.locator("nav")).toHaveCount(0);
  await expectAxe(page, "TD-ADM-UI-auth-28");
});

test("TD-ADM-UI-auth-29 success ends this session and lands on S-A1 with success copy @TD-ADM-UI-auth-29", async ({
  page
}) => {
  await mockAuthApi(page);
  await openSecuritySettings(page);
  await fillChangePassword(page, { current: CURRENT, next: NEXT, code: "123456" });
  await page.getByRole("button", { name: "Change password" }).click();
  await page.waitForURL(/\/admin\/sign-in\?status=password-changed/);
  await expect(page.locator("#status-region")).toHaveText(
    "Your password has been changed. Sign in with your new password and authenticator code."
  );
  await expect(page.locator("nav")).toHaveCount(0);
  await expectAxe(page, "TD-ADM-UI-auth-29");
  await page.unrouteAll({ behavior: "ignoreErrors" });
  await page.goto("/admin/settings/security", { waitUntil: "domcontentloaded" });
  await expect(page.locator("#change-password-form")).toHaveCount(0);
  await expect(page.locator("body")).not.toContainText("Security settings");
});

test("TD-ADM-UI-auth-30 no Turnstile on S-A12 password form @TD-ADM-UI-auth-30", async ({ page }) => {
  await mockAuthApi(page);
  await openSecuritySettings(page);
  await expect(page.locator("#turnstile")).toHaveCount(0);
  await expect(page.locator("#turnstile-widget")).toHaveCount(0);
  await expect(page.locator("script[src*='turnstile']")).toHaveCount(0);
  await expect(page.locator("#change-password-form")).not.toContainText("Turnstile");
  await expect(page.locator("#recovery-n")).toHaveText("10");
  const before = await page.locator("#recovery-n").innerText();
  await fillChangePassword(page, { current: "incorrect-value-aaa", next: NEXT, code: "123456" });
  await page.getByRole("button", { name: "Change password" }).click();
  await expect(page.locator("#stepup-status")).toContainText("We couldn't confirm it's you.");
  await expect(page.locator("#recovery-n")).toHaveText(before);
  await expectAxe(page, "TD-ADM-UI-auth-30");
});
