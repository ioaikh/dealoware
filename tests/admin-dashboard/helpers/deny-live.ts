const LIVE_HOSTS = [
  "admin.core.dealoware.com",
  "core.dealoware.com",
];

export function assertNotLiveAdminHost(): void {
  if (process.env.ALLOW_LIVE_ADMIN_CORE === "1") {
    throw new Error("ALLOW_LIVE_ADMIN_CORE must never be set for this suite.");
  }

  const raw =
    process.env.ADMIN_BASE_URL ||
    process.env.PLAYWRIGHT_BASE_URL ||
    process.env.ADMIN_UI_BASE_URL ||
    "http://127.0.0.1:5055";

  let hostname = "";
  try {
    hostname = new URL(raw).hostname.toLowerCase();
  } catch {
    throw new Error(`Invalid ADMIN_BASE_URL: ${raw}`);
  }

  const live =
    LIVE_HOSTS.includes(hostname) ||
    hostname.endsWith(".dealoware.com");

  if (live) {
    throw new Error(
      `Refusing to run admin UI specs against live host ${hostname}. Use a local in-job URL.`,
    );
  }
}
