/**
 * Safe DOM helpers. Never assign data to innerHTML.
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

  function setInvalid(input, errorNode, message) {
    if (!input) return;
    if (message) {
      input.setAttribute("aria-invalid", "true");
      setText(errorNode, message);
      show(errorNode, true);
    } else {
      input.removeAttribute("aria-invalid");
      setText(errorNode, "");
      show(errorNode, false);
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
