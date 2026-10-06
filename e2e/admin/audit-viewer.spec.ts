import { expect, test, type Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";
import { runAxeAndSave } from "../helpers/axe";

const EDIT_ID = "11111111-1111-1111-1111-111111111111";
const DELETED_ENTITY_ID = "66666666-6666-6666-6666-666666666666";
const EXISTING_PARTICIPANT_ID = "55555555-5555-5555-5555-555555555555";

function readSessionId(): string {
  if (process.env.ADMIN_UI_SESSION) return process.env.ADMIN_UI_SESSION;
  const candidates = [
    path.join(__dirname, "..", "..", "tests", "Dealoware.AdminUi.Host", "bin", "Release", "net10.0", "admin-ui-host.json"),
    path.join(__dirname, "..", "..", "tests", "Dealoware.AdminUi.Host", "bin", "Debug", "net10.0", "admin-ui-host.json")
  ];
  const deadline = Date.now() + 30_000;
  while (Date.now() < deadline) {
    for (const file of candidates) {
      if (!fs.existsSync(file)) continue;
      const info = JSON.parse(fs.readFileSync(file, "utf8")) as { sessionId?: string; SessionId?: string };
      const sessionId = info.sessionId || info.SessionId;
      if (sessionId) return sessionId;
    }
    Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 100);
  }
  throw new Error("admin-ui-host.json was not written; the local test host did not start.");
}

async function addSessionCookie(page: Page): Promise<void> {
  const sessionId = readSessionId();
  const base = new URL(test.info().project.use.baseURL ?? "http://127.0.0.1:4173");
  await page.context().addCookies([
    {
      name: "dw_admin_session",
      value: sessionId,
      domain: base.hostname,
      path: "/admin",
      httpOnly: true,
      secure: false,
      sameSite: "Strict"
    }
  ]);
}

