/**
 * Sign-in, step 1 (S-A1, /admin/sign-in): email, password and the security check.
 *
 * One request: POST /admin/api/auth/sign-in with {email, password, turnstileToken}.
 *
 *   200 {"next": "totp"}     -> the code page
 *   200 {"next": "enroll"}   -> authenticator set-up
 *   401 {error}, 429 {error} -> the alert, in the server's words. The password
 *                               is cleared and the email stays.
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
 */
(function () {
  "use strict";

  var API = "/admin/api/auth/sign-in";
  var CODE_PAGE = "/admin/sign-in/code";
  var ENROLL_PAGE = "/admin/setup/authenticator";
  var EMAIL_SHAPE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  var panel = AdminDom.el("sign-in-panel");
  var form = AdminDom.el("sign-in-form");
  var alertEl = AdminDom.el("form-alert");
  var email = AdminDom.el("email");
  var emailError = AdminDom.el("email-error");
  var password = AdminDom.el("password");
  var passwordError = AdminDom.el("password-error");
  var submit = AdminDom.el("sign-in-submit");
  if (!panel || !form || !alertEl || !email || !password || !submit) {
    return;
  }

  var busy = false;

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
    showAlert(form.getAttribute("data-retry") || "");
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

  AdminPassword.bindToggle("password", "password-toggle");

  // Show the form first: the security check sizes its widget to the visible column.
  AdminDom.show(panel, true);
  AdminTurnstile.mount({ onChange: syncSubmit });
  syncSubmit();
  if (email.value) password.focus();
  else email.focus();
})();
