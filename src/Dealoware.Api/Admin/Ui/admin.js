(function () {
    "use strict";

    var ET = "America/New_York";
    var live = document.getElementById("live");
    var app = document.getElementById("app");
    var titleEl = document.getElementById("page-title");
    var menuToggle = document.getElementById("menu-toggle");
    var nav = document.getElementById("site-nav");
    var searchTimer = null;

    var LISTS = {
        participants: {
            path: "/admin/participants",
            api: "/admin/api/participants",
            typeLabel: "participants",
            searchLabel: "Search by display name or Core identifier",
            columns: [
                { key: "displayName", label: "Name", sort: "name" },
                { key: "id", label: "ID" },
                { key: "status", label: "Status" },
                { key: "createdAt", label: "Created", sort: "created" },
                { key: "updatedAt", label: "Updated", sort: "updated" }
            ],
            statuses: ["Active", "Suspended"],
            filters: { status: true, created: true, updated: true }
        },
        artifacts: {
            path: "/admin/artifacts",
            api: "/admin/api/artifacts",
            typeLabel: "artifacts",
            searchLabel: "Search by artifact name",
            columns: [
                { key: "name", label: "Name", sort: "name" },
                { key: "ownerParticipantId", label: "Owner participant" },
                { key: "createdAt", label: "Created", sort: "created" },
                { key: "updatedAt", label: "Updated", sort: "updated" }
            ],
            filters: { owner: true, created: true, updated: true }
        },
        negotiations: {
            path: "/admin/negotiations",
            api: "/admin/api/negotiations",
            typeLabel: "negotiations",
            searchLabel: "Search by artifact or participant name",
            columns: [
                { key: "id", label: "ID" },
                { key: "artifactId", label: "Artifact" },
                { key: "participants", label: "Participants" },
                { key: "status", label: "Status", sort: "status" },
                { key: "createdAt", label: "Created", sort: "created" },
                { key: "updatedAt", label: "Updated", sort: "updated" }
            ],
            statuses: ["Open", "Closed", "Expired"],
            filters: { status: true, participant: true, artifact: true, created: true, updated: true, negotiationId: true }
        },
        offers: {
            path: "/admin/offers",
            api: "/admin/api/offers",
            typeLabel: "offers",
            searchLabel: "Search by artifact, offering participant, or negotiation id",
            columns: [
                { key: "id", label: "ID" },
                { key: "negotiationId", label: "Negotiation ID" },
                { key: "artifactId", label: "Artifact" },
                { key: "fromParticipantId", label: "Offering participant" },
                { key: "amount", label: "Amount", sort: "amount" },
                { key: "status", label: "Status", sort: "status" },
                { key: "createdAt", label: "Created", sort: "created" },
                { key: "updatedAt", label: "Updated", sort: "updated" }
            ],
            statuses: ["Open", "Accepted", "Declined", "Withdrawn", "Superseded", "Cancelled"],
            filters: { status: true, participant: true, artifact: true, amount: true, negotiationId: true, created: true, updated: true }
        }
    };

    function announce(text) {
        live.textContent = text || "";
    }

    function setTitle(screen) {
        document.title = screen + " · Dealoware admin";
        titleEl.textContent = screen;
    }

    function has(obj, key) {
        return obj != null && Object.prototype.hasOwnProperty.call(obj, key);
    }

    function text(el, value) {
        el.textContent = value == null ? "" : String(value);
    }

    function el(tag, attrs, children) {
        var node = document.createElement(tag);
        if (attrs) {
            Object.keys(attrs).forEach(function (k) {
                if (k === "className") node.className = attrs[k];
                else if (k === "text") node.textContent = attrs[k];
                else if (k.slice(0, 2) === "on") node.addEventListener(k.slice(2).toLowerCase(), attrs[k]);
                else if (attrs[k] === false || attrs[k] == null) return;
                else node.setAttribute(k, attrs[k] === true ? "" : String(attrs[k]));
            });
        }
        (children || []).forEach(function (c) {
            if (c) node.appendChild(c);
        });
        return node;
    }

    function formatEt(iso) {
        if (!iso) return "";
        var d = new Date(iso);
        if (isNaN(d.getTime())) return "";
        var fmt = new Intl.DateTimeFormat("en-US", {
            timeZone: ET,
            month: "short",
            day: "numeric",
            year: "numeric",
            hour: "numeric",
            minute: "2-digit"
        });
        return fmt.format(d) + " ET";
    }

    function timeNode(iso) {
        if (!iso) return document.createTextNode("");
        var label = formatEt(iso);
        return el("time", { datetime: iso, title: String(iso) }, [document.createTextNode(label)]);
    }

    function statusLabel(value, type) {
        if (value === "Superseded") return "Superseded (countered)";
        if (type === "participants") {
            if (value === true || value === "Active") return "Active";
            if (value === false || value === "Suspended") return "Suspended";
        }
        return value == null ? "" : String(value);
    }

    function badge(label) {
        return el("span", { className: "badge", text: label });
    }

    function parsePath() {
        var parts = location.pathname.replace(/\/+$/, "").split("/").filter(Boolean);
        if (parts[0] !== "admin") return { kind: "unknown" };
        if (parts.length === 1) return { kind: "stats" }; // /admin and /admin/
        var type = parts[1];
        if (type === "sign-out") return { kind: "stats" };
        if (type === "audit" || (type === "settings" && parts[2] === "security")) return { kind: "notfound" };
        if (["participants", "artifacts", "negotiations", "offers"].indexOf(type) >= 0) {
            if (parts.length === 2) return { kind: "list", type: type };
            if (parts.length === 3) return { kind: "detail", type: type, id: parts[2] };
        }
        return { kind: "notfound" };
    }

    function queryState() {
        var p = new URLSearchParams(location.search);
        var limit = parseInt(p.get("limit") || "50", 10);
        if (limit !== 50 && limit !== 100 && limit !== 200) limit = Math.min(limit || 50, 200);
        if (limit > 200) limit = 200;
        if (limit !== 50 && limit !== 100 && limit !== 200) limit = 200;
        var offset = parseInt(p.get("offset") || "0", 10);
        if (isNaN(offset) || offset < 0) offset = 0;
        return {
            q: p.get("q") || "",
            sort: p.get("sort") || "updated",
            dir: p.get("dir") || "desc",
            offset: offset,
            limit: limit,
            includeDeleted: p.get("includeDeleted") === "true",
            status: p.get("status") || "",
            participant: p.get("participant") || "",
            artifact: p.get("artifact") || p.get("artifactId") || "",
            negotiationId: p.get("negotiationId") || "",
            amountMin: p.get("amountMin") || "",
            amountMax: p.get("amountMax") || "",
            createdFrom: p.get("createdFrom") || "",
            createdTo: p.get("createdTo") || "",
            updatedFrom: p.get("updatedFrom") || "",
            updatedTo: p.get("updatedTo") || ""
        };
    }

    function writeState(next, push) {
        var p = new URLSearchParams();
        Object.keys(next).forEach(function (k) {
            var v = next[k];
            if (v === false || v == null || v === "") return;
            p.set(k, String(v));
        });
        var url = location.pathname + (p.toString() ? "?" + p.toString() : "");
        if (push) history.pushState(next, "", url);
        else history.replaceState(next, "", url);
    }

    function apiQuery(state, extras) {
        var p = new URLSearchParams();
        var src = Object.assign({}, state, extras || {});
        if (src.q) p.set("q", src.q);
        p.set("sort", src.sort || "updated");
        p.set("dir", src.dir || "desc");
        p.set("offset", String(src.offset || 0));
        var limit = src.limit || 50;
        if (limit > 200) limit = 200;
        p.set("limit", String(limit));
        if (src.includeDeleted) p.set("includeDeleted", "true");
        if (src.status) p.set("status", src.status);
        if (src.participant) p.set("participant", src.participant);
        if (src.artifact) p.set("artifact", src.artifact);
        if (src.negotiationId) p.set("negotiationId", src.negotiationId);
        if (src.amountMin) p.set("amountMin", src.amountMin);
        if (src.amountMax) p.set("amountMax", src.amountMax);
        if (src.createdFrom) p.set("createdFrom", src.createdFrom);
        if (src.createdTo) p.set("createdTo", src.createdTo);
        if (src.updatedFrom) p.set("updatedFrom", src.updatedFrom);
        if (src.updatedTo) p.set("updatedTo", src.updatedTo);
        return p.toString();
    }

    function handleAuth(res) {
        if (res.status === 401) {
            renderSignedOut();
            return true;
        }
        if (res.status === 403) {
            renderDenied();
            return true;
        }
        return false;
    }

    function clearData() {
        while (app.firstChild) app.removeChild(app.firstChild);
    }

    function renderSignedOut() {
        clearData();
        setTitle("Sign in required");
        announce("");
        nav.hidden = true;
        menuToggle.hidden = true;
        app.appendChild(el("p", { className: "notice", text: "Sign in required." }));
    }

    function renderDenied() {
        clearData();
        setTitle("Not allowed");
        app.appendChild(el("p", { className: "notice", text: "You can't do that." }));
        app.appendChild(el("p", null, [el("a", { href: "/admin/", text: "Stats" })]));
    }

    function renderNotFound() {
        clearData();
        setTitle("Not found");
        app.appendChild(el("p", { className: "notice", text: "We can't find that page." }));
        app.appendChild(el("p", null, [el("a", { href: "/admin/", text: "Stats" })]));
    }

    function markNav(current) {
        nav.querySelectorAll("[data-nav]").forEach(function (a) {
            if (a.getAttribute("data-nav") === current) a.setAttribute("aria-current", "page");
            else a.removeAttribute("aria-current");
        });
    }

    function fieldOrEmpty(item, key) {
        return has(item, key) ? item[key] : undefined;
    }

    function cellFor(type, col, item) {
        if (col.key === "status") {
            var wrap = el("span");
            if (type === "participants" && has(item, "isActive")) wrap.appendChild(badge(item.isActive ? "Active" : "Suspended"));
            else if (has(item, "status")) wrap.appendChild(badge(statusLabel(item.status, type)));
            if (has(item, "deletedAt") && item.deletedAt) {
                wrap.appendChild(badge("Deleted"));
                wrap.appendChild(timeNode(item.deletedAt));
            }
            return wrap;
        }
        if (col.key === "participants") {
            var parts = [];
            if (has(item, "partyAParticipantId")) parts.push(item.partyAParticipantId);
            if (has(item, "partyBParticipantId")) parts.push(item.partyBParticipantId);
            return el("span", { text: parts.join(" · ") });
        }
        if (col.key === "amount") {
            if (!has(item, "amount") && !has(item, "currency")) return el("span");
            var amt = has(item, "amount") && item.amount != null ? String(item.amount) : "";
            var cur = has(item, "currency") && item.currency ? String(item.currency) : "";
            return el("span", { text: (amt + " " + cur).trim() });
        }
        if (col.key === "createdAt" || col.key === "updatedAt") {
            var value = fieldOrEmpty(item, col.key);
            if (col.key === "updatedAt" && (value == null) && has(item, "createdAt")) value = item.createdAt;
            return timeNode(value);
        }
        if (col.key === "id" || col.key === "negotiationId" || col.key === "artifactId") {
            var idVal = fieldOrEmpty(item, col.key);
            if (idVal == null) return el("span");
            var href = col.key === "id"
                ? LISTS[type].path + "/" + idVal
                : (col.key === "negotiationId" ? "/admin/negotiations/" + idVal : "/admin/artifacts/" + idVal);
            return el("a", { href: href, text: String(idVal) });
        }
        var raw = fieldOrEmpty(item, col.key);
        return el("span", { text: raw == null ? "" : String(raw) });
    }

    function hasActiveFilters(state) {
        return !!(state.q || state.status || state.participant || state.artifact || state.negotiationId
            || state.amountMin || state.amountMax || state.createdFrom || state.createdTo
            || state.updatedFrom || state.updatedTo);
    }

    function chips(state, spec, onClearOne, onClearAll) {
        var box = el("div", { className: "chips", id: "filter-chips" });
        var keys = [
            ["q", "Search"],
            ["status", "Status"],
            ["participant", "Participant"],
            ["artifact", "Artifact"],
            ["negotiationId", "Negotiation ID"],
            ["amountMin", "Amount min"],
            ["amountMax", "Amount max"],
            ["createdFrom", "Created from"],
            ["createdTo", "Created to"],
            ["updatedFrom", "Updated from"],
            ["updatedTo", "Updated to"]
        ];
        var any = false;
        keys.forEach(function (pair) {
            if (!state[pair[0]]) return;
            any = true;
            var btn = el("button", { type: "button", text: "Remove", "aria-label": "Remove " + pair[1] + " filter" });
            btn.addEventListener("click", function () { onClearOne(pair[0]); });
            box.appendChild(el("span", { className: "chip" }, [
                el("span", { text: pair[1] + ": " + state[pair[0]] }),
                btn
            ]));
        });
        if (any) {
            var clear = el("button", { type: "button", text: "Clear all filters" });
            clear.addEventListener("click", onClearAll);
            box.appendChild(clear);
        }
        return box;
    }

    function renderList(type) {
        var spec = LISTS[type];
        setTitle(type.charAt(0).toUpperCase() + type.slice(1));
        markNav(type);
        clearData();
        var state = queryState();

        var search = el("input", { id: "search", type: "search", value: state.q });
        var searchLabel = el("label", { for: "search" }, [
            el("span", { text: spec.searchLabel }),
            search
        ]);
        var clearSearch = el("button", { type: "button", text: "Clear" });
        var include = el("input", { id: "include-deleted", type: "checkbox" });
        include.checked = state.includeDeleted;
        var includeLabel = el("label", { for: "include-deleted" }, [
            el("span", { text: "Show deleted" }),
            include
        ]);
        var toolbar = el("div", { className: "toolbar" }, [searchLabel, clearSearch, includeLabel]);
        app.appendChild(toolbar);

        var filters = el("div", { className: "filters" });
        if (spec.filters.status) {
            var statusBox = el("fieldset");
            statusBox.appendChild(el("legend", { text: "Status" }));
            spec.statuses.forEach(function (s) {
                var id = "st-" + s;
                var cb = el("input", { type: "checkbox", id: id, value: s });
                cb.checked = state.status === s;
                cb.addEventListener("change", function () {
                    state.status = cb.checked ? s : "";
                    spec.statuses.forEach(function (other) {
                        if (other !== s) {
                            var o = document.getElementById("st-" + other);
                            if (o) o.checked = false;
                        }
                    });
                    state.offset = 0;
                    writeState(state, true);
                    loadList(type, spec, state, tableBody, meta);
                });
                statusBox.appendChild(el("label", { for: id }, [cb, el("span", { text: statusLabel(s, type) })]));
            });
            filters.appendChild(statusBox);
        }
        function addTextFilter(id, label, key) {
            var input = el("input", { id: id, type: "text", value: state[key] || "" });
            input.addEventListener("change", function () {
                state[key] = input.value.trim();
                state.offset = 0;
                writeState(state, true);
                loadList(type, spec, state, tableBody, meta);
            });
            filters.appendChild(el("label", { for: id }, [el("span", { text: label }), input]));
            return input;
        }
        function addTypeahead(id, label, key, api, pick) {
            var input = el("input", { id: id, type: "text", value: state[key] || "", autocomplete: "off", role: "combobox", "aria-autocomplete": "list", "aria-expanded": "false", "aria-controls": id + "-suggest" });
            var list = el("div", { className: "suggest", id: id + "-suggest", role: "listbox" });
            list.hidden = true;
            var box = el("label", { for: id }, [el("span", { text: label }), input, list]);
            var t = null;
            function search(q) {
                if (!q) { list.hidden = true; input.setAttribute("aria-expanded", "false"); return; }
                fetch(api + "?q=" + encodeURIComponent(q) + "&limit=50&sort=updated&dir=desc", { credentials: "same-origin" })
                    .then(function (res) { return res.ok ? res.json() : null; })
                    .then(function (data) {
                        while (list.firstChild) list.removeChild(list.firstChild);
                        if (!data || !data.items) { list.hidden = true; input.setAttribute("aria-expanded", "false"); return; }
                        data.items.slice(0, 50).forEach(function (item) {
                            var opt = el("div", { role: "option", tabindex: "0", text: pick(item) });
                            opt.addEventListener("click", function () {
                                input.value = pick(item);
                                state[key] = pickValue(item);
                                state.offset = 0;
                                list.hidden = true;
                                input.setAttribute("aria-expanded", "false");
                                writeState(state, true);
                                loadList(type, spec, state, tableBody, meta);
                            });
                            list.appendChild(opt);
                        });
                        list.hidden = list.childNodes.length === 0;
                        input.setAttribute("aria-expanded", list.hidden ? "false" : "true");
                    });
            }
            function pickValue(item) {
                if (api.indexOf("participants") >= 0) return item.sub || item.id;
                return item.id;
            }
            input.addEventListener("input", function () {
                if (t) clearTimeout(t);
                t = setTimeout(function () { search(input.value.trim()); }, 300);
            });
            filters.appendChild(box);
            return input;
        }
        if (spec.filters.participant) addTypeahead("filter-participant", "Participant", "participant", "/admin/api/participants", function (i) { return i.displayName || i.sub || i.id; });
        if (spec.filters.owner) addTypeahead("filter-owner", "Owner participant", "participant", "/admin/api/participants", function (i) { return i.displayName || i.sub || i.id; });
        if (spec.filters.artifact) addTypeahead("filter-artifact", "Artifact", "artifact", "/admin/api/artifacts", function (i) { return i.name || i.id; });
        if (spec.filters.negotiationId) addTextFilter("filter-neg", "Negotiation ID", "negotiationId");
        if (spec.filters.amount) {
            addTextFilter("amount-min", "Amount min", "amountMin");
            addTextFilter("amount-max", "Amount max", "amountMax");
        }
        if (spec.filters.created) {
            addTextFilter("created-from", "Created from", "createdFrom");
            addTextFilter("created-to", "Created to", "createdTo");
        }
        if (spec.filters.updated) {
            addTextFilter("updated-from", "Updated from", "updatedFrom");
            addTextFilter("updated-to", "Updated to", "updatedTo");
        }
        app.appendChild(filters);

        var chipHost = el("div");
        app.appendChild(chipHost);
        function refreshChips() {
            while (chipHost.firstChild) chipHost.removeChild(chipHost.firstChild);
            chipHost.appendChild(chips(state, spec, function (key) {
                state[key] = "";
                state.offset = 0;
                writeState(state, true);
                loadList(type, spec, state, tableBody, meta);
                renderList(type);
            }, function () {
                ["q", "status", "participant", "artifact", "negotiationId", "amountMin", "amountMax",
                    "createdFrom", "createdTo", "updatedFrom", "updatedTo"].forEach(function (k) { state[k] = ""; });
                state.offset = 0;
                writeState(state, true);
                renderList(type);
            }));
        }
        refreshChips();

        var table = el("table");
        var caption = el("caption", { text: spec.typeLabel });
        var thead = el("thead");
        var hr = el("tr");
        spec.columns.forEach(function (col) {
            var th = el("th", { scope: "col" });
            if (col.sort) {
                var btn = el("button", { type: "button", text: col.label });
                if (state.sort === col.sort) th.setAttribute("aria-sort", state.dir === "asc" ? "ascending" : "descending");
                btn.appendChild(el("span", { className: "visually-hidden", text: " sort" }));
                btn.addEventListener("click", function () {
                    if (state.sort === col.sort) state.dir = state.dir === "asc" ? "desc" : "asc";
                    else { state.sort = col.sort; state.dir = "desc"; }
                    state.offset = 0;
                    writeState(state, true);
                    renderList(type);
                });
                th.appendChild(btn);
            } else {
                text(th, col.label);
            }
            hr.appendChild(th);
        });
        thead.appendChild(hr);
        var tableBody = el("tbody", { id: "list-body" });
        table.appendChild(caption);
        table.appendChild(thead);
        table.appendChild(tableBody);
        app.appendChild(el("div", { className: "table-wrap", role: "region", "aria-label": spec.typeLabel + " table" }, [table]));

        var meta = el("div", { className: "pager", id: "pager" });
        app.appendChild(meta);

        function runSearch() {
            state.q = search.value.trim();
            state.offset = 0;
            writeState(state, true);
            loadList(type, spec, state, tableBody, meta);
            refreshChips();
        }
        search.addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                if (searchTimer) clearTimeout(searchTimer);
                runSearch();
            }
        });
        search.addEventListener("input", function () {
            if (searchTimer) clearTimeout(searchTimer);
            searchTimer = setTimeout(runSearch, 300);
        });
        clearSearch.addEventListener("click", function () {
            search.value = "";
            runSearch();
        });
        include.addEventListener("change", function () {
            state.includeDeleted = include.checked;
            state.offset = 0;
            writeState(state, true);
            loadList(type, spec, state, tableBody, meta);
        });

        loadList(type, spec, state, tableBody, meta);
    }

    function loadList(type, spec, state, tableBody, meta) {
        while (tableBody.firstChild) tableBody.removeChild(tableBody.firstChild);
        for (var i = 0; i < 5; i++) {
            var sk = el("tr");
            spec.columns.forEach(function () { sk.appendChild(el("td", null, [el("div", { className: "sk" })])); });
            tableBody.appendChild(sk);
        }
        tableBody.setAttribute("aria-busy", "true");
        var url = spec.api + "?" + apiQuery(state);
        fetch(url, { credentials: "same-origin" }).then(function (res) {
            if (handleAuth(res)) return null;
            if (!res.ok) throw new Error("load");
            return res.json();
        }).then(function (data) {
            if (!data) return;
            tableBody.removeAttribute("aria-busy");
            while (tableBody.firstChild) tableBody.removeChild(tableBody.firstChild);
            var items = data.items || [];
            var total = data.total || 0;
            var limit = data.limit || state.limit;
            var offset = data.offset || 0;
            if (limit !== state.limit) {
                state.limit = limit;
                writeState(state, false);
            }
            if (total === 0 && !hasActiveFilters(state)) {
                tableBody.appendChild(el("tr", null, [
                    el("td", { colspan: String(spec.columns.length), className: "empty", text: "No " + spec.typeLabel + " yet." })
                ]));
            } else if (total === 0) {
                var miss = el("td", { colspan: String(spec.columns.length), className: "empty" });
                miss.appendChild(el("p", { text: "No results match these filters." }));
                var clear = el("button", { type: "button", text: "Clear filters" });
                clear.addEventListener("click", function () {
                    location.href = spec.path;
                });
                miss.appendChild(clear);
                tableBody.appendChild(el("tr", null, [miss]));
            } else {
                items.forEach(function (item) {
                    var tr = el("tr");
                    spec.columns.forEach(function (col) { tr.appendChild(el("td", null, [cellFor(type, col, item)])); });
                    tableBody.appendChild(tr);
                });
            }
            announce(total + " results");
            renderPager(meta, spec, state, offset, limit, total, tableBody);
        }).catch(function () {
            tableBody.removeAttribute("aria-busy");
            while (tableBody.firstChild) tableBody.removeChild(tableBody.firstChild);
            var td = el("td", { colspan: String(spec.columns.length), className: "error" });
            td.appendChild(el("p", { text: "We couldn't load " + spec.typeLabel + "." }));
            var retry = el("button", { type: "button", text: "Retry" });
            retry.addEventListener("click", function () { loadList(type, spec, state, tableBody, meta); });
            td.appendChild(retry);
            tableBody.appendChild(el("tr", null, [td]));
        });
    }

    function renderPager(meta, spec, state, offset, limit, total, tableBody) {
        while (meta.firstChild) meta.removeChild(meta.firstChild);
        var start = total === 0 ? 0 : offset + 1;
        var end = Math.min(offset + limit, total);
        meta.appendChild(el("p", { text: "Showing " + start.toLocaleString("en-US") + "–" + end.toLocaleString("en-US") + " of " + total.toLocaleString("en-US") }));
        var size = el("select", { id: "page-size" });
        [50, 100, 200].forEach(function (n) {
            var opt = el("option", { value: String(n), text: String(n) });
            if (n === limit) opt.selected = true;
            size.appendChild(opt);
        });
        size.addEventListener("change", function () {
            state.limit = parseInt(size.value, 10);
            state.offset = 0;
            writeState(state, true);
            loadList(typeOf(spec), spec, state, tableBody, meta);
        });
        meta.appendChild(el("label", { for: "page-size" }, [el("span", { text: "Page size" }), size]));
        var pages = Math.max(1, Math.ceil(total / limit));
        var page = Math.floor(offset / limit) + 1;
        if (page > pages && total > 0) {
            state.offset = (pages - 1) * limit;
            writeState(state, false);
            loadList(typeOf(spec), spec, state, tableBody, meta);
            return;
        }
        function go(toPage) {
            state.offset = (toPage - 1) * limit;
            writeState(state, true);
            loadList(typeOf(spec), spec, state, tableBody, meta);
        }
        var prev = el("button", { type: "button", text: "Previous" });
        prev.disabled = page <= 1;
        prev.addEventListener("click", function () { go(page - 1); });
        var next = el("button", { type: "button", text: "Next" });
        next.disabled = page >= pages;
        next.addEventListener("click", function () { go(page + 1); });
        meta.appendChild(prev);
        if (prev.disabled) meta.appendChild(el("span", { className: "reason", text: "On first page" }));
        meta.appendChild(el("span", { text: "Page " + page }));
        meta.appendChild(next);
        for (var n = 1; n <= pages && n <= 7; n++) {
            (function (num) {
                var b = el("button", { type: "button", text: String(num) });
                b.setAttribute("aria-label", "Page " + num);
                if (num === page) b.setAttribute("aria-current", "page");
                b.addEventListener("click", function () { go(num); });
                meta.appendChild(b);
            })(n);
        }
        if (next.disabled) meta.appendChild(el("span", { className: "reason", text: "On last page" }));
    }

    function typeOf(spec) {
        return Object.keys(LISTS).filter(function (k) { return LISTS[k] === spec; })[0];
    }

    function renderStats() {
        setTitle("Stats");
        markNav("stats");
        clearData();
        var refresh = el("button", { type: "button", text: "Refresh" });
        app.appendChild(refresh);
        var tiles = el("div", { className: "tiles", id: "stats-tiles", role: "region", "aria-label": "Stats" });
        app.appendChild(tiles);
        var defs = [
            { key: "participants", label: "Participants", href: "/admin/participants" },
            { key: "openNegotiations", label: "Open negotiations", href: "/admin/negotiations?status=Open" },
            { key: "offers", label: "Offers", href: "/admin/offers" },
            { key: "accepts", label: "Accepts", href: "/admin/offers?status=Accepted" },
            { key: "declines", label: "Declines", href: "/admin/offers?status=Declined" }
        ];
        function paint(data, asOf) {
            while (tiles.firstChild) tiles.removeChild(tiles.firstChild);
            defs.forEach(function (d) {
                var tile = el("a", { className: "tile", href: d.href, id: "tile-" + d.key });
                tile.appendChild(el("p", { text: d.label }));
                var count = has(data, d.key) ? data[d.key] : 0;
                tile.setAttribute("aria-label", d.label + ": " + count);
                tile.appendChild(el("p", { className: "tile-count", text: String(count) }));
                tile.appendChild(el("p", { text: "As of " + asOf }));
                tiles.appendChild(tile);
            });
        }
        function failOne() {
            while (tiles.firstChild) tiles.removeChild(tiles.firstChild);
            defs.forEach(function (d) {
                var tile = el("div", { className: "tile", id: "tile-" + d.key });
                tile.appendChild(el("p", { text: d.label }));
                tile.appendChild(el("p", { text: "Couldn't load" }));
                var retry = el("button", { type: "button", text: "Retry" });
                retry.addEventListener("click", load);
                tile.appendChild(retry);
                tiles.appendChild(tile);
            });
        }
        function load() {
            defs.forEach(function () {});
            tiles.setAttribute("aria-busy", "true");
            fetch("/admin/api/stats", { credentials: "same-origin" }).then(function (res) {
                tiles.removeAttribute("aria-busy");
                if (handleAuth(res)) return null;
                if (!res.ok) throw new Error("stats");
                return res.json();
            }).then(function (data) {
                if (!data) return;
                paint(data, formatEt(new Date().toISOString()));
            }).catch(failOne);
        }
        refresh.addEventListener("click", load);
        load();
    }

    function renderDetail(type, id) {
        var spec = LISTS[type];
        setTitle(type.charAt(0).toUpperCase() + type.slice(1) + " detail");
        markNav(type);
        clearData();
        var state = queryState();
        fetch(spec.api + "/" + id, { credentials: "same-origin" }).then(function (res) {
            if (handleAuth(res)) return null;
            if (res.status === 404) {
                renderNotFound();
                return null;
            }
            if (!res.ok) throw new Error("detail");
            return res.json();
        }).then(function (item) {
            if (!item) return;
            if (has(item, "deletedAt") && item.deletedAt && !state.includeDeleted) {
                renderNotFound();
                return;
            }
            var back = spec.path + (location.search || "");
            app.appendChild(el("p", null, [el("a", { href: back, text: "Back to list" })]));
            var dl = el("dl", { className: "dl" });
            Object.keys(item).forEach(function (key) {
                if (key === "entities" && item.entities) {
                    dl.appendChild(el("dt", { text: "Entities" }));
                    var box = el("dd");
                    item.entities.forEach(function (ent) {
                        if (has(ent, "name")) box.appendChild(el("p", { text: String(ent.name) }));
                        if (has(ent, "description")) box.appendChild(el("p", { text: String(ent.description) }));
                    });
                    dl.appendChild(box);
                    return;
                }
                var value = item[key];
                dl.appendChild(el("dt", { text: key }));
                var dd = el("dd");
                if (key === "status") dd.appendChild(badge(statusLabel(value, type)));
                else if (key === "isActive") dd.appendChild(badge(value ? "Active" : "Suspended"));
                else if (key === "createdAt" || key === "updatedAt" || key === "deletedAt" || key === "endsAt")
                    dd.appendChild(timeNode(value));
                else dd.textContent = value == null ? "" : String(value);
                dl.appendChild(dd);
            });
            if (has(item, "deletedAt") && item.deletedAt) {
                var badges = el("p");
                if (has(item, "status")) badges.appendChild(badge(statusLabel(item.status, type)));
                if (has(item, "isActive")) badges.appendChild(badge(item.isActive ? "Active" : "Suspended"));
                badges.appendChild(badge("Deleted"));
                app.appendChild(badges);
            }
            app.appendChild(dl);
        }).catch(function () {
            app.appendChild(el("p", { className: "error", text: "We couldn't load " + spec.typeLabel + "." }));
        });
    }

    function route() {
        nav.hidden = false;
        menuToggle.hidden = false;
        var parsed = parsePath();
        if (parsed.kind === "stats") renderStats();
        else if (parsed.kind === "list") renderList(parsed.type);
        else if (parsed.kind === "detail") renderDetail(parsed.type, parsed.id);
        else renderNotFound();
    }

    menuToggle.addEventListener("click", function () {
        var open = nav.classList.toggle("is-open");
        menuToggle.setAttribute("aria-expanded", open ? "true" : "false");
    });

    window.addEventListener("popstate", route);
    route();

})();
