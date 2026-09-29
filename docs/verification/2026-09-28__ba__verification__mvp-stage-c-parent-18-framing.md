# BA business verification — Story #18 Participant data isolation (Option A remainder — parent framing / map / verify ONLY)

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-28 (BA verify executed ~10:52 PM ET)  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/18  
**Framing PR:** https://github.com/ioaikh/dealoware/pull/111 (**MERGED** @ main `7e7731e12f9e41f9e5b554a9aeccdfbed70b81ed`; framing / map / verification docs only — no product `.cs`)  
**Overall Doc PASS handshake Soft Soft CLOSE Soft HOLD SoR:** PR **#131** @ `65dd1afe2add40582a5d2ccb1c0a6229c8bc446e` (points-review + qa-confirm + INDEX)  
**Tip (binding Doc handshake):** `65dd1af` · Soft tip ahead (process note): main `5435de8` = INDEX Overall Doc PASS weave annotation (+1; docs-only)  
**CQ:** `cq:no-refactor` — assessment `specs/2026-09-28__cq__assessment__mvp-stage-c-parent-18-framing-no-refactor.md`  
**Method:** Business-intent AC check from Story #18 body + Spec §8 / §8.1 + Product QA report (6 MET; Sec 1–10 PASS) + framing SD evidence PR #111 @ `7e7731e` (gh remote + KB / docs-mirror reads; **no clone**; **not** code review; **not** Field-capture). Map 6/6 AC themes to locus = Stage A/B tip CLOSED @ `ca827a2` + Stage C named slices **#66–#69** cite-only. **BIND #67** without Field-capture. Soft **#41** CLOSED via **#66+#67**. Do **not** re-score #66–#69. Soft HOLD multi-provider · Gate **#26** backlog · Gate **#27** HOLD. Soft Soft CLOSE Soft HOLD `status:done` until **CBA PASS**. PoC **$0**. No MotorMarket/DC4. Do **not** CLOSE issue or flip `status:done` from this step.

## Binding AC themes (issue #18 / Spec §8 — do not invent)

