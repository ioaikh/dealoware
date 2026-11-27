# The Core admin console: owner's guide

| Field | Value |
|-------|-------|
| **Date** | 2026-11-26 |
| **Type** | Guide |
| **For** | The Core owner, the one person who signs in to the console |
| **Describes** | What `main` does at the time of writing. Every on-screen sentence in quotes is copied from the code. |
| **Sources** | The copy classes in `src/Dealoware.Api/Admin/` (`AdminConsoleCopy.cs`, `AdminEditCopy.cs`, `AdminDeleteCopy.cs` and the sign-in, setup, reset and security settings copy), and the review records `docs/verification/2026-11-26__qa__verification__*.md` |
| **Self-verify record** | [2026-11-26__qa__verification__t08-self-verify.md](../verification/2026-11-26__qa__verification__t08-self-verify.md) |

## 1. What the console is for

The console lets the Core owner look after the data the negotiation engine holds:

- **Participants**, **Artifacts**, **Negotiations** and **Offers**: list them, search and filter them, open one record, change the few fields the owner may change, and delete.
- **Stats**: five totals.
- **Audit log**: a read-only record of every sign-in event and every change made in the console.
- **Security settings**: the owner's own password, authenticator app and recovery codes.

There is one owner and no other admin. The owner is the address configured as `CoreOwner:Email`. Nobody else can sign in, and there is no list of admin users to add to.

The console answers only on its own address, `admin.core.dealoware.com` (configured under `AdminHost`). On that address the engine's routes answer 404, and on the engine's address every `/admin` route answers 404.

All times in the console are Eastern Time (ET).

## 2. First-time setup

This is done once, when the owner has no password yet.

1. **Someone with access to the server runs the one-shot command `bootstrap-link`.** It is the only way a setup link is made. It emails the link to the configured owner and prints only "Bootstrap link sent to the configured Core admin owner. It works once and expires after 24 hours." If the owner already has a password, it says "The Core admin owner is already set up. No bootstrap link was issued." and changes nothing.
2. **Open the link.** The page "Set password" asks for "New password" and "Confirm password", under the rule "Use 15 to 128 characters. Avoid common or account-related words." The security check must pass. Press "Set password".
   - A link that was used, has run out or was replaced shows "This link no longer works" and "Setup links work once and expire after 24 hours. Ask for a new setup link through the same channel you got this one."
3. **Sign in** with the email and the new password (see section 3). Because no authenticator is set up yet, the next page is "Authenticator setup".
4. **Set up the authenticator.** "Scan the QR code with your authenticator app, or enter the key by hand. Then enter a 6-digit code to confirm." Under the code: "If you can't scan the code, enter this key in your app instead." Enter a code from the app and press "Confirm".
5. **Save the recovery codes.** The page "Recovery codes" shows ten codes: "Save these codes now. They are shown once." Use "Copy all", tick "I have saved these recovery codes." and press "Continue".
6. **Sign in again**, this time with a code from the app. Setting up the authenticator signs nobody in by itself.

The pages between steps 3 and 6 belong to one sign-in in progress, which lasts five minutes. If it runs out before the codes are seen, they cannot be shown again; make new ones on the Security settings page once signed in.

## 3. Signing in

Sign-in has two steps.

1. **"Sign in"**: "Email", "Password" and the security check, then "Continue".
2. **"Enter code"**: "Enter the 6-digit code from your authenticator app.", then "Sign in".
   - Without the phone, choose "Use a recovery code instead". The page "Recovery code" asks to "Enter one unused recovery code." Each code works once.
   - "Back to sign in" signs the sign-in in progress out and starts again.

**What a refusal looks like.** A wrong email, a wrong password or a locked account all get the same sentence: "We couldn't sign you in. Check your details and try again later. You can also reset your password." A wrong code gets "Verification failed, please try again." The screens never say which part was wrong, how many tries are left, or that the account is locked. That is on purpose.

**The rules behind it** (Spec §8.6):

- 5 failed password or code attempts for the account within 15 minutes lock sign-in for 30 minutes.
- 20 failed attempts from one network within 15 minutes slow that network down for 30 minutes.
- Step 2 must be done within 5 minutes of step 1, with at most 5 wrong codes.

The Security settings page shows when sign-in was locked (section 6).

**After a recovery-code sign-in** every console page shows "You signed in with a recovery code. You have N recovery codes left.", with "Manage recovery codes" and "Dismiss".

