/**
 * Set a new password from the emailed link (S-A10, /admin/reset/confirm).
 *
 * admin-link.js has already run: the link token is out of the address bar
 * and is handed over here, where it stays in memory only. A reload therefore
 * has no token and lands on the "link no longer works" page; the link in the
 * email still works.
 *
 * 1. Link check, before any form is shown:
 *    POST /admin/api/auth/reset/confirm/check with {token}.
 *    200 exactly {"valid": true} -> the form. 200 exactly {"valid": false}
 *    -> the link-expired page. Anything else -> "Try again".
 *    A dead link therefore never shows the password form.
 *
 * 2. Set the password: POST /admin/api/auth/reset/confirm with {token,
 *    newPassword} and the one second factor on screen, {totp} or
 *    {recoveryCode}. Confirm password is compared here and never sent.
 *    200 {"message"} carrying exactly the server's success sentence (the
 *    form's data-changed) -> sign-in, which then says the password was
 *    changed. Redirect to the link-expired page -> go there.
 *    400 {error}   -> under New password, as the server words it.
 *    401 {error}   -> the alert, as sent; the code is cleared.
 *    429 {message} -> the alert, as sent.
 *    Any other refusal (4xx) -> nothing was changed: the retry sentence.
 *    No clear answer (no reply, a time-out, a server error, a 200 that does
 *    not carry that sentence, a redirect elsewhere) -> the password may or
 *    may not have been changed, so the link is checked again: no longer
 *    usable -> sign-in, which will tell; still usable, or no answer -> the
 *    retry sentence. Wherever the page stays, what was typed stays, except
 *    a refused code.
 */
