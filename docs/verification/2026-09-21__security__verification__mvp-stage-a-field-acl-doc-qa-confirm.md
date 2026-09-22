# Security QA — MVP Stage A #31 Field ACL Doc vs Chief Security Doc checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 Doc-step Security points MET)  
**Asked by:** Dealoware Docs / Bot Manager — Doc Security handshake (PRIORITY — Docs QA HOLD until confirm)  
**Senior Security done-list:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-doc-points-review.md` (**PASS** 10/10)  
**Doc weave (locked):** `ops/2026-09-21__docs__ops__mvp-stage-a-field-acl-doc-security-weave.md`  
**Chief checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-doc-checklist.md` (10 points)  
**Primary Doc surfaces:** PR #37 MERGED Field ACL registry + Profile API projection + locked Doc weave + INDEX #31 annotations  
**Optional Doc mirror:** https://github.com/ioaikh/dealoware/pull/39 @ `693486b` (OPEN — cite only; score on KB weave + INDEX + PR #37)  
**Product QA Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/31 · Field ACL registry + API projection (FieldClass / IFieldPolicy)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #32 Account list fail-closed — **OUT / not scored / not confirmed here** (CLOSED separate; cross-ref only)  
**PR (primary):** https://github.com/ioaikh/dealoware/pull/37 (**MERGED** · main `fb47fdd3…`)  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-doc-qa-confirm.md`  
**Constraints:** Stage A only; Stage B/C + gate #24 HOLD; PoC **$0**; no Cognito/MM/DC4; #7 stub unchanged; never skip Chief. Soft: DisplayName starter/example non-blocking; README root may lack dedicated Field ACL section — weave + Spec/DevPlan/SD/Product QA + INDEX carry Doc Security surface (same pattern as #32 list curl soft note).

## Soft notes accepted (non-blocking)

| Soft note | Disposition |
|-----------|-------------|
| Soft DisplayName starter/example | **Accepted** — non-blocking per CPM/Chief; docs may note starter/example without gating overall Doc (Senior Doc points-review soft note) |
| README root may lack dedicated Field ACL section | **Accepted** — weave §pts 1–10 + Spec/DevPlan/SD/Product QA Sec confirm + INDEX carry Doc Security surface; optional README polish later; **not a HOLD** (aligns Senior soft note) |
| Mirror PR #39 OPEN @ `693486b…` | **Accepted** — cite only; score on locked KB weave + INDEX + PR #37 MERGED (same pattern as #32 Doc QA / PR #36) |
| No live `dotnet test` on this box for Doc step | **Accepted** — Doc score is documentation surfaces; Product QA Sec PASS + `FieldAclTests` / `FieldPolicy_UnknownFieldClass_DeniedByDefault` cites in weave are supporting context only |

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security Doc checklist | `…field-acl-doc-checklist.md` | Binding 10 points |
| Doc Security weave (locked) | `ops/…field-acl-doc-security-weave.md` | HOLD overall Doc until this confirm; pts 1–10 mapped |
| Senior Doc points-review | `…field-acl-doc-points-review.md` | **PASS** 10/10 — present; aligned |
| Product QA Security PASS | `…field-acl-productqa-qa-confirm.md` | Supporting (Sec10 Product QA closed; not overall Doc PASS) |
| PR #37 | MERGED — Field ACL registry + Profile API projection | Primary code Doc surface |
| Mirror PR #39 | OPEN @ `693486b…` | Cite only |
| INDEX.md | #31 Doc checklist + weave HOLD indexed; soft→full MATCH | MATCH |

## Independent re-score (Security QA)

