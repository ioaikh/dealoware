import { defineConfig } from "@playwright/test";
import path from "node:path";

const repoRoot = path.resolve(__dirname, "../..");

export default defineConfig({
  testDir: "./specs",
  fullyParallel: false,
  forbidOnly: true,
  retries: 0,
  workers: 1,
  reporter: [["list"]],
  outputDir: "./test-results",
  use: {
    baseURL: "http://127.0.0.1:5088",
    browserName: "chromium",
    headless: true,
    screenshot: "off",
    video: "off"
  },
  webServer: {
    command: "dotnet run --project src/Dealoware.Api --urls http://127.0.0.1:5088 --no-launch-profile",
    cwd: repoRoot,
    url: "http://127.0.0.1:5088/health",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: {
      ASPNETCORE_ENVIRONMENT: "Development",
      DOTNET_ROOT: process.env.DOTNET_ROOT ?? `${process.env.HOME}/.dotnet`,
      PATH: process.env.PATH ?? "",
      AdminHost__AllowedHosts__0: "admin.core.dealoware.com",
      AdminHost__AllowedHosts__1: "127.0.0.1"
    }
  }
});
