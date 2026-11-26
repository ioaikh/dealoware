/**
 * First-time setup page (S-A4, /admin/bootstrap).
 *
 * admin-link.js has already run: the link token is out of the address bar
 * and is handed over here, where it stays in memory only. A reload therefore
 * has no token and lands on the "link no longer works" page; the link in the
 * email still works.
 *
 * Both requests go to POST /admin/api/auth/bootstrap.
 *
 * 1. Link check, before any form is shown: {token, check: true}.
 *    200 {"valid": true} -> show the form. Redirect to the link-expired page
 *    -> go there. Anything else -> "try again", as on the reset page.
 *    A dead link therefore never shows the password form.
 *
 * 2. Set the password: {token, newPassword, turnstileToken}. Confirm password
 *    is compared here and never sent.
 *    200 {"passwordSet": true} -> sign-in. Redirect to the link-expired page
 *    -> go there. 400 {error} -> under New password. 401 {error} and
 *    429 {message} -> the alert, as the server words them.
 *    No clear answer (network failure, time-out, a server error, a reply
 *    that does not say passwordSet) -> the password may or may not be set,
 *    so the link is checked again: no longer usable -> sign-in, which will
 *    tell; still usable -> the retry sentence and the form stays.
 */
(function () {
  "use strict";

  var API = "/admin/api/auth/bootstrap";
  var EXPIRED = "/admin/link-expired?for=setup";
  var EXPIRED_PATH = "/admin/link-expired";
  var SIGN_IN = "/admin/sign-in";
  var TIMEOUT_MS = 15000;

  var token = AdminLink.takeToken();
  if (!token) {
    location.replace(EXPIRED);
    return;
  }

  var csrfMeta = document.querySelector('meta[name="dw-csrf"]');
  var csrf = csrfMeta ? csrfMeta.getAttribute("content") || "" : "";

  var linkStatus = AdminDom.el("link-status");
  var linkAlert = AdminDom.el("link-alert");
  var linkRetry = AdminDom.el("link-retry");
  var panel = AdminDom.el("bootstrap-panel");
  var form = AdminDom.el("bootstrap-form");
  var alertEl = AdminDom.el("form-alert");
  var newPassword = AdminDom.el("new-password");
  var newPasswordError = AdminDom.el("new-password-error");
  var confirmPassword = AdminDom.el("confirm-password");
  var confirmPasswordError = AdminDom.el("confirm-password-error");
  var submit = AdminDom.el("bootstrap-submit");
  if (!linkStatus || !linkAlert || !linkRetry || !panel || !form || !newPassword || !confirmPassword || !submit) {
    return;
  }

  var checking = false;
  var busy = false;
  var retryIdleLabel = linkRetry.textContent || "";

  function parse(raw) {
    try {
      var body = JSON.parse(raw);
      return body && typeof body === "object" && !Array.isArray(body) ? body : null;
    } catch (e) {
      return null;
    }
  }

  function text(value) {
    return typeof value === "string" ? value : "";
  }

  /** The redirect target when it is this site's link-expired page, else "". */
  function expiredTarget(url) {
    try {
      var target = new URL(url, location.origin);
      if (target.origin === location.origin && target.pathname === EXPIRED_PATH) {
        return target.pathname + target.search;
      }
    } catch (e) {
      /* not a usable address */
    }
    return "";
  }

  /**
   * The one way this page talks to the server. Resolves with the response
   * and its parsed JSON body (null when it has none); rejects on a network
   * failure or when 15 seconds pass before the whole reply has arrived.
   */
  function post(body) {
    var controller = new AbortController();
    var timer = setTimeout(function () { controller.abort(); }, TIMEOUT_MS);
    return fetch(API, {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": csrf },
      body: JSON.stringify(body),
      credentials: "same-origin",
      signal: controller.signal
    }).then(function (r) {
      return r.text().then(function (raw) {
        return { r: r, body: parse(raw) };
      });
    }).then(function (reply) {
      clearTimeout(timer);
      return reply;
    }, function (err) {
      clearTimeout(timer);
      throw err;
    });
  }

  /**
   * Asks whether the link is usable. Never rejects. Resolves with
   * "usable"  - 200 and exactly {"valid": true};
   * "expired" - the server redirected to the link-expired page;
   * "unknown" - anything else: throttled, an error, no answer in time.
   */
  function checkLink() {
    return post({ token: token, check: true }).then(function (reply) {
      if (reply.r.redirected) return expiredTarget(reply.r.url) ? "expired" : "unknown";
      var body = reply.body;
      if (reply.r.status === 200 && body && Object.keys(body).length === 1 && body.valid === true) {
        return "usable";
      }
      return "unknown";
    }, function () {
      return "unknown";
    });
  }

  /* ---- Before the form: checking the link ---- */

  function setRetryBusy(on) {
    if (on) {
      linkRetry.setAttribute("aria-disabled", "true");
      AdminDom.setText(linkRetry, linkRetry.getAttribute("data-busy") || retryIdleLabel);
    } else {
      linkRetry.removeAttribute("aria-disabled");
      AdminDom.setText(linkRetry, retryIdleLabel);
    }
  }

  /** The check could not be made. First time: focus Try again. Again: focus the message. */
  function showCheckRetry(again) {
    setRetryBusy(false);
    AdminDom.setText(linkStatus, "");
    AdminDom.show(linkStatus, false);
    AdminDom.setText(linkAlert, linkAlert.getAttribute("data-retry") || "");
    AdminDom.show(linkAlert, true);
    AdminDom.show(linkRetry, true);
    linkRetry.setAttribute("aria-describedby", linkAlert.id);
    if (again) linkAlert.focus();
    else linkRetry.focus();
  }

  function showForm() {
    AdminDom.setText(linkStatus, "");
    AdminDom.show(linkStatus, false);
    AdminDom.show(linkAlert, false);
    AdminDom.show(linkRetry, false);
    // Show the form first: the security check sizes its widget to the visible column.
    AdminDom.show(panel, true);
    AdminTurnstile.mount({ onChange: syncSubmit });
    syncSubmit();
    newPassword.focus();
  }

  function runCheck() {
    if (checking) return;
    checking = true;
    var again = !linkRetry.hidden;
    if (again) {
      setRetryBusy(true);
      linkRetry.focus();
    } else {
      AdminDom.show(linkStatus, true);
      AdminDom.setText(linkStatus, linkStatus.getAttribute("data-checking") || "");
    }
    checkLink().then(function (outcome) {
      checking = false;
      if (outcome === "usable") showForm();
      else if (outcome === "expired") location.replace(EXPIRED);
      else showCheckRetry(again);
    });
  }

  linkRetry.addEventListener("click", function (event) {
    event.preventDefault();
    if (linkRetry.getAttribute("aria-disabled") === "true") return;
    runCheck();
  });

  /* ---- The form ---- */

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

  /** The form is usable again. The security-check token is spent, so ask for a new one. */
  function finish() {
    setBusy(false);
    AdminTurnstile.retry();
  }

  /**
   * No clear answer came back, so the password may or may not have been set.
   * If the link is no longer usable the set-up may have gone through: go to
   * sign-in, which will tell. Otherwise nothing changed: say so, keep the form.
   */
  function afterUnclearReply() {
    checkLink().then(function (outcome) {
      if (outcome === "expired") {
        location.replace(SIGN_IN);
        return;
      }
      showAlert(retryCopy());
      finish();
    });
  }

  function send(password, turnstileToken) {
    setBusy(true);
    post({ token: token, newPassword: password, turnstileToken: turnstileToken }).then(function (reply) {
      var r = reply.r;
      var body = reply.body || {};
      if (r.redirected) {
        var expired = expiredTarget(r.url);
        if (expired) location.replace(expired);
        else afterUnclearReply();
        return;
      }
      if (r.ok) {
        if (body.passwordSet === true) location.replace(SIGN_IN);
        else afterUnclearReply();
        return;
      }
      if (r.status < 400 || r.status >= 500) {
        afterUnclearReply();
        return;
      }
      // 4xx: the server refused, so nothing was set.
      var error = text(body.error);
      var message = text(body.message);
      if (r.status === 400 && error) showPasswordError(error);
      else if (r.status === 401 && error) showAlert(error);
      else if (r.status === 429 && message) showAlert(message);
      else showAlert(retryCopy());
      finish();
    }, afterUnclearReply);
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

  runCheck();
})();
