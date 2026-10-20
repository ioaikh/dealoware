"use strict";

const assert = require("assert");
const { requireResetConfirmProxy } = require("./require-proxy.cjs");

function throws(label, value) {
  let err;
  try {
    requireResetConfirmProxy(value);
  } catch (e) {
    err = e;
  }
  assert.ok(err, label + " should throw");
}

throws("missing", undefined);
throws("empty", "");
throws("non-loopback", "http://10.0.0.1:18080");
throws("hostname localhost", "http://localhost:18080");
throws("low port", "http://127.0.0.1:80");
throws("implicit port", "http://127.0.0.1");

const ok = requireResetConfirmProxy("http://127.0.0.1:18080");
assert.strictEqual(ok.server, "http://127.0.0.1:18080");
requireResetConfirmProxy("http://127.0.0.1:1024");

process.stdout.write("PASS require-proxy checks\n");
