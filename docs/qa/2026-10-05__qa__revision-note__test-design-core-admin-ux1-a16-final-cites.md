# Revision note — Core admin test design r4 (UX1-A16 two-step sign-in + final Spec cites)

| Field | Value |
|-------|--------|
| Written by | Dealoware Chief QA |
| Date | 2026-10-05 (~8:40pm ET) |
| Status | **Revised r4 — ready for Test design QA (Senior Product QA).** Not a PASS stamp; no Spec QA / SPQA / QAQA verdict is claimed. |
| PoC | $0 |

## Paths and hashes

| Item | Path | sha256 |
|------|------|--------|
| Revised design (r4, edited in place) | `/workspace/dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md` | `8df392554ed887322bd7dc3f6633933bca26209c3bcfbb29321bf055f89814c9` |
| Prior tip r3 (archived) | `/workspace/qa/archive/2026-10-05__qa__test-design__core-admin-dashboard__31095cbb.md` | `31095cbbce1ccba70fcdd585a15611665a5bb956e74a4792fd5d35b23da399d7` |
| Prior harness index (r3) | `/workspace/qa/archive/harness-admin-dashboard__31095cbb/` | — |
| Prior revision note (r3) | `/workspace/qa/2026-10-05__qa__revision-note__test-design-core-admin-ux1-a12-c08-x07-x08.md` | — |

## Final Spec cites (re-hashed on disk at ~8:40pm ET; all match)

| Note | Path | sha256 | Verification on disk |
|------|------|--------|----------------------|
| Lockout window note **v5.5** (replaces v5.3 `67686ce5…`) | `/workspace/dealoware-kb/specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` | `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` | Spec QA PASS on this tip (“v5.5 PASS” entry, `verification/2026-10-05__spec__verification__core-admin-lockout-window-note.md`); Security QA point 5 PASS on this tip (`verification/2026-10-05__security__verification__core-admin-lockout-window-note-pt5-confirm.md`) |
| Password-rules note **v2.3** (replaces v2.2 `7f73db9b…`, v2.1 `1ce5e7f3…`) | `/workspace/dealoware-kb/specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` | `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` | Spec QA PASS (“v2.3 PASS” entry, `verification/2026-10-05__spec__verification__core-admin-password-rules-note.md`); Security QA PASS (`verification/2026-10-05__security__verification__core-admin-password-rules-note-confirm.md`) |
| Sign-in steps note **v1** (UX1-A16) | `/workspace/dealoware-kb/specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` | `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` | Spec QA PASS on this tip only (`verification/2026-10-05__spec__verification__core-admin-signin-steps-note.md`); Security QA PASS (`verification/2026-10-05__security__verification__core-admin-signin-steps-note-confirm.md`) |
| UX1-A16 Chief Security decision | `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` | `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34` | Binding; cited in full by the sign-in steps note |
| Chief Security auth-UI answers | `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` | `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` | Matches the cite in lockout v5.5 and password-rules v2.3 |

Two things to note: the header lines of the lockout and password-rules Spec QA files still show older verdicts, but each file ends with a PASS entry on the cited tip. The sign-in steps note header still reads "Draft", but both verifications pass on its tip, so it's cited as passed on disk.

## Counts

| | r3 | r4 |
|---|---|---|
| Test cases | 104 | **114** (+10) |
| P0 / P1 / P2 | 66 / 33 / 5 | **76 / 33 / 5** |
| Source IDs covered | 161/161 | **183/183** (+12 sign-in steps conditions, +10 password-rules items) |

## Added case IDs (10)

| Case | Priority | Covers |
|------|----------|--------|
| TD-ADM-023 | P0 | Pending-auth token: opaque ≥128 bits, no session, ≤5 min, single use, cleared on success / expiry / lock, host-only Secure / HttpOnly / SameSite=Strict, never in URL, step 2 only (conditions 1, 7, 8) |
| TD-ADM-024 | P0 | One shared account counter across step 1 and step 2 failures; step 2 failures count toward the login IP throttle; 429 on both steps (conditions 2, C1, C2) |
| TD-ADM-025 | P0 | At most 5 code attempts per token, then void and restart step 1 with a new Turnstile (condition 3) |
| TD-ADM-026 | P0 | Locked account: identical 401 at step 1 even with the correct password, no pending token, never step 2; hash timing (conditions 4, 5, C3) |
| TD-ADM-027 | P0 | Session only after step 2; session ID rotated; token invalidated; recovery code spent at once incl. race (condition 7) |
| TD-ADM-028 | P0 | Audit `signin.second_factor_failed` (bad 2FA, after-correct-password flag, HMAC IP only); S-A12 label 'code failed after correct password'; never echoed (conditions 6, C4) |
| TD-ADM-029 | P0 | Step endpoints only on admin.core (condition 8) |
| TD-ADM-UI-auth-21 | P0 | Two-step layout; focus order step 1 email → password → Turnstile → submit, step 2 code → 'Use a recovery code instead' → 'Back to sign in'; token absent from URL, storage and DOM |
| TD-ADM-UI-auth-22 | P0 | Same generic sentence on both steps; locked + correct password stays on step 1; 429 on both steps; no Retry-After / duration / count / 'locked' |
| TD-ADM-UI-auth-23 | P0 | Expired token or exhausted attempts → same sentence, back to step 1 with a new Turnstile |

