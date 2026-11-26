"use strict";

/**
 * The guard every signed-out page suite's Playwright configuration loads
 * (through playwright.base.cjs): the browser may only be sent through a
 * proxy at http://127.0.0.1:<port 1024-65535>, and the base URL must be a
 * plain http(s) origin. Anything else throws, naming the suite's variable,
 * so a run cannot start pointed anywhere but this machine.
 * Its own checks: require-proxy.check.cjs and playwright.base.check.cjs.
 */

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

function requireLoopbackProxy(raw, envName) {
  const url = parseUrl(raw, envName);

  if (url.protocol !== "http:") {
    throw new Error(envName + " must use http://");
  }

  if (url.hostname !== "127.0.0.1") {
    throw new Error(envName + " must be 127.0.0.1");
  }

  const port = Number(url.port);
  if (!Number.isInteger(port) || port < 1024 || port > 65535) {
    throw new Error(envName + " port must be an integer from 1024 to 65535");
  }

  if (url.pathname !== "" && url.pathname !== "/") {
    throw new Error(envName + " must not include a path");
  }

  if (url.search !== "" || url.hash !== "") {
    throw new Error(envName + " must not include a query or fragment");
  }

  return { server: "http://127.0.0.1:" + port };
}

function requireBaseUrl(raw, envName) {
  const url = parseUrl(raw, envName);

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    throw new Error(envName + " must use http:// or https://");
  }

  if (!url.hostname) {
    throw new Error(envName + " host is required");
  }

  return url.origin;
}

module.exports = { requireLoopbackProxy, requireBaseUrl };
