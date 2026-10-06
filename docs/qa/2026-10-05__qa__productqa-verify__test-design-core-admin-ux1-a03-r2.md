# Product QA — Test design re-verify (Core admin dashboard r2 / UX1-A03 r2)

| Field | Value |
|-------|--------|
| Role | Senior Product QA (Test design QA) — independent of Chief QA author |
| Date | 2026-10-05 (~evening ET) |
| Scope | Form of test cases vs Security r2 + lockout note v5.x — not Spec invent, not Stories/code, not merge/deploy |
| Design | `/workspace/dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md` |
| Verified sha256 | `110e075ffc0176ec59f517880cd7090a67f27216b885d9feb0ac476832a142ab` — **MATCH** expected |
| Overall | **PASS** |
| PoC | $0 |

## Sources checked (read-only)

| Source | Path | Hash / note |
|--------|------|-------------|
| Design r2 | `dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md` | `110e075ffc0176ec59f517880cd7090a67f27216b885d9feb0ac476832a142ab` MATCH |
| Revision note | `/workspace/qa/2026-10-05__qa__revision-note__test-design-core-admin-ux1-a03.md` | cites same design hash |
| UX1-A03 Security r2 | `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md` | `0817b7676a0103d606197c07fb6ac48782c12fbd363304637e5340b86af6d3f0` — MATCH design cite |
| Lockout window note v5.3 | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` | `67686ce5044728db5cb754ffb65c68562e5ed38aea26161bf90988a145972b8b` — MATCH design cite; Draft for Spec QA + Security QA |
| H4 context | `/workspace/qa/2026-10-05__qa__qa-report__harden-h1-h6-product-verify-1204.md` | Core API PR #4 head `13a9c20`: 429 + Retry-After + JSON on `/auth/register` and `/auth/token` |

## Verdict table

| # | Item | Verdict | Evidence |
|---|------|---------|----------|
| 1 | **TD-ADM-050** — account failures incl. locked → same 401 + r2 body; no locked/duration/count; reset does not unlock/clear; TOTP still required | **PASS** | Design §0 table + TD-ADM-050 Expected: all account-level failures (incl. locked + refused correct password) return **401** with body byte-identical to wrong-password copy *“We couldn't sign you in. Check your details and try again later. You can also reset your password.”* — matches Security r2 Copy section exactly. No `Retry-After`, remaining time, attempt count, or word “locked”. Step 6: completed reset does **not** end lock, clear account count, or reset IP counters; sign-in still 401 generic until lock ends; **TOTP still required** after reset. Matches Security r2 lock rules + lockout note OQ3 REPLACED unlock-by-reset. Steps assert status + body + headers; server proof via audit `locked` + correct password refused. |
| 2 | **TD-ADM-051** — IP throttle → **429** + same generic body; **no Retry-After**; throttled reset → exists reply + no email | **PASS** | TD-ADM-051 Expected Step 2: **HTTP 429**, body byte-identical to 401 generic sign-in body, **No `Retry-After` header**. Step 4: throttled reset-request → **429** + same “If that account exists, we've sent instructions” reply; **no email sent** (mail fake zero sends); no `Retry-After`. Matches lockout note client table (MUST 429, no Retry-After, throttled reset no email) and Security r2 (no Retry-After on any auth response; reset reply always generic). Design note acknowledges Security r2 allows 401-or-429 for throttle while lockout note + CPM lock **MUST 429** — design asserts 429. Negative variants FAIL if Retry-After present. |
| 3 | **OQ5 / OQ1 / OQ3** closed consistently | **PASS** | §6: **OQ1 Closed** — Turnstile before credential check, no counter, audit `captcha-failed` (lockout note OQ1 + Arch note); tested TD-ADM-040. **OQ3 Closed** — sliding 15 min, 5/20, flat 30 min, in-lock not counted, wrong recovery = bad 2FA, separate reset/bootstrap IP counter; tested TD-ADM-050/051. **OQ5 Closed** — no admin.core auth response carries `Retry-After`; TD-ADM-051 asserts 429 + absence. Resolutions match Security r2 + lockout note. OQ7 Active hold (re-cite v5.3 final hash) and OQ8 open (break-glass / S-A12 / lock-notice email) correctly left open — no invent. |
| 4 | Core API **H4** unchanged | **PASS** | §0 Core API H4 line + §1 Out of scope + OQ5 disposition + TD-ADM-051 preconditions: Core API `/auth/register` and `/auth/token` still expect **HTTP 429 + Retry-After**; pattern not applied to admin.core; admin.core no-Retry-After rules not applied to Core API. Aligns with H4 product verify (PR #4 `13a9c20`: 429 + Retry-After + JSON). No contradiction or rewrite of api.core behavior. |
| 5 | No retired hold shorthand; no word/phrase repeated >5× in a row | **PASS** | Case-insensitive scan of the retired two-word hold phrase on design + revision note: **0 hits**. Consecutive same-token scan (>5): **none**. Holds stated as plain **Active holds** (Stories/build until Test design QA confirms; A7 until H4 live PASS; deploy until Ivan OK; lockout note v5.3 hash re-cite). Informational only: §1 binding table cites a pre-existing CFO estimate DOC-FLOW filename that still embeds the old hyphenated token; path cite only, not hold jargon. |
| 6 | Internal consistency + executable asserts | **PASS** | r2 generic body quoted identically in §0 summary, TD-ADM-050, TD-ADM-051, TD-ADM-020. Cases ↔ §6 OQ table aligned (OQ1→040, OQ3→050/051, OQ5→051). Steps capture status + body + headers; mail fake for zero-send; audit reason classes `locked` / `rate-limited` / `captcha-failed` never echoed to client (TD-ADM-100). No AWS account IDs, ARNs, or CDK invent — only “do not invent” prohibitions and out-of-scope NAT/CDK/bot-platform/App Runner. Host `admin.core.dealoware.com` only. |

## Gaps

| Sev | Gap | Fix |
|-----|-----|-----|
| — | None blocking | — |
| Info | Finance estimate DOC-FLOW filename in §1 binding table still embeds the old hyphenated token | Optional: display cite by date/title only, or rename the finance file later. Not a design FAIL. |
| Info | Lockout note v5.3 still Draft (OQ7); Senior Spec may still edit | Active hold already recorded: re-cite final hash after Spec QA + Security QA; re-check TD-ADM-050/051 if text changes. |

## Active holds

- Stories / build stay held until this Test design QA stamp is accepted by Chief QA / CPM.
- A7 admin build stays held until H4 live PASS (separate harden track).
- Deploy stays held until Ivan OK.
- Lockout note v5.3 final hash to re-cite after Spec QA + Security QA PASS; if the note text changes, re-check TD-ADM-050 and TD-ADM-051.
- Process-global auth flood limiter stays withdrawn until Chief Security approves it (TD-ADM-054).
- Do not invent passwords, AWS account IDs, SES identity, Turnstile keys, HMAC keys, Stories, code, CDK, spend, or provision from this design.
- PoC $0.

## Stamp

**PASS** — design tip sha256 `110e075ffc0176ec59f517880cd7090a67f27216b885d9feb0ac476832a142ab` meets UX1-A03 r2 + lockout note v5.3 (as Draft) for TD-ADM-050 / TD-ADM-051 / OQ1 / OQ3 / OQ5 / Core API H4 separation / wording rules. Ready for Chief QA to route to CPM + Senior Spec with design path + hash.

Report: `/workspace/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-ux1-a03-r2.md`