## Changed case IDs

- **TD-ADM-UI-auth-08 / -10 / -13** now follow password-rules v2.3:
  - length 15–128 code points after NFKC, with the same count on client and server
  - the inline error names the failed rule
  - context-word v1.2 checks. These run on the rule function in unit tests, because the short examples would otherwise fail the length rule first. Required results: `radio station` passes; `io2026!!!` and `io$$$2026$$$` fail. Results must not depend on the process culture.
  - no outbound call during the check
  - a rule reject doesn't count toward lockout or throttle, and doesn't burn the link
  - on success, all sessions end and TOTP is unchanged
  - S-A12 password-change parts wait on the S-A12 Spec adopt
- **TD-ADM-UI-auth-01 / -04 / -07 / -11 / -19** pick up sign-in steps note and lockout v5.5 wording:
  - identical copy on both steps
  - Spec focus order
  - 'Back to sign in' (Spec) instead of the addendum's 'Start over'
  - the S-A12 closed event list now includes 'code failed after correct password'
- **TD-ADM-004, -011, -020, -050, -051** gained r4 notes (two-step meaning, hashing floor review, lockout cite v5.5). API expectations are unchanged.
- **Design-wide edits:**
  - §1 binding inputs re-cited
  - §3 rows extended; new §3.11 (sign-in steps conditions → cases) and §3.12 (password rules → cases)
  - §3.9 UXR map regenerated
  - new §4.3a
  - §6 OQs updated
  - per-case "Active hold" lines are now "Waits on:" lines that point at the **single Active holds list in §0**. The r3 holds table was folded into that list.

**Unchanged:** the UX1-A03 r2 rules (account failures get an identical 401; the IP throttle gets 429 with the generic body; no `Retry-After`; never "locked"; reset doesn't lift a lock) and Core API H4 (429 + Retry-After). No route strings were invented; the sign-in steps note names screen S-A01 only.

## Open questions

- **Closed:** OQ9 (two steps), OQ11 (password-rules v2.3 final). The Active hold that cited lockout v5.3 is gone; v5.5 is cited instead.
- **OQ2 partly closed:** 6 digits are locked. TOTP period and recovery-code count are still with SA.
- **OQ12 (new, awareness only):** the step-2 cap (5 per token) and the account budget (5 in 15 min) are equal. Under default config the 5th wrong code locks the account, so the cap never shows up on its own. TD-ADM-025 and UI-auth-23 prove the cap with a raised threshold set only in the local test host, and assert lock plus token clear under default config. No rule change proposed.
- **OQ13 (new):** the addendum's wording differs from the final Spec ('Start over' vs 'Back to sign in'; event names vs the lockout v5.5 closed list). Tests follow the Spec. This goes to Chief UI/UX.
- **OQ8 still open:** there is no dedicated case for the lock-notice email (a SHOULD). The email should also cover a correct password followed by repeated code failures.

## Active holds (single list, same as design §0)

1. Test design QA (Senior Product QA) has to re-verify this tip; Stories and build stay held until then.
2. UI build lanes stay held until Chief UI/UX approves UX1, and addendum Part A is still Draft. This covers all `TD-ADM-UI-*`.
3. Addendum Parts B–D aren't written yet, so `TD-ADM-UI-na-*` stay SKIP stubs.
4. Delete UI decisions and copy (C01–C07) wait on the Spec lock and addendum Part C.
5. Spec still has to adopt the S-A12 sections and the idle warning (UX1-A21 / A20). The S-A12 password-change parts of auth-10 and auth-13 wait on this too.
6. Spec hasn't adopted route names for the S-A inventory.
7. SA hasn't locked the TOTP period or recovery-code count (OQ2).
8. X08 CI: DevOps owns the pipeline wiring and QA owns the case catalog. Brief 4 makes Playwright optional, but this design requires headless UI cases in CI (OQ10).
9. UI/UX QA cross-checks of the three Spec notes follow their Spec PASS; they don't block these cites. The addendum drift in OQ13 goes to Chief UI/UX.
10. Deploy stays held until Ivan OKs it. admin.core deploy and attach also wait on A11 and the PR #11 conditions. No live admin.core calls.
11. No password values, keys, AWS account IDs or SES identities anywhere.

## A8 harness (`/workspace/qa/harness/admin-dashboard/`)

- New SKIP stubs:
  - `cases/p0/td-adm-023…029__*.md` (group `signin-steps`)
  - `cases/p0/td-adm-ui-auth-21…23__*.md`
- All UI stubs were regenerated with r4 traces and a "Waits on" section.
- `cases.json`: 114 cases (P0 76 / P1 33 / P2 5), and every entry now has `stub`.
- `CASE_INDEX.md` was rebuilt (114 rows plus an r4 section). `README.md` was updated.
- `scripts/run-stubs.sh` → **114 skipped, 0 run, 0 live calls**.
