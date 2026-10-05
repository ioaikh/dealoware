# Spec QA verification — Core admin dashboard

| Field | Value |
|-------|-------|
| Written by | Dealoware Spec QA |
| Date | 2026-10-05 (~1:48–1:50pm ET Soft HOLD content verify on **v2.2**; **~1:54pm ET formal PASS**) |
| Spec under test | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` (**v2.2**, Chief Spec CLEAR; KB only, no PR) |
| Spec sha256 | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — **MATCH** (re-computed `sha256sum` at formal PASS ~1:54pm ET) |
| Binding Product | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` — **13:31** (includes 1:27pm patch + 1:30pm CEO final) |
| Binding SA Soft HOLD SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` (amended; CA grounding PASS + Arch QA formal PASS cited in Spec Sources) |
| Spec Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` (**v2** points **1–15**) |
| Security QA Soft HOLD SoR qa-confirm | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` — **PASS 15/15 MET** (0 GAP, 0 PARTIAL), ~1:53pm ET; Spec tip sha256 **MATCH**; Security Soft HOLD SoR **CLEARED** |
| Host | `admin.core.dealoware.com` (Core admin). Platform hosts are not Core. |
| Verdict | **PASS** — formal Spec gate on Core admin Spec **v2.2**. Product AC1–AC9 + must-covers + SA reconcile MET; §13 binds checklist **v2 pts 1–15**; Security QA Soft HOLD SoR qa-confirm PASS 15/15 with sha256 MATCH → Soft HOLD PASS rule satisfied. **Not a build unlock.** |
| Soft HOLDs remaining | Soft HOLD invent Stories / build until **Dev Plan + Test design PASS** · Soft HOLD invent passwords / AWS · Soft HOLD deploy · Soft HOLD **A7 until H4 live PASS** · Soft HOLD code / CDK / spend / provision · PoC **$0** |
| Constraints | Spec / Product / SA / Security files not edited by Spec QA. No PR. No merge. No build unlock. No password values. No AWS account details. |

---

## Changelog (Soft HOLD / PASS history)

| When (ET) | Verdict | Note |
|-----------|---------|------|
| ~1:15pm | **PASS** (Spec gate, superseded) | Prior PASS on pre-expansion Spec vs Product 13:06 + Security checklist **v1 pts 1–10** + Security QA qa-confirm 10/10. **Superseded** by Product 13:31 + checklist **v2** + Spec **v2.x**. |
| ~1:32pm+ | Soft HOLD / pause | CEO architecture-first order; Spec QA paused on v2.1 pending SA Soft HOLD SoR CLEAR + Arch QA + CFO + Spec reconcile. |
| ~1:48–1:50pm | **Soft HOLD content OK** | Spec **v2.2** content verify OK. Soft HOLD PASS until Security QA confirms checklist **v2**. |
| **~1:54pm** | **PASS** (formal Spec gate, **live**) | Security QA Soft HOLD SoR qa-confirm v2 PASS 15/15 MET, sha256 MATCH (~1:53pm). Spec sha256 re-confirmed MATCH. Soft HOLD lifted → formal PASS on v2.2. |

---

## DOC-FLOW

| Artifact | Path | Result |
|----------|------|--------|
| Spec v2.2 | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | **PASS** — sha256 MATCH; §13 binds v2 1–15 |
| This evidence | `verification/2026-10-05__spec__verification__core-admin-dashboard.md` | **PASS** (formal Spec gate) |
| Product scope **13:31** | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | Binding AC1–AC9; MET vs Spec |
| SA Soft HOLD SoR (amended) | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` | Spec adopted SA open picks; no lock conflicts found |
| CA design grounding PASS | `verification/2026-10-05__ca__verification__core-admin-dashboard-design-grounding-pass.md` | PASS (~1:46pm ET) — cited in Spec Sources |
| Arch QA CEO-amend review | `verification/2026-10-05__sa__verification__core-admin-dashboard-ceo-amend-review.md` | Formal Arch QA PASS — cited in Spec Sources |
| Spec Security checklist **v2** | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` | Binding pts **1–15**; Spec §13 bound |
| Senior Security points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-points-review.md` | v2.2 FINAL 15/15 MET, sha256 match (~1:50pm) — cited; **not** Soft HOLD SoR |
| **Security QA Soft HOLD SoR qa-confirm** | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` | **PASS 15/15 MET** (0 GAP / 0 PARTIAL) on Spec v2.2; sha256 **MATCH**; **Soft HOLD SoR** for checklist v2 (v1 10/10 superseded) |

---

## PASS basis

**PASS.** Soft HOLD PASS rule (Senior Spec + Spec §13 + checklist v2 handshake) was: Spec QA must not PASS until Security QA confirms Spec Security checklist **v2** pts 1–15. That condition is now met.

| Check | Status |
|-------|--------|
| Checklist v2 present under `verification/` | **Yes** — pts 1–15 ISSUED |
| Spec §13 binds v2 pts 1–15 with section cites | **Yes** — all 15 **MET** |
| Senior Security points-review v2.2 FINAL | 15/15 MET, sha256 match — cited (not Soft HOLD SoR) |
| Security QA Soft HOLD SoR qa-confirm for checklist **v2** | **PASS 15/15 MET**, 0 GAP, 0 PARTIAL (~1:53pm ET) |
| qa-confirm Spec tip sha256 | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — **MATCH** |
| Spec file sha256 re-computed by Spec QA (~1:54pm ET) | `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` — **MATCH** (unchanged since Soft HOLD content verify) |
| Soft HOLD build / PoC $0 / no password values / no AWS details | Held |

Note: Chief Security still issues the formal Spec **Security** PASS/HOLD to Chief Spec + CPM (per qa-confirm handshake); that is Security's gate, not a precondition named in the Spec QA Soft HOLD PASS rule.

---

## 1. Spec identity

| Check | Result |
|-------|--------|
| Path exists | **MET** |
| Revision **v2.2**, Chief Spec CLEAR | **MET** — header Status / Brief / §15 |
| sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` | **CONFIRMED** |
| Host `admin.core.dealoware.com` | **MET** |
| Binding Product **13:31** | **MET** |
| Binding SA Soft HOLD SoR + CA grounding PASS cited | **MET** — Sources + header |
| Spec Security checklist **v2 pts 1–15** in §13 | **MET** |
| Spec sha256 re-confirmed at formal PASS (~1:54pm ET) | **MATCH** `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` |
| Soft HOLD build; PoC $0; no password values | **MET** |
| Spec does not implement .NET 10 retarget | **MET** |

