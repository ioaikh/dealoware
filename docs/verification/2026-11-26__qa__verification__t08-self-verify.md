# T08b — admin console self-verify before hand-off (Dev Plan Step 13)

| Field | Value |
|-------|-------|
| Scope | The last checklist of the admin console's Dev Plan ([Step 13](../plans/2026-10-05__devplan__plan__core-admin-dashboard.md), Story [A7 Step 13](../plans/a7-stories/2026-10-05__pm__note__core-admin-dashboard-a7-step-13.md)), and the process cases of the [test design](../qa/2026-10-05__qa__test-design__core-admin-dashboard.md) §4.13 that are checked by hand: TD-ADM-120, 121, 122, 160, 161 and 162 |
| Checked against | `main` at `377296c` (pull request #78 merged) |
| Branch | `claude/t08-hand-off` |
| Hand-off document | [The owner's guide to the admin console](../product/2026-11-26__product__guide__core-admin-console.md) |
| Date | 2026-11-26 |
| Result | **Met, with the gaps stated below.** Three items wait for the owner: the October pull requests, the S-D4 page, and the audit log's four new words. |

## How this was checked

Each line was walked against the code, the tests and the merged review records on `main`. Where a line names a test, the test is on `main`. Where it names a record, the record is under `docs/verification/`. No code was changed for this record.

The denial checks of T08a (one route table, a denial test over every admin route, a signed-out browser walk, and the offer-history test) are in pull request #80, still a draft when this was written. Lines that lean on them say so.

The agent teams named in the October gates (Code QA, Security, UI/UX) are not running. Since T02, each piece was instead reviewed by a separate agent who did not write it, adversarially and with security first, then re-reviewed after fixes, with browser tests in Chromium, Firefox and WebKit standing in for the UI/UX gate. Each record says so under "How this was reviewed". The lines below that name an October gate say how it was met in that form.

## Dev Plan Step 13 checklist

| # | Line | Status | Where it is shown |
|---|------|--------|-------------------|
| 1 | Host only `admin.core.dealoware.com`; one owner (CoreOwner); field policy dual wall; no parallel ACL | Met | `AdminHostOptions.ProductionAdminHost` and `AdminHostOptionsValidator` (outside Development the allow-list must be exactly that host); `AdminHostStartupTests`, `AdminHostFailClosedExitTests`; the owner is the configured `CoreOwner:Email`; `CoreOwnerFieldPolicyTests.TdAdm007_NoParallelAcl_UserEmailIsNotCoreOwner`, `AdminSurfaceTests.TdAdm007_NoOperatorAclEndpoints` |
| 2 | Bootstrap, TOTP, reset by link plus second factor, Turnstile only, lockout and session per Spec | Met | Records [a7-step-02-bootstrap](2026-11-26__qa__verification__a7-step-02-bootstrap.md), the six `a7-step-14-*` records and [t04-session-warnings](2026-11-26__qa__verification__t04-session-warnings.md); `AdminBootstrapTests`, `AdminTotpTests`, `AdminPasswordResetTests` (`TD_ADM_030_Complete_RequiresTokenAnd2Fa_Skip2FaAbsent`), `TdAdm040AuthLockoutSessionTests` (`TdAdm041_NoNonTurnstileCaptchaProviders`), `AdminSessionTimerTests`; limits in `AdminSession` (30 minutes idle, 8 hours) and `AdminAuthService` (5 failures in 15 minutes, 30-minute lock) |
| 3 | Raw IP in counters only; audit IP as keyed HMAC-SHA256; mail through the mail interface; no AWS SDK in Core | Met | `IpHasher`; `AdminAuditAuthEventTests.TdAdm100_AllAuthEvents_AreWrittenWithReasonClassAndHmacIp`; the T07 review found no raw IP anywhere; `AdminMailInterfaceTests` (`TD_ADM_110_*`); `AdminOutPackTests.TdAdm110_NoAwsSesSdkPackageReference` |
| 4 | Lists, search, paging, stats, edit, delete, confirm, soft delete, cascades, audit per Spec | Met | Records [t04-lists-and-detail](2026-11-26__qa__verification__t04-lists-and-detail.md), [t04-console-shell](2026-11-26__qa__verification__t04-console-shell.md), [t05-edit-api](2026-11-26__qa__verification__t05-edit-api.md), [t05-edit-page](2026-11-26__qa__verification__t05-edit-page.md), [t06-delete-server](2026-11-26__qa__verification__t06-delete-server.md), [t06-delete-dialog](2026-11-26__qa__verification__t06-delete-dialog.md), [t07-audit-viewer](2026-11-26__qa__verification__t07-audit-viewer.md); `AdminReadApiTests`, `AdminListPageTests`, `AdminConsoleShellTests.Stats_*`, `AdminEditApiTests`, `AdminDeleteApiTests`, `AdminAudit*Tests` |
| 5 | No password values; no AWS account IDs; no process-global flood limiter without approval | Met | `AdminMailInterfaceTests.TD_ADM_150_MailSources_OmitSecretsAndAwsAccountDetails`, `AdminOutPackTests.TdAdm150_IpHasherKeyNameOnly_NoHardCodedKeyInSource`, `AdminBootstrapTests.TD_ADM_150_NoTokenPasswordOrHashInLogsOrAudit`, `AdminOutPackTests.TdAdm054_NoProcessGlobalAuthFloodLimiterType`, `TdAdm040AuthLockoutSessionTests.TdAdm054_NoProcessGlobalAuthFloodLimiter` |
| 6 | Deploy, harden and UI-lane gates respected | Met for deploy; met differently for the UI lane | No admin piece deployed anything; the console is built, not live, and turning it on waits for the owner's OK (guide §13). The UI-lane approval was met in the form described above (browser tests in three engines, axe scans, keyboard-only runs, screenshots); there was no UI/UX team to sign. |
| 7 | Step 7 W1–W5 and Step 9 E1–E12 delivered per the coverage list; no Withdrawn writer; no migration beyond the cited `UpdatedAt` change | Met, narrowed by the owner | W1–W5: `OfferStatus.Withdrawn = 5`, the Offers status filter and badge. No route sets Withdrawn: `Offer.Withdraw` (maker-only) has no caller in `src/`, and the edit API refuses it (`AdminEditApiTests.TdAdm178_AnOutcomeTheOwnerMayNotSet_Refused`). E1–E12: built except E9–E11 (offer amount, currency, terms), which the owner removed on 26 Nov 2026 so an offer stays as sent ([t05-edit-api](2026-11-26__qa__verification__t05-edit-api.md), "Two decisions"). No migration was added for Withdrawn or for E1–E12. The other admin migrations add tables for sign-in, security settings (`20261126000400`), the recovery banner (`…500`) and delete confirmations (`…600`); the November ones are additive only and each is reviewed in its record. |
| 8 | No MotorMarket or DC4; no Cognito; no paid LLM provisioned | Met | No such name in `src/`; no admin piece adds a provider. |
| 9 | Security points 1–14 evidence ready for the Security confirm | Met differently | Each record's adversarial review covers the security points of its piece. No Security confirm file was issued in the October form, because no Security team is running. |
| 10 | UI Definition of Done (UX1-A11): UI cases mapped to UX1-A01–A10 through UXR IDs, and green | Partly met | The test design's TD-ADM-UI cases named in each record are green in CI on Chromium, Firefox and WebKit (`.github/workflows/admin-ui-ci.yml`). The mapping to UXR IDs could not be checked: the UX addendum that holds UXR-A01 to A45 is not in this repository, as the setup-page record notes. |
| 11 | Steps 14 and 15 checklists complete; UI approval recorded at review | Met differently | Step 14: the six `a7-step-14-*` records. Step 15: [t07-audit-viewer](2026-11-26__qa__verification__t07-audit-viewer.md). Approval in the form above. The owner's acceptances of each piece's limits and wording are listed below. |

## Process cases checked by hand

| Case | What it asks | Status | How it is met |
|------|--------------|--------|---------------|
| TD-ADM-120 | Nothing on the OUT list is delivered | Met, with one note | Human-user list: `AdminSurfaceTests.TdAdm006_NoHumanUserListEndpoint`, `AdminReadApiTests.TD_ADM_006_ParticipantsList_HasNoHumanUserSurface`. Several admins or an operator ACL: `TdAdm007_*`. Email OTP: `AdminTotpTests.TdAdm021_EmailOtpFallbackPathAbsent`. Other CAPTCHA: `TdAdm041_NoNonTurnstileCaptchaProviders`. Platform-admin host: the host validator above. Hard delete: `AdminDeleteApiTests.TdAdm091_SoftDeleteOnly_NothingErased_NoHardDelete`. No SSO, settlement, escrow, checkout, charts, warehouse, MotorMarket or DC4 code under `src/`. Stats are five counts. The app already targets `net10.0`; no retarget was part of this work. **Note:** the engine's own budget meters and its basic participant page (both from September) and its `/connectors/*` routes (4 Oct, commit `1c9d10b`) are older than the admin track and outside it. The admin pieces did not add or change them. |
| TD-ADM-121 | Gates held: no Stories or build before the test design PASS; deploy held until the owner's OK; no invented passwords or AWS values | Met | The test design PASS and the Dev Plan PASS are on `main` (see TD-ADM-122). Build started under the owner's instruction, with the review form above, recorded in each piece. Nothing was deployed. Passwords, keys and account details: line 5 above. |
| TD-ADM-122 | Traceability: the Spec's sha256 matches, and the Security records are cited | Met | The Spec on `main` is byte-identical to the cited v2.2: its committed blob hashes to `25f26477…` (a checkout with Windows line endings hashes differently; the stored file does not). The Dev Plan's blob hashes to `b3788114…` and the test design's to `7bb1dd3b…`, as the Step 13 Story cites. The Security confirm files are on `main`: the Spec confirm (PASS 15/15), the SA confirm (14/14), and the Dev Plan checklist and confirm. |
| TD-ADM-160 | The inbound bot connector is not in this delivery | Met | No admin piece adds or changes a connector. `src/Dealoware.Api/Endpoints/InboundConnectorEndpoints.cs` was last changed in `1c9d10b` (4 Oct), before the admin track. |
| TD-ADM-161 | Every must-cover area has at least one P0 automated case | Met | Sign-in (§8): `AdminBootstrapTests`, `AdminTotpTests`, `AdminPasswordResetTests`, `TdAdm040AuthLockoutSessionTests`, `AdminSessionTimerTests`. Confirm before delete (§9): `AdminDeleteApiTests.TdAdm090_*`, `AdminDeletePageTests`, `e2e/console/delete.spec.ts`. Audit (§10): `AdminAuditApiTests`, `AdminAuditPairingTests.TdAdm102_*`, `AdminAuditViewerApiTests`, `AdminAuditPageTests`. Soft delete and cascades (§11): `AdminDeleteApiTests.TdAdm091_*` to `TdAdm095_*`. Lists and search (§4.5, §4.6): `AdminReadApiTests.TD_ADM_060_*`, `AdminListPagingInterceptorTests.TD_ADM_065_*`, `AdminListPageTests`, `e2e/console/lists.spec.ts`. |
| TD-ADM-162 | Everything on the Spec §12 IN list is delivered | Met, with one narrowing | Host: line 1. List, view, edit and delete for the four types with name search, all statuses, sort, filter and server paging: line 4. Stats: `AdminConsoleShellTests.Stats_ShowsTheFiveTotalsOfTheReadApi_AsText_WithTheTimeInEt` and `Stats_CountsChange_WithTheData_AndLeaveSoftDeletedRowsOut`. One owner and the field policy: line 1. Sign-in §8: line 2. Confirm before delete: TD-ADM-161. Audit log, read-only: `AdminAuditViewerApiTests.TdAdm101_NoMethodChangesAnEntry_EveryOtherMethodIs405_AndTheEntryStaysAsItWas`. Soft delete, cascades and the toggle: TD-ADM-161 and `AdminAuditApiTests.TdAdm064_IncludeDeletedToggle_IsNotAudited`. Mail interface: line 3. **Narrowing:** on an offer the owner can only cancel; amount, currency and terms are read-only (the owner's decision of 26 Nov 2026). That every refused route leaves the offer history unchanged is the offer-history test of pull request #80, not yet on `main`. |

## The owner's acceptances

Guide §12 is the one list of the limits. What the owner accepted, piece by piece:

| Piece | Acceptance |
|-------|------------|
| T02, first-time setup | No separate acceptance is recorded. The limits its record lists (setup leads to sign-in, not straight to authenticator setup; a refused password takes a slot of the shared setup and reset budget; the password-rules note is not in the repository; and "do not run `bootstrap-link` yet", since removed by the setup page) merged as written. |
| T03, sign-in screens | The open limits accepted on 26 Nov 2026. |
| T04, lists, detail, totals, security settings, session warnings | The open limits accepted and the new wording approved on 26 Nov 2026, including the session-warning sentences and the security settings page wording. |
| T05, editing | The six open limits accepted and the new wording approved on 26 Nov 2026, including the edit page's words. |
| T06, deleting | The six open limits accepted and the new wording approved on 26 Nov 2026, including "This confirmation is no longer valid." |
| T07, audit log viewer | The open limits accepted on 26 Nov 2026. The words "Success", "Failure", "Denied" and "Reason" have no recorded approval yet: waiting for the owner (question 3 below). |

### Checks run for this record

On Windows, against `main` at `377296c`, with SQLite:

- The OUT-list, policy, host and audit tests named above (`AdminOutPackTests`, `AdminSurfaceTests`, `CoreOwnerFieldPolicyTests`, `AdminHostOptionsValidatorTests` and every `TdAdm006`, `007`, `021`, `041`, `054`, `091`, `100`, `101`, `TD_ADM_006`, `110` and `150` case): 175 passed, none failed.
- The must-cover and IN-list tests named above (Stats, `TdAdm040`, `064`, `090` to `095`, `102`, `178`, `TD_ADM_030`, `060`, `065`, `178`, `AdminSessionTimerTests` and the host start-up tests): 194 passed, 4 skipped, none failed. The four skipped need PostgreSQL; the PostgreSQL job runs them on every pull request.

No repository check covers documents (there is no link checker or document test), so none was run. The links in this record and in the guide were checked by hand against the files on `main`.

## The October drafts

The six admin drafts of 6 October are closed, each with a comment naming the pull requests that replaced it:

| Draft | Replaced by |
|-------|-------------|
| #36 first-time setup | #51 |
| #38 sign-in screens | #52, #54, #56, #57, #59, #60 |
| #39 lists, detail, stats | #64, #69, #67, with #63 and #68 |
| #41 editing | #71, #74 |
| #42 audit log viewer | #70 |
| #43 delete with confirmation | #75, #78 |

Three older pull requests are still open and are not admin console work. **They wait for the owner's decision; none was closed here.**

| Pull request | What it is | Recommendation |
|--------------|-----------|----------------|
| #40 (6 Oct) | Dependabot: Playwright bump in the old `e2e/` folder | **Close.** It is seven weeks old, and Dependabot opens a fresh one if the folder still needs it. |
| #48 (20 Oct) | A user guide for the negotiations API | **Keep open** for the later step that publishes the API reference. |
| #49 (20 Oct, draft) | Postman test hardening | **Keep open** for the later end-to-end run of the engine. |

## Questions for the owner

1. **The three open pull requests above.** Recommended: close #40; keep #48 and #49 for later steps.
2. **The "You can't do that." page (S-D4) was never built.** Neither the Spec nor the Story says when it shows, and no signed-in page answers 403 today. A refused change already shows its sentence inside the page ("You can't do that. Nothing was changed."), which the test design allows (TD-ADM-UI-na-B13). **Recommended: do not build it.**
3. **The audit log's four new words**, "Success", "Failure", "Denied" and "Reason" (record t07-audit-viewer), have no recorded approval. Recommended: approve them as they are on `main`.

## What this record does not cover

- The denial checks, the signed-out browser walk and the offer-history test: pull request #80 (T08a).
- Anything about turning the console on: guide §13 lists what must be done once before it goes live.
- The index: the guide is not yet listed in `docs/INDEX.md`. The owner adds it there, or it is added once the owner's own pending change to that file is in.
