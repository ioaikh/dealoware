# Security QA confirm — Core admin password-rules Spec note (UX1-A17) v2.3

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~8:30pm ET) |
| Verdict | **PASS** |
| Asked by | Senior Spec (v2.3 tip `1a7b342c…`) |
| Note | `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` **v2.3** |
| Note tip sha256 | `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` — **MATCH** (re-hashed with `sha256sum` immediately before write) |
| Main Spec (unchanged) | sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — **MATCH** |
| CS answers item 1 | `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` tip sha256 `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` — **MATCH**; cited with full hash in note Revision (line 7), Sources (line 20), and Done-list (line 109) |
| Answers Security QA PASS | `verification/2026-10-05__security__verification__admin-core-auth-ui-security-answers-confirm.md` (PASS on `a87e293b…`) |
| Prior tips | `7f73db9ba0fbd8a3fc46686b6b2ee2522ea86c72334ede5cae1b587b6ebc32a7` (v2.2) **VOID** for this confirm; `1ce5e7f3…` (v2.1), `1189b559…` (v2), `66dfedac…` (v1) also VOID |
| Scope | §8.2 / §8.4 + S-A12 password change; delta vs v2.2 is context-word v1.2 + answers re-cite. Lockout and sign-in steps **not** re-scored (sign-in `0a3db5f4…` PASS stands) |
| PoC | **$0** |

Active holds (unchanged by this PASS):
- Build, Stories, code, CDK, spend, and deploy stay held; this note is not a build unlock
- Do not invent password values, AWS account IDs, Turnstile keys, or HMAC keys
- Process-global auth flood limiter stays withdrawn

## Tip identity

| Check | Result |
|-------|--------|
| Note tip MATCH expected `1a7b342c…` | **MET** |
| Main Spec tip unchanged `25f2647…` | **MET** (note line 9, line 75) |
| Cites answers tip `a87e293b…` (full hash) | **MET** (lines 7, 20, 109) |
| Answers tip has Security QA PASS on disk | **MET** |
| Prior `7f73db9b…` void for this confirm | **MET** |

## 1. Context-word match v1.2 vs answers item 1 (tip `a87e293b…`, line 22)

| Clause | Answers item 1 | Spec Rule 4 (line 45) | Result |
|--------|---------|-------------|--------|
| Normalization order | NFKC, then Unicode default full case folding (locale-independent) | Same, same order | **MET** |
| (a) equals any context word, any length | Reject | Reject | **MET** |
| (b) contains, only words of 4+ chars | dealoware, admin; local part only when 4+ | Same | **MET** |
| (c) remove Unicode N, P, S, Z, then equals any word (any length) | Reject; catches `io2026!!` and `io$$$2026$$$` | Same categories and both examples | **MET** |
| Short local part false positive (`io` vs `radio`) | Closed | Closed (Done-list line 110) | **MET** |
| Context set: dealoware, admin, email local part | Stated | Stated | **MET** |
| Local only; outside breach API out for v1; $0 | Yes | Yes | **MET** |

## 2. Inline error names the failed rule

| Check | Result |
|-------|--------|
| Spec binds **MUST** name the failed rule (line 62; Done-list line 114) | **MET** |
| Matches answers item 1 (line 31) MUST wording | **MET** — prior OBS-1 ("may" vs MUST) closed on both files |
| Four messages, one per rule class (too short, over 128, common/context, same as current) | **MET** |
| No password echo, no hint in messages | **MET** |

## 3. Hashing floor (unchanged from v2.2)

| Check | Result | Evidence |
|-------|--------|----------|
| Argon2id **preferred**, 19 MiB or more, t=2, p=1 | **MET** | Rule 6 |
| PBKDF2-HMAC-SHA512 **220,000 or more** (OWASP) | **MET** | Rule 6; Done-list line 112 |
| Per-user salt; never log or echo password | **MET** | Rule 6 |
| Security floor; SA picks scheme at or above | **MET** | Rule 6; Purpose |

## 4. Other item-1 binds (unchanged from v2.2, re-checked on this tip)

| Bind | Result | Evidence |
|------|--------|----------|
| Length 15 minimum, accept up to 128, Unicode code points after NFKC, no silent truncation, same count client and server | **MET** | Rule 1 |
| All printable, spaces, Unicode; NFKC before hashing and normalized checks | **MET** | Rule 2 |
| No composition, no periodic expiry, no hints | **MET** | Rule 3 |
| Bundled local top-100k list shipped with app | **MET** | Rule 4 |
| Reuse: reject equal to current only (incl. S-A12); bootstrap has none | **MET** | Rule 5 |
| Server authority; client checks length only (blur + submit) | **MET** | Rule 7; UI table |
| Paste and managers allowed; `autocomplete="new-password"` | **MET** | Rule 8 |
| Rule reject does not count toward lockout or IP throttle; valid link stays usable | **MET** | Rule 9; §8.4 extension |
| Helper text exact; optional counter; no color meter | **MET** | UI table |
| Success ends **all** sessions and burns reset link; TOTP unchanged | **MET** | UI table; §8.4 / S-A12 extensions |
| Scope §8.2, §8.4, S-A12; no password values; PoC $0 | **MET** | Header; Out of this note |

## 5. Enumeration

| Check | Result | Evidence |
|-------|--------|----------|
| Named-rule errors only on set, reset, and change screens that already need a valid link or session | **MET** | UI table line 62 |
| No new client-visible text on sign-in or reset-request surfaces | **MET** | Scope limited to §8.2 / §8.4 set-password + S-A12 |
| Rule reject does not change lockout counters (no oracle via lock state) | **MET** | Rule 9 |

## Meta

| Check | Result |
|-------|--------|
| Retired hold-tag phrase | **MET** (absent; note uses plain "Active holds", line 88) |
| Secrets, password values, AWS IDs, keys, raw IPs | **MET** (none) |

## OBS (non-blocking)

1. **(c) strip set excludes Cc/Cf** (carried from answers confirm OBS-2). Format characters such as U+200D (category Cf) survive (c), so `i<ZWJ>o2026` is not caught by the context-word rule. Low risk: 15-char minimum and the bundled blocklist still apply. Suggest a later tidy add Cf (or reject default-ignorable code points) in answers first, then Spec.
2. **Context words normalization** (carried from answers confirm OBS-3). Rule 4 states NFKC + case folding for the password but does not say the context words (especially the email local part) run through the same pipeline before comparison. Suggest stating it at the next edit.
3. **Bootstrap link wording** (carried from v2.2 OBS-2). Line 62 says "valid reset link or signed-in session"; the §8.2 bootstrap form is gated by the bootstrap link (covered by line 69). Suggest "valid bootstrap or reset link" at the next edit.
4. **Reset-request same-response rule** (carried from v2.2 OBS-3, outside this note). Belongs to the main Spec / lockout track.

**GAPs:** none.

## Disposition

- **PASS** on tip `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` (v2.3), citing answers tip `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` (Security QA PASS on disk).
- Prior v2.2 tip `7f73db9b…` VOID for this confirm.
- Lockout and sign-in steps not re-scored; sign-in `0a3db5f4…` PASS stands.
- OBS 1–4 non-blocking. Not a build unlock. PoC **$0**.
