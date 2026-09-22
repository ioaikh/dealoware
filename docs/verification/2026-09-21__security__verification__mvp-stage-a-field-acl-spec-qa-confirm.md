# Security QA — MVP Stage A #31 Field ACL registry Spec vs Chief Security Spec checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 Spec-step Security points MET)  
**Asked by:** Dealoware Spec QA (Spec-side PASS / Security confirm pending)  
**Chief checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-checklist.md` (10 points)  
**Senior Security done-list:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-points-review.md` (**PASS** 10/10)  
**Spec:** `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` (bounce amend v2 — full §7 AC map + §7.1 tests)  
**Spec QA Spec-side:** `verification/2026-09-21__spec__verification__mvp-stage-a-field-acl-registry.md` (PASS Spec-side bind; HOLD Spec gate until this confirm)  
**Format ref:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/31 · Field ACL registry + API projection (FieldClass / IFieldPolicy)  
**Parent:** #18 · Option A (CEO ACCEPTED) · Stage A  
**Sibling:** #32 Account list fail-closed — cross-ref only; separate Spec  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-qa-confirm.md`  
**Constraints:** Stage A API/DB only; open-ended FieldClass; deny-by-default; LoginEmail OwnAgent Deny; ContactEmail ShareOutbound Deny / #7 unchanged; omit denied fields; Soft DisplayName non-blocking; Stage B/C OUT; #24 not opened; PoC **$0**; separate from #32.

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Chief Security Spec checklist | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-checklist.md` | Binding 10 points |
| Senior Security points-review | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-points-review.md` | **PASS** 10/10 |
| Spec (#31) | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | Bounce amend v2; Locked #0–#8; §§1–6; §5 maps 1–10; §7 full AC + §7.1 tests |
| Spec QA Spec-side | `verification/2026-09-21__spec__verification__mvp-stage-a-field-acl-registry.md` | Spec-side PASS; Security QA pending (this artifact) |
| #32 catch-up (sibling DOC-FLOW) | `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-points-review.md` | Accepted as DOC-FLOW aligning existing #32 Spec PASS — **not** merged into #31 |

## Independent score (Security QA)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Stage A API/DB scope | **MET** | Locked #3 dual wall — Stage A API layer only; agent/tool hard wall Stage C HOLD. §1 API/DB projection; §5 row 1; §6 OUT agent wall. |
| 2 | Open-ended FieldClass | **MET** | Locked #1/#4; §1 Registry — extensible any account-info class; LoginEmail/ContactEmail starters ≠ exhaustive. Soft DisplayName illustrative. §5 row 2. |
| 3 | Deny-by-default policy | **MET** | Locked #5; §1 Policy — unknown/unregistered FieldClass → deny; no silent allow. §5 row 3. |
| 4 | LoginEmail User-only (API) | **MET** | Locked #6; §2 LoginEmail — User R/W; **OwnAgent Deny**; Counterparty/stranger Deny. §7 AC map + §7.1 OwnAgent denied LoginEmail. §5 row 4. |
| 5 | ContactEmail rules (API, no share) | **MET** | Locked #7; §2 ContactEmail — OwnAgent Read; counterparty Deny; **ShareOutbound Deny** (Stage B HOLD); §3 #7 identity-seal stub unchanged. §5 row 5. |
| 6 | Fail-closed authn | **MET** | §1 API/DB projection — validated #5 principal; unauthenticated → 401/403; errors omit private fields; denied fields **omitted** from DTOs. §7.1 unauth case. §5 row 6. |
| 7 | No Stage B/C inventing | **MET** | Locked #8; §6 OUT — Strategy ACL HOLD B; share-after-Accept HOLD B; agent wall HOLD C; Cognito/SSO/vault Out; MM/DC4 Out; gate #24 not by Spec. §5 row 7. |
| 8 | Cross-story non-merge | **MET** | Constraints + §3 + §6 OUT: separate Spec from #32 (cross-ref only); #7 stub unchanged; Field ACL does not replace #6 party-only; no rewrite PoC #3–#8 AC. §5 row 8. |
| 9 | Cost / spend | **MET** | §4 Host/cost — extend O10 Domain policy + Api/Application projection; local/$0; ECS Express sketch only; **no AWS/IdP provision**. Header PoC **$0**. §5 row 9. |
| 10 | Traceability + handshake | **MET** | Sources cite #31 AC + Option A / #18 + Stage A SA Pick A only. §5 maps checklist 1–10 with section cites. §7 full AC map + §7.1 tests. Done-list: Spec QA must **not** PASS until Security QA confirms; triad CLOSE / Dev Plan unlock HOLD until Spec Security QA PASS (Chief). §5 row 10. Gate **#24** not opened (§6 OUT). |

## Key binds (asker) — cross-check

| Bind | Result | Cite |
|------|--------|------|
| Stage A API/DB only | **OK** | Locked #3; §1; §6 |
| Open-ended FieldClass | **OK** | Locked #1/#4; §1 Registry |
| Deny-by-default | **OK** | Locked #5; §1 Policy |
| LoginEmail OwnAgent Deny | **OK** | Locked #6; §2; §7.1 |
| ContactEmail ShareOutbound Deny / #7 unchanged | **OK** | Locked #7; §2; §3 |
| Omit denied fields | **OK** | §1 API/DB projection; §7 AC |
| Soft DisplayName non-blocking | **OK** | Locked #4; §2; §7.1 |
| Stage B/C OUT | **OK** | Locked #8; §6 |
| #24 not opened | **OK** | §6 OUT “Gate #24 / SA-REV-MVP-A open — After Stage A delivery” |
| PoC $0 | **OK** | §4; header |
| Separate from #32 | **OK** | Constraints; §3; sibling cross-ref only |

## Soft notes (non-blocking)

- **Soft DisplayName.** Spec Locked #4 / §2 / §7.1 state DisplayName soft/illustrative **non-blocking** if not yet projected — aligns Chief asker + Senior; no HOLD.
- **Bounce amend v2.** §7 full issue AC map + §7.1 automated tests detail present; Spec QA Spec-side cleared prior bounce — strengthens point 10 traceability; no content gap.
- **#32 catch-up DOC-FLOW.** Accept `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-points-review.md` as DOC-FLOW aligning existing #32 Spec PASS. Remains sibling cross-ref only — does **not** merge #31/#32 Specs or weaken Field ACL vs list isolation complementarity.

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are process/docs polish only.

## Guardrails noted

- **API/DB only** — FieldClass + IFieldPolicy on projection paths; dual wall end-state accepted; agent/tool hard wall Stage C HOLD.
- **Open-ended + deny-by-default** — extensible registry; unknown FieldClass deny; omit denied fields; no private fields in errors.
- **LoginEmail / ContactEmail** — OwnAgent Deny LoginEmail; ContactEmail ShareOutbound Deny Stage A; #7 stub unchanged.
- **Stage B/C / #24 / #32 / #7** — Strategy ACL HOLD; share-after-Accept HOLD; agent wall HOLD; gate #24 not opened by Spec; #32 separate Spec; #7 stub unchanged.
- **PoC $0** — no IdP/vault provision for this Spec.
- **Soft DisplayName** — non-blocking.

## Senior alignment

Senior Spec-step points-review **PASS 10/10** (`verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-points-review.md`). Independent Security QA score **agrees** on all 10 MET with matching Spec cites (Locked # / §1–§6 / §5 rows). Soft DisplayName non-blocking note aligned. No contradiction. Spec QA Spec-side already bound §5 1–10 and asked Security QA (this confirm).

## Handshake status

Security QA → **PASS** confirm to Chief Security. Spec QA may PASS Spec gate to Chief Spec after this confirm (still subject to Chief clear of SA Arch QA PASS + BA AC locked per Spec-side — already cleared). Cost/critical: none. No AWS/IdP spend. Do **not** open gate #24 now. Triad CLOSE / Dev Plan unlock remains Chief’s after Spec Security QA PASS. Sibling #32 remains separate Spec (cross-ref only); #32 catch-up points-review accepted as DOC-FLOW only.
