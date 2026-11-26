/**
 * Recovery codes (S-A7, /admin/setup/recovery-codes).
 *
 * The server wrote the codes into the page. This script never asks for
 * them: it only reads them off the page when "Copy all" is pressed, and
 * hands them to the clipboard, one code to a line.
 *
 * "Continue" works only once the box "I have saved these recovery codes" is
 * ticked. It then gives the pending sign-in up, so that this browser cannot
 * be shown the codes again: it asks the server to expire the cookie
 * (POST /admin/api/auth/sign-out), and goes to step 1 in place of this page,
 * whatever the answer and with none. Setting the authenticator up signs
 * nobody in; the owner signs in again with a fresh code.
 *
 * The codes matter more than the buttons, so the page shows them even when a
 * helper script did not arrive: without the clipboard helper there is no
 * copy button, and without the request helper "Continue" leaves without
 * asking the server first.
 *
 * The page is served only while a pending sign-in exists. Without one the
 * session gate sends the browser to step 1, so this script never runs.
 */
(function () {
  "use strict";

  var SIGN_IN = "/admin/sign-in";
  var SIGN_OUT = "/admin/api/auth/sign-out";

  var panel = document.getElementById("recovery-codes-panel");
  var list = document.getElementById("recovery-codes-list");
  var saved = document.getElementById("codes-saved");
  var next = document.getElementById("codes-continue");
  if (!panel || !list || !saved || !next) {
    return;
  }

  var copyAll = document.getElementById("copy-codes");
  var copyStatus = document.getElementById("copy-status");
  var leaving = false;

  /** "Continue" works only with the box ticked and no request in flight. */
  function syncContinue() {
    next.disabled = leaving || !saved.checked;
  }

  /** The codes as the page shows them, one to a line. */
  function codesAsText() {
    var lines = [];
    var items = list.getElementsByTagName("li");
    for (var i = 0; i < items.length; i += 1) lines.push(items[i].textContent || "");
    return lines.join("\n");
  }

  function say(message) {
    if (copyStatus) copyStatus.textContent = message;
  }

  saved.addEventListener("change", syncContinue);

  next.addEventListener("click", function () {
    if (leaving || !saved.checked) return;
    leaving = true;
    syncContinue();
    panel.setAttribute("aria-busy", "true");
    var leave = function () { location.replace(SIGN_IN); };
    if (typeof AdminRequest === "undefined") {
      leave();
      return;
    }
    AdminRequest.post(SIGN_OUT, {}).then(leave, leave);
  });

  // "Copy all". The outcome is said in the status line, and the button keeps
  // the focus (a click does not give it one in every browser).
  if (copyAll) {
    if (typeof AdminClipboard === "undefined") {
      copyAll.hidden = true;
    } else {
      copyAll.addEventListener("click", function () {
        say("");
        AdminClipboard.copy(codesAsText(), list).then(function (copied) {
          say(copyAll.getAttribute(copied ? "data-copied" : "data-copy-failed") || "");
          copyAll.focus();
        });
      });
    }
  }

  // A page the browser keeps for its Back button must not keep the codes
  // with it, nor the tick.
  window.addEventListener("pagehide", function () {
    while (list.firstChild) list.removeChild(list.firstChild);
    saved.checked = false;
  });

  // A page the browser kept whole and brings up again is asked for anew: the
  // server decides whether the codes can still be shown.
  window.addEventListener("pageshow", function (event) {
    if (event.persisted) location.reload();
  });

  // A reload must not carry the tick over (some browsers put it back).
  saved.checked = false;
  syncContinue();
  panel.hidden = false;
})();
