/**
 * The copy buttons of the signed-out admin pages.
 *
 * copy(text, node) puts a text on the clipboard. Call it from inside the
 * click handler of the button: browsers allow a copy only while they are
 * handling something the reader did.
 *
 * It asks the browser's asynchronous clipboard first. Where that is missing
 * or refuses, it selects the contents of `node` (the element on the page
 * that shows the same text) and asks for the selection to be copied, the
 * older way. If that fails too, the text stays selected, so the reader's own
 * copy command works on it.
 *
 * It resolves with true when the text is on the clipboard and with false
 * when it is only selected. It never rejects. The text goes nowhere else:
 * nothing is stored, sent or logged.
 */
(function (global) {
  "use strict";

  function select(node) {
    if (!node || typeof global.getSelection !== "function") return false;
    var selection = global.getSelection();
    if (!selection) return false;
    var range = document.createRange();
    range.selectNodeContents(node);
    selection.removeAllRanges();
    selection.addRange(range);
    return true;
  }

  function copySelection(node) {
    if (!select(node)) return false;
    var copied = false;
    try {
      copied = document.execCommand("copy") === true;
    } catch (e) {
      copied = false;
    }
    // A selection that was only a means is put away again. One the reader
    // now has to copy by hand stays.
    if (copied) global.getSelection().removeAllRanges();
    return copied;
  }

  function copy(text, node) {
    var asked = null;
    try {
      var clipboard = global.navigator.clipboard;
      if (clipboard && typeof clipboard.writeText === "function") asked = clipboard.writeText(text);
    } catch (e) {
      asked = null;
    }
    if (!asked || typeof asked.then !== "function") return Promise.resolve(copySelection(node));
    return asked.then(function () {
      return true;
    }, function () {
      return copySelection(node);
    });
  }

  global.AdminClipboard = {
    copy: copy
  };
})(window);
