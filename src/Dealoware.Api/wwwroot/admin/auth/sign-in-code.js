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
 *                                           with at most a plain query,
 *                                           otherwise to /admin/.
 *   401 with the session gate's own word -> there is no pending sign-in any
 *                                           more (it ran out, or was used):
 *                                           back to step 1, which says so in
 *                                           the server's sign-in sentence.
 *   401 {error}, 429 {error}             -> the alert, in the server's words.
 *                                           The field is cleared and keeps
 *                                           the cursor.
 *   403                                  -> the server did not accept this
 *                                           page's anti-forgery pair, usually
 *                                           because another tab was given a
 *                                           newer one. The page loads itself
 *                                           anew, once, and then says the
 *                                           request did not go through.
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
 * "Back to sign in" gives the pending sign-in up before it leaves: it asks
 * the server to expire the cookie (POST /admin/api/auth/sign-out), then goes
 * to step 1 in place of this page, so the Back button does not lead here
 * again. With JavaScript off it is an ordinary link.
 *
 * Both pages are served only while a pending sign-in exists. Without one the
 * session gate sends the browser to step 1, so this script never runs.
 */
(function () {
  "use strict";

  var SIGN_IN = "/admin/sign-in";
  // Step 1 with the note that it has to be done again (see admin-arrival.js).
  var SIGN_IN_AGAIN = "/admin/sign-in#again";
  var SIGN_OUT = "/admin/api/auth/sign-out";
  var HOME = "/admin/";
  var ME = "/admin/api/me";
  var SIGNED_IN_AS = "CoreOwner";
  var MAX_PATH_LENGTH = 2048;

  // After a verified code the server names where to go. It has already held
  // that path against its own list (AdminReturnPath). The browser still
  // follows only this shape: /admin/ and then segments of letters, digits and
  // hyphens. That leaves no room for a scheme, another host ("//" or "\"), a
  // dot segment, an encoded character, a fragment or a control character.
  // A list page may come back with its query, which the server has checked
  // key by key and written out anew: names of letters only, values of
  // unreserved characters and percent escapes. Everything else goes to /admin/.
  var PLAIN_ADMIN_PATH = /^\/admin\/(?:[A-Za-z0-9-]+(?:\/[A-Za-z0-9-]+)*)?$/;
  var PLAIN_QUERY = /^\?[A-Za-z]+=[A-Za-z0-9._~%-]+(?:&[A-Za-z]+=[A-Za-z0-9._~%-]+)*$/;

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

  var panel = document.getElementById("second-step-panel");
  var form = document.getElementById("second-step-form");
  var alertEl = document.getElementById("form-alert");
  if (!panel || !form || !alertEl) {
    return;
  }

  // A script this page needs did not arrive. Nothing could be sent, so the
  // page says so and shows no form.
  if (typeof AdminArrival === "undefined" || typeof AdminDom === "undefined" || typeof AdminRequest === "undefined") {
    form.hidden = true;
    alertEl.textContent = form.getAttribute("data-retry") || "";
    alertEl.hidden = false;
    panel.hidden = false;
    return;
  }

  var step = form.getAttribute("data-step") === "recovery" ? STEPS.recovery : STEPS.code;
  var field = AdminDom.el(step.field);
  var fieldError = AdminDom.el(step.field + "-error");
  var submit = AdminDom.el("second-step-submit");
  var back = AdminDom.el("back-link");
  if (!field || !submit) {
    return;
  }

  var busy = false;
  // A page that has just reloaded itself over a 403 does not do it again
  // until the server has accepted a request from it.
  var mayReload = AdminArrival.flag() !== "fresh";

  function nextPath(value) {
    if (typeof value !== "string" || value.length > MAX_PATH_LENGTH) return HOME;
    var cut = value.indexOf("?");
    var path = cut < 0 ? value : value.slice(0, cut);
    var query = cut < 0 ? "" : value.slice(cut);
    if (!PLAIN_ADMIN_PATH.test(path) || (query && !PLAIN_QUERY.test(query))) return HOME;
    try {
      var target = new URL(value, location.origin);
      if (target.origin !== location.origin || target.pathname !== path || target.search !== query || target.hash) {
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

  function retrySentence() {
    return form.getAttribute("data-retry") || "";
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
    showAlert(retrySentence());
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
      if (reply.status === 403 && mayReload) {
        AdminArrival.reload();
        return;
      }
      if (reply.status !== 403) mayReload = true;
      if (reply.status === 200) {
        if (body.verified === true) location.replace(nextPath(body.next));
        else afterUnclearReply();
        return;
      }
      var error = AdminRequest.text(body.error);
      if (reply.status === 401 && error && error === form.getAttribute("data-no-sign-in")) {
        location.replace(SIGN_IN_AGAIN);
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

  // "Back to sign in": have the server expire the pending sign-in's cookie,
  // then put step 1 in this page's place. Whatever the answer, or with none,
  // the browser still goes. A click that opens a new tab is left alone.
  var leaving = false;
  if (back) {
    back.addEventListener("click", function (event) {
      if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
      event.preventDefault();
      if (leaving) return;
      leaving = true;
      setBusy(true);
      var leave = function () { location.replace(SIGN_IN); };
      AdminRequest.post(SIGN_OUT, {}).then(leave, leave);
    });
  }

  // A page the browser keeps for its Back button must not keep the code with it.
  window.addEventListener("pagehide", function () {
    field.value = "";
  });

  // And when the Back or Forward button brings this page up again:
  // a page the browser kept whole is asked for anew, because the server
  // serves it only while the pending sign-in lasts; and on a page loaded
  // anew, what the browser put back into the field (Safari does) is removed.
  window.addEventListener("pageshow", function (event) {
    if (event.persisted) {
      location.reload();
      return;
    }
    var visits = typeof performance.getEntriesByType === "function" ? performance.getEntriesByType("navigation") : [];
    if (visits.length > 0 && visits[0].type === "back_forward") field.value = "";
  });

  AdminDom.show(panel, true);
  if (AdminArrival.flag() === "fresh") showAlert(retrySentence());
  else field.focus();
})();
