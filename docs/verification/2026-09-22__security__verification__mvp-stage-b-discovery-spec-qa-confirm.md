# Security QA — MVP Stage B #40 Instant search / discovery Spec vs Chief Security Spec checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-22  
**Verdict:** **PASS** (all 10 Spec-step Security points MET)  
**Asked by:** Dealoware Chief Security (Stage B Spec Security handshake)  
**Chief checklist (binding):** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-checklist.md` (10 points)  
**SoR (GitHub main, PR #43 MERGED):** `docs/verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-checklist.md`  
**Senior Security done-list:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-points-review.md` (**PASS** 10/10)  
**Spec:** `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md`  
**Format ref:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-qa-confirm.md` · `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/40 · Instant search / discovery (P2)  
**Parent:** #18 · Option A (CEO ACCEPTED) · Stage B named slice  
**Siblings:** #41 · #42 — cross-ref only; separate Specs / separate confirms  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-qa-confirm.md`  
**Constraints:** P2 instant only; discovery ≠ #32 owner inventory; omit StrategyBody/LoginEmail/ContactEmail/private lists/auth secrets; consume #31; Gate **#25** backlog; Stage C + #18 Spec/SD HOLD; no MM/DC4; no Cognito/vault inventing; PoC **$0**; keep #41/#42 separate.

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Chief Security Spec checklist (KB) | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-checklist.md` | Binding 10 points |
| SoR checklist (GitHub main) | `docs/verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-checklist.md` | **Present** (PR #43 MERGED) — SoR evidence |
| Senior Security points-review | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-points-review.md` | **PASS** 10/10 |
| Spec (#40) | `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md` | Locked #0–#8; §§1–6; §5 maps 1–10; §7 AC + §7.1 tests |

## Independent score (Security QA)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Authn fail-closed | **MET** | Locked #1; §1 Authn — validated #5 principal; unauthenticated → **401**; error bodies omit private fields. §5 row 1. |
| 2 | Discovery ≠ owner inventory | **MET** | Locked #4; §2 — separate surface from #32 owner list/get; must **not** dump another Participant’s private inventory or secrets. §5 row 2. |
| 3 | Search payload omit secrets | **MET** | Locked #3; §1 Search payload omit — explicitly **absent**: StrategyBody, LoginEmail, ContactEmail, private account lists / Strategy inventory, auth secrets. §5 row 3. |
| 4 | Discoverable fields only | **MET** | Locked #2; §1 Discoverable fields — Artifact fields already on path / allowed for discovery (PoC Artifact D1–D5 / MVP surface); **no new Artifact schema**. §5 row 4. |
| 5 | Uniform deny / no leak | **MET** | Locked #5; §1 Authn — wrong-principal / stranger → fail-closed; uniform deny bodies; no private-field leakage. §5 row 5. |
| 6 | Consume Field ACL, don’t rewrite #31 | **MET** | Locked #6; §3 — IFieldPolicy / FieldClass projection omit; do **not** rewrite #31 registry AC. §5 row 6. |
| 7 | No Stage C / #18 inventing | **MET** | Locked #8; Constraints; §6 OUT — no Assistant hard wall, Cognito/SSO, MM/DC4; #18 Spec/SD HOLD. §5 row 7. |
| 8 | OUT locked | **MET** | Locked #7/#8; §6 OUT — P2 instant only; saved-search → **V1**; A1 → **V2**; gate **#25** backlog. §5 row 8. |
| 9 | Cost / spend | **MET** | §4 Host/cost — local/$0; ECS Express sketch only; **no AWS/IdP/vault provision**. Header PoC **$0**. §5 row 9. |
| 10 | Traceability + handshake | **MET** | Sources cite #40 AC + #18 Option A Stage B; §5 maps 1–10; Spec QA must **not** PASS until Security QA confirms; keep #41/#42 separate. §5 row 10. Gate **#25** not opened (§6 OUT). |

## Key binds (asker) — cross-check

| Bind | Result | Cite |
|------|--------|------|
| Authn 401 fail-closed | **OK** | Locked #1; §1 Authn |
| Discovery ≠ #32 inventory | **OK** | Locked #4; §2 |
| Omit StrategyBody / LoginEmail / ContactEmail / private lists / auth secrets | **OK** | Locked #3; §1 omit table |
| Discoverable fields only; no new schema | **OK** | Locked #2; §1 Discoverable fields |
| Uniform deny | **OK** | Locked #5; §1 Authn |
| Consume #31 | **OK** | Locked #6; §3 |
| Stage C / #18 HOLD | **OK** | Locked #8; §6 OUT; Constraints |
| P2 instant; V1/V2; #25 backlog | **OK** | Locked #7/#8; §6 OUT |
| PoC $0 | **OK** | §4; header |
| Separate from #41/#42 | **OK** | Constraints; §3; Sources |

## Soft notes (non-blocking)

- **Senior Spec-step points-review present.** Independent Security QA score **agrees** with Senior **PASS 10/10** on all points with matching Spec cites. Senior soft note on docs lag is **superseded** — SoR checklists are on GitHub main (PR #43).
- **SoR checklists unlocked.** Binding checklist cited from `docs/verification/` (GitHub main) and KB `verification/` twin; Spec scored from KB Spec + checklist evidence.

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are process/docs polish only.

## Guardrails noted

- **P2 instant only** — saved-search → V1; A1 matching → V2; no inventing.
- **Discovery ≠ owner inventory** — separate from #32; no secrets dump.
- **Omit denied classes** — StrategyBody, LoginEmail, ContactEmail, private lists, auth secrets absent from search payloads via #31 IFieldPolicy.
- **Gate #25 backlog** — not opened by this Spec; Stage C + #18 Spec/SD HOLD.
- **PoC $0** — no IdP/vault provision; no MM/DC4.
- **Siblings separate** — #41 / #42 cross-ref only; this confirm is #40 only.

## Senior alignment

Senior Spec-step points-review **PASS 10/10** (`verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-points-review.md`). Independent Security QA score **agrees** on all 10 MET with matching Spec cites (Locked # / §1–§6 / §5 rows). No contradiction.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Spec QA may PASS Spec gate to Chief Spec after this confirm (subject to Chief clear). Cost/critical: none. No AWS/IdP spend. Do **not** open gate #25 now. Triad CLOSE / Dev Plan unlock remains Chief’s after Spec Security QA PASS. Siblings #41/#42 remain separate Specs (cross-ref only).
