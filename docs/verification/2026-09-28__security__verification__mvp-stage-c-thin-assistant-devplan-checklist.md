# Security checklist — Dev Plan · MVP Stage C #66 Thin Strategy-driven AI Assistant runtime (X1 thin)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/66 · Thin Strategy-driven AI Assistant runtime (X1 thin)  
**Parent:** #18 · Option A · Stage C named slice  
**Siblings:** #67 (hard wall — **mandatory bind**) · #68 · #69 — keep separate plan; cross-ref only  
**Order preference:** **#67 → #66 → #68 → #69 → #18** (#66 after #67)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-qa-confirm.md` (Spec Security SoR **#71+#72**; tip `main` @ `f133e90` Senior PM — Spec Soft Soft CLOSE Soft HOLD SoR CLEAR / PR #73 cited by PM for Spec)  
**Architecture:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` (§3b thin Assistant) + Option A tip  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. SD HOLD until Dev Plan QA + Security PASS. Soft **#41 Assistant OUT** closes only via **#66+#67** under wall — not Stage B claim. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate/verify** Spec Security for **thin** OwnAgent-only Strategy-driven Assistant runtime **under** the agent/tool hard wall (#67). Real points — not N/A. Must **bind #67**. Soft #41 Assistant OUT closes by delivering this runtime under #67 — not by claiming Stage B delivered Assistant. Do not invent fuller Assistant, multi-party agents, Gate #26 unlock, or a 5th Story.

## Itemized security points (Dev Plan must weave)

1. **OwnAgent-only 1:1 tasks** — Plan schedules thin Assistant as **OwnAgent** for the owning Participant only (P6 spirit); never Counterparty / Stranger agent identity; no multi-party invent; verify.

2. **StrategyBody via FieldPolicy tasks** — Plan schedules StrategyBody consume/write only when `IFieldPolicy.Evaluate` allows OwnAgent R/W for owner context; never expose another Participant’s StrategyBody; verify.

3. **Mandatory bind to #67 hard wall** — Plan requires runtime uses **platform tools / gateway path only** (no raw DB / arbitrary internal HTTP); **rejects** prompt-only soft wall as sole control; cross-ref #67 for allowlist + scrub; verify.

4. **No LoginEmail in agent context tasks** — Plan requires LoginEmail never enters model / agent context packs or tool outputs; LoginEmail remains User-only (distinct from ContactEmail); verify.

5. **Authn / IDOR fail-closed tasks** — Plan schedules unauthenticated → **401**; wrong principal / cross-tenant → **403** or **404**; uniform deny; **no** private-field leakage in responses or model context; verify.

6. **Soft #41 OUT closed by delivery path only** — Plan documents soft #41 Assistant OUT closed by **#66 + #67** delivery under wall — does **not** claim Assistant/tool runtime was Stage B–delivered; verify no Stage B Assistant claim tasks.

7. **OUT locked (X1 thin)** — Plan keeps **X1 MVP thin** only; fuller / stronger Assistant → **V1**; free-form Strategy engine → **V1**; Strategy sandbox (**A5**) → **V4**; BYO / multi-LLM breadth → later; Soft OTel/audit/idempotent = weave only (no 5th Story).

8. **Sibling / Gate HOLDs** — Plan does not invent #68/#69 surfaces into this Story; Gate **#26** stays backlog until delivery; Gate **#27** HOLD; no Cognito/SSO/MM/DC4/vault invent; keep siblings separate plans.

9. **Cost / spend** — PoC **$0**; any named LLM/API spend → escalate **COO → CEO**; do not provision spend tasks without that path.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