1. Strategy documents readable/writable **only** by owning Participant (and OwnAgent acting for them) — never counterparties / third Participants  
2. List/search of negotiations, offers, account-scoped history returns **only** records that Participant is authorized to see (party to, or owner)  
3. Counterparty views of a shared 1:1 Negotiation expose **only** negotiation-scoped fields allowed by product rules — not private Strategy / full account / unrelated  
4. Unauthenticated and wrong-principal callers receive deny (401/403) with **no** private-field leakage in error bodies  
5. Automated tests (or equivalent): owner OK; counterparty cannot read Strategy; stranger cannot list another’s negotiations/offers; cross-tenant IDOR fail  
6. Documented as distinct from identity-seal / contact-on-accept (**#7** / **P7** / **A9**)

## AC checklist (map / verify ONLY — 6/6 via locus)

| # | AC theme | Verdict | Tip locus (CLOSED @ `ca827a2`) | Stage C locus (named slices — cite only; do **not** re-score) |
|---|----------|---------|--------------------------------|---------------------------------------------------------------|
| 1 | Strategy R/W owning Participant (+ OwnAgent) only — never counterparties / third | **PASS** | #41 Strategy CRUD + StrategyBody FieldPolicy | #66 under #67 wall closes soft #41 OUT (framing Step 5 / Spec Locked #5/#6; Product QA AC1 MET) |
| 2 | List/search party-/owner-scoped only | **PASS** | #32 account list fail-closed; #40 discovery scrub | #69 must not bypass (framing Step 3 / Spec §1; Product QA AC2 MET) |
| 3 | Counterparty negotiation-scoped fields only — not private Strategy / full account / unrelated | **PASS** | #41 never StrategyBody to counterparty; #42 ShareOutbound-after-Accept; #31 Field ACL | #67 share tools Accept-gated; scrub before agent context — **BIND #67**; no Field-capture (framing Step 3–4 / Spec Locked #4/#8; Product QA AC3 MET) |
| 4 | Unauth / wrong-principal deny (401/403); no private-field leakage | **PASS** | #31/#32/#40/#41/#42 fail-closed | Same invariants on #67/#66/#68/#69 planes (framing Step 3 / Spec §2; Product QA AC4 MET) |
| 5 | Automated tests: owner OK; counterparty Strategy deny; stranger list deny; IDOR fail | **PASS** | Stage A/B Spec §7.1 CLOSED | Stage C named-slice Spec §7.1 / §8.1 / §9.1 loci (framing Step 6 / Spec §8.1) — tests remain on tip + named slices; parent does **not** reinvent (Product QA AC5 MET) |
| 6 | Distinct from identity-seal / contact-on-accept (#7 / P7 / A9) | **PASS** | Documented in Stage A/B Specs | Restated Spec Locked #7; framing Step 8 / Spec §6 OUT; issue #18 Context (Product QA AC6 MET) |

**AC count:** **6 PASS / 0 FAIL** (via framing map + Spec §8 + Product QA 6 MET; no soft AC gaps that block map/verify).

## Out of scope held

| OOS item (Story #18 + locks) | Held? | Evidence |
|------------------------------|-------|----------|
| Field-capture / re-score sibling delivery **#66–#69** | **Yes** | Framing-only PR #111 (docs map/verify); Product QA / CQ / Framing Step 4–8: cite/BIND only; Soft HOLD re-score #66–#69 |
| Soft HOLD Field-capture invent (any product `.cs` under #18) | **Yes** | PR #111 = docs only (+264 framing/map); CQ `cq:no-refactor`; no product code under parent |
| Merging #66/#67/#68/#69 into one mega-plan / mega-Spec | **Yes** | Spec Locked #3/#4; framing Step 4 complementary plans only |
| Claiming Stage A/B delivered agent/tool hard wall | **Yes** | Spec Locked #2/#5; framing Step 2 — A/B tip ≠ agent wall; BIND **#67** |
| Soft **#41** Assistant OUT claimed as Stage B delivery | **Yes** | Soft #41 CLOSED via **#66+#67** under wall only — not Stage B (framing Step 5; Spec Locked #5) |
| Rewriting Option A tip / inventing new FieldClasses / product | **Yes** | Spec Locked #0/#1; framing Step 2/8; Product QA Sec pt2/8 |
| PoC identity-seal stub rewrite (**#7**) | **Yes** | Spec Locked #7; issue OOS; AC6 PASS distinct |
| Unlocking gate **#26** before Stage C delivery; gate **#27** HOLD | **Yes** | Spec Locked #10; framing Step 8; Product QA Sec pt7; Soft LOCK |
| Soft HOLD multi-provider Spec/doc rewrite | **Yes** | Product QA / CPM Soft HOLD until BM multi-provider start |
| Cognito/SSO/IdP; mature vault/KMS; MCP; fuller Assistant as MVP; MotorMarket/DC4; 5th OTel Story | **Yes** | Spec §6 OUT; framing Step 8–9; Product QA Sec pt8–9; PoC $0 |
| Soft Soft CLOSE Soft HOLD `status:done` flip from this step | **Yes** | Soft HOLD until **CBA PASS** after BAQA; CBA OPEN comment; labels remain `status:ready-for-ba-verify` |
| Closing issue #18 from this step | **Yes** | Do **not** CLOSE — framing Soft Soft CLOSE Soft HOLD BA-verify chain only |

## Soft gaps (non-blocking for BA business verify)

- Framing-only BA verify — no product code under #18; evidence is map/verify of tip + named-slice loci (accepted scope; matches Product QA / CQ / SD framing).
- Soft tip ahead of binding Doc handshake `65dd1af`: main `5435de8` = INDEX Overall Doc PASS weave annotation only (+1 docs) — process note; non-blocking.
- KB twin for SD framing under `verification/` was absent at BA verify start — GitHub `docs/verification/2026-09-28__sd__verification__mvp-stage-c-parent-18-framing.md` **HTTP 200** (PR #111 @ `7e7731e`); used docs-mirror + gh + Product QA/Spec KB. If twin still missing in Doc Team publish path later → note Chief Docs, **not** reopen BA.
- Sibling Product QA (#66/#67/#68/#69) already PASS CLEAR — cite only; Soft HOLD re-score (do **not** re-score from parent).
- Soft Soft CLOSE Soft HOLD Overall Doc PASS LOCKED (triad #129/#130/#131 @ `65dd1af` + INDEX weave `5435de8`); Soft Soft CLOSE Soft HOLD Product QA PASS LOCKED (report #128 @ `512663e`; handshake #126 @ `1501616`; checklist #125 @ `dcc503b`).
- Soft Soft CLOSE Soft HOLD `status:done` until CBA PASS after BAQA — do **not** flip from this step.
- Soft HOLD multi-provider · Soft **#41** CLOSED via **#66+#67** · Gate **#26** backlog · Gate **#27** HOLD · BIND **#67** without Field-capture · PoC **$0**.

## Prior gate chain (evidence, not re-scored here)

| Gate | Result | Path / link |
|------|--------|-------------|
| Spec QA | PASS (prior) | Spec `specs/2026-09-28__spec__spec__mvp-stage-c-participant-isolation-option-a-remainder.md` + Spec Security confirm 10/10 |
| Dev Plan QA | PASS (prior) | DevPlan Security confirm 10/10 — `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-devplan-qa-confirm.md` |
| SD / framing Dev Code QA | PASS | `docs/verification/2026-09-28__sd__verification__mvp-stage-c-parent-18-framing.md` (PR #111 @ `7e7731e`) |
| SD Security | PASS 10/10 | `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-sd-qa-confirm.md` · handshake Soft Soft CLOSE Soft HOLD SoR #114 @ `32e97d2` |
| CQ | `cq:no-refactor` | Framing PR #111; assessment `specs/2026-09-28__cq__assessment__mvp-stage-c-parent-18-framing-no-refactor.md` |
| Product QA | PASS — Spec §8 / AC **6 MET**; Sec **1–10 PASS** | `qa/2026-09-28__qa__qa-report__mvp-stage-c-participant-isolation-option-a-remainder.md` |
| Product QA Security | PASS 10/10 | points-review + productqa-qa-confirm Soft Soft CLOSE Soft HOLD SoR #126 @ `1501616`; checklist #125 @ `dcc503b`; report Soft Soft CLOSE Soft HOLD SoR #128 @ `512663e` |
| Overall Doc | PASS LOCKED | Doc triad Soft Soft CLOSE Soft HOLD SoR #129 @ `97f6cd3` · #130 @ `e6d6397` · #131 @ `65dd1af` (+ INDEX weave `5435de8`) |
| Stage A/B tip wall | CLOSED @ `ca827a2` | #31/#32/#40/#41/#42 — cite locus only |
| Stage C named slices | Cite / BIND only | #66/#67/#68/#69 — **do not re-score**; Soft #41 CLOSED via #66+#67; BIND #67 as wall |

## Recommendation to CBA

**PASS** — parent #18 framing / map / verify meets Story #18 business AC themes **6/6** via locus map (Stage A/B tip CLOSED @ `ca827a2` + Stage C named slices cite-only): Strategy R/W owner+OwnAgent only; list/search party-/owner-scoped; counterparty negotiation-scoped only; unauth/wrong-principal deny no leakage; automated-test locus (owner OK / counterparty Strategy deny / stranger list deny / IDOR fail); distinct from #7 identity-seal. Framing PR #111 @ `7e7731e` docs-only; Product QA 6 MET + Sec 1–10 PASS; Overall Doc PASS LOCKED @ handshake #131 `65dd1af`. OOS held (Soft HOLD re-score #66–#69; Soft HOLD Field-capture invent; Soft #41 CLOSED via #66+#67; BIND #67 without Field-capture; multi-provider HOLD; #26 backlog; #27 HOLD; no Cognito/vault/MCP/MM/DC4/5th Story; PoC $0). Soft gaps non-blocking. Hand to BAQA for verify-QA; Soft Soft CLOSE Soft HOLD `status:done` until **CBA PASS** after BAQA. Do **not** CLOSE issue #18 from this step, do **not** flip `status:done`, do **not** unlock #26/#27, do **not** Field-capture or re-score #66–#69, do **not** invent product under parent. Framing / map / verify ONLY. PoC $0.
