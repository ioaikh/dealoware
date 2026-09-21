# Security QA — MVP Stage A #32 Account list fail-closed Product QA vs Chief Security Product QA checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 Product-QA-step Security points MET)  
**Asked by:** Dealoware QA / Bot Manager — Product QA Security handshake (PRIORITY — Senior Product QA HOLD)  
**Product QA report:** `qa/2026-09-21__qa__qa-report__mvp-stage-a-account-list-fail-closed.md` (HOLD PASS pending this confirm; pts 1–9 EVIDENCED; pt 10 handshake open)  
**Chief checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-checklist.md` (10 points)  
**Prior SD Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-qa-confirm.md` (PASS 10/10 @ PR HEAD `cf5b0bf…`)  
**Senior Security productqa-points-review:** **not present** under `verification/*list-failclosed-productqa-points*` — optional; independent re-score without it  
**Format ref:** `verification/2026-09-20__security__verification__poc-identity-seal-productqa-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/34 (**MERGED**)  
**PR HEAD:** `cf5b0bf400501dea9eafd237d7573656b559d918`  
**main merge commit:** `3812525301fe9767f0678736843d82a693f992c8`  
**MergedAt:** 2026-09-21T23:45:32Z (≈ 2026-09-21 19:45 EDT)  
**CI (main @ merge):** https://github.com/ioaikh/dealoware/actions/runs/35669004132 — **SUCCESS** (per Product QA report + context)  
**Tests:** `tests/Dealoware.Api.Tests/StageAFailClosedTests.cs` (30 Facts)  
**Issue:** https://github.com/ioaikh/dealoware/issues/32 · Account list fail-closed (negotiations / offers / artifacts)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #31 Field ACL — **not scored / not confirmed here** (keep separate; SD confirm PAUSED)  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-qa-confirm.md`  
**Constraints:** Stage A only; Stage B/C + gate #24 OUT; PoC **$0**; no Cognito/MM/DC4; #7 stub unchanged; never skip Chief. Soft: no live `dotnet test` on this box — CI SUCCESS + `StageAFailClosedTests` inventory + prior SD Security PASS (same pattern as prior PoC Product QA).

## Soft gaps accepted (non-blocking)

| Soft gap | Disposition |
|----------|-------------|
| No live `dotnet restore\|build\|test` on evidence box | **Accepted** — static/CI evidence: main `38125253…` + PR #34 MERGED + `StageAFailClosedTests` (30 Facts) + CI run 35669004132 SUCCESS + prior SD Security PASS on same PR HEAD `cf5b0bf…` (same pattern as identity-seal / prior PoC Product QA) |
| Named IDOR Facts use random GUIDs | **Accepted** — Product QA soft note; cross-tenant stranger cases cover foreign-owned IDs; non-blocking |
| Residual unscoped `GetByIdAsync` on `CreateNegotiation` | **Accepted** — outside list+get #32 surface; cite-only soft observe (aligns SD Security PASS soft notes) |
| Senior productqa-points-review absent | **Accepted** — optional; independent Security QA re-score completes handshake |

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security Product QA checklist | `…list-failclosed-productqa-checklist.md` | Binding 10 points |
| Product QA report | `qa/2026-09-21__qa__qa-report__mvp-stage-a-account-list-fail-closed.md` | HOLD PASS; AC 1–5 MET; Sec 1–9 EVIDENCED; pt 10 HOLD |
| Prior SD Security PASS | `…list-failclosed-sd-qa-confirm.md` | PASS 10/10 — unlock context |
| PR #34 | MERGED @ main `38125253…` / HEAD `cf5b0bf…` | Verified via `gh pr view 34` |
| CI | Actions run 35669004132 | SUCCESS @ merge (Product QA + context) |
| Optional productqa-points-review | `verification/*list-failclosed-productqa-points*` | **Not present** — optional |

## Independent re-score (Security QA)

