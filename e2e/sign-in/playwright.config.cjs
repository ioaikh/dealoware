"use strict";

// The configuration is shared with the other signed-out page suites; this
// suite brings its own @playwright/test and its variable prefix.
const { signedOutConfig } = require("../signed-out/playwright.base.cjs");

module.exports = signedOutConfig(require("@playwright/test"), "SIGN_IN_E2E");
