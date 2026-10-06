# Security QA confirm — Spec checklist point 5 vs lockout-window note v5.5 (cite-only)

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~8:27pm ET) |
| Verdict | **PASS** (point 5) on tip `8a194eb9…` only |
| Asked by | Senior Spec (v5.5 cite-only reconfirm) |
| Note | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` **v5.5** |
| Note tip sha256 | `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` — **MATCH** (re-hashed immediately before write) |
| Main Spec (unchanged) | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — cited note line 9 |
| UX1-A03 r2 | tip `0817b7676a0103d606197c07fb6ac48782c12fbd363304637e5340b86af6d3f0` — cited note line 22 |
| UX1-A16 decision | tip `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34` — cited note line 23 |
| Security answers (items 3–4) | Note cites `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` — **MATCH** on-disk `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` (re-hashed). Items 3–4 unchanged vs prior score. |
| Prior tips VOID for point 5 | `c346f0b1…` (v5.4), `67686ce5…` (v5.3), and earlier — **VOID / SUPERSEDED** |
| Sign-in steps | tip `0a3db5f4…` still current — **not re-scored** this turn |
| Password-rules | **not re-scored** this turn |
| Checklist | Spec Security checklist **point 5** |
| Scope | Paper qa-confirm only. **Not a build unlock.** PoC **$0** |

Active holds:
- Build / deploy stay held; point 5 PASS is not a build unlock
- Gate scope (per CPM clarification): auth/lockout Stories 2, 3, 14 and lockout parts of 4/5 only; not A7 PR #20 Steps 1/11
- Do not invent password values, AWS account IDs, Turnstile keys, or HMAC keys
- Process-global auth flood limiter stays withdrawn until Chief Security approves

## Tip identity (cite-only delta)

| Check | Result |
|-------|--------|
| Note tip MATCH expected `8a194eb9…` | **MET** |
| Answers cite `a87e293b…` (was `f03a82c9…` on v5.4) | **MET** (Sources line 21; Rev line 7) |
| On-disk answers tip MATCH `a87e293b…` | **MET** |
| Items 3–4 still bind; content unchanged | **MET** (answers §§3–4; note closes items 3–4) |
| Prior v5.4 / v5.3 tips superseded for point 5 | **MET** |
| Main Spec tip unchanged `25f2647…` | **MET** (cited) |
| Cites A03 `0817b767…` | **MET** |
| Cites A16 `bb0bcaa2…` | **MET** |

## Spot-check — no rule regression vs v5.4 PASS

| Check | Result | Evidence (tip line) |
|-------|--------|---------------------|
| C4: `signin.second_factor_failed` in §8.8 under bad 2FA + "after correct password" flag | **MET** | 150 |
| C4: S-A12 closed list includes code failed after correct password (`signin.second_factor_failed`) | **MET** | 121 |
| Step-2 5-attempt cap on top of shared account counter | **MET** | 62 |
| Account-level → identical 401 + same body | **MET** | 97 |
| Login IP MUST 429, same body, no Retry-After | **MET** | 98 |
| Reset/bootstrap IP MUST 429; reset-request same reply, no email when throttled | **MET** | 99-100 |
| Completed reset clears neither lock nor account failure count; IP counters unchanged | **MET** | 67, 71 |
| Explicit §8.6 unlock-by-reset REPLACED | **MET** | 71, 148 |
| No counter / 401 / 429 / unlock-by-reset rule change vs v5.4 | **MET** | Rev line 7; carried locks above |

Carried locks from v5.4 PASS (sliding window, Turnstile vs counters, break-glass ops-only, residual risk, S-A12 last-20 + keyed-HMAC IP prefix, 24h lock notice, lock-notice email SHOULD, no secrets invents) remain **MET** by cite of prior confirm on tip `c346f0b1…` plus spot-check above; this turn is cite-only and does not rewrite every cell.

## Hygiene scan

- Retired hold phrase: **Absent**. Plain Active holds only.
- Secrets / password invents / Turnstile keys / HMAC keys / AWS account IDs: **None**
- Raw IPv4/IPv6: **None**; IP mentions are prohibitions or keyed-HMAC / prefix only
- Handshake = qa-confirm only; not a build unlock: **Present**

## Observations (non-blocking, no PASS impact)

- O1: Cite-only delta closes prior v5.4 OBS O1 (answers cite drift `f03a82c9…` → on-disk `a87e293b…`). Note and disk now agree.
- O2: Items 3–4 substance unchanged; password-rules / sign-in-steps not re-scored here.
- O3: C4 event remains audit/S-A12 only; public client never names the factor.

## Disposition

- **PASS** Spec checklist **point 5** on tip `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` only.
- Prior PASS on `c346f0b1…` (v5.4) and `67686ce5…` (v5.3) **VOID / SUPERSEDED** for point 5.
- Not a build unlock. PoC **$0**.
