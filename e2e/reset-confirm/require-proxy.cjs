"use strict";

function parseUrl(raw, envName) {
  if (raw == null || String(raw).trim() === "") {
    throw new Error(envName + " is required");
  }

  let url;
  try {
    url = new URL(String(raw).trim());
  } catch {
    throw new Error(envName + " must be a valid URL");
  }

  if (url.username !== "" || url.password !== "") {
    throw new Error(envName + " must not include userinfo");
  }

  return url;
}

function requireResetConfirmProxy(raw) {
  const url = parseUrl(raw, "RESET_CONFIRM_PROXY");

  if (url.protocol !== "http:") {
    throw new Error("RESET_CONFIRM_PROXY must use http://");
  }

  if (url.hostname !== "127.0.0.1") {
    throw new Error("RESET_CONFIRM_PROXY must be 127.0.0.1");
  }

  const port = Number(url.port);
  if (!Number.isInteger(port) || port < 1024 || port > 65535) {
    throw new Error("RESET_CONFIRM_PROXY port must be an integer from 1024 to 65535");
  }

  if (url.pathname !== "" && url.pathname !== "/") {
    throw new Error("RESET_CONFIRM_PROXY must not include a path");
  }

  if (url.search !== "" || url.hash !== "") {
    throw new Error("RESET_CONFIRM_PROXY must not include a query or fragment");
  }

  return { server: "http://127.0.0.1:" + port };
}

function requireResetConfirmBaseUrl(raw) {
  const url = parseUrl(raw, "RESET_CONFIRM_BASE_URL");

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    throw new Error("RESET_CONFIRM_BASE_URL must use http:// or https://");
  }

  if (!url.hostname) {
    throw new Error("RESET_CONFIRM_BASE_URL host is required");
  }

  return url.origin;
}

module.exports = { requireResetConfirmProxy, requireResetConfirmBaseUrl };
