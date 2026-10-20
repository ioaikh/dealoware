"use strict";

const fs = require("fs");
const vm = require("vm");
const assert = require("assert");

const args = process.argv.slice(2);
const jsPath = args[0];
const htmlPath = args[1];
if (!jsPath || !htmlPath) {
  throw new Error("usage: node reset-confirm-harness.js <served-js> <served-html> [--page=reset] [--only=case]");
}

let page = "reset";
let only = null;
for (const arg of args.slice(2)) {
  if (arg.startsWith("--page=")) page = arg.slice("--page=".length);
  else if (arg.startsWith("--only=")) only = arg.slice("--only=".length);
}
if (page !== "reset") {
  throw new Error("page must be reset");
}

const scriptSource = fs.readFileSync(jsPath, "utf8");
const htmlSource = fs.readFileSync(htmlPath, "utf8");
const headingText = "Set a new password";
const expiredUrl = "/admin/link-expired?for=reset";
const pathname = "/admin/reset/confirm";
const retryCopy = "We couldn't check your link. Wait a moment and try again.";

function classifyContext() {
  const ctx = { __dwResetConfirmSkipBoot: true, globalThis: null };
  ctx.globalThis = ctx;
  vm.createContext(ctx);
  vm.runInContext(scriptSource, ctx);
  return ctx.dwClassifyCheckResponse;
}

function classify(status, redirected, body) {
  return classifyContext()(status, redirected, body);
}

const results = [];
function check(name, fn) {
  try {
    fn();
    results.push("PASS " + name);
  } catch (err) {
    results.push("FAIL " + name + ": " + (err && err.stack ? err.stack : err));
  }
}

function want(name) {
  return !only || only === name;
}

if (!only) {
  check("classifier form exact valid true", () => {
    assert.strictEqual(classify(200, false, "{\"valid\":true}"), "form");
  });
  check("classifier expired exact valid false", () => {
    assert.strictEqual(classify(200, false, "{\"valid\":false}"), "expired");
  });
  check("classifier retry empty object", () => {
    assert.strictEqual(classify(200, false, "{}"), "retry");
  });
  check("classifier retry extra field", () => {
    assert.strictEqual(classify(200, false, "{\"valid\":true,\"x\":1}"), "retry");
  });
  check("classifier retry string true", () => {
    assert.strictEqual(classify(200, false, "{\"valid\":\"true\"}"), "retry");
  });
  check("classifier retry non-json", () => {
    assert.strictEqual(classify(200, false, "not-json"), "retry");
  });
  check("classifier retry redirect flag", () => {
    assert.strictEqual(classify(200, false, "{\"valid\":true}"), "form");
    assert.strictEqual(classify(200, true, "{\"valid\":true}"), "retry");
  });
  check("classifier retry 303", () => {
    assert.strictEqual(classify(303, false, "{\"valid\":true}"), "retry");
  });
  check("classifier retry 429", () => {
    assert.strictEqual(classify(429, false, "{\"valid\":false}"), "retry");
  });
  check("classifier retry 500", () => {
    assert.strictEqual(classify(500, false, "{\"valid\":false}"), "retry");
  });
  check("classifier retry 0", () => {
    assert.strictEqual(classify(0, false, "{\"valid\":true}"), "retry");
  });
}

if (!only) {
  const headingRe = new RegExp(
    `<h1 id="reset-heading">${headingText.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}<\\/h1>`
  );
  assert.match(htmlSource, headingRe);
  assert.match(htmlSource, /id="reset-status" role="status" hidden/);
  assert.doesNotMatch(htmlSource, /<form[\s\S]*id="reset-form"[\s\S]*<\/form>[\s\S]*<h1/);
}

function makeEl(id, tag, opts) {
  opts = opts || {};
  const listeners = {};
  const el = {
    id,
    tagName: String(tag).toUpperCase(),
    hidden: !!opts.hidden,
    textContent: opts.textContent || "",
    value: opts.value || "",
    childNodes: [],
    parentNode: opts.parentNode || null,
    nextSibling: null,
    attributes: Object.assign({}, opts.attrs || {}),
    getAttribute(name) {
      return Object.prototype.hasOwnProperty.call(this.attributes, name)
        ? this.attributes[name]
        : null;
    },
    setAttribute(name, value) {
      this.attributes[name] = String(value);
    },
    removeAttribute(name) {
      delete this.attributes[name];
    },
    focus() {
      documentRef.activeElement = this;
    },
    closest(sel) {
      return sel === "label" ? this : null;
    },
    addEventListener(type, fn) {
      (listeners[type] || (listeners[type] = [])).push(fn);
    },
    dispatch(type, event) {
      (listeners[type] || []).forEach((fn) => fn(event || { preventDefault() {} }));
    }
  };
  return el;
}

