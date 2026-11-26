/**
 * The signed-in console shell (AdminSignedInPage): what every signed-in
 * page does in the browser. Served only with a valid session.
 *
 * Sign out. The nav's "Sign out" is a POST form to /admin/sign-out. This
 * script sends it as one JSON POST carrying the page's anti-forgery token
 * (admin-request.js), because the server reads the token from the
 * X-CSRF-TOKEN header only.
 *
 *   200 {"signedOut": true} -> the sign-in page, with the fixed note that
 *                              the owner signed out (admin-arrival.js reads
 *                              it), in place of this page.
 *   anything else           -> no clear answer: no reply, a time-out, a
 *                              refusal. The page asks once whether this
 *                              browser still has a session
 *                              (GET /admin/api/me). The session gate's own
 *                              401 -> the sign-in page with the "session
 *                              ended" note. Otherwise the retry sentence in
 *                              the page's live region, and the button works
 *                              again.
 *
 * A GET never signs anyone out, and nothing else here sends a request.
 *
 * Back and Forward. A page the browser brings back whole from its
 * back/forward cache is asked for anew, so the server decides what is
 * shown: the same page with a session, the sign-in page without one.
 *
 * Skip link. "Skip to main content" leads to #main; the focus goes there
 * too, in every engine.
 *
 * Nothing is stored, and text goes into the page through textContent only.
 */
(function () {
  "use strict";

  var SIGN_OUT = "/admin/sign-out";
  var SIGNED_OUT = "/admin/sign-in#signed-out";
  var SESSION_ENDED = "/admin/sign-in#ended";
  var ME = "/admin/api/me";
  var GATE_DENIED = "Unauthorized";

  window.addEventListener("pageshow", function (event) {
    if (event.persisted) location.reload();
  });

  var skip = document.querySelector("a.skip-link");
  var main = document.getElementById("main");
  if (skip && main) {
    skip.addEventListener("click", function () {
      main.focus();
    });
  }

  var form = document.getElementById("sign-out-form");
  var button = document.getElementById("sign-out");
  var live = document.getElementById("console-status");
  if (!form || !button) {
    return;
  }

  var busy = false;

  function say(message) {
    if (live) live.textContent = message;
  }

  function setBusy(on) {
    busy = on;
    button.disabled = on;
    if (on) form.setAttribute("aria-busy", "true");
    else form.removeAttribute("aria-busy");
  }

  /** Still signed in, or it cannot be told: say so, and the button works again. */
  function sayRetry() {
    setBusy(false);
    say(form.getAttribute("data-retry") || "");
    button.focus();
  }

  /** No clear answer to the sign-out. Ask whether this browser still has a session. */
  function afterUnclearReply() {
    AdminRequest.get(ME).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 401 && body.error === GATE_DENIED) location.replace(SESSION_ENDED);
      else sayRetry();
    }, sayRetry);
  }

  form.addEventListener("submit", function (event) {
    event.preventDefault();
    if (busy) return;
    // The request helper did not arrive: nothing can be sent.
    if (typeof AdminRequest === "undefined") {
      say(form.getAttribute("data-retry") || "");
      return;
    }
    setBusy(true);
    say("");
    AdminRequest.post(SIGN_OUT, {}).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 200 && body.signedOut === true) location.replace(SIGNED_OUT);
      else afterUnclearReply();
    }, afterUnclearReply);
  });
})();
