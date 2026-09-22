# Security QA — MVP Stage A #31 Field ACL registry Product QA vs Chief Security Product QA checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 Product-QA-step Security points MET)  
**Asked by:** Dealoware QA / QAQA / Senior Product QA / Chief Security (PRIORITY — after main-refresh HOLD cleared)  
**Product QA report:** `qa/2026-09-21__qa__qa-report__mvp-stage-a-field-acl-registry.md` (HOLD PASS pending this confirm; pts 1–9 EVIDENCED on **main**; pt 10 handshake open)  
**Chief checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-checklist.md` (10 points)  
**Senior Security done-list:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-points-review.md` (**PASS** 10/10)  
**Prior SD Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-qa-confirm.md` (PASS 10/10 @ PR HEAD `2228718e…`)  
**Format ref:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/37 (**MERGED**)  
**PR HEAD:** `2228718eb7d5f7e7e0a95ac984a01532055c398b`  
**main merge commit:** `fb47fdd3c3563a6eccfbc85ee60ce1dd5b65c7ab`  
**CI (main @ merge):** https://github.com/ioaikh/dealoware/actions/runs/35670498420 — **SUCCESS**  
**Tests:** `tests/Dealoware.Api.Tests/FieldAclTests.cs` (~29 Facts)  
**Issue:** https://github.com/ioaikh/dealoware/issues/31  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #32 Account list fail-closed — **CLOSED / not re-scored** (separate)  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-qa-confirm.md`  
**Constraints:** Stage A only; Stage B/C + gate #24 HOLD; #7 stub unchanged; PoC **$0**; no Cognito/MM/DC4; never skip Chief. Soft: no live `dotnet` — CI + FieldAclTests + prior SD PASS. Prior HOLD until report cited main — **cleared** (report now cites main `fb47fdd3…`).

## Soft gaps accepted (non-blocking)

| Soft gap | Disposition |
|----------|-------------|
| No live `dotnet test` on evidence box | **Accepted** — main CI SUCCESS run 35670498420 + FieldAclTests + prior SD Security PASS (same pattern as #32) |
| Denied fields as `null` + `Includes*` vs strict JSON omit | **Accepted** — aligned with SD Security soft note |

## Independent re-score (Security QA)

| # | Point | Product QA | Security QA | Evidence |
|---|-------|------------|-------------|----------|
| 1 | API/DB scope only | EVIDENCED | **MET** | Domain FieldAcl + Profile API/DB projection; agent hard-wall OUT (AC7 / Sec #1); Stage C HOLD |
| 2 | Open-ended FieldClass | EVIDENCED | **MET** | Starters + `Custom`; extensibility Facts (AC1 / Sec #2) |
| 3 | Deny-by-default | EVIDENCED | **MET** | `FieldPolicy_UnknownFieldClass_DeniedByDefault` (AC2 / Sec #3) |
| 4 | LoginEmail User-only (API) | EVIDENCED | **MET** | LoginEmail matrix; OwnAgent deny Fact (AC4 / Sec #4) |
| 5 | ContactEmail rules; no share; #7 unchanged | EVIDENCED | **MET** | ContactEmail matrix; ShareOutbound deny-all; #7 stub cite (AC5 / Sec #5) |
| 6 | Authn fail-closed; no private in errors | EVIDENCED | **MET** | unauth/invalid 401; NoPrivateFields Facts; Health open (AC6 / Sec #6) |
| 7 | No Stage B/C inventing | EVIDENCED | **MET** | No Strategy ACL / Cognito / MM-DC4 / agent wall in Product QA scope (Sec #7) |
| 8 | Cross-story non-merge | EVIDENCED | **MET** | Field ACL + Profile only; #32 not rewritten (Sec #8) |
| 9 | Cost / spend PoC $0 | EVIDENCED | **MET** | No IdP/vault; PoC $0 (Sec #9) |
| 10 | Handshake close | HOLD (confirm missing) | **MET** | This confirm closes Product QA Security gate; Product QA may clear HOLD → PASS |

## Alignment with Senior review

Senior Security done-list (`…field-acl-productqa-points-review.md`) scored all 10 **MET**. Independent Security QA re-score on **main-refreshed** Product QA report **agrees** — no gaps; soft notes align.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Stage A API/DB only; Stage C agent wall HOLD | Held |
| #7 stub unchanged | Held |
| #32 CLOSED separate | Held |
| Stage B/C + #24 HOLD | Held |
| PoC $0; no Cognito/MM/DC4 | Held |
| Evidence on main `fb47fdd3…` | Held (report re-verified after merge) |

## Gaps

**None.**

## Handshake status

Security QA → **PASS** confirm to Chief Security. Product QA / QAQA may clear HOLD and PASS to Chief. Cost/critical: none. No AWS/IdP spend.
