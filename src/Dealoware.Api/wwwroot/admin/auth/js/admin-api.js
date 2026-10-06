/**
 * Single client module for Steps 2–5 auth API routes and JSON shapes.
 * Paths follow route-prefix note r3 (b079a814) §2.2 optional auth JSON.
 * Every exempt POST sends X-CSRF-TOKEN (C3). Return path is never put in a
 * sign-in query string (C6).
 *
 * Assumed endpoints (admin host only; listed exemptions only):
 *   POST /admin/api/auth/sign-in
 *        body { email, password, turnstileToken }
 *        → 200 { next: "code" } + pending-auth HttpOnly cookie
 *        → 401/429 { error }
 *   POST /admin/api/auth/sign-in/code
 *        body { code }
 *        → 200 { next: "home"|"return", returnPath? }
 *        → 401/429 { error, restart?: true }
 *   POST /admin/api/auth/sign-in/recovery
 *        body { recoveryCode }
 *        → 200 { next: "home"|"return", returnPath?, remainingRecoveryCodes?: number }
 *        → 401/429 { error, restart?: true }
 *   POST /admin/api/auth/reset
 *        body { email, turnstileToken }
 *        → 200 {} (same status/body for unknown email, wrong password, or hold) | 429 { error }
 *   POST /admin/api/auth/bootstrap
 *        body { token, password, confirmPassword, turnstileToken }
 *        → 200 { next: "authenticator" }
 *        → 400 { error, reason?: "too_short"|"too_long"|"common"|"same_as_current"|"mismatch" }
 *        → 404 { next: "link-expired", variant: "bootstrap" }
 *   POST /admin/api/auth/reset/confirm
 *        body { token, password, confirmPassword, code?: string, recoveryCode?: string }
 *        → 200 { next: "signin", status: "password-set" }
 *        → 400 { error, reason? } | 401 { error } | 404 { next: "link-expired", variant: "reset" }
 *   POST /admin/api/auth/sign-out
 *        → 204 (idempotent; anti-forgery required)
 *   POST /admin/api/settings/password  (session required; not exempt)
 *        body { currentPassword, newPassword, confirmPassword, code?, recoveryCode? }
 *        → 200 { next: "signin", status: "password-changed" }
 *        → 400 { error, reason?: "too_short"|"too_long"|"common"|"same_as_current"|"mismatch" }
 *        → 401 { error }  (step-up failure; same body each time)
 *        → 401 { error, signedOut: true }  (account hold; land on S-A1)
 *        No Turnstile. Notice email is out of v1.
 *
 * Not exempt (session or later PRs): GET /admin/api/me, enroll JSON, any other /admin/api/**
 * TOTP enroll/confirm are assumed as later Step 3 routes and are called only after
 * enrol pending (dw_admin_pending=enrol in Development) has passed the session gate.
 */
