/**
 * The list pages (S-B2 to S-B5, AdminListPages). Served only with a valid
 * session.
 *
 * The page is written by the server from its address, and works without this
 * script: the filters are a GET form, the headers small GET forms, the chips
 * and the pages plain links. This script makes it quicker to use, and never
 * draws a row itself:
 *
 *   - A checkbox (a status, "Show deleted") or the page size asks for the
 *     new list as soon as it changes. Typed filters go with Enter or
 *     "Apply filters". The search goes after a 300 ms pause in typing, or at
 *     once with Enter (TD-ADM-UI-na-B07). Any of these starts again at page 1.
 *   - Each one is a move to a new address on this page, with the control
 *     that made it named after "#focus=". The new page puts the focus back on
 *     that control (or on the page's main area when it is gone), says the
 *     number of results in the page's one live region, and takes the mark
 *     off the address. Until the new page arrives the table is marked busy
 *     and keeps its rows (TD-ADM-UI-na-X04). A page shown again (pageshow,
 *     the back/forward cache included) is not busy and takes a move again.
 *   - The address is set to the page's own reading of it (data-canonical),
 *     so a value the page dropped does not stay in the address bar.
 *   - The two pickers (Participant or Owner participant, and Artifact) are
 *     comboboxes: after a 300 ms pause they ask the read API for one page of
 *     matches for the typed text (at most ten, never the whole table), and
 *     list them; arrows move, Enter or a click chooses, Escape closes.
 *     Typing clears the choice: Enter on typed text that was not chosen
 *     sends no value for that filter.
 *     A 401 from the session gate goes to sign-in, as any page would.
 *
 * Nothing is stored. Text goes into the page through textContent only. The
 * one-shot timers wait for typing to pause; none repeats, and none asks the
 * server for anything the owner did not ask for.
 */
