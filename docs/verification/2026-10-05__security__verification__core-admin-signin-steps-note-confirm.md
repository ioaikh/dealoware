# Security QA confirm — Core admin sign-in steps Spec note (UX1-A16 bind) v1

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~8:25pm ET) |
| Verdict | **PASS** |
| Asked by | Senior Spec (sign-in steps tip `0a3db5f4…`) |
| Note | `specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` **v1** |
| Note tip sha256 | `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` — **MATCH** (re-hashed immediately before write) |
| A16 decision | `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` tip sha256 `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34` — **MATCH**; cited full in note Sources (line 21) |
| Prior A16 Security QA | `verification/2026-10-05__security__verification__ux1-a16-signin-step-shape-confirm.md` — PASS with Spec-carry **C1–C4**; cited note line 22 |
| Main Spec (unchanged) | tip `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — cite-only; note does not edit it |
| Sibling | Lockout note v5.4 (C4 placement) — separate point-5 reconfirm |
| Scope | Paper qa-confirm only. **Not a build unlock.** PoC **$0** |

Active holds:
- Build / Stories / code / CDK / spend / deploy stay held — this note is not a build unlock
- No admin.core deploy or attach until A11, PR #11 conditions, and Ivan's OK via Bot Manager
- Do not invent password values, Turnstile keys, HMAC keys, or AWS account IDs
- Process-global auth flood limiter stays withdrawn until Chief Security approves

## Tip identity

| Check | Result |
|-------|--------|
| Note tip MATCH expected `0a3db5f4…` | **MET** |
| Cites A16 decision tip `bb0bcaa2…` (full hash) | **MET** (line 21) |
| Cites prior A16 Security QA confirm (C1–C4) | **MET** (line 22) |
| Main Spec tip unchanged `25f2647…` | **MET** (lines 9, 92) |
| Password-rules tip `7f73db9b…` left unchanged | **MET** (lines 12, 101) |

## Binding conditions 1–8 (vs A16 decision + prior PASS)

| # | Condition | Result | Evidence (note) |
|---|-----------|--------|-----------------|
| 1 | Pending-auth token: opaque, ≥128 bits, no session; bound to account + Turnstile; ≤5 min, single use; cleared on success/expiry/lock; host-only cookie Secure/HttpOnly/SameSite=Strict; grants only step 2 | **MET** | Lines 47 |
| 2 | One failure counter: wrong password / TOTP / recovery → same account counter; no separate step-2 budget | **MET** | Lines 49 |
| 3 | Step-2 cap: ≤5 code attempts per pending token, then void; restart step 1 + new Turnstile; on top of condition 2 | **MET** | Lines 51 |
| 4 | Generic failure text both steps (A02/A03); locked account identical 401 at step 1 even if password correct; never reaches step 2 while locked | **MET** | Lines 53 |
| 5 | No hints: no Retry-After, duration, count, or "locked"; unknown email vs wrong password match status/body/timing (dummy hash) | **MET** | Lines 55 |
| 6 | Owner notice: `signin.second_factor_failed` audit (HMAC IP prefix); S-A12; lock-notice email SHOULD also cover correct-password then repeated code failures | **MET** | Lines 57 |
| 7 | Session only after valid TOTP or recovery; rotate session ID; invalidate pending; recovery spent immediately | **MET** | Lines 59 |
| 8 | Host isolation: both step endpoints only on admin.core.dealoware.com; never api.core or raw host | **MET** | Lines 61 |

## Spec-carry C1–C4 (from prior A16 PASS)

| # | Spec lock required | Result | Evidence (note) |
|---|--------------------|--------|-----------------|
| C1 | Login IP throttle **429** + same generic body + **no** Retry-After on **both** steps | **MET** | Lines 71 |
| C2 | Wrong TOTP/recovery at step 2 also counts toward login IP throttle | **MET** | Lines 73 |
| C3 | At step 1 when locked, password hash still runs (timing parity with A03 r2) | **MET** | Lines 75 |
| C4 | `signin.second_factor_failed` mapped in lockout note into §8.8 + S-A12 (display: code failed after correct password) | **MET** (bind here; placement scored on lockout v5.4) | Lines 74; sibling lockout v5.4 |

## UX / amend scope

| Check | Result | Note |
|-------|--------|------|
| Two-step shape: step 1 email+password+Turnstile; step 2 TOTP + recovery link | **MET** | Lines 38-39 |
| S-A01 focus order (step 1 / step 2) | **MET** | Lines 80-81 |
| Amends §8 sign-in flow + S-A01; does not edit main Spec file | **MET** | Lines 86-92 |
| Session timers stay in §8.7 (untouched) | **MET** | Line 94 |

## Hygiene scan

- Retired hold phrase: **Absent** (case-insensitive; 0 hits). Plain Active holds only.
- Secrets / password invents / Turnstile keys / HMAC keys / AWS account IDs: **None**
- Raw IPv4/IPv6: **None**; IP appears only in HMAC-prefix / throttle prose
- Handshake = qa-confirm only; not a build unlock: **Present** (Active holds + Handshake)

## Observations (non-blocking)

- O1: C4 placement is carried by lockout note v5.4; this note correctly points there and does not duplicate the §8.8 / S-A12 list.
- O2: Conditions 1–8 are carried as written (no paraphrase that drops cookie attributes, bit length, or host isolation).

## Disposition

- **PASS** on tip `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` only.
- Void on any newer hash of this note.
- Not a build unlock. PoC **$0**.
