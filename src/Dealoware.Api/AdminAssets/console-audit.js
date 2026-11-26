/**
 * The audit log list (S-D1, AdminAuditPages). Served only with a valid
 * session.
 *
 * The page is written by the server from its address, and works without this
 * script: the filters are a GET form, the chips and the pages plain links.
 * This script makes it quicker to use, and never draws a row itself:
 *
 *   - An action checkbox, the entity type or the page size asks for the new
 *     list as soon as it changes. The two dates go with Enter or
 *     "Apply filters". Any of these starts again at page 1.
 *   - Each one is a move to a new address on this page, with the control
 *     that made it named after "#focus=". The new page puts the focus back on
 *     that control (or on the page's main area when it is gone), says the
 *     number of results in the page's one live region, and takes the mark
 *     off the address. Until the new page arrives the table is marked busy
 *     and keeps its rows. A page shown again (pageshow, the back/forward
 *     cache included) is not busy and takes a move again.
 *   - The address is set to the page's own reading of it (data-canonical),
 *     so a value the page dropped does not stay in the address bar.
 *
 * It asks the server for nothing but the page the owner moved to. Nothing is
 * stored. Text goes into the page through textContent only. Its one timer
 * runs once, after a move, so that screen readers hear the count.
 */
(function () {
  "use strict";

  var FOCUS_MARK = "#focus=";

  var form = document.getElementById("list-form");
  var region = document.getElementById("list-region");
  var live = document.getElementById("console-status");
  if (!form) return;

  var path = form.getAttribute("action") || location.pathname;

  // ---- after a move: the address, the focus, the count ----

  var canonical = form.getAttribute("data-canonical");
  var mark = location.hash.indexOf(FOCUS_MARK) === 0 ? location.hash.slice(FOCUS_MARK.length) : null;
  if (canonical && (canonical !== location.pathname + location.search || mark !== null)) {
    history.replaceState(null, "", canonical);
  }

  if (mark !== null) {
    var target = /^[A-Za-z][\w-]*$/.test(mark) ? document.getElementById(mark) : null;
    if (!target) target = document.getElementById("main");
    if (target) target.focus();
    var count = region ? region.getAttribute("data-announce") : null;
    // A moment after load, so that screen readers hear the change.
    if (count && live) setTimeout(function () { live.textContent = count; }, 100);
  }

  // ---- moving to a new address ----

  var leaving = false;

  // A page the browser brings back whole (back/forward cache), or a move that never
  // arrived, must not stay busy: the controls work again.
  window.addEventListener("pageshow", function () {
    leaving = false;
    if (region) region.removeAttribute("aria-busy");
  });

  function go(query, focusId) {
    var next = path + query;
    if (next === location.pathname + location.search) {
      if (focusId) {
        var here = document.getElementById(focusId);
        if (here) here.focus();
      }
      return;
    }
    if (leaving) return;
    leaving = true;
    if (region) region.setAttribute("aria-busy", "true");
    location.assign(next + (focusId ? FOCUS_MARK + focusId : ""));
  }

  /** The query the form asks for: empty values left out, the ticked actions joined, back to page 1. */
  function queryOf(source) {
    var names = [];
    var values = {};
    var fields = source.querySelectorAll("input[name], select[name]");
    for (var i = 0; i < fields.length; i += 1) {
      var field = fields[i];
      if (field.type === "checkbox" && !field.checked) continue;
      var value = field.value.trim();
      // Page 1, and the default page size, are what an address without them means.
      if (value === "" || field.name === "offset" || (field.name === "limit" && value === "50")) continue;
      if (!Object.prototype.hasOwnProperty.call(values, field.name)) {
        names.push(field.name);
        values[field.name] = [];
      }
      values[field.name].push(value);
    }
    var params = new URLSearchParams();
    for (var n = 0; n < names.length; n += 1) params.set(names[n], values[names[n]].join(","));
    var text = params.toString();
    return text ? "?" + text : "";
  }

  form.addEventListener("submit", function (event) {
    event.preventDefault();
    // Enter in a box sends the form as if by its button: the focus stays in the box.
    var active = document.activeElement;
    var by = active && active.id && form.contains(active) && active.tagName !== "BUTTON"
      ? active.id
      : event.submitter && event.submitter.id;
    go(queryOf(form), by || "apply-filters");
  });

  form.addEventListener("change", function (event) {
    var field = event.target;
    if (field && field.hasAttribute("data-auto")) go(queryOf(form), field.id);
  });

  // Chips, Clear all, Clear filters and the pages: links that say where the focus goes.
  document.addEventListener("click", function (event) {
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    var link = event.target.closest ? event.target.closest("a[data-focus]") : null;
    if (!link) return;
    var url = new URL(link.href, location.href);
    if (url.origin !== location.origin || url.pathname !== path) return;
    event.preventDefault();
    go(url.search, link.getAttribute("data-focus"));
  });
})();
