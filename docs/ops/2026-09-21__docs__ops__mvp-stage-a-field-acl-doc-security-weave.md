# Doc step weave — Security checklist pts 1–10 · MVP Stage A Field ACL registry + API projection (#31)

| Field | Value |
|-------|-------|
| **Status** | **Overall Doc step #31 PASS** — Security QA cleared HOLD (doc-qa-confirm; all 10 MET) |
| **Date** | 2026-09-21 |
| **Author** | Dealoware Senior Docs |
| **Story** | GitHub issue #31 · Field ACL registry + API projection (FieldClass / IFieldPolicy) |
| **Issue** | https://github.com/ioaikh/dealoware/issues/31 |
| **PR** | https://github.com/ioaikh/dealoware/pull/37 (**MERGED**) — Field ACL domain + Profile API projection = primary Doc surface for Security score (main `fb47fdd3…`) |
| **Checklist (binding, ISSUED)** | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-doc-checklist.md` |
| **DOC-FLOW** | `ops/2026-09-21__docs__ops__mvp-stage-a-field-acl-doc-security-weave.md` |
| **Constraints** | Stage A only · CQ no-refactor · **#32 OUT** of this Doc weave (cross-ref only; #32 CLOSED) · Soft DisplayName non-blocking · Stage B/C HOLD · gate **#24** HOLD · PoC **$0** · #7 stub unchanged · no inventing Stories · no MotorMarket / MM/DC4 · no Cognito/SSO as delivered |

## Primary surfaces Security will score
1. Merged PR #37 — Field ACL registry (`FieldClass` / `IFieldPolicy` / `FieldPolicy`) + Profile API projection (`GET`/`PATCH /profile`; deny omit; authn fail-closed) on main `fb47fdd3…` / CI SUCCESS (Actions run 35670498420)
2. This Doc weave (`ops/2026-09-21__docs__ops__mvp-stage-a-field-acl-doc-security-weave.md`) + living `INDEX.md` annotations for #31 paths

Supporting (not primary score surface): KB Product QA report `qa/2026-09-21__qa__qa-report__mvp-stage-a-field-acl-registry.md` (**PASS**; Sec10 closed via productqa-qa-confirm — **not** overall Doc PASS); Product QA Security trio; Spec / DevPlan / SD verify + Security trios; Doc checklist (**ISSUED**). SA architecture/verify with `field-acl-list-failclosed` in name is **cross-ref only** (shared Stage A brief with #32 — **#32 OUT** of this weave score surface).

## Explicit separations (non-merge)
- **#32 Account list fail-closed** — **OUT** of this Doc weave score surface; CLOSED separate track; cross-ref only. Checklist pt 8: do not rewrite #32 deliverables under this weave.
- **Soft DisplayName** — non-blocking (CPM); docs may note starter/example without gating overall Doc.
- **Stage B/C** — HOLD (no Strategy ACL; no ContactEmail share-after-Accept; no agent/tool hard-wall inventing).
- **Gate #24** — HOLD (not opened).
- **#7** identity-seal stub — unchanged (Accept = state-only).
- **PoC $0** — no IdP/vault provision as delivered by #31 Docs.

## Point → how Doc artifacts satisfy

Mapped strictly to `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-doc-checklist.md` (pts 1–10). Handshake files indexed after Security PASS (not invented).

| # | Security point | Doc satisfaction (cites) |
|---|----------------|--------------------------|
| 1 | API/DB scope only | Weave Constraints + Spec/DevPlan/SD/Product QA: FieldClass registry + `IFieldPolicy` on API/DB projection (`ProfileMapper`, `/profile`); agent hard-wall **impl** Stage C HOLD. Product QA Sec confirm MET pt1 (supporting). |
| 2 | Open-ended FieldClass registry | PR #37 / Spec / Product QA AC1: extensible `FieldClass` (`Custom(name)`); LoginEmail/ContactEmail/DisplayName starters/examples — not exhaustive closed set. Weave restates. Product QA Sec confirm MET pt2. |
| 3 | Deny-by-default | PR #37 tests `FieldPolicy_UnknownFieldClass_DeniedByDefault`; Spec/DevPlan/SD/Product QA cite unknown → deny. Weave restates. Product QA Sec confirm MET pt3. |
| 4 | LoginEmail User-only (API) | PR #37 / Product QA AC4: LoginEmail not projected to counterparty, stranger, or OwnAgent via API/DB; User R/W only. Weave + Spec/DevPlan/SD. Product QA Sec confirm MET pt4. |
| 5 | ContactEmail rules (no share path) | PR #37 / Product QA AC5: OwnAgent Read / counterparty Deny on API; ShareOutbound Deny until Stage B; **#7** stub unchanged. Weave restates. Product QA Sec confirm MET pt5. |
| 6 | Authn fail-closed on projection | PR #37 Profile endpoints: protected projection requires #5 principal; unauth → 401; write deny → 403 ProblemDetails without private fields. Weave + Product QA AC6. Product QA Sec confirm MET pt6. |
| 7 | No Stage B/C inventing | Weave Constraints + Spec/DevPlan: no Strategy ACL, agent hard-wall, Cognito/SSO, or MM/DC4 as #31 delivered. Stage B/C HOLD. Product QA Sec confirm MET pt7. |
| 8 | Cross-story non-merge | Weave + INDEX: **#32 OUT**; harden/consume cross-refs to #4–#7 only; #7 stub unchanged; no rewrite of #32 deliverables under this weave. Product QA Sec confirm MET pt8. |
| 9 | Cost / spend | Docs affirm PoC **$0**; no IdP/vault provision as delivered. Weave: PoC $0. Product QA Sec confirm MET pt9. |
| 10 | Handshake close | **PASS.** Security QA `doc-qa-confirm` + Senior Security `doc-points-review` landed — all 10 MET. Docs QA may overall-PASS. |

## Indexed Product QA Security (Sec10 for Product QA step — already closed; not overall Doc PASS)
- `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-checklist.md`
- `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-points-review.md` (present on disk; indexed — not invented)
- `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-qa-confirm.md` (**PASS**; all 10 MET — Sec10 for Product QA only)

## Indexed SD Security (SD step — already closed; not overall Doc PASS)
- `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-checklist.md`
- `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-points-review.md` (present on disk; indexed — not invented)
- `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-qa-confirm.md` (**PASS**; all 10 MET — SD Sec only)

## Indexed Doc Security handshake (clears HOLD)
- Binding checklist (**ISSUED**): `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-doc-checklist.md`
- Senior Security points-review: `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-doc-points-review.md` — **PASS** 10/10
- Security QA confirm: `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-doc-qa-confirm.md` — **PASS** 10/10 (clears overall Doc HOLD)

## PASS
**Overall Doc-step #31 PASS** — Security QA confirm cleared HOLD (pts 1–10 MET). Primary surfaces: PR #37 Field ACL API projection + this weave. Mirror: https://github.com/ioaikh/dealoware/pull/39. **#32 OUT** (cross-ref only); Soft DisplayName non-blocking; Stage B/C HOLD; #24 HOLD; PoC $0; CQ no-refactor; no MotorMarket; #7 stub unchanged.
