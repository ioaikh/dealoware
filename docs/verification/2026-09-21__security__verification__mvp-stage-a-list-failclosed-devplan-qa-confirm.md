# Security QA — MVP Stage A #32 Account list fail-closed Dev Plan vs Chief Security Dev Plan checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 Dev Plan-step Security points MET)  
**Asked by:** Dealoware Dev Plan QA / Chief Security (PRIORITY)  
**Chief checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-checklist.md` (10 points)  
**Senior Security done-list:** *not present* (`verification/*list-failclosed-devplan-points*` absent) — **direct plan evidence used**  
**Dev Plan:** `plans/2026-09-21__devplan__plan__mvp-stage-a-account-list-fail-closed.md`  
**Spec (context):** `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md`  
**Spec Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md` (points 1–10 MET)  
**Format ref:** `verification/2026-09-20__security__verification__poc-identity-seal-devplan-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/32 · Account list fail-closed (negotiations / offers / artifacts)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #31 Field ACL registry — cross-ref only; **keep separate** from this Dev Plan  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-qa-confirm.md`  
**Constraints:** Stage A only; Stage B/C + gate #24 OUT; PoC **$0**; no Cognito/MM/DC4; #7 stub unchanged; never skip Chief.

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Chief Security Dev Plan checklist | `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-checklist.md` | Binding 10 points |
| Senior Security points-review | `verification/*list-failclosed-devplan-points*` | **Absent** — direct plan evidence OK |
| Dev Plan (#32) | `plans/2026-09-21__devplan__plan__mvp-stage-a-account-list-fail-closed.md` | Steps 1–11; Locked #0–#8; §6 Security table; Explicit OUT; Done-list |
| Spec (#32) | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Context; Spec locks consumed |
| Spec Security PASS | `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md` | Upstream Spec-step 1–10 MET — binding unlock |

## Independent re-score (Security QA)

| # | Point | Result | Evidence (plan section / step) |
|---|-------|--------|--------------------------------|
| 1 | Authn fail-closed tasks — #5 principal on Neg/Offer/Artifact list+get; unauth → 401/403 no private leak | **MET** | Step 5: validated #5 on all Artifact/Negotiation/Offer list+get; unauth/invalid → 401/403; bodies omit private fields. Steps 2–4 Authn rows; Step 6 unauth deny tests; Step 11 self-verify. Locked #6. §6 row 1 |
| 2 | Negotiation list/get isolation — party filters + unauthorized fail-closed (404 preferred) no cross-account leak | **MET** | Step 3: party A/B repository/query list+get; stranger/non-party → 404 preferred; IDOR fail-closed; no UI-only. Step 6 party OK + stranger/IDOR cases. Locked #2. §6 row 2 |
| 3 | Offer list/get isolation — party via parent Negotiation | **MET** | Step 4: party-via-parent query-plane join/filter; stranger/IDOR → 404 preferred; no cross-account offer leak. Step 6 Offer cases. Locked #3. §6 row 3 |
| 4 | Artifact list/get isolation — owner-scoped; align #4 | **MET** | Step 2: `OwnerParticipantId` == `#5.sub`; harden #4 list-own/get in query plane; IDOR deny; no secret-bearing body. Step 6 Artifact cases. Locked #1. §6 row 4 |
| 5 | Deny-body / empty-list hygiene — error bodies and empty lists do not leak other Participants’ private fields | **MET** | Step 7 dedicated hygiene verify; Step 5 deny/empty-list rules; Locked #4; Step 6 asserts absence of private/secret fields in deny bodies. §6 row 5 |
| 6 | Complement #31 Field ACL — cross-ref field projection; do not replace party/owner list rules with Field ACL alone | **MET** | Step 8: #32 = which rows; #31 = which fields; no Field ACL impl; do not weaken #6 party rules; no Story merge. Locked #7; Explicit OUT. §6 row 6 |
| 7 | No Stage B/C inventing — no Strategy list ACL, agent hard-wall, Cognito, or MM/DC4 tasks | **MET** | Step 9 OUT table (Strategy HOLD B; agent wall HOLD C; Cognito/MM/DC4 Out; gate #24 backlog). Step 10 forbid Cognito/MM. Locked #8. §6 row 7 |
| 8 | Cross-story non-merge — separate from #31 plan and PoC Stories except consume patterns | **MET** | Header Constraints (#32 only; #31 cross-ref separate); Steps 2–4 harden #4/#6 (no rewrite); Step 8 #31 cross-ref only; #7 stub unchanged (Steps 7–9); Sources cite sibling Spec without merging. §6 row 8 |
| 9 | Cost / spend — PoC **$0**; no IdP/vault provision tasks | **MET** | Step 10 local/$0; no Cognito/IdP/vault/AWS provision; Cost/critical → CPM→COO→CEO. Header PoC $0; §8 Cost/critical. Locked #8. §6 row 9 |
| 10 | Handshake close — Dev Plan QA must **not** PASS until Security QA confirms | **MET** | §6 Handshake note + Done-list §9: Dev Plan QA must ask Security QA confirm points 1–10 before PASS; do not skip Chief; HOLD stops SD unlock. §6 row 10 |

## Soft notes (non-blocking)

- **No Senior Dev Plan-step points-review.** `verification/*list-failclosed-devplan-points*` absent at QA time — independent score used Dev Plan + Chief checklist only (same posture as Spec-step QA confirm). No content gap.
- **Step 3 list surface.** Plan allows adding party-scoped list if PoC only had get-by-id — bounded to party-scoped query (no discovery/search inventing); aligns Spec; soft clarity only.
- **Keep separate from #31.** Plan repeatedly forbids implementing Field ACL under #32 and merging Stories — clear weave of checklist point 6/8; no HOLD.

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are process/docs polish only.

## Alignment with Senior review

Senior Dev Plan-step points-review **not present** — no Senior score to align or contradict. Direct plan evidence supports **PASS**. Plan §6 already maps checklist 1–10 with step cites matching this independent re-score.

## Spec Security PASS cite

Upstream Spec-step Security QA PASS: `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md` (2026-09-21, all 10 MET). Dev Plan cites it as binding unlock; no inventing beyond Spec/#32 Stage A slice.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Stage A only (#32 list fail-closed) | Held (Steps 1–8; Locked #0–#7) |
| Stage B/C OUT | Held (Step 9; Locked #8; Explicit OUT) |
| Gate #24 not opened | Held (Steps 1, 9–10; Explicit OUT) |
| Separate from #31 | Held (Step 8; Header; Explicit OUT) |
| PoC $0; no Cognito/MM/DC4 | Held (Steps 1, 9–10; Cost/critical) |
| #7 stub unchanged | Held (Steps 7–9; Explicit OUT) |
| Query-plane (not UI-only) | Held (Steps 1–4; Locked #0/#5) |

## Handshake status

Security QA → **PASS** confirm to Chief Security. Dev Plan QA may PASS to Chief Dev Planner on Security gate. Cost/critical: none. No AWS/IdP/vault spend. Do **not** open gate #24 now. Keep #31 on its own plan/Story.
