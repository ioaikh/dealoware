# Security QA — MVP Stage A #32 Account list fail-closed Spec vs Chief Security Spec checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 Spec-step Security points MET)  
**Asked by:** Dealoware Spec QA (Spec-side PASS / Security confirm pending)  
**Chief checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-checklist.md` (10 points)  
**Senior Security done-list:** *not present* (`verification/*list-failclosed-spec-points*` absent) — **direct Spec evidence used**  
**Spec:** `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md`  
**Spec QA Spec-side:** `verification/2026-09-21__spec__verification__mvp-stage-a-account-list-fail-closed.md` (PASS Spec-side bind; HOLD Spec gate until this confirm)  
**Format ref:** `verification/2026-09-21__security__verification__mvp-participant-secrets-acl-sa-qa-confirm.md` · recent Spec-step `verification/2026-09-20__security__verification__poc-negotiation-spec-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/32 · Account list fail-closed (negotiations / offers / artifacts)  
**Parent:** #18 · Option A (CEO ACCEPTED) · Stage A  
**Sibling:** #31 Field ACL registry — cross-ref only  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md`  
**Constraints:** Artifact owner fail-closed; Negotiation/Offer party fail-closed; no private leak; query plane; #5 authn; complement #31; Stage B/C OUT; gate #24 not opened; PoC **$0**; #7 stub unchanged; never skip Chief.

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Chief Security Spec checklist | `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-checklist.md` | Binding 10 points |
| Senior Security points-review | `verification/*list-failclosed-spec-points*` | **Absent** — direct evidence OK |
| Spec (#32) | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Locked #0–#8; §§1–6; §5 maps 1–10 |
| Spec QA Spec-side | `verification/2026-09-21__spec__verification__mvp-stage-a-account-list-fail-closed.md` | Spec-side PASS; Security QA pending (this artifact) |

## Independent score (Security QA)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Fail-closed authn | **MET** | §1 Authn: all list/get paths require validated principal; unauthenticated → **401/403** with bodies that omit private fields. Locked #6. §5 row 1. Aligns PoC #5. |
| 2 | Negotiation list/get isolation | **MET** | §1 Negotiation: authorized = party A or B; unauthorized fail-closed; no leak of other accounts’ negotiations. Locked #2. §5 row 2. Harden PoC #6 party-only. |
| 3 | Offer list/get isolation | **MET** | §1 Offer: authorized = party to parent negotiation; unauthorized fail-closed; no cross-account offer leak. Locked #3. §5 row 3. |
| 4 | Artifact list/get isolation | **MET** | §1 Artifact: `OwnerParticipantId` == caller `sub`; unauthorized/IDOR fail-closed (404 preferred or 403); no secret-bearing body. Locked #1. §5 row 4. Align/harden PoC #4. |
| 5 | No private-field leakage in denies | **MET** | §1 Deny hygiene: error bodies — no private fields / contact/PII / FieldClass secrets; empty lists must not smuggle other tenants; prefer 404 for non-authorized get-by-id. Locked #4. §5 row 5. |
| 6 | Complement #31; don’t weaken #6 | **MET** | §3: #31 = field projection for allowed rows; list isolation selects **which rows**; #6 1:1 party-only remains. Locked #7. §5 row 6. Does not replace Field ACL or party rules. |
| 7 | No Stage B/C inventing | **MET** | Locked #8; §6 OUT — Strategy list ACL HOLD B; agent/tool hard wall HOLD C; Cognito/SSO Out; MM/DC4 Out; no vault inventing. §5 row 7. |
| 8 | Cross-story non-merge | **MET** | Constraints + §3 + §6 OUT: separate Spec from #31 (cross-ref only); #7 seal stub unchanged; no rewrite #31/#7/PoC Stories. §5 row 8. |
| 9 | Cost / spend | **MET** | §4 Host/cost: extend O10 query/repository filters; local/$0; ECS Express sketch only; **no AWS/IdP provision**. Header PoC **$0**. §5 row 9. |
| 10 | Traceability + handshake | **MET** | Sources cite #32 AC + Option A / #18 + Stage A SA Pick A only. §5 maps checklist 1–10 with section cites. Done-list: Spec QA must **not** PASS until Security QA confirms; triad CLOSE / Dev Plan unlock HOLD until Spec Security QA PASS (Chief). §5 row 10. Gate **#24** not opened (§6 OUT). |

## Key binds (asker) — cross-check

| Bind | Result | Cite |
|------|--------|------|
| Artifact owner fail-closed | **OK** | Locked #1; §1 Artifact |
| Negotiation/Offer party fail-closed | **OK** | Locked #2–#3; §1 Negotiation/Offer |
| No private leak | **OK** | Locked #4; §1 Deny hygiene |
| Query plane (not UI-only) | **OK** | Locked #0/#5; SA Pick A cite |
| #5 authn | **OK** | Locked #6; §1 Authn |
| Complement #31 | **OK** | Locked #7; §3 |
| Stage B/C OUT | **OK** | Locked #8; §6 |
| #24 not opened | **OK** | §6 OUT “Gate #24 open — After Stage A delivery” |
| PoC $0 | **OK** | §4; header |

## Soft notes (non-blocking)

- **No Senior Spec-step points-review.** `verification/*list-failclosed-spec-points*` absent at QA time — independent score used Spec + Chief checklist only (per asker: direct evidence OK). No content gap.
- **“Owning account” wording.** Spec clarifies issue AC “owning account” = authorized account scope (owner for artifacts; party for Neg/Offer) — aligns checklist isolation points; good; no HOLD.
- **Query-plane emphasis.** Locked #0/#5 + SA Pick A citation bind repository/query constraints; Spec §5 checklist table does not restate “query plane” as its own checklist row (checklist points 1–10 use authn/isolation/leak/#31/OUT/$0/handshake) — binding is in Locked decisions; soft polish only.

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are process/docs polish only.

## Guardrails noted

- **Owner vs party** — Artifact owner-scoped; Negotiation/Offer party-scoped; no multi-tenant admin inventing.
- **Query plane** — repository/query constraints; not UI-only filter.
- **Complement #31** — list isolation = which rows; Field ACL = field projection; #6 party rules untouched.
- **Stage B/C / #24 / #7** — Strategy ACL HOLD; agent wall HOLD; gate #24 not opened by Spec; #7 stub unchanged.
- **PoC $0** — no IdP/vault provision for this Spec.

## Senior alignment

Senior Spec-step points-review **not present** — no Senior score to align or contradict. Direct Spec evidence supports **PASS**. Spec QA Spec-side already bound §5 1–10 and asked Security QA (this confirm).

## Handshake status

Security QA → **PASS** confirm to Chief Security. Spec QA may PASS Spec gate to Chief Spec after this confirm (still subject to Chief clear of SA Arch QA PASS + BA AC locked per Spec-side HOLD). Cost/critical: none. No AWS/IdP spend. Do **not** open gate #24 now. Triad CLOSE / Dev Plan unlock remains Chief’s after Spec Security QA PASS.
