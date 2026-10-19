(function () {
  var CHECK = "/admin/api/auth/reset/confirm/check";
  var CONFIRM = "/admin/api/auth/reset/confirm";
  var EXPIRED = "/admin/link-expired";
  var SIGN_IN = "/admin/sign-in";
  var TIMEOUT_MS = 15000;

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

  function showRetry() {
    if (statusEl) statusEl.hidden = true;
    if (form) form.hidden = true;
    if (alertEl) {
      alertEl.hidden = false;
      alertEl.textContent = retryCopy();
      alertEl.focus();
    }
    if (retryBtn) retryBtn.hidden = false;
    if (main) main.removeAttribute("aria-busy");
  }

  function showForm() {
    if (statusEl) statusEl.hidden = true;
    if (alertEl) alertEl.hidden = true;
    if (retryBtn) retryBtn.hidden = true;
    if (form) form.hidden = false;
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
        inflight = false;
      }).catch(function () {
        inflight = false;
      });
    });
  }

  check();
})();
