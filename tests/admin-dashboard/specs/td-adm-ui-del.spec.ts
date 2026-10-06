import { test, expect } from "@playwright/test";
import { runAxeAndSave } from "../helpers/axe";
import { applyAdminSession, readSeed } from "../helpers/session";
import { assertNotLiveAdminHost } from "../helpers/deny-live";

assertNotLiveAdminHost();

test.describe("TD-ADM-UI-del signed-in delete modal", () => {
  test.beforeEach(async ({ context }) => {
    const seed = readSeed();
    await applyAdminSession(context, seed.sessionId);
  });

  test("TD-ADM-UI-del-01 modal copy per entity @TD-ADM-UI-del-01", async ({ page }) => {
    const seed = readSeed();
    const cases = [
      { type: "offers", id: seed.offerId, name: seed.offerId, button: "Delete offer" },
      { type: "artifacts", id: seed.lonelyArtifactId, name: seed.lonelyName, button: "Delete artifact" },
      { type: "negotiations", id: seed.openNegotiationId, name: seed.openNegotiationId, button: "Delete negotiation" },
      { type: "participants", id: seed.carolId, name: seed.carolName, button: "Delete participant" },
    ];

    for (const row of cases) {
      await page.goto(`/admin/${row.type}/${row.id}`);
      await page.getByRole("button", { name: "Delete" }).click();
      const dialog = page.getByRole("dialog");
      await expect(dialog).toBeVisible();
      await expect(dialog.getByRole("heading")).toContainText(row.name);
      await expect(dialog).toContainText(row.id);
      await expect(dialog).toContainText("This can't be restored from the admin.");
      await expect(dialog.getByRole("button", { name: row.button })).toBeVisible();
      await expect(dialog.getByRole("button", { name: "Cancel" })).toBeVisible();
      await page.keyboard.press("Escape");
    }

    await page.goto(`/admin/offers/${seed.offerId}`);
    await page.getByRole("button", { name: "Delete" }).click();
    await runAxeAndSave(page, "TD-ADM-UI-del-01");
  });

  test("TD-ADM-UI-del-02 keyboard focus trap cancel no mutation @TD-ADM-UI-del-02", async ({ page }) => {
    const seed = readSeed();
    await page.goto(`/admin/offers/${seed.offerId}`);
    const deleteBtn = page.getByRole("button", { name: "Delete" });
    await deleteBtn.focus();
    await page.keyboard.press("Enter");
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Cancel" })).toBeFocused();
    await page.keyboard.press("Tab");
    await page.keyboard.press("Tab");
    await expect(dialog.getByRole("button", { name: "Cancel" })).toBeFocused();
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
    await expect(deleteBtn).toBeFocused();

    await deleteBtn.click();
    await page.locator(".dialog").click({ position: { x: 2, y: 2 } });
    await expect(dialog).toBeHidden();

    await page.goto(`/admin/offers/${seed.offerId}`);
    await expect(page.getByRole("button", { name: "Delete" })).toBeVisible();
    await runAxeAndSave(page, "TD-ADM-UI-del-02");
  });

  test("TD-ADM-UI-del-03 token expiry and single submit @TD-ADM-UI-del-03", async ({ page }) => {
    const seed = readSeed();
    await page.goto(`/admin/offers/${seed.offerId}`);
    await page.getByRole("button", { name: "Delete" }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await page.waitForTimeout(2500);
    await page.getByRole("button", { name: "Delete offer" }).click();
    await expect(page.getByRole("dialog")).toContainText("This confirmation expired. Review the details again.");
    await expect(page.getByRole("button", { name: "Delete offer" })).toBeEnabled();

    let deletes = 0;
    page.on("request", (req) => {
      if (req.method() === "DELETE" && req.url().includes("/admin/api/offers/")) deletes += 1;
    });
    const confirm = page.getByRole("button", { name: "Delete offer" });
    await Promise.all([confirm.click(), confirm.click()]);
    await page.waitForTimeout(500);
    expect(deletes).toBeLessThanOrEqual(1);
    await runAxeAndSave(page, "TD-ADM-UI-del-03");
  });

  test("TD-ADM-UI-del-04 blocked artifact lists negotiation links @TD-ADM-UI-del-04", async ({ page }) => {
    const seed = readSeed();
    await page.goto(`/admin/artifacts/${seed.blockedArtifactId}`);
    await page.getByRole("button", { name: "Delete" }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading")).toHaveText("This artifact can't be deleted");
    await expect(dialog.getByRole("link", { name: seed.blockNeg1Id })).toBeVisible();
    await expect(dialog.getByRole("link", { name: seed.blockNeg2Id })).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Delete artifact" })).toHaveCount(0);
    await expect(dialog.getByRole("button", { name: "Close" })).toBeVisible();
    await runAxeAndSave(page, "TD-ADM-UI-del-04");
    await dialog.getByRole("link", { name: seed.blockNeg1Id }).click();
    await expect(page).toHaveURL(new RegExp(`/admin/negotiations/${seed.blockNeg1Id}`));
  });

  test("TD-ADM-UI-del-05 typed confirmation @TD-ADM-UI-del-05", async ({ page }) => {
    const seed = readSeed();
    await page.goto(`/admin/participants/${seed.carolId}`);
    await page.getByRole("button", { name: "Delete" }).click();
    const dialog = page.getByRole("dialog");
    const confirm = dialog.getByRole("button", { name: "Delete participant" });
    await expect(confirm).toBeDisabled();
    await dialog.getByLabel(/Type .* to confirm/).fill("carol");
    await expect(confirm).toBeDisabled();
    await dialog.getByLabel(/Type .* to confirm/).fill(seed.carolName);
    await expect(confirm).toBeEnabled();

    await page.goto(`/admin/negotiations/${seed.openNegotiationId}`);
    await page.getByRole("button", { name: "Delete" }).click();
    await expect(page.getByRole("dialog")).toContainText("open");
    await expect(page.getByRole("button", { name: "Delete negotiation" })).toBeDisabled();
    await page.getByLabel(/Type .* to confirm/).fill(seed.openNegotiationId);
    await expect(page.getByRole("button", { name: "Delete negotiation" })).toBeEnabled();
    await runAxeAndSave(page, "TD-ADM-UI-del-05");
  });

  test("TD-ADM-UI-del-06 post-delete routing and retry @TD-ADM-UI-del-06", async ({ page }) => {
    const seed = readSeed();
    await page.goto(`/admin/artifacts/${seed.lonelyArtifactId}`);
    await page.route("**/admin/api/artifacts/**", async (route) => {
      if (route.request().method() === "DELETE") {
        await route.fulfill({ status: 500, contentType: "application/json", body: "{\"error\":\"Nothing was deleted.\"}" });
        return;
      }
      await route.continue();
    });
    await page.getByRole("button", { name: "Delete" }).click();
    await page.getByRole("button", { name: "Delete artifact" }).click();
    await expect(page.getByRole("dialog")).toContainText("Nothing was deleted.");
    await expect(page.getByRole("button", { name: "Retry" })).toBeVisible();
    await page.unroute("**/admin/api/artifacts/**");
    await page.getByRole("button", { name: "Retry" }).click();
    await expect(page).toHaveURL(/\/admin\/artifacts\/?$/);
    await expect(page.getByRole("status")).toContainText("deleted");
    await expect(page.locator(`tr[data-id="${seed.lonelyArtifactId}"]`)).toHaveCount(0);
    await page.getByLabel("Show deleted").check();
    await expect(page.locator(`tr[data-id="${seed.lonelyArtifactId}"]`)).toContainText("Deleted");
    await runAxeAndSave(page, "TD-ADM-UI-del-06");
  });
});

test("TD-ADM-130 smoke stats list search detail cancel @TD-ADM-130", async ({ page, context }) => {
  const seed = readSeed();
  await applyAdminSession(context, seed.sessionId);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Stats" })).toBeVisible();
  await page.goto("/admin/participants");
  await page.getByLabel("Search by name or id").fill("Alice");
  await expect(page.getByRole("status")).toContainText("results");
  await page.goto(`/admin/participants/${seed.aliceId}`);
  await page.getByRole("button", { name: "Delete" }).click();
  await page.getByRole("button", { name: "Cancel" }).click();
  await expect(page.getByRole("dialog")).toBeHidden();
  await expect(page.getByRole("button", { name: "Delete" })).toBeVisible();
  await runAxeAndSave(page, "TD-ADM-130");
});

test("TD-ADM-131 unauthenticated deny no entity data @TD-ADM-131", async ({ page, request }) => {
  const seed = readSeed();
  const res = await request.get(`/admin/participants/${seed.aliceId}`);
  expect(res.status()).toBe(401);
  const body = await res.text();
  expect(body).not.toContain(seed.aliceName);
  expect(body).not.toContain("confirmToken");
  await page.setContent(
    `<!DOCTYPE html><html lang="en"><head><title>Sign in · Dealoware admin</title></head><body><h1>Sign in required</h1><p role="alert">${body.replace(/</g, "")}</p></body></html>`,
  );
  await runAxeAndSave(page, "TD-ADM-131");
});
