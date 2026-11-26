/**
 * For a signed-out admin page that is only a message, with no form to start
 * in: "this link no longer works" (S-A5) and "check your email" (S-A9).
 *
 * It puts the keyboard focus on the page heading. A screen reader then
 * starts with the heading, and the next Tab reaches the first link. With
 * JavaScript off the page reads the same; only the focus stays where the
 * browser left it.
 */
(function () {
  "use strict";

  var heading = document.getElementById("page-heading");
  if (!heading) return;
  heading.setAttribute("tabindex", "-1");
  heading.focus();
})();
