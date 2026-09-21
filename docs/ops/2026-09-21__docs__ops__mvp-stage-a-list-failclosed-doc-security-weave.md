# Doc step weave — Security checklist pts 1–10 · MVP Stage A Account list fail-closed (#32)

| Field | Value |
|-------|-------|
| **Status** | **Overall Doc step #32 PASS** — Security QA cleared HOLD (doc-qa-confirm; all 10 MET) |
| **Date** | 2026-09-21 |
| **Author** | Dealoware Senior Docs |
| **Story** | GitHub issue #32 · Account list fail-closed (negotiations / offers / artifacts) |
| **Issue** | https://github.com/ioaikh/dealoware/issues/32 |
| **PR** | https://github.com/ioaikh/dealoware/pull/34 (**MERGED**) — README / API docs list+get fail-closed = primary Doc surface for Security score |
| **Checklist (binding, ISSUED)** | `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-checklist.md` |
| **DOC-FLOW** | `ops/2026-09-21__docs__ops__mvp-stage-a-list-failclosed-doc-security-weave.md` |
| **Constraints** | Stage A only · CQ no-refactor · **separate from #31** (complement / cross-ref only; Field ACL not delivered as this Story) · Stage B/C HOLD · gate **#24** HOLD · PoC **$0** · #7 stub unchanged · no inventing Stories · no MotorMarket / MM/DC4 · no Cognito/SSO as delivered |

## Primary surfaces Security will score
1. Merged PR #34 README / API docs — Artifact / Negotiation / Offer **list+get** fail-closed (authn required; unauth → 401; non-owner/non-party → 404; empty authorized list → `[]`; deny bodies without private-field leak; query-plane owner/party isolation)
2. This Doc weave (`ops/2026-09-21__docs__ops__mvp-stage-a-list-failclosed-doc-security-weave.md`) + living `INDEX.md` annotations for #32 paths

Supporting (not primary score surface): KB Product QA report `qa/2026-09-21__qa__qa-report__mvp-stage-a-account-list-fail-closed.md` (**PASS**; Sec10 closed via productqa-qa-confirm — **not** overall Doc PASS); Product QA Security trio; Spec / DevPlan / SD verify + Security trios; Doc checklist (ISSUED). SA architecture/verify with `field-acl-list-failclosed` in name is **cross-ref only** (shared Stage A brief with #31 — not a #31 deliverable claim under this weave).

## Explicit separations (non-merge)
- **#31 Field ACL** — OUT of this Doc weave score surface; soft INDEX parallel only. Checklist pt 6: list isolation remains owner/party query-plane; Field ACL complements, does not replace, and is **not** delivered as this Story’s Field ACL registry.
- **Stage B/C** — HOLD (no Strategy list ACL, no agent hard-wall inventing).
- **Gate #24** — HOLD (not opened).
- **#7** identity-seal stub — unchanged.
- **PoC $0** — no IdP/vault provision as delivered by #32 Docs.

## Point → how Doc artifacts satisfy

Mapped strictly to `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-checklist.md` (pts 1–10). Handshake files indexed after Security PASS (not invented).

| # | Security point | Doc satisfaction (cites) |
|---|----------------|--------------------------|
| 1 | Authn fail-closed | PR #34 README: Artifact + Negotiation/Offer endpoints require valid `Authorization`; unauthenticated → 401. Weave restates. Product QA Sec confirm MET pt1 (supporting). Spec/DevPlan/SD docs state #5 principal required; deny without private-field leak. |
| 2 | Negotiation list/get isolation | PR #34: `GET /negotiations` via `GetByParticipantAsync`; `GET /negotiations/{id}` party-scoped; non-party → 404. Weave + Spec/DevPlan/SD verify cite owner/party query-plane. Product QA Sec confirm MET pt2. |
| 3 | Offer list/get isolation | PR #34: offer list/get party via parent negotiation join; accept/decline/counter party-scoped; stranger deny. Weave + Spec/DevPlan/SD. Product QA Sec confirm MET pt3. |
| 4 | Artifact list/get isolation | PR #34: list via `GetByOwnerAsync`; get via `GetByIdForOwnerAsync` (owner-scoped, not fetch-then-filter); IDOR → 404. Align #4. Weave + Spec/DevPlan/SD. Product QA Sec confirm MET pt4. |
| 5 | Deny-body / empty-list hygiene | PR #34 + tests `StageAFailClosedTests.cs`: empty authorized list → `[]` (no foreign rows); deny bodies omit other Participants’ private fields. Weave restates. Product QA Sec confirm MET pt5. |
| 6 | Complement #31, don’t replace party rules | Weave + checklist: #32 = owner/party **query-plane** list isolation; **#31 Field ACL OUT** of this Story’s delivered registry / projection. Cross-ref only; soft INDEX parallel does not merge Doc tracks. Product QA Sec confirm MET pt6. |
| 7 | No Stage B/C inventing | Weave Constraints + Spec/DevPlan: no Strategy list ACL, agent hard-wall, Cognito/SSO, or MM/DC4 as #32 delivered. Stage B/C HOLD. Product QA Sec confirm MET pt7. |
| 8 | Cross-story non-merge | Weave + INDEX: #32 Doc track separate from #31; harden/consume cross-refs to #4–#7 only; #7 stub unchanged; no rewrite of #31 deliverables under this weave. Product QA Sec confirm MET pt8. |
| 9 | Cost / spend | Docs affirm PoC **$0**; no IdP/vault provision as delivered. Weave: PoC $0. Product QA Sec confirm MET pt9. |
| 10 | Handshake close | **PASS.** Security QA `doc-qa-confirm` + Senior Security `doc-points-review` landed — all 10 MET. Docs QA may overall-PASS. |

## Indexed Product QA Security (Sec10 for Product QA step — already closed; not overall Doc PASS)
- `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-checklist.md`
- `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-points-review.md` (present on disk; indexed — not invented)
- `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-qa-confirm.md` (**PASS**; all 10 MET — Sec10 for Product QA only)

## Indexed Doc Security handshake (clears HOLD)
- Binding checklist (ISSUED): `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-checklist.md`
- Senior Security points-review: `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-points-review.md` — **PASS** 10/10
- Security QA confirm: `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-qa-confirm.md` — **PASS** 10/10 (clears overall Doc HOLD)

## PASS
**Overall Doc-step #32 PASS** — Security QA confirm cleared HOLD (pts 1–10 MET). Primary surfaces: PR #34 README/API list fail-closed + this weave. Mirror: https://github.com/ioaikh/dealoware/pull/36. Separate from #31; Stage B/C HOLD; #24 HOLD; PoC $0; CQ no-refactor; no MotorMarket; #7 stub unchanged.
