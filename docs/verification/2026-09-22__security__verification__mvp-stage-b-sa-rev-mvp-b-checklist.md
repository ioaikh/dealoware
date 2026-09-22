# Security checklist — Architecture (SA) · Gate #25 SA-REV-MVP-B (Stage B #40+#41+#42 post-delivery)

**Status:** Chief Security itemized points for Architecture step (handshake per `ops/ORG-OPS.md`). Issue **before** Architecture QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Gate:** https://github.com/ioaikh/dealoware/issues/25 · **SA-REV-MVP-B** post-delivery review  
**Stories reviewed:** https://github.com/ioaikh/dealoware/issues/40 · https://github.com/ioaikh/dealoware/issues/41 · https://github.com/ioaikh/dealoware/issues/42  
**Parent:** #18 · Option A · Stage B named slice (as delivered)  
**Deliverable:** `architecture/2026-09-22__sa__architecture__mvp-stage-b-sa-rev-mvp-b-review.md`  
**Prior Stage A Gate #24 PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-sa-rev-mvp-a-qa-confirm.md`  
**Prior Stage B Spec→Doc Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-{discovery,strategy,contact-on-accept}-doc-qa-confirm.md`  
**Baselines:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` Stage B row · Specs `specs/2026-09-22__spec__spec__mvp-stage-b-*`  
**Delivery evidence:** PR #53 (#40 discovery) MERGED @ `767ab29e…` · PR #51 (#41 Strategy) MERGED @ `43adb283…` · PR #52 (#42 Contact) MERGED @ `ac5bc136…`  
**Hold:** Agent/tool hard wall = **Stage C HOLD** (not delivered). Soft **Assistant OUT** on #41 — OwnAgent = API policy only. #18 Spec/SD (whole) HOLD. Do **not** invent Stage C unlocks as delivered. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-sa-rev-mvp-b-checklist.md`

## Scope note
Architecture post-delivery review must score **delivered** Stage B security posture on `main` (#40 instant discovery; #41 minimal Strategy CRUD + StrategyBody ACL; #42 ContactEmail ShareOutbound-after-Accept) against Option A Stage B row and Stage A Gate #24 PASS. Real points — not N/A. This gate reviews Stage B delivery — it does **not** open Stage C / #26 / #18 Spec/SD (whole). Soft Assistant OUT = OwnAgent API policy only.

## Itemized security points (Architecture review must answer)

1. **Discovery payload scrub / no secret leak (#40)** — Review confirms instant-search results omit StrategyBody / LoginEmail / ContactEmail / private lists / Strategy inventory / auth secrets; discovery ≠ #32 owner inventory; discoverable-fields-only (no new Artifact schema); authn fail-closed (unauth **401**); uniform deny / no private leakage.

2. **StrategyBody ACL owner + OwnAgent only (#41)** — Review confirms StrategyBody FieldClass is User R/W + OwnAgent R/W for owner; Counterparty / Stranger / Unauth Deny; owner-scoped query-plane (create/edit/get/list-own); IDOR fail-closed; Negotiation DTOs never expose StrategyBody / private Strategy fields.

3. **OwnAgent = API policy only (soft Assistant OUT)** — Review confirms OwnAgent StrategyBody R/W is **API policy only** — no Assistant / tool runtime delivered; Stage C agent/tool hard-wall **not** claimed as delivered.

4. **ContactEmail ShareOutbound-after-Accept only (#42)** — Review confirms ContactEmail ShareOutbound only with Accept grant (`HasAcceptGrant` or equivalent) to authorized counterparty; Deny before Accept; policy rows: User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny.

5. **LoginEmail never shared** — Review confirms LoginEmail remains User-only; never shared on Accept; distinct from ContactEmail; not projected via discovery, Strategy DTOs, or Accept share.

6. **Seal until Accept + #7 extend-only (#42)** — Review confirms Neg/Offer omit counterparty contact PII until Accept; PoC **#7** identity-seal **extended** (no history rewrite; no regression of stub-as-delivered); Accept persists grant for FieldPolicy.

7. **Fail-closed IDOR / unauth (all three)** — Review confirms unauth **401**; wrong principal **403**/**404**; stranger deny (including ContactEmail post-Accept for non-party); uniform deny; no private leakage across #40/#41/#42 surfaces.

8. **Consume Stage A / Option A; do not merge or invent** — Review confirms Stage B consumes #31 Field ACL + #32 list fail-closed + Gate #24 PASS; #40/#41/#42 remain complementary named slices (not one merged surface); Stage A posture not rewritten.

9. **No inventing / no spend / no MM** — Review must **not** claim agent/tool hard-wall, thin/full Assistant, Cognito/SSO/IdP, mature vault, Stage C share-tool, saved-search, A1/A5, or MM/DC4 as Stage B–delivered; PoC **$0**; cost/critical → COO → CEO.

10. **Traceability + handshake** — Review cites Option A Stage B row + Gate #24 PASS + `main` evidence (PRs #53/#51/#52, Spec→Doc Security PASS paths) per area; maps residual gaps to Stage C HOLD or CEO escalation (no guessing). Architecture QA must **not** PASS until Security QA confirms these points.

## Handshake next
1. Senior Architect answers points in the SA-REV-MVP-B review deliverable (cite sections / evidence).
2. Senior Security reviews → done-list to Security QA.
3. Security QA PASS to Chief Security (or further instructions).
4. Chief Security PASS/HOLD to CPM + Chief Architect.

## Cost/critical
No AWS / IdP spend. Cost/critical → COO → CEO.
