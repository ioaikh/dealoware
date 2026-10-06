(function () {
    "use strict";

    var LIST_PATH = "/admin/audit";
    var API_PATH = "/admin/api/audit";
    var DEFAULT_LIMIT = 50;
    var MAX_LIMIT = 200;
    // Step 8 PR #32 @ db22afba: items, total, offset, limit, ipHmacPrefix only.
    var SECRET_KEYS = {
        password: true,
        passwordhash: true,
        totp: true,
        totpsecret: true,
        totpcode: true,
        recovery: true,
        recoverycode: true,
        recoverycodes: true,
        secret: true,
        token: true,
        apikey: true,
        ip: true,
        rawip: true,
        clientip: true,
        iphmac: true,
        iphash: true,
        hmac: true,
        hmackey: true,
        strategybody: true,
        turnstile: true,
        turnstiletoken: true
    };

    function $(id) {
        return document.getElementById(id);
    }

    function text(el, value) {
        el.textContent = value == null ? "" : String(value);
    }

    function el(name, attrs) {
        var node = document.createElement(name);
        if (attrs) {
            Object.keys(attrs).forEach(function (key) {
                if (key === "text") text(node, attrs[key]);
                else node.setAttribute(key, attrs[key]);
            });
        }
        return node;
    }

    function isSecretKey(key) {
        return SECRET_KEYS[String(key).replace(/[_-]/g, "").toLowerCase()] === true;
    }

    function looksLikeIp(value) {
        if (typeof value !== "string") return false;
        return /^(?:\d{1,3}\.){3}\d{1,3}$/.test(value.trim())
            || value.indexOf(":") !== -1 && /[0-9a-fA-F]{2,}:[0-9a-fA-F]{2,}/.test(value);
    }

    function parseSnapshot(raw) {
        if (!raw) return {};
        if (typeof raw === "object") return raw;
        try {
            var parsed = JSON.parse(raw);
            return parsed && typeof parsed === "object" ? parsed : {};
        } catch (err) {
            return {};
        }
    }

    function safeSnapshot(obj) {
        var out = {};
        if (!obj || typeof obj !== "object") return out;
        Object.keys(obj).forEach(function (key) {
            if (isSecretKey(key)) return;
            var value = obj[key];
            if (typeof value === "string" && looksLikeIp(value)) return;
            out[key] = value;
        });
        return out;
    }

    function formatEt(iso) {
        if (!iso) return { label: "", utc: "" };
        var date = new Date(iso);
        if (Number.isNaN(date.getTime())) return { label: String(iso), utc: String(iso) };
        var label = new Intl.DateTimeFormat("en-US", {
            timeZone: "America/New_York",
            month: "short",
            day: "numeric",
            year: "numeric",
            hour: "numeric",
            minute: "2-digit",
            hour12: true
        }).format(date) + " ET";
        return { label: label, utc: date.toISOString() };
    }

    function setTimeCell(cell, iso) {
        var formatted = formatEt(iso);
        var time = el("time", { datetime: iso || "" });
        text(time, formatted.label);
        time.title = formatted.utc ? "UTC " + formatted.utc : "";
        cell.appendChild(time);
    }

    function entityHref(type, id) {
        if (!type || !id) return "";
        var map = {
            Participant: "/admin/participants/",
            Artifact: "/admin/artifacts/",
            Negotiation: "/admin/negotiations/",
            Offer: "/admin/offers/"
        };
        return map[type] ? map[type] + encodeURIComponent(id) : "";
    }

    function actorOf(item) {
        return item.actorEmail || item.actor || "";
    }

    function outcomeOf(item) {
        if (item.reasonClass) return item.reasonClass;
        if (item.outcome) return item.outcome;
        return "success";
    }

    function parseListState() {
        var params = new URLSearchParams(window.location.search);
        var limit = Number(params.get("limit") || DEFAULT_LIMIT);
        if (!Number.isFinite(limit) || limit < 1) limit = DEFAULT_LIMIT;
        if (limit > MAX_LIMIT) limit = MAX_LIMIT;
        var offset = Number(params.get("offset") || 0);
        if (!Number.isFinite(offset) || offset < 0) offset = 0;
        return {
            action: params.get("action") || "",
            entityType: params.get("entityType") || "",
            from: params.get("from") || "",
            to: params.get("to") || "",
            limit: limit,
            offset: offset
        };
    }

    function writeListState(state, replace) {
        var params = new URLSearchParams();
        if (state.action) params.set("action", state.action);
        if (state.entityType) params.set("entityType", state.entityType);
        if (state.from) params.set("from", state.from);
        if (state.to) params.set("to", state.to);
        params.set("offset", String(state.offset));
        params.set("limit", String(state.limit));
        var url = LIST_PATH + "?" + params.toString();
        if (replace) window.history.replaceState(state, "", url);
        else window.history.pushState(state, "", url);
    }

    function queryFromState(state) {
        var params = new URLSearchParams();
        if (state.action) params.set("action", state.action);
        if (state.entityType) params.set("entityType", state.entityType);
        if (state.from) params.set("from", state.from);
        if (state.to) params.set("to", state.to);
        params.set("offset", String(state.offset));
        params.set("limit", String(state.limit));
        params.set("sort", "-timestamp");
        return params.toString();
    }

    function showError(message, retry) {
        var error = $("error");
        if (!error) return;
        error.hidden = false;
        while (error.firstChild) error.removeChild(error.firstChild);
        error.appendChild(document.createTextNode(message + " "));
        if (retry) {
            var button = el("button", { type: "button", class: "secondary", id: "retry" });
            text(button, "Retry");
            button.addEventListener("click", retry);
            error.appendChild(button);
        }
    }

    function hideError() {
        var error = $("error");
        if (!error) return;
        error.hidden = true;
        while (error.firstChild) error.removeChild(error.firstChild);
    }

    function bindNavMenu() {
        var button = document.querySelector(".nav-menu");
        var list = document.querySelector(".admin-nav ul");
        if (!button || !list) return;
        if (window.matchMedia("(max-width: 768px)").matches) {
            button.hidden = false;
            list.setAttribute("data-collapsed", "true");
            button.addEventListener("click", function () {
                var open = button.getAttribute("aria-expanded") === "true";
                button.setAttribute("aria-expanded", open ? "false" : "true");
                list.setAttribute("data-collapsed", open ? "true" : "false");
            });
        }
    }

    function canLinkEntity(item) {
        if (!item.entityId || !item.entityType) return false;
        if (item.entityExists === false) return false;
        if (item.action === "entity_delete") return false;
        return !!entityHref(item.entityType, item.entityId);
    }

    function renderRows(items) {
        var body = $("audit-rows");
        while (body.firstChild) body.removeChild(body.firstChild);
        items.forEach(function (item) {
            var row = el("tr");
            row.setAttribute("data-audit-id", item.id || "");
            var time = el("td");
            setTimeCell(time, item.timestamp);
            var actor = el("td");
            text(actor, actorOf(item));
            var action = el("td");
            var link = el("a", { href: LIST_PATH + "/" + encodeURIComponent(item.id || "") });
            text(link, item.action || "");
            action.appendChild(link);
            var type = el("td");
            text(type, item.entityType || "");
            var entity = el("td");
            if (canLinkEntity(item)) {
                var entityLink = el("a", { href: entityHref(item.entityType, item.entityId) });
                text(entityLink, item.entityId);
                entity.appendChild(entityLink);
            } else {
                text(entity, item.entityId || "");
            }
            var outcome = el("td");
            text(outcome, outcomeOf(item));
            row.appendChild(time);
            row.appendChild(actor);
            row.appendChild(action);
            row.appendChild(type);
            row.appendChild(entity);
            row.appendChild(outcome);
            body.appendChild(row);
        });
    }

    function renderSkeletons() {
        var body = $("audit-rows");
        while (body.firstChild) body.removeChild(body.firstChild);
        for (var i = 0; i < 3; i += 1) {
            var row = el("tr", { class: "skeleton" });
            for (var j = 0; j < 6; j += 1) {
                var cell = el("td");
                text(cell, "Loading");
                row.appendChild(cell);
            }
            body.appendChild(row);
        }
    }

    function hasActiveFilters(state) {
        return !!(state.action || state.entityType || state.from || state.to);
    }

    function loadList(state) {
        var table = $("audit-table");
        var empty = $("empty");
        var status = $("status");
        hideError();
        empty.hidden = true;
        table.setAttribute("aria-busy", "true");
        renderSkeletons();
        text(status, "Loading audit log");
        fetch(API_PATH + "?" + queryFromState(state), { credentials: "same-origin" })
            .then(function (response) {
                if (response.status === 401) {
                    throw new Error("signed-out");
                }
                if (!response.ok) throw new Error("load-failed");
                return response.json();
            })
            .then(function (payload) {
                table.setAttribute("aria-busy", "false");
                var items = Array.isArray(payload.items) ? payload.items : [];
                var total = Number(payload.total || 0);
                var offset = Number(payload.offset || state.offset);
                var limit = Number(payload.limit || state.limit);
                if (limit > MAX_LIMIT) limit = MAX_LIMIT;
                $("limit").value = String(limit === 100 || limit === 200 ? limit : 50);
                renderRows(items);
                if (items.length === 0) {
                    empty.hidden = false;
                    while (empty.firstChild) empty.removeChild(empty.firstChild);
                    if (hasActiveFilters(state)) {
                        text(empty, "No results match these filters.");
                        var clear = el("button", { type: "button", class: "secondary", id: "empty-clear" });
                        text(clear, "Clear filters");
                        clear.addEventListener("click", function () {
                            $("clear-filters").click();
                        });
                        empty.appendChild(document.createTextNode(" "));
                        empty.appendChild(clear);
                    } else {
                        text(empty, "No audit entries yet.");
                    }
                }
                var start = total === 0 ? 0 : offset + 1;
                var end = Math.min(offset + items.length, total);
                text($("page-summary"), "Showing " + start + "–" + end + " of " + total);
                text(status, total === 1 ? "1 result" : total + " results");
                $("prev-page").disabled = offset <= 0;
                $("next-page").disabled = offset + items.length >= total;
                state.offset = offset;
                state.limit = limit;
            })
            .catch(function (err) {
                table.setAttribute("aria-busy", "false");
                var body = $("audit-rows");
                while (body.firstChild) body.removeChild(body.firstChild);
                if (err && err.message === "signed-out") {
                    text(status, "");
                    showError("We couldn't load the audit log.", null);
                    return;
                }
                text(status, "");
                showError("We couldn't load the audit log.", function () { loadList(state); });
            });
    }

    function applyFiltersFromForm(state) {
        state.action = $("action").value;
        state.entityType = $("entityType").value;
        state.from = $("from").value;
        state.to = $("to").value;
        state.offset = 0;
        state.limit = Number($("limit").value || DEFAULT_LIMIT);
        if (state.limit > MAX_LIMIT) state.limit = MAX_LIMIT;
        writeListState(state, false);
        loadList(state);
    }

    function initList() {
        var state = parseListState();
        $("action").value = state.action;
        $("entityType").value = state.entityType;
        $("from").value = state.from;
        $("to").value = state.to;
        $("limit").value = String(state.limit === 100 || state.limit === 200 ? state.limit : 50);
        writeListState(state, true);
        $("audit-filters").addEventListener("submit", function (event) {
            event.preventDefault();
            applyFiltersFromForm(state);
        });
        $("clear-filters").addEventListener("click", function () {
            state.action = "";
            state.entityType = "";
            state.from = "";
            state.to = "";
            state.offset = 0;
            $("action").value = "";
            $("entityType").value = "";
            $("from").value = "";
            $("to").value = "";
            writeListState(state, false);
            loadList(state);
        });
        $("limit").addEventListener("change", function () {
            state.limit = Number($("limit").value || DEFAULT_LIMIT);
            if (state.limit > MAX_LIMIT) state.limit = MAX_LIMIT;
            state.offset = 0;
            writeListState(state, false);
            loadList(state);
        });
        $("prev-page").addEventListener("click", function () {
            state.offset = Math.max(0, state.offset - state.limit);
            writeListState(state, false);
            loadList(state);
        });
        $("next-page").addEventListener("click", function () {
            state.offset = state.offset + state.limit;
            writeListState(state, false);
            loadList(state);
        });
        window.addEventListener("popstate", function () {
            state = parseListState();
            $("action").value = state.action;
            $("entityType").value = state.entityType;
            $("from").value = state.from;
            $("to").value = state.to;
            $("limit").value = String(state.limit === 100 || state.limit === 200 ? state.limit : 50);
            loadList(state);
        });
        loadList(state);
    }

    function addSummaryRow(dl, term, valueNode) {
        var dt = el("dt");
        text(dt, term);
        var dd = el("dd");
        dd.appendChild(valueNode);
        dl.appendChild(dt);
        dl.appendChild(dd);
    }

    function renderSnapshot(title, values, changedKeys) {
        var panel = el("div", { class: "snapshot-panel" });
        var heading = el("h3");
        text(heading, title);
        panel.appendChild(heading);
        var list = el("dl");
        var keys = Object.keys(values);
        if (keys.length === 0) {
            var empty = el("p");
            text(empty, "No allowed fields.");
            panel.appendChild(empty);
            return panel;
        }
        keys.forEach(function (key) {
            var dt = el("dt");
            text(dt, key);
            var dd = el("dd");
            if (changedKeys[key]) dd.className = "changed";
            text(dd, values[key] == null ? "" : String(values[key]));
            if (changedKeys[key]) {
                var flag = el("span", { class: "changed-flag" });
                text(flag, "Changed");
                dd.appendChild(flag);
            }
            list.appendChild(dt);
            list.appendChild(dd);
        });
        panel.appendChild(list);
        return panel;
    }

    function hmacPrefixOf(item) {
        var prefix = item.ipHmacPrefix;
        if (!prefix || typeof prefix !== "string") return "";
        if (looksLikeIp(prefix)) return "";
        return prefix.slice(0, 12);
    }

    function initDetail() {
        var parts = window.location.pathname.split("/").filter(Boolean);
        var id = parts[parts.length - 1];
        var back = $("back-to-list");
        if (window.location.search) back.setAttribute("href", LIST_PATH + window.location.search);
        hideError();
        text($("status"), "Loading audit entry");
        fetch(API_PATH + "/" + encodeURIComponent(id), { credentials: "same-origin" })
            .then(function (response) {
                if (response.status === 404) throw new Error("not-found");
                if (!response.ok) throw new Error("load-failed");
                return response.json();
            })
            .then(function (item) {
                text($("status"), "");
                var dl = $("summary-fields");
                while (dl.firstChild) dl.removeChild(dl.firstChild);
                var timeValue = el("span");
                setTimeCell(timeValue, item.timestamp);
                addSummaryRow(dl, "Time", timeValue);
                addSummaryRow(dl, "Actor", el("span", { text: actorOf(item) }));
                addSummaryRow(dl, "Action", el("span", { text: item.action || "" }));
                addSummaryRow(dl, "Entity type", el("span", { text: item.entityType || "" }));
                var entityNode = el("span");
                if (canLinkEntity(item)) {
                    var link = el("a", { href: entityHref(item.entityType, item.entityId) });
                    text(link, item.entityId);
                    entityNode.appendChild(link);
                } else {
                    text(entityNode, item.entityId || "");
                }
                addSummaryRow(dl, "Entity ID", entityNode);
                addSummaryRow(dl, "Outcome", el("span", { text: outcomeOf(item) }));
                var hmac = hmacPrefixOf(item);
                if (hmac) addSummaryRow(dl, "IP HMAC prefix", el("span", { text: hmac }));
                var before = safeSnapshot(parseSnapshot(item.beforeSnapshot || item.before));
                var after = safeSnapshot(parseSnapshot(item.afterSnapshot || item.after));
                var changed = {};
                Object.keys(before).concat(Object.keys(after)).forEach(function (key) {
                    if (String(before[key]) !== String(after[key])) changed[key] = true;
                });
                var grid = $("snapshot-grid");
                while (grid.firstChild) grid.removeChild(grid.firstChild);
                grid.appendChild(renderSnapshot("Before", before, changed));
                grid.appendChild(renderSnapshot("After", after, changed));
            })
            .catch(function (err) {
                text($("status"), "");
                if (err && err.message === "not-found") {
                    showError("We can't find that page.", null);
                    var stats = el("a", { href: "/admin" });
                    text(stats, "Stats");
                    $("error").appendChild(document.createTextNode(" "));
                    $("error").appendChild(stats);
                    return;
                }
                showError("We couldn't load the audit log.", function () { initDetail(); });
            });
    }

    bindNavMenu();
    var screen = document.body.getAttribute("data-admin-screen");
    if (screen === "S-D1") initList();
    if (screen === "S-D2") initDetail();
})();
