# Security checklist — Doc · MVP Stage C #66 Thin Strategy-driven AI Assistant runtime (X1 thin)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.
**Date:** 2026-09-28
**Author:** Dealoware Chief Security
**Story:** https://github.com/ioaikh/dealoware/issues/66 · Thin Strategy-driven AI Assistant runtime (X1 thin)
**Parent:** #18 · Option A · Stage C named slice
**Siblings:** #67 (hard wall — **mandatory bind only**; already Product QA + Doc-checklist tracks separate) · #68 · #69 — **OUT** of this Story (keep separate Doc; cross-ref only)
**Impl PR:** https://github.com/ioaikh/dealoware/pull/79 MERGED @ `199125a`
**Product QA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-productqa-qa-confirm.md`
**Product QA handshake SoR:** PR **#91** @ `3400480` (Product QA SoR ≠ Doc)
**Product QA report SoR:** PR **#89** @ `460a648`
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-sd-qa-confirm.md`
**Tests:** `tests/Dealoware.Api.Tests/StageCThinAssistantTests.cs` (28)
**Hold:** Soft Soft CLOSE Soft HOLD treat #66 Doc as PASS until handshake SoR MERGED + INDEX. Soft Soft CLOSE Soft HOLD status:done until CBA. Soft **#41** → **#66+#67** under wall (not Stage B). Gate **#26** backlog. Gate **#27** HOLD. **#67/#68/#69** OUT of this Story (bind wall only). Parent #18 framing-only. PoC **$0**; no MotorMarket/Cognito/DC4.
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-doc-checklist.md`
**Tip:** `3400480`

## Scope note
Doc must accurately describe **thin** OwnAgent-only Strategy-driven Assistant runtime **under** the agent/tool hard wall (#67). Product QA SoR ≠ Doc — this is a separate Doc-step checklist. Real points — not N/A. Do not invent requirements beyond Spec / Plan / SD / Product QA Security for #66. Soft Soft CLOSE Soft HOLD treat #66 Doc as PASS until handshake Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD Soft Soft CLOSE Soft HOLD SoR MERGED + INDEX; Soft Soft CLOSE Soft HOLD status:done until CBA.

## Itemized security points (Doc must satisfy)

1. **OwnAgent-only 1:1** — Docs state thin Assistant is OwnAgent for the owning Participant only (P6 spirit); never Counterparty / Stranger agent identity; no multi-party invent.

2. **StrategyBody via FieldPolicy** — Docs state StrategyBody consume/write only when `IFieldPolicy.Evaluate` allows OwnAgent R/W for owner context; no foreign Participant StrategyBody leak.

3. **Mandatory bind to #67 hard wall** — Docs state runtime on platform tools / gateway path only (no raw DB / arbitrary internal HTTP); reject prompt-only soft wall as sole control; consume of #67 allowlist + the same `IFieldPolicy` scrub.

4. **No LoginEmail in agent context** — Docs state LoginEmail stripped/denied from model / agent context packs, tools, capabilities, and errors; LoginEmail remains User-only (distinct from ContactEmail).

5. **Authn / IDOR fail-closed** — Docs state unauthenticated → 401; wrong principal / cross-tenant → 403 or 404; uniform deny; no private-field leakage.

6. **Soft #41 OUT closed by delivery path only** — Docs state soft #41 Assistant OUT closes by #66 + #67 under wall — not a Stage B Assistant claim.

7. **OUT locked (X1 thin) + one Dealoware API** — Docs state X1 MVP thin only (fuller / free-form / A5 / BYO OUT). Multi-provider clients, if present, are interchangeable vs one Dealoware API — no provider-specific invent in #66; same wall + same scrub; no provider bypass.

8. **Sibling / Gate HOLDs** — Docs state #67/#68/#69 remain OUT of this Story (bind wall only); Gate #26 backlog; Gate #27 HOLD; no Cognito/MotorMarket/DC4/vault invent; Soft OTel/audit/idempotent = weave only (no 5th Story).

9. **Cost / spend** — Docs state PoC $0; any named LLM/API spend → COO → CEO.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points. Parent #18 framing-only does not Field-capture #66.

## Handshake next
Senior Docs weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