let documentRef = { activeElement: null };

function abortable(result, signal) {
  return new Promise((resolve, reject) => {
    if (signal.aborted) {
      reject(Object.assign(new Error("Aborted"), { name: "AbortError" }));
      return;
    }
    const onAbort = () => {
      reject(Object.assign(new Error("Aborted"), { name: "AbortError" }));
    };
    signal.addEventListener("abort", onAbort, { once: true });
    Promise.resolve(result).then(
      (value) => {
        if (value && typeof value.text === "function") {
          const inner = value.text.bind(value);
          value.text = function () {
            return abortable(inner(), signal);
          };
        }
        if (signal.aborted) {
          reject(Object.assign(new Error("Aborted"), { name: "AbortError" }));
          return;
        }
        resolve(value);
      },
      reject
    ).finally(() => signal.removeEventListener("abort", onAbort));
  });
}

function boot(options) {
  options = options || {};
  const replaced = [];
  const timeouts = new Map();
  let nextTimer = 1;
  const fetchCalls = [];
  let fetchImpl = options.fetchImpl;

  const main = makeEl("reset-main", "main", { attrs: { "aria-busy": "true" } });
  const heading = makeEl("reset-heading", "h1", { textContent: headingText, parentNode: main });
  const statusEl = makeEl("reset-status", "p", {
    hidden: true,
    textContent: "Checking your link…",
    attrs: { role: "status" },
    parentNode: main
  });
  const alertEl = makeEl("reset-alert", "p", {
    hidden: true,
    attrs: { tabindex: "-1", "data-retry": retryCopy },
    parentNode: main
  });
  const retryBtn = makeEl("reset-retry", "button", {
    hidden: true,
    textContent: "Try again",
    parentNode: main
  });
  const form = makeEl("reset-form", "form", { hidden: true, parentNode: main });
  const token = makeEl("token", "input", { value: "" });
  const newPassword = makeEl("newPassword", "input", { value: "" });
  const totp = makeEl("totp", "input", { value: "" });
  const recovery = makeEl("recoveryCode", "input", { value: "" });
  const csrf = makeEl("dw-csrf", "meta", { attrs: { name: "dw-csrf", content: "csrf-test" } });
  const byId = {
    "reset-main": main,
    "reset-heading": heading,
    "reset-status": statusEl,
    "reset-alert": alertEl,
    "reset-retry": retryBtn,
    "reset-form": form,
    token,
    newPassword,
    totp,
    recoveryCode: recovery
  };

  documentRef = {
    activeElement: { tagName: "BODY" },
    getElementById(id) { return byId[id] || null; },
    querySelector(sel) {
      if (sel === 'meta[name="dw-csrf"]') return csrf;
      return null;
    },
    createElement(tag) { return makeEl("", tag); }
  };
  heading.parentNode = main;
  statusEl.parentNode = main;
  alertEl.parentNode = main;
  retryBtn.parentNode = main;
  form.parentNode = main;
  newPassword.parentNode = form;
  newPassword.closest = () => newPassword;

  const ctx = {
    document: documentRef,
    location: {
      hash: options.hash === undefined ? "#token=live-token" : options.hash,
      pathname,
      replace(url) { replaced.push(url); }
    },
    history: { replaceState() { ctx.location.hash = ""; } },
    fetch(url, init) {
      fetchCalls.push({ url, init });
      const result = fetchImpl(url, init, fetchCalls.length);
      const signal = init && init.signal;
      if (!signal) return result;
      return abortable(result, signal);
    },
    AbortController,
    setTimeout(fn, ms) {
      const id = nextTimer++;
      timeouts.set(id, { fn, ms });
      return id;
    },
    clearTimeout(id) { timeouts.delete(id); },
    JSON,
    Object,
    Array,
    Error,
    console,
    globalThis: null
  };
  ctx.globalThis = ctx;
  ctx.window = ctx;
  vm.createContext(ctx);
  vm.runInContext(scriptSource, ctx);

  function fireTimeouts() {
    for (const [id, timer] of [...timeouts]) {
      timeouts.delete(id);
      timer.fn();
    }
  }

  async function flush() {
    for (let i = 0; i < 12; i++) await Promise.resolve();
  }

  return {
    ctx,
    replaced,
    fetchCalls,
    timeouts,
    fireTimeouts,
    flush,
    heading,
    statusEl,
    alertEl,
    retryBtn,
    form,
    main,
    newPassword,
    totp,
    recovery,
    setFetch(impl) { fetchImpl = impl; }
  };
}

