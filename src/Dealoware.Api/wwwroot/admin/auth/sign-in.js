/**
 * Sign-in, step 1 (S-A1, /admin/sign-in): email, password and the security check.
 *
 * One request: POST /admin/api/auth/sign-in with {email, password, turnstileToken}.
 *
 *   200 {"next": "totp"}     -> the code page
 *   200 {"next": "enroll"}   -> authenticator set-up
 *   401 {error}, 429 {error} -> the alert, in the server's words. The password
 *                               is cleared and the email stays.
 *   403                      -> the server did not accept this page's
 *                               anti-forgery pair, usually because another tab
 *                               was given a newer one. The page loads itself
 *                               anew, once, and then says the request did not
 *                               go through.
 *   anything else            -> the retry sentence, and nothing typed is lost:
 *                               no reply, a time-out, a server error, or a 200
 *                               that does not name one of the two next steps.
 *
 * The page moves on only for those two exact replies, never for a bare 200.
 * After every reply the security check runs again, because its token is spent
 * once the server has seen it.
 *
 * No return path is sent. Nothing hands this page one: the server keeps the
 * return path with the pending sign-in, never in a sign-in address, and
 * without one it lands on /admin/ after step 2.
 *
 * The browser checks two things before it sends: both fields are filled, and
 * the email looks like one. Whether the details are right is the server's
 * answer alone.
 *
 * When the browser was sent back here because no pending sign-in was left
 * (see admin-arrival.js), the page opens with the server's sign-in sentence.
 * When it was sent here because a session ended, or because a reset has just
 * changed the password, the page opens with that one sentence in the status
 * line above the form. The sentences are in the page; the address carries
 * only which one.
 */
(function () {
  "use strict";

  var API = "/admin/api/auth/sign-in";
  var CODE_PAGE = "/admin/sign-in/code";
  var ENROLL_PAGE = "/admin/setup/authenticator";
  var EMAIL_SHAPE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  var panel = document.getElementById("sign-in-panel");
  var form = document.getElementById("sign-in-form");
  var alertEl = document.getElementById("form-alert");
  var statusEl = document.getElementById("form-status");
  if (!panel || !form || !alertEl) {
    return;
  }

  // A script this page needs did not arrive. Nothing could be sent, so the
  // page says so and shows no form.
  if (typeof AdminArrival === "undefined" || typeof AdminDom === "undefined" || typeof AdminPassword === "undefined"
      || typeof AdminTurnstile === "undefined" || typeof AdminRequest === "undefined") {
    form.hidden = true;
    alertEl.textContent = form.getAttribute("data-retry") || "";
    alertEl.hidden = false;
    panel.hidden = false;
    return;
  }

  var email = AdminDom.el("email");
  var emailError = AdminDom.el("email-error");
  var password = AdminDom.el("password");
  var passwordError = AdminDom.el("password-error");
  var submit = AdminDom.el("sign-in-submit");
  if (!email || !password || !submit) {
    return;
  }

  var busy = false;
  // A page that has just reloaded itself over a 403 does not do it again
  // until the server has accepted a request from it.
  var mayReload = AdminArrival.flag() !== "fresh";

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
    alertEl.focus();
  }

  function hideAlert() {
    AdminDom.setText(alertEl, "");
    AdminDom.show(alertEl, false);
  }

  /** The status line: shown empty first, then filled, so that it is announced. */
  function showStatus(message) {
    if (!statusEl || !message) return;
    AdminDom.show(statusEl, true);
    AdminDom.setText(statusEl, message);
  }

  function hideStatus() {
    AdminDom.setText(statusEl, "");
    AdminDom.show(statusEl, false);
  }

  function retrySentence() {
    return form.getAttribute("data-retry") || "";
  }

  function checkEmail() {
    var value = email.value.trim();
    var message = "";
    if (!value) message = email.getAttribute("data-required") || "";
    else if (!EMAIL_SHAPE.test(value)) message = email.getAttribute("data-invalid") || "";
    AdminDom.setInvalid(email, emailError, message);
    return message;
  }

  function checkPassword() {
    var message = password.value ? "" : password.getAttribute("data-required") || "";
    AdminDom.setInvalid(password, passwordError, message);
    return message;
  }

  /** The form is usable again. The security-check token is spent, so ask for a new one. */
  function finish() {
    setBusy(false);
    AdminTurnstile.retry();
  }

  /** The server refused: its words in the alert, the password gone, the email kept. */
  function refused(message) {
    password.value = "";
    showAlert(message);
    finish();
  }

  /** No clear answer came back. Say so plainly and keep what was typed. */
  function unclear() {
    showAlert(retrySentence());
    finish();
  }

  function send(turnstileToken) {
    setBusy(true);
    AdminRequest.post(API, {
      email: email.value.trim(),
      password: password.value,
      turnstileToken: turnstileToken
    }).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 403 && mayReload) {
        AdminArrival.reload();
        return;
      }
      if (reply.status !== 403) mayReload = true;
      if (reply.status === 200) {
        if (body.next === "totp") location.replace(CODE_PAGE);
        else if (body.next === "enroll") location.replace(ENROLL_PAGE);
        else unclear();
        return;
      }
      var error = AdminRequest.text(body.error);
      if ((reply.status === 401 || reply.status === 429) && error) refused(error);
      else unclear();
    }, unclear);
  }

  form.addEventListener("submit", function (event) {
    event.preventDefault();
    if (busy) return;
    hideAlert();
    hideStatus();
    var emailMessage = checkEmail();
    var passwordMessage = checkPassword();
    if (emailMessage) {
      email.focus();
      return;
    }
    if (passwordMessage) {
      password.focus();
      return;
    }
    var turnstileToken = AdminTurnstile.token();
    if (AdminTurnstile.state() !== "ready" || !turnstileToken) return;
    send(turnstileToken);
  });

  email.addEventListener("input", function () {
    if (email.hasAttribute("aria-invalid")) AdminDom.setInvalid(email, emailError, "");
  });
  password.addEventListener("input", function () {
    if (password.hasAttribute("aria-invalid")) AdminDom.setInvalid(password, passwordError, "");
  });

  // A page the browser keeps for its Back button must not keep the password with it.
  window.addEventListener("pagehide", function () {
    password.value = "";
  });

  AdminPassword.bindToggle("password", "password-toggle");

  // Show the form first: the security check sizes its widget to the visible column.
  AdminDom.show(panel, true);
  AdminTurnstile.mount({ onChange: syncSubmit });
  syncSubmit();

  var arrival = AdminArrival.flag();
  if (arrival === "again") showAlert(form.getAttribute("data-sent-back") || "");
  else if (arrival === "fresh") showAlert(retrySentence());
  else {
    if (arrival === "ended" || arrival === "changed") showStatus(statusEl && statusEl.getAttribute("data-" + arrival));
    if (email.value) password.focus();
    else email.focus();
  }
})();
