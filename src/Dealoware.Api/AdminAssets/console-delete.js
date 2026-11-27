/**
 * Delete with confirmation (AdminDeletePage; A7 Step 10, UXR-C01 to C07), on
 * the detail pages and the list rows. Served only with a valid session.
 *
 * The server writes a hidden Delete beside Edit on a detail page and in the
 * last cell of each list row (none for a soft-deleted record), and one empty
 * dialog whose words come in its data-copy. Without this script nothing can
 * be deleted.
 *
 * Delete asks the server first: POST /admin/api/{type}/{id}/delete-intent,
 * with the anti-forgery token in X-CSRF-TOKEN. Then the modal dialog opens
 * with the focus on Cancel:
 *   200 -> "Delete {type} {name}?", the ID, what goes with it as a list of
 *          the server's counts, an open negotiation named as open, "This
 *          can't be restored from the admin.", the destructive button
 *          ("Delete participant") and Cancel. A participant with records
 *          that go with it, and an open negotiation, need the name or the ID
 *          typed exactly (case and spaces too) before the button works.
 *   409 Blocked -> "This artifact can't be deleted" and the negotiations
 *          that use it, each a link to its page; Close is the only button.
 *   404 -> the record is not there any more (deleted already, perhaps by a
 *          first try whose answer was lost): the page goes to the list,
 *          which says so.
 * While that request is out, Delete, Edit and the actions beside it are
 * disabled, so nothing else starts under the dialog.
 * The confirm token stays in this script's memory while the dialog is open,
 * and goes with the dialog. It travels in the X-Admin-Confirm-Token header,
 * never in an address.
 *
 * The destructive button sends one DELETE /admin/api/{type}/{id}. While it
 * is out the button says "Deleting…" and is disabled, Cancel too, and the
 * dialog does not close: a double click sends one request.
 *   200 with this record -> the page goes to the list (with its state),
 *          loaded anew, and the list says "{Type} {name} deleted." in the
 *          shell's live region. What to say rides in the history entry's
 *          state, which only this script sets, never in the address.
 *   409 ConfirmTokenInvalid or Conflict -> nothing was deleted: the dialog
 *          asks the server again and shows its fresh answer, with "This
 *          confirmation is no longer valid. Review the details again." (or
 *          that the record changed). What was typed is kept for that answer.
 *   409 Blocked, 404 -> as above.
 *   500 from the server, or its 400 -> "Nothing was deleted." and Retry.
 *   401 from the session gate -> sign-in with the "session ended" note.
 *   403 -> the gate did not take this page's anti-forgery token: still
 *          signed in, the page loads itself anew once with #fresh.
 *   Anything else, or no reply -> ask GET /admin/api/me: the session gone ->
 *          sign-in; otherwise "We couldn't confirm the delete. Try again
 *          to see whether it went through.", and Retry.
 * aria-busy is on the dialog only while a request is out.
 *
 * The dialog handles Escape itself, in a capture-phase keydown on the
 * document (an Escape that ends text composition is left alone). Escape,
 * Cancel and a press on the backdrop close it and give the focus back to the
 * Delete that opened it at once (or to the page's main area, should it be
 * gone); nothing is sent, so no audit row is written. Every open starts
 * empty: nothing typed, no sentence, no token from an earlier one. Tab and
 * Shift+Tab stay in the dialog.
 *
 * The list is loaded anew with location.reload() after replaceState, so that
 * the history entry's state reaches it. That state holds what the sentence
 * needs (the record's name, or that it was missing) and is taken off as soon
 * as the list has read it. A list that arrived this way still loads itself
 * anew once over a refused anti-forgery token.
 *
 * Nothing is stored. Text goes into the page through textContent only.
 */
