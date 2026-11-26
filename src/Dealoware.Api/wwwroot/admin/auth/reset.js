/**
 * Ask for a reset link (S-A8, /admin/reset): the email and the security check.
 *
 * One request: POST /admin/api/auth/reset with {email, turnstileToken}.
 *
 *   200 {"message": the server's neutral sentence} -> /admin/reset/sent
 *   401 {error}   -> the alert, in the server's words: the security check
 *                    was not accepted. The email stays.
 *   429 {message} -> the alert, in the server's words. The email stays.
 *   403           -> the server did not accept this page's anti-forgery
 *                    pair, usually because another tab was given a newer
 *                    one. The page loads itself anew, once, and then says
 *                    the request did not go through.
 *   anything else -> the retry sentence, and nothing typed is lost: no
 *                    reply, a time-out, a server error, or a 200 that does
 *                    not carry that sentence.
 *
 * The server gives the same 200 whether or not the address is the owner's,
 * and this page does the same thing with it: it cannot tell the two apart
 * and shows nothing that could. It moves on only for that exact reply, never
 * for a bare 200. After every reply the security check runs again, because
 * its token is spent once the server has seen it.
 *
 * The browser checks one thing before it sends: the field holds something
 * that looks like an email address.
 */
(function () {
  "use strict";

  var API = "/admin/api/auth/reset";
  var SENT_PAGE = "/admin/reset/sent";
  var EMAIL_SHAPE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  var panel = document.getElementById("reset-panel");
  var form = document.getElementById("reset-request-form");
  var alertEl = document.getElementById("form-alert");
  if (!panel || !form || !alertEl) {
    return;
  }

  // A script this page needs did not arrive. Nothing could be sent, so the
  // page says so and shows no form.
  if (typeof AdminArrival === "undefined" || typeof AdminDom === "undefined"
      || typeof AdminTurnstile === "undefined" || typeof AdminRequest === "undefined") {
    form.hidden = true;
    alertEl.textContent = form.getAttribute("data-retry") || "";
    alertEl.hidden = false;
    panel.hidden = false;
    return;
  }

  var email = AdminDom.el("email");
  var emailError = AdminDom.el("email-error");
  var submit = AdminDom.el("reset-submit");
  if (!email || !submit) {
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

  /** The form is usable again, with the alert saying why. The security-check token is spent, so ask for a new one. */
  function stay(message) {
    showAlert(message);
    setBusy(false);
    AdminTurnstile.retry();
  }

  /** No clear answer came back. Say so plainly and keep what was typed. */
  function unclear() {
    stay(retrySentence());
  }

  function send(turnstileToken) {
    setBusy(true);
    AdminRequest.post(API, {
      email: email.value.trim(),
      turnstileToken: turnstileToken
    }).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 403 && mayReload) {
        AdminArrival.reload();
        return;
      }
      if (reply.status !== 403) mayReload = true;
      var message = AdminRequest.text(body.message);
      if (reply.status === 200) {
        if (message && message === form.getAttribute("data-accepted")) location.replace(SENT_PAGE);
        else unclear();
        return;
      }
      var error = AdminRequest.text(body.error);
      if (reply.status === 401 && error) stay(error);
      else if (reply.status === 429 && message) stay(message);
      else unclear();
    }, unclear);
  }

  form.addEventListener("submit", function (event) {
    event.preventDefault();
    if (busy) return;
    hideAlert();
    if (checkEmail()) {
      email.focus();
      return;
    }
    var turnstileToken = AdminTurnstile.token();
    if (AdminTurnstile.state() !== "ready" || !turnstileToken) return;
    send(turnstileToken);
  });

  email.addEventListener("input", function () {
    if (email.hasAttribute("aria-invalid")) AdminDom.setInvalid(email, emailError, "");
  });

  // Show the form first: the security check sizes its widget to the visible column.
  AdminDom.show(panel, true);
  AdminTurnstile.mount({ onChange: syncSubmit });
  syncSubmit();

  if (AdminArrival.flag() === "fresh") showAlert(retrySentence());
  else email.focus();
})();
