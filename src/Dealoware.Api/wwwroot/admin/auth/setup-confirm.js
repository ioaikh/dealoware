(function (global) {
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

  global.dwClassifyCheckResponse = dwClassifyCheckResponse;
  if (global.__dwResetConfirmSkipBoot) return;

  var CHECK = "/admin/api/auth/bootstrap/check";
  var CONFIRM = "/admin/api/auth/bootstrap";
  var EXPIRED = "/admin/link-expired?for=setup";
  var SIGN_IN = "/admin/sign-in";
  var TIMEOUT_MS = 15000;
  var SIGN_IN_GENERIC =
    "We couldn't sign you in. Check your details and try again later. You can also reset your password.";
  var FACTOR_ERROR =
    "Enter your authenticator code or a recovery code.";
  var PASSWORD_ERROR_ID = "newPassword-error";
  var TOTP_ERROR_ID = "totp-error";
  var RETRY_CHECKING = "Checking…";

  var hash = location.hash;
  history.replaceState(null, "", location.pathname);
  var match = /[#&]token=([^&]*)/.exec(hash);
  var token = match ? decodeURIComponent(match[1].replace(/\+/g, " ")) : "";
  var tokenEl = document.getElementById("token");
  if (tokenEl) tokenEl.value = token;

  var csrfMeta = document.querySelector('meta[name="dw-csrf"]');
  var csrf = csrfMeta ? csrfMeta.getAttribute("content") || "" : "";

  var statusEl = document.getElementById("reset-status");
  var alertEl = document.getElementById("reset-alert");
  var retryBtn = document.getElementById("reset-retry");
  var form = document.getElementById("reset-form");
  var main = document.getElementById("reset-main");
  var inflight = false;
  var retryAction = check;
  var retryIdleLabel = (retryBtn && retryBtn.textContent) || "Try again";

  function retryCopy() {
    return (alertEl && alertEl.getAttribute("data-retry")) || "";
  }

  function newPasswordEl() {
    return document.getElementById("newPassword");
  }

  function totpFieldEl() {
    return document.getElementById("totp");
  }

  function recoveryFieldEl() {
    return document.getElementById("recoveryCode");
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

  function clearDescribedError(id, field) {
    if (field) {
      field.removeAttribute("aria-invalid");
      field.removeAttribute("aria-describedby");
    }
    var existing = document.getElementById(id);
    if (existing && existing.parentNode) existing.parentNode.removeChild(existing);
  }

  function clearPasswordError() {
    clearDescribedError(PASSWORD_ERROR_ID, newPasswordEl());
  }

  function clearTotpError() {
    clearDescribedError(TOTP_ERROR_ID, totpFieldEl());
  }

  function showFieldError(id, field, message) {
    clearDescribedError(id, field);
    if (!field) return;
    var error = document.createElement("p");
    error.id = id;
    error.textContent = message || "";
    field.setAttribute("aria-invalid", "true");
    field.setAttribute("aria-describedby", id);
    var label = field.closest("label") || field;
    if (label.parentNode) {
      if (label.nextSibling) label.parentNode.insertBefore(error, label.nextSibling);
      else label.parentNode.appendChild(error);
    }
    field.focus();
  }

  function showPasswordError(message) {
    showFieldError(PASSWORD_ERROR_ID, newPasswordEl(), message);
  }

  function showTotpError(message) {
    showFieldError(TOTP_ERROR_ID, totpFieldEl(), message);
  }

  function showRetry(message, action, focusMessage) {
    retryAction = action || check;
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
      alertEl.textContent = message || retryCopy();
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
    clearPasswordError();
    clearTotpError();
    setRetryBusy(false);
    if (statusEl) statusEl.hidden = true;
    if (alertEl) alertEl.hidden = true;
    hideRetry();
    if (form) form.hidden = false;
    if (main) main.removeAttribute("aria-busy");
    var field = newPasswordEl();
    if (field) field.focus();
  }

  function showSignInAlert() {
    if (statusEl) statusEl.hidden = true;
    if (form) form.hidden = false;
    hideRetry();
    if (alertEl) {
      alertEl.setAttribute("role", "alert");
      alertEl.removeAttribute("aria-live");
      alertEl.hidden = false;
      alertEl.textContent = SIGN_IN_GENERIC;
      alertEl.focus();
    }
    if (main) main.removeAttribute("aria-busy");
  }

  function goExpired(url) {
    location.replace(url || EXPIRED);
  }

  function jsonHeaders() {
    var headers = { "Content-Type": "application/json" };
    headers["X-CSRF-TOKEN"] = csrf;
    return headers;
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
      showRetry(undefined, undefined, retrying);
    }

    var timeout = startTimeout();
    fetch(CHECK, {
      method: "POST",
      headers: jsonHeaders(),
      body: JSON.stringify({ token: token }),
      credentials: "same-origin",
      redirect: "manual",
      signal: timeout.signal
    }).then(function (r) {
      if (timeout.didTimeout()) return;
      return r.text().then(function (text) {
        if (timeout.didTimeout()) return;
        var outcome = dwClassifyCheckResponse(r.status, isRedirectResponse(r), text);
        if (outcome === "form") showForm();
        else if (outcome === "expired") goExpired(EXPIRED);
        else endCheckFailure();
      });
    }).catch(function () {
      endCheckFailure();
    }).finally(function () {
      timeout.done();
      inflight = false;
    });
  }

  function submitConfirm() {
    if (inflight) return;
    inflight = true;
    clearPasswordError();
    clearTotpError();
    var retrying = isRetryVisible();
    if (retrying) {
      if (alertEl) alertEl.hidden = false;
      retryBtn.hidden = false;
      setRetryBusy(true);
      retryBtn.focus();
    } else {
      if (alertEl) alertEl.hidden = true;
      hideRetry();
      if (form) form.hidden = false;
    }

    var totpEl = totpFieldEl();
    var recoveryEl = recoveryFieldEl();
    var totpVal = totpEl ? totpEl.value.trim() : "";
    var recVal = recoveryEl ? recoveryEl.value.trim() : "";
    if (!totpVal && !recVal) {
      hideRetry();
      if (form) form.hidden = false;
      showTotpError(FACTOR_ERROR);
      inflight = false;
      return;
    }

    var timeout = startTimeout();
    fetch(CONFIRM, {
      method: "POST",
      headers: jsonHeaders(),
      body: JSON.stringify({
        token: document.getElementById("token").value,
        newPassword: document.getElementById("newPassword").value,
        totp: totpVal,
        recoveryCode: totpVal ? "" : recVal
      }),
      credentials: "same-origin",
      signal: timeout.signal
    }).then(function (r) {
      if (timeout.didTimeout()) {
        showRetry(undefined, submitConfirm, retrying);
        inflight = false;
        return;
      }
      if (r.redirected) {
        goExpired(r.url);
        return;
      }
      if (r.ok) {
        location.replace(SIGN_IN);
        return;
      }
      if (r.status === 400) {
        hideRetry();
        if (form) form.hidden = false;
        return r.text().then(function (text) {
          if (timeout.didTimeout()) {
            showRetry(undefined, submitConfirm, retrying);
            inflight = false;
            return;
          }
          var body = null;
          try { body = JSON.parse(text); } catch (e) { body = null; }
          showPasswordError(body && body.error ? body.error : "");
          inflight = false;
        });
      }
      if (r.status === 401) {
        if (totpEl) totpEl.value = "";
        if (recoveryEl) recoveryEl.value = "";
        showSignInAlert();
        inflight = false;
        return;
      }
      if (r.status === 429) {
        return r.text().then(function (text) {
          if (timeout.didTimeout()) {
            showRetry(undefined, submitConfirm, retrying);
            inflight = false;
            return;
          }
          var body = null;
          try { body = JSON.parse(text); } catch (e) { body = null; }
          showRetry(body && body.message ? body.message : retryCopy(), submitConfirm, retrying);
          inflight = false;
        });
      }
      showRetry(undefined, submitConfirm, retrying);
      inflight = false;
    }).catch(function () {
      showRetry(undefined, submitConfirm, retrying);
      inflight = false;
    }).finally(function () {
      timeout.done();
    });
  }

  if (retryBtn) {
    retryBtn.addEventListener("click", function (e) {
      if (isRetryBusy()) {
        e.preventDefault();
        return;
      }
      retryAction();
    });
  }

  if (form) {
    form.addEventListener("submit", function (e) {
      e.preventDefault();
      submitConfirm();
    });
  }

  if (!token) {
    location.replace(EXPIRED);
    return;
  }

  check();
})(typeof globalThis !== "undefined" ? globalThis : this);
