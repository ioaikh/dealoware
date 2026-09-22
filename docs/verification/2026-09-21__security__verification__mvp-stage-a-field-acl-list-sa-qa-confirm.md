# Security QA — MVP Stage A #31+#32 Field ACL + list fail-closed Architecture vs Chief Security SA checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 Architecture-step Security points MET)  
**Asked by:** Dealoware Architecture QA / Senior Security done-list  
**Chief checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-checklist.md` (10 points)  
**Senior Security done-list:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-points-review.md` (PASS; soft notes none blocking)  
**Architecture:** `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md` (§§1–6 + Security table)  
**Format ref:** `verification/2026-09-21__security__verification__mvp-participant-secrets-acl-sa-qa-confirm.md`  
**Stories:** https://github.com/ioaikh/dealoware/issues/31 · https://github.com/ioaikh/dealoware/issues/32  
**Parent:** #18 · Option A (CEO ACCEPTED) · Stage A slice  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-qa-confirm.md`  
**Constraints:** Stage A = API/DB FieldPolicy + account list fail-closed only; agent/tool hard wall **implementation** Stage C HOLD; #7 identity-seal stub unchanged; PoC **$0**; no Cognito inventing; no MM/DC4; gate #24 backlog until after Stage A delivery. **#18 Architecture DOC-FLOW already closed — not re-opened.**

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Chief Security SA checklist | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-checklist.md` | Binding 10 points |
| Senior Security points-review | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-points-review.md` | PASS all 10; soft none blocking |
| Stage A architecture | `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md` | §§1–6 bind Stage A IN/OUT; §6 maps 1–10 with cites |
| Binding #18 proposal (context only) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Option A accepted — **not re-scored** (#18 DOC-FLOW closed) |

## Independent score (Security QA)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Stage A scope lock — API/DB only; agent wall HOLD Stage C | **MET** | Header HOLD: agent/tool hard wall **implementation** Stage B/C. §1 #31 OUT agent/tool gateway; #32 OUT Stage B/C. §3a#7 no agent gateway code — tool scrubbers Stage C HOLD; dual wall accepted end-state. §4 OUT agent wall HOLD Stage C. §2 Option C rejected (agent wall now). §6#1. |
| 2 | Open-ended FieldClass registry | **MET** | §3a#1 registry open-ended (not closed enum); new classes = new FieldClass + policy rows. §3a#2 LoginEmail/ContactEmail/DisplayName starters **not exhaustive**; extras only if illustrative/derived. §1 #31 IN generic any account-info. §6#2 + binding Option A. |
| 3 | IFieldPolicy on API/DB projection; deny unknown | **MET** | §3a#3 `IFieldPolicy.Evaluate(principal, fieldClass, action, resourceContext)`. §3a#4 API/DTO projection omits denied fields. Pick A: Domain registry + Evaluate + projection + query filters. §6#3 deny-by-default unknown/unregistered (Spec must encode). |
| 4 | LoginEmail User-only (API); not OwnAgent | **MET** | §3a#5 LoginEmail User R/W; **OwnAgent Deny** on API/DB even before Stage C tools. §6#4. Agent-tool plane remains Stage C (scope lock #1). |
| 5 | ContactEmail OwnAgent vs counterparty (API); no share-after-Accept | **MET** | §3a#6 ContactEmail User R/W; OwnAgent Read allowed; ShareOutbound Deny until Stage B. §4 ContactEmail ShareOutbound-after-Accept HOLD Stage B; Expanding PoC #7 OUT. §6#5 counterparty Deny; #7 stub unchanged. |
| 6 | Negotiation list/get fail-closed | **MET** | §1 auth: party to negotiation (A or B). §3b repository/query plane constraints; unauthorized 401/403 uniform deny body — no private fields; IDOR fail-closed; stranger/unauth tests. §6#6. |
| 7 | Offer list/get fail-closed | **MET** | §1 auth: party via parent negotiation. §3b same fail-closed / no cross-account leak. §6#7. |
| 8 | Artifact list/get fail-closed owner-scoped | **MET** | §1 auth: `OwnerParticipantId` == principal `sub` (PoC intent + harden). §3b IDOR harden + owner OK / stranger deny / cross-tenant deny tests. §6#8. Aligns #4 owner model. |
| 9 | No inventing / $0 / #7 preserved | **MET** | §4 OUT: Strategy ACL HOLD B; agent wall HOLD C; Cognito/SSO Out; MM/DC4 Out; Expanding #7 stub Out. Header Cost PoC/MVP **$0**; §3c local/$0 no AWS provision. §6#9. |
| 10 | Traceability + handshake; #24 backlog | **MET** | Sources + header cite #31/#32 AC + Option A / #18 proposal only. §5 SA-REV-MVP-A (#24) trigger **after Stage A delivery** — do **not** open now. §6#10 + Done-list require Security QA confirm before Architecture QA PASS. |

## Soft notes (non-blocking)

- **Deny-default placement.** Unknown/unregistered FieldClass deny-by-default is explicit in §6#3 ("Spec must encode") more than as a numbered §3a IN bullet. Architecture-step binding is present; Spec must carry the rule — soft polish only, no HOLD.
- **Counterparty Deny wording.** ContactEmail counterparty Deny is clearest in §6#5; §3a#6 emphasizes OwnAgent Read + ShareOutbound Deny until Stage B. Same intent; Spec should name counterparty Deny explicitly — soft polish only.
- Senior soft notes: **None blocking** — Security QA agrees; no content gap.

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are Spec-handoff polish only.

## Guardrails noted

- **Stage A API/DB only** — FieldClass + IFieldPolicy projection + Neg/Offer/Artifact list fail-closed; agent/tool hard wall **impl** Stage C HOLD (dual-wall Option A end-state kept).
- **Open FieldClass** — examples not exhaustive; no invented product classes beyond CEO examples unless illustrative.
- **LoginEmail User-only / ContactEmail OwnAgent** — API policy rows now; share-after-Accept Stage B HOLD; #7 stub as-is.
- **List fail-closed** — party (Neg/Offer) / owner (Artifact); uniform deny; no private-field leak.
- **PoC $0 / no Cognito inventing / no MM** — Header Cost; §4 OUT; §3c.
- **#24 backlog** — open only after Stage A delivery moment.
- **#18 Architecture DOC-FLOW closed** — not re-opened by this Stage A confirm.

## Senior alignment

Senior Security **PASS** (all 10 MET) aligns with this independent Security QA score. Senior claims (API/DB scope + Stage C agent-wall HOLD; open FieldClass; IFieldPolicy deny-default; LoginEmail User-only; ContactEmail OwnAgent/counterparty without share-after-Accept; Neg/Offer/Artifact list fail-closed; #7 stub; $0; #24 backlog) are **supported** by Architecture §§1–6. Soft polish items above do not change PASS.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Architecture QA may proceed on the Security gate for this Stage A Architecture deliverable (still subject to Architecture QA / CA PASS). Cost/critical: none. No AWS/IdP spend to accept. Do **not** open gate #24 now.
