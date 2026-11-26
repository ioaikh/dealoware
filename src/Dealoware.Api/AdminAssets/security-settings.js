/**
 * S-A12 Security settings (/admin/settings/security). Served only with a
 * valid session. The server actions are JSON under
 * /admin/api/settings/security (AdminSecuritySettingsEndpoints); every POST
 * goes through admin-request.js, so it carries the page's anti-forgery token
 * (the dw-csrf meta) in X-CSRF-TOKEN, follows no redirect and sets no mode.
 *
 * The helpers are the signed-out pages' own files, loaded from /admin/auth/
 * as the shell loads admin-request.js: admin-dom.js (text in through
 * textContent only), admin-password.js (length check, show toggles) and
 * admin-clipboard.js (Copy). One copy of each is served; none is duplicated
 * under /admin/assets/.
 *
 * One request at a time, page-wide: while one is out, every submit and
 * button that would send another does nothing.
 *
 * Change password (S-A12 note v1.4). POST /password with {currentPassword,
 * newPassword} and the one second factor on screen, {totpCode} or
 * {recoveryCode}. Confirm new password is compared here and never sent.
 *   200 {"message", "signedOut": true}, the message exactly the form's
 *       data-changed -> the server has ended every session: sign-in, with
 *       the note that the password was changed.
 *   400 reason "step_up_failed" -> the server's one sentence in the form's
 *       polite live region; Current password and the code are cleared, New
 *       and Confirm new are kept, the focus goes to Current password.
 *   400 with another reason -> a password rule: the server's named message
 *       under New password; every field is kept; focus on New password.
 *
 * Create new codes, Set up again (TD-ADM-174). Each needs a step-up within
 * the last five minutes. The page sends the action alone when it gave a
 * step-up less than five minutes ago (by its own clock, counted from before
 * that request left); otherwise, and whenever the server answers 403
 * "step_up_required", it opens the step-up dialog, and the action is sent
 * again with the current password and a code in its body (the server checks
 * and records the step-up first).
 *   POST /recovery-codes -> 200 {"codes": [...], "recoveryCodesLeft"}: the
 *       codes are shown once, as on S-A7.
 *   POST /authenticator -> 200 {"key", "otpauthUri", "qrSvg"}: the new key,
 *       its QR code (drawn here from the reply's path data, never parsed as
 *       markup) and its account name. Then POST /authenticator/confirm with
 *       {code}: 200 {"changed": true, "codes", "recoveryCodesLeft"} -> the
 *       new codes, shown once; 400 reason "code_failed" -> the server's
 *       sentence, the field cleared; 403 "step_up_required" -> the key is
 *       gone and the step-up is asked for again.
 *   400 "step_up_failed" in the dialog -> the server's sentence in the
 *       dialog; the password and the code are cleared; focus on the password.
 *
 * Every POST:
 *   401 from the session gate ({"error": "Unauthorized"}) -> sign-in, with
 *       the "session ended" note.
 *   401 reason "signed_out" -> the account locked and every session ended:
 *       sign-in with #again, where the page shows the server's generic
 *       sign-in sentence (note v1.4 (b): no duration, no "locked").
 *   403 without "step_up_required" -> the gate did not take this page's
 *       anti-forgery token (a newer pair from another tab): as console.js,
 *       ask GET /admin/api/me; still signed in -> load the page anew, once.
 *   No clear answer (no reply, a time-out, a server error, a 200 that is not
 *       exactly the expected shape) -> ask GET /admin/api/me: the session
 *       gone -> sign-in with the "session ended" note; otherwise the retry
 *       sentence, and the step-up is asked for again before the next action.
 *
 * Reads, on load: GET / (the count of unused codes) and GET /activity
 * (Recent sign-in activity and the 24-hour lock notice). Times are the
 * server's, shown in ET.
 *
 * Secrets. The new codes, the new key, its otpauth address (only as the
 * account name) and its QR code exist only in the page while they are shown,
 * put there with textContent and createElementNS/setAttribute, never as
 * markup. They are never in an address, in storage or in a log. "Continue"
 * and "Cancel" take them out of the page; so does leaving it (pagehide),
 * which also empties every field. Nothing is stored.
 */
