/**
 * Session warnings on every signed-in page (Spec §8.7: 30 minutes idle,
 * 8 hours in all; TD-ADM-UI-auth-14, TD-ADM-UI-auth-24, TD-ADM-176).
 * Served only with a valid session, after admin-request.js and console.js.
 *
 * The page asks the server how long the session has left:
 *
 *   GET /admin/api/session  -> 200 { idleSecondsLeft, absoluteSecondsLeft, ... }
 *
 * That read is the one request the session gate does not count as activity,
 * so asking never keeps the session alive. The page asks when it loads, when
 * it is shown again after being hidden, and at each moment below, and
 * nothing else. It keeps no clock of the server's: it counts the seconds
 * the server gave it on its own clock.
 *
 * The session ends at the idle end or the 8-hour end, whichever is first.
 * Two minutes before that end one dialog opens, never two:
 *
 *   "Still there?"            the idle end comes first, and the 8-hour end
 *                             is not within the same 2 minutes. Focus on
 *                             "Stay signed in". "Stay signed in" sends one
 *                             POST /admin/api/session/stay-signed-in with
 *                             the page's anti-forgery token: the server
 *                             renews the idle window only. "Sign out" is the
 *                             nav's Sign out. Escape closes the dialog and
 *                             sends nothing.
 *   "Your session is ending"  the 8-hour end is within those 2 minutes.
 *                             role="alertdialog", one "OK"; Escape acts as
 *                             OK. Nothing can extend the session, and the
 *                             dialog does not come back once closed.
 *
 * Either dialog is modal: focus moves into it, Tab stays in it, and on
 * closing, focus goes back to where it was. An open dialog sends nothing by
 * itself: only "Stay signed in" (or the owner opening a page) renews the
 * session.
 *
 * At the end the page asks once more. The gate's own 401 -> the page asks
 * for itself anew, and the gate sends it to the sign-in page with the
 * "session ended" note, keeping the page for after sign-in. Still signed in
 * (the owner was active in another tab) -> the dialog closes and the count
 * starts again. No clear answer -> it asks again 30 seconds later.
 *
 * Nothing is stored, and text goes into the page through textContent only.
 */
