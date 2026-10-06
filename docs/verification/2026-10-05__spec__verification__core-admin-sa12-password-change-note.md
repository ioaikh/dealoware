# Spec QA verification: Core admin S-A12 password-change note v1.1

| Field | Value |
|-------|-------|
| Written by | Dealoware Spec QA |
| Date | 2026-10-05 (~9:35pm ET) |
| Note | `specs/2026-10-05__spec__spec__core-admin-sa12-password-change-note.md` **v1.1** (147 lines) |
| Verdict | **PASS waiting on Security QA** on tip `47491c1a0efd7390469258ae7129d73ed744caec49681f526cb93e65b10d8b79`. Prior tips `3330a478…` and `f4848447…` are **VOID**. |
| Scope | Paper gate only. This is not a build, deploy or attach unlock. PoC $0. |
| Baseline copy | `/workspace/spec-qa-baselines/sa12-password-change-note_v1.1_47491c1a.md` (same sha256) |

## Hashes (re-hashed on disk before writing)

| File | Expected | On disk | Result |
|------|----------|---------|--------|
| S-A12 password-change Spec note v1.1 | `47491c1a…` | `47491c1a0efd7390469258ae7129d73ed744caec49681f526cb93e65b10d8b79` | **MATCH** (147 lines). `3330a478…` VOID. `f4848447…` VOID. |
| Security answers (binding item 5) | `7504914d…` | `7504914d0ffc3a49a83669c8093bc2aac966d3684c3f95e4a7c59a5df1ad9b6c` | MATCH (cited lines 7, 19–20, 134, 146) |
| Main Spec v2.2 | `25f2647…` | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` | MATCH, file not edited |
| Password-rules note v2.3 | `1a7b342c…` | `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` | MATCH, cited; file not edited |
| Lockout note v5.5 | `8a194eb9…` | `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` | MATCH, cited; file not edited |
| Sign-in steps note v1 | `0a3db5f4…` | `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` | MATCH, cited; file not edited |
| UX1 redlines note v2 | `eb63276f…` | `eb63276fc51217c3c82772c0b8de26ead1337f6e382938a1d736a05d13f6f2d0` | MATCH, cited; file not edited |
| Security QA confirm on this Spec tip | `47491c1a…` | *(none under `verification/*sa12*` / `*password-change*`)* | **MISSING** — handshake open |

## Checks

| # | Check | Result | Evidence |
|---|-------|--------|----------|
| 1 | Item 5 rules carried testably (MUST) | **PASS** (with OBS-1 on 5.6 wording) | POST + anti-forgery + no-store (line 46); audits `account.password_change_failed` / `account.password_changed` (lines 62, 98); TOTP last-accepted-step replay (line 86); recovery burned only on whole-submit success (line 87); new ≠ current after same normalisation as password-rules / context-word v1.2 (lines 95–96); no Turnstile (line 47); success ends **every** session including this one (line 98) |
| 2a | Session-end vs UX1 redlines A05 | **PASS** | Actions are separate. A05 re-enrol/regenerate: end **all other** sessions (ux1-redlines line 52) — source-supported. Password-change success: end **every** session including this one (this note line 98; password-rules v2.3 UI table "all existing sessions"; UXR-A42) — source-supported. Note lines 106 and 115 keep A05 untouched. |
| 2b | Step-up wording | **PASS** | Password change: current password + TOTP or recovery **together on submit** (line 45; answers item 5 #1). Re-enrol/regenerate keep A05 **within 5 minutes** window. Different shapes; both cited. |
| 2c | Failures / lock vs lockout v5.5 | **PASS** | One generic step-up failure body (lines 56–58); Security (a) shared counter + audit (lines 60–62); Security (b) lock ends every session → S-A1 with UX1-A03, no duration, no word "locked" (lines 64–68). Lockout tip `8a194eb9…` cited; IP-throttle **MUST 429** / no Retry-After live in lockout v5.5 (not restated here — nit). |
| 2d | New password vs password-rules v2.3 | **PASS** | Lines 91–99 bind tip `1a7b342c…` and item 1; rule reject does not count (rule 9); four rule-reject messages only. |
| 3 | Chief UI/UX (a)–(c), 9:30pm ET | **PASS** | Lines 72–80. (a) lock → S-A1 + A03, no duration / no "locked". (b) clear Current + Code, keep New + Confirm, focus Current, polite live region + `aria-describedby`, single in-flight submit. (c) show/hide on every password field; confirm mismatch on blur and submit. |
| 3b | Confirm-mismatch cite | **PASS** | Claim true: password-rules v2.3 line 62 lists only the four rule-reject strings (no mismatch). UXR-A23 supplies `These passwords don't match.` (addendum). Note lines 7, 21, 27, 80 correct. |
| 4 | Hosts / secrets / PoC / deploy | **PASS** | Host `admin.core.dealoware.com` only (line 10). No password values, secrets, or AWS IDs. PoC $0. Deploy only as hold. |
| 5 | Wording | **PASS** | No "Soft HOLD". Exactly one `## Active holds` (line 120). No word/phrase repeated more than 5 times in a row. |
| 6 | Answers drift `a87e293b…` → `7504914d…` | **Report only (non-blocking)** | See below. Does not block this note. |
| 7 | Security handshake on `47491c1a…` | **OPEN** | No Security QA PASS file for this tip. Full Spec PASS waits on Security QA confirm of `47491c1a…`. |
| 8 | Chief Spec decision points 1–4 | **NOT FOUND** | Searched briefs/specs/ops/KB/`/workspace` (incl. `decision point`, `points 1–4`, L6b). Only L6b summary in `pm/admin-dashboard-checklist.md` ("decided yes… current password plus a TOTP or recovery code"). **Do not mark points 1–4 as met.** Note line 74 cites them; Spec QA could not verify that layer. |

## OBS (non-blocking)

1. **Answers item 5 #6 vs this note line 98.** On-disk answers tip `7504914d…` point 6 says success ends "**all other** sessions, as v2.3 already says". v2.3 and UXR-A42 say **all existing** / every session **including this one**. This note locks the v2.3 / UXR-A42 reading (line 98). Security QA already **BOUNCE**d answers tip `7504914d…` for that GAP (`verification/2026-10-05__security__verification__admin-core-auth-ui-security-answers-item5-7504914d-confirm.md`). Spec QA does not invent a fix to answers. If Security amends 5.6 to "all existing" / "every including this one", this note needs no content edit for that line. If Security keeps "other", Spec must bounce and realign.
2. **No Turnstile** (line 47) is carried and matches the parent check list; item 5 text does not itself say "no Turnstile". Acceptable as Spec lock for a signed-in settings form; Security QA may confirm.
3. **Chief Spec points 1–4** cited at line 74 were not found as a numbered source. Content of (a)–(c) and item 5 still scored above.

## Answers drift (`a87e293b…` → `7504914d…`)

Password-rules v2.3 and lockout v5.5 still cite answers `a87e293b…`. On-disk tip is `7504914d…`.

Proof (same method as Security QA item-5 confirm): deleting the item 5 block (lines 60–70 inclusive through the blank before `## Active holds`) from tip `7504914d…` and re-hashing yields exactly `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15`.

**Finding:** The delta is **item 5 only** (S-A12 password-change step-up). Items 1–4 and Active holds are byte-identical to `a87e293b…`. Nothing those passed notes rely on (item 1 password rules; items 3–4 reset/lock / S-A12 activity panel) changed in substance. Cite-identity drift only for siblings; **non-blocking** for this note. Queued next-edit for siblings: re-cite `7504914d…` (or successor) when convenient.

## Security handshake

- **No** Security QA PASS file names tip `47491c1a…` (searched `verification/*sa12*`, `*password-change*`).
- Answers tip `7504914d…` itself is Security QA **BOUNCE** (GAP-1 on session-end wording), not a PASS.
- Spec QA content verdict on this note tip is ready; **full PASS waits on Security QA confirm of `47491c1a…`**.

## Active holds

- Build and deploy stay held. This is a paper gate, not a build unlock.
- No admin.core deploy or attach until A11, the PR #11 conditions, and Ivan's OK via Bot Manager.
- Full Spec PASS waits on Security QA confirm of tip `47491c1a…`.
- UI/UX QA cross-checks after Spec QA (including Chief UI/UX (a)–(c)).
- Main Spec `25f2647…` and passed siblings (`1a7b342c…`, `8a194eb9…`, `0a3db5f4…`, `eb63276f…`) stay untouched.
- Answers tip `7504914d…` Security GAP-1 (5.6 "other" vs v2.3 "all existing") is owned by Security; watch for a successor answers tip.
- PoC $0.

## Nits (queued; non-blocking)

- Line 74 cites "Chief Spec’s points 1–4" with no findable numbered source in the tree.
- Lockout v5.5 IP-throttle **MUST 429** / no `Retry-After` is not restated on this form (lives in lockout note).
- Sibling password-rules / lockout still cite answers `a87e293b…` (cite-identity only; see drift).
- Line 47 "No Turnstile" is not verbatim in answers item 5.

**PASS waiting on Security QA** on tip `47491c1a0efd7390469258ae7129d73ed744caec49681f526cb93e65b10d8b79`. Prior `3330a478…` and `f4848447…` VOID. Not a build unlock. PoC **$0**.

## v1.2 pre-check (2026-10-05 ~9:37pm ET)

Tip: `4817afc24d5f7016be8d888cca2a8d83e0b3c1d82d9fe8f6f057ae23a5336684`. `47491c1a…` is void.

Sources, re-hashed:
- Answers: `6f4db2d6…`.
- Chief Spec decision: `6721779a…`. Points 1–4 match the note: the four-field form, the generic failure message, (a) and (b), v2.3 rules with every session ending, and no Turnstile.

I diffed v1.2 against the v1.1 copy. Besides the cite changes, there are three content changes, and I checked each against answers item 5:
1. Only step-up failures count toward the shared counter. This matches items 5.2 and 5.6.
2. Rule rejects and confirm mismatches don't count. Rule rejects match 5.6. Mismatches aren't step-up failures under 5.2, so not counting them is a consistent reading; I've flagged it to Security QA for confirmation.
3. The recovery code is burned atomically with the password update. This matches 5.5.

Answers 5.6 now says every session ends, which matches the note, so the earlier gap is closed. Chief Spec accepted both nits as non-blocking.

**Content OK. The PASS waits on Security QA confirming `4817afc2…`.**

## v1.3 check (2026-10-05 ~9:43pm ET): BOUNCE (one cite fix)

Tip: `6fc85dc63f758ee39f4a6d9551876bed2d19dff487aa2396c3e28d0beadad510`. `4817afc2…` is void. The main Spec is still `25f2647…`. I diffed v1.3 against the v1.2 copy: it adds lines 89 and 119, plus header changes. Chief Spec accepted the mismatch reading.

- **Line 89 (TOTP step in the same transaction): content OK.** Security QA's 47491c1a confirm file (OBS item 1, lines 77–81) supports it. Answers `6f4db2d6…` 5.5 names only the recovery-code burn. Non-blocking: cite the Security QA file by path.
- **Line 119 (password-change notice email deferred out of v1, "answers tip `6f4db2d6…`"): BOUNCE.** I found no deferral in answers `6f4db2d6…` or in any file under `/workspace/security-out/`. Answers item 4 has only the lock-notice email, which is a SHOULD. Fix: cite the on-disk Chief Security source for the deferral, or remove the attribution and list it plainly as out of this note's scope.
- **Security handshake:** there is no Security QA PASS file for `4817afc2…` or `6fc85dc6…`. The PASS waits on Security QA confirming the next tip.

Active holds:
- The Step 14 password-change PR waits on this note passing.
- The build stays held. No deploy. PoC $0.

## v1.4 pre-check (2026-10-05 ~9:45pm ET)

Tip: `df797fefa331bce02cdb28e3686cefcc1cc91ea6d24529d8937ee703a2eb9feb`. `6fc85dc6…` is void. I diffed v1.4 against the v1.3 copy:
- Line 89 now cites the Security QA 47491c1a confirm file, OBS item 1.
- Line 119 now reads "(not part of v1)", with no attribution.
- Everything else is header changes only.

The v1.3 bounce is fixed. **Content OK. The PASS waits on Security QA confirming `df797fef…`.**

## v1.4 PASS (2026-10-05 ~9:46pm ET)

Security QA passed `df797fef…` (`verification/2026-10-05__security__verification__core-admin-sa12-password-change-note-df797fef-confirm.md`, sha256 `f0d747e4…`). Security QA also confirmed that a confirm mismatch doesn't count toward the shared counter. I re-hashed the note just before writing this, and it is still `df797fef…`. The main Spec is still `25f2647…`.

**Verdict: PASS (Spec gate) on S-A12 password-change note v1.4 `df797fefa331bce02cdb28e3686cefcc1cc91ea6d24529d8937ee703a2eb9feb`.** `6fc85dc6…`, `4817afc2…`, `47491c1a…`, `f4848447…` and `3330a478…` are void. The note goes to UI/UX QA for its cross-check.

Active holds:
- UI/UX QA cross-checks v1.4.
- The Step 14 password-change PR follows its own plan and QA gates. This PASS doesn't unlock the build.
- No deploy. PoC $0.
