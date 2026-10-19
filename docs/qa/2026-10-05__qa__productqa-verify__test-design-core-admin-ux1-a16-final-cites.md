# Product QA — Test design re-verify (Core admin dashboard r4 / UX1-A16 + final Spec cites)

| Field | Value |
|-------|--------|
| Role | Senior Product QA (Test design QA) — independent of Chief QA author |
| Date | 2026-10-05 (~8:40pm ET window) |
| Scope | Design tip vs UX1-A16 / A12 / C08 / X07 / X08 + final Spec cites; not Spec invent, not Stories/code, not merge/deploy |
| Design | `/workspace/dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md` |
| Design sha256 | `8df392554ed887322bd7dc3f6633933bca26209c3bcfbb29321bf055f89814c9` — **MATCH** expected |
| Revision note | `/workspace/qa/2026-10-05__qa__revision-note__test-design-core-admin-ux1-a16-final-cites.md` |
| Revision note sha256 | `6801699c837ac358434b49dc0b0c01af7efdc5edd59c07546622aca406ca1295` — **MATCH** expected |
| Prior PASS (r2) | `/workspace/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-ux1-a03-r2.md` (design tip `110e075f…`) |
| Overall | **PASS** |
| PoC | $0 |

## Cited tips (item 2)

| Note | Path | On-disk sha256 | Prefix | Spec QA | Security QA | Status field on note |
|------|------|----------------|--------|---------|-------------|----------------------|
| Lockout **v5.5** | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` | `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` | `8a194eb9` MATCH | **PASS** entry “v5.5 PASS” ~8:30pm ET | point 5 **PASS** on `8a194eb9…` | Header still says Draft (cosmetic lag) |
| Password-rules **v2.3** | `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` | `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` | `1a7b342c` MATCH | **PASS** entry “v2.3 PASS” ~8:31pm ET | **PASS** on `1a7b342c…` | Header still says Draft (cosmetic lag) |
| Sign-in steps **v1** | `specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` | `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` | `0a3db5f4` MATCH | **PASS** on tip | **PASS** on tip | Header still says Draft; design + rev note already disclose |
| UX1-A16 decision | `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` | `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34` | `bb0bcaa2` MATCH | n/a (Security decision) | Binding | Not a Spec Draft |

**Final for cite purposes:** yes — dual Spec QA + Security QA PASS (or binding Security decision) on each tip. **OQ7 closed:** r3 closed the v5.3 re-cite hold; r4 replaces the cite with v5.5 `8a194eb9…` and drops the old Active hold (rev note: “The Active hold that cited lockout v5.3 is gone”).

## Verdict table

| # | Item | Verdict | Evidence |
|---|------|---------|----------|
| 1 | Both tip hashes match | **PASS** | Design `8df39255…` MATCH; revision note `6801699c…` MATCH |
| 2 | Cited specs exist; prefixes MATCH; final for cite; OQ7 closed | **PASS** | All four files present; full hashes above; Spec QA + Security QA PASS entries on lockout v5.5, password-rules v2.3, sign-in steps v1; A16 decision binding. OQ7 closed (r3) and superseded by v5.5 cite (r4). Info: Spec note Status headers still say Draft — same disclosure as rev note; not a content FAIL |
| 3 | 114 cases; 10 unique A16 IDs with traces + executable asserts | **PASS** | Coverage §3.7 / harness `cases.json` / combined headings+table stubs = **114**. A16 IDs unique: TD-ADM-023…029 + TD-ADM-UI-auth-21…23. Each traces to UX1-A16 + SIGNIN-1…8 / C1–C4 (§3.11). §4.3a / §4.16 cases have Steps + Expected (token properties, shared counter, 5-attempt cap, locked→401 no pending token, session after step 2, `signin.second_factor_failed`, host binding, UI two-step layout / generic copy / restart) |
| 4 | A12 / C08 / X07 / X08 present and match rev note + sources | **PASS** | §0a + §3.8: A12 → TD-ADM-UI-auth-01…20 (+21…23 in r4); C08 → TD-ADM-UI-del-01…06; X07 → TD-ADM-UI-na-B*/X* + axe + kbd stubs; X08 → TD-ADM-UI-ci-01 (CI fails on broken UI). Matches revision note and UX1 routing cite |
| 5 | No regression vs r2 PASS (050 / 051 / OQ1 / OQ3 / OQ5 / H4) | **PASS** | TD-ADM-050 Expected unchanged in meaning: identical **401** + r2 body; no Retry-After / time / count / “locked”; reset does not lift lock or clear counts; TOTP still required. TD-ADM-051: **429** + generic body; **no Retry-After**; throttled reset → exists reply + **no email**. §0 Unchanged (locked) + §0b + §6: OQ1 / OQ3 / OQ5 still **Closed**. Core API H4 still 429 + Retry-After, separate. r4 only adds notes pointing at two-step siblings and re-cites lockout v5.5 |
| 6 | Password-rule cases ↔ v2.3; lockout cases ↔ v5.5 | **PASS** | TD-ADM-UI-auth-10 asserts length 15–128 after NFKC (same client/server), named inline errors, context-word v1.2 with required examples `radio station` pass / `io2026!!!` + `io$$$2026$$$` fail, culture-independent, no outbound call, rule reject does not count toward lockout/throttle. auth-08 / -13 cite v2.3. TD-ADM-050/051/024/025/026 bind lockout v5.5 (`8a194eb9…`): 5/15→30, 20/15→30 MUST 429, reset does not lift, break-glass ops-only, step-2 on shared counter |
| 7 | Retired hold phrase absent; no >5× consecutive repeats | **PASS** | Case-insensitive scan of the retired two-word hold phrase on design + revision note: **0 hits**. Consecutive same-token >5: **none**. Info: §1 still cites the pre-existing CFO estimate DOC-FLOW filename that embeds the old hyphenated token (path cite only) |
| 8 | Internal consistency; one Active holds list | **PASS** | Single **Active holds** list in §0 (11 items); per-case “Waits on:” points there. §6 OQs align with cases (OQ1→040, OQ3→050/051, OQ5→051, OQ9/OQ11 closed, OQ12/OQ13 awareness). No AWS account IDs / ARNs invented. Host `admin.core.dealoware.com` only |

## Gaps

| Sev | Gap | Fix |
|-----|-----|-----|
| — | None blocking | — |
| Info | Lockout / password-rules / sign-in-steps note **Status** headers still say Draft while Spec QA + Security QA PASS on tip | Optional: flip Status to Final on the next Spec edit; cites already treat them as final. Non-blocking |
| Info | Sign-in steps note still points at lockout v5.4 in a few lines (Spec QA O2 / addendum) | Non-blocking per Spec QA; next Spec edit should name v5.5 `8a194eb9…` |
| Info | CFO estimate DOC-FLOW filename in §1 embeds the old hyphenated token | Optional: cite by date/title only, or rename the finance file later |

## Active holds (from design §0 — unchanged by this stamp)

1. Stories/build stay held until Chief QA / CPM accept this Test design QA stamp.
2. UI build lanes and addendum Part A Draft — all `TD-ADM-UI-*` wait on Chief UI/UX.
3. Addendum Parts B–D not written — `TD-ADM-UI-na-*` stay SKIP stubs.
4. Delete UI decisions / copy (C01–C07) await Spec lock + addendum Part C.
5. Spec must adopt S-A12 sections and idle warning (UX1-A21 / A20); S-A12 password-change parts of auth-10 / -13 wait.
6. Spec has not adopted route names for the S-A inventory.
7. SA has not locked TOTP period or recovery-code count (OQ2; 6 digits locked).
8. X08 CI: DevOps owns pipeline wiring; QA owns catalog (OQ10).
9. UI/UX QA cross-checks of the three Spec notes follow Spec PASS; OQ13 addendum drift goes to Chief UI/UX.
10. Deploy stays held until Ivan OK; admin.core deploy/attach also waits on A11 and PR #11 conditions. No live admin.core calls.
11. No password values, keys, AWS account IDs or SES identities.

PoC $0.

## Stamp

**PASS** — design tip `8df392554ed887322bd7dc3f6633933bca26209c3bcfbb29321bf055f89814c9` meets UX1-A16 two-step coverage (+10 cases → 114), final cites (lockout v5.5 / password-rules v2.3 / sign-in steps v1 / A16 decision), A12/C08/X07/X08 presence, and no regression vs the r2 Product QA PASS on TD-ADM-050 / TD-ADM-051 / OQ1 / OQ3 / OQ5 / Core API H4.

Report: `/workspace/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-ux1-a16-final-cites.md`
