/**
 * The first script on a page opened from an emailed link.
 *
 * Before anything else can go wrong it reads the link token from the URL
 * fragment and removes it from the address bar, exactly as reset-confirm.js
 * does. If a later script fails to load, the token is already out of the
 * address bar, the history entry and anything a reload would send.
 *
 * The token is kept here in memory until the page script takes it, once.
 * After that this script holds nothing.
 */
(function (global) {
  "use strict";

  var hash = location.hash;
  history.replaceState(null, "", location.pathname);
  var match = /[#&]token=([^&]*)/.exec(hash);
  var token = "";
  if (match) {
    try {
      token = decodeURIComponent(match[1].replace(/\+/g, " "));
    } catch (e) {
      token = "";
    }
  }

  global.AdminLink = {
    /** The link token, or "" when the link had none. Empty on every later call. */
    takeToken: function () {
      var value = token;
      token = "";
      return value;
    }
  };
})(window);
