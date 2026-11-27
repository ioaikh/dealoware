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
 */
(function (global) {
  "use strict";

  var TIMEOUT_MS = 15000;

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
      return reply;
    }, function (err) {
      clearTimeout(timer);
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