(function () {
  "use strict";

  var STATUS = "/admin/api/session";
  var STAY_SIGNED_IN = "/admin/api/session/stay-signed-in";
  var GATE_DENIED = "Unauthorized";
  var WARN_MS = 2 * 60 * 1000;
  var RETRY_MS = 30 * 1000;
  var MIN_WAIT_MS = 1000;
  // Two answers about the same 8-hour end differ by the rounding of whole seconds.
  var SAME_END_MS = 5000;

  // Titles and button labels: TD-ADM-UI-auth-14 and TD-ADM-UI-auth-24.
  // The two sentences are new copy.
  var COPY = {
    idleTitle: "Still there?",
    idleText: "Your session will end within 2 minutes because there has been no activity.",
    stay: "Stay signed in",
    signOut: "Sign out",
    endingTitle: "Your session is ending",
    endingText: "Your session will end within 2 minutes because of the 8-hour limit, and it can't be extended. "
      + "Anything you haven't saved will be lost.",
    ok: "OK"
  };

  if (typeof AdminRequest === "undefined" || typeof HTMLDialogElement === "undefined") return;

  var idleEnd = 0;
  var absoluteEnd = 0;
  var timer = null;
  var asking = false;
  var ending = false;
  var busy = false;
  /** The open dialog: { kind: "idle" | "ending", dialog, returnTo }, or null. */
  var open = null;
  /** The 8-hour end whose dialog the owner has closed, or 0. */
  var endingClosedFor = 0;

  function isGateDenial(reply) {
    return reply.status === 401 && (reply.body || {}).error === GATE_DENIED;
  }

  function secondsIn(value) {
    return typeof value === "number" && isFinite(value) && value >= 0;
  }

  /** Takes the two ends from a 200 reply. False for anything else. */
  function learn(reply) {
    var body = reply.body || {};
    if (reply.status !== 200 || !secondsIn(body.idleSecondsLeft) || !secondsIn(body.absoluteSecondsLeft)) return false;
    var now = Date.now();
    idleEnd = now + body.idleSecondsLeft * 1000;
    absoluteEnd = now + body.absoluteSecondsLeft * 1000;
    return true;
  }

  function wait(ms) {
    clearTimeout(timer);
    timer = setTimeout(ask, Math.max(ms, MIN_WAIT_MS));
  }

  function ask() {
    if (asking || ending) return;
    asking = true;
    AdminRequest.get(STATUS).then(function (reply) {
      asking = false;
      if (isGateDenial(reply)) end();
      else if (learn(reply)) decide();
      else wait(RETRY_MS);
    }, function () {
      asking = false;
      wait(RETRY_MS);
    });
  }

  /** The session is over: the page meets the gate again, which sends it to sign-in. */
  function end() {
    if (ending) return;
    ending = true;
    clearTimeout(timer);
    location.reload();
  }

  function decide() {
    var now = Date.now();
    var sessionEnd = Math.min(idleEnd, absoluteEnd);
    if (sessionEnd - now > WARN_MS) {
      // Not yet, or no longer (renewed in another tab): no dialog.
      close();
      wait(sessionEnd - WARN_MS - now);
      return;
    }

    // The last 2 minutes. When the 8-hour end falls in them, only its dialog shows.
    if (absoluteEnd - WARN_MS <= idleEnd) {
      if (open && open.kind === "idle") close();
      if (!open && Math.abs(endingClosedFor - absoluteEnd) > SAME_END_MS) show("ending");
    } else if (open && open.kind === "ending") {
      // A new session (a sign-in in another tab) with its own 8-hour end.
      close();
      show("idle");
    } else if (!open) {
      show("idle");
    }
    // At the end, ask again: the gate's answer decides.
    wait(sessionEnd - now);
  }

  function element(tag, id, text) {
    var el = document.createElement(tag);
    if (id) el.id = id;
    if (text) el.textContent = text;
    return el;
  }

  function button(id, label, secondary, onPress) {
    var b = element("button", id, label);
    b.type = "button";
    if (secondary) b.className = "secondary";
    b.addEventListener("click", onPress);
    return b;
  }

  function show(kind) {
    var idle = kind === "idle";
    var dialog = element("dialog", idle ? "session-idle" : "session-ending");
    dialog.className = "session-dialog";
    if (!idle) dialog.setAttribute("role", "alertdialog");
    dialog.setAttribute("aria-labelledby", "session-dialog-title");
    dialog.setAttribute("aria-describedby", "session-dialog-text");
    dialog.appendChild(element("h2", "session-dialog-title", idle ? COPY.idleTitle : COPY.endingTitle));
    dialog.appendChild(element("p", "session-dialog-text", idle ? COPY.idleText : COPY.endingText));

    var actions = element("div");
    actions.className = "session-dialog-actions";
    var first;
    if (idle) {
      first = button("stay-signed-in", COPY.stay, false, function () { staySignedIn(first); });
      actions.appendChild(first);
      actions.appendChild(button("session-sign-out", COPY.signOut, true, signOut));
    } else {
      first = button("session-ending-ok", COPY.ok, false, dismissEnding);
      actions.appendChild(first);
    }
    dialog.appendChild(actions);

    // Escape: the idle dialog closes and sends nothing; the 8-hour one acts as OK.
    dialog.addEventListener("cancel", function (event) {
      event.preventDefault();
      if (idle) close();
      else dismissEnding();
    });
    // Closed by the browser itself: tidy up the same way.
    dialog.addEventListener("close", function () {
      if (open && open.dialog === dialog) close();
    });
    dialog.addEventListener("keydown", function (event) {
      if (event.key === "Tab") keepFocusIn(dialog, event);
    });

    open = { kind: kind, dialog: dialog, returnTo: document.activeElement };
    document.body.appendChild(dialog);
    dialog.showModal();
    first.focus();
  }

  /** Tab from the last control goes to the first, Shift+Tab from the first to the last. */
  function keepFocusIn(dialog, event) {
    var controls = dialog.querySelectorAll("button:not([disabled])");
    if (!controls.length) return;
    var firstControl = controls[0];
    var lastControl = controls[controls.length - 1];
    if (event.shiftKey && document.activeElement === firstControl) {
      event.preventDefault();
      lastControl.focus();
    } else if (!event.shiftKey && document.activeElement === lastControl) {
      event.preventDefault();
      firstControl.focus();
    }
  }

  function close() {
    if (!open) return;
    var was = open;
    open = null;
    busy = false;
    if (was.dialog.open) was.dialog.close();
    if (was.dialog.parentNode) was.dialog.parentNode.removeChild(was.dialog);
    var back = was.returnTo;
    if (back && back !== document.body && back.isConnected && typeof back.focus === "function") back.focus();
  }

  function dismissEnding() {
    endingClosedFor = absoluteEnd;
    close();
  }

  function signOut() {
    close();
    var nav = document.getElementById("sign-out");
    if (nav) nav.click();
  }

  /** The retry sentence the shell gives its Sign out form, said in the dialog. */
  function sayRetry(stay) {
    if (!open) return;
    var form = document.getElementById("sign-out-form");
    var note = open.dialog.querySelector(".session-dialog-error");
    if (!note) {
      note = element("p");
      note.className = "session-dialog-error";
      note.setAttribute("role", "alert");
      open.dialog.insertBefore(note, open.dialog.lastChild);
    }
    note.textContent = form ? form.getAttribute("data-retry") || "" : "";
    stay.focus();
  }

  function staySignedIn(stay) {
    if (busy) return;
    busy = true;
    stay.disabled = true;
    AdminRequest.post(STAY_SIGNED_IN, {}).then(function (reply) {
      busy = false;
      stay.disabled = false;
      if (isGateDenial(reply)) {
        end();
      } else if (learn(reply)) {
        close();
        decide();
      } else {
        sayRetry(stay);
      }
    }, function () {
      busy = false;
      stay.disabled = false;
      sayRetry(stay);
    });
  }

  document.addEventListener("visibilitychange", function () {
    if (document.visibilityState === "visible") ask();
  });

  ask();
})();
