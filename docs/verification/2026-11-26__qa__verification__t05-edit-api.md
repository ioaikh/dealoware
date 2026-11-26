# T05a — admin edit API (A7 Step 9, server side): review

| Field | Value |
|-------|-------|
| Scope | `PATCH /admin/api/{participants, artifacts, negotiations, offers}/{id}`, the domain changes behind it, one field-policy change, and one engine-wide answer for a save that lost a race |
| Branch | `claude/t05-edit-server` |
| Rules | Story A7 Step 9; Dev Plan Step 9 (edit paths E1 to E12); Spec rules for PATCH with If-Match and for audit; test design TD-ADM-005, 060, 061, 080, 081, 130, 141, 178 |
| Reference | Draft pull request #41. Its server code is ported with changes. Its page shell and test harness are not used. |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below |

## Two decisions that narrow the October plan

1. **On an offer the console can only cancel.** An offer's amount, currency and terms stay as the party sent them. This is product rule E3.7; the owner decided it on 26 Nov 2026. Edit paths E9 to E11 are not built, and the field policy no longer lets the Core owner write those three fields.
2. **An admin write can never save over a party's action.** Party actions do not move `Version`, so each admin write is checked twice against the state it was prepared for:
   - **The token.** It covers the row's version, its time of last change, its status, the values the admin can overwrite and, for a negotiation, its open offers.
   - **The claim.** A conditional update inside the write's transaction holds the row until commit.

   A party action in between gives the owner a 409, and the party's result stands. No party action's own logic changes.

## How this was reviewed

A reviewer who did not write the code read the committed code adversarially while the builder was still finishing its tests, then made a second pass over the fixes. It checked the race with party actions first, then:
- the offer-history rule;
- authorization and the gates;
- If-Match handling;
- audit;
- the domain rules;
- regressions.

The builder and the reviewer were separate Claude Code agents. A third pass read the one change that touches the engine's routes.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important | No edit test ran on PostgreSQL. The race tests could not show two live transactions on SQLite. | Fixed. The PostgreSQL CI job runs the edit class. Five PostgreSQL-only tests hold one transaction open, check that the other request is waiting on its row lock, then let the first commit. Exactly one side wins, the other gets 409, and an accepted offer is never overwritten. With the claim weakened on purpose, two of them fail. |
| 2 | Important | A Close or Expire with an old token cancelled an offer the admin had never seen: one a party placed or countered after the admin read the negotiation. | Fixed. The negotiation's token covers its open offers, so that case is a 409 and the admin reads again. |
| 3 | Minor | An engine request that lost a race to an admin write got 500. | Fixed. One handler maps that failure to 409 on the engine's routes, in the engine's existing error shape. Nothing of the losing request is stored. |
| 4 | Minor | Changing `endsAt` could revive a negotiation whose end time had passed, or close it instead of expiring it. | Fixed. Once the stored end time has passed, only Expire works. |
| 5 | Minor | The close cascade did not ask the field policy before writing offer status. It also treated soft-deleted open offers differently from the count shown. | Fixed. One rule applies to both. |
| 6 | Minor | The token was not bound to its row, so two rows in the same state shared a token. | Fixed. Entity type and id are in the hash, and the test uses two rows in identical state. |
| 7 | Note | The cascade's row claims had no fixed order, so a deadlock on PostgreSQL was possible. | Fixed. Claims go in id order, and a deadlock or serialization failure answers 409 with nothing written. |
| 8 | Note | A value of the wrong JSON type reported "too long". The audit address fallback differed from the session gate's. | Fixed. |
| 9 | Note | A party can place an offer at the instant an admin Close commits. An open offer then sits in a closed negotiation. | Not changed. It predates this piece (two parties can do the same to each other), and closing it means changing the party's place-offer action. Recorded for the engine work. |

The reviewer found:
- Amount, currency and terms are refused twice: by the policy, and by there being no edit path.
- The request body is parsed by hand, so no unexpected property is bound.
- The four routes need the Core owner's session, the Origin check and the anti-forgery token, with no exemption.
- Soft-deleted and unknown ids give the same 404.
- Every write and its audit row share one transaction. Snapshots go through the field policy.

## Tests

- Full suite on Windows after the fixes: 1,966 tests, 1,955 passed, 6 skipped, 5 failed. The same five fail on untouched `main` on this machine. The six skipped are the five PostgreSQL-only tests and one older placeholder.
- The edit class on a local PostgreSQL 15: 144 of 144. The PostgreSQL job on GitHub runs it on every push.

## Limits to know about

1. The page must send back the `ETag` header as `If-Match`. The token is not the bare version number.
2. A request that changes nothing gets 400 "No changes." and writes nothing.
3. An artifact write must name the subject entity when the artifact has several.
4. The engine's new 409 also covers a save that failed the same way for a reason other than an admin write.
5. An `endsAt`-only edit gets 409 if the negotiation's offers changed since the read.
6. New wording, in `AdminEditCopy.cs` and the engine's 409 sentence, is listed in the builder's report for the owner's approval.
