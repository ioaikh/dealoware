# Verification — Security points vs PoC Negotiation Doc (#6)

**Author:** Dealoware Senior Security  
**Date:** 2026-09-20  
**Verdict:** **PASS** (all 10 Doc-step Security points MET)  
**Checklist (binding):** `verification/2026-09-20__security__verification__poc-negotiation-doc-checklist.md` (10 points)  
**Doc surfaces:**  
- Weave: `ops/2026-09-20__docs__ops__poc-negotiation-doc-security-weave.md` (overall Doc PASS HOLD until Security QA)  
- README (main): **Negotiation API (PoC - D7-D10)** — https://github.com/ioaikh/dealoware/blob/main/README.md  
- INDEX.md (doc checklist + weave indexed HOLD)  
**Optional Doc mirror:** https://github.com/ioaikh/dealoware/pull/16 (OPEN — optional cite; score on KB weave + main README)  
**Product QA Security PASS:** `verification/2026-09-20__security__verification__poc-negotiation-productqa-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/15 (MERGED)  
**Issue:** https://github.com/ioaikh/dealoware/issues/6  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-negotiation-doc-points-review.md`  
**Constraints:** PoC $0; no Cognito/SSO; #7–#8 backlog; no contact-on-Accept; keep separate from #4/#5 Doc Security. Reviewed via `gh` main README + KB weave/INDEX.

## Sources checked

| Source | Path / link | Result |
|--------|-------------|--------|
| Doc Security checklist (Chief) | `…poc-negotiation-doc-checklist.md` | Binding 10 |
| Doc Security weave | `ops/…poc-negotiation-doc-security-weave.md` | Pts 1–10 mapped; Doc PASS HOLD |
| README main Negotiation API | D7–D10 section + error table | Primary Doc surface |
| INDEX.md | Doc checklist + weave HOLD annotations | Index hygiene |
| Optional PR #16 | docs mirror | Optional; no verdict change |
| Product QA Security PASS | `…productqa-qa-confirm.md` | Prior step clear |

## Checklist vs Doc surface

| # | Security point | Result | Evidence |
|---|----------------|--------|----------|
| 1 | **Authn fail-closed** | **MET** | README: all negotiation/offer endpoints require Authorization (fail-closed); Error Responses **401**. Weave pt1. |
| 2 | **Party-only** | **MET** | README: non-party → **404** (no leak); GET 404 if not party. Weave pt2. |
| 3 | **Strictly 1:1** | **MET** | README: exactly two parties + one Artifact; no multi-party/multi-Artifact as delivered. Weave pt3. |
| 4 | **Complementary intents** | **MET** | README create + complementary intent pairs table; 400 for non-complementary. Weave pt4. |
| 5 | **Offer lifecycle** | **MET** | README place/Accept/Decline/Counter: recipient-only; one-open-per-side → 400; Closed/Expired → 409. Weave pt5. |
| 6 | **Close cancels opens** | **MET** | README Close: ALL open offers → Cancelled; post-close mutations → 409. Weave pt6. |
| 7 | **D10 expiration** | **MET** | README D10: endsAt → Expired; opens Cancelled; post-expiry writes → 409; GET OK for party. Weave pt7. |
| 8 | **No contact/PII on Accept** | **MET** | README Accept: status Accepted only; **NO contact/PII** — defer to **#7**; #8 backlog. Weave pt8. |
| 9 | **Secrets / host / OUT** | **MET** | Placeholder ApiKey examples; Authorization header only; localhost PoC; ECS Express sketch; no Cognito/SSO/settlement/MM how-tos. Weave pt9. Soft: marketing blurb “Contact stays protected until accept” is product vision, not PoC Accept API claiming contact delivery. |
| 10 | **Handshake close** | **MET** | Weave HOLD until Security QA; this done-list → Security QA. **Docs QA must not overall-PASS until Security QA confirms.** |

## Soft notes (non-blocking)

- Optional PR #16 mirror OPEN — cite only; score remains KB + main README.
- Top-of-README product vision line about contact-until-accept does not contradict PoC Accept = state-only (explicitly deferred to #7 in Negotiation API section).

## Gaps for Senior Docs

**None** blocking.

## Done-list (for Security QA)

- [x] DOC-FLOW: `verification/2026-09-20__security__verification__poc-negotiation-doc-points-review.md`
- [x] All 10 checklist points scored with README + weave + INDEX evidence
- [x] Kept separate from #4/#5 Doc reviews; #7–#8 not invented
- [x] Security QA: confirm **PASS** (`verification/2026-09-20__security__verification__poc-negotiation-doc-qa-confirm.md`) — Docs QA may clear HOLD; DOC-FLOW closed

## Cost/critical

None. No Cognito/IdP spend. No escalate.
