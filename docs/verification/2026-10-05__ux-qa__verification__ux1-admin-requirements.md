# UI/UX QA verification: UX1 admin requirements findings

| Field | Value |
|---|---|
| Verifier | Dealoware UI/UX QA (independent of the authors) |
| Checked | 2026-10-05, about 8:00pm ET |
| Artifact verified | `/workspace/dealoware-kb/ux/2026-10-05__ux__findings__ux1-admin-requirements.md`, sha256 `5154b0a29ec4e8735265607038a627a31bfa008b80bcb7bed336507b1ac5ab4e` (the `/workspace/uiux-out/` mirror is byte-identical) |
| Brief | `/workspace/dealoware-kb/ux/2026-10-05__ux__review-plan__ux1-admin-requirements.md`, items 1 to 7 and Outputs |
| Repo pin | ioaikh/dealoware `4911b08` (confirmed it exists; it is the PR #18 merge) |
| Verdict | **BOUNCE to Senior UI/UX.** The findings are substantively strong, but 1 row cites the wrong source, 5 rows are stale against decisions already on file, 2 rows contradict their own Resolutions log, and 2 plan outputs are missing. Fix list below. This is not a PASS. |

## What I re-checked and confirmed

| Check | Result | Evidence |
|---|---|---|
| Source hashes | PASS | I recomputed sha256 for the A3 Spec, A2 SA and A5 plan KB copies, and `git show 4911b08:<path>` gives the same three values (`25f26477…cf2e51`, `a7fe7b67…2b41b27`, `5fefb550…3bd4eaf`). A6 `870f348c…7bca7` and A7 briefs `0365e99f…29ee` match too. |
| Row count and tally | PASS | 52 rows: 16 blocker, 33 major, 3 minor. Matches the tally table. |
| One row per gap with source, section, severity, owning team and testable criterion | PASS | Every row has all five fields. |
| No WCAG or accessibility requirement in sources (A10, X05) | PASS | `rg -i 'wcag|accessib|aria|keyboard|contrast|responsive'` across A1, A2, A3, A5, A6, A7 briefs and all 13 A7 Stories finds no accessibility requirement. |
| Spec §7 says only "thin admin UI" (X01) | PASS | Spec line 227; SA lines 56 and 86. |
| A6 UI coverage is smoke only (X07, A12, C08) | PASS | TD-ADM-130 and 131 are E2E-UI-smoke; 040 and 140 are API+UI-smoke; nothing else has a UI layer. |
| A6 OQ1, OQ2, OQ5, OQ6 cited correctly | PASS | A6 lines 866 to 871. |
| Product says reset is "Email link plus 2FA code" (A07) | PASS | Product scope line 82; Spec §8.4 line 272 says TOTP or recovery code. |
| A13 resolution evidence exists | PASS | Arch note and Arch QA file are present; the QA file reads PASS (line 7) and says neither counter moves on a Turnstile outcome (line 20). |
| Retired phrase | PASS | The findings file does not use it. |

## Fix list (each must be closed before I can pass)

| # | Row | Problem | Evidence | Required fix |
|---|---|---|---|---|
| Q1 | UX1-B03 | **Wrong source.** The row says Spec §4.5 lists negotiation statuses "Open/Closed/Expired + Withdrawn". It doesn't. Spec §4.5 lists the Product set (open, accepted, declined, expired, withdrawn) for both lists, plus Core-cited Negotiation Closed and Offer Superseded and Cancelled. The "Open, Closed, Expired, Withdrawn" list comes from A7 Brief 1. | Spec lines 136 to 137 and 146; SA line 191; A7 briefs line 80 | Re-cite the conflict as three sources: Spec §4.5 (Product set plus Closed, Superseded, Cancelled, not split per entity), SA §3.9 (Open, Accepted, Declined, Expired, Withdrawn, with no Closed), and A7 Brief 1 (no Accepted or Declined on negotiations). Note Chief Dev decision 4 ("follow the Spec") in briefs.md. The blocker stands, because the Spec still doesn't give one list per entity. |
| Q2 | UX1-A14 | **Stale.** "No other brief or PR takes Plan Steps 2–6" is out of date. Chief Dev decision 2 (7:53pm) owns Steps 2 to 6 on the server side. The PM checklist (7:58pm) types Stories 2, 3, 4 and 5 as UI with the Chief Developer as owner. Brief 4 still marks the auth pages OUT. | briefs.md "Chief Dev decisions" item 2; checklist lines 25, 40 to 43; briefs.md Brief 4 OUT | Restate the gap as a conflict. The checklist gives the auth screens a UI owner, but Brief 4 excludes them and the Story files have no UI acceptance criteria (that second part is A11). Ask the CPM to make the Story files and Brief 4 agree. Re-rate the severity based on that. |
| Q3 | UX1-X02 | **Stale.** Chief Dev decision 5 puts the audit read API in the Step 8 PR. Step 8 is typed Backend-only in the checklist, so the audit viewer screen still has no UI lane. | briefs.md decision 5; checklist line 47 | Say the API owner is known and the viewer UI is unowned. Owning team: CPM, to add the viewer to a UI-typed Story. |
| Q4 | UX1-B04, B06, B08, B01, X08 | **Decisions already on file aren't reflected.** No price sort for negotiations (decision 4). The limit is clamped at 200, the deleted toggle is per list, and Playwright is optional (smaller defaults). UpdatedAt is added in the Step 7 PR (decision 4). | briefs.md "Chief Dev decisions" items 4 and smaller defaults | B04: close it or turn it into a UI criterion (no value or price control on the negotiations table). B06: drop "reject vs clamp is open"; the UI shows the clamped page size. B08: the toggle is on all four lists. B01: keep the column and default-sort gap, but say UpdatedAt is coming. X08: cite "Playwright optional" as the decision being challenged, and keep the owning team as QA plus DevOps. Each row should also note that a Chief Dev SD decision is not a Spec lock until Spec cites it. |
| Q5 | UX1-A03 | **The row contradicts its own Resolutions log.** The acceptance criterion still says the UI shows a message "naming 30-minute wait". The 7:56pm entry says the UI shows only what the server returns and never hard-codes a duration. | Findings lines 36, 175 | Rewrite the criterion now: the locked message renders the server-returned text, and its duration wording depends on the Security decision in Active holds. It should not wait for the Security answer to stop contradicting itself. |
| Q6 | UX1-A13 and its notes | **Stale state.** The Resolutions log marks A13 resolved at 7:57pm, but the row, the tally (minor, open) and the Senior notes ("pending Arch QA") still treat it as open. | Findings lines 46, 82, 155, 176 | Mark A13 resolved in the row with the Arch QA path, update the note, and either keep it in the tally as resolved or update the counts. |
| Q7 | Plan Outputs | **Missing: verdict per source.** The plan requires approve or redlines for each of A1, A2, A3, A5, A6 and A7. The file gives one overall "Redlines". | Plan line 38 | Add a short table with one row per source, each with a verdict and the row IDs behind it. |
| Q8 | Sources reviewed | **The PM checklist is missing.** It is the first source in the plan and was edited at 7:55pm and 7:58pm (sha256 now `d66613d4…caf6c`). The Story types and owners in it change Q2 and Q3. | Plan line 6; checklist mtime | Add the checklist with its current hash and re-check A14, A11 and X02 against it. |
| Q9 | New gap, not in the file | **UI-bearing Stories are typed Backend-only, so they skip the UI gate.** Step 11 (Backend-only) carries TD-ADM-131, which is an E2E UI smoke for the unauthenticated redirect. Step 8 (Backend-only) carries TD-ADM-101, whose step 2 is the owner reading the audit log in admin. | Checklist lines 47, 50; A6 TD-ADM-131 layer line 804 | Add a row with the CPM as owning team: re-type Steps 8 and 11 as UI, or move their UI parts to a UI-typed Story so the UI/UX gate applies. Severity: major. |
| Q10 | UX1-A05 | **Minor citation.** "A7 step-03 line ~29" is the UX1 hold line. The digits and period text is on line 32. | step-03.md lines 29, 32 | Fix the line reference. |

## Not bounced (checked, fine as written)
A01, A02, A04, A06 to A12, A15 to A21, B02, B05, B07, B09 to B15, C01 to C08, X01, X03 to X07. I spot-checked their citations against the pinned files, and every criterion can be tested as written.

### Holds at that time (superseded by the Active holds list at the end)
- UX1 isn't passed. The Chief UI/UX approval can't close the UI gate until this file says PASS.
- UI build lanes stay on hold. Backend-only work continues.

## Next step
Senior UI/UX fixes Q1 to Q10 in the same file and posts the new sha256. I re-verify just those rows plus the tally.

---

## Re-verification, 2026-10-05 about 8:01pm ET

| Field | Value |
|---|---|
| Artifact | Findings file sha256 `f32ae5adb73d…` (the mirror is identical) |
| Verdict | **PASS on the findings file.** It is an accurate, complete redline set against the plan. This is not approval of the admin requirements. Those stay redlined (see the findings file's "Verdict per source"), and the Chief UI/UX decides next. |

| Fix | Result | Evidence |
|---|---|---|
| Q1 B03 | Closed | Now cites Spec lines 136–137 and 146, SA line 191, Brief 1 line 80 and decision 4. |
| Q2 A14 | Closed | Restated as a conflict and re-rated major. The ownership part is now met: checklist `018a3519` row 14 and `/workspace/a7-stories/step-14.md` (8:00pm) exist, and Brief 4 OUT and the Common OUT list no longer exclude the auth pages UI. A11 (UI criteria in the Story files) is still open. |
| Q3 X02 | Closed | The viewer is now owned by checklist row 15 and `/workspace/a7-stories/step-15.md`. The decision on whether audit search applies is still open with Spec. |
| Q4 B01 B04 B06 B08 X08 | Closed | Each row cites its SD decision and says it is not a Spec lock yet. |
| Q5 A03 | Closed | The criterion renders server-returned text with no hard-coded duration. |
| Q6 A13 | Closed | The row is marked resolved with the Arch QA path. The tally counts it as resolved. |
| Q7 per-source verdict | Closed | There is now a table for A1, A2, A3, A5, A6, A7 and the checklist. |
| Q8 checklist source | Closed | Added. Its hash has moved again since then (`85606e89` to `018a3519`), so cite the newer hash on the next edit. |
| Q9 X09 | Closed | Checklist rows 8 and 11 now say "UI (waits for UX1 approval)", and step-08.md and step-11.md list the UI/UX gate first. |
| Q10 A05 line | Closed | Now points to step-03 line 32. |
| Tally | PASS | 53 rows: 15 blocker, 34 major, 4 minor (one of them A13, resolved). I recounted these myself. |

### Residual for the CPM (not a bounce of Senior UI/UX)
- In checklist `018a3519`, the "Gates (in order)" column for rows 8 and 11 still lists only Dev Code QA, then Security, then the BM merge. The column is empty for rows 14 and 15. The Story files put Chief UI/UX approval plus a UI/UX QA PASS file first, so the checklist column should say the same.

### Holds at that time (superseded by the Active holds list at the end)
- UI build lanes stay on hold until the Chief UI/UX approves UX1. The redlines in the findings file go to their owning teams through the CPM. Backend-only work continues.

---

## Re-verification 2, 2026-10-05 about 8:14pm ET (findings sha256 `0a50e61d`)

| Field | Value |
|---|---|
| Scope | The rows the Chief named (A02, A03, A04, A18, B03, B08), plus the tally |
| Verdict | **BOUNCE on A02 and A03 only.** The other four rows pass. The tally is correct at 53 rows: 15 blocker, 34 major, 4 minor (one resolved). |
| Cause | Chief Security revised the decision file to r2 at about 8:12pm ET, after the 8:08pm sync (`/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md`, "revised ~8:12pm ET (r2)"). |

| # | Row | Problem | Evidence | Required fix |
|---|---|---|---|---|
| R1 | A02 | The AC requires an identical status code for a throttled IP. Under r2, account-level failures return 401 with one shared body, but the IP throttle may return **429** with the same generic body. | Decision, "Server rules", bullets 1 and 2 | The AC should require identical status and body across the account-level cases (unknown email, wrong password, wrong TOTP, locked account). For a throttled IP it should require the same body, with a 429 allowed. There must be no `Retry-After` header. |
| R2 | A03 | The copy is out of date. The row still suggests "…try again later." and says the ending is "being replaced". The r2 accepted copy is "We couldn't sign you in. Check your details and try again later. You can also reset your password.", with the reset text linking to the email-link reset. | Decision, "Copy (r2)" | Use the r2 text and add the reset link to the AC. The AC for "same text during an IP throttle" can stay, but it shouldn't claim the status code is identical. |
| R3 | A03 and the unlock path | Spec and Security now conflict. Spec §8.6 says the lock holds "or until successful unlock via completed password-reset flow". r2 says a completed reset **does not clear an active lock early**. The A03 criterion says the unlock path is "wait, or a completed reset". | Spec line 290; Decision, "Copy (r2)", lock rules for reset | Mark A03's unlock path as waiting on a Spec decision, and record the conflict for the CPM to route to Chief Spec and Chief Security. The UI can't promise that a reset unlocks the account until one rule wins. |

Passed as written: A04 (it uses the r2 Turnstile copy, and a Turnstile failure doesn't count toward lockout), A18 (it uses the r2 reset reply and includes a locked account), B03 (it matches the status-list note exactly: three negotiation values, six offer values, the "Superseded (countered)" label, and Deleted as a badge rather than a status; it correctly stays a blocker until Spec QA), and B08 (it matches the note's "Soft-deleted rows").

Minor, not blocking: the line "Top blockers 5" still says "Spec and SA disagree on negotiation statuses". It could say "the status list is settled in draft and waits for Spec QA".

Noted for the CPM, outside UX1 rows: the r2 decision says "Superadmins can see lock status in the admin users view". Admin.core has no users view (Spec §12 OUT has no human-user list, and the X01 inventory has no such screen). Security should restate where the lock status is visible, for example the audit log viewer from step-15.

Addendum Part A (`59cf5cb4`) is not verified yet. Heads-up: UXR-A01, UXR-A02 and the A.4 decisions row (line 165) carry the same r1 wording as R1 and R2. Fix those before you send it for QA.

### Holds at that time (superseded by the Active holds list at the end)
- UX1 is not approved, and UI build lanes stay on hold. Backend-only work continues.

---

## Re-verification 3, 2026-10-05 about 8:16pm ET (findings sha256 `d8c6778c`)

| Field | Value |
|---|---|
| Verdict | **PASS** on the redline set. R1–R3 from re-verification 2 are closed. |
| Mirror | The uiux-out copy is byte-identical. |
| Tally | 53 rows: 15 blocker, 34 major, 4 minor (A13 resolved). The tally is unchanged and matches the rows. |

- R1 (A02): closed. The AC now requires identical copy and a 401 for unknown email, wrong password, wrong TOTP and a locked account. A throttled IP gets a 429 with the same copy, and no response has `Retry-After`. This matches Security r2's "Server rules".
- R2 (A03): closed. The r2 final copy is used verbatim, with the reset link going to the email-link reset. The AC also says that no screen promises a reset ends a lock.
- R3 (A03): closed. The row records the reset-unlock conflict with Spec §8.6 and the open question of where lock status shows. Both were routed by the CPM to Chief Spec and Chief Security.
- PM checklist `5f368bf5`: rows 8, 11, 14 and 15 are typed UI, and their gates column matches the Story files (UX1 approval before code, then Dev Code QA, Security, Chief UI/UX approval plus a UI/UX QA PASS on the built UI, then merge). The residual from the 8:01pm PASS is closed.
- Minor residual, not blocking: line 184 ("Top blockers 5") still says "Spec and SA disagree on negotiation statuses". The status list is now settled in the draft note and waits for Spec QA. Fix it in the next revision.

This PASS covers the redline set only. The requirements stay redlined for every source until the owning teams fix them. The Chief can now route each source's redlines through the CPM.

Addendum Part A (`61984745`): the three spot-fixes are confirmed (UXR-A01, UXR-A02 and the A.4 rows at lines 165–166; S-A10 and UXR-A37 no longer promise that a reset ends a lock). This is **not** a full verification of Part A. A full pass happens when Senior sends Parts A–D for QA.

### Holds at that time (superseded by the Active holds list at the end)
- UX1 is not approved, and UI build lanes stay on hold. Backend-only work continues.
- B03 stays a blocker until Spec QA passes the status-list note.
- The reset-unlock rule and where lock status shows are open with Chief Spec and Chief Security.
- The password rules (UX1-A17) are open with Chief Security.

---

## UX1-B03 cross-check of the status-list note, 2026-10-05 about 8:18pm ET

| Field | Value |
|---|---|
| Note | `specs/2026-10-05__spec__spec__core-admin-status-list-note.md`, sha256 `056bcf0a…` (matches) |
| Spec QA evidence | `verification/2026-10-05__spec__verification__core-admin-status-list-note.md` |
| UX content verdict | **PASS.** Negotiations have 3 values and offers have 6. The "Superseded (countered)" label is set. Withdrawn applies to offers only. Deleted is a text badge beside the status, is not a status, and is not in the filter. Each list's filter offers exactly its own values. Badges are text, never color alone, which meets WCAG 1.4.1. Findings B03 and B08 match the note word for word. |
| B03 status | **Still open, as paperwork only.** |

Before B03 can close:
1. The Spec QA file contradicts itself. The header says PASS because the Chief Architect agreed at 8:15pm. But the Verdict section still says "PASS waits for Chief Architect confirm", item 10 says NARROW / WAIT, and its Active holds list still names the Chief Architect confirm. Spec QA needs to update the body, item 10 and the Active holds to match the header.
2. The note's line 26 still says "Chief Architect confirmation is still pending." Chief Spec needs to record the 8:15pm agreement in the note.
3. SA §3.9 still lists Accepted, Declined and Withdrawn for negotiations. The Chief Architect needs to amend or annotate it. This is already in the SA row of the routing file.

After those three are done, B03 is resolved by citation. Then the Spec, SA, A7 Brief 1 and test-design owners cite the note.

---

## Spot-checks, 2026-10-05 about 8:19pm ET

- **Routing r2** (`ux/2026-10-05__ux__redlines__ux1-routing.md`, sha256 `fde55fba`, mirror matches): **PASS.** The six owner gaps I raised at 8:14pm are closed: SA on B01 and X01, Spec on A05, Product on A10, and new rows for Security (A16, A17), DevOps (X08) and UI/UX (X04). Every open row in findings `168db839` now reaches every owner named in its Owner column.
- **Findings `168db839`** (mirror matches): A17's helper text matches word for word `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` line 29. The Top blockers 5 note is fixed. The tally is still 15 blockers, 34 majors and 4 minors. These are spot-checks, not a new full review.
- **Spec QA evidence for the status-list note**: Spec QA reports it is now consistent (sha256 `865e62f1…`). B03 still waits on two things: Chief Spec updating line 26 of the note, and the SA §3.9 amendment.
- **Addendum Part A `5183af82`**: not verified yet. It gets the full review when Parts A–D arrive.

## Re-verification 4: addendum Parts A–D and sign-in Spec set (written 2026-10-05 ~8:58pm ET, cleared by Ivan via Bot Manager 8:57pm ET)

Verdict: **PASS** on addendum `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` sha256 `599d6a18` (mirror in uiux-out matches) and findings `ux/2026-10-05__ux__findings__ux1-admin-requirements.md` sha256 `8efb1d30` (53 rows: 15 blockers, 34 majors, 4 minors).

History: I bounced `dd847e70` on 4 record fixes (Status line, Built from, a future time on the Written line, the step-15 hold). I bounced `a1f4b8a1` on 3 more (UXR-A22 length and messages, the step-15 hold, "QAQA" in UXR-D14). All are fixed in `599d6a18`.

| Item | Source checked on disk | Result |
|---|---|---|
| UX1-A03 lockout copy and S-A12 panel | lockout note v5.5 `8a194eb9` | PASS. The 401 is identical and the 429 has the same body and no Retry-After. The reset reply, the 20-event panel including the second-factor event, the 24-hour notice and the SHOULD email all match. Wording nit for Spec: on line 142, "out of scope" should say "out of this note". |
| UX1-A17 and UXR-A21–A23 | password rules v2.3 `1a7b342c` | PASS. Length is 15 to 128. It's checked on blur and on submit, the four messages are verbatim, paste and new-password are allowed, a reject keeps the link, and success ends every session. |
| UX1-A16 and UXR-A08, A15, A16, A18 | sign-in steps v1 `0a3db5f4`, Security decision `bb0bcaa2` | PASS. There are two steps and both use the generic text. The cap is 5 codes, the token lasts 5 minutes, and the session starts only after step 2. Record nits for Spec: line 8 still says draft, and the recovery-code view has no focus order. |
| UX1-B03 | status-list note `0a5ff57b` (line 26 Chief Architect agreement), SA §3.9 amend with Arch QA PASS | **CLOSE** |
| UXR-B03, B14 scope | Product decision `6ea05ddb` with Product QA PASS | PASS. Withdrawn is maker-only and shows an empty state, and the Expire dialog follows Part C. |

Not yet cross-checked by UI/UX QA (each has its own team's QA PASS): Product scope note `d8a2acd5` (X05, A07, C01, A10), Dev Plan `3385872a` (A11, A14), SA redlines note `0286c68e` content, Spec UX1 redlines note v2 `eb63276f` (A05, A15, A20, B10, B01, X01), test design `8df39255`.

## Cross-checks of owner fix files (written 2026-10-05 ~9:08pm ET, each hash checked on disk)

| File | sha256 | UX1 rows | Result |
|---|---|---|---|
| Product scope note `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | `d8a2acd5` | X05, A07, C01, A10 | **PASS**. The cites match UXR-D06–D09, A33–A37, C01 and A04/A05/A07/A09. The addendum is now passed, so the note's "cite once UI/UX QA passes" condition is met. |
| SA redlines note `architecture/2026-10-05__sa__architecture__core-admin-ux1-redlines-note.md` | `0286c68e` | A05, A15, A20, B10 | **PASS**. The §4 editable fields, limits and status changes match UXR-B14 one for one. |
| Spec UX1 redlines note v2 `specs/2026-10-05__spec__spec__core-admin-ux1-redlines-note.md` | `eb63276f` | A05, A15, A20, B10, B01, X01 | **PASS**. It has the idle and absolute warnings 2 minutes ahead, list state restored after re-auth, Back after sign-out showing no data (no-store), FieldPolicy Write deciding which fields are inputs, and the S-B1, S-C1, Toggle and S-D2 rows. Follow-up for Senior UI/UX, not blocking: add an absolute-expiry warning row (single "OK", no extend), because UXR-A38 covers idle only. Also add the Created fallback sort and the Deleted badge position beside the status. |
| Dev Plan `plans/2026-10-05__devplan__plan__core-admin-dashboard.md` | `41928cbe` | A11, A14, plus Steps 7 and 9 | **BOUNCE on 4 record fixes.** The content for A11 and A14 is fine: Steps 2–5 cite UXR and S-A IDs, and Steps 14 and 15 bind lockout v5.5, password rules v2.3 and sign-in steps v1. Fixes: (1) line 48 cites findings `168db839`, but the current version is `8efb1d30`. (2) Line 49 cites addendum `dd847e70`, but the passed version is `599d6a18`. (3) Line 160 says the password rules aren't locked in Spec yet, but v2.3 is binding. (4) The Step 9 UI cases don't cite UXR-B14 or B15, which cover the edit form, Save and Cancel, and the "Expire negotiation" dialog. |
| Test design `qa/2026-10-05__qa__test-design__core-admin-dashboard.md` | `8df39255` | A12, C08, X07, X08 and Part A | **PASS for Part A and C08.** All 44 UXR-A rows map to TD cases, and it covers the 5-code cap, axe-core and keyboard-only runs. The B, C and X cases are stubs. They stay open on Chief QA's list until after UX1 approval, and must then cover UXR-B14 Expire and B15. |

## Dev Plan re-check (2026-10-05 ~9:07pm ET)

The Dev Plan tip is `plans/2026-10-05__devplan__plan__core-admin-dashboard.md`, sha256 `b3788114` (the bounced version was `41928cbe`). **PASS.** All 4 record fixes were checked on disk:
1. Line 48 cites findings `8efb1d30`.
2. Line 49 cites addendum `599d6a18`.
3. Line 160 and the Sources list state password rules v2.3 `1a7b342c` as binding.
4. Step 9 (lines 366 and 372) cites UXR-B14, covering edit and the Expire dialog, and UXR-B15, covering Save and Cancel.

Neither old hash (`168db839`, `dd847e70`) appears anywhere in the plan. The A11 and A14 content is unchanged, and the plan uses none of the retired hold wording. This PASS is not a build unlock.

## Approval condition 2: addendum follow-up (2026-10-05 ~9:11pm ET)

Checked addendum `c578d618` and findings `07b80927`. The hashes match on disk and in uiux-out. Verdict: **BOUNCE on record fixes only. The content passes.**

Content checked against Spec UX1 redlines note v2 `eb63276f`:
- UXR-A45 matches A20 line 72: a warning 2 minutes before the 8-hour cap, a single "OK", no way to extend, and lost input stated. The QA case covers 7h58m and 8h. UXR-A38 now covers idle only.
- UXR-B01 matches lines 145–152: the columns are in order, the default sort falls back to Created until `UpdatedAt` ships in Step 7, the Deleted badge sits beside the status, and offers show amount with currency.
- UXR-A42 matches X01 S-A12 (line 173): the password change section is there, and success ends every session. The hold on whether Spec asks for the current password is valid and scoped to the S-A12 password PR.
- The `eb63276f` cites are present on A38, A42, A45, B01 and D01.

Record fixes for Senior UI/UX:
1. In the addendum's Active holds, bullet 1 still says the UI lanes stay on hold until UX1 is approved. Drop it, because the approval released them.
2. Bullet 2 says password rules v2.3 and lockout v5.5 still wait on the UI/UX QA cross-check, and that the S-A12 activity panel needs Spec to adopt it. These are done: re-verification 4 passed the two notes, and Spec X01 S-A12 adopts Recent sign-in activity. Drop it.
3. The findings resolutions log entry is timed "9:14pm ET", but it was on disk by 9:11pm. Use the real time.

### Condition 2 re-check (2026-10-05 ~9:13pm ET)

**PASS.** Addendum `7aa374e9` and findings `4b612834` match on disk and in uiux-out. All three record fixes are confirmed: the two outdated bullets are gone from the addendum's Active holds, and the findings log entry now reads 9:11pm. The content is unchanged from `c578d618`, which already passed above. Approval condition 2 is met.

## Active holds
UX1 was approved at 9:05pm ET (`ux/2026-10-05__ux__approval__ux1-admin-requirements.md`, sha256 `6247be65`). The UI lanes for rows 8, 11, 14 and 15 are released. Only the approval's merge conditions remain:
1. Dev Plan record fixes: met on `b3788114`. The docs PR still needs Docs QA before it merges.
2. Senior UI/UX's addendum follow-up: met on addendum `7aa374e9` (UI/UX QA PASS above). Spec still has to confirm whether S-A12 password change asks for the current password, and that holds only the S-A12 password PR.
3. Chief QA fills out the B, C and X test-case stubs, including UXR-B14 Expire and B15, before those screens' UI PRs merge.
4. Withdrawn (Step 7 PR) and the edit paths and Expire (Step 9 PR) each need their own Dev Code QA PASS.
