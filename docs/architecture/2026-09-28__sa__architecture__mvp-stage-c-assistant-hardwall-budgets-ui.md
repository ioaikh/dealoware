# Architecture options — MVP Stage C: Agent hard wall + thin Assistant + A8-min budgets + X2 UI/bot (#18 remainder)

**Status:** Senior Architect **Architecture delta brief** for Architecture QA (CEO Stage C + #18 Spec unlock 2026-09-28 via BM). **Not** pure reuse-as-is — delta binds Stage C impl to Option A tip. **Amended:** §6 Security answers points 1–10.  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Architect  
**Brief:** Chief Architect PRIORITY — Stage C + #18 Spec SA unlock  
**Binding tip (cite; do not rewrite):** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` — **CEO ACCEPTED Option A**; especially **§3b Platform hard wall** + Stage C Story row + dual-wall binding for **all** FieldClasses  
**Tip delivery:** Stage A/B on `main` @ `ca827a2` — #31+#32+#40+#41+#42; gates **#24/#25 CLOSED**; soft **#41 Assistant OUT** held for Stage C  
**Parent:** https://github.com/ioaikh/dealoware/issues/18  
**DOC-FLOW:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md`  
**Cost:** PoC/MVP **$0** · Host: **Amazon ECS Express Mode (Fargate)** sketch (`open`) · **App Runner** excluded (`existing-customers-only` + `no-new-features`) · No MotorMarket/DC4  
**HOLD:** Spec for #18 / Stage C eng until **CA PASS** · Gate **#26 / SA-REV-MVP-C** stays **backlog** until **after Stage C delivery** · Gate **#27** HOLD · Do **not** invent Story IDs

## Sources

| Source | Role |
|--------|------|
| CA PRIORITY Stage C + #18 Spec unlock (CEO 2026-09-28 via BM) | Binding brief — Architecture **delta** |
| Option A ACCEPTED (CEO 2026-09-21) | Dual wall end-state; §3b hard wall; FieldClass open-ended; Stage C agent tool hard wall + scrubber |
| Moments SA-REV-MVP-C | `architecture/2026-09-20__sa__architecture__mvp-proposed-arch-review-moments.md` · gate **#26** |
| Roadmap | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` — MVP thin Assistant (**X1** partial); **A8-minimum** meters+hard budgets (Option 1); **X2** one bot/basic UI |
| Stage A/B delivery tip | `main` @ `ca827a2` — FieldAcl + StrategyBody + AcceptGrant ShareOutbound + discovery |
| Soft #41 Assistant OUT | Stage B OwnAgent = API policy only → Stage C delivers thin Assistant + agent wall |
| Templates | `architecture/templates/sa-architecture-options-brief-template.md` |
| Security SA checklist | `verification/2026-09-28__security__verification__mvp-stage-c-sa-checklist.md` |

---

## 1. Delta vs tip (what Stage C must add)

| Already on tip (A/B) | Stage C delta (this brief) |
|----------------------|----------------------------|
| Domain `FieldClass` + `IFieldPolicy.Evaluate` (API/DB wall) | **Agent/tool hard wall** bound to **same** Evaluate (§3b) |
| LoginEmail User-only; ContactEmail OwnAgent Read; ShareOutbound-after-Accept | **No LoginEmail in agent context**; share **tools** Accept-gated; prompt cannot escalate |
| StrategyBody User + OwnAgent API policy; Assistant runtime OUT | **Thin Strategy-driven Assistant (X1 MVP partial)** OwnAgent-only |
| — | **A8-minimum** per-Participant meters + hard cutoff budgets |
| — | **X2** one first-party bot and/or basic UI |
| Gates #24/#25 CLOSED | Gate **#26** backlog until Stage C **delivery**; #27 HOLD |

**#18 AC remainder (map — not invent Stories):** Option A §5 Stage C row “Agent tool hard wall + response scrubber” + dual-wall for all FieldClasses + CEO threat rows (prompt injection / agent↔agent exfil). A/B closed Field ACL API, list fail-closed, discovery scrub, StrategyBody ACL, contact ShareOutbound. Remaining = **defense #2** (agent/tool plane) + Product MVP surfaces (thin Assistant, A8-min, X2) that consume OwnAgent under that wall.

---

## 2. Options / tradeoffs + pick

### 2a. Agent/tool hard wall (#18 dual-wall remainder)

| Option | Pros | Cons | |
|--------|------|------|--|
| **A. Separate agent runtime gateway + tool allowlist + server-side scrub calling same `IFieldPolicy.Evaluate`** | Matches CEO Option A §3b; one policy source; prompt cannot escalate; testable | Needs gateway discipline | **Recommended** |
| B. Prompt-only / system-prompt soft guidance | Fast | **Fails** CEO dual-defense lock | **Reject** |
| C. Separate agent ACL tables parallel to FieldPolicy | Isolation theater | Drift from API wall; two sources of truth | **Reject** |
| D. Secrets microservice / vault now | Strong isolation | Invents spend/ops; PoC **$0** | **Defer** post-MVP unless CEO spend |

**Pick: A** — bind Stage C agent/tool plane to existing Domain FieldPolicy (Option A already accepted). Do **not** rewrite Option A.

### 2b. Thin Assistant (X1 MVP partial)

| Option | Pros | Cons | |
|--------|------|------|--|
| **A. OwnAgent-only thin Strategy-driven Assistant behind gateway; StrategyBody via Evaluate; no LoginEmail tool** | Matches soft #41 OUT → Stage C; roadmap X1 MVP partial | Limited capability vs V1 fuller Assistant | **Recommended** |
| B. Full free-form Assistant + multi-agent in MVP | Product stretch | Invents beyond roadmap; cost/risk | **Reject** (fuller → **V1**) |
| C. Skip Assistant; ship wall-only | Smaller | Leaves X1 MVP partial unmet | **Reject** unless Product/CEO re-scopes |

**Pick: A.**

### 2c. A8-minimum budgets

| Option | Pros | Cons | |
|--------|------|------|--|
| **A. Per-Participant meters + hard cutoff (Option 1)** | Roadmap-locked; cost-critical with any LLM path | Mature UI deferred | **Recommended** |
| B. Soft tags only / no cutoff | Under-protects spend | Fails A8-minimum | **Reject** |
| C. Mature metering + owner cost UI now | Nice | Roadmap **V3** | **Defer V3** |

**Pick: A** (A8-minimum only).

### 2d. X2 bot / UI

| Option | Pros | Cons | |
|--------|------|------|--|
| **A. One first-party bot **and/or** basic UI (MVP X2 partial)** | Roadmap-locked; smallest distribution surface | Not full channel matrix | **Recommended** |
| B. OpenAPI/webhooks breadth now | Faster integrations | Roadmap **V1** | **Defer V1** |
| C. MCP breadth now | Agent ecosystem | Roadmap **V5** | **Defer V5** |

**Pick: A.**

---

## 3. Recommended design (IN)

### 3a. Agent/tool hard wall (bind §3b — #18 remainder)

1. **Separate agent runtime gateway** — Agents call **platform tools only** (not raw DB, not arbitrary internal HTTP).  
2. **Tool allowlist per Agent** — each tool declares FieldClasses it may `Read` / `ShareOutbound` (deny-by-default).  
3. **Server-side scrub** — every tool response / context pack runs `IFieldPolicy.Evaluate`; denied fields stripped **before** model context.  
4. **No LoginEmail tool / never in agent context** — LoginEmail remains User-only (CEO); OwnAgent Deny held from Stage A policy rows.  
5. **ShareOutbound tools Accept-gated** — e.g. `share_contact_email` (or equivalent) requires `HasAcceptGrant` (or Stage B AcceptGrant record); deny regardless of prompt text.  
6. **Cross-agent messaging mediated** — payloads cannot include denied FieldClasses; prompt text **cannot** escalate rights.  
7. **Reject** “system prompt says don’t leak” as sole control.

**Binding:** Same Domain `IFieldPolicy` as API/DB wall — dual wall for **all** FieldClasses (open-ended registry; examples ≠ exhaustive). Soft #41 Assistant OUT **closed** by delivering this wall + thin Assistant under it — not by claiming wall was delivered in Stage B.

### 3b. Thin Strategy-driven Assistant (X1 MVP partial)

1. **Boundary:** OwnAgent-only (acting for owning Participant); never Counterparty/Stranger agent identity.  
2. **Consumes StrategyBody** only when Evaluate allows OwnAgent Read/Write for owner context.  
3. **Runs behind** §3a gateway — tools scrubbed; no LoginEmail.  
4. **OUT:** Fuller free-form / stronger Assistant → **V1** (roadmap X1 primary at V1). Do not invent multi-party agents, DC4, or MotorMarket assistants.

### 3c. A8-minimum (Option 1)

1. **Per-Participant meters** for billable/LLM-touching Assistant (and any Stage C metered surface Product names without inventing extras).  
2. **Hard cutoff budgets** — when meter hits budget, further spend-path calls **fail closed** (no soft “please stop”).  
3. **OUT / DEFER:** Mature metering, owner cost UI, SSO billing → **V3**.  
4. **Cost:** PoC **$0**. Any named LLM/API spend → **escalate COO → CEO** (do not provision).

### 3d. X2 — one first-party bot and/or basic UI

1. **IN:** Exactly one first-party bot channel **and/or** a basic UI that exercises Authenticated Participant flows under existing auth (#5) + FieldPolicy.  
2. **OUT / DEFER:** OpenAPI/webhooks breadth → **V1**; MCP breadth → **V5**.  
3. Bot/UI must not bypass API or agent hard wall (no privileged back doors).

### 3e. Hosting / cost / currency

| Service | Label | Stage C use |
|---------|-------|-------------|
| Amazon ECS Express Mode (Fargate) | `open` | Sketch only until spend OK |
| AWS App Runner | `existing-customers-only` + `no-new-features` | **Excluded** greenfield |
| Cognito / vault / KMS | spend | **Out** unless CEO spend OK |

Local/$0 continues. No MotorMarket/DC4.

---

## 4. Explicit IN / OUT / DEFER

| Item | Status |
|------|--------|
| Agent/tool gateway + tool allowlist + response scrubber → same `IFieldPolicy.Evaluate` | **IN Stage C** |
| No LoginEmail in agent context; ShareOutbound tools Accept-gated; prompt cannot escalate | **IN Stage C** |
| Thin Strategy-driven Assistant OwnAgent-only (X1 MVP partial) | **IN Stage C** |
| A8-minimum per-Participant meters + hard cutoff | **IN Stage C** |
| One first-party bot and/or basic UI (X2 MVP partial) | **IN Stage C** |
| OTel/audit/idempotent-offers hooks (moments scope) | **IN** only as already Product-locked MVP hooks — do not invent new product surfaces |
| Fuller Assistant / free-form Strategy engine | **DEFER V1** |
| OpenAPI/webhooks | **DEFER V1** |
| Mature metering / owner cost UI | **DEFER V3** |
| MCP breadth | **DEFER V5** |
| Secrets vault microservice / Cognito/SSO | **OUT** unless CEO spend |
| App Runner greenfield | **OUT** |
| MotorMarket / DC4 | **OUT** |
| Gate #26 SA-REV-MVP-C open/unlock | **HOLD backlog until after Stage C delivery** |
| Gate #27 SA-REV-MVP-CLOSE | **HOLD** |
| Inventing Story IDs / Stage C Story catalog | **OUT** (PM/CPM owns IDs) |
| Spec / Stage C eng start | **HOLD until CA PASS** on this brief |

---

## 5. Proposed SA architecture-review moments (CA → CPM)

Baseline moments remain. **Confirm** (no new Moment IDs):

| Moment ID | Name | Trigger | Scope under review | Next stage HOLD until |
|-----------|------|---------|--------------------|------------------------|
| SA-REV-MVP-C | MVP Stage C — Thin Assistant + budgets + UI/bot | After Stage C **delivery** | Assistant boundary + agent/tool hard wall + scrubbers + no prompt bypass + A8-min + X2 bot/UI (+ OTel/audit/idempotent hooks as delivered) | CA PASS → MVP close path (`gate:sa-arch-review` **#26**) |
| SA-REV-MVP-CLOSE | MVP post-milestone architecture review | CPM declares MVP milestone closed | Full MVP vs SA baselines + secrets/ACL dual-wall posture | CA PASS → V1 (`#27`) |

**Rules:** Gate **#26** stays **backlog** until Stage C delivery (do **not** open now). Do **not** unlock Spec/eng or Stories from SA alone. Hand table to CA for CPM awareness (moments already scheduled as issues).

---

## 6. Security answers (Architecture always-critical)

**Checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-checklist.md` (Chief Security, 2026-09-28).  
**Rule:** Architecture QA must **not** PASS until Security QA confirms points **1–10**.

| # | Security point | Architecture answer | Cite |
|---|----------------|---------------------|------|
| 1 | Agent/tool hard wall = defense #2 on same FieldPolicy (§3b) | **MET.** Pick **A**: separate agent runtime gateway + per-Agent tool allowlist (deny-by-default) + server-side scrub of every tool response / context pack via the **same** Domain `IFieldPolicy.Evaluate` as the API/DB wall. Prompt-only soft guidance **rejected**; parallel agent ACL tables **rejected**. Dual wall for **all** FieldClasses (open-ended registry). | §2a Pick A; §3a items 1–3,7; Option A tip §3b (cite, not rewrite) |
| 2 | No LoginEmail in agent context | **MET.** No LoginEmail tool; LoginEmail never enters model / agent context packs; LoginEmail remains User-only (OwnAgent Deny held from Stage A policy rows); distinct from ContactEmail. | §3a item 4; §3b thin Assistant; Option A LoginEmail row |
| 3 | ShareOutbound tools Accept-gated; prompt cannot escalate | **MET.** Share tools (e.g. ContactEmail ShareOutbound equivalent) require `HasAcceptGrant` / Stage B AcceptGrant **server-side**; deny regardless of prompt text. Prompt injection cannot grant FieldClasses Evaluate denies. | §3a items 5–6; Option A §3b item 5 + ShareOutbound row |
| 4 | Cross-agent / mediated exfil posture | **MET.** Cross-agent messaging (if any in Stage C scope) is mediated; payloads cannot include denied FieldClasses. Agent↔agent exfil and prompt-injection threat rows addressed by gateway + scrub — **not** by trust in model behavior. | §3a items 6–7; Option A §2 Agent↔agent / prompt injection; §3b item 6 |
| 5 | Thin Assistant OwnAgent-only under the wall (closes soft #41 OUT) | **MET.** Thin Strategy-driven Assistant (X1 MVP partial) is OwnAgent-only; consumes StrategyBody only when Evaluate allows OwnAgent R/W for owner; runs **behind** §3a gateway. Soft **#41 Assistant OUT** closes only by delivering this wall + thin Assistant under it — **not** claimed as Stage B delivery. Fuller free-form / multi-party → **DEFER V1**. | §2b Pick A; §3b; §1 soft #41; §4 IN/OUT |
| 6 | A8-minimum meters + hard cutoff fail-closed | **MET.** Per-Participant meters for billable/LLM-touching Assistant (+ Product-named Stage C metered surfaces without inventing extras); hard cutoff → further spend-path calls **fail closed** (no soft stop). Mature metering/owner cost UI → **DEFER V3**. Named LLM/API spend → escalate **COO → CEO**; do not provision. PoC **$0**. | §2c Pick A; §3c; header Cost |
| 7 | X2 bot/UI must not bypass walls | **MET.** One first-party bot and/or basic UI (X2 MVP partial) under existing auth + FieldPolicy; **must not** bypass API wall or agent/tool hard wall (no privileged back doors). OpenAPI/webhooks → **DEFER V1**; MCP → **DEFER V5**. | §2d Pick A; §3d; §4 OUT |
| 8 | Consume Stage A/B tip; do not rewrite Option A | **MET.** Consumes Field ACL + list fail-closed + discovery scrub + StrategyBody ACL + ContactEmail ShareOutbound-after-Accept + Gates #24/#25 CLOSED on `main` @ `ca827a2`; Stage C is **delta** binding §3b without rewriting Option A tip; does not merge A/B named slices into one invented surface. | Header Binding tip; §1 Delta table; Sources |
| 9 | No inventing / no spend / no MM / Gate #26 HOLD | **MET.** Does **not** claim Cognito/SSO/IdP, mature vault/KMS, App Runner greenfield, MotorMarket/DC4, fuller Assistant as MVP-delivered, Gate **#26** open now, Gate **#27** unlock, or invent Stage C Story IDs. ECS Express sketch (`open`); App Runner excluded. Cost/critical → COO → CEO. PoC **$0**. | §3e; §4 OUT/DEFER; §5 moments (#26 backlog); header HOLD |
| 10 | Traceability + handshake | **MET.** Cites Option A tip (§3b + Stage C row) + CEO Stage C unlock + Stage A/B `main` @ `ca827a2` + soft #41 OUT carry-in. Residuals → Spec HOLD until CA PASS; Gate #26 backlog until delivery; spend → CEO escalation — **no guessing**. Architecture QA awaits Security QA confirm before PASS. | This §6; Sources; §5; checklist DOC-FLOW |


---

## Done-list (Architecture QA)

- [ ] Options cover CA brief + Product sources (Option A tip + roadmap X1/A8/X2)
- [ ] Review moments table present (#26 backlog until delivery; #27 HOLD)
- [ ] § Security answers when checklist issued + **Security QA confirm before PASS**
- [ ] Confirm to **Chief Architect only**; do **not** unlock Spec/eng or invent Story IDs
- [ ] Soft #41 Assistant OUT resolved only by Stage C wall+thin Assistant under wall — not claimed as Stage B delivery
