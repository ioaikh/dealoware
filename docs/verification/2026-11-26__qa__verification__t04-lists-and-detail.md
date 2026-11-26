# T04 piece 2 — console list pages and detail pages: review

| Field | Value |
|-------|-------|
| Scope | The four list pages (S-B2 to S-B5), detail pages (S-B6), and the read-API changes behind them |
| Branch | `claude/t04-lists-and-detail` |
| Rules | Story A7 Step 7; Spec §4.5, §4.6, §6 and the field policy; test design TD-ADM-006, 060–066, 070, 130, 140 and TD-ADM-UI-na-B01 to B09, B14, B15, X04 |
| Reference | Draft pull request #39, for list definitions and labels |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below |

## How this was reviewed

A reviewer who did not write the code read the change adversarially, covering, in order:
1. data exposure through the field policy and deleted records;
2. injection and encoding;
3. the return path;
4. the session;
5. regressions to the read API;
6. the test seed.

It then re-reviewed the fix commits. The builder and the reviewer were separate Claude Code agents. A third pass ran the full suite.

## What the change does

- **The four lists.** The server writes each list from its address, through the same query functions as the JSON API, so the field policy and paging are the same in both. Sorting, filters, chips, paging (50, 100 or 200) and "Show deleted" are plain GET forms and links, so the lists work without JavaScript. A small script adds focus handling, an "N results" announcement, and two pickers that fetch one page of matches after a pause in typing. Nothing polls the server.
- **Detail pages** show the record as text. A soft-deleted record shows only with the toggle on; otherwise the page is S-D3 with a 404.
- **Read API changes:**
  - names joined into negotiation, offer and artifact rows, each through the field policy;
  - `updatedAt` on every row, and Updated as the default order;
  - the participant status filter now filters;
  - several statuses can be given at once;
  - participant list rows carry no email (the detail page keeps them);
  - a soft-deleted record's detail needs `includeDeleted=true`.
- **Values the page drops.** A value the list does not take is dropped by the page before it asks the server; the API itself keeps its 400.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important (CI) | On Linux Chromium the keyboard test read the focus ring before the stylesheet had applied. | Fixed in the test: it waits for the stylesheet and expects a solid 2px ring in every engine. |
| 2 | Minor | A date filter of `9999-12-31` made the list page answer 500, and the value was kept across sign-in. | Fixed. Only dates in the years 1900 to 2999 are kept, both in the page and in the return path. |
| 3 | Minor | With "Show deleted" off, the name of a soft-deleted participant or artifact still appeared in list rows and filter chips. | Fixed, in rows, chips and the JSON API. With the toggle off, a deleted reference shows as its id with "Deleted" and no name. With the toggle on, its name shows. |
| 4 | Minor | A live row could link to a deleted artifact or negotiation, and the link led to a 404. | Fixed with 3. With the toggle off such a reference is not a link. With it on, links carry `includeDeleted=true`. |
| 5 | Minor | After Back restored a page from the back/forward cache, filters, sorting and paging could stop working. | Fixed. |
| 6 | Note | Callers of the JSON API see the changed behaviour: default order, status parsing, emails dropped from list rows, deleted detail. | Accepted. These changes are intended, and the console is the only caller. |
| 7 | Note | The return path kept API sort keys that the page then drops. | Fixed. It keeps only the sorts the page's column headers offer. |
| 8 | Note | No test showed that list pages keep the anti-forgery token out of GET forms. | Test added. |
| 9 | Note | No test showed that the entity seed script refuses another database. | Test added. |
| 10 | Note | A picker could send its earlier choice after new text was typed. | Fixed. Typing clears the choice. |

The reviewer found:
- **No data leak.** Every joined name needs both the reference and the name to be allowed by the field policy.
- **No injection.** Every value is HTML-encoded, and the script uses `textContent` only.
- **No open redirect.**
- **No unbounded read.** Counts and paging run in SQL, the page size is capped at 200, and related rows are read in batches by key.
- **Nothing keeps the session alive.**

## Tests

- Full suite on Windows after the fixes: 1,808 tests, 1,802 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on this machine.
- `e2e/console`: 39 tests in each of Chromium, Firefox and WebKit, 15 of them for the lists. On GitHub all five checks passed on the fixed head.

## Limits to know about

1. With "Show deleted" on, a deleted owner, party or artifact appears by name without the "Deleted" mark in rows and chips. The API sends the mark. To be added when the lists are next touched.
2. With the toggle off, search can still match a deleted referenced name. This behaviour was already there before this change. Soft-delete cascades keep it from happening on consistent data.
3. A detail page's own query (the toggle and the list state) is not kept across sign-in.
4. Links in table cells are under 24 px high, under the WCAG 2.5.8 inline exception. At 1280 px the Offers table scrolls sideways inside its own region.
5. New copy (labels, detail headings, "Apply filters", "No matches") is listed in the builder's report. Ivan approved the T04 wording on 26 Nov 2026.