| # | Point | Product QA | Security QA | Evidence |
|---|-------|------------|-------------|----------|
| 1 | Authn fail-closed — Neg/Offer/Artifact list+get reject unauthenticated (401/403); no private-field leak on deny | EVIDENCED | **MET** | `*_UnauthDeny_Returns401` + `*_InvalidAuth_Returns401` for Artifact/Neg/Offer; Health open (`Health_NoAuth_Returns200`). Aligns SD PASS #1 / endpoints `AuthHelper.GetAuthenticatedSub` → Unauthorized |
| 2 | Negotiation list/get isolation — owner/party only; unauthorized → fail-closed (404 preferred); no cross-account leak | EVIDENCED | **MET** | Party OK + stranger empty list + stranger/IDOR get 404 NoPrivateFields + `Negotiation_GetByIdForParty_QueryPlaneFilter_*`. SD PASS #2 |
| 3 | Offer list/get isolation — party via parent negotiation; same fail-closed (+ accept/decline/counter stranger deny) | EVIDENCED | **MET** | Party via parent; stranger empty/404; `Offer_AcceptDeclineCounter_StrangerDeny_Returns404`; query-plane Fact. SD PASS #3 |
| 4 | Artifact list/get isolation — `OwnerParticipantId` == sub; IDOR fail-closed; align #4 | EVIDENCED | **MET** | Owner OK; stranger empty/404; IDOR; `Artifact_GetByIdForOwner_QueryPlaneFilter_*`. SD PASS #4 |
| 5 | Deny-body / empty-list hygiene — errors and empty lists do not leak other Participants’ private fields; stranger list → `[]` | EVIDENCED | **MET** | Stranger list → empty not foreign rows; stranger get bodies assert no Owner/Party/Artifact private fields. SD PASS #5 |
| 6 | Complement #31; don’t weaken party rules — list isolation stays owner/party query-plane; #31 Field ACL not delivered by this Story | EVIDENCED | **MET** | PR scoped to list+get fail-closed; no Field ACL invent; #31 OUT/separate. **#31 not confirmed here.** SD PASS #6 |
| 7 | No Stage B/C inventing — no Strategy list ACL, agent hard-wall, Cognito, or MM/DC4 | EVIDENCED | **MET** | Product QA scope + SD PASS: no Strategy ACL / Cognito / MM-DC4 / agent hard-wall. Stage B/C + gate #24 OUT |
| 8 | Cross-story non-merge — harden/consume #4–#7; do not rewrite #31 or PoC Stories | EVIDENCED | **MET** | Hardens #4–#7 surfaces only; does not rewrite #31. Sibling kept separate. SD PASS #8 |
| 9 | Cost / spend — PoC **$0**; no IdP/vault provision | EVIDENCED | **MET** | No IdP/vault/AWS provision; local #5 auth only. PoC $0. SD PASS #9 |
| 10 | Handshake close — Product QA must **not** PASS until Security QA confirms | HOLD (confirm missing) | **MET** | This confirm closes Product QA Security gate. Product QA may clear HOLD → PASS after this file. **Do not treat as #31 confirm.** |

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Fail-closed list+get (401 unauth / 404 stranger) | Held (`StageAFailClosedTests` + SD PASS) |
| Owner/party isolation query-plane | Held (GetByIdForOwner / GetByIdForParty / GetByParticipant) |
| No private leak in deny/empty | Held (NoPrivateFields + EmptyListNotForeignRows) |
| Stage A only; Stage B/C + #24 OUT | Held |
| #31 separate — not scored / not confirmed here | Held |
| PoC $0; no Cognito/MM/DC4 | Held |
| Soft gaps = static/CI + prior SD Security PASS | Held (same pattern as identity-seal Product QA) |

## Alignment with Product QA report

Product QA scored pts **1–9 EVIDENCED** on main `38125253…` / PR HEAD `cf5b0bf…` with AC 1–5 **MET**, CI SUCCESS run 35669004132, and correctly **HOLD** on pt **10** until this confirm. Independent Security QA re-score **agrees** on 1–9; pt 10 now **MET** by this file. Soft gaps disclosed in Product QA report are **accepted**. Prior SD Security PASS (10/10) reinforces without re-litigating Code QA.

## Gaps

**None.** Soft gaps (no live dotnet; IDOR random GUIDs; CreateNegotiation GetByIdAsync observe; optional points-review absent) non-blocking per Soft gaps accepted.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Dealoware QA / Senior Product QA may clear HOLD and PASS Product QA on Security gate. Cost/critical: none. No AWS/IdP/vault spend. Do **not** open gate #24. Do **not** confirm #31 (Field ACL remains separate / PAUSED).
