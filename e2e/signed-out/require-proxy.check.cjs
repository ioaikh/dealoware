"use strict";

const assert = require("assert");
const { requireLoopbackProxy, requireBaseUrl } = require("./require-proxy.cjs");

const PROXY = "EXAMPLE_E2E_PROXY";
const BASE = "EXAMPLE_E2E_BASE_URL";
const proxyGuard = (raw) => requireLoopbackProxy(raw, PROXY);
const baseGuard = (raw) => requireBaseUrl(raw, BASE);

function throws(fn, label) {
  let err;
  try {
    fn();
  } catch (e) {
    err = e;
  }
  assert.ok(err, label + " should throw");
}

throws(() => proxyGuard(undefined), "missing");
throws(() => proxyGuard(""), "empty");
throws(() => proxyGuard("http://10.0.0.1:18080"), "non-loopback");
throws(() => proxyGuard("http://localhost:18080"), "hostname localhost");
throws(() => proxyGuard("http://127.0.0.1:80"), "low port");
throws(() => proxyGuard("http://127.0.0.1:1023"), "port 1023");
throws(() => proxyGuard("http://127.0.0.1"), "implicit port");
throws(() => proxyGuard("http://[::1]:18080"), "::1");
throws(() => proxyGuard("http://0.0.0.0:18080"), "0.0.0.0");
throws(() => proxyGuard("http://127.0.0.1.nip.io:18080"), "nip.io");
throws(() => proxyGuard("http://[::ffff:127.0.0.1]:18080"), "IPv4-mapped IPv6");
throws(() => proxyGuard("http://127.0.0.1:0"), "port 0");
throws(() => proxyGuard("http://127.0.0.1:65536"), "port 65536");
throws(() => proxyGuard("socks5://127.0.0.1:18080"), "socks5");
throws(() => proxyGuard("https://127.0.0.1:18080"), "https proxy");
throws(() => proxyGuard("ftp://127.0.0.1:18080"), "ftp");
throws(() => proxyGuard("javascript:alert(1)"), "javascript scheme");
throws(() => proxyGuard("http://user:pass@127.0.0.1:18080"), "userinfo");
throws(() => proxyGuard("http://user@127.0.0.1:18080"), "username only");
throws(() => proxyGuard("http://127.0.0.1:18080/admin"), "path");
throws(() => proxyGuard("http://127.0.0.1:18080?x=1"), "query");

const ok = proxyGuard("http://127.0.0.1:18080");
assert.strictEqual(ok.server, "http://127.0.0.1:18080");
assert.strictEqual(
  proxyGuard("  http://127.0.0.1:1024/  ").server,
  "http://127.0.0.1:1024"
);
assert.strictEqual(
  proxyGuard("http://127.0.0.1:65535").server,
  "http://127.0.0.1:65535"
);

throws(() => baseGuard(undefined), "base missing");
throws(() => baseGuard(""), "base empty");
throws(() => baseGuard("ftp://admin.core.dealoware.com"), "base ftp");
throws(
  () => baseGuard("https://user:pass@admin.core.dealoware.com"),
  "base userinfo"
);
throws(() => baseGuard("javascript:alert(1)"), "base javascript");

assert.strictEqual(
  baseGuard("https://admin.core.dealoware.com"),
  "https://admin.core.dealoware.com"
);
assert.strictEqual(
  baseGuard("http://127.0.0.1:18443"),
  "http://127.0.0.1:18443"
);

process.stdout.write("PASS require-proxy checks\n");
