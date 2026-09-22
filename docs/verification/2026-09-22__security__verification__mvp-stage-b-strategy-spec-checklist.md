# Security checklist — Spec · MVP Stage B #41 Minimal Strategy create/edit (P3)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/41 · Minimal Strategy create/edit (P3 partial)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #42 — keep separate Spec; cross-ref only  
**Architecture:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` (StrategyBody row)  
**Baselines:** Stage A #31 Field ACL CLOSED (FieldPolicy registry exists); #32 CLOSED; gate #24 CA PASS  
**Hold:** Gate **#25** backlog. Stage C Assistant hard wall (#26) + #27 + #18 Spec/SD HOLD. Thin Assistant runtime **out** of this Story. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-spec-checklist.md`

## Scope note
Spec-binding for **minimal** Strategy CRUD (create/edit/get/list-own) with **StrategyBody** FieldClass ACL: Owner + OwnAgent only — never Counterparty / Stranger / Unauth. Real points — not N/A. Do not invent Assistant runtime, full free-form engine, or Stage C.

## Itemized security points (Spec must bind)

1. **Owner-scoped query plane** — Spec binds Strategy create/edit/get/list-own to **owner-scoped** query plane (never “filter in UI only”); cross-tenant Strategy list/get and IDOR → fail-closed.

2. **StrategyBody FieldClass ACL** — Spec binds **StrategyBody** via Stage A `IFieldPolicy`: **User** Read/Write; **OwnAgent** Read/Write when acting for owner; **Counterparty Deny**; **Stranger / Unauth Deny**.

3. **Never to counterparty** — Spec requires counterparty views of a shared 1:1 Negotiation **never** expose StrategyBody / private Strategy fields (negotiation-scoped DTOs only).

4. **Authn fail-closed** — Unauthenticated → **401**; wrong principal → **403** or **404** (Spec may keep PoC consistency); uniform deny bodies with **no** private-field leakage.

5. **OwnAgent = API policy row only** — Spec allows OwnAgent StrategyBody when acting for owner as **API policy** — does **not** deliver Assistant / tool runtime (Stage C / X1 out).

6. **Consume #31, don’t rewrite** — Spec adds StrategyBody policy rows / enforcement on #31 registry; does not rewrite #31 or #32 Stories except consume/cross-ref.

7. **No Stage C / Assistant inventing** — Spec does not invent thin/full Assistant runtime, agent hard wall (#26), Cognito/SSO, or MM/DC4.

8. **OUT locked** — Spec documents **P3 minimal** CRUD; full free-form Strategy conditions / evaluation → **V1**; Strategy sandbox (**A5**) → **V4**; thin Assistant → Stage C / **X1**; gate **#25** backlog.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision required to accept this Spec.

10. **Traceability + handshake** — Spec cites #41 AC + #18 Option A Stage B only. Spec QA must **not** PASS until Security QA confirms. Keep #40/#42 separate.

## Handshake next
Senior Spec weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Spec.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
