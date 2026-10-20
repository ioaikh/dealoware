"use strict";

const assert = require("assert");
const {
  requireResetConfirmProxy,
  requireResetConfirmBaseUrl
} = require("./require-proxy.cjs");

function throws(fn, label) {
  let err;
  try {
    fn();
  } catch (e) {
    err = e;
  }
  assert.ok(err, label + " should throw");
}

throws(() => requireResetConfirmProxy(undefined), "missing");
throws(() => requireResetConfirmProxy(""), "empty");
throws(() => requireResetConfirmProxy("http://10.0.0.1:18080"), "non-loopback");
throws(() => requireResetConfirmProxy("http://localhost:18080"), "hostname localhost");
throws(() => requireResetConfirmProxy("http://127.0.0.1:80"), "low port");
throws(() => requireResetConfirmProxy("http://127.0.0.1"), "implicit port");
throws(() => requireResetConfirmProxy("http://[::1]:18080"), "::1");
throws(() => requireResetConfirmProxy("http://0.0.0.0:18080"), "0.0.0.0");
throws(() => requireResetConfirmProxy("http://127.0.0.1.nip.io:18080"), "nip.io");
throws(() => requireResetConfirmProxy("http://[::ffff:127.0.0.1]:18080"), "IPv4-mapped IPv6");
throws(() => requireResetConfirmProxy("http://127.0.0.1:0"), "port 0");
throws(() => requireResetConfirmProxy("http://127.0.0.1:65536"), "port 65536");
throws(() => requireResetConfirmProxy("socks5://127.0.0.1:18080"), "socks5");
throws(() => requireResetConfirmProxy("https://127.0.0.1:18080"), "https proxy");
throws(() => requireResetConfirmProxy("ftp://127.0.0.1:18080"), "ftp");
throws(() => requireResetConfirmProxy("javascript:alert(1)"), "javascript scheme");
throws(() => requireResetConfirmProxy("http://user:pass@127.0.0.1:18080"), "userinfo");
throws(() => requireResetConfirmProxy("http://user@127.0.0.1:18080"), "username only");
throws(() => requireResetConfirmProxy("http://127.0.0.1:18080/admin"), "path");
throws(() => requireResetConfirmProxy("http://127.0.0.1:18080?x=1"), "query");

const ok = requireResetConfirmProxy("http://127.0.0.1:18080");
assert.strictEqual(ok.server, "http://127.0.0.1:18080");
assert.strictEqual(
  requireResetConfirmProxy("  http://127.0.0.1:1024/  ").server,
  "http://127.0.0.1:1024"
);

throws(() => requireResetConfirmBaseUrl(undefined), "base missing");
throws(() => requireResetConfirmBaseUrl(""), "base empty");
throws(() => requireResetConfirmBaseUrl("ftp://admin.core.dealoware.com"), "base ftp");
throws(
  () => requireResetConfirmBaseUrl("https://user:pass@admin.core.dealoware.com"),
  "base userinfo"
);
throws(() => requireResetConfirmBaseUrl("javascript:alert(1)"), "base javascript");

assert.strictEqual(
  requireResetConfirmBaseUrl("https://admin.core.dealoware.com"),
  "https://admin.core.dealoware.com"
);
assert.strictEqual(
  requireResetConfirmBaseUrl("http://127.0.0.1:18443"),
  "http://127.0.0.1:18443"
);

process.stdout.write("PASS require-proxy checks\n");
