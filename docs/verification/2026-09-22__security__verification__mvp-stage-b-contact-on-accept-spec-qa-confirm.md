# Security QA — MVP Stage B #42 Contact on accept Spec vs Chief Security Spec checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-22  
**Verdict:** **PASS** (all 10 Spec-step Security points MET)  
**Asked by:** Dealoware Chief Security (Stage B Spec Security handshake)  
**Chief checklist (binding):** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-checklist.md` (10 points)  
**SoR (GitHub main, PR #43 MERGED):** `docs/verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-checklist.md`  
**Senior Security done-list:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-points-review.md` (**PASS** 10/10)  
**Spec:** `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md`  
**Format ref:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-qa-confirm.md` · `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/42 · Contact on accept — identity seal → contact (P7 / A9 minimum)  
**Parent:** #18 · Option A (CEO ACCEPTED) · Stage B named slice  
**Precursor:** PoC #7 identity-seal stub CLOSED — **extend**, do not rewrite  
**Siblings:** #40 · #41 — cross-ref only; separate Specs / separate confirms  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-qa-confirm.md`  
**Constraints:** P7/A9 MVP minimum; pre-Accept seal held (extend #7); ShareOutbound(ContactEmail) only after Accept; LoginEmail never on Accept; HasAcceptGrant; Gate **#25** backlog; Stage C + #18 Spec/SD HOLD; mature vault → V3; no MM/DC4; no Cognito inventing; PoC **$0**; keep #40/#41 separate.

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Chief Security Spec checklist (KB) | `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-checklist.md` | Binding 10 points |
| SoR checklist (GitHub main) | `docs/verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-checklist.md` | **Present** (PR #43 MERGED) — SoR evidence |
| Senior Security points-review | `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-points-review.md` | **PASS** 10/10 |
| Spec (#42) | `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md` | Locked #0–#10; §§1–6; §5 maps 1–10; §7 AC + §7.1 tests |

## Independent score (Security QA)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Pre-Accept seal held | **MET** | Locked #1; §1 — Negotiation/Offer APIs continue to **omit** counterparty contact PII until Accept; extend #7; **no regression**. §5 row 1. |
| 2 | Accept grant record | **MET** | Locked #2; §2 HasAcceptGrant — User Accept on that offer/negotiation → `resourceContext.HasAcceptGrant` (or equivalent) usable by FieldPolicy. §5 row 2. |
| 3 | ShareOutbound only after Accept | **MET** | Locked #3/#4; §2 ShareOutbound rules — ContactEmail to counterparty **only** when Accept recorded on **that** offer/negotiation; before Accept → ShareOutbound **Deny** for all principals. §5 row 3. |
| 4 | ContactEmail policy rows (Stage B) | **MET** | Locked #5; §3 matrix — User R/W; OwnAgent **Read** (not ShareOutbound by default); Counterparty Deny until ShareOutbound grant; Stranger / Unauth Deny. §5 row 4. |
| 5 | LoginEmail never on Accept | **MET** | Locked #6; §3 — LoginEmail remains **User-only** (OwnAgent Deny; **not** shared on Accept); do not conflate with ContactEmail. §5 row 5. |
| 6 | Authn / stranger fail-closed | **MET** | Locked #7; §2; §7.1 — unauthenticated → deny; stranger still deny ContactEmail post-Accept; uniform deny bodies; no private-field leakage. §5 row 6. |
| 7 | Extend #7 under ACL, don’t rewrite | **MET** | Locked #8; §1; §4; Constraints — seal → contact under #18 / #31 FieldPolicy; #7 CLOSED history intact; #18 Spec/SD HOLD. §5 row 7. |
| 8 | OUT locked | **MET** | Locked #9/#10; §6 OUT — P7/A9 MVP **minimum**; mature PII vault → **V3**; distinct from #31/#32; gate **#25** backlog; no Cognito/SSO/MM. §5 row 8. |
| 9 | Cost / spend | **MET** | §4 Host/cost — local/$0; ECS Express sketch only; **no AWS/IdP/vault provision** (mature vault out). Header PoC **$0**. §5 row 9. |
| 10 | Traceability + handshake | **MET** | Sources cite #42 AC + #18 Option A Stage B + #7 precursor; §5 maps 1–10; Spec QA must **not** PASS until Security QA confirms; keep #40/#41 separate. §5 row 10. Gate **#25** not opened (§6 OUT). |

## Key binds (asker) — cross-check

| Bind | Result | Cite |
|------|--------|------|
| Pre-Accept seal held; extend #7; no regression | **OK** | Locked #1; §1 |
| HasAcceptGrant for FieldPolicy | **OK** | Locked #2; §2 HasAcceptGrant |
| ShareOutbound only after Accept; Deny before | **OK** | Locked #3/#4; §2 ShareOutbound rules |
| ContactEmail User R/W; OwnAgent Read; Counterparty until grant Deny | **OK** | Locked #5; §3 |
| LoginEmail never on Accept | **OK** | Locked #6; §3 |
| Unauth / stranger fail-closed | **OK** | Locked #7; §2; §7.1 |
| Extend #7 under ACL; #18 Spec/SD HOLD | **OK** | Locked #8; §1; §4; Constraints |
| P7/A9 min; vault→V3; #25 backlog; no Cognito/MM | **OK** | Locked #9/#10; §6 OUT |
| PoC $0 | **OK** | §4; header |
| Separate from #40/#41 | **OK** | Constraints; §4; Sources |

## Soft notes (non-blocking)

- **Senior Spec-step points-review present.** Independent Security QA score **agrees** with Senior **PASS 10/10** on all points with matching Spec cites. Senior soft note on docs lag is **superseded** — SoR checklists are on GitHub main (PR #43).
- **SoR checklists unlocked.** Binding checklist cited from `docs/verification/` (GitHub main) and KB `verification/` twin; Spec scored from KB Spec + checklist evidence.
- **LoginEmail ≠ ContactEmail.** Spec §3 matrix + Locked #6 explicitly separate the two FieldClasses — aligns Chief asker; no HOLD.

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are process/docs polish only.

## Guardrails noted

- **P7/A9 MVP minimum** — mature PII vault → V3; no LoginEmail-on-Accept; no extra PII channels.
- **Pre-Accept seal** — extend #7; omit contact PII until Accept; no regression / rewrite of #7 history.
- **ShareOutbound after Accept only** — HasAcceptGrant; before Accept Deny all; stranger deny post-Accept.
- **LoginEmail User-only** — never shared on Accept; OwnAgent Deny.
- **Gate #25 backlog** — not opened by this Spec; Stage C + #18 Spec/SD HOLD.
- **PoC $0** — no IdP/vault provision; no MM/DC4.
- **Siblings separate** — #40 / #41 cross-ref only; this confirm is #42 only.

## Senior alignment

Senior Spec-step points-review **PASS 10/10** (`verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-points-review.md`). Independent Security QA score **agrees** on all 10 MET with matching Spec cites (Locked # / §1–§6 / §5 rows). No contradiction.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Spec QA may PASS Spec gate to Chief Spec after this confirm (subject to Chief clear). Cost/critical: none. No AWS/IdP spend. Do **not** open gate #25 now. Triad CLOSE / Dev Plan unlock remains Chief’s after Spec Security QA PASS. Siblings #40/#41 remain separate Specs (cross-ref only).
