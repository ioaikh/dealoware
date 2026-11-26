/**
 * Edit mode of the detail pages (S-B6, AdminEditPage; A7 Step 9, UXR-B14 to
 * B17). Served only with a valid session.
 *
 * The server writes the page as text, with every control it allows hidden:
 * the Edit button, the actions (Close and Expire on an open negotiation,
 * Cancel on an open offer) and, beside the text of each editable field, its
 * input. Without this script the page only reads.
 *
 * Edit is a mode of the same address. "Edit" shows the inputs, Save and
 * Cancel; Save stays disabled until a value differs from the page's and
 * while a request is out ("Saving…"), so a double click sends one request.
 * Save sends one PATCH to /admin/api/{type}/{id} with only the changed
 * fields, the page's edit token as If-Match (the ETag of the read the page
 * was written from) and the anti-forgery token in X-CSRF-TOKEN. Cancel puts
 * the page's values back and returns to the view.
 *
 * Replies (navigation only on these exact contracts):
 *   200 with this record's id -> the page loads itself anew, with a note in
 *       its history entry's state (never in the address, so a crafted address
 *       announces nothing); the new page shows the new values and says
 *       "Changes saved." (or "Negotiation {ID} expired." and the rest) in the
 *       shell's live region, once.
 *   400 with "fields" -> each sentence (the server's own, never a value)
 *       under its field and in the summary above, which takes the focus; the
 *       typed values stay.
 *   400 without -> "You can't do that. Nothing was changed."
 *   409 -> the server's sentence, typed values kept, and Reload, which loads
 *       the page anew (nothing stale is kept).
 *   401 from the session gate -> sign-in with the "session ended" note.
 *   403 -> the gate did not take this page's anti-forgery token: as the
 *       shell does, ask GET /admin/api/me; still signed in -> the page loads
 *       itself anew once with #fresh.
 *   Anything else, or no reply -> ask GET /admin/api/me: the session gone ->
 *       sign-in; otherwise the retry sentence.
 *
 * Actions open a modal confirm dialog (focus on its second button). The
 * dialog handles Escape itself, in a capture-phase keydown on the document
 * while it is open (an Escape that ends text composition is left alone), and
 * a press on its backdrop closes it too; neither sends anything. Its confirm
 * button shows "Expiring…" (or "Closing…", "Cancelling…") and is disabled
 * while the one request is out. A 400 keeps it open with "Nothing was
 * changed." and Retry; a 409 with the server's sentence and Reload. Closed,
 * the focus goes back to the button that opened it, if it is adrift.
 *
 * Unsaved changes. While a value differs, leaving asks "Discard your
 * changes?" in the page's own dialog (Discard, Keep editing): a link, Sign
 * out, Back (edit mode is a history entry of the same address, so Back
 * first leaves edit mode; Keep editing steps forward again), and picking
 * another subject entity over typed words. With nothing changed nothing is
 * asked. While a save is out nothing leaves. Every way out takes edit
 * mode's history entry off first, so one Back returns to the record.
 *
 * The controls start from the server's values (defaultValue, defaultChecked,
 * defaultSelected, the owner's data-initial), never from what a browser put
 * back on reload or Back; the form also has autocomplete="off".
 *
 * Nothing is stored. Text goes into the page through textContent only.
 */