| # | Point | Senior | Security QA | Evidence |
|---|-------|--------|-------------|---------|
| 1 | API/DB scope only — FieldClass + IFieldPolicy on API/DB projection; agent hard-wall impl not delivered (Stage C HOLD) | MET | **MET** | Weave §pt1 + Constraints: FieldClass registry + `IFieldPolicy` on Profile API/DB (`ProfileMapper`, `/profile`); agent hard-wall Stage C HOLD. Product QA Sec confirm MET pt1 (supporting). |
| 2 | Open-ended FieldClass registry — extensible; LoginEmail/ContactEmail/DisplayName starters not closed set | MET | **MET** | Weave §pt2: `Custom(name)`; LoginEmail/ContactEmail/DisplayName starters/examples — not exhaustive. Soft DisplayName non-blocking. Product QA AC1/Sec pt2. |
| 3 | Deny-by-default — unknown/unregistered FieldClass → deny; cite tests | MET | **MET** | Weave §pt3 cites `FieldPolicy_UnknownFieldClass_DeniedByDefault`. Spec/DevPlan/SD/Product QA. Product QA Sec pt3. |
| 4 | LoginEmail User-only (API) — not projected to counterparty, stranger, or OwnAgent | MET | **MET** | Weave §pt4: LoginEmail not projected to counterparty/stranger/OwnAgent via API/DB; User R/W. Product QA AC4/Sec pt4. |
| 5 | ContactEmail rules; no share path; #7 stub unchanged | MET | **MET** | Weave §pt5: OwnAgent Read / counterparty Deny on API; ShareOutbound Deny until Stage B; **#7** stub Accept=state-only. Product QA AC5/Sec pt5. |
| 6 | Authn fail-closed on projection — #5 principal; 401/403; no private fields in error bodies | MET | **MET** | Weave §pt6: protected projection requires #5 principal; unauth → 401; deny → 403 ProblemDetails without private fields. Product QA AC6/Sec pt6. |
| 7 | No Stage B/C inventing — no Strategy ACL, agent hard-wall, Cognito/SSO, MM/DC4 as delivered | MET | **MET** | Weave Constraints + §pt7: Stage B/C HOLD; gate #24 HOLD; no Strategy ACL / agent hard-wall / Cognito/SSO / MM/DC4 as #31 delivered. Product QA Sec pt7. |
| 8 | Cross-story non-merge — do not rewrite #32 or PoC Stories; #32 remains separate CLOSED | MET | **MET** | Weave Explicit separations + INDEX: **#32 OUT**; harden/consume cross-refs to #4–#7 only; #7 stub unchanged; no rewrite of #32 under this weave. **#32 not confirmed here.** Product QA Sec pt8. |
| 9 | Cost / spend — PoC **$0**; no IdP/vault provision as delivered | MET | **MET** | Weave Constraints + §pt9: PoC $0; no IdP/vault as delivered. Product QA Sec pt9. |
| 10 | Handshake close — Docs QA must **not** PASS until Security QA confirms | MET | **MET** | Weave correctly HOLDs overall Doc PASS until this confirm. This file closes Doc Security gate. Docs QA may clear HOLD → overall Doc PASS. **Do not treat as #32 confirm.** |

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| API/DB projection only; Stage C agent wall HOLD | Held (weave + Constraints) |
| Open-ended FieldClass; Soft DisplayName non-blocking | Held |
| Deny-by-default cited (`FieldPolicy_UnknownFieldClass_DeniedByDefault`) | Held |
| LoginEmail User-only; ContactEmail no ShareOutbound; #7 stub | Held |
| Authn fail-closed on projection (401/403; no private in errors) | Held |
| #32 OUT — separate CLOSED; not scored / not confirmed | Held |
| Stage A only; Stage B/C + #24 HOLD | Held |
| PoC $0; no Cognito/MM/DC4 | Held |
| Soft DisplayName + README polish = non-blocking | Held (Senior + Security QA agree) |
| Mirror PR #39 cite-only | Held |

## Alignment with Senior Security done-list

Senior Doc points-review scored pts **1–10 MET** on locked weave + checklist + INDEX/PR #37 with soft notes on Soft DisplayName and README root Field ACL section. Independent Security QA re-score **agrees** on all 10; soft notes **accepted**. No bounce. Senior catch-up was present at confirm time — **aligned**.

## Gaps

**None.** Soft notes (Soft DisplayName; README root Field ACL polish; PR #39 OPEN cite-only; no live dotnet for Doc step) non-blocking. **#32 OUT.**

## Handshake status

Security QA → **PASS** confirm to Chief Security. Dealoware Docs / Senior Docs / Docs QA may clear overall Doc-step #31 HOLD on Security gate (clear weave HOLD). Cost/critical: none. No AWS/IdP/vault spend. Do **not** open gate #24. Do **not** confirm / re-open #32 (Account list fail-closed remains separate CLOSED).
