# Doc step weave — Security checklist pts 1–10 · PoC Negotiation + Offers D7–D10/P4 (#6)

| Field | Value |
|-------|-------|
| **Status** | **Overall Doc step #6 PASS** — Security QA cleared HOLD (doc-qa-confirm; all 10 MET) |
| **Date** | 2026-09-20 |
| **Author** | Dealoware Senior Docs |
| **Story** | GitHub issue #6 · 1:1 Negotiation + Offers (D7–D9, P4 + D10) |
| **Issue** | https://github.com/ioaikh/dealoware/issues/6 |
| **PR** | https://github.com/ioaikh/dealoware/pull/15 (MERGED) — README Negotiation API (D7–D10) = primary Doc surface for Security score |
| **Checklist** | `verification/2026-09-20__security__verification__poc-negotiation-doc-checklist.md` |
| **DOC-FLOW** | `ops/2026-09-20__docs__ops__poc-negotiation-doc-security-weave.md` |
| **Constraints** | PoC $0 · CQ no-refactor · keep separate from #4 Artifact and #5 auth · #7–#8 backlog · no inventing · no MotorMarket · no contact-on-Accept |

## Primary surfaces Security will score
1. Merged PR #15 README — **Negotiation API (PoC - D7-D10)** section (auth fail-closed, party-only 404, 1:1, complementary intents, offer lifecycle, Close cancels opens, D10 expiry, Authorization header examples)
2. This Doc weave (`ops/2026-09-20__docs__ops__poc-negotiation-doc-security-weave.md`) + living `INDEX.md` annotations

Supporting (not primary score surface): KB `qa/2026-09-20__qa__qa-report__poc-negotiation-offers-d7-d10.md`; Product QA Security PASS `verification/2026-09-20__security__verification__poc-negotiation-productqa-qa-confirm.md`.

## Point → how Doc artifacts satisfy

Mapped strictly to `verification/2026-09-20__security__verification__poc-negotiation-doc-checklist.md`.

| # | Security point | Doc satisfaction (cites) |
|---|----------------|--------------------------|
| 1 | Authn fail-closed | README: all negotiation/offer endpoints require valid `Authorization` header (fail-closed). Weave restates. Product QA Sec confirm MET pt1. |
| 2 | Party-only | README: non-party requests return 404 (no information leak); GET returns 404 if not a party. Weave restates. Product QA Sec confirm MET pt2. |
| 3 | Strictly 1:1 | README: exactly two parties (partyA/partyB) + exactly one Artifact. Docs do not describe multi-party / multi-Artifact as PoC delivered. Product QA Sec confirm MET pt3. |
| 4 | Complementary intents | README create table (sell/buy, provide/consume, etc.); non-complementary rejected in product. Weave notes complementary-intent requirement for start. Product QA Sec confirm MET pt4. |
| 5 | Offer lifecycle | README place / Accept / Decline / Counter: recipient-only (`toParticipantId`); one-open-per-side → 400; Closed/Expired → 409. Weave restates. Product QA Sec confirm MET pt5. |
| 6 | Close cancels opens | README Close: ALL open offers → Cancelled; post-close mutations → 409. Weave restates. Product QA Sec confirm MET pt6. |
| 7 | D10 expiration | README D10: `endsAt` → Expired; open offers Cancelled; post-expiry writes → 409; GET still works for party. Weave restates. Product QA Sec confirm MET pt7. |
| 8 | No contact/PII on Accept | README Accept returns offer status **Accepted** (state only). Docs explicitly defer contact exchange / identity reveal to **#7 backlog**; no contact-on-Accept as delivered. #8 backlog. Product QA Sec confirm MET pt8. |
| 9 | Secrets / host / OUT | README uses placeholder ApiKey examples (`dlw_AbCdEfGh_...`); `Authorization` header only (not query/body); localhost PoC; no Cognito/SSO/settlement/MM how-tos as delivered. ECS Express = host sketch only. Product QA Sec confirm MET pt9. |
| 10 | Handshake close | Handshake indexed: checklist + points-review + doc-qa-confirm. Security QA PASS 10/10 — Docs QA may overall-PASS. |

## Indexed Product QA Security (Sec10 for Product QA step — already closed)
- `verification/2026-09-20__security__verification__poc-negotiation-productqa-checklist.md`
- `verification/2026-09-20__security__verification__poc-negotiation-productqa-qa-confirm.md` (PASS; all 10 MET)

## PASS
**Overall Doc-step #6 PASS** — Security QA confirm `verification/2026-09-20__security__verification__poc-negotiation-doc-qa-confirm.md` (pts 1–10 MET); Senior Security points-review `verification/2026-09-20__security__verification__poc-negotiation-doc-points-review.md` (PASS 10/10). Mirror: https://github.com/ioaikh/dealoware/pull/16. Keep separate from #4 Artifact and #5 auth. No contact-on-Accept; #7–#8 backlog; PoC $0.