---

## 2. Product AC1–AC9 (binding **13:31**)

| AC | Product requirement | Spec cite | Result |
|----|---------------------|-----------|--------|
| AC1 | Open Core admin at `admin.core.dealoware.com`, not `admin.platform` | Header; Locked #1; §3; §12 | **MET** |
| AC2 | List / open / edit / delete Participants (not human users) + search by Participant name | §4.1, §4.6, §5, §9–§11 | **MET** |
| AC3 | List / open / edit / delete Artifacts + search by Artifact name | §4.2, §4.6, §5, §9–§11 | **MET** |
| AC4 | Negotiations/offers: all statuses (soft-deleted via toggle); sort/filter; server-side paging; name search per Product | §4.3–§4.6, §5, §9–§11 | **MET** |
| AC5 | Counts: Participants, open negotiations, offers, accepts, declines; no charts; no warehouse | §6; Locked #4 | **MET** |
| AC6 | Not Participant UI; FieldPolicy for Core owner; **one** system superadmin; no parallel multi-user ACL | §3, §5, §7, §8.1 | **MET** |
| AC7 | Required designs: superadmin `io@aiknowhow.com` email+password; bootstrap; TOTP (Spec decides email OTP — Spec locks **OUT**); recovery codes; reset = link + 2FA; Turnstile only ($0); SES; lockout/session; auth + edit/delete audit; confirm-before-delete; delete semantics; lists/search/paging | §4.5–§4.6; §8–§11 | **MET** |
| AC8 | No inbound connector, platform admin, human registration, payments, token allowance | §12 OUT | **MET** |
| AC9 | Soft HOLD build/deploy until harden live QA PASS after app image bake; Soft HOLD invent AWS; PoC $0 | Header; Locked #7; §12 OUT | **MET** |

**Product highlights spot-check:** all-status lists + name search (**MET** §4.5–§4.6); locked auth §8 (**MET** — superadmin, bootstrap, TOTP, no email OTP, recovery codes hashed, Turnstile only $0, SES behind mail interface, lockout/sessions); soft-delete + cascades + confirm-before-delete + audit immutable from admin (**MET** §9–§11); §12 OUT AWS WAF CAPTCHA + reCAPTCHA OUT / Turnstile only (**MET**); soft-deleted toggle superadmin-only + not audited (**MET** §4.5); server-only paging/search + parameterized + FieldPolicy (**MET** §4.5–§4.6); edit before/after FieldPolicy-only + no secrets (**MET** §5, §10).

**Spec vs Product gaps:** none.

---

## 3. Spec vs SA Soft HOLD SoR (reconcile)

Spec **v2.2** adopts SA open picks where Spec left mechanism open; locked Spec values unchanged. Spot-check:

| SA pick / lock | Spec adopt | Result |
|----------------|------------|--------|
| Principal **`CoreOwner`** | §3; §7 | **MET** |
| Host `admin.core.dealoware.com` only | Locked #1; §3 | **MET** |
| Session: server-side Postgres + HttpOnly Secure SameSite=Strict cookie; 30m idle / 8h absolute | §8.7 | **MET** |
| Offset/limit paging default **50** / max **200** | §4.5 | **MET** |
| `Withdrawn` first-class status | §4.5 | **MET** |
| Soft-delete marker **`DeletedAt`**; hard delete deferred | §11.1 | **MET** |
| Stats exclude soft-deleted; open = Status Open + not soft-deleted | §6 | **MET** |
| Confirm modal + API confirmToken (5 min) | §9 | **MET** |
| Same-txn audit pairing; append-only audit table; 4 KiB truncate | §10 | **MET** |
| Soft-deleted toggle CoreOwner-only; not audited | §4.5; §11 | **MET** |
| Case-insensitive **contains** search | §4.6 | **MET** |
| Optimistic concurrency Version/UpdatedAt → 409 | §5 | **MET** |
| Turnstile only; SES behind mail interface; no email OTP; lockout 5/15→30 + IP 20/15→30 | §8 | **MET** |
| CFO Soft HOLD cost cite | §8.9; Sources | **MET** |
| CA grounding PASS + Arch QA interim/formal cites | Sources | **MET** |