(function () {
  function dwClassifyCheckResponse(status, redirected, bodyText) {
    if (redirected) return "retry";
    if (status !== 200) return "retry";
    var body;
    try {
      body = JSON.parse(bodyText);
    } catch (e) {
      return "retry";
    }
    if (!body || typeof body !== "object" || Array.isArray(body)) return "retry";
    var keys = Object.keys(body);
    if (keys.length !== 1 || keys[0] !== "valid") return "retry";
    if (body.valid === true) return "form";
    if (body.valid === false) return "expired";
    return "retry";
  }

  var CHECK = "/admin/api/auth/reset/confirm/check";
  var CONFIRM = "/admin/api/auth/reset/confirm";
  var EXPIRED = "/admin/link-expired?for=reset";
  var EXPIRED_PATH = "/admin/link-expired";
  // The sign-in page, with the fixed flag that makes it say the password was changed.
  var SIGN_IN = "/admin/sign-in#changed";
  // The sign-in page as it is, for when it is not known whether the password was changed.
  var SIGN_IN_PAGE = "/admin/sign-in";
  var TIMEOUT_MS = 15000;
  var RETRY_CHECKING = "Checking…";

  var statusEl = document.getElementById("reset-status");
  var alertEl = document.getElementById("reset-alert");
  var retryBtn = document.getElementById("reset-retry");
  var form = document.getElementById("reset-form");
  var main = document.getElementById("reset-main");

  // A script this page needs did not arrive. The token leaves the address
  // bar here too; then the page says so and shows no form.
  if (typeof AdminLink === "undefined" || typeof AdminDom === "undefined" || typeof AdminPassword === "undefined") {
    history.replaceState(null, "", location.pathname);
    if (alertEl) {
      alertEl.textContent = alertEl.getAttribute("data-retry") || "";
      alertEl.hidden = false;
    }
    if (main) main.removeAttribute("aria-busy");
    return;
  }

  var token = AdminLink.takeToken();

  var csrfMeta = document.querySelector('meta[name="dw-csrf"]');
  var csrf = csrfMeta ? csrfMeta.getAttribute("content") || "" : "";

  var formAlert = AdminDom.el("form-alert");
  var newPassword = AdminDom.el("new-password");
  var newPasswordError = AdminDom.el("new-password-error");
  var confirmPassword = AdminDom.el("confirm-password");
  var confirmPasswordError = AdminDom.el("confirm-password-error");
  var submitBtn = AdminDom.el("reset-submit");
  var factorSwitch = AdminDom.el("factor-switch");

  // The two second factors. One is on screen at a time, and only it is sent.
  var FACTORS = {
    code: {
      block: AdminDom.el("code-field"),
      field: AdminDom.el("code"),
      error: AdminDom.el("code-error"),
      // Spaces are dropped ("123 456" is how many apps show it); then six digits.
      read: function (raw) { return raw.replace(/\s+/g, ""); },
      wellFormed: function (value) { return /^[0-9]{6}$/.test(value); },
      sendAs: "totp",
      other: "recovery",
      switchLabel: "data-to-recovery"
    },
    recovery: {
      block: AdminDom.el("recovery-code-field"),
      field: AdminDom.el("recovery-code"),
      error: AdminDom.el("recovery-code-error"),
      // Sent as typed. The server ignores case, spaces and dashes.
      read: function (raw) { return raw.trim(); },
      wellFormed: function (value) { return value.length > 0; },
      sendAs: "recoveryCode",
      other: "code",
      switchLabel: "data-to-code"
    }
  };
  var factor = FACTORS.code;

  var inflight = false;
  // True after a submission got no clear answer and the link could not be checked either.
  var unsettled = false;
  var retryIdleLabel = (retryBtn && retryBtn.textContent) || "Try again";

  function retryCopy() {
    return (alertEl && alertEl.getAttribute("data-retry")) || "";
  }

  function isRetryVisible() {
    return retryBtn && !retryBtn.hidden;
  }

  function isRetryBusy() {
    return retryBtn && retryBtn.getAttribute("aria-disabled") === "true";
  }

  function setRetryBusy(busy) {
    if (!retryBtn) return;
    if (busy) {
      retryBtn.setAttribute("aria-disabled", "true");
      retryBtn.textContent = RETRY_CHECKING;
    } else {
      retryBtn.removeAttribute("aria-disabled");
      retryBtn.textContent = retryIdleLabel;
    }
  }

  function hideRetry() {
    if (!retryBtn) return;
    retryBtn.hidden = true;
    retryBtn.removeAttribute("aria-describedby");
    setRetryBusy(false);
  }

  function isRedirectResponse(r) {
    return !!(r && (r.redirected || r.type === "opaqueredirect"));
  }

  function startTimeout() {
    var controller = new AbortController();
    var timedOut = false;
    var timer = setTimeout(function () {
      timedOut = true;
      controller.abort();
    }, TIMEOUT_MS);
    return {
      signal: controller.signal,
      didTimeout: function () { return timedOut; },
      done: function () { clearTimeout(timer); }
    };
  }

  /**
   * One JSON POST to this site, with the page's anti-forgery token. Resolves
   * with the response and its body text; rejects on a network failure, or
   * when 15 seconds pass before the whole reply has arrived. The link check
   * never follows a redirect (redirect: "manual"); the submission does, so
   * the page can tell where the server sent it. The request mode is left at
   * its default: see admin-request.js.
   */
  function post(path, body, followRedirect) {
    var timeout = startTimeout();
    var init = {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": csrf },
      body: JSON.stringify(body),
      credentials: "same-origin",
      signal: timeout.signal
    };
    if (!followRedirect) init.redirect = "manual";
    return fetch(path, init).then(function (r) {
      return r.text().then(function (text) {
        if (timeout.didTimeout()) throw new Error("timed out");
        return { r: r, text: text };
      });
    }).then(function (reply) {
      timeout.done();
      return reply;
    }, function (err) {
      timeout.done();
      throw err;
    });
  }

  /** Asks whether the link is usable: "form", "expired", or "retry" when that cannot be told. Never rejects. */
  function checkLink() {
    return post(CHECK, { token: token }, false).then(function (reply) {
      return dwClassifyCheckResponse(reply.r.status, isRedirectResponse(reply.r), reply.text);
    }, function () {
      return "retry";
    });
  }

  /* ---- Before the form: checking the link ---- */

  function showRetry(focusMessage) {
    setRetryBusy(false);
    // v1.14 L245 / r4 Rule B: when state 4 renders, empty the state 1
    // role=status region. Hidden alone is not enough. Do not add a live region.
    if (statusEl) {
      statusEl.textContent = "";
      statusEl.hidden = true;
    }
    if (form) form.hidden = true;
    if (alertEl) {
      alertEl.removeAttribute("role");
      alertEl.removeAttribute("aria-live");
      alertEl.textContent = retryCopy();
      alertEl.hidden = false;
    }
    if (retryBtn) {
      retryBtn.hidden = false;
      if (alertEl && alertEl.id) retryBtn.setAttribute("aria-describedby", alertEl.id);
    }
    if (main) main.removeAttribute("aria-busy");
    // State 4 first failure (v1.14 L242): focus Try again.
    // Rule C repeat failure (v1.14 L248 / UXR-A26 r4): focus the message.
    // 312ca380 Rule A item 6 is superseded.
    if (focusMessage && alertEl) alertEl.focus();
    else if (retryBtn) retryBtn.focus();
  }

  function showForm() {
    setRetryBusy(false);
    if (statusEl) statusEl.hidden = true;
    if (alertEl) alertEl.hidden = true;
    hideRetry();
    if (form) form.hidden = false;
    if (main) main.removeAttribute("aria-busy");
    // Rule A: the cursor goes to the first field, New password.
    if (newPassword) newPassword.focus();
  }

  function check() {
    if (inflight) return;
    inflight = true;
    var retrying = isRetryVisible();
    if (retrying) {
      if (statusEl) statusEl.hidden = true;
      if (form) form.hidden = true;
      if (alertEl) alertEl.hidden = false;
      retryBtn.hidden = false;
      setRetryBusy(true);
      retryBtn.focus();
    } else {
      if (alertEl) alertEl.hidden = true;
      hideRetry();
      if (form) form.hidden = true;
      if (statusEl) statusEl.hidden = false;
      if (main) main.setAttribute("aria-busy", "true");
    }

    function endCheckFailure() {
      // Spec v1.14 L248 Rule C / UXR-A26 r4: a retry from state 4 that fails
      // or times out again restores "Try again", drops aria-disabled, keeps
      // the same non-live copy, and moves focus to the message.
      showRetry(retrying);
    }

    checkLink().then(function (outcome) {
      inflight = false;
      if (outcome === "form") showForm();
      else if (outcome === "expired") location.replace(EXPIRED);
      else endCheckFailure();
    });
  }

  if (retryBtn) {
    retryBtn.addEventListener("click", function (e) {
      if (isRetryBusy()) {
        e.preventDefault();
        return;
      }
      check();
    });
  }

  /* ---- The form ---- */

  function setBusy(on) {
    inflight = on;
    if (on) form.setAttribute("aria-busy", "true");
    else form.removeAttribute("aria-busy");
    if (submitBtn) submitBtn.disabled = on;
  }

  /** The form's alert announces itself (role="alert") and takes the focus. */
  function showFormAlert(message) {
    AdminDom.setText(formAlert, message);
    AdminDom.show(formAlert, true);
    if (formAlert) formAlert.focus();
  }

  function hideFormAlert() {
    AdminDom.setText(formAlert, "");
    AdminDom.show(formAlert, false);
  }

  function formRetryCopy() {
    return form.getAttribute("data-retry") || "";
  }

  function text(value) {
    return typeof value === "string" ? value : "";
  }

  function parse(raw) {
    try {
      var body = JSON.parse(raw);
      return body && typeof body === "object" && !Array.isArray(body) ? body : null;
    } catch (e) {
      return null;
    }
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

  function checkFactor() {
    var message = factor.wellFormed(factor.read(factor.field.value))
      ? ""
      : factor.field.getAttribute("data-required") || "";
    AdminDom.setInvalid(factor.field, factor.error, message);
    return message;
  }

  /** The switch: the other second-factor field takes this one's place, and the cursor. */
  function useFactor(next) {
    var previous = factor;
    factor = next;
    previous.field.value = "";
    AdminDom.setInvalid(previous.field, previous.error, "");
    AdminDom.show(previous.block, false);
    AdminDom.show(factor.block, true);
    AdminDom.setText(factorSwitch, factorSwitch.getAttribute(factor.switchLabel) || "");
    factor.field.focus();
  }

  /**
   * No clear answer came back, so the password may or may not have been
   * changed. If the link is no longer usable the reset may have gone
   * through: go to sign-in, which will tell. Otherwise nothing changed, or
   * that cannot be told either: say so, and keep the form as it is.
   */
  function afterUnclearReply() {
    checkLink().then(function (outcome) {
      if (outcome === "expired") {
        location.replace(SIGN_IN_PAGE);
        return;
      }
      // Even a link still usable now may be spent a moment later by this very request, if the
      // server commits after the check. So from here on, a "link no longer works" leads to
      // sign-in, which tells. Only a 400 or 401, read after the link, settles it again.
      unsettled = true;
      setBusy(false);
      showFormAlert(formRetryCopy());
    });
  }

  function submitConfirm() {
    var body = { token: token, newPassword: newPassword.value };
    body[factor.sendAs] = factor.read(factor.field.value);
    setBusy(true);
    post(CONFIRM, body, true).then(function (reply) {
      var r = reply.r;
      var data = parse(reply.text) || {};
      if (r.redirected) {
        var expired = expiredTarget(r.url);
        // After an earlier reply that never came, the link may be spent by that very change:
        // sign-in tells, where "this link no longer works" would not.
        if (expired) location.replace(unsettled ? SIGN_IN_PAGE : expired);
        else afterUnclearReply();
        return;
      }
      if (r.status === 200) {
        var message = text(data.message);
        if (message && message === form.getAttribute("data-changed")) location.replace(SIGN_IN);
        else afterUnclearReply();
        return;
      }
      if (r.status < 400 || r.status >= 500) {
        afterUnclearReply();
        return;
      }
      // 4xx: the server refused, so nothing was changed.
      setBusy(false);
      var error = text(data.error);
      if (r.status === 400 && error) {
        // A password rule: the server's named reason, under the field. Both fields keep what was typed.
        // The server reads the link before the rules, so the link is still usable.
        unsettled = false;
        AdminDom.setInvalid(newPassword, newPasswordError, error);
        newPassword.focus();
      } else if (r.status === 401 && error) {
        // The code was refused: the server's sentence. The code is spent or wrong, so it goes.
        unsettled = false;
        factor.field.value = "";
        showFormAlert(error);
      } else if (r.status === 429 && text(data.message)) {
        showFormAlert(text(data.message));
      } else {
        showFormAlert(formRetryCopy());
      }
    }, afterUnclearReply);
  }

  if (form && newPassword && confirmPassword && formAlert && factorSwitch
      && FACTORS.code.field && FACTORS.recovery.field) {
    form.addEventListener("submit", function (e) {
      e.preventDefault();
      if (inflight) return;
      hideFormAlert();
      var lengthError = checkLength();
      var mismatch = checkMatch();
      var factorError = checkFactor();
      if (lengthError) newPassword.focus();
      else if (mismatch) confirmPassword.focus();
      else if (factorError) factor.field.focus();
      else submitConfirm();
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
    [FACTORS.code, FACTORS.recovery].forEach(function (one) {
      one.field.addEventListener("input", function () {
        if (one.field.hasAttribute("aria-invalid")) AdminDom.setInvalid(one.field, one.error, "");
      });
    });
    factorSwitch.addEventListener("click", function () {
      useFactor(FACTORS[factor.other]);
    });

    AdminPassword.bindToggle("new-password", "new-password-toggle");
    AdminPassword.bindToggle("confirm-password", "confirm-password-toggle");

    // A page the browser keeps for its Back button must not keep what was typed with it.
    window.addEventListener("pagehide", function () {
      newPassword.value = "";
      confirmPassword.value = "";
      FACTORS.code.field.value = "";
      FACTORS.recovery.field.value = "";
    });
  }

  if (!token) {
    location.replace(EXPIRED);
    return;
  }

  check();
})();
