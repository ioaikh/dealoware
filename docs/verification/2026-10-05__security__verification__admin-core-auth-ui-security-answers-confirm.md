# Security QA confirm — admin.core auth UI Security answers (CPM four items)

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~8:30pm ET) |
| Verdict | **PASS** |
| Subject | `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` |
| Answers tip sha256 | `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` — **MATCH** (re-hashed with `sha256sum`, expected prefix `a87e293b`) |
| Prior tips | `f03a82c9b03cbca18c1c15fed2ecb01ddffe51bce439cd12971a916db4e88a25` **VOID** for answers tip identity (superseded); `8cf244d3…` VOID; `30efe246…` VOID |
| Delta | Deferred tidy (item 1 (c) strip set, case folding, MUST inline error): `verification/2026-10-05__security__verification__admin-core-auth-ui-security-answers-context-word-delta-confirm.md` |
| Asked by | Chief Security |
| Scope | Paper-only full re-score of the answers file on the new tip |
| PoC | **$0** |
| Build | **Not a build unlock** (no Stories, code, CDK, spend or deploy) |

Active holds:
- Not a build unlock; build follows the A7 unlock path only
- Do not invent passwords, Turnstile keys, AWS account IDs or raw IPs
- Process-global auth flood limiter stays withdrawn until Chief Security approves it
- PoC **$0**

## Meta

| Check | Result |
|-------|--------|
| Retired hold-tag phrase | **MET** (absent; file uses plain "Active holds") |
| Secrets, passwords, Turnstile keys, AWS IDs | **MET** (none) |
| Raw IPs | **MET** (none; HMAC IP prefix only) |

## Item 1 — Password rules (UX1-A17)

| Rule | Required | Result |
|------|----------|--------|
| Min length 15; accept ≥128; no silent truncation | Stated | **MET** |
| All printable / Unicode; NFKC before hashing | Stated | **MET** |
| No composition rules; no periodic expiry | Stated | **MET** |
| Normalization pipeline for context words | NFKC, then **Unicode default full case folding, locale-independent** | **MET** — new; replaces plain "lowercase" (closes Turkish-I / locale drift class; `ß`→`ss` handled) |
| (a) Equals any context word, any length | Reject | **MET** |
| (b) Contains a context word of ≥4 chars | Reject; short local parts (e.g. `io`) not contains-checked | **MET** — `radio` still allowed |
| (c) After removing Unicode categories **N, P, S, Z**, equals any context word (any length) | Reject | **MET** — new strip set adds Symbols (S) and all Z; catches `io2026!!` and `io$$$2026$$$` |
| Context set: dealoware, admin, email local part | Stated | **MET** |
| Bundled local blocklist; no external breach API; $0 | Stated | **MET** |
| Reuse: reject equal to current | Stated | **MET** |
| Hashing: Argon2id (≥19 MiB, t=2, p=1) preferred, or PBKDF2-HMAC-SHA512 **≥220,000** | Stated | **MET** |
| Server is authority; client checks length only | Stated | **MET** |
| Paste / password managers allowed | Stated | **MET** |
| Inline error on rejected set/reset | **MUST** name the failed rule | **MET** — closes prior Security OBS-1 and the Spec QA point; enumeration-safe because screens already need a valid reset link or session |
| Never log or echo password | Stated | **MET** |
| Reset/change ends all sessions, burns link; TOTP enrolment unchanged | Stated | **MET** |

## Items 2–4 (re-checked on new tip)

| Item | Check | Result |
|------|-------|--------|
| 2 — Generic failure copy | Text matches UX1-A03 r2 decision verbatim; same for every 401 and the 429 IP throttle; no Retry-After, duration, count or "locked" wording; never names the failed factor | **MET** |
| 3 — Reset vs lock / sessions | Reset does not end an active lock or reset counters; after expiry needs new password **plus TOTP**; §8.6 clause superseded via lockout note v5; Turnstile + 20-per-IP throttle + lock-notice email + audited server-side ops clear; process-global limiter withdrawn | **MET** |
| 4 — Lock status in S-A12 | Last 20 auth audit events incl. recovery code used and TOTP changed; keyed-HMAC IP prefix only, **no raw IP**, browser family only, no secrets; lock notice visible to fully signed-in owner only; lock-notice email SHOULD with no IP, counts or bypass links | **MET** |
| TOTP / recovery | TOTP enrolment unchanged by reset; second factor still required after lock expiry; recovery-code use audited | **MET** |

## OBS (non-blocking)

1. **Cite drift only.** Spec password-rules v2.2 tip `7f73db9ba0fbd8a3fc46686b6b2ee2522ea86c72334ede5cae1b587b6ebc32a7` still cites answers `f03a82c9…`. Spec v2.2 already says MUST for the inline error; the stricter (c) strip set and full case folding arrive in Chief Spec's later batch with a re-cite to `a87e293b…`. No Spec reconfirm this turn (no GAP).
2. **(c) strip set excludes Cc/Cf.** Format characters such as U+200D (zero-width joiner, category Cf) survive (c), so `i<ZWJ>o2026` is not caught by the context-word rule. Low risk: 15-char minimum and the bundled blocklist still apply, and context words are a defense-in-depth layer. Suggest a later tidy add Cf (or reject default-ignorable code points) and confirm Cc is rejected under "printable characters".
3. **Context words normalization.** File implies but does not state that context words (especially the email local part) run through the same NFKC + case folding before comparison. Spec later batch should state it.

**GAPs:** none.

## Disposition

**PASS** on answers tip `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` only. Prior tip `f03a82c9…` VOID for this file. Not a build unlock. PoC **$0**.