**Keep the recovery codes apart from the phone.** A password reset needs a code from the app or a recovery code. The console has no way back in without one of them.

## 4. Resetting a forgotten password

1. On the sign-in page choose "Forgot your password?".
2. "Reset password": enter the email, pass the security check, press "Send reset link".
3. "Check your email" always says "If that account exists, we've sent instructions." and "The link expires in 1 hour." It says the same for any address.
4. Open the emailed link. "Set a new password": "Choose a new password and confirm with an authenticator or recovery code." Enter the new password twice and a code (or switch to a recovery code), then "Change password".
5. The sign-in page then says "Your password has been changed. Sign in with your new password and authenticator code."

A link that was used or is older than one hour shows "This link no longer works", "Reset links work once and expire after 1 hour." and "Request a new link".

## 5. The session, its warnings, and signing out

- **A session ends after 30 minutes without use, and always 8 hours after sign-in.** Using the console renews the 30 minutes. Nothing renews the 8 hours.
- **"Still there?"** opens 2 minutes before the idle end: "Your session will end within 2 minutes because there has been no activity." "Stay signed in" renews it; "Sign out" ends it.
- **"Your session is ending"** opens 2 minutes before the 8-hour end: "Your session will end within 2 minutes because of the 8-hour limit, and it can't be extended. Anything you haven't saved will be lost." It has one button, "OK".
- **When a session ends**, the page goes to sign-in with "Your session has ended. Sign in again to continue." After signing in, the console returns to the page that was open.
- **"Sign out"** is in the menu on every page. It ends the session, writes `sign_out` to the audit log, and shows "You've been signed out." Opening an address cannot sign anyone out; only the button can.
- No timer keeps a session alive on its own. Only the owner's use or "Stay signed in" does.
- Every signed-in page is sent with `Cache-Control: no-store`, so the browser does not keep a copy.

## 6. Security settings

"Security settings" is in the menu. It has four parts.

- **"Password"**: "Current password", "New password", "Confirm new password", and a code from the app or a recovery code, then "Change password". Success ends every session, this one included, and goes to sign-in.
- **"Recovery codes"**: shows how many are left ("N codes left"). "Create new codes" first asks "Confirm it's you" ("Enter your current password and a code from your authenticator app."). "New codes replace all of your current codes. Your old codes stop working." The new codes are shown once.
- **"Authenticator"**: "Set up again" is for a new phone. "Your current app keeps working until you confirm a code from the new one." It also asks "Confirm it's you" first. After the confirm: "Your authenticator app has been changed. Your old recovery codes no longer work." New codes are shown once.
- **"Recent sign-in activity"**: "Your last 20 sign-in events. Times are Eastern Time (ET)." The "Network (hashed)" column shows a short keyed hash, never an address. If sign-in was locked in the last 24 hours, a lock notice says when, for example "Sign-in to this account was locked from {start} to {end}."

"Confirm it's you" lasts 5 minutes for this session only. Creating codes and changing the authenticator end every other session. A wrong answer counts toward the same lock as sign-in, and the message never says which part was wrong: "We couldn't confirm it's you. Check your current password and code."

## 7. Stats, lists and details

### Stats

The first page, "Stats", shows five totals: "Participants", "Open negotiations", "Offers", "Accepts" and "Declines", with "As of {time} ET". Deleted records are not counted. Each total links to its list. "Refresh" counts again; a total that could not be counted shows "Couldn't load" and "Retry".

### The four lists

| List | Columns | Search covers | Filters |
|------|---------|---------------|---------|
| Participants | Name, ID, Status, Created, Updated | "Search by display name or Core identifier" | Status (Active, Suspended), dates |
| Artifacts | Name, Owner participant, Created, Updated | "Search by artifact name" | Owner participant, dates |
| Negotiations | ID, Artifact, Participants, Status, Created, Updated | "Search by artifact or participant name" | Status (Open, Closed, Expired), Participant, Artifact, Negotiation ID, dates |
| Offers | ID, Negotiation ID, Artifact, Offering participant, Amount, Status, Created, Updated | "Search by artifact, offering participant, or negotiation id" | Status (Open, Accepted, Declined, Superseded, Cancelled, Withdrawn), Participant, Artifact, Negotiation ID, "Amount min", "Amount max", dates |