(function () {
  "use strict";

  var SESSION_ENDED = "/admin/sign-in#ended";
  var ME = "/admin/api/me";
  var GATE_DENIED = "Unauthorized";
  var BAD_REQUEST = "BadRequest";
  var SIGNED_IN_AS = "CoreOwner";
  var FRESH = "#fresh";
  var NOTES = ["saved", "closed", "expired", "cancelled"];
  var TIMEOUT_MS = 15000;
  var PAUSE_MS = 300;
  var MATCHES = 10;

  var root = document.getElementById("record");
  var live = document.getElementById("console-status");
  var main = document.getElementById("main");
  if (!root) return;
  // A helper did not arrive: the page stays as the server wrote it, read-only.
  if (typeof AdminRequest === "undefined" || typeof AdminDom === "undefined") return;

  var el = AdminDom.el;
  var text = AdminRequest.text;
  var recordId = (root.getAttribute("data-id") || "").toLowerCase();
  var api = root.getAttribute("data-api") || "";
  var etag = root.getAttribute("data-etag") || "";
  var copy = {
    retry: root.getAttribute("data-retry") || "",
    refused: root.getAttribute("data-refused") || "",
    nothing: root.getAttribute("data-nothing") || "",
    retryLabel: root.getAttribute("data-retry-label") || "",
    conflict: root.getAttribute("data-conflict") || "",
    saving: root.getAttribute("data-saving") || "",
    save: root.getAttribute("data-save") || ""
  };

  // A page loaded anew (by a script over a refused token, or by the owner) does not do it again by itself.
  var navigation = typeof performance.getEntriesByType === "function" ? performance.getEntriesByType("navigation") : [];
  var mayReload = !(navigation.length > 0 && navigation[0].type === "reload");

  // The page is going away: an answer that arrives now changes nothing.
  var leaving = false;
  window.addEventListener("pagehide", function () { leaving = true; });
  window.addEventListener("pageshow", function () { leaving = false; });

  function say(message) {
    AdminDom.setText(live, message);
  }

  /* ---- Requests ---- */

  function csrfToken() {
    var meta = document.querySelector('meta[name="dw-csrf"]');
    return meta ? meta.getAttribute("content") || "" : "";
  }

  function parse(raw) {
    try {
      var body = JSON.parse(raw);
      return body && typeof body === "object" && !Array.isArray(body) ? body : null;
    } catch (e) {
      return null;
    }
  }

  /**
   * One PATCH of this record, as admin-request.js sends a POST: same origin,
   * no redirect followed, no cache, no mode set, 15 seconds at most.
   * Resolves with { status, body }; rejects when there was no reply.
   */
  function patch(changes) {
    var controller = new AbortController();
    var timer = setTimeout(function () { controller.abort(); }, TIMEOUT_MS);
    return fetch(api, {
      method: "PATCH",
      headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": csrfToken(), "If-Match": etag },
      body: JSON.stringify(changes),
      credentials: "same-origin",
      redirect: "error",
      cache: "no-store",
      signal: controller.signal
    }).then(function (r) {
      return r.text().then(function (raw) {
        clearTimeout(timer);
        return { status: r.status, body: parse(raw) };
      });
    }, function (err) {
      clearTimeout(timer);
      throw err;
    });
  }

  /**
   * The change was made: the page loads itself anew and then says so. The note rides in this
   * history entry's state, which only this script sets, never in the address: an address someone
   * crafted cannot make the page announce a change that did not happen.
   */
  function done(note) {
    guarded = false;
    leaveEditEntry(function () {
      history.replaceState({ dwNote: note }, "", location.pathname + location.search);
      location.reload();
    });
  }

  function signedOut(reply) {
    var body = reply.body || {};
    if (reply.status === 401 && body.error === GATE_DENIED) {
      guarded = false;
      location.replace(SESSION_ENDED);
      return true;
    }
    return false;
  }

  /** The gate refused this page's anti-forgery token: load the page anew once, if still signed in. */
  function afterTokenRefused(fail) {
    AdminRequest.get(ME).then(function (reply) {
      if (leaving || signedOut(reply)) return;
      var body = reply.body || {};
      if (mayReload && reply.status === 200 && body.principal === SIGNED_IN_AS) {
        guarded = false;
        leaveEditEntry(function () {
          history.replaceState(null, "", location.pathname + location.search + FRESH);
          location.reload();
        });
      } else {
        fail(copy.retry);
      }
    }, function () {
      if (!leaving) fail(copy.retry);
    });
  }

  /** No clear answer: it may or may not have gone through. A session that has gone means sign-in. */
  function afterUnclearReply(fail) {
    AdminRequest.get(ME).then(function (reply) {
      if (leaving || signedOut(reply)) return;
      fail(copy.retry);
    }, function () {
      if (!leaving) fail(copy.retry);
    });
  }

  function isThisRecord(reply) {
    return reply.status === 200 && !!reply.body && text(reply.body.id).toLowerCase() === recordId && recordId !== "";
  }

  /* ---- Dialogs ---- */

  var busy = false;
  var dialogs = [];

  /** The focus is where nobody put it: on nothing, on the page itself, or still in a closed dialog. */
  function focusAdrift(dialog) {
    var active = document.activeElement;
    return !active || active === document.body || active === document.documentElement || dialog.contains(active);
  }

  /**
   * A modal dialog of this page. Closed by Escape, its backdrop or its own
   * buttons, never while its request is out; then the focus goes back to its
   * opener, if the focus is adrift.
   */
  function Dialog(node) {
    var state = { node: node, opener: null, refocusTimer: 0, onClose: null };
    dialogs.push(state);

    node.addEventListener("cancel", function (event) {
      if (busy) event.preventDefault();
    });
    node.addEventListener("click", function (event) {
      if (event.target === node && !busy) node.close();
    });
    // Tab and Shift+Tab go round the dialog's buttons; the focus never leaves it for the browser.
    node.addEventListener("keydown", function (event) {
      if (event.key !== "Tab") return;
      var all = node.querySelectorAll("button");
      var usable = [];
      for (var i = 0; i < all.length; i += 1) {
        if (!all[i].disabled && !all[i].hidden) usable.push(all[i]);
      }
      if (usable.length === 0) {
        event.preventDefault();
        return;
      }
      var first = usable[0];
      var last = usable[usable.length - 1];
      var at = document.activeElement;
      if (event.shiftKey && (at === first || !node.contains(at))) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && (at === last || !node.contains(at))) {
        event.preventDefault();
        first.focus();
      }
    });
    node.addEventListener("close", function () {
      if (node.open) return;
      var back = state.opener;
      var then = state.onClose;
      state.opener = null;
      state.onClose = null;
      clearTimeout(state.refocusTimer);
      if (then) then();
      if (!back || back.hidden || back.disabled) return;
      if (focusAdrift(node)) back.focus();
      state.refocusTimer = setTimeout(function () {
        if (!node.open && focusAdrift(node)) back.focus();
      }, 0);
    });

    state.open = function (opener, first) {
      clearTimeout(state.refocusTimer);
      state.opener = opener;
      if (!node.open) node.showModal();
      if (first) first.focus();
    };
    return state;
  }

  // Escape is handled here, not left to the browser: WebKit fires no "cancel" for an Escape typed
  // in a dialog's fields, and Chromium closes a modal dialog on a second Escape even when its
  // "cancel" was refused. It listens on the document, in the capture phase: while a request is out
  // the confirm button is disabled, so the focus may have left the dialog for the body. An Escape
  // meant for anything else is left alone, and so is one that ends text composition.
  document.addEventListener("keydown", function (event) {
    if (event.isComposing) return;
    if (event.key !== "Escape" && event.key !== "Esc") return;
    for (var i = 0; i < dialogs.length; i += 1) {
      var node = dialogs[i].node;
      if (!node.open) continue;
      var target = event.target;
      if (target !== node && !node.contains(target) && target !== document.body && target !== document.documentElement) return;
      event.preventDefault();
      if (!busy) node.close();
      return;
    }
  }, true);

  /* ---- Edit mode ---- */

  var actions = el("record-actions");
  var form = el("edit-form");
  var editButton = el("edit-button");
  var guarded = true;
  var editing = false;
  // Takes edit mode's history entry off before the page goes; a page without edit mode has none.
  var leaveEditEntry = function (then) { then(); };

  if (actions && actions.querySelector("button")) AdminDom.show(actions, true);

  if (form && editButton) {
    var saveButton = el("edit-save");
    var cancelButton = el("edit-cancel");
    var buttonsBox = el("edit-buttons");
    var summary = el("edit-summary");
    var summaryList = el("edit-summary-list");
    var conflict = el("edit-conflict");
    var conflictText = el("edit-conflict-text");
    var reloadButton = el("edit-reload");
    var alertBox = el("edit-alert");
    var entityPick = el("edit-entityId");
    var ownerBox = el("edit-owner-picker");
    var views = root.querySelectorAll(".detail.editable .view");
    var edits = root.querySelectorAll(".detail.editable .edit");
    var keyed = form.querySelectorAll("[data-key]");
    var initial = {};

    var read = function (control) {
      if (control.tagName === "FIELDSET") {
        var checked = control.querySelector("input:checked");
        return checked ? checked.value : "";
      }
      return control.value;
    };

    var write = function (control, value) {
      if (control.tagName === "FIELDSET") {
        var radios = control.querySelectorAll("input");
        for (var i = 0; i < radios.length; i += 1) radios[i].checked = radios[i].value === value;
        return;
      }
      control.value = value;
    };

    var keyOf = function (control) {
      return control.getAttribute("data-key");
    };

    // The page's values are the server's, never what the browser kept from an earlier visit (some
    // engines put typed values, a checked radio or a chosen option back on reload and on Back): every
    // control is set to the value it was written with, then that is what a change is measured from.
    if (entityPick && entityPick.tagName === "SELECT") {
      for (var o = 0; o < entityPick.options.length; o += 1) entityPick.options[o].selected = entityPick.options[o].defaultSelected;
    }
    for (var k = 0; k < keyed.length; k += 1) {
      var control = keyed[k];
      if (control.tagName === "FIELDSET") {
        var radios = control.querySelectorAll("input");
        for (var c = 0; c < radios.length; c += 1) radios[c].checked = radios[c].defaultChecked;
      } else if (control.type === "hidden") {
        control.value = control.getAttribute("data-initial") || "";
      } else {
        control.value = control.defaultValue;
      }
      initial[keyOf(control)] = read(control);
    }
    if (ownerBox) ownerBox.value = ownerBox.defaultValue;
    var ownerShown = ownerBox ? ownerBox.value : "";
    var entityIndex = entityPick && entityPick.tagName === "SELECT" ? entityPick.selectedIndex : -1;

    var changed = function (control) {
      return read(control) !== initial[keyOf(control)];
    };

    var dirty = function () {
      if (!editing) return false;
      for (var i = 0; i < keyed.length; i += 1) {
        if (changed(keyed[i])) return true;
      }
      return false;
    };

    var updateSave = function () {
      saveButton.disabled = busy || !dirty();
    };

    /** Where a field's error points: the control the owner can reach. */
    var focusTarget = function (control) {
      if (control.tagName === "FIELDSET") return control.querySelector("input:checked") || control.querySelector("input");
      if (control.type === "hidden" && ownerBox) return ownerBox;
      return control;
    };

    var errorNode = function (key) {
      return el("edit-" + key + "-error");
    };

    var setFieldError = function (control, message) {
      var key = keyOf(control);
      if (control.tagName === "FIELDSET") {
        var radios = control.querySelectorAll("input");
        for (var i = 0; i < radios.length; i += 1) {
          if (message) radios[i].setAttribute("aria-invalid", "true");
          else radios[i].removeAttribute("aria-invalid");
        }
        AdminDom.setText(errorNode(key), message);
        return;
      }
      AdminDom.setInvalid(focusTarget(control), errorNode(key), message);
    };

    var clearErrors = function () {
      for (var i = 0; i < keyed.length; i += 1) setFieldError(keyed[i], "");
      AdminDom.clear(summaryList);
      AdminDom.show(summary, false);
      AdminDom.show(conflict, false);
      AdminDom.setText(conflictText, "");
      AdminDom.show(alertBox, false);
      AdminDom.setText(alertBox, "");
    };

    var setMode = function (on) {
      editing = on;
      for (var i = 0; i < views.length; i += 1) AdminDom.show(views[i], !on);
      for (var j = 0; j < edits.length; j += 1) AdminDom.show(edits[j], on);
      AdminDom.show(buttonsBox, on);
      AdminDom.show(actions, !on);
      if (!on) {
        closeOptions();
        clearErrors();
        for (var k = 0; k < keyed.length; k += 1) write(keyed[k], initial[keyOf(keyed[k])]);
        if (ownerBox) ownerBox.value = ownerShown;
      }
      updateSave();
    };

    // ---- Back leaves edit mode (one history entry of the same address) ----
    // Edit pushes one entry (on the owner's press, so every engine keeps it). Back from it leaves
    // edit mode; with changes it asks first, and Keep editing steps forward onto it again. Every
    // way out of the page takes the entry off first, so one Back always returns to the record.

    var editEntry = false;
    var afterPop = null;

    var pushEditEntry = function () {
      try {
        history.pushState({ dwEdit: true }, "", location.href);
        editEntry = true;
      } catch (e) {
        editEntry = false;
      }
    };

    /** Takes edit mode's history entry off (if it is the current one), then goes on. */
    leaveEditEntry = function (then) {
      if (!editEntry) {
        then();
        return;
      }
      editEntry = false;
      afterPop = then;
      history.back();
      // Should the browser not report the step back, go on anyway.
      setTimeout(function () {
        if (afterPop === then) {
          afterPop = null;
          then();
        }
      }, 500);
    };

    window.addEventListener("popstate", function () {
      if (afterPop) {
        var then = afterPop;
        afterPop = null;
        then();
        return;
      }
      var state = history.state;
      if (state && state.dwEdit) {
        // Forward onto edit mode's entry again: after Keep editing it is edit mode's again; in view
        // mode (Forward after Back) it becomes a plain entry, so the next Edit's entry is the only one.
        if (editing) editEntry = true;
        else history.replaceState(null, "", location.href);
        return;
      }
      if (!editEntry) return;
      editEntry = false;
      if (!editing) return;
      var stay = function () { history.forward(); };
      if (busy) {
        stay();
        return;
      }
      if (guarded && dirty()) {
        var at = document.activeElement;
        askDiscard(at && at !== form && form.contains(at) ? at : cancelButton, function () {
          setMode(false);
          editButton.focus();
        }, stay);
        return;
      }
      setMode(false);
      editButton.focus();
    });

    // ---- the unsaved-changes guard (UXR-B16) ----

    var discardNode = el("discard-dialog");
    var discard = discardNode ? Dialog(discardNode) : null;
    var discardThen = null;
    var discardKeep = null;

    /**
     * "Discard your changes?": Discard goes on with then(); Keep editing, Escape or the backdrop
     * goes on with keep(), if given. Never while a save is out.
     */
    var askDiscard = function (opener, then, keep) {
      if (busy) {
        if (keep) keep();
        return;
      }
      if (!discard) {
        then();
        return;
      }
      discardThen = then;
      discardKeep = keep || null;
      discard.onClose = function () {
        var kept = discardKeep;
        discardThen = null;
        discardKeep = null;
        if (kept) kept();
      };
      discard.open(opener, el("discard-keep"));
    };

    el("discard-keep").addEventListener("click", function () {
      discardNode.close();
    });

    el("discard-confirm").addEventListener("click", function () {
      if (busy) return;
      var then = discardThen;
      discardThen = null;
      discardKeep = null;
      discard.opener = null;
      discardNode.close();
      if (then) then();
    });

    // A link out of the page, from edit mode. With changes it asks first; either way edit mode's
    // history entry goes before the page does. While a save is out it waits for the answer. A link
    // opened elsewhere (a modifier, another button) loses nothing. A link within the page (the skip
    // link, the error summary's) only moves the focus: it adds no history entry, which Back would
    // otherwise read as leaving edit mode.
    document.addEventListener("click", function (event) {
      if (!editing || !guarded || event.defaultPrevented) return;
      if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
      var link = event.target && event.target.closest ? event.target.closest("a[href]") : null;
      if (!link || link.target) return;
      var href = link.getAttribute("href") || "";
      if (href === "") return;
      if (href.charAt(0) === "#") {
        event.preventDefault();
        var spot = href.length > 1 ? document.getElementById(href.slice(1)) : null;
        if (spot) {
          if (spot.tabIndex < 0 && !spot.hasAttribute("tabindex")) spot.setAttribute("tabindex", "-1");
          spot.focus();
        }
        return;
      }
      event.preventDefault();
      if (busy) return;
      var go = function () {
        guarded = false;
        leaveEditEntry(function () { location.assign(link.href); });
      };
      if (dirty()) askDiscard(link, go);
      else go();
    }, true);

    // Sign out (or any other form of the page) while editing, the same way.
    document.addEventListener("submit", function (event) {
      if (event.target === form || !editing || !guarded) return;
      event.preventDefault();
      event.stopImmediatePropagation();
      if (busy) return;
      var button = event.target.querySelector("button[type=submit]");
      var go = function () {
        leaveEditEntry(function () {
          setMode(false);
          if (button) button.click();
        });
      };
      if (dirty()) askDiscard(button, go);
      else go();
    }, true);

    // ---- the owner picker: one page of matches from the read API ----

    var ownerRoot = el("edit-owner");
    var ownerValue = el("edit-ownerParticipantId");
    var ownerList = el("edit-owner-options");
    var ownerTimer = null;
    var asked = 0;
    var activeOption = -1;

    function closeOptions() {
      if (!ownerList) return;
      clearTimeout(ownerTimer);
      asked += 1;
      ownerList.hidden = true;
      ownerBox.setAttribute("aria-expanded", "false");
      ownerBox.removeAttribute("aria-activedescendant");
      activeOption = -1;
    }

    if (ownerRoot && ownerBox && ownerValue && ownerList) {
      var source = ownerRoot.getAttribute("data-source") || "";
      var none = ownerRoot.getAttribute("data-no-matches") || "";

      var options = function () {
        return ownerList.querySelectorAll("li[data-value]");
      };

      var highlight = function (index) {
        var all = options();
        for (var i = 0; i < all.length; i += 1) {
          all[i].classList.toggle("active", i === index);
          all[i].setAttribute("aria-selected", i === index ? "true" : "false");
        }
        activeOption = index;
        if (index >= 0 && all[index]) {
          ownerBox.setAttribute("aria-activedescendant", all[index].id);
          if (all[index].scrollIntoView) all[index].scrollIntoView({ block: "nearest" });
        } else {
          ownerBox.removeAttribute("aria-activedescendant");
        }
      };

      var choose = function (option) {
        ownerValue.value = option.getAttribute("data-value");
        ownerBox.value = option.textContent;
        closeOptions();
        setFieldError(ownerValue, "");
        updateSave();
      };

      var showOptions = function (items) {
        AdminDom.clear(ownerList);
        var shown = 0;
        for (var i = 0; i < items.length && shown < MATCHES; i += 1) {
          var item = items[i] || {};
          var value = text(item.sub);
          if (!value) continue;
          var option = document.createElement("li");
          option.id = ownerList.id + "-" + shown;
          option.setAttribute("role", "option");
          option.setAttribute("aria-selected", "false");
          option.setAttribute("data-value", value);
          option.textContent = text(item.displayName) || value;
          ownerList.appendChild(option);
          shown += 1;
        }
        if (shown === 0) {
          var empty = document.createElement("li");
          empty.setAttribute("role", "option");
          empty.setAttribute("aria-disabled", "true");
          empty.setAttribute("aria-selected", "false");
          empty.textContent = none;
          ownerList.appendChild(empty);
        }
        ownerList.hidden = false;
        ownerBox.setAttribute("aria-expanded", "true");
        activeOption = -1;
      };

      var ask = function () {
        var typed = ownerBox.value.trim();
        if (!typed) {
          closeOptions();
          return;
        }
        var ticket = ++asked;
        var query = "?q=" + encodeURIComponent(typed) + "&limit=" + MATCHES + "&sort=name&dir=asc";
        AdminRequest.get(source + query).then(function (reply) {
          if (ticket !== asked || leaving) return;
          if (signedOut(reply)) return;
          var body = reply.body || {};
          showOptions(reply.status === 200 && Array.isArray(body.items) ? body.items : []);
        }, function () {
          if (ticket === asked && !leaving) showOptions([]);
        });
      };

      ownerBox.addEventListener("input", function () {
        // Typed text is not a choice: until an option is chosen, no owner is set.
        ownerValue.value = "";
        clearTimeout(ownerTimer);
        ownerTimer = setTimeout(ask, PAUSE_MS);
        updateSave();
      });

      ownerBox.addEventListener("keydown", function (event) {
        var all = options();
        if (event.key === "ArrowDown") {
          event.preventDefault();
          if (ownerList.hidden) { ask(); return; }
          if (all.length) highlight(Math.min(activeOption + 1, all.length - 1));
        } else if (event.key === "ArrowUp") {
          event.preventDefault();
          if (all.length) highlight(Math.max(activeOption - 1, 0));
        } else if (event.key === "Enter") {
          if (!ownerList.hidden) {
            event.preventDefault();
            if (activeOption >= 0 && all[activeOption]) choose(all[activeOption]);
          }
        } else if (event.key === "Escape" && !event.isComposing) {
          if (!ownerList.hidden) {
            event.preventDefault();
            closeOptions();
          }
        }
      });

      ownerBox.addEventListener("blur", function () {
        closeOptions();
      });

      // A press on an option keeps the focus in the box, then chooses it.
      ownerList.addEventListener("mousedown", function (event) {
        event.preventDefault();
      });
      ownerList.addEventListener("click", function (event) {
        var option = event.target.closest ? event.target.closest("li[data-value]") : null;
        if (option) choose(option);
      });
    }

    // ---- several subject entities: the one picked is the one edited ----

    if (entityPick && entityPick.tagName === "SELECT") {
      var entityName = el("edit-name");
      var entityDescription = el("edit-description");

      /** The picked entity's stored words into the boxes; a change is measured from what the boxes then hold. */
      var loadEntity = function () {
        var option = entityPick.options[entityPick.selectedIndex];
        if (!option) return;
        entityIndex = entityPick.selectedIndex;
        if (entityName) {
          entityName.value = option.getAttribute("data-name") || "";
          // A text box drops line breaks: a stored name with one is not a change by being shown.
          initial.name = read(entityName);
          setFieldError(entityName, "");
        }
        if (entityDescription) {
          entityDescription.value = option.getAttribute("data-description") || "";
          initial.description = read(entityDescription);
          setFieldError(entityDescription, "");
        }
        updateSave();
      };

      entityPick.addEventListener("change", function () {
        var typed = (entityName && changed(entityName)) || (entityDescription && changed(entityDescription));
        if (!typed) {
          loadEntity();
          return;
        }
        // What was typed for this entity would go: the pick waits for the owner's answer.
        var wanted = entityPick.selectedIndex;
        entityPick.selectedIndex = entityIndex;
        askDiscard(entityPick, function () {
          entityPick.selectedIndex = wanted;
          loadEntity();
        });
      });
    }

    // ---- the request ----

    /** An end time typed in ET ("2026-12-01T10:00") as one instant in UTC, or null when it is not one. */
    var eastern = null;
    try {
      eastern = new Intl.DateTimeFormat("en-US", {
        timeZone: "America/New_York", hourCycle: "h23", year: "numeric", month: "2-digit", day: "2-digit",
        hour: "2-digit", minute: "2-digit", second: "2-digit"
      });
    } catch (e) {
      eastern = null;
    }

    var offsetAt = function (utcMs) {
      var parts = eastern.formatToParts(new Date(utcMs));
      var p = {};
      for (var i = 0; i < parts.length; i += 1) p[parts[i].type] = parts[i].value;
      var asUtc = Date.UTC(Number(p.year), Number(p.month) - 1, Number(p.day), Number(p.hour) % 24, Number(p.minute), Number(p.second));
      return asUtc - Math.floor(utcMs / 1000) * 1000;
    };

    var easternToIso = function (typed) {
      var m = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(typed);
      if (!m || !eastern) return null;
      var wall = Date.UTC(Number(m[1]), Number(m[2]) - 1, Number(m[3]), Number(m[4]), Number(m[5]));
      var first = wall - offsetAt(wall);
      return new Date(wall - offsetAt(first)).toISOString();
    };

    /** The changed fields, as the API takes them; null when nothing changed. */
    var changes = function () {
      var body = {};
      var any = false;
      for (var i = 0; i < keyed.length; i += 1) {
        var control = keyed[i];
        if (!changed(control)) continue;
        var key = keyOf(control);
        var value = read(control);
        if (key === "isActive") body.isActive = value === "true";
        else if (key === "endsAt") body.endsAt = value === "" ? null : (easternToIso(value) || value);
        else body[key] = value;
        any = true;
      }
      if (!any) return null;
      if ((body.name !== undefined || body.description !== undefined) && entityPick) body.entityId = entityPick.value;
      return body;
    };

    var setSaving = function (on) {
      busy = on;
      AdminDom.setText(saveButton, on ? copy.saving : copy.save);
      if (on) form.setAttribute("aria-busy", "true");
      else form.removeAttribute("aria-busy");
      updateSave();
    };

    var showAlert = function (message) {
      AdminDom.setText(alertBox, message);
      AdminDom.show(alertBox, true);
      alertBox.focus();
    };

    var showFieldErrors = function (fields) {
      AdminDom.clear(summaryList);
      for (var key in fields) {
        if (!Object.prototype.hasOwnProperty.call(fields, key)) continue;
        var message = text(fields[key]);
        if (!message) continue;
        var control = form.querySelector("[data-key=\"" + (/^[A-Za-z]+$/.test(key) ? key : "-") + "\"]");
        var item = document.createElement("li");
        if (control) {
          setFieldError(control, message);
          var target = focusTarget(control);
          var link = document.createElement("a");
          link.href = "#" + target.id;
          link.setAttribute("data-field", target.id);
          link.textContent = message;
          item.appendChild(link);
        } else {
          item.textContent = message;
        }
        summaryList.appendChild(item);
      }
      if (!summaryList.firstChild) {
        showAlert(copy.refused);
        return;
      }
      AdminDom.show(summary, true);
      summary.focus();
    };

    var showConflict = function (body) {
      AdminDom.setText(conflictText, text(body.message) || copy.conflict);
      AdminDom.show(conflict, true);
      conflict.focus();
    };

    summaryList.addEventListener("click", function (event) {
      var link = event.target.closest ? event.target.closest("a[data-field]") : null;
      if (!link) return;
      event.preventDefault();
      var target = el(link.getAttribute("data-field"));
      if (target) target.focus();
    });

    form.addEventListener("input", function (event) {
      var control = event.target.closest ? event.target.closest("[data-key]") : null;
      if (control && control !== ownerValue) setFieldError(control, "");
      updateSave();
    });
    form.addEventListener("change", updateSave);

    form.addEventListener("submit", function (event) {
      event.preventDefault();
      if (busy || !editing) return;
      var body = changes();
      if (!body) {
        updateSave();
        return;
      }
      clearErrors();
      setSaving(true);
      patch(body).then(function (reply) {
        if (leaving) return;
        var b = reply.body || {};
        if (isThisRecord(reply)) {
          done("saved");
          return;
        }
        if (signedOut(reply)) return;
        setSaving(false);
        if (reply.status === 403) {
          afterTokenRefused(showAlert);
        } else if (reply.status === 409) {
          showConflict(b);
        } else if (reply.status === 400 && b.error === BAD_REQUEST && b.fields && typeof b.fields === "object" && !Array.isArray(b.fields)) {
          showFieldErrors(b.fields);
        } else if (reply.status === 400 && b.error === BAD_REQUEST) {
          showAlert(copy.refused);
        } else {
          afterUnclearReply(showAlert);
        }
      }, function () {
        if (leaving) return;
        setSaving(false);
        afterUnclearReply(showAlert);
      });
    });

    editButton.addEventListener("click", function () {
      if (busy || editing) return;
      setMode(true);
      pushEditEntry();
      var first = form.querySelector(".edit input:not([type=hidden]), .edit select, .edit textarea");
      if (first && first.type === "radio") first = form.querySelector(".edit input:checked") || first;
      if (first) first.focus();
    });

    cancelButton.addEventListener("click", function () {
      if (busy) return;
      leaveEditEntry(function () {
        setMode(false);
        editButton.focus();
      });
    });

    reloadButton.addEventListener("click", function () {
      guarded = false;
      leaveEditEntry(function () {
        location.reload();
      });
    });

    setMode(false);
  }

  /* ---- Actions: Close, Expire, Cancel offer ---- */

  var actionNode = el("action-dialog");
  if (actionNode) {
    var action = Dialog(actionNode);
    var title = el("action-dialog-title");
    var bodyText = el("action-dialog-body");
    var actionAlert = el("action-dialog-alert");
    var actionConfirm = el("action-dialog-confirm");
    var actionReload = el("action-dialog-reload");
    var actionCancel = el("action-dialog-cancel");
    var current = null;

    var setActionBusy = function (on) {
      busy = on;
      actionConfirm.disabled = on;
      actionCancel.disabled = on;
      if (on) {
        AdminDom.setText(actionConfirm, current.getAttribute("data-busy") || "");
        actionNode.setAttribute("aria-busy", "true");
      } else {
        actionNode.removeAttribute("aria-busy");
      }
    };

    var actionFailed = function (message) {
      setActionBusy(false);
      AdminDom.setText(actionAlert, message);
      AdminDom.setText(actionConfirm, copy.retryLabel);
      actionConfirm.focus();
    };

    var openAction = function (button) {
      current = button;
      AdminDom.setText(title, button.getAttribute("data-title") || "");
      AdminDom.setText(bodyText, button.getAttribute("data-body") || "");
      AdminDom.setText(actionAlert, "");
      AdminDom.setText(actionConfirm, button.getAttribute("data-confirm") || "");
      AdminDom.setText(actionCancel, button.getAttribute("data-cancel") || "");
      AdminDom.show(actionConfirm, true);
      AdminDom.show(actionReload, false);
      actionConfirm.disabled = false;
      actionCancel.disabled = false;
      action.open(button, actionCancel);
    };

    var buttons = root.querySelectorAll("button.record-action");
    for (var b = 0; b < buttons.length; b += 1) {
      buttons[b].addEventListener("click", function (event) {
        if (busy || editing) return;
        openAction(event.currentTarget);
      });
    }

    actionCancel.addEventListener("click", function () {
      if (!busy) actionNode.close();
    });

    actionReload.addEventListener("click", function () {
      guarded = false;
      location.reload();
    });

    actionConfirm.addEventListener("click", function () {
      if (busy || !current) return;
      var status = current.getAttribute("data-status") || "";
      var note = current.getAttribute("data-note") || "";
      AdminDom.setText(actionAlert, "");
      setActionBusy(true);
      patch({ status: status }).then(function (reply) {
        if (leaving) return;
        var body = reply.body || {};
        if (isThisRecord(reply) && text(body.status) === status) {
          done(note);
          return;
        }
        if (signedOut(reply)) return;
        if (reply.status === 403) {
          afterTokenRefused(actionFailed);
        } else if (reply.status === 409) {
          setActionBusy(false);
          AdminDom.setText(actionAlert, text(body.message) || copy.conflict);
          AdminDom.show(actionConfirm, false);
          AdminDom.show(actionReload, true);
          actionReload.focus();
        } else if (reply.status === 400 && body.error === BAD_REQUEST) {
          actionFailed(copy.nothing);
        } else {
          afterUnclearReply(actionFailed);
        }
      }, function () {
        if (leaving) return;
        afterUnclearReply(actionFailed);
      });
    });
  }

  /* ---- What was just done, said once the page has loaded anew ---- */

  var arrived = history.state && typeof history.state === "object" ? history.state : null;
  if (arrived && arrived.dwEdit) {
    // Loaded on edit mode's history entry. After a reload in edit mode, step back onto the record's
    // own entry, so one Back still leaves the page. Any other load (Safari keeps the state when the
    // same address is opened again) starts in view mode on this entry, made a plain one.
    var how = typeof performance.getEntriesByType === "function" ? performance.getEntriesByType("navigation") : [];
    if (how.length > 0 && how[0].type === "reload") history.back();
    else history.replaceState(null, "", location.href);
  } else if (arrived && typeof arrived.dwNote === "string") {
    // Set by this script just before it loaded the page anew over a change it made; used once.
    var note = arrived.dwNote;
    history.replaceState(null, "", location.pathname + location.search + location.hash);
    var sentence = NOTES.indexOf(note) >= 0 ? root.getAttribute("data-note-" + note) : null;
    if (sentence) {
      var target = note === "saved" && editButton && !editButton.hidden ? editButton : main;
      if (target) target.focus();
      // A moment after load, so that screen readers hear the change.
      setTimeout(function () { say(sentence); }, 100);
    }
  }
})();