function jsonResponse(status, body, extra) {
  extra = extra || {};
  return Promise.resolve({
    status,
    ok: status >= 200 && status < 300,
    redirected: !!extra.redirected,
    type: extra.type || "basic",
    url: extra.url || "",
    text() {
      if (typeof extra.bodyDelay === "function") return extra.bodyDelay().then(() => body);
      return Promise.resolve(body);
    }
  });
}

function opaqueRedirect(url) {
  return Promise.resolve({
    status: 0,
    ok: false,
    redirected: false,
    type: "opaqueredirect",
    url,
    text() { return Promise.resolve(""); }
  });
}

function assertNoNavigate(session) {
  assert.deepStrictEqual(session.replaced, []);
}

function assertManualRedirect(session) {
  assert.ok(session.fetchCalls.length >= 1);
  assert.strictEqual(session.fetchCalls[0].init.redirect, "manual");
}

function assertRetryVisible(session) {
  assert.strictEqual(session.form.hidden, true);
  assert.strictEqual(session.retryBtn.hidden, false);
  assert.strictEqual(session.alertEl.hidden, false);
  assert.strictEqual(session.alertEl.textContent, retryCopy);
  assert.strictEqual(session.heading.textContent, headingText);
  assertNoNavigate(session);
}

function passName(label) {
  return "TOK-AC-14 " + page + " " + label;
}

const retryReplies = {
  "400": () => jsonResponse(400, "no"),
  "403": () => jsonResponse(403, "no"),
  "404": () => jsonResponse(404, "no"),
  "204": () => jsonResponse(204, ""),
  "302-link-expired": () => opaqueRedirect("/admin/link-expired"),
  "200-unparseable": () => jsonResponse(200, "not-json{"),
  "200-empty": () => jsonResponse(200, "{}"),
  "200-valid-yes": () => jsonResponse(200, "{\"valid\":\"yes\"}"),
  "200-valid-null": () => jsonResponse(200, "{\"valid\":null}"),
  "200-valid-true-extra": () => jsonResponse(200, "{\"valid\":true,\"x\":1}"),
  "200-valid-false-reason": () => jsonResponse(200, "{\"valid\":false,\"reason\":\"expired\"}")
};