- Search ignores case and matches part of a name.
- Press "Apply filters". Each filter in use shows as a chip; "Remove" drops one and "Clear all filters" drops all.
- Sort by pressing a column heading that offers it. The default order is Updated, newest first.
- "Page size" is 50, 100 or 200. "Showing 101–200 of 1,234", with "Previous" and "Next".
- Empty states: "No {type} yet." and "No results match these filters." (with "Clear filters").
- **"Show deleted"** adds deleted records, marked "Deleted". Without it they do not appear. Using it is a read and is not written to the audit log.
- The lists work without JavaScript. Nothing on them polls the server.

### Detail pages

Each row opens the record's page: "Participant details", "Artifact details", "Negotiation details" or "Offer details". "Back to list" returns to the list as it was. A deleted record opens only with "Show deleted" on; otherwise, and for any unknown ID, the page says "We can't find that page."

## 8. Editing, closing, expiring and cancelling

Press "Edit" on a detail page. Only these can change:

| Record | What can change |
|--------|-----------------|
| Participant | "Display name"; Active or Suspended |
| Artifact | Each subject entity's "Name" and "Description"; the owner participant |
| Negotiation | "Ends at", typed in "Eastern Time (ET).", while it is open and its end time has not passed |
| Offer | Nothing. An offer can only be cancelled. |

"Save" sends only what changed. Success says "Changes saved." "Cancel" puts the values back. Leaving with unsaved changes asks "Discard your changes?" ("Discard" or "Keep editing").

**Actions on an open negotiation:**

- **"Close negotiation"** asks "Close negotiation {ID}?". **"Expire negotiation"** asks "Expire negotiation {ID}?". Both say how many open offers will be cancelled, for example "3 open offers will be cancelled.", and cancel them in the same step.
- Once the end time has passed, only Expire is offered.
- Closed and expired are final.

**Cancelling an open offer:** "Cancel offer" asks "Cancel offer {ID}?" and says "Its amount, currency and terms stay as sent. Cancelled is final." The buttons are "Cancel offer" and "Keep offer".

**When something changed meanwhile.** A party may act while the owner is editing. The party's action always wins. The console then says "This record changed after you opened it. Reload it and try again." and offers "Reload". Nothing of the owner's change is saved. A change the server refuses for another reason says "You can't do that. Nothing was changed."

Every edit, close, expiry and cancel is written to the audit log with the values before and after.

## 9. Deleting, with confirmation

"Delete" is on each detail page and at the end of each list row. It needs JavaScript.

1. **The console asks the server first**, then opens one dialog: "Delete {type} {name}?" (a negotiation or an offer by its ID), "ID: {id}", and what goes with it ("Also deleted with it:" and the counts, or "Nothing else is deleted with it."). It ends with "This can't be restored from the admin."
2. **Typed confirmation.** For a participant that has records going with it, and for an open negotiation, the button works only after typing the name or ID exactly: "Type {value} to confirm".
3. **Press "Delete {type}"**, or "Cancel". Afterwards the list says "{Type} {name} deleted."

**What goes with what:**

| Deleting | Also deleted |
|----------|--------------|
| An offer | Nothing else |
| A negotiation | Its offers. If it is open, the dialog says so: "This negotiation is open and has {n} offers. They will no longer show in the lists unless “Show deleted” is on." |
| A participant | Every negotiation it is a party to, and their offers. Never its artifacts. |
| An artifact | Nothing else. While a negotiation that is not deleted uses it, it cannot be deleted: "This artifact can't be deleted", "It is used by these negotiations:" with links. Delete those negotiations first. |

**If something changed after the dialog opened**, nothing is deleted. The dialog asks again and shows fresh counts: "This record changed after this dialog opened. Nothing was deleted. Review the details again." A confirmation older than five minutes, or replaced by one in another tab, gets "This confirmation is no longer valid. Review the details again."

**If the answer is lost**: "We couldn't confirm the delete. Try again to see whether it went through." If the server could not write the audit entry, the whole delete is undone and the dialog says "Nothing was deleted." with "Retry".

**What "deleted" means.** Delete is soft. The record gets a delete mark; nothing is erased, and there is no hard delete. "Show deleted" still finds it, and the audit log keeps one entry for each record marked. The engine treats a deleted record as if it did not exist: a deleted participant cannot sign in or be named in a new negotiation, and parties cannot read or act on a deleted negotiation or offer. The console has no undo.

## 10. The audit log

"Audit log" is in the menu. "Read-only record of admin authentication and entity changes. This log cannot be edited or deleted."