test.describe("Admin audit viewer", () => {
  test.describe.configure({ mode: "serial" });

  test("TD-ADM-131 unauthenticated /admin/audit and shell assets deny with no entity data @TD-ADM-131 @TD-ADM-UI-na-X01", async ({ request }) => {
    for (const pathName of [
      "/admin/audit",
      `/admin/audit/${EDIT_ID}`,
      "/admin/audit/shell/audit.js",
      "/admin/audit/shell/audit.css"
    ]) {
      const response = await request.get(pathName, {
        headers: { Host: "admin.core.dealoware.com" }
      });
      expect(response.status(), pathName).toBe(401);
      const body = await response.text();
      expect(body).toContain("Unauthorized");
      expect(body).not.toContain("Ada Lovelace");
      expect(body).not.toContain(EXISTING_PARTICIPANT_ID);
      expect(body).not.toContain("203.0.113.10");
    }
  });

  test("TD-ADM-UI-na-X01 audit routes are /admin/audit and /admin/audit/{guid} @TD-ADM-UI-na-X01", async ({ page }) => {
    await addSessionCookie(page);
    const list = await page.goto("/admin/audit");
    expect(list?.ok()).toBeTruthy();
    await expect(page.locator("body")).toHaveAttribute("data-admin-screen", "S-D1");
    expect(page.url()).toContain("/admin/audit");
    expect(page.url()).not.toContain("admin.core.dealoware.com");

    const detail = await page.goto(`/admin/audit/${EDIT_ID}`);
    expect(detail?.ok()).toBeTruthy();
    await expect(page.locator("body")).toHaveAttribute("data-admin-screen", "S-D2");
    await runAxeAndSave(page, "td-adm-ui-na-x01-detail");
  });

  test("TD-ADM-UI-na-X02 S-D1 columns, filters, paging, URL state, no mutate @TD-ADM-UI-na-X02 @TD-ADM-101", async ({ page }) => {
    await addSessionCookie(page);
    await page.goto("/admin/audit");
    await expect(page.getByRole("heading", { name: "Audit log" })).toBeVisible();
    await expect(page.locator("#audit-table")).toBeVisible();
    await expect(page.getByRole("columnheader", { name: /Time/ })).toHaveAttribute("aria-sort", "descending");
    for (const name of ["Actor", "Action", "Entity type", "Entity ID", "Outcome"]) {
      await expect(page.getByRole("columnheader", { name })).toBeVisible();
    }
    await expect(page.locator("#audit-rows tr").first()).toBeVisible({ timeout: 15_000 });
    await expect(page.locator("input[type=search]")).toHaveCount(0);
    await expect(page.getByRole("button", { name: /edit record|delete entry|save changes/i })).toHaveCount(0);
    await expect(page.locator("body")).not.toContainText("203.0.113.10");
    await expect(page.locator("body")).not.toContainText("should-never-render");

    await page.locator("#action").selectOption("entity_edit");
    await page.locator("#entityType").selectOption("Participant");
    await page.getByRole("button", { name: "Apply filters" }).click();
    await expect(page).toHaveURL(/action=entity_edit/);
    await expect(page).toHaveURL(/entityType=Participant/);
    await expect(page.locator("#audit-rows tr")).toHaveCount(1);
    await expect(page.getByRole("link", { name: EXISTING_PARTICIPANT_ID })).toHaveAttribute(
      "href",
      `/admin/participants/${EXISTING_PARTICIPANT_ID}`
    );

    await page.goto("/admin/audit?limit=50");
    await expect(page.locator("#audit-rows tr")).toHaveCount(50);
    await page.locator("#next-page").click();
    await expect(page).toHaveURL(/offset=50/);
    await expect(page.locator("#page-summary")).toContainText("of ");
    await runAxeAndSave(page, "td-adm-ui-na-x02-list");
  });

  test("TD-ADM-101 step 2 S-D2 before/after is read-only and FieldPolicy-safe @TD-ADM-101 @TD-ADM-UI-na-X02", async ({ page }) => {
    await addSessionCookie(page);
    await page.goto(`/admin/audit/${EDIT_ID}`);
    await expect(page.getByRole("heading", { name: "Audit entry" })).toBeVisible();
    await expect(page.getByText("Ada Lovelace <img>")).toBeVisible();
    await expect(page.locator("img")).toHaveCount(0);
    await expect(page.locator("body")).not.toContainText("should-never-render");
    await expect(page.locator("body")).not.toContainText("203.0.113.10");
    await expect(page.locator("body")).not.toContainText("passwordHash");
    await expect(page.getByText("Changed").first()).toBeVisible();
    await expect(page.getByText("a1b2c3d4e5f6")).toBeVisible();
    await expect(page.getByRole("button", { name: /edit|delete|save/i })).toHaveCount(0);
    await expect(page.getByRole("link", { name: "Back to audit log" })).toHaveAttribute("href", /\/admin\/audit/);
    await runAxeAndSave(page, "td-adm-101-s-d2");
  });

  test("TD-ADM-UI-na-B15 timestamps use ET label and UTC tooltip @TD-ADM-UI-na-B15", async ({ page }) => {
    await addSessionCookie(page);
    await page.goto("/admin/audit");
    const time = page.locator("#audit-rows time").first();
    await expect(time).toBeVisible();
    await expect(time).toContainText("ET");
    await expect(time).toHaveAttribute("title", /UTC /);
    await runAxeAndSave(page, "td-adm-ui-na-b15");
  });

  test("TD-ADM-UI-na-axe S-D1 and S-D2 states write axe artifacts @TD-ADM-UI-na-axe", async ({ page }) => {
    await addSessionCookie(page);

    let holdList = true;
    await page.route("**/admin/api/audit**", async (route) => {
      const url = route.request().url();
      const isDetail = /\/admin\/api\/audit\/[0-9a-f-]+$/i.test(new URL(url).pathname);
      if (holdList && !isDetail) {
        await new Promise((resolve) => setTimeout(resolve, 2_500));
      }
      await route.continue();
    });
    const loading = page.goto("/admin/audit");
    await expect(page.locator(".skeleton").first()).toBeVisible({ timeout: 5_000 });
    await runAxeAndSave(page, "td-adm-ui-na-axe-s-d1-loading");
    holdList = false;
    await loading;
    await page.unroute("**/admin/api/audit**");

    await page.goto("/admin/audit");
    await expect(page.locator("#audit-rows tr").first()).toBeVisible();
    await runAxeAndSave(page, "td-adm-ui-na-axe-s-d1-default");

    await page.goto("/admin/audit?action=totp_change");
    await expect(page.locator("#empty")).toContainText("No results match these filters.");
    await runAxeAndSave(page, "td-adm-ui-na-axe-s-d1-filter-miss");

    await page.route("**/admin/api/audit**", (route) => {
      const pathname = new URL(route.request().url()).pathname;
      if (/\/admin\/api\/audit\/[0-9a-f-]+$/i.test(pathname)) {
        return route.continue();
      }
      return route.fulfill({ status: 500, contentType: "application/json", body: "{}" });
    });
    await page.goto("/admin/audit?action=entity_edit");
    await expect(page.locator("#error")).toContainText("We couldn't load the audit log.");
    await runAxeAndSave(page, "td-adm-ui-na-axe-s-d1-error");
    await page.unroute("**/admin/api/audit**");

    await page.goto(`/admin/audit/${EDIT_ID}`);
    await expect(page.getByRole("heading", { name: "Audit entry" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Before", exact: true })).toBeVisible();
    await runAxeAndSave(page, "td-adm-ui-na-axe-s-d2-default");

    await page.goto("/admin/audit/99999999-9999-9999-9999-999999999999");
    await expect(page.locator("#error")).toContainText("We can't find that page.");
    await runAxeAndSave(page, "td-adm-ui-na-axe-s-d2-not-found");

    await page.goto("/admin/audit/44444444-4444-4444-4444-444444444444");
    await expect(page.locator("body")).toContainText(DELETED_ENTITY_ID);
    await expect(page.getByRole("link", { name: DELETED_ENTITY_ID })).toHaveCount(0);
    await runAxeAndSave(page, "td-adm-ui-na-axe-s-d2-deleted-entity");
  });

  test("TD-ADM-UI-na-kbd keyboard order on S-D1 and S-D2 @TD-ADM-UI-na-kbd", async ({ page }) => {
    await addSessionCookie(page);
    await page.goto("/admin/audit");
    await expect(page.locator("#audit-rows tr").first()).toBeVisible();
    await page.keyboard.press("Tab");
    await expect(page.locator(".skip-link")).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(page.locator("#main")).toBeFocused();
    await page.locator("#action").selectOption("entity_edit");
    await page.getByRole("button", { name: "Apply filters" }).focus();
    await page.keyboard.press("Enter");
    await expect(page).toHaveURL(/action=entity_edit/);
    await page.locator("#audit-rows a").first().focus();
    await page.keyboard.press("Enter");
    await expect(page.locator("body")).toHaveAttribute("data-admin-screen", "S-D2");
    await page.getByRole("link", { name: "Back to audit log" }).focus();
    await page.keyboard.press("Enter");
    await expect(page.locator("body")).toHaveAttribute("data-admin-screen", "S-D1");
    await runAxeAndSave(page, "td-adm-ui-na-kbd");
  });
});
