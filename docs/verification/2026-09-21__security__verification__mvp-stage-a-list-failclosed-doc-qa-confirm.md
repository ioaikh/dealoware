# Security QA — MVP Stage A #32 Account list fail-closed Doc vs Chief Security Doc checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 Doc-step Security points MET)  
**Asked by:** Dealoware Docs / Bot Manager — Doc Security handshake (PRIORITY — Docs QA HOLD until confirm)  
**Senior Security done-list:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-points-review.md` (**PASS** 10/10)  
**Doc weave (locked):** `ops/2026-09-21__docs__ops__mvp-stage-a-list-failclosed-doc-security-weave.md`  
**Chief checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-checklist.md` (10 points)  
**Primary Doc surfaces:** PR #34 MERGED README/API Authn + Artifact/Negotiation/Offer fail-closed + locked Doc weave + INDEX #32 annotations  
**Optional Doc mirror:** https://github.com/ioaikh/dealoware/pull/36 @ `015fcfacdb45bd292239f5f530e9d67b1f66314a` (OPEN — cite only; score on KB weave + main README + INDEX)  
**Product QA Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/32 · Account list fail-closed (negotiations / offers / artifacts)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #31 Field ACL — **OUT / not scored / not confirmed here** (complement / cross-ref only)  
**PR (primary):** https://github.com/ioaikh/dealoware/pull/34 (**MERGED**)  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-qa-confirm.md`  
**Constraints:** Stage A only; Stage B/C + gate #24 HOLD; PoC **$0**; no Cognito/MM/DC4; #7 stub unchanged; never skip Chief. Soft: README Neg/Offer **list** curl sections thinner than weave — non-blocking (Senior soft note accepted).

## Soft notes accepted (non-blocking)

| Soft note | Disposition |
|-----------|-------------|
| README strongly documents Artifact list/get and Negotiation/Offer **get** + mutation party rules; explicit `GET /negotiations` and `GET /offers` **list** curl sections thinner than weave/Spec | **Accepted** — weave §pts 2–3 + Spec/DevPlan/SD + Product QA Sec confirm + INDEX carry list isolation for Doc score; optional README polish later; **not a HOLD** (aligns Senior Doc points-review soft note) |
| Mirror PR #36 OPEN @ `015fcfa…` | **Accepted** — cite only; score on locked KB weave + main README + INDEX (same pattern as prior PoC Doc QA) |
| No live `dotnet test` on this box for Doc step | **Accepted** — Doc score is documentation surfaces; Product QA Sec PASS + `StageAFailClosedTests` cites in weave are supporting context only |

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security Doc checklist | `…list-failclosed-doc-checklist.md` | Binding 10 points |
| Doc Security weave (locked) | `ops/…list-failclosed-doc-security-weave.md` | HOLD overall Doc until this confirm; pts 1–10 mapped |
| Senior Doc points-review | `…list-failclosed-doc-points-review.md` | **PASS** 10/10 — present; aligned |
| Product QA Security PASS | `…list-failclosed-productqa-qa-confirm.md` | Supporting (Sec10 Product QA closed; not overall Doc PASS) |
| PR #34 | MERGED — README/API fail-closed surface | Primary code Doc surface |
| Mirror PR #36 | OPEN @ `015fcfa…` | Cite only |
| INDEX.md | #32 Doc checklist + weave HOLD indexed | MATCH |

## Independent re-score (Security QA)