(function () {
  "use strict";

  var SESSION_ENDED = "/admin/sign-in#ended";
  var GATE_DENIED = "Unauthorized";
  var FOCUS_MARK = "#focus=";
  var PAUSE_MS = 300;
  var MATCHES = 10;

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
    if (target) {
      target.focus();
      if (target.id === "q" && typeof target.setSelectionRange === "function") {
        var end = target.value.length;
        try { target.setSelectionRange(end, end); } catch (e) { /* not a text box */ }
      }
    }
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

  /** The query a form asks for: empty values left out, statuses joined, back to page 1. */
  function queryOf(source) {
    var params = new URLSearchParams();
    var statuses = [];
    var fields = source.querySelectorAll("input[name], select[name]");
    for (var i = 0; i < fields.length; i += 1) {
      var field = fields[i];
      if ((field.type === "checkbox" || field.type === "radio") && !field.checked) continue;
      var value = field.value.trim();
      // Page 1, and the default page size, are what an address without them means.
      if (value === "" || field.name === "offset" || (field.name === "limit" && value === "50")) continue;
      if (field.name === "status") statuses.push(value);
      else params.set(field.name, value);
    }
    if (statuses.length) params.set("status", statuses.join(","));
    var text = params.toString();
    return text ? "?" + text : "";
  }

  function submitFrom(source, focusId) {
    go(queryOf(source), focusId);
  }

  form.addEventListener("submit", function (event) {
    event.preventDefault();
    clearTimeout(searchTimer);
    // Enter in a box sends the form as if by its button: the focus stays in the box.
    var active = document.activeElement;
    var by = active && active.id && form.contains(active) && active.tagName !== "BUTTON"
      ? active.id
      : event.submitter && event.submitter.id;
    submitFrom(form, by || "apply-filters");
  });

  form.addEventListener("change", function (event) {
    var field = event.target;
    if (field && field.hasAttribute("data-auto")) submitFrom(form, field.id);
  });

  var sortForms = document.querySelectorAll("form.sort-form");
  for (var s = 0; s < sortForms.length; s += 1) {
    sortForms[s].addEventListener("submit", function (event) {
      event.preventDefault();
      var button = event.currentTarget.querySelector("button");
      submitFrom(event.currentTarget, button ? button.id : null);
    });
  }

  // Chips, Clear, Clear all, Clear filters and the pages: links that say where the focus goes.
  document.addEventListener("click", function (event) {
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    var link = event.target.closest ? event.target.closest("a[data-focus]") : null;
    if (!link) return;
    var url = new URL(link.href, location.href);
    if (url.origin !== location.origin || url.pathname !== path) return;
    event.preventDefault();
    go(url.search, link.getAttribute("data-focus"));
  });

  // ---- the search: one request after a pause, or at once with Enter ----

  var search = document.getElementById("q");
  var searchTimer = null;
  if (search) {
    search.addEventListener("input", function () {
      clearTimeout(searchTimer);
      searchTimer = setTimeout(function () { submitFrom(form, "q"); }, PAUSE_MS);
    });
  }

  // ---- the pickers ----

  function Picker(root) {
    var box = root.querySelector("input[role=combobox]");
    var hidden = root.querySelector("input[type=hidden]");
    var list = root.querySelector("[role=listbox]");
    var source = root.getAttribute("data-source");
    var valueKey = root.getAttribute("data-value-key");
    var labelKey = root.getAttribute("data-label-key");
    var none = root.getAttribute("data-no-matches") || "";
    if (!box || !hidden || !list || !source) return;

    var timer = null;
    var asked = 0;
    var active = -1;

    function options() {
      return list.querySelectorAll("li[data-value]");
    }

    function close() {
      list.hidden = true;
      box.setAttribute("aria-expanded", "false");
      box.removeAttribute("aria-activedescendant");
      active = -1;
    }

    function clear() {
      while (list.firstChild) list.removeChild(list.firstChild);
    }

    function highlight(index) {
      var all = options();
      for (var i = 0; i < all.length; i += 1) {
        all[i].classList.toggle("active", i === index);
        all[i].setAttribute("aria-selected", i === index ? "true" : "false");
      }
      active = index;
      if (index >= 0 && all[index]) {
        box.setAttribute("aria-activedescendant", all[index].id);
        if (all[index].scrollIntoView) all[index].scrollIntoView({ block: "nearest" });
      } else {
        box.removeAttribute("aria-activedescendant");
      }
    }

    function choose(option) {
      hidden.value = option.getAttribute("data-value");
      box.value = option.textContent;
      close();
      submitFrom(form, box.id);
    }

    function show(items) {
      clear();
      var shown = 0;
      for (var i = 0; i < items.length && shown < MATCHES; i += 1) {
        var item = items[i] || {};
        var value = AdminRequest.text(item[valueKey]);
        if (!value) continue;
        var option = document.createElement("li");
        option.id = list.id + "-" + shown;
        option.setAttribute("role", "option");
        option.setAttribute("aria-selected", "false");
        option.setAttribute("data-value", value);
        option.textContent = AdminRequest.text(item[labelKey]) || value;
        list.appendChild(option);
        shown += 1;
      }
      if (shown === 0) {
        var empty = document.createElement("li");
        empty.setAttribute("role", "option");
        empty.setAttribute("aria-disabled", "true");
        empty.setAttribute("aria-selected", "false");
        empty.textContent = none;
        list.appendChild(empty);
      }
      list.hidden = false;
      box.setAttribute("aria-expanded", "true");
      active = -1;
    }

    function ask() {
      var text = box.value.trim();
      if (!text || typeof AdminRequest === "undefined") {
        close();
        return;
      }
      var ticket = ++asked;
      var query = "?q=" + encodeURIComponent(text) + "&limit=" + MATCHES + "&sort=name&dir=asc";
      AdminRequest.get(source + query).then(function (reply) {
        if (ticket !== asked) return;
        var body = reply.body || {};
        if (reply.status === 401 && body.error === GATE_DENIED) {
          location.replace(SESSION_ENDED);
          return;
        }
        show(reply.status === 200 && Array.isArray(body.items) ? body.items : []);
      }, function () {
        if (ticket === asked) show([]);
      });
    }

    box.addEventListener("input", function () {
      // Typed text is not a choice: until an option is chosen, the form sends no value
      // for this filter, never the earlier choice under new words.
      hidden.value = "";
      clearTimeout(timer);
      timer = setTimeout(ask, PAUSE_MS);
    });

    box.addEventListener("keydown", function (event) {
      var all = options();
      if (event.key === "ArrowDown") {
        event.preventDefault();
        if (list.hidden) { ask(); return; }
        if (all.length) highlight(Math.min(active + 1, all.length - 1));
      } else if (event.key === "ArrowUp") {
        event.preventDefault();
        if (all.length) highlight(Math.max(active - 1, 0));
      } else if (event.key === "Enter") {
        if (!list.hidden && active >= 0 && all[active]) {
          event.preventDefault();
          choose(all[active]);
        }
        // Otherwise the form goes: an emptied box drops this filter.
      } else if (event.key === "Escape") {
        if (!list.hidden) {
          event.preventDefault();
          close();
        }
      }
    });

    box.addEventListener("blur", function () {
      close();
    });

    // A press on an option keeps the focus in the box, then chooses it.
    list.addEventListener("mousedown", function (event) {
      event.preventDefault();
    });
    list.addEventListener("click", function (event) {
      var option = event.target.closest ? event.target.closest("li[data-value]") : null;
      if (option) choose(option);
    });
  }

  var pickers = document.querySelectorAll(".picker");
  for (var p = 0; p < pickers.length; p += 1) Picker(pickers[p]);
})();
