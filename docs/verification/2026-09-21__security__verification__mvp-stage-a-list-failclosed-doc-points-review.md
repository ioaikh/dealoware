# Verification — Security points vs MVP Stage A #32 Account list fail-closed Doc

**Author:** Dealoware Senior Security  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 Doc-step Security points MET)  
**Checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-checklist.md` (10 points)  
**Weave (locked):** `ops/2026-09-21__docs__ops__mvp-stage-a-list-failclosed-doc-security-weave.md`  
**INDEX:** living INDEX.md — #32 paths MATCH (Chief Docs: 24)  
**Mirror:** https://github.com/ioaikh/dealoware/pull/36 @ `015fcfa…` (OPEN)  
**Primary code Doc surface:** PR #34 MERGED README/API Authn + Artifact/Negotiation/Offer fail-closed  
**Product QA Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/32  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-points-review.md`  
**Constraints:** #31 OUT of this Doc score; Stage B/C + gate #24 HOLD; #7 stub; PoC $0; no Cognito/MM/DC4.

## Checklist vs Doc surfaces

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Authn fail-closed | **MET** | Weave §pt1; README Artifact + Negotiation/Offer: valid Authorization required; unauth → 401; fail-closed wording. Supporting Product QA Sec confirm pt1. |
| 2 | Negotiation list/get isolation | **MET** | Weave §pt2: `GetByParticipantAsync` / party-scoped get → non-party 404. README Get Negotiation: 404 if not party (no leak). Spec/DevPlan/SD + Product QA Sec pt2. |
| 3 | Offer list/get isolation | **MET** | Weave §pt3: party via parent negotiation; accept/decline/counter stranger deny. README: non-party 404; mutation recipients only. Product QA Sec pt3. |
| 4 | Artifact list/get isolation | **MET** | Weave §pt4: `GetByOwnerAsync` / `GetByIdForOwnerAsync`; IDOR → 404; align #4. README List Own + Get: owned only / 404 not owned. Product QA Sec pt4. |
| 5 | Deny-body / empty-list hygiene | **MET** | Weave §pt5 + `StageAFailClosedTests.cs` cites: empty → `[]`; deny bodies omit other Participants’ private fields. README empty-if-none + no cross-owner leak. Product QA Sec pt5. |
| 6 | Complement #31; don’t replace party rules | **MET** | Weave Explicit separations + §pt6: #32 = owner/party query-plane; **#31 Field ACL OUT** of this Story/Doc track; INDEX soft parallel only. Product QA Sec pt6. |
| 7 | No Stage B/C inventing | **MET** | Weave Constraints + §pt7: no Strategy list ACL, agent hard-wall, Cognito/SSO, MM/DC4 as delivered; Stage B/C HOLD; gate #24 HOLD. |
| 8 | Cross-story non-merge | **MET** | Weave + INDEX: #32 Doc track separate from #31; harden/consume #4–#7 only; #7 stub unchanged. |
| 9 | Cost / spend PoC $0 | **MET** | Weave Constraints + §pt9: PoC $0; no IdP/vault as delivered. |
| 10 | Handshake close | **MET** | Weave correctly HOLDs overall Doc PASS until Security QA `doc-qa-confirm`. This points-review → Security QA. Docs QA must not PASS until confirm. |

## Soft notes (non-blocking)

- README strongly documents Artifact list/get and Negotiation/Offer **get** + mutation party rules; explicit `GET /negotiations` and `GET /offers` **list** curl sections are thinner than weave/Spec — weave + Product QA report + INDEX carry list isolation for Doc score. Optional README polish later; not a HOLD.
- Mirror PR #36 OPEN at score time — KB weave + INDEX already locked per Chief Docs.

## Gaps

**None.** #31 not scored here.

## Done-list (for Security QA)

- [x] DOC-FLOW filed
- [x] Weave + checklist + README/INDEX cites for pts 1–10
- [x] #31 OUT; Stage B/C + #24 HOLD; #7 stub; PoC $0
- [ ] Security QA: confirm **PASS** to Chief Security **or** further instructions

## Cost/critical

None.
