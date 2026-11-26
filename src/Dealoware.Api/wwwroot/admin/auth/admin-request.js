/**
 * How a signed-out admin page talks to the server.
 *
 * post(path, body) sends one JSON POST to a path on this site, carrying the
 * page's anti-forgery token (the dw-csrf meta) in X-CSRF-TOKEN.
 * get(path) asks a path on this site for its JSON, sending nothing.
 *
 * Either way a redirect is never followed: it fails the request. So does a
 * reply that has not fully arrived after 15 seconds. Another origin is never
 * asked: the callers pass constant paths, and the page's own policy
 * (connect-src 'self') refuses anything else.
 *
 * The request mode is left at its default on purpose. With mode
 * "same-origin" under the pages' no-referrer policy, Firefox and Safari send
 * "Origin: null" on a POST, as the Fetch standard says, and the server
 * refuses a null origin. Chromium does not, so only the other two show it.
 *
 * Both resolve with { status, body } where body is the reply's JSON object,
 * or null when the reply is not one. They reject when there was no reply at
 * all. Nothing is kept between calls.
 *
 * A page on its way to another (beforeunload, heard only while a request is
 * out) gives its requests up quietly: one the browser cancels as the page
 * goes neither resolves nor rejects, and no new one is sent. Its caller would
 * take the cancel for "no reply" and ask the server again from a page that is
 * going; WebKit refuses that request and reports it as a page error. This is
 * how the session timer's move to sign-in (a reload) met a sign-out still out:
 * the sign-out's caller asked /admin/api/me as the page went.
 */
(function (global) {
  "use strict";

  var TIMEOUT_MS = 15000;

  // Requests out now, and whether the page has started to go while one was.
  var out = 0;
  var leaving = false;

  function onLeave() {
    leaving = true;
  }

  /** A promise that never settles: what a request is worth on a page that is going. */
  function given() {
    return new Promise(function () {});
  }

  function settled() {
    out -= 1;
    if (out === 0) {
      window.removeEventListener("beforeunload", onLeave);
      leaving = false;
    }
  }

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

  function send(path, options) {
    if (leaving) return given();
    if (out === 0) window.addEventListener("beforeunload", onLeave);
    out += 1;
    var controller = new AbortController();
    var timer = setTimeout(function () { controller.abort(); }, TIMEOUT_MS);
    options.credentials = "same-origin";
    options.redirect = "error";
    options.cache = "no-store";
    options.signal = controller.signal;
    return fetch(path, options).then(function (r) {
      return r.text().then(function (raw) {
        return { status: r.status, body: parse(raw) };
      });
    }).then(function (reply) {
      clearTimeout(timer);
      settled();
      return reply;
    }, function (err) {
      clearTimeout(timer);
      var gone = leaving;
      settled();
      if (gone) return given();
      throw err;
    });
  }

  function post(path, body) {
    return send(path, {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": csrfToken() },
      body: JSON.stringify(body)
    });
  }

  function get(path) {
    return send(path, { method: "GET" });
  }

  /** A value from a reply as text: the string itself, or "" for anything else. */
  function text(value) {
    return typeof value === "string" ? value : "";
  }

  global.AdminRequest = {
    post: post,
    get: get,
    text: text
  };
})(window);
