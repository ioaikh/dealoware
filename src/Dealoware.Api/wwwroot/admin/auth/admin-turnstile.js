/**
 * Security check (Cloudflare Turnstile) for the signed-out admin pages.
 *
 * States: loading, ready, failed, expired, unavailable, unconfigured.
 * A page may submit only in "ready", with the token from token(). Every
 * other state has no token, so a page that follows that rule can never
 * send a request the server has not been asked to verify.
 *
 * The page supplies the block (see AdminSignedOutPage.TurnstileBlock):
 *   #turnstile          data-sitekey, optional data-action, and the status
 *                       text for each state as data-loading / data-failed /
 *                       data-expired / data-unavailable / data-unconfigured
 *   #turnstile-status   where that text is shown (role="status")
 *   #turnstile-widget   where Cloudflare draws the widget
 *   #turnstile-retry    the Retry button
 *
 * With no data-sitekey the state is "unconfigured" for good: Cloudflare's
 * script is not loaded and there is nothing to retry.
 */
(function (global) {
  "use strict";

  var API_SRC = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";
  var LOAD_WAIT_MS = 10000;
  var STANDARD_WIDGET_WIDTH = 300;

  var token = "";
  var widgetId = null;
  var state = "loading";
  var onChange = null;
  var siteKey = "";
  var action = "";
  var root = null;
  var statusEl = null;
  var hostEl = null;
  var retryBtn = null;
  var scriptEl = null;
  var waitTimer = null;

  function apiReady() {
    return !!global.turnstile && typeof global.turnstile.render === "function";
  }

  function setState(next, freshToken) {
    state = next;
    token = next === "ready" ? freshToken || "" : "";
    if (root) root.setAttribute("data-state", next);
    AdminDom.setText(statusEl, next === "ready" || !root ? "" : root.getAttribute("data-" + next) || "");
    AdminDom.show(retryBtn, next === "failed" || next === "expired" || next === "unavailable");
    if (onChange) onChange(state, token);
  }

  function render() {
    if (!apiReady() || !hostEl) {
      setState("unavailable");
      return;
    }
    AdminDom.clear(hostEl);
    var options = {
      sitekey: siteKey,
      // The standard widget is 300px wide. Use the compact one in a narrower column.
      size: hostEl.clientWidth > 0 && hostEl.clientWidth < STANDARD_WIDGET_WIDTH ? "compact" : "normal",
      callback: function (value) {
        if (value) setState("ready", value);
        else setState("failed");
      },
      "error-callback": function () {
        setState("failed");
        return true;
      },
      "expired-callback": function () {
        setState("expired");
      },
      "timeout-callback": function () {
        setState("unavailable");
      },
      "unsupported-callback": function () {
        setState("unavailable");
      }
    };
    if (action) options.action = action;
    try {
      widgetId = global.turnstile.render(hostEl, options);
    } catch (e) {
      widgetId = null;
      setState("unavailable");
    }
  }

  /**
   * Loads Cloudflare's script, then draws the widget. After 10 seconds without
   * it the check is "unavailable". A request still on its way is not repeated:
   * Retry waits for it again, and if it arrives late the widget is drawn then.
   */
  function loadApi() {
    if (apiReady()) {
      render();
      return;
    }
    clearTimeout(waitTimer);
    waitTimer = setTimeout(function () {
      if (state === "loading") setState("unavailable");
    }, LOAD_WAIT_MS);
    if (scriptEl) return;

    var el = document.createElement("script");
    el.src = API_SRC;
    el.async = true;
    function arrived() {
      if (scriptEl === el) scriptEl = null;
      if (apiReady()) {
        if (state !== "loading" && state !== "unavailable") return;
        clearTimeout(waitTimer);
        if (state === "unavailable") setState("loading");
        render();
        return;
      }
      // Blocked or failed: drop the tag so Retry can ask again.
      if (el.parentNode) el.parentNode.removeChild(el);
      if (state !== "loading") return;
      clearTimeout(waitTimer);
      setState("unavailable");
    }
    el.addEventListener("load", arrived);
    el.addEventListener("error", arrived);
    scriptEl = el;
    document.head.appendChild(el);
  }

  /**
   * Asks for a fresh token: after Retry, and after any submit, because a
   * token is spent once the server has checked it.
   */
  function retry() {
    if (state === "unconfigured") return;
    setState("loading");
    if (apiReady() && widgetId !== null && typeof global.turnstile.reset === "function") {
      try {
        global.turnstile.reset(widgetId);
        return;
      } catch (e) {
        widgetId = null;
      }
    }
    loadApi();
  }

  function mount(options) {
    onChange = (options && options.onChange) || null;
    root = AdminDom.el("turnstile");
    statusEl = AdminDom.el("turnstile-status");
    hostEl = AdminDom.el("turnstile-widget");
    retryBtn = AdminDom.el("turnstile-retry");
    siteKey = (root && root.getAttribute("data-sitekey")) || "";
    action = (root && root.getAttribute("data-action")) || "";

    if (retryBtn) {
      retryBtn.addEventListener("click", function (event) {
        event.preventDefault();
        // Retry hides itself: keep keyboard focus on the status line.
        if (statusEl) statusEl.focus();
        retry();
      });
    }

    if (!siteKey) {
      setState("unconfigured");
      return;
    }
    setState("loading");
    loadApi();
  }

  global.AdminTurnstile = {
    mount: mount,
    retry: retry,
    token: function () { return token; },
    state: function () { return state; }
  };
})(window);
