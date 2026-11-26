/**
 * Safe DOM helpers for the signed-out admin pages.
 * Text goes in through textContent only, never as markup.
 */
(function (global) {
  "use strict";

  function el(id) {
    return document.getElementById(id);
  }

  function setText(node, value) {
    if (!node) return;
    node.textContent = value == null ? "" : String(value);
  }

  function clear(node) {
    if (!node) return;
    while (node.firstChild) node.removeChild(node.firstChild);
  }

  function show(node, on) {
    if (!node) return;
    node.hidden = !on;
  }

  /**
   * Marks a field invalid and fills its error line, or clears both.
   * The error line stays in the page (empty when there is no error) and the
   * field points at it with aria-describedby, so the text is announced.
   */
  function setInvalid(input, errorNode, message) {
    if (!input) return;
    if (message) {
      input.setAttribute("aria-invalid", "true");
      setText(errorNode, message);
    } else {
      input.removeAttribute("aria-invalid");
      setText(errorNode, "");
    }
  }

  global.AdminDom = {
    el: el,
    setText: setText,
    clear: clear,
    show: show,
    setInvalid: setInvalid
  };
})(window);
