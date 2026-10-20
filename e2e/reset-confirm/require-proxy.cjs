"use strict";

function requireResetConfirmProxy(raw) {
  if (raw == null || String(raw).trim() === "") {
    throw new Error("RESET_CONFIRM_PROXY is required");
  }

  let url;
  try {
    url = new URL(String(raw));
  } catch {
    throw new Error("RESET_CONFIRM_PROXY must be a URL on 127.0.0.1 with port >= 1024");
  }

  if (url.hostname !== "127.0.0.1") {
    throw new Error("RESET_CONFIRM_PROXY must be 127.0.0.1");
  }

  const port = Number(url.port);
  if (!Number.isInteger(port) || port < 1024) {
    throw new Error("RESET_CONFIRM_PROXY port must be >= 1024");
  }

  return { server: String(raw) };
}

module.exports = { requireResetConfirmProxy };
