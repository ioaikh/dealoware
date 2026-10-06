/**
 * Client password checks: length after NFKC only (password-rules note v2.3).
 * Blocklist / context-word / same-as-current stay on the server.
 */
(function (global) {
  "use strict";

  function nfkc(value) {
    if (!value) return "";
    return value.normalize ? value.normalize("NFKC") : value;
  }

  function length(value) {
    return Array.from(nfkc(value)).length;
  }

  function lengthMessage(value) {
    var n = length(value);
    if (n < 15) return AdminCopy.passwordTooShort;
    if (n > 128) return AdminCopy.passwordTooLong;
    return "";
  }

  function bindToggle(inputId, buttonId) {
    var input = AdminDom.el(inputId);
    var button = AdminDom.el(buttonId);
    if (!input || !button) return;
    button.addEventListener("click", function () {
      var hidden = input.getAttribute("type") === "password";
      input.setAttribute("type", hidden ? "text" : "password");
      button.setAttribute("aria-pressed", hidden ? "true" : "false");
      AdminDom.setText(button, hidden ? "Hide" : "Show");
    });
  }

  function namedServerReason(reason) {
    if (reason === "too_short") return AdminCopy.passwordTooShort;
    if (reason === "too_long") return AdminCopy.passwordTooLong;
    if (reason === "common") return AdminCopy.passwordCommon;
    if (reason === "same_as_current") return AdminCopy.passwordSameAsCurrent;
    if (reason === "mismatch") return AdminCopy.passwordMismatch;
    return "";
  }

  global.AdminPassword = {
    nfkc: nfkc,
    length: length,
    lengthMessage: lengthMessage,
    bindToggle: bindToggle,
    namedServerReason: namedServerReason
  };
})(window);
