/**
 * Authenticator set-up (S-A6, /admin/setup/authenticator).
 *
 * The server wrote the key and its QR code into the page. This script never
 * asks for them: it only reads the key off the page when "Copy key" is
 * pressed, and hands it to the clipboard.
 *
 * One request: POST /admin/setup/authenticator with {code}.
 *
 *   200 {"enrolled": true}               -> the authenticator is set up: on to
 *                                           the recovery codes, in place of
 *                                           this page.
 *   401 with the session gate's own word -> there is no pending sign-in any
 *                                           more (it ran out): back to step 1,
 *                                           which says so in the server's
 *                                           sign-in sentence.
 *   401 {error}, 429 {error}             -> the alert, in the server's words.
 *                                           The field is cleared and keeps the
 *                                           cursor; the key stays as it is.
 *   403                                  -> the server did not accept this
 *                                           page's anti-forgery pair, usually
 *                                           because another tab was given a
 *                                           newer one. The page loads itself
 *                                           anew, once, and then says the
 *                                           request did not go through.
 *   any other refusal (4xx)              -> nothing was accepted: the retry
 *                                           sentence, and what was typed stays.
 *   anything else                        -> no clear answer: no reply, a
 *                                           time-out, a server error, or a 200
 *                                           that does not say enrolled. The
 *                                           code may or may not have been
 *                                           accepted, and only the server
 *                                           knows. So the page first asks
 *                                           whether the server is answering at
 *                                           all (GET /admin/api/me, which says
 *                                           nothing about set-up). If it is,
 *                                           the page loads itself anew: the
 *                                           server then sends the browser on to
 *                                           the recovery codes when the
 *                                           authenticator is set up, or shows
 *                                           this page again, which says the
 *                                           request did not go through. If it
 *                                           is not, the retry sentence, and
 *                                           what was typed stays. Until the
 *                                           server has been asked, Confirm asks
 *                                           again before it sends a new code.
 *
 * The page never submits by itself, however complete the code looks.
 *
 * It is served only while a pending sign-in exists. Without one the session
 * gate sends the browser to step 1, so this script never runs.
 */
(function () {
  "use strict";

  var API = "/admin/setup/authenticator";
  var RECOVERY_CODES = "/admin/setup/recovery-codes";
  // Step 1 with the note that it has to be done again (see admin-arrival.js).
  var SIGN_IN_AGAIN = "/admin/sign-in#again";
  var ME = "/admin/api/me";

  var panel = document.getElementById("authenticator-panel");
  var keyPart = document.getElementById("authenticator-key");
  var form = document.getElementById("authenticator-form");
  var alertEl = document.getElementById("form-alert");
  if (!panel || !keyPart || !form || !alertEl) {
    return;
  }

  // A script this page needs did not arrive. Nothing could be sent, so the
  // page says so and shows neither the key nor the form.
  if (typeof AdminArrival === "undefined" || typeof AdminDom === "undefined" || typeof AdminRequest === "undefined"
      || typeof AdminClipboard === "undefined") {
    keyPart.hidden = true;
    form.hidden = true;
    alertEl.textContent = form.getAttribute("data-retry") || "";
    alertEl.hidden = false;
    panel.hidden = false;
    return;
  }

  var field = AdminDom.el("code");
  var fieldError = AdminDom.el("code-error");
  var submit = AdminDom.el("authenticator-submit");
  var qr = AdminDom.el("authenticator-qr");
  var key = AdminDom.el("manual-key");
  var copyKey = AdminDom.el("copy-key");
  var copyStatus = AdminDom.el("copy-status");
  if (!field || !submit || !key || !copyKey) {
    return;
  }

  var busy = false;
  // A page that has just reloaded itself over a 403 does not do it again
  // until the server has accepted a request from it.
  var mayReload = AdminArrival.flag() !== "fresh";
  // The last code got no clear answer, and the server has not been asked since.
  var unsettled = false;

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

  /** Nothing was set up, as far as can be told. Say so plainly and keep what was typed. */
  function sayRetry() {
    setBusy(false);
    showAlert(retrySentence());
  }

  /**
   * No clear answer came back, so the code may or may not have been accepted.
   * If the server is answering, load this page anew and let the server say
   * where things stand. If it is not, or that cannot be told, the form stays.
   */
  function afterUnclearReply() {
    unsettled = true;
    AdminRequest.get(ME).then(function (reply) {
      if (reply.status === 200 || reply.status === 401) AdminArrival.reload();
      else sayRetry();
    }, sayRetry);
  }

  function send(value) {
    setBusy(true);
    AdminRequest.post(API, { code: value }).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 403 && mayReload) {
        AdminArrival.reload();
        return;
      }
      if (reply.status !== 403) mayReload = true;
      if (reply.status === 200) {
        if (body.enrolled === true) location.replace(RECOVERY_CODES);
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
    // Spaces are dropped ("123 456" is how many apps show it); then six digits.
    var value = field.value.replace(/\s+/g, "");
    if (!/^[0-9]{6}$/.test(value)) {
      AdminDom.setInvalid(field, fieldError, field.getAttribute("data-required") || "");
      field.focus();
      return;
    }
    AdminDom.setInvalid(field, fieldError, "");
    if (unsettled) {
      setBusy(true);
      afterUnclearReply();
      return;
    }
    send(value);
  });

  field.addEventListener("input", function () {
    if (field.hasAttribute("aria-invalid")) AdminDom.setInvalid(field, fieldError, "");
  });

  // "Copy key": the key as the page shows it, without the gaps between its
  // groups. The outcome is said in the status line, and the button keeps the
  // focus (a click does not give it one in every browser).
  copyKey.addEventListener("click", function () {
    AdminDom.setText(copyStatus, "");
    AdminClipboard.copy(key.textContent || "", key).then(function (copied) {
      AdminDom.setText(copyStatus, copyKey.getAttribute(copied ? "data-copied" : "data-copy-failed") || "");
      copyKey.focus();
    });
  });

  // A page the browser keeps for its Back button must not keep the code, the
  // key or its QR code with it.
  window.addEventListener("pagehide", function () {
    field.value = "";
    AdminDom.clear(key);
    AdminDom.clear(qr);
  });

  // And when the Back or Forward button brings this page up again:
  // a page the browser kept whole is asked for anew, because the server
  // serves it only while the pending sign-in lasts and the authenticator is
  // not set up; and on a page loaded anew by Back, Forward or a reload, what
  // the browser put back into the field (Safari and Firefox do) is removed.
  window.addEventListener("pageshow", function (event) {
    if (event.persisted) {
      location.reload();
      return;
    }
    var visits = typeof performance.getEntriesByType === "function" ? performance.getEntriesByType("navigation") : [];
    if (visits.length > 0 && (visits[0].type === "back_forward" || visits[0].type === "reload")) field.value = "";
  });

  // The reader has the QR code to scan first, so the cursor is not put in
  // the field: that would scroll a small screen past the code.
  AdminDom.show(panel, true);
  if (AdminArrival.flag() === "fresh") showAlert(retrySentence());
})();
