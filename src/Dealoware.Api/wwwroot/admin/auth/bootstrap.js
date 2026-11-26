/**
 * First-time setup page (S-A4, /admin/bootstrap).
 *
 * The link token is handled as on the reset page (reset-confirm.js): read
 * from the URL fragment, removed from the address bar at once, and kept only
 * in this script's memory. A reload therefore has no token and lands on the
 * "link no longer works" page; the link in the email still works.
 *
 * Request: POST /admin/api/auth/bootstrap {token, newPassword, turnstileToken}.
 * Confirm password is compared here and never sent.
 * Replies: 200 -> sign-in; 303 -> the link-expired page; 400 {error} -> under
 * New password; 401 {error} and 429 {message} -> the alert, as the server
 * words them; anything else -> the page's own retry sentence.
 */
(function () {
  "use strict";

  var API = "/admin/api/auth/bootstrap";
  var EXPIRED = "/admin/link-expired?for=setup";
  var EXPIRED_PATH = "/admin/link-expired";
  var SIGN_IN = "/admin/sign-in";
  var TIMEOUT_MS = 15000;

  var hash = location.hash;
  history.replaceState(null, "", location.pathname);
  var match = /[#&]token=([^&]*)/.exec(hash);
  var token = "";
  if (match) {
    try {
      token = decodeURIComponent(match[1].replace(/\+/g, " "));
    } catch (e) {
      token = "";
    }
  }

  if (!token) {
    location.replace(EXPIRED);
    return;
  }

  var csrfMeta = document.querySelector('meta[name="dw-csrf"]');
  var csrf = csrfMeta ? csrfMeta.getAttribute("content") || "" : "";

  var panel = AdminDom.el("bootstrap-panel");
  var form = AdminDom.el("bootstrap-form");
  var alertEl = AdminDom.el("form-alert");
  var newPassword = AdminDom.el("new-password");
  var newPasswordError = AdminDom.el("new-password-error");
  var confirmPassword = AdminDom.el("confirm-password");
  var confirmPasswordError = AdminDom.el("confirm-password-error");
  var submit = AdminDom.el("bootstrap-submit");
  if (!panel || !form || !newPassword || !confirmPassword || !submit) return;

  var busy = false;

  function retryCopy() {
    return form.getAttribute("data-retry") || "";
  }

  /** Submit works only with a fresh security-check token and no request in flight. */
  function syncSubmit() {
    var ready = AdminTurnstile.state() === "ready" && !!AdminTurnstile.token();
    submit.disabled = busy || !ready;
  }

  function setBusy(on) {
    busy = on;
    if (on) form.setAttribute("aria-busy", "true");
    else form.removeAttribute("aria-busy");
    syncSubmit();
  }

  function showAlert(message) {
    AdminDom.setText(alertEl, message);
    AdminDom.show(alertEl, true);
    if (alertEl) alertEl.focus();
  }

  function hideAlert() {
    AdminDom.setText(alertEl, "");
    AdminDom.show(alertEl, false);
  }

  function checkLength() {
    var message = AdminPassword.lengthMessage(newPassword);
    AdminDom.setInvalid(newPassword, newPasswordError, message);
    return message;
  }

  function checkMatch() {
    var message = newPassword.value === confirmPassword.value
      ? ""
      : confirmPassword.getAttribute("data-mismatch") || "";
    AdminDom.setInvalid(confirmPassword, confirmPasswordError, message);
    return message;
  }

  function showPasswordError(message) {
    AdminDom.setInvalid(newPassword, newPasswordError, message);
    newPassword.focus();
  }

  function parse(text) {
    try {
      var body = JSON.parse(text);
      return body && typeof body === "object" ? body : null;
    } catch (e) {
      return null;
    }
  }

  function text(value) {
    return typeof value === "string" ? value : "";
  }

  /** The server's redirect target when it is the link-expired page, else the known one. */
  function expiredTarget(url) {
    try {
      var target = new URL(url, location.origin);
      if (target.origin === location.origin && target.pathname === EXPIRED_PATH) {
        return target.pathname + target.search;
      }
    } catch (e) {
      /* fall through */
    }
    return EXPIRED;
  }

  /** The form is usable again. The security-check token is spent, so ask for a new one. */
  function finish() {
    setBusy(false);
    AdminTurnstile.retry();
  }

  function send(password, turnstileToken) {
    setBusy(true);
    var controller = new AbortController();
    var timer = setTimeout(function () { controller.abort(); }, TIMEOUT_MS);
    fetch(API, {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": csrf },
      body: JSON.stringify({
        token: token,
        newPassword: password,
        turnstileToken: turnstileToken
      }),
      credentials: "same-origin",
      signal: controller.signal
    }).then(function (r) {
      if (r.redirected) {
        location.replace(expiredTarget(r.url));
        return;
      }
      if (r.ok) {
        location.replace(SIGN_IN);
        return;
      }
      return r.text().then(function (raw) {
        var body = parse(raw) || {};
        var error = text(body.error);
        var message = text(body.message);
        if (r.status === 400 && error) showPasswordError(error);
        else if (r.status === 401 && error) showAlert(error);
        else if (r.status === 429 && message) showAlert(message);
        else showAlert(retryCopy());
        finish();
      });
    }).catch(function () {
      showAlert(retryCopy());
      finish();
    }).finally(function () {
      clearTimeout(timer);
    });
  }

  form.addEventListener("submit", function (event) {
    event.preventDefault();
    if (busy) return;
    hideAlert();
    var lengthError = checkLength();
    var mismatch = checkMatch();
    if (lengthError) {
      newPassword.focus();
      return;
    }
    if (mismatch) {
      confirmPassword.focus();
      return;
    }
    var turnstileToken = AdminTurnstile.token();
    if (AdminTurnstile.state() !== "ready" || !turnstileToken) return;
    send(newPassword.value, turnstileToken);
  });

  newPassword.addEventListener("blur", function () {
    if (newPassword.value) checkLength();
  });
  newPassword.addEventListener("input", function () {
    if (newPassword.hasAttribute("aria-invalid")) AdminDom.setInvalid(newPassword, newPasswordError, "");
  });
  confirmPassword.addEventListener("input", function () {
    if (confirmPassword.hasAttribute("aria-invalid")) AdminDom.setInvalid(confirmPassword, confirmPasswordError, "");
  });

  AdminPassword.bindToggle("new-password", "new-password-toggle");
  AdminPassword.bindToggle("confirm-password", "confirm-password-toggle");

  // Show the form first: the security check sizes its widget to the visible column.
  AdminDom.show(panel, true);
  AdminTurnstile.mount({ onChange: syncSubmit });
  syncSubmit();
  newPassword.focus();
})();
