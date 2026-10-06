/**
 * Per-screen behaviour for S-A1–S-A11. Encodes all data via textContent.
 */
(function (global) {
  "use strict";

  var busy = false;

  function emailWellFormed(value) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
  }

  function normalizeRecovery(value) {
    return String(value || "").replace(/[\s-]+/g, "").toLowerCase();
  }

  function setBusy(form, on) {
    busy = on;
    if (form) form.setAttribute("aria-busy", on ? "true" : "false");
    var submit = form && form.querySelector("[data-submit]");
    if (submit) submit.disabled = on || submit.hasAttribute("data-hold");
  }

  function showAlert(message) {
    var alert = AdminDom.el("form-alert");
    AdminDom.setText(alert, message);
    AdminDom.show(alert, true);
    if (alert) alert.focus();
  }

  function hideAlert() {
    var alert = AdminDom.el("form-alert");
    AdminDom.setText(alert, "");
    AdminDom.show(alert, false);
  }

  function wireFailureAlert(alert) {
    if (!alert) return;
    AdminDom.clear(alert);
    var parts = AdminCopy.signInFailure.split(AdminCopy.resetLinkText);
    alert.appendChild(document.createTextNode(parts[0] || ""));
    var link = document.createElement("a");
    link.href = "/admin/reset";
    link.id = "reset-password-link";
    AdminDom.setText(link, AdminCopy.resetLinkText);
    alert.appendChild(link);
    if (parts[1]) alert.appendChild(document.createTextNode(parts[1]));
  }

  function showSignInFailure() {
    var alert = AdminDom.el("form-alert");
    wireFailureAlert(alert);
    AdminDom.show(alert, true);
    if (alert) alert.focus();
  }

  function bindDoubleSubmit(form) {
    form.addEventListener("submit", function (event) {
      if (busy) {
        event.preventDefault();
        event.stopImmediatePropagation();
      }
    }, true);
  }

  function holdSubmitUntilTurnstile(form) {
    var submit = form.querySelector("[data-submit]");
    function sync() {
      var ready = AdminTurnstile.state() === "ready" && !!AdminTurnstile.token();
      if (submit) {
        if (ready) submit.removeAttribute("data-hold");
        else submit.setAttribute("data-hold", "true");
        submit.disabled = busy || !ready;
      }
    }
    AdminTurnstile.mount({ onChange: sync });
    sync();
  }

  function applyStatus() {
    var status = AdminApi.queryParam("status");
    var box = AdminDom.el("status-region");
    if (!box || !status) return;
    var text = "";
    if (status === "expired" || status === "signed-out") text = AdminCopy.sessionEnded;
    else if (status === "password-set") text = AdminCopy.passwordSetStatus;
    else if (status === "password-changed") text = AdminCopy.passwordChangedStatus;
    else if (status === "auth-failed") {
      showSignInFailure();
      return;
    }
    if (!text) return;
    AdminDom.setText(box, text);
    AdminDom.show(box, true);
  }

  function focusFirstField() {
    var first = document.querySelector("input:not([type=hidden])");
    if (first && !first.value) first.focus();
  }

  function scanPendingTokenLeak() {
    var hay = window.location.href + document.cookie;
    if (/pending/i.test(hay) && /token=/i.test(window.location.search)) {
      return;
    }
  }

  function initSignIn() {
    applyStatus();
    var form = AdminDom.el("signin-form");
    if (!form) return;
    bindDoubleSubmit(form);
    holdSubmitUntilTurnstile(form);
    AdminPassword.bindToggle("password", "password-toggle");
    focusFirstField();
    form.addEventListener("submit", function (event) {
      event.preventDefault();
      if (busy) return;
      hideAlert();
      var email = AdminDom.el("email").value.trim();
      var password = AdminDom.el("password").value;
      var emailError = !email ? AdminCopy.emailRequired : (emailWellFormed(email) ? "" : AdminCopy.emailInvalid);
      var passwordError = password ? "" : AdminCopy.passwordRequired;
      AdminDom.setInvalid(AdminDom.el("email"), AdminDom.el("email-error"), emailError);
      AdminDom.setInvalid(AdminDom.el("password"), AdminDom.el("password-error"), passwordError);
      if (emailError || passwordError) return;
      if (AdminTurnstile.state() !== "ready" || !AdminTurnstile.token()) return;
      setBusy(form, true);
      AdminApi.signInStep1({
        email: email,
        password: password,
        turnstileToken: AdminTurnstile.token()
      }).then(function (result) {
        setBusy(form, false);
        AdminDom.el("password").value = "";
        if (result.ok) {
          AdminApi.go(AdminApi.PAGES.signInCode);
          return;
        }
        showSignInFailure();
        AdminTurnstile.retry();
      }, function () {
        setBusy(form, false);
        AdminDom.el("password").value = "";
        showSignInFailure();
      });
    });
  }

  function completeStep2(form, body) {
    if (busy) return;
    hideAlert();
    setBusy(form, true);
    var call = body.recoveryCode ? AdminApi.signInRecovery(body) : AdminApi.signInCode(body);
    call.then(function (result) {
      setBusy(form, false);
      if (result.ok) {
        var remaining = result.payload && result.payload.remainingRecoveryCodes;
        var next = AdminApi.PAGES.stats;
        if (result.payload && result.payload.returnPath)
          next = AdminApi.safeReturnPath(result.payload.returnPath);
        if (typeof remaining === "number")
          next += (next.indexOf("?") === -1 ? "?" : "&") + "recoveryRemaining=" + encodeURIComponent(String(remaining));
        AdminApi.go(next);
        return;
      }
      if (result.payload && result.payload.restart) {
        showSignInFailure();
        AdminApi.go(AdminApi.PAGES.signIn);
        return;
      }
      showSignInFailure();
      var code = AdminDom.el("code");
      var recovery = AdminDom.el("recovery-code");
      if (code) code.value = "";
      if (recovery) recovery.value = "";
    }, function () {
      setBusy(form, false);
      showSignInFailure();
    });
  }

  function initSignInCode() {
    scanPendingTokenLeak();
    var form = AdminDom.el("code-form");
    if (!form) return;
    bindDoubleSubmit(form);
    focusFirstField();
    var back = AdminDom.el("back-to-signin");
    if (back) {
      back.addEventListener("click", function (event) {
        event.preventDefault();
        AdminApi.go(AdminApi.PAGES.signIn);
      });
    }
    form.addEventListener("submit", function (event) {
      event.preventDefault();
      var code = AdminDom.el("code").value.trim();
      var error = /^\d{6}$/.test(code) ? "" : AdminCopy.codeRequired;
      AdminDom.setInvalid(AdminDom.el("code"), AdminDom.el("code-error"), error);
      if (error) return;
      completeStep2(form, { code: code });
    });
  }

  function initSignInRecovery() {
    var form = AdminDom.el("recovery-form");
    if (!form) return;
    bindDoubleSubmit(form);
    focusFirstField();
    var back = AdminDom.el("back-to-signin");
    if (back) {
      back.addEventListener("click", function (event) {
        event.preventDefault();
        AdminApi.go(AdminApi.PAGES.signIn);
      });
    }
    form.addEventListener("submit", function (event) {
      event.preventDefault();
      var raw = AdminDom.el("recovery-code").value;
      var error = raw.trim() ? "" : AdminCopy.recoveryRequired;
      AdminDom.setInvalid(AdminDom.el("recovery-code"), AdminDom.el("recovery-error"), error);
      if (error) return;
      completeStep2(form, { recoveryCode: normalizeRecovery(raw) });
    });
  }

  function bindPasswordPair(form, options) {
    AdminPassword.bindToggle("new-password", "new-password-toggle");
    AdminPassword.bindToggle("confirm-password", "confirm-password-toggle");
    var helper = AdminDom.el("password-helper");
    if (helper) AdminDom.setText(helper, AdminCopy.passwordHelper);
    function checkLength() {
      var message = AdminPassword.lengthMessage(AdminDom.el("new-password").value);
      AdminDom.setInvalid(AdminDom.el("new-password"), AdminDom.el("new-password-error"), message);
      return message;
    }
    AdminDom.el("new-password").addEventListener("blur", checkLength);
    form.addEventListener("submit", function (event) {
      event.preventDefault();
      if (busy) return;
      hideAlert();
      var lengthError = checkLength();
      var a = AdminDom.el("new-password").value;
      var b = AdminDom.el("confirm-password").value;
      var mismatch = a === b ? "" : AdminCopy.passwordMismatch;
      AdminDom.setInvalid(AdminDom.el("confirm-password"), AdminDom.el("confirm-password-error"), mismatch);
      if (lengthError || mismatch) {
        AdminDom.el("new-password").focus();
        return;
      }
      if (options.requireTurnstile && (AdminTurnstile.state() !== "ready" || !AdminTurnstile.token()))
        return;
      options.submit(a, b);
    });
  }

  function applyPasswordReason(result) {
    var named = result.payload && AdminPassword.namedServerReason(result.payload.reason);
    if (named) {
      AdminDom.setInvalid(AdminDom.el("new-password"), AdminDom.el("new-password-error"), named);
      AdminDom.el("new-password").focus();
      return;
    }
    showAlert(AdminApi.readError(result.payload, AdminCopy.signInFailure));
  }

  function initBootstrap() {
    var form = AdminDom.el("bootstrap-form");
    if (!form) return;
    bindDoubleSubmit(form);
    holdSubmitUntilTurnstile(form);
    focusFirstField();
    var token = AdminApi.queryToken();
    if (!token) {
      AdminApi.go(AdminApi.PAGES.linkExpired + "?variant=bootstrap");
      return;
    }
    bindPasswordPair(form, {
      requireTurnstile: true,
      submit: function (password, confirmPassword) {
        setBusy(form, true);
        AdminApi.bootstrapSet({
          token: token,
          password: password,
          confirmPassword: confirmPassword,
          turnstileToken: AdminTurnstile.token()
        }).then(function (result) {
          setBusy(form, false);
          if (result.ok) {
            AdminApi.go(AdminApi.PAGES.authenticator);
            return;
          }
          if (result.payload && result.payload.next === "link-expired") {
            AdminApi.go(AdminApi.PAGES.linkExpired + "?variant=bootstrap");
            return;
          }
          applyPasswordReason(result);
        }, function () {
          setBusy(form, false);
          showAlert(AdminCopy.signInFailure);
        });
      }
    });
  }

  function initLinkExpired() {
    var variant = AdminApi.queryParam("variant") === "bootstrap" ? "bootstrap" : "reset";
    AdminDom.setText(AdminDom.el("expired-lede"), AdminCopy.linkExpired);
    var resetLink = AdminDom.el("request-new-link");
    var bootstrapNote = AdminDom.el("bootstrap-note");
    if (variant === "reset") {
      AdminDom.show(resetLink, true);
      AdminDom.show(bootstrapNote, false);
    } else {
      AdminDom.show(resetLink, false);
      AdminDom.setText(bootstrapNote, AdminCopy.linkExpiredBootstrap);
      AdminDom.show(bootstrapNote, true);
    }
  }

  function initAuthenticator() {
    var form = AdminDom.el("totp-form");
    if (!form) return;
    bindDoubleSubmit(form);
    focusFirstField();
    AdminApi.totpEnroll().then(function (result) {
      if (!result.ok || !result.payload) {
        showAlert(AdminApi.readError(result.payload, AdminCopy.totpWrong));
        return;
      }
      var secret = result.payload.secret || "";
      var grouped = secret.replace(/(.{4})/g, "$1 ").trim();
      AdminDom.setText(AdminDom.el("manual-key"), grouped);
      var img = AdminDom.el("totp-qr");
      if (img && result.payload.qrPngBase64) {
        img.setAttribute("src", "data:image/png;base64," + result.payload.qrPngBase64);
      }
      var copyBtn = AdminDom.el("copy-key");
      if (copyBtn && navigator.clipboard) {
        copyBtn.addEventListener("click", function () {
          navigator.clipboard.writeText(secret).then(function () {
            AdminDom.setText(AdminDom.el("copy-live"), AdminCopy.copied);
          });
        });
      }
    });
    form.addEventListener("submit", function (event) {
      event.preventDefault();
      if (busy) return;
      var code = AdminDom.el("code").value.trim();
      var error = /^\d{6}$/.test(code) ? "" : AdminCopy.codeRequired;
      AdminDom.setInvalid(AdminDom.el("code"), AdminDom.el("code-error"), error);
      if (error) return;
      setBusy(form, true);
      AdminApi.totpEnrollConfirm({ code: code }).then(function (result) {
        setBusy(form, false);
        if (result.ok && result.payload && result.payload.recoveryCodes) {
          try {
            sessionStorage.setItem("dw-admin-recovery-once", JSON.stringify(result.payload.recoveryCodes));
          } catch (e) { /* ignore */ }
          AdminApi.go(AdminApi.PAGES.recoveryCodes);
          return;
        }
        AdminDom.setInvalid(AdminDom.el("code"), AdminDom.el("code-error"), AdminCopy.totpWrong);
      }, function () {
        setBusy(form, false);
        AdminDom.setInvalid(AdminDom.el("code"), AdminDom.el("code-error"), AdminCopy.totpWrong);
      });
    });
  }

  function initRecoveryCodes() {
    var list = [];
    try {
      list = JSON.parse(sessionStorage.getItem("dw-admin-recovery-once") || "[]");
    } catch (e) {
      list = [];
    }
    if (!list.length) {
      AdminApi.go(AdminApi.PAGES.stats);
      return;
    }
    var box = AdminDom.el("codes");
    AdminDom.setText(box, list.join("\n"));
    var confirm = AdminDom.el("codes-saved");
    var cont = AdminDom.el("continue");
    function sync() {
      if (cont) cont.disabled = !(confirm && confirm.checked);
    }
    if (confirm) confirm.addEventListener("change", sync);
    sync();
    var copyAll = AdminDom.el("copy-all");
    if (copyAll && navigator.clipboard) {
      copyAll.addEventListener("click", function () {
        navigator.clipboard.writeText(list.join("\n")).then(function () {
          AdminDom.setText(AdminDom.el("copy-live"), AdminCopy.copied);
        });
      });
    }
    var download = AdminDom.el("download-codes");
    if (download) {
      download.addEventListener("click", function () {
        var blob = new Blob([list.join("\n")], { type: "text/plain" });
        var url = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = url;
        a.download = "dealoware-admin-recovery-codes.txt";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        AdminDom.setText(AdminDom.el("copy-live"), AdminCopy.downloaded);
      });
    }
    if (cont) {
      cont.addEventListener("click", function () {
        if (!confirm.checked) return;
        try { sessionStorage.removeItem("dw-admin-recovery-once"); } catch (e) { /* ignore */ }
        AdminApi.go(AdminApi.PAGES.stats);
      });
    }
  }

  function initReset() {
    var form = AdminDom.el("reset-form");
    if (!form) return;
    bindDoubleSubmit(form);
    holdSubmitUntilTurnstile(form);
    focusFirstField();
    form.addEventListener("submit", function (event) {
      event.preventDefault();
      if (busy) return;
      hideAlert();
      var email = AdminDom.el("email").value.trim();
      var emailError = !email ? AdminCopy.emailRequired : (emailWellFormed(email) ? "" : AdminCopy.emailInvalid);
      AdminDom.setInvalid(AdminDom.el("email"), AdminDom.el("email-error"), emailError);
      if (emailError) return;
      if (AdminTurnstile.state() !== "ready" || !AdminTurnstile.token()) return;
      setBusy(form, true);
      AdminApi.resetRequest({
        email: email,
        turnstileToken: AdminTurnstile.token()
      }).then(function (result) {
        setBusy(form, false);
        if (result.status === 429) {
          showAlert(AdminApi.readError(result.payload, AdminCopy.signInFailure));
          AdminTurnstile.retry();
          return;
        }
        AdminApi.go(AdminApi.PAGES.resetSent);
      }, function () {
        setBusy(form, false);
        AdminApi.go(AdminApi.PAGES.resetSent);
      });
    });
  }

  function initResetCheckEmail() {
    AdminDom.setText(AdminDom.el("reset-neutral"), AdminCopy.resetNeutral);
  }

  function initResetPassword() {
    var form = AdminDom.el("reset-password-form");
    if (!form) return;
    bindDoubleSubmit(form);
    focusFirstField();
    var token = AdminApi.queryToken();
    if (!token) {
      AdminApi.go(AdminApi.PAGES.linkExpired + "?variant=reset");
      return;
    }
    var mode = "totp";
    var switchLink = AdminDom.el("use-recovery");
    function setMode(next) {
      mode = next;
      AdminDom.show(AdminDom.el("totp-field"), mode === "totp");
      AdminDom.show(AdminDom.el("recovery-field"), mode === "recovery");
      AdminDom.setText(switchLink, mode === "totp" ? "Use a recovery code instead" : "Use an authenticator code instead");
    }
    if (switchLink) {
      switchLink.addEventListener("click", function (event) {
        event.preventDefault();
        setMode(mode === "totp" ? "recovery" : "totp");
      });
    }
    bindPasswordPair(form, {
      requireTurnstile: false,
      submit: function (password, confirmPassword) {
        var body = {
          token: token,
          password: password,
          confirmPassword: confirmPassword
        };
        if (mode === "totp") {
          var code = AdminDom.el("code").value.trim();
          var codeError = /^\d{6}$/.test(code) ? "" : AdminCopy.codeRequired;
          AdminDom.setInvalid(AdminDom.el("code"), AdminDom.el("code-error"), codeError);
          if (codeError) return;
          body.code = code;
        } else {
          var recovery = AdminDom.el("recovery-code").value;
          var recError = recovery.trim() ? "" : AdminCopy.recoveryRequired;
          AdminDom.setInvalid(AdminDom.el("recovery-code"), AdminDom.el("recovery-error"), recError);
          if (recError) return;
          body.recoveryCode = normalizeRecovery(recovery);
        }
        setBusy(form, true);
        AdminApi.resetComplete(body).then(function (result) {
          setBusy(form, false);
          if (result.ok) {
            AdminApi.go(AdminApi.PAGES.signIn + "?status=password-set");
            return;
          }
          if (result.payload && result.payload.next === "link-expired") {
            AdminApi.go(AdminApi.PAGES.linkExpired + "?variant=reset");
            return;
          }
          if (result.status === 401) {
            showSignInFailure();
            var codeInput = AdminDom.el("code");
            var recInput = AdminDom.el("recovery-code");
            if (codeInput) codeInput.value = "";
            if (recInput) recInput.value = "";
            return;
          }
          applyPasswordReason(result);
        }, function () {
          setBusy(form, false);
          showSignInFailure();
        });
      }
    });
  }

  function initSessionEnded() {
    AdminDom.setText(AdminDom.el("ended-lede"), AdminCopy.sessionEnded);
  }

  function initHome() {
    var remaining = AdminApi.queryParam("recoveryRemaining");
    var banner = AdminDom.el("recovery-banner");
    if (banner && remaining) {
      AdminDom.setText(
        AdminDom.el("recovery-banner-text"),
        AdminCopy.recoveryBannerPrefix + remaining + AdminCopy.recoveryBannerSuffix
      );
      AdminDom.show(banner, true);
      var manage = AdminDom.el("manage-recovery-codes");
      AdminDom.show(manage, true);
      var dismiss = AdminDom.el("dismiss-banner");
      if (dismiss) {
        dismiss.addEventListener("click", function () {
          AdminDom.show(banner, false);
        });
      }
    }
  }

  function showStepUpStatus(message) {
    var status = AdminDom.el("stepup-status");
    AdminDom.setText(status, message || "");
  }

  function initSecuritySettings() {
    var form = AdminDom.el("change-password-form");
    if (!form) return;
    bindDoubleSubmit(form);
    AdminPassword.bindToggle("current-password", "current-password-toggle");
    AdminPassword.bindToggle("new-password", "new-password-toggle");
    AdminPassword.bindToggle("confirm-password", "confirm-password-toggle");
    var helper = AdminDom.el("password-helper");
    if (helper) AdminDom.setText(helper, AdminCopy.passwordHelper);
    focusFirstField();

    AdminApi.me().then(function (result) {
      var count = result.payload && result.payload.remainingRecoveryCodes;
      if (typeof count === "number")
        AdminDom.setText(AdminDom.el("recovery-n"), String(count));
    });

    var signOut = AdminDom.el("sign-out");
    if (signOut) {
      signOut.addEventListener("click", function () {
        AdminApi.signInCancel().then(function () {
          AdminApi.go(AdminApi.PAGES.signIn + "?status=signed-out");
        });
      });
    }

    var mode = "totp";
    var switchLink = AdminDom.el("use-recovery");
    function setMode(next) {
      mode = next;
      AdminDom.show(AdminDom.el("totp-field"), mode === "totp");
      AdminDom.show(AdminDom.el("recovery-field"), mode === "recovery");
      if (switchLink)
        AdminDom.setText(switchLink, mode === "totp" ? "Use a recovery code instead" : "Use an authenticator code instead");
    }
    if (switchLink) {
      switchLink.addEventListener("click", function (event) {
        event.preventDefault();
        setMode(mode === "totp" ? "recovery" : "totp");
      });
    }

    function checkLength() {
      var message = AdminPassword.lengthMessage(AdminDom.el("new-password").value);
      AdminDom.setInvalid(AdminDom.el("new-password"), AdminDom.el("new-password-error"), message);
      return message;
    }

    function checkMismatch() {
      var a = AdminDom.el("new-password").value;
      var b = AdminDom.el("confirm-password").value;
      var mismatch = b && a !== b ? AdminCopy.passwordMismatch : "";
      AdminDom.setInvalid(AdminDom.el("confirm-password"), AdminDom.el("confirm-password-error"), mismatch);
      return mismatch;
    }

    AdminDom.el("new-password").addEventListener("blur", checkLength);
    AdminDom.el("confirm-password").addEventListener("blur", checkMismatch);

    form.addEventListener("submit", function (event) {
      event.preventDefault();
      if (busy) return;
      showStepUpStatus("");
      var current = AdminDom.el("current-password").value;
      var currentError = current ? "" : AdminCopy.passwordRequired;
      AdminDom.setInvalid(AdminDom.el("current-password"), AdminDom.el("current-password-error"), currentError);
      var lengthError = checkLength();
      var mismatch = checkMismatch();
      var body = {
        currentPassword: current,
        newPassword: AdminDom.el("new-password").value,
        confirmPassword: AdminDom.el("confirm-password").value
      };
      if (mode === "totp") {
        var code = AdminDom.el("code").value.trim();
        var codeError = /^\d{6}$/.test(code) ? "" : AdminCopy.codeRequired;
        AdminDom.setInvalid(AdminDom.el("code"), AdminDom.el("code-error"), codeError);
        if (codeError || currentError || lengthError || mismatch) {
          if (lengthError || mismatch) AdminDom.el("new-password").focus();
          return;
        }
        body.code = code;
      } else {
        var recovery = AdminDom.el("recovery-code").value;
        var recError = recovery.trim() ? "" : AdminCopy.recoveryRequired;
        AdminDom.setInvalid(AdminDom.el("recovery-code"), AdminDom.el("recovery-error"), recError);
        if (recError || currentError || lengthError || mismatch) {
          if (lengthError || mismatch) AdminDom.el("new-password").focus();
          return;
        }
        body.recoveryCode = normalizeRecovery(recovery);
      }
      setBusy(form, true);
      AdminApi.changePassword(body).then(function (result) {
        setBusy(form, false);
        if (result.ok) {
          AdminApi.go(AdminApi.PAGES.signIn + "?status=password-changed");
          return;
        }
        if (result.payload && result.payload.signedOut) {
          AdminApi.go(AdminApi.PAGES.signIn + "?status=auth-failed");
          return;
        }
        var named = result.payload && AdminPassword.namedServerReason(result.payload.reason);
        if (named) {
          showStepUpStatus("");
          AdminDom.setInvalid(AdminDom.el("new-password"), AdminDom.el("new-password-error"), named);
          AdminDom.el("new-password").focus();
          return;
        }
        showStepUpStatus(AdminCopy.stepUpFailure);
        AdminDom.el("current-password").value = "";
        var codeInput = AdminDom.el("code");
        var recInput = AdminDom.el("recovery-code");
        if (codeInput) codeInput.value = "";
        if (recInput) recInput.value = "";
        AdminDom.el("current-password").focus();
      }, function () {
        setBusy(form, false);
        showStepUpStatus(AdminCopy.stepUpFailure);
        AdminDom.el("current-password").value = "";
        AdminDom.el("current-password").focus();
      });
    });
  }

  var screen = document.body.getAttribute("data-screen");
  var starters = {
    "S-A1": initSignIn,
    "S-A2": initSignInCode,
    "S-A3": initSignInRecovery,
    "S-A4": initBootstrap,
    "S-A5": initLinkExpired,
    "S-A6": initAuthenticator,
    "S-A7": initRecoveryCodes,
    "S-A8": initReset,
    "S-A9": initResetCheckEmail,
    "S-A10": initResetPassword,
    "S-A11": initSessionEnded,
    "S-A12": initSecuritySettings,
    "home": initHome
  };
  if (starters[screen]) starters[screen]();
})(window);
