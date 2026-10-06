(() => {
  const type = document.body.dataset.entityType;
  const id = document.body.dataset.entityId;
  const api = `/admin/api/${type}/${id}`;
  const form = document.getElementById("edit-form");
  const fieldsEl = document.getElementById("fields");
  const saveBtn = document.getElementById("save");
  const cancelBtn = document.getElementById("cancel");
  const expireOpen = document.getElementById("expire-open");
  const live = document.getElementById("live");
  const summary = document.getElementById("error-summary");
  const summaryList = document.getElementById("error-summary-list");
  const conflict = document.getElementById("conflict");
  const typedRef = document.getElementById("typed-reference");
  const typedValues = document.getElementById("typed-values");
  const pageError = document.getElementById("page-error");
  const readonlyNote = document.getElementById("readonly-note");
  const recordId = document.getElementById("record-id");

  const expireDialog = document.getElementById("expire-dialog");
  const expireTitle = document.getElementById("expire-title");
  const expireBody = document.getElementById("expire-body");
  const expireError = document.getElementById("expire-error");
  const expireConfirm = document.getElementById("expire-confirm");
  const expireCancel = document.getElementById("expire-cancel");
  const expireRetry = document.getElementById("expire-retry");

  const closeDialog = document.getElementById("close-dialog");
  const closeConfirm = document.getElementById("close-confirm");
  const closeCancel = document.getElementById("close-cancel");

  const discardDialog = document.getElementById("discard-dialog");
  const discardConfirm = document.getElementById("discard-confirm");
  const discardKeep = document.getElementById("discard-keep");

  let record = null;
  let version = null;
  let dirty = false;
  let saving = false;
  let pendingLeave = null;
  let lastTyped = {};
  let expireInFlight = false;

  const titles = {
    participants: "Participant",
    artifacts: "Artifact",
    negotiations: "Negotiation",
    offers: "Offer"
  };

  function announce(text) {
    live.textContent = text;
  }

  function hide(el, on) {
    el.hidden = on;
  }

  function setSaving(on) {
    saving = on;
    saveBtn.disabled = on || !dirty;
    saveBtn.textContent = on ? "Saving…" : "Save";
    saveBtn.setAttribute("aria-busy", on ? "true" : "false");
  }

  function clearErrors() {
    hide(summary, true);
    summaryList.replaceChildren();
    fieldsEl.querySelectorAll("[aria-invalid='true']").forEach((el) => {
      el.removeAttribute("aria-invalid");
    });
    fieldsEl.querySelectorAll(".field-error").forEach((el) => el.remove());
  }

  function showSummary(entries) {
    summaryList.replaceChildren();
    entries.forEach(([name, message]) => {
      const li = document.createElement("li");
      const a = document.createElement("a");
      a.href = `#${name}`;
      a.textContent = message;
      li.appendChild(a);
      summaryList.appendChild(li);
    });
    hide(summary, false);
    summary.focus?.();
    summary.setAttribute("tabindex", "-1");
    summary.focus();
  }

  function fieldError(name, message) {
    const input = document.getElementById(name);
    if (!input) return;
    input.setAttribute("aria-invalid", "true");
    const p = document.createElement("p");
    p.className = "field-error";
    p.id = `${name}-error`;
    p.textContent = message;
    input.setAttribute("aria-describedby", p.id);
    input.after(p);
  }

  function formatEt(value) {
    if (!value) return "—";
    const dt = new Date(value);
    const text = new Intl.DateTimeFormat("en-US", {
      timeZone: "America/New_York",
      dateStyle: "medium",
      timeStyle: "short"
    }).format(dt) + " ET";
    return text;
  }

  function ro(label, value, name) {
    const wrap = document.createElement("div");
    const lab = document.createElement("p");
    lab.className = "ro-label";
    lab.textContent = label;
    const v = document.createElement("p");
    v.className = "ro";
    v.id = name ? `${name}-readonly` : undefined;
    v.textContent = value == null || value === "" ? "—" : String(value);
    wrap.append(lab, v);
    return wrap;
  }

  function inputField({ name, label, required, type, maxlength, value, extra }) {
    const wrap = document.createElement("div");
    const lab = document.createElement("label");
    lab.setAttribute("for", name);
    lab.textContent = label + " ";
    if (required) {
      const req = document.createElement("span");
      req.className = "req";
      req.textContent = "(required)";
      lab.appendChild(req);
    }
    const el = document.createElement(type === "textarea" ? "textarea" : type === "select" ? "select" : "input");
    el.id = name;
    el.name = name;
    if (type === "textarea") el.maxLength = maxlength || 4096;
    else if (type !== "select") {
      el.type = type || "text";
      if (maxlength) el.maxLength = maxlength;
    }
    if (value != null) el.value = value;
    if (extra) extra(el);
    el.addEventListener("input", markDirty);
    el.addEventListener("change", markDirty);
    wrap.append(lab, el);
    return wrap;
  }

  function markDirty() {
    dirty = true;
    if (!saving) saveBtn.disabled = false;
  }

  function readForm() {
    const data = {};
    if (type === "participants") {
      data.displayName = document.getElementById("displayName")?.value ?? "";
      data.isActive = document.getElementById("isActive")?.value === "true";
    } else if (type === "artifacts") {
      data.name = document.getElementById("name")?.value ?? "";
      data.description = document.getElementById("description")?.value ?? "";
      data.ownerParticipantId = document.getElementById("ownerParticipantId")?.value ?? "";
    } else if (type === "negotiations") {
      const status = document.getElementById("status");
      if (status && !status.disabled) data.status = status.value;
      const ends = document.getElementById("endsAt");
      if (ends && !ends.disabled) data.endsAt = ends.value ? new Date(ends.value).toISOString() : null;
    } else if (type === "offers") {
      const amount = document.getElementById("amount");
      const currency = document.getElementById("currency");
      const terms = document.getElementById("terms");
      const status = document.getElementById("status");
      if (amount && !amount.disabled) {
        data.amount = amount.value === "" ? null : Number(amount.value);
        data.currency = currency?.value || null;
        data.terms = terms?.value || null;
      }
      if (status && !status.disabled) data.status = status.value;
    }
    return data;
  }

  function clientValidate(data) {
    const errors = [];
    if (type === "participants" && data.displayName && data.displayName.length > 256) {
      errors.push(["displayName", "Enter a display name of at most 256 characters."]);
    }
    if (type === "artifacts") {
      if (!data.name) errors.push(["name", "Name is required."]);
      if (data.name && data.name.length > 4096) errors.push(["name", "Enter a name of at most 4096 characters."]);
      if (data.description && data.description.length > 4096) errors.push(["description", "Enter a description of at most 4096 characters."]);
    }
    if (type === "negotiations" && data.endsAt && record.startsAt && new Date(data.endsAt) <= new Date(record.startsAt)) {
      errors.push(["endsAt", "Ends at must be after Starts at."]);
    }
    if (type === "offers" && document.getElementById("amount") && !document.getElementById("amount").disabled) {
      if (data.amount != null && data.amount < 0) errors.push(["amount", "Amount must be 0 or more."]);
      if (data.amount != null && !/^\d+(\.\d{1,2})?$/.test(String(document.getElementById("amount").value))) {
        errors.push(["amount", "Amount can have at most 2 decimal places."]);
      }
      if (data.amount != null && !data.currency) errors.push(["currency", "Currency is required when Amount is set."]);
      if (data.currency && data.currency.length !== 3) errors.push(["currency", "Currency must be 3 letters."]);
      if (data.terms && data.terms.length > 2000) errors.push(["terms", "Terms can be at most 2000 characters."]);
      if (data.amount == null && !data.terms) {
        errors.push(["amount", "Enter an amount or terms."]);
        errors.push(["terms", "Enter an amount or terms."]);
      }
    }
    return errors;
  }

  function render() {
    fieldsEl.replaceChildren();
    recordId.textContent = `ID ${id}`;
    const deleted = !!record.deletedAt;
    hide(readonlyNote, !deleted);
    hide(form, false);
    dirty = false;
    saveBtn.disabled = true;
    saveBtn.textContent = "Save";

    if (type === "participants") {
      fieldsEl.append(
        ro("ID", record.id, "id"),
        ro("Sub", record.sub, "sub"),
        ro("Login email", record.loginEmail, "loginEmail"),
        ro("Contact email", record.contactEmail, "contactEmail"),
        ro("Created", formatEt(record.createdAt), "createdAt")
      );
      if (deleted) {
        fieldsEl.append(
          ro("Display name", record.displayName, "displayName"),
          ro("Status", record.isActive ? "Active" : "Suspended", "isActive")
        );
      } else {
        fieldsEl.append(
          inputField({ name: "displayName", label: "Display name", maxlength: 256, value: record.displayName || "" }),
          inputField({
            name: "isActive",
            label: "Status",
            required: true,
            type: "select",
            extra: (el) => {
              el.append(new Option("Active", "true"), new Option("Suspended", "false"));
              el.value = record.isActive ? "true" : "false";
            }
          })
        );
      }
    }

    if (type === "artifacts") {
      const entity = (record.entities && record.entities[0]) || {};
      fieldsEl.append(
        ro("ID", record.id, "id"),
        ro("Intent", record.intent, "intent"),
        ro("Created", formatEt(record.createdAt), "createdAt")
      );
      if (deleted) {
        fieldsEl.append(
          ro("Name", record.name || entity.name, "name"),
          ro("Description", entity.description, "description"),
          ro("Owner participant", record.ownerParticipantId, "ownerParticipantId")
        );
      } else {
        fieldsEl.append(
          inputField({ name: "name", label: "Name", required: true, maxlength: 4096, value: record.name || entity.name || "" }),
          inputField({ name: "description", label: "Description", type: "textarea", value: entity.description || "" }),
          ownerField(record.ownerParticipantId)
        );
      }
    }

    if (type === "negotiations") {
      const open = record.status === "Open" && !deleted;
      fieldsEl.append(
        ro("ID", record.id, "id"),
        ro("Artifact", record.artifactId, "artifactId"),
        ro("Party A", record.partyAParticipantId, "partyA"),
        ro("Party B", record.partyBParticipantId, "partyB"),
        ro("Starts at", formatEt(record.startsAt), "startsAt"),
        ro("Created", formatEt(record.createdAt), "createdAt")
      );
      if (open) {
        fieldsEl.append(
          inputField({
            name: "status",
            label: "Status",
            required: true,
            type: "select",
            extra: (el) => {
              el.append(new Option("Open", "Open"), new Option("Closed", "Closed"), new Option("Expired", "Expired"));
              el.value = "Open";
            }
          }),
          inputField({
            name: "endsAt",
            label: "Ends at",
            type: "datetime-local",
            extra: (el) => {
              if (record.endsAt) el.value = toLocalInput(record.endsAt);
            }
          })
        );
        hide(expireOpen, false);
      } else {
        fieldsEl.append(
          ro("Status", record.status, "status"),
          ro("Ends at", formatEt(record.endsAt), "endsAt")
        );
        hide(expireOpen, true);
      }
    }

    if (type === "offers") {
      const open = record.status === "Open" && !deleted;
      fieldsEl.append(
        ro("ID", record.id, "id"),
        ro("Negotiation ID", record.negotiationId, "negotiationId"),
        ro("From", record.fromParticipantId, "from"),
        ro("To", record.toParticipantId, "to"),
        ro("Created", formatEt(record.createdAt), "createdAt")
      );
      if (open) {
        fieldsEl.append(
          inputField({
            name: "amount",
            label: "Amount",
            type: "number",
            extra: (el) => {
              el.min = "0";
              el.step = "0.01";
              if (record.amount != null) el.value = record.amount;
            }
          }),
          inputField({
            name: "currency",
            label: "Currency",
            maxlength: 3,
            value: record.currency || "",
            extra: (el) => { el.maxLength = 3; }
          }),
          inputField({
            name: "terms",
            label: "Terms",
            type: "textarea",
            maxlength: 2000,
            value: record.terms || ""
          }),
          inputField({
            name: "status",
            label: "Status",
            required: true,
            type: "select",
            extra: (el) => {
              el.append(new Option("Open", "Open"), new Option("Cancelled", "Cancelled"));
              el.value = "Open";
            }
          })
        );
      } else {
        fieldsEl.append(
          ro("Amount", record.amount, "amount"),
          ro("Currency", record.currency, "currency"),
          ro("Terms", record.terms, "terms"),
          ro("Status", record.status === "Superseded" ? "Superseded (countered)" : record.status, "status")
        );
      }
    }

    if (deleted) {
      saveBtn.hidden = true;
      cancelBtn.hidden = true;
      hide(expireOpen, true);
    } else {
      saveBtn.hidden = false;
      cancelBtn.hidden = false;
    }
  }

  function ownerField(current) {
    const wrap = document.createElement("div");
    const lab = document.createElement("label");
    lab.setAttribute("for", "ownerSearch");
    lab.textContent = "Owner participant ";
    const req = document.createElement("span");
    req.className = "req";
    req.textContent = "(required)";
    lab.appendChild(req);
    const hidden = document.createElement("input");
    hidden.type = "hidden";
    hidden.id = "ownerParticipantId";
    hidden.name = "ownerParticipantId";
    hidden.value = current || "";
    const search = document.createElement("input");
    search.id = "ownerSearch";
    search.type = "search";
    search.setAttribute("autocomplete", "off");
    search.value = current || "";
    const list = document.createElement("ul");
    list.className = "picker";
    list.id = "owner-picker";
    list.hidden = true;
    search.addEventListener("input", async () => {
      markDirty();
      const q = search.value.trim();
      const res = await fetch(`/admin/api/participants?q=${encodeURIComponent(q)}&limit=50`, { credentials: "same-origin" });
      if (res.status === 401) return onUnauthorized();
      if (!res.ok) return;
      const page = await res.json();
      list.replaceChildren();
      (page.items || []).forEach((p) => {
        if (p.deletedAt) return;
        const li = document.createElement("li");
        const btn = document.createElement("button");
        btn.type = "button";
        btn.textContent = `${p.displayName || p.sub} (${p.sub})`;
        btn.addEventListener("click", () => {
          hidden.value = p.sub;
          search.value = p.displayName || p.sub;
          list.hidden = true;
          markDirty();
        });
        li.appendChild(btn);
        list.appendChild(li);
      });
      list.hidden = list.childElementCount === 0;
    });
    wrap.append(lab, search, hidden, list);
    return wrap;
  }

  function toLocalInput(iso) {
    const dt = new Date(iso);
    const tz = dt.getTime() - dt.getTimezoneOffset() * 60000;
    return new Date(tz).toISOString().slice(0, 16);
  }

  async function load() {
    hide(pageError, true);
    hide(conflict, true);
    const res = await fetch(api, { credentials: "same-origin" });
    if (res.status === 401) return onUnauthorized();
    if (res.status === 403) return onForbidden();
    if (!res.ok) {
      pageError.textContent = "We can't find that page.";
      hide(pageError, false);
      return;
    }
    record = await res.json();
    version = record.version;
    render();
  }

  function onUnauthorized() {
    window.location.href = "/admin/sign-in";
  }

  function onForbidden() {
    pageError.textContent = "You can't do that.";
    hide(pageError, false);
    hide(form, true);
  }

  async function submit(data) {
    lastTyped = data;
    setSaving(true);
    clearErrors();
    try {
      const res = await fetch(api, {
        method: "PATCH",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "If-Match": `"${version}"`
        },
        body: JSON.stringify(data)
      });
      if (res.status === 401) return onUnauthorized();
      if (res.status === 403) return onForbidden();
      if (res.status === 409) {
        hide(conflict, false);
        hide(typedRef, false);
        typedValues.textContent = JSON.stringify(data, null, 2);
        return;
      }
      if (res.status === 428 || res.status === 400) {
        const payload = await res.json().catch(() => ({}));
        if (payload.fields) {
          const entries = Object.entries(payload.fields);
          entries.forEach(([name, message]) => fieldError(name, message));
          showSummary(entries);
        } else {
          pageError.textContent = "You can't do that.";
          hide(pageError, false);
        }
        return;
      }
      if (!res.ok) {
        pageError.textContent = "You can't do that.";
        hide(pageError, false);
        return;
      }
      record = await res.json();
      version = record.version;
      dirty = false;
      render();
      announce("Changes saved.");
    } finally {
      setSaving(false);
    }
  }

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (saving || saveBtn.disabled) return;
    const data = readForm();
    if (type === "negotiations" && data.status === "Closed") {
      openModal(closeDialog, closeCancel);
      closeDialog.dataset.pending = JSON.stringify(data);
      return;
    }
    if (type === "negotiations" && data.status === "Expired") {
      openExpire();
      return;
    }
    const errors = clientValidate(data);
    if (errors.length) {
      clearErrors();
      errors.forEach(([name, message]) => fieldError(name, message));
      showSummary(errors);
      return;
    }
    await submit(data);
  });

  cancelBtn.addEventListener("click", () => {
    if (!dirty) {
      render();
      return;
    }
    pendingLeave = () => { dirty = false; load(); };
    openModal(discardDialog, discardKeep);
  });

  document.getElementById("reload").addEventListener("click", async () => {
    const kept = { ...lastTyped };
    await load();
    hide(conflict, false);
    hide(typedRef, false);
    typedValues.textContent = JSON.stringify(kept, null, 2);
  });

  function trapFocus(dialog, first) {
    const focusable = () => [...dialog.querySelectorAll("button, [href], input, select, textarea")].filter((el) => !el.hidden && !el.disabled);
    first.focus();
    dialog.onkeydown = (event) => {
      if (event.key === "Escape") {
        event.preventDefault();
        return;
      }
      if (event.key !== "Tab") return;
      const list = focusable();
      if (!list.length) return;
      const start = list[0];
      const end = list[list.length - 1];
      if (event.shiftKey && document.activeElement === start) {
        event.preventDefault();
        end.focus();
      } else if (!event.shiftKey && document.activeElement === end) {
        event.preventDefault();
        start.focus();
      }
    };
  }

  function openModal(dialog, initial) {
    hide(dialog, false);
    trapFocus(dialog, initial);
  }

  function closeModal(dialog) {
    hide(dialog, true);
    dialog.onkeydown = null;
  }

  function openExpire() {
    const count = record.openOfferCount ?? 0;
    expireTitle.textContent = `Expire negotiation ${id}?`;
    expireBody.textContent = count === 0
      ? "No open offers will be cancelled."
      : `${count} open offers will be cancelled.`;
    hide(expireError, true);
    hide(expireRetry, true);
    hide(expireConfirm, false);
    expireConfirm.disabled = false;
    expireConfirm.textContent = "Expire negotiation";
    openModal(expireDialog, expireCancel);
  }

  async function doExpire() {
    if (expireInFlight) return;
    expireInFlight = true;
    expireConfirm.disabled = true;
    expireRetry.disabled = true;
    expireConfirm.textContent = "Expiring…";
    expireRetry.textContent = "Expiring…";
    hide(expireError, true);
    try {
      const res = await fetch(api, {
        method: "PATCH",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "If-Match": `"${version}"`
        },
        body: JSON.stringify({ status: "Expired" })
      });
      if (res.status === 401) return onUnauthorized();
      if (!res.ok) {
        hide(expireError, false);
        hide(expireRetry, false);
        hide(expireConfirm, true);
        expireRetry.disabled = false;
        expireRetry.textContent = "Retry";
        expireRetry.focus();
        return;
      }
      record = await res.json();
      version = record.version;
      closeModal(expireDialog);
      render();
      announce(`Negotiation ${id} expired.`);
      expireOpen.focus();
    } finally {
      expireInFlight = false;
      expireConfirm.disabled = false;
      expireConfirm.textContent = "Expire negotiation";
    }
  }

  expireOpen.addEventListener("click", openExpire);
  expireCancel.addEventListener("click", () => closeModal(expireDialog));
  expireDialog.querySelector("[data-expire-dismiss]").addEventListener("click", () => closeModal(expireDialog));
  expireConfirm.addEventListener("click", doExpire);
  expireRetry.addEventListener("click", doExpire);
  expireDialog.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      event.preventDefault();
      closeModal(expireDialog);
    }
  });

  closeCancel.addEventListener("click", () => closeModal(closeDialog));
  closeDialog.querySelector("[data-close-dismiss]").addEventListener("click", () => closeModal(closeDialog));
  closeConfirm.addEventListener("click", async () => {
    const data = JSON.parse(closeDialog.dataset.pending || "{}");
    closeModal(closeDialog);
    await submit(data);
  });

  discardKeep.addEventListener("click", () => closeModal(discardDialog));
  discardDialog.querySelector("[data-discard-keep]").addEventListener("click", () => closeModal(discardDialog));
  discardConfirm.addEventListener("click", () => {
    closeModal(discardDialog);
    dirty = false;
    if (pendingLeave) pendingLeave();
  });

  window.addEventListener("beforeunload", (event) => {
    if (dirty) event.preventDefault();
  });

  document.querySelectorAll("nav a").forEach((link) => {
    link.addEventListener("click", (event) => {
      if (!dirty) return;
      event.preventDefault();
      pendingLeave = () => { window.location.href = link.href; };
      openModal(discardDialog, discardKeep);
    });
  });

  load();
})();