(function () {
  "use strict";

  var API = "/admin/api/settings/security";
  var PASSWORD = API + "/password";
  var CODES = API + "/recovery-codes";
  var AUTHENTICATOR = API + "/authenticator";
  var CONFIRM = API + "/authenticator/confirm";
  var ACTIVITY = API + "/activity";
  var ME = "/admin/api/me";

  // The sign-in page with one of its fixed notes (admin-arrival.js).
  var SIGN_IN_CHANGED = "/admin/sign-in#changed";
  var SIGN_IN_ENDED = "/admin/sign-in#ended";
  var SIGN_IN_REFUSED = "/admin/sign-in#again";
  var FRESH = "#fresh";

  var GATE_DENIED = "Unauthorized";
  var SIGNED_IN_AS = "CoreOwner";
  var STEP_UP_FAILED = "step_up_failed";
  var STEP_UP_REQUIRED = "step_up_required";
  var SIGNED_OUT = "signed_out";
  var CODE_FAILED = "code_failed";

  // Five minutes (TD-ADM-174), less a margin for the time a request takes.
  var STEP_UP_MS = 5 * 60 * 1000 - 15 * 1000;

  var SVG = "http://www.w3.org/2000/svg";
  var OTPAUTH = "otpauth://totp/";

  var root = document.getElementById("security-settings");
  var live = document.getElementById("console-status");
  if (!root) return;

  // A helper this page needs did not arrive: nothing could be sent, so the page says so and shows no form.
  if (typeof AdminRequest === "undefined" || typeof AdminDom === "undefined"
      || typeof AdminPassword === "undefined" || typeof AdminClipboard === "undefined") {
    if (live) live.textContent = root.getAttribute("data-retry") || "";
    return;
  }

  var el = AdminDom.el;
  var retrySentence = root.getAttribute("data-retry") || "";

  var busy = false;
  var stepUpUntil = 0;
  // A page loaded anew (by this script over a refused token, or by the owner) does not do it again by itself.
  var navigation = typeof performance.getEntriesByType === "function" ? performance.getEntriesByType("navigation") : [];
  var mayReload = !(navigation.length > 0 && navigation[0].type === "reload");

  /* ---- Shared ---- */

  function say(region, message) {
    AdminDom.setText(region, message);
  }

  function text(value) {
    return AdminRequest.text(value);
  }

  function setBusy(on, button, form) {
    busy = on;
    if (button) button.disabled = on;
    if (form) {
      if (on) form.setAttribute("aria-busy", "true");
      else form.removeAttribute("aria-busy");
    }
  }

  /** The server has ended this session, or the owner's lock has. Never stays on this page. */
  function leaveIfSignedOut(reply) {
    var body = reply.body || {};
    if (reply.status === 401 && body.error === GATE_DENIED) {
      location.replace(SIGN_IN_ENDED);
      return true;
    }
    if (reply.status === 401 && body.reason === SIGNED_OUT) {
      location.replace(SIGN_IN_REFUSED);
      return true;
    }
    return false;
  }

  /** The gate refused this page's anti-forgery token: load the page anew once, if still signed in. */
  function afterTokenRefused(region) {
    AdminRequest.get(ME).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 401 && body.error === GATE_DENIED) {
        location.replace(SIGN_IN_ENDED);
      } else if (mayReload && reply.status === 200 && body.principal === SIGNED_IN_AS) {
        clearSecrets();
        history.replaceState(null, "", location.pathname + location.search + FRESH);
        location.reload();
      } else {
        say(region, retrySentence);
      }
    }, function () {
      say(region, retrySentence);
    });
  }

  /**
   * No clear answer: the action may or may not have gone through. A session
   * that has gone means sign-in; otherwise the retry sentence, and the next
   * action asks for a step-up again (the one just sent may have been used).
   */
  function afterUnclearReply(region, then) {
    stepUpUntil = 0;
    AdminRequest.get(ME).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 401 && body.error === GATE_DENIED) {
        location.replace(SIGN_IN_ENDED);
        return;
      }
      say(region, retrySentence);
      if (then) then();
    }, function () {
      say(region, retrySentence);
      if (then) then();
    });
  }

  function isCodeList(value) {
    if (!Array.isArray(value) || value.length === 0) return false;
    for (var i = 0; i < value.length; i += 1) {
      if (typeof value[i] !== "string" || value[i].length === 0) return false;
    }
    return true;
  }

  /** One second factor at a time, with the switch between them (as on S-A10). */
  function secondFactor(prefix) {
    var factors = {
      code: {
        block: el(prefix + "-code-field"),
        field: el(prefix + "-code"),
        error: el(prefix + "-code-error"),
        // Spaces are dropped ("123 456" is how many apps show it); then six digits.
        read: function (raw) { return raw.replace(/\s+/g, ""); },
        wellFormed: function (value) { return /^[0-9]{6}$/.test(value); },
        sendAs: "totpCode",
        other: "recovery",
        switchLabel: "data-to-recovery"
      },
      recovery: {
        block: el(prefix + "-recovery-code-field"),
        field: el(prefix + "-recovery-code"),
        error: el(prefix + "-recovery-code-error"),
        // Sent as typed. The server ignores case, spaces and dashes.
        read: function (raw) { return raw.trim(); },
        wellFormed: function (value) { return value.length > 0; },
        sendAs: "recoveryCode",
        other: "code",
        switchLabel: "data-to-code"
      }
    };
    var toggle = el(prefix + "-factor-switch");
    var current = factors.code;

    function use(next, focus) {
      var previous = current;
      current = next;
      previous.field.value = "";
      AdminDom.setInvalid(previous.field, previous.error, "");
      AdminDom.show(previous.block, false);
      AdminDom.show(current.block, true);
      AdminDom.setText(toggle, toggle.getAttribute(current.switchLabel) || "");
      if (focus) current.field.focus();
    }

    toggle.addEventListener("click", function () {
      use(factors[current.other], true);
    });
    [factors.code, factors.recovery].forEach(function (one) {
      one.field.addEventListener("input", function () {
        if (one.field.hasAttribute("aria-invalid")) AdminDom.setInvalid(one.field, one.error, "");
      });
    });

    return {
      check: function () {
        var message = current.wellFormed(current.read(current.field.value))
          ? ""
          : current.field.getAttribute("data-required") || "";
        AdminDom.setInvalid(current.field, current.error, message);
        return message;
      },
      addTo: function (body) {
        body[current.sendAs] = current.read(current.field.value);
        return body;
      },
      focus: function () { current.field.focus(); },
      clear: function () {
        factors.code.field.value = "";
        factors.recovery.field.value = "";
        AdminDom.setInvalid(factors.code.field, factors.code.error, "");
        AdminDom.setInvalid(factors.recovery.field, factors.recovery.error, "");
      },
      /** Back to the authenticator code, emptied. */
      reset: function () {
        if (current !== factors.code) use(factors.code, false);
        this.clear();
      }
    };
  }

  function checkRequired(input, error) {
    var message = input.value ? "" : input.getAttribute("data-required") || "";
    AdminDom.setInvalid(input, error, message);
    return message;
  }

  function hidePassword(input, toggleId) {
    input.setAttribute("type", "password");
    var toggle = el(toggleId);
    if (toggle) toggle.setAttribute("aria-pressed", "false");
  }

  /* ---- Change password (S-A12 note v1.4) ---- */

  var passwordForm = el("password-form");
  var passwordAlert = el("password-alert");
  var passwordSubmit = el("password-submit");
  var current = el("current-password");
  var currentError = el("current-password-error");
  var newPassword = el("new-password");
  var newPasswordError = el("new-password-error");
  var confirmPassword = el("confirm-password");
  var confirmPasswordError = el("confirm-password-error");
  var passwordFactor = secondFactor("password");

  function checkLength() {
    var message = AdminPassword.lengthMessage(newPassword);
    AdminDom.setInvalid(newPassword, newPasswordError, message);
    return message;
  }

  function checkMatch() {
    var message = newPassword.value === confirmPassword.value ? "" : confirmPassword.getAttribute("data-mismatch") || "";
    AdminDom.setInvalid(confirmPassword, confirmPasswordError, message);
    return message;
  }

  function passwordUnclear() {
    setBusy(false, passwordSubmit, passwordForm);
    afterUnclearReply(passwordAlert, null);
  }

  passwordForm.addEventListener("submit", function (event) {
    event.preventDefault();
    if (busy) return;
    say(passwordAlert, "");
    var currentMissing = checkRequired(current, currentError);
    var lengthError = checkLength();
    var mismatch = checkMatch();
    var factorError = passwordFactor.check();
    if (currentMissing) { current.focus(); return; }
    if (lengthError) { newPassword.focus(); return; }
    if (mismatch) { confirmPassword.focus(); return; }
    if (factorError) { passwordFactor.focus(); return; }

    var body = passwordFactor.addTo({ currentPassword: current.value, newPassword: newPassword.value });
    setBusy(true, passwordSubmit, passwordForm);
    AdminRequest.post(PASSWORD, body).then(function (reply) {
      var data = reply.body || {};
      if (reply.status === 200) {
        if (data.signedOut === true && text(data.message) && text(data.message) === passwordForm.getAttribute("data-changed")) {
          clearSecrets();
          location.replace(SIGN_IN_CHANGED);
        } else {
          passwordUnclear();
        }
        return;
      }
      if (leaveIfSignedOut(reply)) return;
      setBusy(false, passwordSubmit, passwordForm);
      var error = text(data.error);
      if (reply.status === 400 && data.reason === STEP_UP_FAILED && error) {
        // Note v1.4 (b): clear the current password and the code, keep the new ones, focus Current password.
        current.value = "";
        passwordFactor.clear();
        say(passwordAlert, error);
        current.focus();
      } else if (reply.status === 400 && error) {
        // A password rule: the server's named message under New password. Every field keeps what was typed.
        AdminDom.setInvalid(newPassword, newPasswordError, error);
        newPassword.focus();
      } else if (reply.status === 403) {
        afterTokenRefused(passwordAlert);
      } else if (reply.status >= 400 && reply.status < 500) {
        say(passwordAlert, retrySentence);
      } else {
        passwordUnclear();
      }
    }, passwordUnclear);
  });

  current.addEventListener("input", function () {
    if (current.hasAttribute("aria-invalid")) AdminDom.setInvalid(current, currentError, "");
  });
  newPassword.addEventListener("blur", function () {
    if (newPassword.value) checkLength();
  });
  newPassword.addEventListener("input", function () {
    if (newPassword.hasAttribute("aria-invalid")) AdminDom.setInvalid(newPassword, newPasswordError, "");
  });
  // Note v1.4 (c): the mismatch is checked on blur and on submit.
  confirmPassword.addEventListener("blur", function () {
    if (confirmPassword.value) checkMatch();
  });
  confirmPassword.addEventListener("input", function () {
    if (confirmPassword.hasAttribute("aria-invalid")) AdminDom.setInvalid(confirmPassword, confirmPasswordError, "");
  });
  AdminPassword.bindToggle("current-password", "current-password-toggle");
  AdminPassword.bindToggle("new-password", "new-password-toggle");
  AdminPassword.bindToggle("confirm-password", "confirm-password-toggle");

  /* ---- Codes shown once (as on S-A7) ---- */

  var codesLeft = el("codes-left");

  function showCount(left) {
    if (typeof left !== "number" || left < 0 || Math.floor(left) !== left) return;
    var words = left === 1 ? codesLeft.getAttribute("data-one") : (codesLeft.getAttribute("data-many") || "").replace("{0}", String(left));
    AdminDom.setText(codesLeft, words || "");
  }

  function codesPanel(prefix, afterDone) {
    var panel = el(prefix);
    var lede = el(prefix + "-lede");
    var list = el(prefix + "-list");
    var copy = el(prefix + "-copy");
    var copyStatus = el(prefix + "-copy-status");
    var saved = el(prefix + "-saved");
    var done = el(prefix + "-done");

    function empty() {
      AdminDom.clear(list);
      say(copyStatus, "");
      saved.checked = false;
      done.disabled = true;
      AdminDom.show(panel, false);
    }

    saved.addEventListener("change", function () {
      done.disabled = !saved.checked;
    });
    done.addEventListener("click", function () {
      if (!saved.checked) return;
      empty();
      afterDone();
    });
    copy.addEventListener("click", function () {
      var lines = [];
      var items = list.getElementsByTagName("li");
      for (var i = 0; i < items.length; i += 1) lines.push(items[i].textContent || "");
      say(copyStatus, "");
      AdminClipboard.copy(lines.join("\n"), list).then(function (copied) {
        say(copyStatus, copy.getAttribute(copied ? "data-copied" : "data-copy-failed") || "");
        copy.focus();
      });
    });

    return {
      show: function (codes) {
        AdminDom.clear(list);
        for (var i = 0; i < codes.length; i += 1) {
          var item = document.createElement("li");
          item.textContent = codes[i];
          list.appendChild(item);
        }
        say(copyStatus, "");
        saved.checked = false;
        done.disabled = true;
        AdminDom.show(panel, true);
        lede.focus();
      },
      empty: empty
    };
  }

  /* ---- Step-up (TD-ADM-174) ---- */

  var dialog = el("step-up-dialog");
  var stepUpForm = el("step-up-form");
  var stepUpAlert = el("step-up-alert");
  var stepUpSubmit = el("step-up-submit");
  var stepUpCancel = el("step-up-cancel");
  var stepUpPassword = el("step-up-password");
  var stepUpPasswordError = el("step-up-password-error");
  var stepUpFactor = secondFactor("step-up");
  // What the dialog is for: a function(proof, sentAt), and the button to give the focus back to.
  var pending = null;
  var opener = null;
  // The second look at the focus after a close (see the "close" handler), while it waits.
  var refocusTimer = 0;

  function stepUpFresh() {
    return Date.now() < stepUpUntil;
  }

  function clearStepUp() {
    stepUpPassword.value = "";
    hidePassword(stepUpPassword, "step-up-password-toggle");
    AdminDom.setInvalid(stepUpPassword, stepUpPasswordError, "");
    stepUpFactor.reset();
    say(stepUpAlert, "");
  }

  function openStepUp(action, from, message) {
    // A dialog opened again owes the focus to no button of the one closed before it.
    clearTimeout(refocusTimer);
    pending = action;
    opener = from;
    clearStepUp();
    if (!dialog.open) dialog.showModal();
    if (message) say(stepUpAlert, message);
    stepUpPassword.focus();
  }

  /** The page closes the dialog after an action: the action decides where the focus goes. */
  function closeStepUp() {
    pending = null;
    opener = null;
    if (dialog.open) dialog.close();
    clearStepUp();
  }

  /** A refused step-up: the server's sentence; the password and the code are cleared; focus on the password. */
  function stepUpRefused(message) {
    stepUpPassword.value = "";
    stepUpFactor.clear();
    say(stepUpAlert, message);
    stepUpPassword.focus();
  }

  // Escape is handled here, not left to the browser: WebKit fires no "cancel" for an Escape typed
  // in the dialog's fields, so the dialog would stay open; and Chromium closes a modal dialog on a
  // second Escape even when its "cancel" was refused. While a request is out the dialog waits for
  // the answer, so a refusal is never written into a closed dialog. It listens on the document:
  // while a request is out the dialog's Continue is disabled, so the focus may have left the
  // dialog for the body, and Escape then reaches no element in it. An Escape meant for anything
  // else (another dialog in front, a field outside) is left alone. So is an Escape typed while
  // text is being composed (an input method's window is open): it ends the composition only.
  document.addEventListener("keydown", function (event) {
    if (event.isComposing) return;
    if (!dialog.open || (event.key !== "Escape" && event.key !== "Esc")) return;
    var target = event.target;
    if (target !== dialog && !dialog.contains(target) && target !== document.body && target !== document.documentElement) return;
    event.preventDefault();
    if (!busy) dialog.close();
  }, true);
  // Any other close the browser asks for: the same rule.
  dialog.addEventListener("cancel", function (event) {
    if (busy) event.preventDefault();
  });
  /** The focus is where nobody put it: on nothing, on the page itself, or still in the closed dialog. */
  function focusAdrift() {
    var active = document.activeElement;
    return !active || active === document.body || active === document.documentElement || dialog.contains(active);
  }

  // Closed by the owner (Escape or Cancel): the focus goes back to the button that opened it. That
  // button may have been disabled while its request was out, so the browser may have nothing to
  // give the focus back to, and some engines settle the focus only after this event: the page
  // looks now, and once more on the next tick.
  // This event comes after the close, not with it (Chromium sends it with the next frame), and
  // the owner may have gone on by then. A focus the owner has put elsewhere stays there: only a
  // focus that is adrift is given to the button. And a dialog that is open again is another one:
  // its action, its button and its fields are not this event's to clear.
  dialog.addEventListener("close", function () {
    if (dialog.open) return;
    var back = opener;
    pending = null;
    opener = null;
    clearStepUp();
    clearTimeout(refocusTimer);
    if (!back || back.hidden) return;
    if (focusAdrift()) back.focus();
    refocusTimer = setTimeout(function () {
      if (!dialog.open && focusAdrift()) back.focus();
    }, 0);
  });
  stepUpCancel.addEventListener("click", function () {
    if (!busy) dialog.close();
  });
  stepUpPassword.addEventListener("input", function () {
    if (stepUpPassword.hasAttribute("aria-invalid")) AdminDom.setInvalid(stepUpPassword, stepUpPasswordError, "");
  });
  AdminPassword.bindToggle("step-up-password", "step-up-password-toggle");

  stepUpForm.addEventListener("submit", function (event) {
    event.preventDefault();
    if (busy || !pending) return;
    say(stepUpAlert, "");
    var missing = checkRequired(stepUpPassword, stepUpPasswordError);
    var factorError = stepUpFactor.check();
    if (missing) { stepUpPassword.focus(); return; }
    if (factorError) { stepUpFactor.focus(); return; }
    var proof = stepUpFactor.addTo({ currentPassword: stepUpPassword.value });
    pending(proof, Date.now());
  });

  /**
   * Sends one step-up action, with the proof from the dialog or with none.
   * onOk(body) runs for a 200 that has the expected shape (isOk(body)).
   */
  function sendProtected(path, proof, sentAt, button, region, isOk, onOk, again) {
    var viaDialog = sentAt > 0;
    setBusy(true, viaDialog ? stepUpSubmit : button, viaDialog ? stepUpForm : null);
    AdminRequest.post(path, proof).then(function (reply) {
      var body = reply.body || {};
      setBusy(false, viaDialog ? stepUpSubmit : button, viaDialog ? stepUpForm : null);
      if (reply.status === 200 && isOk(body)) {
        if (viaDialog) {
          stepUpUntil = sentAt + STEP_UP_MS;
          closeStepUp();
        }
        onOk(body);
        return;
      }
      if (leaveIfSignedOut(reply)) return;
      var error = text(body.error);
      if (reply.status === 403 && body.reason === STEP_UP_REQUIRED && error) {
        // No step-up, or one older than five minutes: ask for it.
        stepUpUntil = 0;
        if (viaDialog) stepUpRefused(error);
        else openStepUp(again, button, error);
      } else if (reply.status === 400 && body.reason === STEP_UP_FAILED && error && viaDialog) {
        stepUpRefused(error);
      } else if (reply.status === 403) {
        afterTokenRefused(viaDialog ? stepUpAlert : region);
      } else if (reply.status >= 400 && reply.status < 500) {
        say(viaDialog ? stepUpAlert : region, retrySentence);
      } else {
        unclear();
      }
    }, function () {
      setBusy(false, viaDialog ? stepUpSubmit : button, viaDialog ? stepUpForm : null);
      unclear();
    });

    function unclear() {
      // The proof may have been used: the dialog closes, and the next action asks again.
      if (viaDialog) closeStepUp();
      afterUnclearReply(region, function () {
        loadState();
        button.focus();
      });
    }
  }

  /* ---- Recovery codes ---- */

  var codesCreate = el("codes-create");
  var codesAlert = el("codes-alert");
  var newCodes = codesPanel("codes-new", function () { codesCreate.focus(); });

  function sendCodes(proof, sentAt) {
    sendProtected(CODES, proof, sentAt, codesCreate, codesAlert,
      function (body) { return isCodeList(body.codes) && typeof body.recoveryCodesLeft === "number"; },
      function (body) {
        showCount(body.recoveryCodesLeft);
        newCodes.show(body.codes);
        loadActivity();
      },
      sendCodes);
  }

  codesCreate.addEventListener("click", function () {
    if (busy) return;
    say(codesAlert, "");
    if (stepUpFresh()) sendCodes({}, 0);
    else openStepUp(sendCodes, codesCreate, "");
  });

  /* ---- Authenticator ---- */

  var authenticatorStart = el("authenticator-start");
  var authenticatorAlert = el("authenticator-alert");
  var newKey = el("new-key");
  var newKeyLede = el("new-key-lede");
  var qrSlot = el("new-key-qr");
  var manualKey = el("manual-key");
  var accountName = el("account-name");
  var copyKey = el("copy-key");
  var copyKeyStatus = el("copy-key-status");
  var newKeyForm = el("new-key-form");
  var newKeyAlert = el("new-key-alert");
  var newKeyCode = el("new-key-code");
  var newKeyCodeError = el("new-key-code-error");
  var newKeySubmit = el("new-key-submit");
  var newKeyCancel = el("new-key-cancel");
  var reenrolCodes = codesPanel("reenrol-codes", function () { authenticatorStart.focus(); });

  /** The QR code from the reply: its size and its path data only, each checked, drawn with createElementNS. */
  function qrParts(svg) {
    if (typeof svg !== "string") return null;
    var size = /viewBox="0 0 (\d{2,3}) (\d{2,3})"/.exec(svg);
    var path = /<path fill="#000" d="([Mhvz0-9 -]+)"\/>/.exec(svg);
    if (!size || !path || size[1] !== size[2]) return null;
    return { side: size[1], d: path[1] };
  }

  function keyParts(body) {
    var key = text(body.key);
    var uri = text(body.otpauthUri);
    var qr = qrParts(body.qrSvg);
    var end = uri.indexOf("?");
    if (!/^[A-Z2-7]{16,128}$/.test(key) || uri.indexOf(OTPAUTH) !== 0 || end <= OTPAUTH.length || !qr) return null;
    var name;
    try {
      name = decodeURIComponent(uri.slice(OTPAUTH.length, end));
    } catch (e) {
      return null;
    }
    return { key: key, name: name, qr: qr };
  }

  function drawQr(qr) {
    var svg = document.createElementNS(SVG, "svg");
    svg.setAttribute("id", "authenticator-qr");
    svg.setAttribute("class", "qr");
    svg.setAttribute("role", "img");
    svg.setAttribute("aria-label", qrSlot.getAttribute("data-label") || "");
    svg.setAttribute("aria-describedby", "manual-key-help");
    svg.setAttribute("viewBox", "0 0 " + qr.side + " " + qr.side);
    svg.setAttribute("shape-rendering", "crispEdges");
    var square = document.createElementNS(SVG, "rect");
    square.setAttribute("width", qr.side);
    square.setAttribute("height", qr.side);
    square.setAttribute("fill", "#fff");
    var modules = document.createElementNS(SVG, "path");
    modules.setAttribute("fill", "#000");
    modules.setAttribute("d", qr.d);
    svg.appendChild(square);
    svg.appendChild(modules);
    AdminDom.clear(qrSlot);
    qrSlot.appendChild(svg);
  }

  /** The key in groups of four, set apart by the stylesheet, so the text copied by hand is the key itself. */
  function writeKey(key) {
    AdminDom.clear(manualKey);
    for (var at = 0; at < key.length; at += 4) {
      if (at > 0) manualKey.appendChild(document.createElement("wbr"));
      var group = document.createElement("span");
      group.textContent = key.slice(at, at + 4);
      manualKey.appendChild(group);
    }
  }

  function showKey(parts) {
    reenrolCodes.empty();
    drawQr(parts.qr);
    writeKey(parts.key);
    AdminDom.setText(accountName, parts.name);
    say(copyKeyStatus, "");
    say(newKeyAlert, "");
    newKeyCode.value = "";
    AdminDom.setInvalid(newKeyCode, newKeyCodeError, "");
    AdminDom.show(newKey, true);
    // The QR code comes first, so the cursor is not put in the field (as on S-A6).
    newKeyLede.focus();
  }

  function hideKey() {
    AdminDom.clear(qrSlot);
    AdminDom.clear(manualKey);
    AdminDom.setText(accountName, "");
    say(copyKeyStatus, "");
    say(newKeyAlert, "");
    newKeyCode.value = "";
    AdminDom.setInvalid(newKeyCode, newKeyCodeError, "");
    AdminDom.show(newKey, false);
  }

  function sendStart(proof, sentAt) {
    sendProtected(AUTHENTICATOR, proof, sentAt, authenticatorStart, authenticatorAlert,
      function (body) { return keyParts(body) !== null; },
      function (body) { showKey(keyParts(body)); },
      sendStart);
  }

  authenticatorStart.addEventListener("click", function () {
    if (busy) return;
    say(authenticatorAlert, "");
    authenticatorAlert.classList.remove("done");
    if (stepUpFresh()) sendStart({}, 0);
    else openStepUp(sendStart, authenticatorStart, "");
  });

  newKeyCancel.addEventListener("click", function () {
    if (busy) return;
    hideKey();
    authenticatorStart.focus();
  });

  copyKey.addEventListener("click", function () {
    say(copyKeyStatus, "");
    AdminClipboard.copy(manualKey.textContent || "", manualKey).then(function (copied) {
      say(copyKeyStatus, copyKey.getAttribute(copied ? "data-copied" : "data-copy-failed") || "");
      copyKey.focus();
    });
  });

  newKeyCode.addEventListener("input", function () {
    if (newKeyCode.hasAttribute("aria-invalid")) AdminDom.setInvalid(newKeyCode, newKeyCodeError, "");
  });

  function confirmUnclear() {
    setBusy(false, newKeySubmit, newKeyForm);
    afterUnclearReply(newKeyAlert, function () {
      loadState();
      newKeyCode.focus();
    });
  }

  newKeyForm.addEventListener("submit", function (event) {
    event.preventDefault();
    if (busy) return;
    say(newKeyAlert, "");
    var value = newKeyCode.value.replace(/\s+/g, "");
    if (!/^[0-9]{6}$/.test(value)) {
      AdminDom.setInvalid(newKeyCode, newKeyCodeError, newKeyCode.getAttribute("data-required") || "");
      newKeyCode.focus();
      return;
    }
    AdminDom.setInvalid(newKeyCode, newKeyCodeError, "");
    setBusy(true, newKeySubmit, newKeyForm);
    AdminRequest.post(CONFIRM, { code: value }).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 200) {
        if (body.changed === true && isCodeList(body.codes) && typeof body.recoveryCodesLeft === "number") {
          setBusy(false, newKeySubmit, newKeyForm);
          hideKey();
          authenticatorAlert.classList.add("done");
          say(authenticatorAlert, authenticatorAlert.getAttribute("data-changed") || "");
          showCount(body.recoveryCodesLeft);
          reenrolCodes.show(body.codes);
          loadActivity();
        } else {
          confirmUnclear();
        }
        return;
      }
      if (leaveIfSignedOut(reply)) return;
      setBusy(false, newKeySubmit, newKeyForm);
      var error = text(body.error);
      if (reply.status === 400 && body.reason === CODE_FAILED && error) {
        newKeyCode.value = "";
        say(newKeyAlert, error);
        newKeyCode.focus();
      } else if (reply.status === 403 && body.reason === STEP_UP_REQUIRED && error) {
        // The step-up is older than five minutes: this key can no longer be confirmed. A new one, after a new step-up.
        stepUpUntil = 0;
        hideKey();
        openStepUp(sendStart, authenticatorStart, error);
      } else if (reply.status === 403) {
        afterTokenRefused(newKeyAlert);
      } else if (reply.status >= 400 && reply.status < 500) {
        say(newKeyAlert, retrySentence);
      } else {
        confirmUnclear();
      }
    }, confirmUnclear);
  });

  /* ---- Reads: the count, and Recent sign-in activity ---- */

  var lockNotice = el("lock-notice");
  var lockText = el("lock-notice-text");
  var activityAlert = el("activity-alert");
  var activityEmpty = el("activity-empty");
  var activityWrap = el("activity-wrap");
  var activityRows = el("activity-rows");
  var labels = {};
  (function () {
    var items = el("activity-labels").getElementsByTagName("li");
    for (var i = 0; i < items.length; i += 1) labels[items[i].getAttribute("data-event")] = items[i].textContent || "";
  })();

  var eastern = null;
  try {
    eastern = new Intl.DateTimeFormat("en-US", {
      timeZone: "America/New_York", month: "short", day: "numeric", year: "numeric", hour: "numeric", minute: "2-digit"
    });
  } catch (e) {
    eastern = null;
  }

  /** A server time as the console shows it: "Oct 5, 2026, 7:55 PM ET". */
  function inEt(iso) {
    var at = new Date(iso);
    if (!eastern || isNaN(at.getTime())) return text(iso);
    return eastern.format(at).replace(/[  ]/g, " ") + " ET";
  }

  function loadState() {
    AdminRequest.get(API).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 401 && body.error === GATE_DENIED) location.replace(SIGN_IN_ENDED);
      else if (reply.status === 200 && typeof body.recoveryCodesLeft === "number") showCount(body.recoveryCodesLeft);
      else say(codesAlert, retrySentence);
    }, function () {
      say(codesAlert, retrySentence);
    });
  }

  function showActivity(body) {
    var lock = body.lock;
    if (lock && typeof lock === "object" && typeof lock.startedAt === "string" && typeof lock.endsAt === "string") {
      var words = lockNotice.getAttribute(lock.active === true ? "data-active" : "data-ended") || "";
      AdminDom.setText(lockText, words.replace("{start}", inEt(lock.startedAt)).replace("{end}", inEt(lock.endsAt)));
      AdminDom.show(lockNotice, true);
    } else {
      AdminDom.setText(lockText, "");
      AdminDom.show(lockNotice, false);
    }

    AdminDom.clear(activityRows);
    var items = body.items;
    for (var i = 0; i < items.length; i += 1) {
      var item = items[i] || {};
      var row = document.createElement("tr");
      var when = document.createElement("td");
      var time = document.createElement("time");
      time.setAttribute("datetime", text(item.timestamp));
      time.textContent = inEt(item.timestamp);
      when.appendChild(time);
      var what = document.createElement("td");
      var name = text(item.event);
      what.textContent = Object.prototype.hasOwnProperty.call(labels, name) ? labels[name] : name;
      var where = document.createElement("td");
      where.className = "network";
      where.textContent = text(item.network);
      row.appendChild(when);
      row.appendChild(what);
      row.appendChild(where);
      activityRows.appendChild(row);
    }
    AdminDom.show(activityEmpty, items.length === 0);
    AdminDom.show(activityWrap, items.length > 0);
  }

  function loadActivity() {
    AdminRequest.get(ACTIVITY).then(function (reply) {
      var body = reply.body || {};
      if (reply.status === 401 && body.error === GATE_DENIED) {
        location.replace(SIGN_IN_ENDED);
      } else if (reply.status === 200 && Array.isArray(body.items)) {
        say(activityAlert, "");
        showActivity(body);
      } else {
        say(activityAlert, retrySentence);
      }
    }, function () {
      say(activityAlert, retrySentence);
    });
  }

  /* ---- Leaving the page ---- */

  /** Every code, key and field out of the page. */
  function clearSecrets() {
    newCodes.empty();
    reenrolCodes.empty();
    hideKey();
    [current, newPassword, confirmPassword, stepUpPassword].forEach(function (input) { input.value = ""; });
    passwordFactor.clear();
    stepUpFactor.clear();
    if (dialog.open) dialog.close();
  }

  // A page the browser keeps for its Back button must keep none of it (console.js asks for a kept page anew).
  window.addEventListener("pagehide", clearSecrets);

  root.hidden = false;
  loadState();
  loadActivity();
})();
