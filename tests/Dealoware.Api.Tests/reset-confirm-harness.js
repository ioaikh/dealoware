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

const path = require("path");
const scriptSource = fs.readFileSync(jsPath, "utf8");
// The page loads these first (AdminSignedOutPage.LinkScriptPath, DomScriptPath, PasswordScriptPath).
const assetDir = path.join(__dirname, "..", "..", "src", "Dealoware.Api", "wwwroot", "admin", "auth");
const helperSources = ["admin-link.js", "admin-dom.js", "admin-password.js"]
  .map((file) => fs.readFileSync(path.join(assetDir, file), "utf8"));
const htmlSource = fs.readFileSync(htmlPath, "utf8");
const headingText = "Set a new password";
const expiredUrl = "/admin/link-expired?for=reset";
const pathname = "/admin/reset/confirm";
const retryCopy = "We couldn't check your link. Wait a moment and try again.";
const formRetry = "We couldn't set your password. Wait a moment and try again.";
const changed = "Your password has been changed. Sign in with your new password and authenticator code.";
const origin = "https://admin.core.dealoware.com";

function classifyContext() {
  const start = scriptSource.indexOf("function dwClassifyCheckResponse");
  const end = scriptSource.indexOf("\n  var CHECK =", start);
  if (start < 0 || end < 0) {
    throw new Error("dwClassifyCheckResponse is missing from served JS");
  }
  const src = scriptSource.slice(start, end)
    + "; this.dwClassifyCheckResponse = dwClassifyCheckResponse;";
  const ctx = { globalThis: null };
  ctx.globalThis = ctx;
  vm.createContext(ctx);
  vm.runInContext(src, ctx);
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
    `<h1 id="page-heading">${headingText.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}<\\/h1>`
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
    hasAttribute(name) {
      return Object.prototype.hasOwnProperty.call(this.attributes, name);
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
  const form = makeEl("reset-form", "form", {
    hidden: true,
    parentNode: main,
    attrs: { "data-retry": formRetry, "data-changed": changed }
  });
  const formAlert = makeEl("form-alert", "div", { hidden: true, attrs: { role: "alert", tabindex: "-1" }, parentNode: form });
  const newPassword = makeEl("new-password", "input", {
    value: "",
    attrs: {
      "data-min-length": "15",
      "data-max-length": "128",
      "data-too-short": "Use at least 15 characters.",
      "data-too-long": "Use at most 128 characters."
    }
  });
  const newPasswordError = makeEl("new-password-error", "span");
  const confirmPassword = makeEl("confirm-password", "input", { attrs: { "data-mismatch": "These passwords don't match." } });
  const confirmPasswordError = makeEl("confirm-password-error", "span");
  const codeBlock = makeEl("code-field", "div");
  const totp = makeEl("code", "input", { attrs: { "data-required": "Enter the 6-digit code." } });
  const codeError = makeEl("code-error", "span");
  const recoveryBlock = makeEl("recovery-code-field", "div", { hidden: true });
  const recovery = makeEl("recovery-code", "input", { attrs: { "data-required": "Enter a recovery code." } });
  const recoveryError = makeEl("recovery-code-error", "span");
  const submit = makeEl("reset-submit", "button");
  const factorSwitch = makeEl("factor-switch", "button", {
    textContent: "Use a recovery code instead",
    attrs: { "data-to-recovery": "Use a recovery code instead", "data-to-code": "Use an authenticator code instead" }
  });
  const csrf = makeEl("dw-csrf", "meta", { attrs: { name: "dw-csrf", content: "csrf-test" } });
  const byId = {};
  for (const el of [main, heading, statusEl, alertEl, retryBtn, form, formAlert, newPassword, newPasswordError,
    confirmPassword, confirmPasswordError, codeBlock, totp, codeError, recoveryBlock, recovery, recoveryError,
    submit, factorSwitch]) {
    byId[el.id] = el;
  }

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

  const ctx = {
    document: documentRef,
    location: {
      hash: options.hash === undefined ? "#token=live-token" : options.hash,
      pathname,
      origin,
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
    URL,
    addEventListener() {},
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
  for (const helper of options.helpers === undefined ? helperSources : options.helpers) {
    vm.runInContext(helper, ctx);
  }
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
    formAlert,
    newPassword,
    newPasswordError,
    confirmPassword,
    confirmPasswordError,
    totp,
    codeError,
    codeBlock,
    recovery,
    recoveryBlock,
    factorSwitch,
    submit,
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
      // The form, filled in correctly, after the link check answered "usable".
      async function filledForm(confirmReply, checkReplies) {
        const confirms = [];
        let checks = 0;
        checkReplies = checkReplies || [];
        const session = boot({
          fetchImpl(url, init) {
            if (String(url).endsWith("/check")) {
              checks += 1;
              const next = checkReplies[checks - 2];
              if (checks === 1 || next === undefined) return jsonResponse(200, "{\"valid\":true}");
              return next();
            }
            confirms.push(JSON.parse(init.body));
            return confirmReply(confirms.length, init);
          }
        });
        await session.flush();
        assert.strictEqual(session.form.hidden, false);
        session.newPassword.value = "radio station later!";
        session.confirmPassword.value = "radio station later!";
        session.totp.value = "123 456";
        session.confirms = confirms;
        session.checks = () => checks;
        return session;
      }

      async function submitAndSettle(session) {
        session.form.dispatch("submit");
        await session.flush();
        await session.flush();
      }

      async function namedCase(name, fn) {
        await fn().then(() => results.push("PASS " + name))
          .catch((err) => results.push("FAIL " + name + ": " + err.stack));
      }

      await namedCase("success only on the exact reply: 200 with the server's sentence goes to sign-in#changed", async () => {
        const session = await filledForm(() => jsonResponse(200, JSON.stringify({ message: changed })));
        await submitAndSettle(session);
        assert.deepStrictEqual(session.replaced, ["/admin/sign-in#changed"]);
        // Only the contract fields: the one second factor on screen, spaces dropped from the code.
        assert.deepStrictEqual(
          JSON.parse(JSON.stringify(session.confirms)),
          [{ token: "live-token", newPassword: "radio station later!", totp: "123456" }]);
        assert.strictEqual(session.checks(), 1);
        // The submission follows redirects (to see the link-expired page); the check never does. No mode.
        assert.strictEqual(session.fetchCalls[1].init.redirect, undefined);
        assert.strictEqual(session.fetchCalls[1].init.mode, undefined);
        assert.strictEqual(session.fetchCalls[1].init.headers["X-CSRF-TOKEN"], "csrf-test");
      });

      const notTheReply = {
        "200 {}": () => jsonResponse(200, "{}"),
        "200 another message": () => jsonResponse(200, JSON.stringify({ message: "Done." })),
        "200 message not a string": () => jsonResponse(200, JSON.stringify({ message: true })),
        "200 not JSON": () => jsonResponse(200, "<html>"),
        "204": () => jsonResponse(204, ""),
        "redirect followed elsewhere": () => jsonResponse(200, JSON.stringify({ message: changed }), { redirected: true, url: origin + "/admin/sign-in" }),
        "500": () => jsonResponse(500, "error"),
        "no reply": () => Promise.reject(new Error("network"))
      };
      for (const [label, reply] of Object.entries(notTheReply)) {
        await namedCase("no clear answer (" + label + "): the link is checked again; usable, so the retry sentence and the form stays", async () => {
          const session = await filledForm(() => reply());
          await submitAndSettle(session);
          assert.deepStrictEqual(session.replaced, []);
          assert.strictEqual(session.checks(), 2);
          assert.strictEqual(session.formAlert.hidden, false);
          assert.strictEqual(session.formAlert.textContent, formRetry);
          assert.strictEqual(documentRef.activeElement, session.formAlert);
          assert.strictEqual(session.form.hidden, false);
          assert.strictEqual(session.retryBtn.hidden, true);
          assert.strictEqual(session.newPassword.value, "radio station later!");
          assert.strictEqual(session.confirmPassword.value, "radio station later!");
          assert.strictEqual(session.totp.value, "123 456");
          assert.strictEqual(session.submit.disabled, false);
        });
      }

      await namedCase("no clear answer and the link is spent: the change may have happened, so sign-in without the flag", async () => {
        const session = await filledForm(() => jsonResponse(500, "error"), [() => jsonResponse(200, "{\"valid\":false}")]);
        await submitAndSettle(session);
        assert.deepStrictEqual(session.replaced, ["/admin/sign-in"]);
      });

      await namedCase("time-out, a link that cannot be checked, then the spent link: sign-in, not the link-expired page", async () => {
        const session = await filledForm(
          (n) => (n === 1
            ? new Promise(() => {})
            : jsonResponse(200, "", { redirected: true, url: origin + "/admin/link-expired?for=reset" })),
          [() => jsonResponse(500, "error")]);
        session.form.dispatch("submit");
        await session.flush();
        session.fireTimeouts();
        await session.flush();
        await session.flush();
        assert.strictEqual(session.formAlert.textContent, formRetry);
        assert.deepStrictEqual(session.replaced, []);
        await submitAndSettle(session);
        assert.deepStrictEqual(session.replaced, ["/admin/sign-in"]);
        assert.strictEqual(session.confirms.length, 2);
      });

      await namedCase("time-out, a link still usable at the check, then the spent link: sign-in, not the link-expired page", async () => {
        // The server may commit the first request after the check said the link was still usable.
        const session = await filledForm(
          (n) => (n === 1
            ? new Promise(() => {})
            : jsonResponse(200, "", { redirected: true, url: origin + "/admin/link-expired?for=reset" })));
        session.form.dispatch("submit");
        await session.flush();
        session.fireTimeouts();
        await session.flush();
        await session.flush();
        assert.strictEqual(session.formAlert.textContent, formRetry);
        assert.deepStrictEqual(session.replaced, []);
        await submitAndSettle(session);
        assert.deepStrictEqual(session.replaced, ["/admin/sign-in"]);
        assert.strictEqual(session.confirms.length, 2);
      });

      await namedCase("a redirect to the link-expired page goes there", async () => {
        const session = await filledForm(() => jsonResponse(200, "", { redirected: true, url: origin + "/admin/link-expired?for=reset" }));
        await submitAndSettle(session);
        assert.deepStrictEqual(session.replaced, [expiredUrl]);
        assert.strictEqual(session.checks(), 1);
      });

      await namedCase("N2 a refused code shows the server's sentence as sent in one role=alert, clears the code, keeps the passwords", async () => {
        const said = "Any sentence the server sends.";
        const session = await filledForm(() => jsonResponse(401, JSON.stringify({ error: said })));
        await submitAndSettle(session);
        assert.strictEqual(session.formAlert.getAttribute("role"), "alert");
        assert.strictEqual(session.formAlert.textContent, said);
        assert.strictEqual(documentRef.activeElement, session.formAlert);
        assert.strictEqual(session.totp.value, "");
        assert.strictEqual(session.newPassword.value, "radio station later!");
        assert.strictEqual(session.confirmPassword.value, "radio station later!");
        assert.strictEqual(session.alertEl.hidden, true);
        assert.strictEqual(session.checks(), 1);
      });

      await namedCase("a password rule refusal (400): the named reason under New password, both fields kept, focus there", async () => {
        const reason = "This password is too common. Choose another.";
        const session = await filledForm(() => jsonResponse(400, JSON.stringify({ error: reason, reason: "common_or_context" })));
        await submitAndSettle(session);
        assert.strictEqual(session.newPasswordError.textContent, reason);
        assert.strictEqual(session.newPassword.getAttribute("aria-invalid"), "true");
        assert.strictEqual(documentRef.activeElement, session.newPassword);
        assert.strictEqual(session.newPassword.value, "radio station later!");
        assert.strictEqual(session.confirmPassword.value, "radio station later!");
        assert.strictEqual(session.totp.value, "123 456");
        assert.strictEqual(session.formAlert.hidden, true);
        assert.strictEqual(session.checks(), 1);
      });

      await namedCase("the switch sends the recovery code alone, and the authenticator field is emptied", async () => {
        const session = await filledForm(() => jsonResponse(200, JSON.stringify({ message: changed })));
        session.factorSwitch.dispatch("click");
        assert.strictEqual(session.codeBlock.hidden, true);
        assert.strictEqual(session.recoveryBlock.hidden, false);
        assert.strictEqual(session.totp.value, "");
        assert.strictEqual(session.factorSwitch.textContent, "Use an authenticator code instead");
        assert.strictEqual(documentRef.activeElement, session.recovery);
        session.recovery.value = "  abcd-efgh-jkmn ";
        await submitAndSettle(session);
        assert.deepStrictEqual(
          JSON.parse(JSON.stringify(session.confirms)),
          [{ token: "live-token", newPassword: "radio station later!", recoveryCode: "abcd-efgh-jkmn" }]);
      });

      await namedCase("the browser checks length, match and the code before sending anything", async () => {
        const session = await filledForm(() => jsonResponse(200, JSON.stringify({ message: changed })));
        session.newPassword.value = "x".repeat(14);
        session.confirmPassword.value = "y";
        session.totp.value = "12345";
        await submitAndSettle(session);
        assert.strictEqual(session.newPasswordError.textContent, "Use at least 15 characters.");
        assert.strictEqual(session.confirmPasswordError.textContent, "These passwords don't match.");
        assert.strictEqual(session.codeError.textContent, "Enter the 6-digit code.");
        assert.strictEqual(documentRef.activeElement, session.newPassword);
        assert.strictEqual(session.confirms.length, 0);
      });

      await namedCase("without its helper scripts the page takes the token out of the address bar and shows no form", async () => {
        const session = boot({ helpers: [], fetchImpl() { return jsonResponse(200, "{\"valid\":true}"); } });
        await session.flush();
        assert.strictEqual(session.ctx.location.hash, "");
        assert.strictEqual(session.fetchCalls.length, 0);
        assert.strictEqual(session.form.hidden, true);
        assert.strictEqual(session.alertEl.hidden, false);
        assert.strictEqual(session.alertEl.textContent, retryCopy);
      });
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