(function (global) {
  "use strict";

  var ROUTES = {
    signIn: "/admin/api/auth/sign-in",
    signInCode: "/admin/api/auth/sign-in/code",
    signInRecovery: "/admin/api/auth/sign-in/recovery",
    reset: "/admin/api/auth/reset",
    bootstrap: "/admin/api/auth/bootstrap",
    resetConfirm: "/admin/api/auth/reset/confirm",
    signOut: "/admin/api/auth/sign-out",
    totpEnroll: "/admin/api/auth/totp/enroll",
    totpEnrollConfirm: "/admin/api/auth/totp/enroll/confirm",
    passwordChange: "/admin/api/settings/password",
    me: "/admin/api/me"
  };

  var PAGES = {
    signIn: "/admin/sign-in",
    signInCode: "/admin/sign-in/code",
    signInRecovery: "/admin/sign-in/recovery",
    bootstrap: "/admin/bootstrap",
    linkExpired: "/admin/link-expired",
    authenticator: "/admin/setup/authenticator",
    recoveryCodes: "/admin/setup/recovery-codes",
    reset: "/admin/reset",
    resetSent: "/admin/reset/sent",
    resetConfirm: "/admin/reset/confirm",
    security: "/admin/settings/security",
    stats: "/admin/"
  };

  function readError(payload, fallback) {
    if (payload && typeof payload.error === "string" && payload.error.length)
      return payload.error;
    return fallback || global.AdminCopy.signInFailure;
  }

  var csrfToken = "";

  function ensureCsrf() {
    if (csrfToken) return Promise.resolve(csrfToken);
    return fetch(PAGES.signIn, {
      method: "GET",
      credentials: "same-origin",
      headers: { Accept: "text/html" }
    }).then(function (response) {
      csrfToken = response.headers.get("X-CSRF-TOKEN") || "";
      return csrfToken;
    });
  }

  function send(method, url, body, csrf) {
    var headers = { Accept: "application/json" };
    if (csrf) headers["X-CSRF-TOKEN"] = csrf;
    var init = {
      method: method,
      credentials: "same-origin",
      headers: headers
    };
    if (body !== undefined) {
      headers["Content-Type"] = "application/json";
      init.body = JSON.stringify(body);
    }
    return fetch(url, init).then(function (response) {
      var headerToken = response.headers.get("X-CSRF-TOKEN");
      if (headerToken) csrfToken = headerToken;
      return response.text().then(function (text) {
        var payload = null;
        if (text) {
          try { payload = JSON.parse(text); }
          catch (e) { payload = null; }
        }
        return {
          ok: response.ok,
          status: response.status,
          payload: payload,
          retryAfter: response.headers.get("Retry-After")
        };
      });
    });
  }

  function request(method, url, body) {
    if (method === "GET") return send(method, url, body, "");
    return ensureCsrf().then(function (csrf) {
      return send(method, url, body, csrf);
    });
  }

  function queryToken() {
    return new URLSearchParams(window.location.search).get("token") || "";
  }

  function queryParam(name) {
    return new URLSearchParams(window.location.search).get(name) || "";
  }

  function safeReturnPath(value) {
    if (!value) return PAGES.stats;
    if (value.length > 2048) return PAGES.stats;
    if (value.indexOf("://") !== -1) return PAGES.stats;
    if (value.indexOf("//") === 0) return PAGES.stats;
    if (value.indexOf("javascript:") === 0) return PAGES.stats;
    if (value.charAt(0) !== "/") return PAGES.stats;
    if (value.indexOf("/admin/") !== 0 && value !== "/admin") return PAGES.stats;
    if (value === "/admin") return PAGES.stats;
    if (value.indexOf("/admin/api") === 0) return PAGES.stats;
    if (value.indexOf("/admin/sign-in") === 0) return PAGES.stats;
    if (value.indexOf("/admin/setup/") === 0) return PAGES.stats;
    if (value.indexOf("/admin/settings/") === 0) return PAGES.stats;
    if (value.indexOf("/admin/sign-out") === 0) return PAGES.stats;
    if (value.indexOf("/admin/reset") === 0) return PAGES.stats;
    if (value.indexOf("/admin/bootstrap") === 0) return PAGES.stats;
    if (value.indexOf("/admin/link-expired") === 0) return PAGES.stats;
    if (value.indexOf("?") !== -1) return PAGES.stats;
    return value;
  }

  function go(path) {
    window.location.assign(path);
  }

  global.AdminApi = {
    ROUTES: ROUTES,
    PAGES: PAGES,
    request: request,
    readError: readError,
    queryToken: queryToken,
    queryParam: queryParam,
    safeReturnPath: safeReturnPath,
    go: go,
    signInStep1: function (body) {
      return request("POST", ROUTES.signIn, body);
    },
    signInCode: function (body) {
      return request("POST", ROUTES.signInCode, body);
    },
    signInRecovery: function (body) {
      return request("POST", ROUTES.signInRecovery, body);
    },
    signInCancel: function () {
      return request("POST", ROUTES.signOut);
    },
    bootstrapSet: function (body) {
      return request("POST", ROUTES.bootstrap, body);
    },
    totpEnroll: function () {
      return request("POST", ROUTES.totpEnroll);
    },
    totpEnrollConfirm: function (body) {
      return request("POST", ROUTES.totpEnrollConfirm, body);
    },
    resetRequest: function (body) {
      return request("POST", ROUTES.reset, body);
    },
    resetComplete: function (body) {
      return request("POST", ROUTES.resetConfirm, body);
    },
    changePassword: function (body) {
      return request("POST", ROUTES.passwordChange, body);
    },
    me: function () {
      return request("GET", ROUTES.me);
    }
  };
})(window);
