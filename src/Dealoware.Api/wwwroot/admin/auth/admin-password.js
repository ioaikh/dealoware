/**
 * New-password helpers for the signed-out admin pages.
 * The browser checks length only, counted the way the server counts it
 * (code points after NFKC). The limits and both messages are the server's:
 * the page passes them in as data attributes on the field. Every other rule
 * (common words, account words) is decided by the server alone.
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

  function rulesFor(input) {
    return {
      min: parseInt(input.getAttribute("data-min-length"), 10),
      max: parseInt(input.getAttribute("data-max-length"), 10),
      tooShort: input.getAttribute("data-too-short") || "",
      tooLong: input.getAttribute("data-too-long") || ""
    };
  }

  /** The server's message for a value that is too short or too long, or "". */
  function lengthMessage(input) {
    if (!input) return "";
    var rules = rulesFor(input);
    var n = length(input.value);
    if (!isNaN(rules.min) && n < rules.min) return rules.tooShort;
    if (!isNaN(rules.max) && n > rules.max) return rules.tooLong;
    return "";
  }

  /**
   * The show toggle for one password field. Its label and name never change:
   * aria-pressed alone says whether the password is shown, so a screen reader
   * hears "Show New password, pressed", never "Hide, pressed".
   */
  function bindToggle(inputId, buttonId) {
    var input = AdminDom.el(inputId);
    var button = AdminDom.el(buttonId);
    if (!input || !button) return;
    button.addEventListener("click", function () {
      var reveal = input.getAttribute("type") === "password";
      input.setAttribute("type", reveal ? "text" : "password");
      button.setAttribute("aria-pressed", reveal ? "true" : "false");
    });
  }

  global.AdminPassword = {
    nfkc: nfkc,
    length: length,
    lengthMessage: lengthMessage,
    bindToggle: bindToggle
  };
})(window);
