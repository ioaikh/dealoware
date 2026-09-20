# BA business verification — Story #6 1:1 Negotiation + Offers (D7–D9, P4 + D10)

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-20 (BA verify executed ~11:10 AM ET)  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/6  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/15 (MERGED @ `08639c983de4229958b21cfd827f198fd6d3250d`; SD HEAD `d549958ae7c5b0450b989ee7d744a73d07d51623`)  
**Docs PR:** https://github.com/ioaikh/dealoware/pull/16 (OPEN — soft gap OK)  
**Method:** Business-intent AC check from Story #6 body + CPM/PM lock comments + merged PR #15 evidence on main + KB Product QA / Security / Doc / Spec / DevPlan / SD (not code review). No invented requirements. PoC $0. No MotorMarket.

## Binding AC (Story #6 — do not invent)

- **D7** 1:1 Negotiation + place Offer (complementary intents)
- **D8** Accept / Decline / Counter
- **D9** Close Negotiation cancels all open offers
- **D10** Negotiation expiration (thin OK in PoC)
- **P4** participant offer-cycle start (place / accept / decline / counter path)
- Strictly **1:1** (no multi-party / multi-Artifact)

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | D7 1:1 Negotiation + place Offer (complementary intents) | **PASS** | Main: `POST /negotiations` + `POST /negotiations/{id}/offers` (`NegotiationEndpoints`); `Negotiation.Create` + `IntentComplement` (buy↔sell, provide↔consume, rent↔rent); non-complementary / same-party → 400; Product QA AC table + SD Steps 3–4/7–8 PASS @ merge `08639c98…`. |
| 2 | D8 Accept / Decline / Counter | **PASS** | Main: `OfferEndpoints` accept/decline/counter; recipient-only (`toParticipantId`); Accept cancels other opens; Counter supersedes prior; Facts Accept*/Decline*/Counter* (Product QA + SD Steps 9–11). |
| 3 | D9 Close cancels all open offers | **PASS** | Main: `POST /negotiations/{id}/close`; domain `Close()` → `CancelAllOpenOffers()`; endpoint cancels via `GetOpenByNegotiationAsync`; post-Close mutations → 409; `CloseNegotiation_CancelsAllOpenOffers` + PostClose Facts. |
| 4 | D10 Negotiation expiration (thin OK) | **PASS** | Main: `CheckAndApplyExpiration` → Expired + cancel opens; place/accept/decline/counter/close after expiry → 409; party GET OK; `Expiration_*` Facts; README D10 section. |
| 5 | P4 participant offer-cycle start | **PASS** | Place / accept / decline / counter path on main + README exercise flow; covered by endpoint Facts (Product QA P4 PASS; SD Step 14). |
| 6 | Strictly 1:1 (no multi-party / multi-Artifact) | **PASS** | Create invariants: exactly two distinct parties + one ArtifactId; reject self-negotiate / non-complementary; no multi-party or multi-Artifact API surface in PR #15 file set; Product QA / Security pts 3 MET. |

## Out of scope held

| OOS item (Story #6 + CPM lock) | Held? | Evidence |
|--------------------------------|-------|----------|
| Strategy engine / AI Assistant | **Yes** | Not in PR #15 file set / Program routes; Product QA OUT scan; CQ OUT honored |
| Contact exchange on Accept (P7/A9 → MVP; #7 backlog) | **Yes** | `OfferResponse` state-only (id/status/amount/currency/terms — no contact/email/phone); README Accept defers #7; Accept Facts assert no contact; Security ProductQA/Doc pt 8 MET |
| Multi-party / multi-Artifact (A13 → V2) | **Yes** | Strict 1:1 create only; tree/API surface has no multi-party/multi-Artifact; Security pt 3 MET |
| Settlement / checkout | **Yes** | No settlement paths in main tree scan / PR #15; Spec/DevPlan/SD OUT |
| MotorMarket / DC4 | **Yes** | `gh` recursive tree scan empty for motormarket; PR OUT; Security pt 9 MET |
| Cognito / SSO | **Yes** | Reuses #5 header auth; no Cognito/SSO packages or endpoints; PoC local/$0 |
| #7–#8 unlock | **Yes** | CPM/PM/CBA comments hold backlog; Accept has no contact productization; no #8 surface |

## Soft gaps (non-blocking for BA business verify)

- No live `dotnet restore|build|test` or `curl` on evidence box (same soft gap as Product QA / Security — accepted; static + test inventory via `gh`).
- No CI checks reported on PR #15 branch (SD soft note).
- Docs PR #16 still **OPEN** at verify time; impl PR #15 **MERGED** (soft gap OK per CBA / task brief).
- Top-of-README product vision (“Contact stays protected until accept”) ≠ PoC Accept API claiming contact delivery — Negotiation section explicitly defers contact to #7 (Doc Security soft note accepted).

## Prior gate chain (evidence, not re-scored here)

| Gate | Result | Path / link |
|------|--------|-------------|
| Spec QA | PASS (Spec-side; Security closed) | `verification/2026-09-20__spec__verification__poc-negotiation-offers-d7-d10.md` + Spec Security confirm |
| Dev Plan QA | PASS | `verification/2026-09-20__devplan__verification__poc-negotiation-offers-d7-d10.md` |
| SD / Dev Code QA | PASS | `verification/2026-09-20__sd__verification__poc-negotiation-offers-d7-d10.md` |
| SD Security | PASS (1–10 MET) | `verification/2026-09-20__security__verification__poc-negotiation-sd-qa-confirm.md` |
| CQ | `cq:no-refactor` | Issue #6 CQ comment; label `cq:no-refactor` |
| Product QA | PASS | `qa/2026-09-20__qa__qa-report__poc-negotiation-offers-d7-d10.md` |
| Product QA Security | PASS (1–10 MET) | `verification/2026-09-20__security__verification__poc-negotiation-productqa-qa-confirm.md` |
| Doc Security weave | PASS | `ops/2026-09-20__docs__ops__poc-negotiation-doc-security-weave.md` |
| Doc Security QA | PASS (1–10 MET) | `verification/2026-09-20__security__verification__poc-negotiation-doc-qa-confirm.md` |

## Recommendation to CBA

**PASS** — deliverable meets Story #6 business AC (D7–D10, P4, strictly 1:1), and OOS held (Strategy/AI; contact-on-Accept/#7; multi-party; settlement; MotorMarket; Cognito/SSO; #7–#8 backlog). Hand to BAQA for verify-QA; eng `done` HOLD until CBA final confirm after BAQA. Do not unlock #7–#8 from this step. PoC $0.