| # | Point | Senior | Security QA | Evidence |
|---|-------|--------|-------------|---------|
| 1 | Authn fail-closed — Neg/Offer/Artifact list+get require #5 principal; unauth → 401/403; no private-field leak on deny | MET | **MET** | Weave §pt1; README Artifact + Negotiation/Offer: valid `Authorization` required (fail-closed); unauth → 401. Product QA Sec confirm MET pt1 (supporting). |
| 2 | Negotiation list/get isolation — owner/party only; unauthorized → fail-closed (404 preferred); no cross-account leak | MET | **MET** | Weave §pt2: `GetByParticipantAsync` / party-scoped get → non-party 404. README Get Negotiation: 404 if not party (no leak). Spec/DevPlan/SD + Product QA Sec pt2 carry list isolation (README list curl soft — accepted). |
| 3 | Offer list/get isolation — party via parent negotiation; same fail-closed (+ stranger deny on mutations if documented) | MET | **MET** | Weave §pt3: party via parent negotiation; accept/decline/counter stranger deny. README: non-party 404; mutation recipients only. Product QA Sec pt3. Soft list-curl polish OK. |
| 4 | Artifact list/get isolation — `OwnerParticipantId` == sub; IDOR fail-closed; align #4 | MET | **MET** | Weave §pt4: `GetByOwnerAsync` / `GetByIdForOwnerAsync`; IDOR → 404; align #4. README List Own Artifacts + Get: owned only / 404 not owned (no cross-owner leak). Product QA Sec pt4. |
| 5 | Deny-body / empty-list hygiene — errors and empty lists do not leak other Participants’ private fields; stranger list → `[]` | MET | **MET** | Weave §pt5 + `StageAFailClosedTests.cs` cites: empty → `[]`; deny bodies omit private fields. README: empty-if-none + no cross-owner leak; Negotiation/Offer 404 no information leak. Product QA Sec pt5. |
| 6 | Complement #31; don’t replace party rules — list isolation remains owner/party query-plane; Field ACL not delivered as this Story | MET | **MET** | Weave Explicit separations + §pt6: #32 = owner/party **query-plane**; **#31 Field ACL OUT** of this Story/Doc track; INDEX soft parallel only. **#31 not confirmed here.** Product QA Sec pt6. |
| 7 | No Stage B/C inventing — no Strategy list ACL, agent hard-wall, Cognito, or MM/DC4 as delivered | MET | **MET** | Weave Constraints + §pt7: Stage B/C HOLD; gate #24 HOLD; no Strategy list ACL / agent hard-wall / Cognito/SSO / MM/DC4 as #32 delivered. README OOS Cognito/SSO/MM/DC4. |
| 8 | Cross-story non-merge — harden/consume #4–#7; do not rewrite #31 or PoC Stories | MET | **MET** | Weave + INDEX: #32 Doc track separate from #31; harden/consume cross-refs to #4–#7 only; #7 stub unchanged; no rewrite of #31 deliverables under this weave. |
| 9 | Cost / spend — PoC **$0**; no IdP/vault provision as delivered | MET | **MET** | Weave Constraints + §pt9: PoC $0; no IdP/vault as delivered. README: PoC stays local/$0; no Cognito/SSO. |
| 10 | Handshake close — Docs QA must **not** PASS until Security QA confirms | MET | **MET** | Weave correctly HOLDs overall Doc PASS until this confirm. This file closes Doc Security gate. Docs QA may clear HOLD → overall Doc PASS. **Do not treat as #31 confirm.** |

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Authn fail-closed documented (401 unauth) | Held (README + weave) |
| Owner/party isolation documented (404 stranger / owned-only list) | Held (README get + Artifact list; weave/Spec for Neg/Offer list) |
| Deny-body / empty-list hygiene | Held (weave + README empty-if-none / no leak) |
| #31 OUT — complement only; not scored / not confirmed | Held |
| Stage A only; Stage B/C + #24 HOLD | Held |
| PoC $0; no Cognito/MM/DC4 | Held |
| Soft README Neg/Offer list curl polish = non-blocking | Held (Senior + Security QA agree) |

## Alignment with Senior Security done-list

Senior Doc points-review scored pts **1–10 MET** on locked weave + checklist + README/INDEX with soft note on thinner Neg/Offer list curls. Independent Security QA re-score **agrees** on all 10; soft note **accepted**. No bounce. Senior catch-up was present at confirm time — **aligned**.

## Gaps

**None.** Soft notes (README Neg/Offer list curl polish; PR #36 OPEN cite-only; no live dotnet for Doc step) non-blocking. **#31 OUT.**

## Handshake status

Security QA → **PASS** confirm to Chief Security. Dealoware Docs / Senior Docs / Docs QA may clear overall Doc-step #32 HOLD on Security gate (clear weave HOLD). Cost/critical: none. No AWS/IdP/vault spend. Do **not** open gate #24. Do **not** confirm #31 (Field ACL remains separate).
