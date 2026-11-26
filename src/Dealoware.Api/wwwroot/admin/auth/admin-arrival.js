/**
 * The first script on a sign-in page: why the browser is here, when whatever
 * sent it left a note.
 *
 * A page (or the server) that sends the browser to a sign-in page may put
 * one of two fixed flags after the address. A flag says nothing about the
 * visitor, and this script takes it out of the address bar before anything
 * else can go wrong. Nothing is stored: a flag lasts for one page load.
 *
 *   #again  no pending sign-in was left, so step 1 has to be done again
 *   #fresh  the page reloaded itself because the server did not accept its
 *           anti-forgery pair (another tab had been given a newer one)
 *
 * Any other fragment is left alone and means nothing here.
 */
(function (global) {
  "use strict";

  var AGAIN = "#again";
  var FRESH = "#fresh";

  var flag = location.hash === AGAIN ? "again" : location.hash === FRESH ? "fresh" : "";
  if (flag) history.replaceState(null, "", location.pathname + location.search);

  global.AdminArrival = {
    /** "again", "fresh", or "" when the browser came here in the ordinary way. */
    flag: function () {
      return flag;
    },

    /**
     * Loads this page anew, which gives it a new anti-forgery pair. The
     * reloaded page finds the flag and so knows not to do it a second time.
     */
    reload: function () {
      history.replaceState(null, "", location.pathname + location.search + FRESH);
      location.reload();
    }
  };
})(window);
