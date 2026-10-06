namespace Dealoware.Api.Admin;

/// <summary>
/// Signed-in admin shells. Served only after the session gate (route note r3 b079a814).
/// No entity data in HTML. JS/CSS are not wwwroot static files.
/// </summary>
public static class AdminDeleteUiPages
{
    public static IResult Page(string screenName, string view, string? entityType, Guid? id)
    {
        var title = $"{screenName} · Dealoware admin";
        var typeAttr = entityType is null ? "" : $" data-entity-type=\"{entityType}\"";
        var idAttr = id is null ? "" : $" data-entity-id=\"{id:D}\"";
        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta http-equiv="Cache-Control" content="no-store">
              <title>{{title}}</title>
              <link rel="stylesheet" href="/admin/assets/admin-delete.css">
            </head>
            <body>
              <a class="skip-link" href="#main">Skip to main content</a>
              <header>
                <p class="brand">Dealoware admin</p>
                <nav aria-label="Admin">
                  <a href="/admin" data-nav="stats">Stats</a>
                  <a href="/admin/participants" data-nav="participants">Participants</a>
                  <a href="/admin/artifacts" data-nav="artifacts">Artifacts</a>
                  <a href="/admin/negotiations" data-nav="negotiations">Negotiations</a>
                  <a href="/admin/offers" data-nav="offers">Offers</a>
                </nav>
              </header>
              <main id="main" data-view="{{view}}"{{typeAttr}}{{idAttr}}>
                <h1>{{screenName}}</h1>
                <div id="status" class="status" role="status" aria-live="polite"></div>
                <div id="app"></div>
              </main>
              <div id="delete-dialog" class="dialog" role="dialog" aria-modal="true" aria-labelledby="delete-dialog-title" hidden></div>
              <script src="/admin/assets/admin-delete.js"></script>
            </body>
            </html>
            """;
        return new NoStoreResult(html, "text/html; charset=utf-8");
    }

    public static IResult Css()
        => new NoStoreResult(CssText, "text/css; charset=utf-8");

    public static IResult JavaScript()
        => new NoStoreResult(JsText, "text/javascript; charset=utf-8");

    private sealed class NoStoreResult : IResult
    {
        private readonly string _body;
        private readonly string _contentType;

        public NoStoreResult(string body, string contentType)
        {
            _body = body;
            _contentType = contentType;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers.CacheControl = "no-store";
            httpContext.Response.ContentType = _contentType;
            await httpContext.Response.WriteAsync(_body);
        }
    }

    private const string CssText = """
        :root { color-scheme: light; }
        * { box-sizing: border-box; }
        body { margin: 0; font: 16px/1.5 system-ui, sans-serif; color: #111; background: #fff; }
        .skip-link { position: absolute; left: 8px; top: -40px; background: #111; color: #fff; padding: 8px; }
        .skip-link:focus { top: 8px; }
        header { border-bottom: 1px solid #222; padding: 12px 16px; }
        .brand { margin: 0 0 8px; font-weight: 700; }
        nav { display: flex; flex-wrap: wrap; gap: 8px; }
        nav a { min-height: 24px; min-width: 24px; padding: 6px 10px; color: #003366; }
        nav a[aria-current="page"] { font-weight: 700; text-decoration: none; border-bottom: 2px solid #111; }
        main { padding: 16px; max-width: 1100px; }
        h1 { margin-top: 0; }
        button, .btn { min-height: 32px; min-width: 32px; padding: 6px 12px; font: inherit; cursor: pointer; border: 2px solid #111; background: #fff; color: #111; }
        button:focus-visible, a:focus-visible, input:focus-visible { outline: 3px solid #005fcc; outline-offset: 2px; }
        button:disabled { cursor: not-allowed; opacity: 0.7; }
        button.danger { background: #8b0000; color: #fff; border-color: #8b0000; }
        button.secondary { background: #f4f4f4; }
        label { display: block; font-weight: 600; margin: 8px 0 4px; }
        input, select { font: inherit; padding: 6px; border: 1px solid #111; min-height: 32px; }
        table { width: 100%; border-collapse: collapse; margin-top: 12px; }
        th, td { text-align: left; padding: 8px; border-bottom: 1px solid #444; }
        .badge { display: inline-block; padding: 2px 8px; border: 1px solid #111; margin-right: 6px; }
        .status { min-height: 1.5em; margin-bottom: 8px; }
        .toolbar { display: flex; flex-wrap: wrap; gap: 12px; align-items: end; margin: 12px 0; }
        .dialog[hidden] { display: none; }
        .dialog { position: fixed; inset: 0; background: rgba(0,0,0,0.55); display: flex; align-items: center; justify-content: center; padding: 16px; }
        .dialog-panel { background: #fff; color: #111; max-width: 36rem; width: 100%; max-height: 90vh; overflow: auto; padding: 20px; border: 2px solid #111; }
        .dialog ul { margin: 8px 0; }
        .error { color: #8b0000; }
        .tiles { display: grid; grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr)); gap: 12px; }
        .tile { border: 1px solid #111; padding: 12px; }
        .tile a { color: #003366; }
        """;

    private const string JsText = """
        (function () {
          const main = document.getElementById("main");
          const app = document.getElementById("app");
          const status = document.getElementById("status");
          const dialog = document.getElementById("delete-dialog");
          const view = main.getAttribute("data-view");
          const entityType = main.getAttribute("data-entity-type");
          const entityId = main.getAttribute("data-entity-id");
          let lastTrigger = null;
          let confirmToken = null;
          let intent = null;
          let sending = false;

          document.querySelectorAll("nav a").forEach(function (a) {
            const key = a.getAttribute("data-nav");
            if ((view === "stats" && key === "stats") || key === entityType) {
              a.setAttribute("aria-current", "page");
            }
          });

          function encode(s) {
            return String(s == null ? "" : s)
              .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
              .replace(/"/g, "&quot;");
          }
          function setStatus(text) { status.textContent = text || ""; }
          function consumeNotice() {
            try {
              const n = sessionStorage.getItem("dw-admin-notice");
              if (n) sessionStorage.removeItem("dw-admin-notice");
              return n || "";
            } catch (e) { return ""; }
          }
          function rememberNotice(text) {
            try { sessionStorage.setItem("dw-admin-notice", text); } catch (e) {}
          }
          function singular(type) {
            return ({ participants: "participant", artifacts: "artifact", negotiations: "negotiation", offers: "offer" })[type] || type;
          }
          function api(path, options) {
            return fetch(path, Object.assign({ credentials: "same-origin" }, options || {}));
          }

          async function loadStats() {
            app.setAttribute("aria-busy", "true");
            const res = await api("/admin/api/delete-ui/stats");
            if (res.status === 401) { setStatus("Sign in required."); app.innerHTML = ""; return; }
            const data = await res.json();
            app.removeAttribute("aria-busy");
            app.innerHTML =
              '<div class="tiles">' +
              tile("Participants", data.participants, "/admin/participants") +
              tile("Open negotiations", data.openNegotiations, "/admin/negotiations") +
              tile("Offers", data.offers, "/admin/offers") +
              tile("Accepts", data.accepts, "/admin/offers") +
              tile("Declines", data.declines, "/admin/offers") +
              "</div>";
          }
          function tile(label, count, href) {
            return '<article class="tile"><h2>' + encode(label) + '</h2><p><a href="' + href + '">' + encode(count) + "</a></p></article>";
          }

          async function loadList() {
            const params = new URLSearchParams(location.search);
            const q = params.get("q") || "";
            const includeDeleted = params.get("includeDeleted") === "true";
            app.innerHTML =
              '<div class="toolbar">' +
              '<div><label for="search">Search by name or id</label>' +
              '<input id="search" type="search" value="' + encode(q) + '"></div>' +
              '<div><label for="show-deleted"><input id="show-deleted" type="checkbox"' + (includeDeleted ? " checked" : "") + "> Show deleted</label></div>" +
              "</div><div id=\"table-wrap\"></div>";
            const search = document.getElementById("search");
            const toggle = document.getElementById("show-deleted");
            async function refresh() {
              const wrap = document.getElementById("table-wrap");
              wrap.setAttribute("aria-busy", "true");
              const res = await api("/admin/api/delete-ui/" + entityType + "?includeDeleted=" + toggle.checked);
              if (res.status === 401) { setStatus("Sign in required."); return; }
              const data = await res.json();
              const needle = search.value.trim().toLowerCase();
              const items = (data.items || []).filter(function (row) {
                if (!needle) return true;
                return (row.displayIdentity || "").toLowerCase().indexOf(needle) >= 0 ||
                  (row.id || "").toLowerCase().indexOf(needle) >= 0;
              });
              const notice = consumeNotice();
              setStatus((notice ? notice + " " : "") + items.length + " results");
              if (!items.length) {
                wrap.innerHTML = "<p>" + (needle || toggle.checked ? "No results match these filters." : "No " + entityType + " yet.") + "</p>";
                return;
              }
              wrap.innerHTML = '<table><caption>' + encode(entityType) + '</caption><thead><tr>' +
                "<th scope=\"col\">Name</th><th scope=\"col\">ID</th><th scope=\"col\">Status</th><th scope=\"col\">Actions</th></tr></thead><tbody>" +
                items.map(function (row) {
                  const badges = (row.status ? '<span class="badge">' + encode(row.status) + "</span>" : "") +
                    (row.deletedAt ? '<span class="badge">Deleted</span>' : "");
                  const del = row.deletedAt ? "" :
                    '<button type="button" class="danger" data-delete="' + encode(row.id) + '" data-name="' + encode(row.displayIdentity) + '" data-version="' + encode(row.version) + '">Delete</button>';
                  return "<tr data-id=\"" + encode(row.id) + "\"><td><a href=\"/admin/" + entityType + "/" + encode(row.id) + "\">" +
                    encode(row.displayIdentity) + "</a></td><td>" + encode(row.id) + "</td><td>" + badges + "</td><td>" + del + "</td></tr>";
                }).join("") + "</tbody></table>";
              wrap.querySelectorAll("[data-delete]").forEach(function (btn) {
                btn.addEventListener("click", function () { openDelete(entityType, btn.getAttribute("data-delete"), btn.getAttribute("data-version"), btn); });
              });
            }
            let t;
            search.addEventListener("input", function () {
              clearTimeout(t);
              t = setTimeout(function () {
                const next = new URL(location.href);
                if (search.value) next.searchParams.set("q", search.value); else next.searchParams.delete("q");
                history.replaceState(null, "", next);
                refresh();
              }, 300);
            });
            toggle.addEventListener("change", function () {
              const next = new URL(location.href);
              if (toggle.checked) next.searchParams.set("includeDeleted", "true"); else next.searchParams.delete("includeDeleted");
              history.replaceState(null, "", next);
              refresh();
            });
            await refresh();
          }

          async function loadDetail() {
            const res = await api("/admin/api/delete-ui/" + entityType + "/" + entityId + "?includeDeleted=true");
            if (res.status === 401) { setStatus("Sign in required."); return; }
            if (!res.ok) { app.innerHTML = "<p>We can't find that page.</p>"; return; }
            const row = await res.json();
            const deleted = !!row.deletedAt;
            app.innerHTML = "<p>ID: " + encode(row.id) + "</p><p>Name: " + encode(row.displayIdentity) + "</p>" +
              (row.status ? "<p>Status: <span class=\"badge\">" + encode(row.status) + "</span>" +
                (deleted ? ' <span class="badge">Deleted</span>' : "") + "</p>" : (deleted ? '<p><span class="badge">Deleted</span></p>' : "")) +
              (deleted ? "<p>This record is read-only.</p>" :
                '<p><button type="button" class="danger" id="detail-delete" data-version="' + encode(row.version) + '">Delete</button></p>');
            const btn = document.getElementById("detail-delete");
            if (btn) btn.addEventListener("click", function () { openDelete(entityType, row.id, row.version, btn); });
          }

          async function openDelete(type, id, version, trigger) {
            lastTrigger = trigger;
            sending = false;
            const res = await api("/admin/api/" + type + "/" + id + "/delete-intent", { method: "POST" });
            if (!res.ok) { setStatus("Nothing was deleted."); return; }
            intent = await res.json();
            confirmToken = intent.confirmToken || "";
            renderDialog(type, id, version, false);
          }

          function cascadeItems(c) {
            const items = [];
            if (c.participants) items.push(c.participants + " participant" + (c.participants === 1 ? "" : "s") + " will also be deleted");
            if (c.negotiations) items.push(c.negotiations + " negotiation" + (c.negotiations === 1 ? "" : "s") + " will also be deleted");
            if (c.offers) items.push(c.offers + " offer" + (c.offers === 1 ? "" : "s") + " will also be deleted");
            if (c.artifacts) items.push(c.artifacts + " artifact" + (c.artifacts === 1 ? "" : "s") + " will also be deleted");
            if (!items.length) items.push("No additional records will be deleted");
            return items;
          }

          function needsTypedConfirm() {
            if (!intent) return false;
            if (intent.entityType === "participants" && (intent.cascade.negotiations > 0 || intent.cascade.offers > 0)) return true;
            if (intent.entityType === "negotiations" && intent.isOpen) return true;
            return false;
          }
          function typedValue() {
            if (intent.entityType === "participants") return intent.displayIdentity;
            return intent.id;
          }

          function renderDialog(type, id, version, expired) {
            const name = intent.displayIdentity;
            const blocked = !!intent.blocked;
            let inner = "";
            if (blocked) {
              inner = '<div class="dialog-panel" role="document">' +
                '<h2 id="delete-dialog-title">This artifact can\'t be deleted</h2>' +
                "<p>It is used by these negotiations:</p><ul>" +
                (intent.blockedByNegotiations || []).map(function (nid) {
                  return '<li><a href="/admin/negotiations/' + encode(nid) + '">' + encode(nid) + "</a></li>";
                }).join("") + "</ul>" +
                '<button type="button" class="secondary" data-close>Close</button></div>';
            } else {
              const title = "Delete " + singular(type) + " " + name + "?";
              const typed = needsTypedConfirm();
              inner = '<div class="dialog-panel" role="document">' +
                '<h2 id="delete-dialog-title">' + encode(title) + "</h2>" +
                (expired ? '<p class="error">This confirmation expired. Review the details again.</p>' : "") +
                "<p>ID: " + encode(intent.id) + "</p>" +
                (intent.entityType === "negotiations" && intent.isOpen ? "<p>This negotiation is open. " + (intent.cascade.offers || 0) + " child offers are included.</p>" : "") +
                "<ul>" + cascadeItems(intent.cascade).map(function (i) { return "<li>" + encode(i) + "</li>"; }).join("") + "</ul>" +
                "<p>This can't be restored from the admin.</p>" +
                (typed ? '<label for="confirm-type">Type ' + encode(typedValue()) + " to confirm</label>" +
                  '<input id="confirm-type" type="text" autocomplete="off">' : "") +
                '<p class="error" id="delete-error" hidden></p>' +
                '<button type="button" class="danger" id="confirm-delete" data-version="' + encode(version) + '">Delete ' + encode(singular(type)) + "</button> " +
                '<button type="button" class="secondary" data-close>Cancel</button></div>';
            }
            dialog.innerHTML = inner;
            dialog.hidden = false;
            const closeBtn = dialog.querySelector("[data-close]");
            const confirmBtn = dialog.querySelector("#confirm-delete");
            const typeInput = dialog.querySelector("#confirm-type");
            (closeBtn || dialog).focus();
            if (typeInput && confirmBtn) {
              confirmBtn.disabled = true;
              typeInput.addEventListener("input", function () {
                confirmBtn.disabled = typeInput.value !== typedValue() || sending;
              });
            }
            dialog.querySelectorAll("[data-close]").forEach(function (b) {
              b.addEventListener("click", closeDialog);
            });
            dialog.addEventListener("click", function (e) { if (e.target === dialog) closeDialog(); });
            if (confirmBtn) {
              confirmBtn.addEventListener("click", function () { confirmDelete(type, id, confirmBtn.getAttribute("data-version"), confirmBtn); });
            }
            trapFocus(dialog);
          }

          function closeDialog() {
            dialog.hidden = true;
            dialog.innerHTML = "";
            confirmToken = null;
            if (lastTrigger) lastTrigger.focus();
          }

          async function confirmDelete(type, id, version, btn) {
            if (sending) return;
            sending = true;
            btn.disabled = true;
            btn.textContent = "Deleting…";
            btn.setAttribute("aria-busy", "true");
            const res = await api("/admin/api/" + type + "/" + id, {
              method: "DELETE",
              headers: {
                "X-Admin-Confirm-Token": confirmToken || "",
                "If-Match": '"' + version + '"'
              }
            });
            if (res.status === 400) {
              await refreshExpired(type, id, version);
              return;
            }
            if (!res.ok) {
              sending = false;
              const err = dialog.querySelector("#delete-error");
              if (err) { err.hidden = false; err.textContent = "Nothing was deleted."; }
              btn.disabled = false;
              btn.textContent = "Delete " + singular(type);
              btn.removeAttribute("aria-busy");
              const retry = document.createElement("button");
              retry.type = "button";
              retry.className = "secondary";
              retry.textContent = "Retry";
              retry.addEventListener("click", function () { confirmDelete(type, id, version, btn); });
              if (err) err.after(retry);
              return;
            }
            closeDialog();
            rememberNotice(capitalize(singular(type)) + " " + (intent && intent.displayIdentity ? intent.displayIdentity : id) + " deleted.");
            location.href = "/admin/" + type;
          }

          async function refreshExpired(type, id, version) {
            sending = false;
            const res = await api("/admin/api/" + type + "/" + id + "/delete-intent", { method: "POST" });
            if (!res.ok) { setStatus("Nothing was deleted."); return; }
            intent = await res.json();
            confirmToken = intent.confirmToken || "";
            renderDialog(type, id, intent.version, true);
          }

          function capitalize(s) { return s ? s.charAt(0).toUpperCase() + s.slice(1) : s; }

          function trapFocus(root) {
            root.addEventListener("keydown", function (e) {
              if (e.key === "Escape") { e.preventDefault(); closeDialog(); return; }
              if (e.key !== "Tab") return;
              const focusable = root.querySelectorAll("button, [href], input, select, textarea");
              if (!focusable.length) return;
              const first = focusable[0];
              const last = focusable[focusable.length - 1];
              if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
              else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
            });
          }

          if (view === "stats") loadStats();
          else if (view === "list") loadList();
          else if (view === "detail") loadDetail();
        })();
        """;
}