- **The list** is newest first, with "Time (ET)", "Actor", "Action", "Entity type", "Entity ID" and "Outcome" ("Success", "Failure" or "Denied"). Filter by action (several at once), entity type and a date range ("From", "To"). Page size 50, 100 or 200.
- **An entry** shows its fields, then "Before and after" side by side, with "Changed" beside each field that differs. "IP HMAC prefix" shows the first 12 characters of a keyed hash of the address; no address is ever shown or stored.
- **What is recorded**: sign-ins and failed sign-ins, codes and recovery codes used, setup links, password resets and changes, authenticator changes, new recovery codes, sign-outs, and every edit, close, expiry, cancel and delete (one entry for each record a delete marks, all sharing one correlation id).
- **Never recorded**: passwords, codes, keys, tokens or the plain network address.
- For a failed identity check or password change, the reason is not shown. That keeps the log from telling someone holding a stolen session whether a guessed password was right. The stored entry keeps it.
- Nothing in the console or its API can change or remove an entry: every change method answers 405.

## 11. What the console never does

- **It never changes what a party sent.** An offer's amount, currency and terms are never editable. The field policy gives the owner read-only access to them, and there is no edit path for them. On an offer, the console can only cancel an open one. It cannot set Accepted, Declined or Withdrawn.
- **It never erases.** Delete is soft: a mark, not a removal. There is no hard delete and no way to edit or remove an audit entry.
- **It never writes over a party's action.** If a party acted since the owner opened a record, the owner's change is refused and the party's result stands.
- **It never lets anyone in without a full session.** Every signed-in page and request passes the same session check: a session that exists, used in the last 30 minutes, under 8 hours old, with both sign-in steps done, belonging to the configured owner. The only routes open without a session are the sign-in, first-time setup, authenticator setup, reset and sign-out routes and their scripts, each named on one exact list (`AdminSignedOutExemptions`). The authenticator and code pages also need a sign-in in progress. This hand-off adds nothing to that list.
- **It never accepts a change from another site.** Every POST, PATCH and DELETE needs the console's own Origin and its anti-forgery token.
- **It never shows the network address, a password, a code or a key** in a page, a log or the audit log.
- It has no other admins, no single sign-on, no email codes as a second factor, and no security check other than Cloudflare Turnstile.

## 12. Limits the owner accepted

These are the limits listed in the review records of the merged pieces, T02 to T07, one line each. The pieces merged with these limits stated. The two delete records also record the owner's own acceptance, on 26 Nov 2026. Limits a later piece removed are listed at the end of this section.

### First-time setup

- Setting the password leads to sign-in, not straight to authenticator setup.
- A refused password, and each check of a setup or reset link, uses one slot of the budget that setup and reset share.
- The password-rules note is not in the repository; the code's own rules (15 to 128 characters) apply.
- The detailed UX wording requirements are not in the repository; the wording comes from the server, the October drafts and the test design.
- A refused security check costs the sender nothing and writes one audit entry. Limiting such requests belongs at the network edge, with the deployment.

### Sign-in

- "Back to sign in" on the code page ends the browser's sign-in in progress; the server's record of it lasts its five minutes and cannot be used without the cookie.
- After being sent back to step 1, the email is not filled in again.
- A link from another site lands on "Your session has ended" because the session cookie is not sent across sites; signing in returns to the page.

### Authenticator and recovery codes

- Recovery codes not seen within the five minutes of the sign-in in progress cannot be shown again; new ones are made on Security settings.
- There is no Download button for the codes.
- "Continue" on the codes page expires the browser's cookie; the server's record stays until it runs out, and its copy of the codes is encrypted.

### Password reset

- Reset needs the security check's site key and private key configured; start-up warns when either is missing.
- An out-of-date anti-forgery token on submit shows the retry sentence; re-open the emailed link.
- The retry sentence may show when the change went through; the next press leads to sign-in, which says so.
- If a page script fails to load, reloading cannot help; re-open the emailed link.
- Checking the link again uses a slot of the reset budget.
- The switch to a recovery code is a secondary button, not a link.
- A hand-made link can show either arrival sentence on the real sign-in page.
- A slowed-down reset request shows the neutral sentence in the alert box.
- A reset confirmed at the very moment new recovery codes are made can fail with a server error; nothing is written.

### Checks not yet done in a real setting

- The real Cloudflare widget has never been loaded; every test uses a stand-in.
- No phone has scanned the QR code.
- No screen-reader pass has been done on any page.
- WebKit tests use Playwright's build, not Safari on a Mac.
- No real back/forward-cache test.