**Chief Spec decision (new in v2.2):** auth audit IP = keyed **HMAC-SHA256** only; raw IP only in short-lived rate-limit counters; unkeyed hash forbidden (§8.6, §10, §13 pt 6). Spec notes SA should mirror in a later Soft HOLD SoR touch. **Not a Spec bounce** — Spec binds the decision; SA lag is forward Soft HOLD for SA, not a Spec↔Product contradiction.

**Spec vs SA lock conflicts:** none found.

**Soft notes (not bounces):**
- **S1:** Spec §7 still says “SA to confirm … after Spec QA” for Option A carry-forward; Sources/done-list already cite cleared SA Soft HOLD SoR + CA PASS. Wording lag only.
- **S2:** Auth audit IP HMAC is Spec-bound ahead of SA Soft HOLD SoR body — Spec explicitly defers SA mirror.
- **S3:** Prior Spec Security qa-confirm (v1 10/10) and prior Spec QA PASS (~1:15pm) are superseded history; live gate is this formal PASS on v2.2.

---

## 4. §13 Security checklist v2 pts 1–15 binding

| # | Point | Spec §13 | Spec QA | Security QA Soft HOLD SoR qa-confirm |
|---|-------|----------|---------------------------|
| 1 | Host + single superadmin role | MET | **MET** | **MET** |
| 2 | Email + password; secure one-time bootstrap | MET | **MET** | **MET** |
| 3 | TOTP 2FA required; no email OTP fallback | MET | **MET** | **MET** |
| 4 | Password reset = email link + 2FA | MET | **MET** | **MET** |
| 5 | CAPTCHA (Turnstile only) + lockout / rate limit + session | MET | **MET** | **MET** |
| 6 | Login / reset audit + edit/delete audit (+ keyed HMAC IP) | MET | **MET** | **MET** |
| 7 | Fail-closed deny for non-superadmin | MET | **MET** | **MET** |
| 8 | FieldPolicy only; no parallel multi-user admin ACL | MET | **MET** | **MET** |
| 9 | Confirm before every delete | MET | **MET** | **MET** |
| 10 | Soft-delete + cascades; soft-deleted toggle (superadmin-only) | MET | **MET** | **MET** |
| 11 | All-status lists + name search + paging (safe) | MET | **MET** | **MET** |
| 12 | Edit write surface FieldPolicy-only | MET | **MET** | **MET** |
| 13 | SES mail + Turnstile as dependency + cost only | MET | **MET** | **MET** |
| 14 | OUT / Soft HOLD pack | MET | **MET** | **MET** |
| 15 | Traceability + re-QA handshake | MET | **MET** (handshake satisfied — Security QA Soft HOLD SoR v2 PASS) | **MET** |

**§13 bound:** **Yes** (v2 pts 1–15). Prior v1 1–10 numbering removed from Spec §13. Spot-check §13 vs Security QA Soft HOLD SoR qa-confirm pts 1–15: point titles + MET claims align 15/15; hard guardrails (§8.6 raw IP only in short-lived counters; §10 keyed HMAC-SHA256 auth-audit IP; Turnstile only; SES behind mail interface; no password values) **Held**. No mismatch.

---

## 5. Security QA v2 status

| Item | Status |
|------|--------|
| Checklist v2 ISSUED | Yes |
| Spec §13 rebound to v2 1–15 | Yes (v2.2) |
| Senior Security points-review | v2.2 FINAL 15/15 MET — cited, not Soft HOLD SoR |
| Security QA Soft HOLD SoR qa-confirm for checklist **v2** | **PASS 15/15 MET** (~1:53pm ET), sha256 MATCH — **Security Soft HOLD SoR CLEARED** |
| Prior `…spec-qa-confirm.md` v1 10/10 (~1:15pm) | Superseded (overwritten by v2 Soft HOLD SoR) |

---

## 6. Soft HOLD holds (remain after PASS)

- Soft HOLD invent Stories / build until **Dev Plan + Test design PASS**
- Soft HOLD invent passwords — **no password values** in Spec/docs/chat/commit history
- Soft HOLD invent AWS (account IDs / region / SES identity / Turnstile account / provision)
- Soft HOLD deploy
- Soft HOLD **A7 until H4 live PASS**
- Soft HOLD code / CDK / spend / provision
- PoC **$0**; cost/critical → COO → CEO
- **Not a build unlock**

---

## 7. Triad / next

- **Formal PASS** Spec gate on Core admin Spec v2.2 → report PASS path to Chief Spec, COO, and CPM.
- Next on track: Dev Plan + Test design (Stories/build stay Soft HOLD until both PASS).
- Spec not edited. No PR. No merge. PoC $0.
