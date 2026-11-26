/**
 * The recovery-code banner (UXR-A40, AdminRecoveryNotice): "Dismiss".
 * Served only with a valid session, and loaded only by a page that shows the
 * banner. The sentence, the count in it and the "Manage recovery codes" link
 * are the server's; this script never changes them.
 *
 * One request: POST /admin/api/settings/security/recovery-notice/dismiss,
 * with the page's anti-forgery token (admin-request.js).
 *
 *   200 {"dismissed": true}   -> the banner leaves the page, and the server
 *                                shows it on no other page of this session.
 *                                The focus goes to main.
 *   401 from the session gate -> the session has ended: the sign-in page with
 *                                the "session ended" note.
 *   anything else             -> the retry sentence in the page's live
 *                                region; the banner and its button stay.
 *
 * Nothing is stored. Without JavaScript the button stays hidden.
 */
(function () {
  "use strict";

  var DISMISS = "/admin/api/settings/security/recovery-notice/dismiss";
  var SESSION_ENDED = "/admin/sign-in#ended";
  var GATE_DENIED = "Unauthorized";

  var banner = document.getElementById("recovery-notice");
  var button = document.getElementById("recovery-notice-dismiss");
  var live = document.getElementById("console-status");
  var main = document.getElementById("main");
  if (!banner || !button || typeof AdminRequest === "undefined") return;

  var busy = false;

  function sayRetry() {
    busy = false;
    button.disabled = false;
    if (live) live.textContent = banner.getAttribute("data-retry") || "";
    button.focus();
  }

  button.addEventListener("click", function () {
    if (busy) return;
    busy = true;
    button.disabled = true;
    if (live) live.textContent = "";
    AdminRequest.post(DISMISS, {}).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 200 && body.dismissed === true) {
        banner.parentNode.removeChild(banner);
        if (main) main.focus();
      } else if (reply.status === 401 && body.error === GATE_DENIED) {
        location.replace(SESSION_ENDED);
      } else {
        sayRetry();
      }
    }, sayRetry);
  });

  button.hidden = false;
})();
