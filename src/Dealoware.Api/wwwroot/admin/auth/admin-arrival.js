/**
 * The first script on a sign-in page: why the browser is here, when whatever
 * sent it left a note.
 *
 * A page (or the server) that sends the browser to a sign-in page may put
 * one of five fixed flags after the address. A flag says nothing about the
 * visitor, and this script takes it out of the address bar before anything
 * else can go wrong. Nothing is stored: a flag lasts for one page load.
 *
 *   #again    no pending sign-in was left, so step 1 has to be done again
 *   #fresh    the page reloaded itself because the server did not accept its
 *             anti-forgery pair (another tab had been given a newer one)
 *   #ended       the session ended, so the owner has to sign in again
 *   #changed     a reset has just changed the password
 *   #signed-out  the owner has just signed out
 *
 * Any other fragment is left alone and means nothing here. The page that
 * reads a flag decides what to show for it, in words the server gave it.
 * The reset-request page loads this script too, for #fresh alone.
 */
(function (global) {
  "use strict";

  var AGAIN = "#again";
  var FRESH = "#fresh";
  var ENDED = "#ended";
  var CHANGED = "#changed";
  var SIGNED_OUT = "#signed-out";

  var hash = location.hash;
  var flag = hash === AGAIN ? "again" : hash === FRESH ? "fresh" : hash === ENDED ? "ended" : hash === CHANGED ? "changed"
    : hash === SIGNED_OUT ? "signed-out" : "";
  if (flag) history.replaceState(null, "", location.pathname + location.search);

  global.AdminArrival = {
    /** "again", "fresh", "ended", "changed", "signed-out", or "" when the browser came here in the ordinary way. */
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