(async () => {
  for (const [label, factory] of Object.entries(retryReplies)) {
    if (!want(label)) continue;
    await (async () => {
      const session = boot({ fetchImpl() { return factory(); } });
      await session.flush();
      assertManualRedirect(session);
      assertRetryVisible(session);
    })().then(() => results.push("PASS " + passName(label)))
      .catch((err) => results.push("FAIL " + passName(label) + ": " + err.stack));
  }

  if (want("200-valid-true")) {
    await (async () => {
      const session = boot({
        fetchImpl() { return jsonResponse(200, "{\"valid\":true}"); }
      });
      await session.flush();
      assertManualRedirect(session);
      assert.strictEqual(session.form.hidden, false);
      assert.strictEqual(session.retryBtn.hidden, true);
      assert.strictEqual(session.heading.textContent, headingText);
      assertNoNavigate(session);
    })().then(() => results.push("PASS " + passName("200-valid-true")))
      .catch((err) => results.push("FAIL " + passName("200-valid-true") + ": " + err.stack));
  }

  if (want("200-valid-false")) {
    await (async () => {
      const session = boot({
        fetchImpl() { return jsonResponse(200, "{\"valid\":false}"); }
      });
      await session.flush();
      assertManualRedirect(session);
      assert.deepStrictEqual(session.replaced, [expiredUrl]);
      assert.strictEqual(session.form.hidden, true);
    })().then(() => results.push("PASS " + passName("200-valid-false")))
      .catch((err) => results.push("FAIL " + passName("200-valid-false") + ": " + err.stack));
  }

  if (want("tok-ac-15")) {
    await (async () => {
      let checks = 0;
      let releaseFirstBody;
      const firstBody = new Promise((resolve) => { releaseFirstBody = resolve; });
      let releaseSecond;
      const secondGate = new Promise((resolve) => { releaseSecond = resolve; });
      const session = boot({
        fetchImpl() {
          checks += 1;
          if (checks === 1) {
            return jsonResponse(200, "{\"valid\":true}", { bodyDelay: () => firstBody });
          }
          return secondGate.then(() => jsonResponse(200, "{\"valid\":true}"));
        }
      });
      await session.flush();
      assert.strictEqual(session.form.hidden, true);
      session.fireTimeouts();
      await session.flush();
      assertRetryVisible(session);
      session.retryBtn.dispatch("click");
      await session.flush();
      assert.strictEqual(checks, 2);
      releaseFirstBody();
      await session.flush();
      assert.strictEqual(session.form.hidden, true);
      assertNoNavigate(session);
      assert.strictEqual(checks, 2);
      releaseSecond();
      await session.flush();
      assert.strictEqual(session.form.hidden, false);
      assert.strictEqual(documentRef.activeElement, session.newPassword);
    })().then(() => results.push("PASS TOK-AC-15 " + page + " late answer ignored and new check proceeds"))
      .catch((err) => results.push("FAIL TOK-AC-15 " + page + ": " + err.stack));
  }

  if (want("tok-ac-16")) {
    await (async () => {
      const session = boot({
        fetchImpl() { return jsonResponse(500, "error"); }
      });
      await session.flush();
      session.retryBtn.dispatch("click");
      await session.flush();
      assert.strictEqual(session.retryBtn.textContent, "Try again");
      assert.strictEqual(session.retryBtn.getAttribute("aria-disabled"), null);
      assert.strictEqual(documentRef.activeElement, session.alertEl);
      assert.strictEqual(session.alertEl.getAttribute("role"), null);
      assert.strictEqual(session.alertEl.getAttribute("aria-live"), null);
      assert.strictEqual(session.retryBtn.getAttribute("aria-describedby"), "reset-alert");
      assert.strictEqual(session.heading.hidden, false);
    })().then(() => results.push("PASS TOK-AC-16 " + page + " rule C repeat failure focuses non-live message"))
      .catch((err) => results.push("FAIL TOK-AC-16 " + page + ": " + err.stack));
  }

  if (!only) {
    await (async () => {
      let releaseBody;
      const bodyGate = new Promise((resolve) => { releaseBody = resolve; });
      const session = boot({
        fetchImpl() {
          return jsonResponse(200, "{\"valid\":true}", { bodyDelay: () => bodyGate });
        }
      });
      await session.flush();
      assert.strictEqual(session.form.hidden, true);
      assert.strictEqual(session.replaced.length, 0);
      session.fireTimeouts();
      await session.flush();
      releaseBody();
      await session.flush();
      assert.strictEqual(session.form.hidden, true);
      assert.strictEqual(session.retryBtn.hidden, false);
      assert.strictEqual(session.replaced.length, 0);
      assert.strictEqual(documentRef.activeElement, session.retryBtn);
    })().then(() => results.push("PASS timeout covers body read and ignores late valid true"))
      .catch((err) => results.push("FAIL timeout covers body read: " + err.stack));

    await (async () => {
      const session = boot({
        fetchImpl() { return jsonResponse(500, "error"); }
      });
      await session.flush();
      assert.strictEqual(session.retryBtn.hidden, false);
      assert.strictEqual(session.retryBtn.textContent, "Try again");
      assert.strictEqual(documentRef.activeElement, session.retryBtn);
      assert.strictEqual(session.alertEl.hidden, false);
      assert.strictEqual(session.alertEl.textContent, retryCopy);
      assert.strictEqual(session.alertEl.getAttribute("role"), null);
      assert.strictEqual(session.statusEl.hidden, true);
      assert.strictEqual(session.statusEl.textContent, "");
      assert.strictEqual(session.heading.hidden, false);
      assert.strictEqual(session.heading.textContent, headingText);
      assert.strictEqual(session.form.hidden, true);
      assert.ok(!session.main.getAttribute("aria-busy"));
    })().then(() => results.push("PASS state 4 first failure focuses Try again"))
      .catch((err) => results.push("FAIL state 4: " + err.stack));

    await (async () => {
      let checks = 0;
      let releaseSecond;
      const secondGate = new Promise((resolve) => { releaseSecond = resolve; });
      const session = boot({
        fetchImpl() {
          checks += 1;
          if (checks === 1) return jsonResponse(500, "error");
          return secondGate.then(() => jsonResponse(200, "{\"valid\":true}"));
        }
      });
      await session.flush();
      session.retryBtn.dispatch("click");
      await session.flush();
      assert.strictEqual(session.retryBtn.getAttribute("aria-disabled"), "true");
      assert.strictEqual(session.retryBtn.textContent, "Checking…");
      assert.strictEqual(documentRef.activeElement, session.retryBtn);
      assert.strictEqual(session.retryBtn.hidden, false);
      assert.ok(!session.main.getAttribute("aria-busy"));
      assert.strictEqual(session.alertEl.hidden, false);
      const during = checks;
      session.retryBtn.dispatch("click");
      session.retryBtn.dispatch("click");
      await session.flush();
      assert.strictEqual(checks, during);
      releaseSecond();
      await session.flush();
      assert.strictEqual(session.form.hidden, false);
      assert.strictEqual(documentRef.activeElement, session.newPassword);
      assert.strictEqual(session.heading.hidden, false);
    })().then(() => results.push("PASS rule B retry busy then rule A focus first field"))
      .catch((err) => results.push("FAIL rules A/B: " + err.stack));

    await (async () => {
      const session = boot({
        fetchImpl() { return jsonResponse(500, "error"); }
      });
      await session.flush();
      session.retryBtn.dispatch("click");
      await session.flush();
      assert.strictEqual(session.retryBtn.textContent, "Try again");
      assert.strictEqual(session.retryBtn.getAttribute("aria-disabled"), null);
      assert.strictEqual(documentRef.activeElement, session.alertEl);
      assert.strictEqual(session.alertEl.getAttribute("role"), null);
      assert.strictEqual(session.alertEl.getAttribute("aria-live"), null);
      assert.strictEqual(session.retryBtn.getAttribute("aria-describedby"), "reset-alert");
      assert.strictEqual(session.heading.hidden, false);
    })().then(() => results.push("PASS rule C repeat failure focuses non-live message"))
      .catch((err) => results.push("FAIL rule C: " + err.stack));

    if (page === "reset") {
      await (async () => {
        let confirms = 0;
        const session = boot({
          fetchImpl(url) {
            if (String(url).includes("/check")) return jsonResponse(200, "{\"valid\":true}");
            confirms += 1;
            return Promise.reject(new Error("network"));
          }
        });
        await session.flush();
        session.newPassword.value = "radio station later!";
        session.totp.value = "123456";
        session.form.dispatch("submit");
        await session.flush();
        assert.strictEqual(session.retryBtn.hidden, false);
        assert.strictEqual(documentRef.activeElement, session.retryBtn);
        session.retryBtn.dispatch("click");
        await session.flush();
        assert.strictEqual(session.retryBtn.hidden, false);
        assert.notStrictEqual(documentRef.activeElement, null);
        assert.ok(documentRef.activeElement === session.retryBtn || documentRef.activeElement === session.alertEl);
        assert.strictEqual(documentRef.activeElement.hidden, false);
        assert.ok(confirms >= 2);
      })().then(() => results.push("PASS N1 submit retry keeps focus on a visible control"))
        .catch((err) => results.push("FAIL N1: " + err.stack));

      await (async () => {
        const session = boot({
          fetchImpl(url) {
            if (String(url).includes("/check")) return jsonResponse(200, "{\"valid\":true}");
            return jsonResponse(401, "{\"error\":\"We couldn't sign you in. Check your details and try again later. You can also reset your password.\"}");
          }
        });
        await session.flush();
        session.newPassword.value = "radio station later!";
        session.totp.value = "123456";
        session.form.dispatch("submit");
        await session.flush();
        assert.strictEqual(session.alertEl.getAttribute("role"), "alert");
        assert.strictEqual(documentRef.activeElement, session.alertEl);
        assert.strictEqual(
          session.alertEl.textContent,
          "We couldn't sign you in. Check your details and try again later. You can also reset your password."
        );
      })().then(() => results.push("PASS N2 sign-in failure is one role=alert with focus"))
        .catch((err) => results.push("FAIL N2: " + err.stack));
    }

    await (async () => {
      const session = boot({
        fetchImpl() { return jsonResponse(200, "{\"valid\":false}"); }
      });
      await session.flush();
      assert.deepStrictEqual(session.replaced, [expiredUrl]);
      assert.ok(session.fetchCalls[0].init.redirect === "manual");
    })().then(() => results.push(
      "PASS exact valid false goes to S-A5 ?for=reset"
      + " without following redirects"
    ))
      .catch((err) => results.push("FAIL expired redirect: " + err.stack));

    await (async () => {
      const session = boot({ hash: "", fetchImpl() { return jsonResponse(200, "{\"valid\":true}"); } });
      await session.flush();
      assert.deepStrictEqual(session.replaced, [expiredUrl]);
      assert.strictEqual(session.fetchCalls.length, 0);
    })().then(() => results.push(
      "PASS no fragment goes to S-A5 ?for=reset with no check"
    ))
      .catch((err) => results.push("FAIL no fragment: " + err.stack));
  }

  const failed = results.filter((line) => line.startsWith("FAIL"));
  process.stdout.write(results.join("\n") + "\n");
  if (failed.length) {
    process.stderr.write(failed.join("\n") + "\n");
    process.exit(1);
  }
})().catch((err) => {
  process.stderr.write(String(err && err.stack ? err.stack : err) + "\n");
  process.exit(1);
});
