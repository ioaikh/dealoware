# Security checklist — Architecture (SA) · Stage C + #18 Spec unlock (agent hard wall + thin Assistant + A8-min + X2)

**Status:** Chief Security itemized points for Architecture step (handshake per `ops/ORG-OPS.md`). Issue **before** Architecture QA PASS.  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · CEO Stage C + #18 Spec unlock 2026-09-28  
**Deliverable:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md`  
**Binding tip:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` — CEO ACCEPTED Option A (§3b Platform hard wall + dual wall for **all** FieldClasses)  
**Prior gates:** #24 / #25 CLOSED · Stage A/B on `main` @ `ca827a2` (#31+#32+#40+#41+#42)  
**Soft carry-in:** Soft **#41 Assistant OUT** (OwnAgent = API policy only in Stage B) → Stage C must deliver agent/tool hard wall + thin Assistant under it  
**Hold:** Gate **#26 / SA-REV-MVP-C** stays **backlog until after Stage C delivery** (do **not** open now). Gate **#27** HOLD. Spec/eng unlock is **CA PASS** path (not Security inventing Story IDs). No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-checklist.md`

## Scope note

Architecture **delta** for Stage C / #18 remainder must bind agent/tool plane + thin Assistant + A8-minimum + X2 bot/UI to Option A tip. Real points — not N/A. This checklist is for the **Architecture options / Spec-unlock SA step**, not Gate #26 post-delivery review. Soft #41 Assistant OUT is **not** claimed closed by Stage B delivery.

## Itemized security points (Architecture brief must answer)

1. **Agent/tool hard wall = defense #2 on same FieldPolicy (§3b)** — Brief picks separate agent runtime gateway + per-Agent tool allowlist (deny-by-default) + **server-side** scrub of every tool response / context pack via the **same** Domain `IFieldPolicy.Evaluate` as the API/DB wall. Prompt-only / system-prompt soft guidance is **rejected** as sole control. Parallel agent ACL tables that drift from FieldPolicy are **rejected**. Dual wall applies to **all** FieldClasses (open-ended registry; CEO examples ≠ exhaustive).

2. **No LoginEmail in agent context** — Brief confirms no LoginEmail tool and LoginEmail never enters model / agent context packs; LoginEmail remains User-only (OwnAgent Deny held from Stage A policy rows); distinct from ContactEmail.

3. **ShareOutbound tools Accept-gated; prompt cannot escalate** — Brief confirms share tools (e.g. ContactEmail ShareOutbound or equivalent) require `HasAcceptGrant` (or Stage B AcceptGrant record) **server-side**; deny regardless of prompt text. Prompt injection cannot grant FieldClasses Evaluate denies.

4. **Cross-agent / mediated exfil posture** — Brief confirms cross-agent messaging (if any in Stage C scope) is mediated so payloads cannot include denied FieldClasses; agent↔agent exfil and prompt-injection threat rows from Option A / CEO are addressed by gateway + scrub, not by trust in model behavior.

5. **Thin Assistant OwnAgent-only under the wall (closes soft #41 OUT)** — Brief confirms thin Strategy-driven Assistant (X1 MVP partial) is OwnAgent-only (owner Participant); consumes StrategyBody only when Evaluate allows OwnAgent R/W for owner context; runs **behind** the §3b gateway. Soft **#41 Assistant OUT** closes only by delivering this wall + thin Assistant under it — **not** by claiming Assistant/tool runtime was delivered in Stage B. Fuller free-form / multi-party Assistant → **DEFER V1**.

6. **A8-minimum meters + hard cutoff fail-closed** — Brief confirms per-Participant meters for billable/LLM-touching Assistant (and any Product-named Stage C metered surface without inventing extras) plus **hard cutoff** budgets: further spend-path calls **fail closed** at budget (no soft “please stop”). Mature metering / owner cost UI → **DEFER V3**. Any named LLM/API spend → escalate **COO → CEO**; do not provision. PoC **$0**.

7. **X2 bot/UI must not bypass walls** — Brief confirms one first-party bot and/or basic UI (X2 MVP partial) exercises Authenticated Participant flows under existing auth + FieldPolicy and **must not** bypass the API wall or the agent/tool hard wall (no privileged back doors). OpenAPI/webhooks breadth → **DEFER V1**; MCP breadth → **DEFER V5**.

8. **Consume Stage A/B tip; do not rewrite Option A** — Brief consumes Field ACL + list fail-closed + discovery scrub + StrategyBody ACL + ContactEmail ShareOutbound-after-Accept + Gates #24/#25 PASS; binds Stage C delta to Option A without rewriting accepted dual-wall / §3b tip; does not merge Stage A/B named slices into one invented surface.

9. **No inventing / no spend / no MM / Gate #26 HOLD** — Brief must **not** claim Cognito/SSO/IdP, mature vault/KMS microservice, App Runner greenfield, MotorMarket/DC4, fuller Assistant as MVP-delivered, Gate **#26** open/unlock now, Gate **#27** unlock, or invent Stage C Story IDs (PM/CPM owns IDs). Hosting stay sketch-only (ECS Express `open`; App Runner excluded). Cost/critical → COO → CEO. PoC **$0**.

10. **Traceability + handshake** — Brief cites Option A tip (§3b + Stage C row) + CEO Stage C unlock + Stage A/B `main` @ `ca827a2` + soft #41 OUT carry-in; maps residuals to Spec HOLD until CA PASS, Gate #26 backlog until delivery, or CEO escalation (no guessing). Architecture QA must **not** PASS until Security QA confirms these points.

## Handshake next

1. Senior Architect answers points in `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` § Security answers (cite sections / evidence).
2. Senior Security reviews → done-list to Security QA.
3. Security QA PASS to Chief Security (or further instructions).
4. Chief Security PASS/HOLD to CPM + Chief Architect (Spec HOLD until CA PASS; Arch QA HOLD until Security QA).

## Cost/critical

No AWS / IdP / LLM spend without COO → CEO. PoC **$0**. Cost/critical → COO → CEO.
