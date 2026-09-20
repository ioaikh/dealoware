# Doc step weave — Security checklist pts 1–10 · PoC Identity-seal stub (#7)

| Field | Value |
|-------|-------|
| **Status** | **Overall Doc step #7 HOLD** — deferred until Security QA doc-qa-confirm (points-review + qa-confirm handshake) |
| **Date** | 2026-09-20 |
| **Author** | Dealoware Senior Docs |
| **Story** | GitHub issue #7 · Identity-seal stub (no contact exchange) |
| **Issue** | https://github.com/ioaikh/dealoware/issues/7 |
| **PR** | https://github.com/ioaikh/dealoware/pull/19 (MERGED) — README **Identity Seal (PoC Stub)** = primary Doc surface for Security score |
| **Checklist** | `verification/2026-09-20__security__verification__poc-identity-seal-doc-checklist.md` |
| **DOC-FLOW** | `ops/2026-09-20__docs__ops__poc-identity-seal-doc-security-weave.md` |
| **Constraints** | PoC $0 · CQ no-refactor · keep separate from #4 Artifact / #5 auth / #6 negotiation · #8 backlog · do **not** pull #18 tenancy as delivered · no inventing · no MotorMarket · **no contact-on-Accept** |

## Primary surfaces Security will score
1. Merged PR #19 README — **### Identity Seal (PoC Stub)** (opaque IDs; `identitySealed: true`; Accept/Decline/Counter/Close = state-only; leak-proof DTO design; MVP P7/A9 roadmap table; Out of Scope: real contact release / vault / Cognito/SSO / MM/DC4 / AWS provision)
2. This Doc weave (`ops/2026-09-20__docs__ops__poc-identity-seal-doc-security-weave.md`) + living `INDEX.md` annotations

Supporting (not primary score surface): KB `qa/2026-09-20__qa__qa-report__poc-identity-seal-stub.md` (Product QA **PASS**; Sec10 closed via productqa-qa-confirm); Product QA Security PASS `verification/2026-09-20__security__verification__poc-identity-seal-productqa-qa-confirm.md`; Product QA points-review present on disk `verification/2026-09-20__security__verification__poc-identity-seal-productqa-points-review.md` (indexed; not invented).

## Point → how Doc artifacts satisfy

Mapped strictly to `verification/2026-09-20__security__verification__poc-identity-seal-doc-checklist.md`.

| # | Security point | Doc satisfaction (cites) |
|---|----------------|--------------------------|
| 1 | Authn fail-closed | README Identity Seal + Negotiation/Offer sections: #7-touched Neg/Offer APIs require #5 `Authorization` (fail-closed). Weave restates. Product QA Sec confirm MET pt1. |
| 2 | Party-only (narrow) | README: party-only access; non-party 404/403 without leaking contact/identity/other Negotiations. Docs do **not** document full #18 tenancy as PoC delivered. Product QA Sec confirm MET pt2. |
| 3 | Public DTOs omit contact/PII | README: **Opaque IDs only** (`participant:{uuid}`); no email/phone/address on Negotiation/Offer responses; example JSON shows opaque party ids + `identitySealed: true`. Weave restates. Product QA Sec confirm MET pt3. |
| 4 | Accept path = state-only | README: **State-only responses** — Accept/Decline/Counter/Close return offer/negotiation state only; no contact release event. Weave restates. Product QA Sec confirm MET pt4. |
| 5 | No contact-exchange surface | README Out of Scope: real contact release on accept **not** implemented in PoC; MVP roadmap defers contact-on-accept to **P7/A9**. Docs do not describe a PoC contact-exchange endpoint. Weave: **no contact-on-Accept**. Product QA Sec confirm MET pt5. |
| 6 | Stub as precursor (documented, not MVP) | README: stub = foundation for MVP contact-on-accept (P7/A9); PoC vs MVP table (opaque ids / `identitySealed` vs vault). Docs do **not** claim vault/KMS or MVP release behavior as delivered. Product QA Sec confirm MET pt6. |
| 7 | No-leak evidence cited | README Verification: `dotnet test --filter FullyQualifiedName~IdentitySealTests` (no PII field names; no email/phone patterns; `identitySealed: true`; Accept state-only). Product QA report cites 13 Facts. Weave cites. Product QA Sec confirm MET pt7. |
| 8 | No inventing OUT | README Out of Scope + Weave: no Strategy/AI, mature vault, settlement, Cognito/SSO/IdP, MM/DC4 as delivered; **#8** stays backlog; **#18** stays out of PoC. Separate from #4/#5/#6. Product QA Sec confirm MET pt8. |
| 9 | Secrets / host / OUT | README uses placeholder examples; Authorization-header hygiene; local/$0 PoC; ECS Express = host sketch only; no Cognito/SSO/IdP how-tos as delivered. Product QA Sec confirm MET pt9. |
| 10 | Handshake close | **HOLD** — Docs QA must **not** overall-PASS until Security QA indexes doc-points-review + doc-qa-confirm against this checklist. Weave Status = HOLD until that handshake. |

## Indexed Product QA Security (Sec10 for Product QA step — already closed)
- `verification/2026-09-20__security__verification__poc-identity-seal-productqa-checklist.md`
- `verification/2026-09-20__security__verification__poc-identity-seal-productqa-qa-confirm.md` (PASS; all 10 MET)
- `verification/2026-09-20__security__verification__poc-identity-seal-productqa-points-review.md` (present on disk; indexed — not invented)

## HOLD (overall Doc step)
**Overall Doc-step #7 HOLD** — awaiting Senior Security / Security QA handshake on Doc checklist pts 1–10 (`…poc-identity-seal-doc-checklist.md`). Do **not** claim overall Doc PASS. No contact-on-Accept; #8 backlog; #18 not PoC; keep separate from #4 Artifact / #5 auth / #6 negotiation; PoC $0; CQ no-refactor; no MotorMarket.