### The console shell, session and sign-out

- Browsers too old to send `Sec-Fetch-*` headers cannot sign out with JavaScript off; the request is refused.
- The page "You can't do that." (S-D4) is not built, and `Clear-Site-Data` is not sent.
- The anti-forgery token is not tied to the session nor renewed at sign-in; the Origin check and the header-only token still stop forgery.
- The session-ended sentence is "Your session has ended. Sign in again to continue.", not the test design's "Your session ended. …".
- "Stay signed in" after a new sign-in in another tab shows the retry sentence instead of reloading.
- With several tabs open, each reloads to sign-in at the end; the page kept for afterwards is the last tab that reloaded.
- If the 8-hour end is within 2 minutes after the idle end, only the 8-hour warning shows, with no "Stay signed in".

### Lists and details

- With "Show deleted" on, a deleted owner, party or artifact appears by name in rows and chips without the "Deleted" mark.
- With it off, search can still match the name of a deleted record a row refers to.
- A detail page's own list state is not kept across sign-in.
- Links in table cells are under 24 px high, and the Offers table scrolls sideways at 1280 px.
- The read API's default order, status parsing, list rows without emails and deleted-record detail changed; the console is its only caller.

### Security settings

- A recovery-code sign-in through the older path, used only when no owner account row exists, shows no banner.
- The lock notice and session-ended cases are tested in the browser with stood-in replies.
- If the reply to "Create new codes" or to an authenticator confirm is lost, the new codes are never seen; make new ones again.
- The codes-left count in the banner is out of date on this page until the next page load.
- No notice email is sent after a security change.
- A request cancelled after its failure was counted keeps the count and writes no audit entry.

### Editing

- After a save the page loads itself again rather than updating in place.
- After a conflict, "Reload" drops what was typed.
- A reload, closing the tab or typing an address in edit mode is not asked about.
- After a reload in edit mode, it takes one extra Back to leave the record.
- In edit mode two labels both read "Owner participant".
- Returning to the same record after signing in again is not tested on the edit page.
- A change that changes nothing is refused with "No changes." and writes nothing.
- An artifact with several subject entities needs the entity named.
- Changing only "Ends at" is refused if the negotiation's offers changed since it was opened.
- The engine's new conflict answer also covers a clash that has nothing to do with the console.
- A party can place an offer at the instant a Close is saved, leaving an open offer in a closed negotiation.

### Deleting

- A deleted participant's own artifacts stay live and searchable (Spec §11.2).
- A new offer or negotiation saved at the same instant as a delete can stay live under a deleted parent; the engine never shows it or acts on it.
- Delete requests refuse any query string.
- The audit entries of a participant delete leave out the participant's emails.
- Delete is its own action and does not ask the field policy for a write.
- A blocked artifact lists at most 100 negotiations; the count is always complete.
- At 320 px, a row's Delete is reached by scrolling the table sideways.
- Typed confirmation is asked only for a participant with records going with it and for an open negotiation.
- The delete dialog cannot be used without JavaScript.
- A confirmation lasts five minutes; the dialog does not count down.

### Audit log

- Action names and entity types show as stored, for example `entity_edit`.
- The list has no sort control and no actor filter.
- An unknown action name in the API's filter is refused rather than matching nothing.
- The correlation id is in the API but not on the page.

### Removed by a later piece

- "Do not run `bootstrap-link` yet" and "do not put the console in front of the owner yet": the setup, sign-in, authenticator, reset and console pages are now built.
- "Audit log" missing from the menu: added with the audit log viewer.
- Browser tests not in CI: every suite runs in Chromium, Firefox and WebKit on each pull request.
- Unsaved-form warnings waiting for the edit pages: built with editing.
- The page asked for not kept across sign-in: the console now returns to it.

## 13. To do once before the console goes live

The console is built, not live. Turning it on is a deployment and waits for the owner's OK. Before the first real use:

1. Run the database migrations before starting the new version.
2. Configure the security check's site key and private key, load the real widget once, and confirm that it appears and that the page's content policy does not block it.
3. Set up the mail sending identity, so setup and reset links arrive.
4. Scan the QR code once with a real phone and sign in with its code.
5. Do one screen-reader pass over the sign-in pages and the console.
6. Do one pass in Safari on a Mac.
7. Set up a rate limit at the network edge for refused security checks.
8. Run `bootstrap-link` only once all of the above is done and the console's address is live.
