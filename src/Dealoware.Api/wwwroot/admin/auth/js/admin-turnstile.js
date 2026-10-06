/**
 * Turnstile widget states (UXR-A03): loading, ready, failed, expired, unavailable.
 * Primary submit stays disabled until a fresh token exists.
 */
(function (global) {
  "use strict";

  var token = "";
  var widgetId = null;
  var state = "loading";
  var onChange = null;

  function currentToken() {
    return token;
  }

  function setState(next, message) {
    state = next;
    var status = AdminDom.el("turnstile-status");
    var retry = AdminDom.el("turnstile-retry");
    if (next === "ready") {
      AdminDom.setText(status, "");
      AdminDom.show(status, false);
      AdminDom.show(retry, false);
    } else {
      AdminDom.setText(status, message || "");
      AdminDom.show(status, true);
      AdminDom.show(retry, next === "failed" || next === "expired" || next === "unavailable");
    }
    if (onChange) onChange(state, token);
  }

  function clearToken() {
    token = "";
    if (onChange) onChange(state, token);
  }

  function renderWidget(siteKey) {
    var host = AdminDom.el("turnstile-widget");
    if (!host) return;
    AdminDom.clear(host);
    if (global.__ADMIN_AUTH_TEST__ && global.__ADMIN_AUTH_TEST__.renderTurnstile) {
      global.__ADMIN_AUTH_TEST__.renderTurnstile(host, {
        siteKey: siteKey,
        setReady: function (value) {
          token = value || "test-turnstile-token";
          setState("ready");
        },
        setFailed: function () {
          token = "";
          setState("failed", AdminCopy.turnstileFailed);
        },
        setExpired: function () {
          token = "";
          setState("expired", AdminCopy.turnstileExpired);
        },
        setUnavailable: function () {
          token = "";
          setState("unavailable", AdminCopy.turnstileUnavailable);
        },
        setLoading: function () {
          token = "";
          setState("loading", AdminCopy.turnstileLoading);
        }
      });
      return;
    }

    if (!global.turnstile || typeof global.turnstile.render !== "function") {
      setState("unavailable", AdminCopy.turnstileUnavailable);
      return;
    }

    widgetId = global.turnstile.render(host, {
      sitekey: siteKey,
      callback: function (value) {
        token = value || "";
        setState("ready");
      },
      "error-callback": function () {
        token = "";
        setState("failed", AdminCopy.turnstileFailed);
      },
      "expired-callback": function () {
        token = "";
        setState("expired", AdminCopy.turnstileExpired);
      },
      "timeout-callback": function () {
        token = "";
        setState("unavailable", AdminCopy.turnstileUnavailable);
      }
    });
  }

  function retry() {
    token = "";
    setState("loading", AdminCopy.turnstileLoading);
    if (global.turnstile && widgetId !== null && typeof global.turnstile.reset === "function") {
      global.turnstile.reset(widgetId);
      return;
    }
    var siteKey = (global.__ADMIN_AUTH_SITE_KEY__ || "");
    renderWidget(siteKey);
  }

  function mount(options) {
    onChange = options && options.onChange;
    setState("loading", AdminCopy.turnstileLoading);
    var retryBtn = AdminDom.el("turnstile-retry");
    if (retryBtn) {
      retryBtn.addEventListener("click", function (event) {
        event.preventDefault();
        retry();
      });
    }

    var test = global.__ADMIN_AUTH_TEST__;
    if (test && test.siteKey) {
      global.__ADMIN_AUTH_SITE_KEY__ = test.siteKey;
      renderWidget(test.siteKey);
      return Promise.resolve();
    }

    var waitMs = 10000;
    var started = Date.now();
    return AdminApi.publicConfig().then(function (result) {
      var siteKey = result.payload && result.payload.turnstileSiteKey;
      if (!siteKey) {
        setState("unavailable", AdminCopy.turnstileUnavailable);
        return;
      }
      global.__ADMIN_AUTH_SITE_KEY__ = siteKey;

      function waitForApi() {
        if (global.turnstile || (Date.now() - started) > waitMs) {
          renderWidget(siteKey);
          return;
        }
        window.setTimeout(waitForApi, 100);
      }
      waitForApi();
    }, function () {
      setState("unavailable", AdminCopy.turnstileUnavailable);
    });
  }

  global.AdminTurnstile = {
    mount: mount,
    token: currentToken,
    state: function () { return state; },
    retry: retry,
    clearToken: clearToken
  };
})(window);