(function () {
  "use strict";

  var SESSION_ENDED = "/admin/sign-in#ended";
  var ME = "/admin/api/me";
  var GATE_DENIED = "Unauthorized";
  var BAD_REQUEST = "BadRequest";
  var NOT_FOUND = "NotFound";
  var TOKEN_INVALID = "ConfirmTokenInvalid";
  var CHANGED = "Conflict";
  var BLOCKED = "Blocked";
  var SERVER_FAULT = "Internal Server Error";
  var SIGNED_IN_AS = "CoreOwner";
  var FRESH = "#fresh";
  var TOKEN_HEADER = "X-Admin-Confirm-Token";
  var NAME = "{name}";
  var COUNT = "{n}";
  var TIMEOUT_MS = 15000;
  var TYPES = ["participants", "artifacts", "negotiations", "offers"];
  var GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
  var CONFIRM_TOKEN = /^[0-9a-f]{64}$/;

  var node = document.getElementById("delete-dialog");
  var live = document.getElementById("console-status");
  var main = document.getElementById("main");
  if (!node) return;
  // A helper did not arrive, or the browser has no modal dialog: no Delete is shown.
  if (typeof AdminRequest === "undefined" || typeof AdminDom === "undefined") return;
  if (typeof node.showModal !== "function") return;

  var el = AdminDom.el;
  var text = AdminRequest.text;
  var copy = parse(node.getAttribute("data-copy")) || {};
  var type = node.getAttribute("data-type") || "";
  var returnTo = node.getAttribute("data-return") || "";
  if (TYPES.indexOf(type) < 0) return;
  // Where the page goes after a delete: this kind's list, on this site, and nowhere else.
  var listPath = "/admin/" + type;
  if (returnTo !== listPath && returnTo.indexOf(listPath + "?") !== 0) returnTo = listPath;

  var titleNode = el("delete-dialog-title");
  var content = el("delete-dialog-content");
  var field = el("delete-dialog-field");
  var actions = el("delete-dialog-actions");
  if (!titleNode || !content || !field || !actions) return;
  // One alert line for the dialog's whole life, so a sentence put into it is announced.
  var alertLine = document.createElement("p");
  alertLine.id = "delete-dialog-alert";
  alertLine.className = "edit-dialog-alert";
  alertLine.setAttribute("role", "alert");
  actions.parentNode.insertBefore(alertLine, actions);


  // The page is going away: an answer that arrives now changes nothing.
  var leaving = false;
  window.addEventListener("pagehide", function () { leaving = true; });
  window.addEventListener("pageshow", function () { leaving = false; });

  function parse(raw) {
    try {
      var body = JSON.parse(raw);
      return body && typeof body === "object" && !Array.isArray(body) ? body : null;
    } catch (e) {
      return null;
    }
  }

  function words(key) {
    return text(copy[key]);
  }

  /** A template with its one placeholder filled in. The value is put in once, never read as a template. */
  function fill(template, key, value) {
    var at = template.indexOf(key);
    return at < 0 ? template : template.slice(0, at) + value + template.slice(at + key.length);
  }

  function count(n) {
    return n.toLocaleString("en-US");
  }

  function say(message) {
    AdminDom.setText(live, message);
  }

  /* ---- What a delete just did, said once the list has loaded anew ---- */

  var arrived = history.state && typeof history.state === "object" ? history.state : null;
  var arrivedByDelete = !!arrived && (arrived.dwDelete === "deleted" || arrived.dwDelete === "missing");
  if (arrivedByDelete) {
    // Set by this script just before it loaded the list; used once, and gone from the history entry.
    var sentence = arrived.dwDelete === "deleted" && typeof arrived.name === "string"
      ? fill(words("note"), NAME, arrived.name)
      : words("notFound");
    history.replaceState(null, "", location.pathname + location.search + location.hash);
    if (location.pathname === listPath) {
      if (main) main.focus();
      // A moment after load, so that screen readers hear the change.
      setTimeout(function () { say(sentence); }, 100);
    }
  }

  // A page loaded anew (by a script over a refused token, or by the owner) does not do it again by
  // itself. The list this script loaded after a delete is not such a page.
  var navigation = typeof performance.getEntriesByType === "function" ? performance.getEntriesByType("navigation") : [];
  var mayReload = arrivedByDelete || !(navigation.length > 0 && navigation[0].type === "reload");

  /* ---- Requests ---- */

  function csrfToken() {
    var meta = document.querySelector('meta[name="dw-csrf"]');
    return meta ? meta.getAttribute("content") || "" : "";
  }

  /**
   * One request to the delete API, as admin-request.js sends a POST: same
   * origin, no redirect followed, no cache, no mode set, 15 seconds at most.
   * Resolves with { status, body }; rejects when there was no reply.
   */
  function send(method, path, confirmToken) {
    var controller = new AbortController();
    var timer = setTimeout(function () { controller.abort(); }, TIMEOUT_MS);
    var headers = { "X-CSRF-TOKEN": csrfToken() };
    if (confirmToken) headers[TOKEN_HEADER] = confirmToken;
    return fetch(path, {
      method: method,
      headers: headers,
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

  function signedOut(reply) {
    var body = reply.body || {};
    if (reply.status === 401 && body.error === GATE_DENIED) {
      location.replace(SESSION_ENDED);
      return true;
    }
    return false;
  }

  /** The gate refused this page's anti-forgery token: load the page anew once, if still signed in. */
  function afterTokenRefused(fail, sentence) {
    AdminRequest.get(ME).then(function (reply) {
      if (leaving || signedOut(reply)) return;
      var body = reply.body || {};
      if (mayReload && reply.status === 200 && body.principal === SIGNED_IN_AS) {
        history.replaceState(null, "", location.pathname + location.search + FRESH);
        location.reload();
      } else {
        fail(sentence);
      }
    }, function () {
      if (!leaving) fail(sentence);
    });
  }

  /** No clear answer. A session that has gone means sign-in; otherwise the given sentence. */
  function afterUnclearReply(fail, sentence) {
    AdminRequest.get(ME).then(function (reply) {
      if (leaving || signedOut(reply)) return;
      fail(sentence);
    }, function () {
      if (!leaving) fail(sentence);
    });
  }

  /* ---- The dialog ---- */

  var busy = false;
  var opener = null;
  var refocusTimer = 0;
  // The record the dialog is about: { id, name } from its Delete, until the server answers.
  var current = null;
  // The server's answer: { token, name, typed }. typed is what must be typed, or null.
  var intent = null;
  var confirmButton = null;
  var cancelButton = null;
  var typedInput = null;

  function apiPath(id) {
    return "/admin/api/" + type + "/" + id;
  }

  function button(label, className) {
    var b = document.createElement("button");
    b.type = "button";
    if (className) b.className = className;
    b.textContent = label;
    return b;
  }

  function paragraph(sentence, className) {
    var p = document.createElement("p");
    if (className) p.className = className;
    p.textContent = sentence;
    return p;
  }

  /** Empties the dialog: its body, its field and its buttons. The alert line stays. */
  function reset(title) {
    AdminDom.setText(titleNode, title);
    AdminDom.clear(content);
    AdminDom.clear(field);
    AdminDom.clear(actions);
    AdminDom.setText(alertLine, "");
    confirmButton = null;
    cancelButton = null;
    typedInput = null;
  }

  /** No request is out: the dialog can close, and it is no longer busy. */
  function idle() {
    busy = false;
    node.removeAttribute("aria-busy");
  }

  /**
   * While the delete-intent is out and the dialog not yet open: Delete, Edit and the actions are
   * disabled, so neither another dialog nor edit mode starts under this one. Only what this script
   * disabled is enabled again.
   */
  function lockPage(on) {
    var controls = document.querySelectorAll("button.delete-open, #edit-button, button.record-action");
    for (var i = 0; i < controls.length; i += 1) {
      var c = controls[i];
      if (on && !c.disabled) {
        c.disabled = true;
        c.setAttribute("data-delete-locked", "");
      } else if (!on && c.hasAttribute("data-delete-locked")) {
        c.disabled = false;
        c.removeAttribute("data-delete-locked");
      }
    }
  }

  /** The focus back to the Delete that opened the dialog; to the page's main area should it be gone. */
  function focusBack(back) {
    if (back && !back.hidden && !back.disabled && document.contains(back)) back.focus();
    else if (main) main.focus();
  }

  /** The focus is where nobody put it: on nothing, on the page itself, or still in the closed dialog. */
  function focusAdrift() {
    var active = document.activeElement;
    return !active || active === document.body || active === document.documentElement || node.contains(active);
  }

  function open(first) {
    clearTimeout(refocusTimer);
    if (!node.open) node.showModal();
    if (first) first.focus();
  }

  /**
   * Closed by the owner (Escape, Cancel, Close or the backdrop): the focus
   * goes back to the Delete that opened it, at once, after the close. Some
   * engines move the focus themselves as the dialog closes (WebKit gives it
   * to the page's main area), so the page does not wait to see where it went.
   */
  function closeByOwner() {
    var back = opener;
    node.close();
    if (!node.open) focusBack(back);
  }

  // Any close: the token and the answer go with the dialog, and a focus left adrift goes back to
  // the Delete that opened it, now and once more on the next tick (an engine may settle the focus
  // after this event). A focus the owner has put elsewhere stays there.
  node.addEventListener("close", function () {
    if (node.open) return;
    var back = opener;
    opener = null;
    current = null;
    intent = null;
    // Nothing of this open stays for the next: what was typed, the sentence, the buttons.
    reset("");
    idle();
    lockPage(false);
    clearTimeout(refocusTimer);
    if (focusAdrift()) focusBack(back);
    refocusTimer = setTimeout(function () {
      if (!node.open && focusAdrift()) focusBack(back);
    }, 0);
  });

  // Any other close the browser asks for: never while a request is out.
  node.addEventListener("cancel", function (event) {
    if (busy) event.preventDefault();
  });

  // A press on the backdrop (the dialog element itself, around its inner box) closes it.
  node.addEventListener("click", function (event) {
    if (event.target === node && !busy) closeByOwner();
  });

  // Escape is handled here, not left to the browser: WebKit fires no "cancel" for an Escape typed
  // in a dialog's field, and Chromium closes a modal dialog on a second Escape even when its
  // "cancel" was refused. It listens on the document, in the capture phase: while a request is out
  // the buttons are disabled, so the focus may have left the dialog for the body. An Escape meant
  // for anything else is left alone, and so is one that ends text composition.
  document.addEventListener("keydown", function (event) {
    if (event.isComposing) return;
    if (!node.open || (event.key !== "Escape" && event.key !== "Esc")) return;
    var target = event.target;
    if (target !== node && !node.contains(target) && target !== document.body && target !== document.documentElement) return;
    event.preventDefault();
    if (!busy) closeByOwner();
  }, true);

  // Tab and Shift+Tab go round the dialog's controls; the focus never leaves it for the page.
  node.addEventListener("keydown", function (event) {
    if (event.key !== "Tab") return;
    var all = node.querySelectorAll("button, input, a[href]");
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

  function addClose() {
    var close = button(words("close"), "secondary");
    close.addEventListener("click", function () {
      if (!busy) closeByOwner();
    });
    actions.appendChild(close);
    return close;
  }

  /** One sentence and Close: the record is gone, or there was no clear answer. */
  function showMessage(sentence) {
    idle();
    lockPage(false);
    reset(fill(words("title"), NAME, current ? current.name : ""));
    content.appendChild(paragraph(sentence));
    open(addClose());
  }

  /** UXR-C05: the negotiations that use the artifact, each a link; Close only, no destructive button. */
  function showBlocked(body) {
    idle();
    lockPage(false);
    intent = null;
    reset(words("blockedTitle"));
    content.appendChild(paragraph(words("blockedLede")));
    var list = document.createElement("ul");
    list.className = "delete-list";
    var ids = Array.isArray(body.negotiationIds) ? body.negotiationIds : [];
    var shown = 0;
    for (var i = 0; i < ids.length; i += 1) {
      var id = text(ids[i]).toLowerCase();
      if (!GUID.test(id)) continue;
      var item = document.createElement("li");
      var link = document.createElement("a");
      link.className = "id";
      link.href = "/admin/negotiations/" + id;
      link.textContent = id;
      item.appendChild(link);
      list.appendChild(item);
      shown += 1;
    }
    content.appendChild(list);
    var total = typeof body.negotiationCount === "number" ? body.negotiationCount : shown;
    if (total > shown) content.appendChild(paragraph(fill(words("blockedMore"), COUNT, count(total - shown))));
    open(addClose());
  }

  /** "1 offer", "3 offers". */
  function counted(n, one, many) {
    return n === 1 ? words(one) : fill(words(many), COUNT, count(n));
  }

  function number(value) {
    return typeof value === "number" && value >= 0 && Math.floor(value) === value ? value : 0;
  }

  /** UXR-C01, C02, C06: the record, what goes with it, the warning, the undo statement, the typed confirmation. */
  function showConfirm(body, note, keepTyped) {
    var identity = body.identity && typeof body.identity === "object" ? body.identity : {};
    var cascade = body.cascade && typeof body.cascade === "object" ? body.cascade : {};
    var id = current.id;
    var name = id;
    if (type === "participants" && text(identity.displayName)) name = text(identity.displayName);
    if (type === "artifacts" && text(identity.name)) name = text(identity.name);

    // What goes with it: the counts less the record itself.
    var negotiations = number(cascade.negotiations) - (type === "negotiations" ? 1 : 0);
    var offers = number(cascade.offers) - (type === "offers" ? 1 : 0);
    var isOpen = type === "negotiations" && identity.isOpen === true;
    var typed = null;
    if (type === "participants" && (negotiations > 0 || offers > 0)) typed = name;
    if (isOpen) typed = id;

    // What was typed stays only when the server is asked again within this open.
    var before = keepTyped && typedInput ? typedInput.value : "";
    intent = { token: text(body.confirmToken), name: name, typed: typed };
    idle();
    lockPage(false);
    reset(fill(words("title"), NAME, name));

    content.appendChild(paragraph(fill(words("id"), NAME, id), "delete-id"));
    if (negotiations > 0 || offers > 0) {
      content.appendChild(paragraph(words("also")));
      var list = document.createElement("ul");
      list.className = "delete-list";
      if (negotiations > 0) {
        var n = document.createElement("li");
        n.textContent = counted(negotiations, "negotiationsOne", "negotiationsMany");
        list.appendChild(n);
      }
      if (offers > 0) {
        var o = document.createElement("li");
        o.textContent = counted(offers, "offersOne", "offersMany");
        list.appendChild(o);
      }
      content.appendChild(list);
    } else {
      content.appendChild(paragraph(words("none")));
    }
    if (isOpen) {
      content.appendChild(paragraph(offers === 0 ? words("openNone") : counted(offers, "openOne", "openMany"), "delete-warning"));
    }
    var openAmong = type === "participants" ? number(cascade.openNegotiations) : 0;
    if (openAmong > 0) {
      content.appendChild(paragraph(counted(openAmong, "openAmongOne", "openAmongMany"), "delete-warning"));
    }
    content.appendChild(paragraph(words("undo"), "delete-undo"));

    if (typed !== null) {
      var label = document.createElement("label");
      label.htmlFor = "delete-dialog-typed";
      label.textContent = fill(words("typed"), NAME, typed);
      typedInput = document.createElement("input");
      typedInput.type = "text";
      typedInput.id = "delete-dialog-typed";
      typedInput.setAttribute("autocomplete", "off");
      typedInput.spellcheck = false;
      typedInput.setAttribute("autocapitalize", "off");
      typedInput.value = before;
      typedInput.addEventListener("input", updateConfirm);
      field.appendChild(label);
      field.appendChild(typedInput);
    }

    confirmButton = button(words("confirm"), "danger");
    confirmButton.addEventListener("click", confirmDelete);
    cancelButton = button(words("cancel"), "secondary");
    cancelButton.addEventListener("click", function () {
      if (!busy) closeByOwner();
    });
    actions.appendChild(confirmButton);
    actions.appendChild(cancelButton);
    updateConfirm();
    AdminDom.setText(alertLine, note || "");
    open(cancelButton);
  }

  /** The destructive button works only with a token, while nothing is out, and once the typed text matches exactly. */
  function updateConfirm() {
    if (!confirmButton) return;
    var matches = !intent || intent.typed === null || (typedInput !== null && typedInput.value === intent.typed);
    confirmButton.disabled = busy || !intent || !matches;
  }

  function setDeleting(on) {
    if (on) {
      busy = true;
      node.setAttribute("aria-busy", "true");
    } else {
      idle();
    }
    if (confirmButton && on) AdminDom.setText(confirmButton, words("busy"));
    if (cancelButton) cancelButton.disabled = on;
    updateConfirm();
  }

  /** The delete did not go through: the sentence, and the destructive button as Retry. */
  function deleteFailed(message) {
    setDeleting(false);
    AdminDom.setText(alertLine, message);
    if (confirmButton) {
      AdminDom.setText(confirmButton, words("retry"));
      if (!confirmButton.disabled) confirmButton.focus();
      else if (typedInput) typedInput.focus();
    }
  }

  /** Asks the server what a delete of the record would take, and shows its answer. */
  function askIntent(note) {
    busy = true;
    if (node.open) node.setAttribute("aria-busy", "true");
    send("POST", apiPath(current.id) + "/delete-intent").then(function (reply) {
      if (leaving || !current) return;
      var body = reply.body || {};
      if (reply.status === 200 && text(body.entityType) === type && text(body.id).toLowerCase() === current.id
          && CONFIRM_TOKEN.test(text(body.confirmToken))) {
        showConfirm(body, note, note !== null);
        return;
      }
      if (signedOut(reply)) return;
      if (reply.status === 409 && body.error === BLOCKED) {
        showBlocked(body);
      } else if (reply.status === 404 && body.error === NOT_FOUND) {
        toList({ dwDelete: "missing" });
      } else if (reply.status === 403) {
        afterTokenRefused(showMessage, words("unclear"));
      } else {
        afterUnclearReply(showMessage, words("unclear"));
      }
    }, function () {
      if (leaving || !current) return;
      afterUnclearReply(showMessage, words("unclear"));
    });
  }

  /**
   * Deleted, or not there any more: the list, with its state, loaded anew, and what it is to say
   * in the history entry's state (never the address). The dialog stays busy until the page goes.
   */
  function toList(state) {
    busy = true;
    history.replaceState(state, "", returnTo);
    location.reload();
  }

  function confirmDelete() {
    if (busy || !intent || !current) return;
    if (intent.typed !== null && (!typedInput || typedInput.value !== intent.typed)) return;
    AdminDom.setText(alertLine, "");
    setDeleting(true);
    var id = current.id;
    send("DELETE", apiPath(id), intent.token).then(function (reply) {
      if (leaving || !current) return;
      var body = reply.body || {};
      if (reply.status === 200 && text(body.entityType) === type && text(body.id).toLowerCase() === id
          && body.deleted && typeof body.deleted === "object") {
        toList({ dwDelete: "deleted", name: intent.name });
        return;
      }
      if (signedOut(reply)) return;
      if (reply.status === 409 && body.error === TOKEN_INVALID) {
        // Nothing was deleted: ask again, and show the fresh answer.
        askIntent(words("expired"));
      } else if (reply.status === 409 && body.error === CHANGED) {
        askIntent(words("changed"));
      } else if (reply.status === 409 && body.error === BLOCKED) {
        showBlocked(body);
      } else if (reply.status === 404 && body.error === NOT_FOUND) {
        toList({ dwDelete: "missing" });
      } else if (reply.status === 403) {
        // The gate refused it before the delete was reached: nothing was deleted.
        afterTokenRefused(deleteFailed, words("nothing"));
      } else if ((reply.status === 500 && body.error === SERVER_FAULT) || (reply.status === 400 && body.error === BAD_REQUEST)) {
        // The server refused it, or rolled it back with its audit rows: nothing was written.
        deleteFailed(words("nothing"));
      } else {
        afterUnclearReply(deleteFailed, words("unclearDelete"));
      }
    }, function () {
      if (leaving || !current) return;
      afterUnclearReply(deleteFailed, words("unclearDelete"));
    });
  }

  /** A Delete on the page: the dialog for its record, after the server's answer. */
  function start(trigger) {
    if (busy || node.open) return;
    var id = (trigger.getAttribute("data-delete-id") || "").toLowerCase();
    if (!GUID.test(id)) return;
    opener = trigger;
    current = { id: id, name: trigger.getAttribute("data-delete-name") || id };
    intent = null;
    reset("");
    lockPage(true);
    askIntent(null);
  }

  var triggers = document.querySelectorAll("button.delete-open");
  for (var t = 0; t < triggers.length; t += 1) {
    if (!GUID.test((triggers[t].getAttribute("data-delete-id") || "").toLowerCase())) continue;
    triggers[t].hidden = false;
    triggers[t].addEventListener("click", function (event) {
      start(event.currentTarget);
    });
  }
  // A detail page without edit mode has a bar of its own for its Delete.
  var bar = el("delete-actions");
  if (bar && bar.querySelector("button.delete-open:not([hidden])")) AdminDom.show(bar, true);
})();
