/**
 * Sign-in, step 2: the authenticator code (S-A2, /admin/sign-in/code) and a
 * recovery code (S-A3, /admin/sign-in/recovery). The form's data-step says
 * which of the two this page is.
 *
 * One request:
 *   S-A2  POST /admin/api/auth/sign-in/code      with {code}
 *   S-A3  POST /admin/api/auth/sign-in/recovery  with {recoveryCode}
 *
 *   200 {"verified": true, "next": path} -> signed in: go to that path if it
 *                                           is a plain path under /admin/,
 *                                           otherwise to /admin/.
 *   401 with the session gate's own word -> there is no pending sign-in any
 *                                           more (it ran out, or was used):
 *                                           back to step 1.
 *   401 {error}, 429 {error}             -> the alert, in the server's words.
 *                                           The field is cleared and keeps
 *                                           the cursor.
 *   any other refusal (4xx)              -> nothing was accepted: the retry
 *                                           sentence, and what was typed stays.
 *   anything else                        -> no clear answer: no reply, a
 *                                           time-out, a server error, or a
 *                                           200 that does not say verified.
 *                                           The code may or may not have been
 *                                           accepted, so the page asks once
 *                                           whether this browser is signed in
 *                                           now (GET /admin/api/me). Signed
 *                                           in -> /admin/. Otherwise the retry
 *                                           sentence, and what was typed stays.
 *
 * The page never submits by itself, however complete the code looks.
 *
 * Both pages are served only while a pending sign-in exists. Without one the
 * session gate answers the page request itself, so this script never runs.
 */
(function () {
  "use strict";

  var SIGN_IN = "/admin/sign-in";
  var HOME = "/admin/";
  var ME = "/admin/api/me";
  var SIGNED_IN_AS = "CoreOwner";
  var MAX_PATH_LENGTH = 2048;

  // After a verified code the server names where to go. It has already held
  // that path against its own list (AdminReturnPath). The browser still
  // follows only this shape: /admin/ and then segments of letters, digits and
  // hyphens. That leaves no room for a scheme, another host ("//" or "\"), a
  // dot segment, an encoded character, a query, a fragment or a control
  // character. Everything else goes to /admin/.
  var PLAIN_ADMIN_PATH = /^\/admin\/(?:[A-Za-z0-9-]+(?:\/[A-Za-z0-9-]+)*)?$/;

  var STEPS = {
    code: {
      api: "/admin/api/auth/sign-in/code",
      field: "code",
      // Spaces are dropped ("123 456" is how many apps show it); then six digits.
      read: function (raw) { return raw.replace(/\s+/g, ""); },
      wellFormed: function (value) { return /^[0-9]{6}$/.test(value); },
      body: function (value) { return { code: value }; }
    },
    recovery: {
      api: "/admin/api/auth/sign-in/recovery",
      field: "recovery-code",
      // Sent as typed. The server ignores case, spaces and dashes.
      read: function (raw) { return raw.trim(); },
      wellFormed: function (value) { return value.length > 0; },
      body: function (value) { return { recoveryCode: value }; }
    }
  };

  var panel = AdminDom.el("second-step-panel");
  var form = AdminDom.el("second-step-form");
  var alertEl = AdminDom.el("form-alert");
  var submit = AdminDom.el("second-step-submit");
  if (!panel || !form || !alertEl || !submit) {
    return;
  }

  var step = form.getAttribute("data-step") === "recovery" ? STEPS.recovery : STEPS.code;
  var field = AdminDom.el(step.field);
  var fieldError = AdminDom.el(step.field + "-error");
  if (!field) {
    return;
  }

  var busy = false;

  function nextPath(value) {
    if (typeof value !== "string" || value.length > MAX_PATH_LENGTH || !PLAIN_ADMIN_PATH.test(value)) {
      return HOME;
    }
    try {
      var target = new URL(value, location.origin);
      if (target.origin !== location.origin || target.pathname !== value || target.search || target.hash) {
        return HOME;
      }
    } catch (e) {
      return HOME;
    }
    return value;
  }

  function setBusy(on) {
    busy = on;
    if (on) form.setAttribute("aria-busy", "true");
    else form.removeAttribute("aria-busy");
    submit.disabled = on;
  }

  /** The alert announces itself (role="alert"); the cursor goes back to the field. */
  function showAlert(message) {
    AdminDom.setText(alertEl, message);
    AdminDom.show(alertEl, true);
    field.focus();
  }

  function hideAlert() {
    AdminDom.setText(alertEl, "");
    AdminDom.show(alertEl, false);
  }

  /** The server refused the code: its words in the alert, the field emptied. */
  function refused(message) {
    setBusy(false);
    field.value = "";
    showAlert(message);
  }

  /** Not signed in, as far as can be told. Say so plainly and keep what was typed. */
  function sayRetry() {
    setBusy(false);
    showAlert(form.getAttribute("data-retry") || "");
  }

  /**
   * No clear answer came back, so the code may or may not have been accepted.
   * Ask the server whether this browser is signed in now. If it is, go on to
   * /admin/ (the path a lost reply would have named is not known). If it is
   * not, or that cannot be told either, the form stays.
   */
  function afterUnclearReply() {
    AdminRequest.get(ME).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 200 && body.principal === SIGNED_IN_AS) location.replace(HOME);
      else sayRetry();
    }, sayRetry);
  }

  function send(value) {
    setBusy(true);
    AdminRequest.post(step.api, step.body(value)).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 200) {
        if (body.verified === true) location.replace(nextPath(body.next));
        else afterUnclearReply();
        return;
      }
      var error = AdminRequest.text(body.error);
      if (reply.status === 401 && error && error === form.getAttribute("data-no-sign-in")) {
        location.replace(SIGN_IN);
        return;
      }
      if ((reply.status === 401 || reply.status === 429) && error) refused(error);
      else if (reply.status >= 400 && reply.status < 500) sayRetry();
      else afterUnclearReply();
    }, afterUnclearReply);
  }

  form.addEventListener("submit", function (event) {
    event.preventDefault();
    if (busy) return;
    hideAlert();
    var value = step.read(field.value);
    if (!step.wellFormed(value)) {
      AdminDom.setInvalid(field, fieldError, field.getAttribute("data-required") || "");
      field.focus();
      return;
    }
    AdminDom.setInvalid(field, fieldError, "");
    send(value);
  });

  field.addEventListener("input", function () {
    if (field.hasAttribute("aria-invalid")) AdminDom.setInvalid(field, fieldError, "");
  });

  AdminDom.show(panel, true);
  field.focus();
})();
