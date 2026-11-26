# T06a — admin delete with confirmation (A7 Step 10, server side): review

| Field | Value |
|-------|-------|
| Scope | `POST /admin/api/{participants, artifacts, negotiations, offers}/{id}/delete-intent` and `DELETE /admin/api/{type}/{id}`, the confirm-token table, soft delete on the four entities, the engine respecting deletes, and one engine-wide answer for a transaction that collided |
| Branch | `claude/t06-delete-server` (pull request #75) |
| Rules | Story A7 Step 10; Dev Plan Step 10; Spec §9 (confirm before delete), §10 (audit), §11 (soft delete, cascades, open negotiation); test design TD-ADM-060, 061, 090 to 095, 130 |
| Reference | Draft pull request #43. Its delete plan, cascade and token logic are ported with changes. Its pages, its second read API, its seed route and its test harness are not used. |
| Date | 2026-11-26 |
| Result | **MERGE**, after the fixes listed below |

## What was built

1. **Two steps, never one.** The intent names the record, counts what goes with it (and the open negotiations and open offers among them), and hands out a confirm token. The DELETE deletes only with that token in the `X-Admin-Confirm-Token` header. Without one it is refused (428). Any query string is refused (400), so a token or a "hard delete" switch in a URL is never read.
2. **The token.** 32 random bytes; only its SHA-256 is stored. It is good for five minutes, once, in the session that asked for it, for the one record it names, and for the exact set of rows the delete will mark, each with its id, version, time of last change and the state a party can change. A new intent for the same record replaces the earlier token. A token that is unknown, used, run out, from another session or for another record is a 409 with nothing deleted.
3. **Soft delete only.** `DeletedAt`, the next `Version` and the time of the change; nothing is erased and there is no hard delete. An offer's status, amount, currency and terms are never written.
4. **Cascades (Spec §11.2, §11.3).**
   - An offer takes only itself.
   - A negotiation takes its live offers. If it is open, the intent says so and gives the offer counts.
   - An artifact takes only itself. It is refused (409, with the ids of the negotiations) while a negotiation that is not deleted refers to it.
   - A participant takes every negotiation it is a party to and their offers. It never takes an artifact.
   - Rows already soft-deleted are left as they are, and a soft-deleted record cannot be deleted again (404).
5. **No write over a party's action.** Inside one transaction the delete uses the token (a conditional update, so two confirms of one token delete once), claims every row in a fixed order on the state the token was made for, and checks that no row has joined the set. Any of these failing is a 409 with nothing written; so is a PostgreSQL deadlock or serialization failure.
6. **Audit.** One `entity_delete` row per marked row, all with one correlation id, in the delete's transaction. Before and after go through the field policy. Asking for an intent and not confirming it writes no audit row.
7. **The engine respects deletes (owner's decision, 26 Nov 2026).** The engine's own lookups leave soft-deleted rows out, so each refusal is the engine's existing answer for an id that does not exist:
   - A deleted participant cannot sign in (API key, or a JWT issued before), cannot get a new JWT, and cannot be named as a counterparty.
   - A party cannot read or act on a deleted negotiation or offer.
   - No negotiation starts on a deleted artifact.
   - Lists and search leave deleted rows out.
   - A deleted offer is not counted as a side's open offer, and an accept or a close does not write it.
8. **Gates.** Both routes need the Core owner's session, the Origin check and the anti-forgery token. No route is exempted.
9. **The token table.** Migration `20261126000600_AdminDeleteConfirmTokens` adds one new table only. Each row is deleted with its session.
10. **One engine-wide answer.** An engine request whose transaction PostgreSQL stopped as a deadlock or a serialization failure gets the engine's 409, with nothing of it saved. Any other database fault stays a 500.

## How this was reviewed

Two reviewers read the committed code. Neither wrote it. The builder and the reviewers were separate Claude Code agents.

- **The first review** read the delete API itself. It found the code sound, raised one question for the owner and asked for one small fix.
- **The second review** read the engine change made after the owner's decision. It found no blocking issue.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important (owner's decision) | The engine ignored `DeletedAt`. A deleted participant could still sign in and act, and parties could still act on deleted negotiations, offers and artifacts. | Fixed, by the owner's decision. Lookups and lists leave deleted rows out, with the unknown-id answer. Five tests show each refusal equals the unknown-id or unknown-credential answer; all five fail with the change taken out. |
| 2 | Minor | The artifact's name in the intent and in the audit row was taken from its first subject entity as loaded, in no fixed order. | Fixed. The subject entity with the lowest id names it, the order the edit token uses. A test deletes a two-entity artifact and checks both. |
| 3 | Low, policy | A deleted participant's own artifacts stay live and searchable, because a participant delete never takes an artifact. | Not changed: Spec §11.2 says a participant delete must not take artifacts. An open limit for the owner to accept. |
| 4 | Low, tests | No test closed a live negotiation that holds a deleted open offer. | Added. The close goes through, and the deleted offer keeps its status, version, time of last change and delete mark. |
| 5 | Optional | No test reaches a deleted offer through the Muse `list_offers` or Dot interact connectors. No test compares an inactive participant with a deleted one. | Not done. The connectors use the same negotiation lookup, and the two participants share one sign-in path. |
| 6 | Note | A party insert (a new offer, or a new negotiation for the participant) that commits at the same instant as the delete can leave one row live under a deleted parent. An insert does not lock its parent row. | Not changed. The check inside the delete's transaction narrows it to that instant, and every party request after the commit is refused. The engine never shows such a row or acts on it, because its parent is deleted. |

The reviewers found:
- No token reaches a URL, a log line or an audit row. Only its hash is stored.
- Every refusal writes nothing and no audit row.
- Soft-deleted and unknown records get the same 404.
- Both routes sit behind the session, Origin and anti-forgery gates with no exemption.

## Tests

- **`AdminDeleteApiTests`**, on Windows with SQLite: 67 tests, 63 passed, 4 skipped. The four skipped are PostgreSQL-only, each with two live transactions:
  - two confirms of one token at once, which delete once
  - a party accept holding the offer, so the delete waits and then gets 409
  - a delete holding the offer, so the accept waits and then gets the engine's 409
  - a party accept holding an offer, so the negotiation's delete waits and then gets 409
- **Mutation checks:**
  - With the token's conditional update removed, the replay test fails.
  - With the offer claim's status check removed, the in-request accept test fails.
  - With the engine change taken out, all five engine tests fail.
- **The migration test** shows the previous release's rows and queries survive migration 600, the token hash is unique, tokens go with their session, and its Down drops only the new table.
- **Full suite on Windows** after the engine change: 2,140 tests, 2,125 passed, 10 skipped, 5 failed. The same five fail on untouched `main` on this machine:
  - `AdminTotpTests.SecSc6_NewServiceInstanceKeepsFactorLock`
  - four `PostmanCollectionHygieneTests.RunNewmanScript_*`
- **CI:** the PostgreSQL job runs the whole `AdminDeleteApiTests` class on every push, including the four live-transaction tests.

## New wording, for the owner's approval

The delete API answers with three new sentences, kept in `AdminDeleteCopy.cs` to be replaced. No document gives the wording (test design OQ6).

| Key | Sentence | When |
|-----|----------|------|
| `TokenInvalid` | "This delete confirmation is no longer valid. Nothing was deleted. Open the delete again." | The token is unknown, used, run out, from another session or for another record (`error: ConfirmTokenInvalid`) |
| `Changed` | "This record or something deleted with it changed after you opened the delete. Nothing was deleted. Open the delete again." | The cascade set moved, a row was changed, a row joined the set, or the transaction collided (`error: Conflict`) |
| `ArtifactBlocked` | "This artifact cannot be deleted while negotiations refer to it. Delete those negotiations first." | An artifact that a negotiation which is not deleted still refers to (`error: Blocked`, with the negotiation ids) |

## Limits to know about

1. The delete dialog must send the token in `X-Admin-Confirm-Token` together with the anti-forgery header. It should show `ConfirmTokenInvalid` and `Conflict` as "open the delete again", and show `Blocked` with its negotiation ids (at most 100 are listed; the count is always complete).
2. Every DELETE under these routes refuses any query string.
3. The audit rows of a participant delete leave the participant's emails out, to keep personal data out of an audit log that is kept indefinitely.
4. The delete does not ask the field policy for a write: the policy gives the Core owner read only on `SoftDeletedAt`, and delete is its own action under Spec §9 and §11.
5. The engine's new 409 for a collided transaction also covers a collision that has nothing to do with an admin write.
