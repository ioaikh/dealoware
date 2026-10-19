(function () {
  var CHECK = "/admin/api/auth/reset/confirm/check";
  var CONFIRM = "/admin/api/auth/reset/confirm";
  var EXPIRED = "/admin/link-expired";
  var SIGN_IN = "/admin/sign-in";
  var TIMEOUT_MS = window.__dwResetTimeoutMs || 15000;
  var SIGN_IN_GENERIC =
    "We couldn't sign you in. Check your details and try again later. You can also reset your password.";
  var PASSWORD_ERROR_ID = "newPassword-error";

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

  function retryCopy() {
    return (alertEl && alertEl.getAttribute("data-retry")) || "";
  }

  function newPasswordEl() {
    return document.getElementById("newPassword");
  }

  function clearPasswordError() {
    var field = newPasswordEl();
    if (field) {
      field.removeAttribute("aria-invalid");
      field.removeAttribute("aria-describedby");
    }
    var existing = document.getElementById(PASSWORD_ERROR_ID);
    if (existing && existing.parentNode) existing.parentNode.removeChild(existing);
  }

  function showPasswordError(message) {
    clearPasswordError();
    var field = newPasswordEl();
    if (!field) return;
    var error = document.createElement("p");
    error.id = PASSWORD_ERROR_ID;
    error.textContent = message || "";
    field.setAttribute("aria-invalid", "true");
    field.setAttribute("aria-describedby", PASSWORD_ERROR_ID);
    var label = field.closest("label") || field;
    if (label.parentNode) {
      if (label.nextSibling) label.parentNode.insertBefore(error, label.nextSibling);
      else label.parentNode.appendChild(error);
    }
    field.focus();
  }

  function showRetry(message) {
    if (statusEl) statusEl.hidden = true;
    if (form) form.hidden = true;
    if (alertEl) {
      alertEl.hidden = false;
      alertEl.textContent = message || retryCopy();
    }
    if (retryBtn) {
      retryBtn.hidden = false;
      retryBtn.focus();
    }
    if (main) main.removeAttribute("aria-busy");
  }

  function showForm() {
    clearPasswordError();
    if (statusEl) statusEl.hidden = true;
    if (alertEl) alertEl.hidden = true;
    if (retryBtn) retryBtn.hidden = true;
    if (form) form.hidden = false;
    if (main) main.removeAttribute("aria-busy");
    var field = newPasswordEl();
    if (field) field.focus();
  }

  function showSignInAlert() {
    if (statusEl) statusEl.hidden = true;
    if (form) form.hidden = false;
    if (retryBtn) retryBtn.hidden = true;
    if (alertEl) {
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

  function fetchWithTimeout(url, options) {
    var controller = new AbortController();
    var timer = setTimeout(function () { controller.abort(); }, TIMEOUT_MS);
    options = options || {};
    options.signal = controller.signal;
    options.credentials = "same-origin";
    return fetch(url, options).finally(function () { clearTimeout(timer); });
  }

  function readJson(r) {
    return r.json().catch(function () { return null; });
  }

  function check() {
    if (inflight) return;
    inflight = true;
    if (alertEl) alertEl.hidden = true;
    if (retryBtn) retryBtn.hidden = true;
    if (form) form.hidden = true;
    if (statusEl) statusEl.hidden = false;
    if (main) main.setAttribute("aria-busy", "true");

    fetchWithTimeout(CHECK, {
      method: "POST",
      headers: jsonHeaders(),
      body: JSON.stringify({ token: token })
    }).then(function (r) {
      if (r.redirected) {
        goExpired(r.url);
        return;
      }
      if (r.status === 429 || r.status >= 500) {
        showRetry();
        return;
      }
      if (!r.ok) {
        showRetry();
        return;
      }
      return r.json().then(function (body) {
        if (body && body.valid === true) showForm();
        else goExpired(EXPIRED);
      });
    }).catch(function () {
      showRetry();
    }).finally(function () {
      inflight = false;
    });
  }

  if (retryBtn) {
    retryBtn.addEventListener("click", function () { check(); });
  }

  if (form) {
    form.addEventListener("submit", function (e) {
      e.preventDefault();
      if (inflight) return;
      inflight = true;
      clearPasswordError();
      if (alertEl) alertEl.hidden = true;
      if (retryBtn) retryBtn.hidden = true;

      fetchWithTimeout(CONFIRM, {
        method: "POST",
        headers: jsonHeaders(),
        body: JSON.stringify({
          token: document.getElementById("token").value,
          newPassword: document.getElementById("newPassword").value,
          totp: document.getElementById("totp").value,
          recoveryCode: document.getElementById("recoveryCode").value
        })
      }).then(function (r) {
        if (r.redirected) {
          goExpired(r.url);
          return;
        }
        if (r.ok) {
          location.replace(SIGN_IN);
          return;
        }
        if (r.status === 400) {
          return readJson(r).then(function (body) {
            showPasswordError(body && body.error ? body.error : "");
            inflight = false;
          });
        }
        if (r.status === 401) {
          var totpEl = document.getElementById("totp");
          var recoveryEl = document.getElementById("recoveryCode");
          if (totpEl) totpEl.value = "";
          if (recoveryEl) recoveryEl.value = "";
          showSignInAlert();
          inflight = false;
          return;
        }
        if (r.status === 429) {
          return readJson(r).then(function (body) {
            showRetry(body && body.message ? body.message : retryCopy());
            inflight = false;
          });
        }
        showRetry();
        inflight = false;
      }).catch(function () {
        showRetry();
        inflight = false;
      });
    });
  }

  if (!token) {
    location.replace(EXPIRED);
    return;
  }

  check();
})();
