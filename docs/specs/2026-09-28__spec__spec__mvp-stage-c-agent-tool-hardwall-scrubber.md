# Spec — MVP Stage C: Agent/tool hard wall + response scrubber (#67)

**Status:** Senior Spec draft; BA AC locked; Spec QA HOLD until Spec-step Security PASS + Chief clear  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY Stage C Specs #67 → #66 → #68 → #69 → parent #18 (CEO Stage C + #18 Spec unlock; CA PASS delta)  
**Issue:** https://github.com/ioaikh/dealoware/issues/67  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A (CEO ACCEPTED) · `stage:mvp` · Stage C · #18 dual-wall remainder (eng-facing slice)  
**DOC-FLOW:** `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md`  
**Constraints:** Separate Spec from #66 / #68 / #69 / parent #18 framing (cross-ref only). Consume Stage A/B tip on `main` @ `ca827a2` (#31+#32+#40+#41+#42); do **not** rewrite Option A; do **not** claim Stage B delivered agent wall. PoC **$0**. Gate **#26** stays **backlog** until Stage C **delivery**. Gate **#27** HOLD. No MM/DC4; no Cognito/vault/KMS inventing; no MCP/public tool marketplace; no 5th Story for OTel/audit/idempotent (Soft Spec weave only). **BA AC locked.** Cite CA PASS delta hard-wall pick A + Option A §3b. Spec QA must **not** PASS until Spec-step Security checklist delivered + Security QA confirms. **Triad CLOSE / Dev Plan unlock ping HOLD** until Spec Security QA PASS (Chief clears). Docs SoR PR soft — do **not** HOLD Spec content on docs merge.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #67 AC + OUT | https://github.com/ioaikh/dealoware/issues/67 | Binding acceptance |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Dual-wall remainder framing; authz/tenancy |
| Option A architecture (§3b + Stage C row) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Platform hard wall; Story split “Agent tool hard wall + response scrubber”; dual wall all FieldClasses |
| CA PASS Stage C delta (hard wall pick A) | `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` | Separate gateway + allowlist + same Evaluate scrub; reject prompt-only / parallel ACL |
| BA note #67 | `plans/2026-09-28__ba__note__story-67-agent-tool-hardwall-scrubber.md` | Spec-ready refine; Soft OTel/audit/idempotent weave |
| Spec Security checklist (#67) | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-checklist.md` | Points **1–10** (Spec must bind) |
| SA Security PASS trio | `verification/2026-09-28__security__verification__mvp-stage-c-sa-checklist.md`, `...-sa-points-review.md`, `...-sa-qa-confirm.md` | Architecture-step Security PASS prior |
| Stage A #31 Spec | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | FieldClass + IFieldPolicy API/DB wall (CLOSED) |
| Stage B #41 / #42 Specs | `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md`, `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md` | StrategyBody ACL; ShareOutbound-after-Accept (CLOSED) |
| Sibling #66 / #68 / #69 Specs | `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md`, `...-a8-minimum-meters-budgets.md`, `...-basic-ui-first-party-bot-x2.md` | Cross-ref only — do not merge |
| Parent #18 Spec | `specs/2026-09-28__spec__spec__mvp-stage-c-participant-isolation-option-a-remainder.md` | Framing Spec; does **not** Field-capture this slice |

**Product alignment:** CEO Stage C named slice — eng-facing **#18 dual-wall remainder** (defense #2 agent/tool plane). Soft #41 Assistant OUT closes with **#66** under this wall — not Stage B claim. Mature vault → **V3**. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | SA hard wall pick A | Separate **agent runtime gateway** + tool allowlist + server-side scrub calling **same** Domain `IFieldPolicy.Evaluate` as API/DB wall (cite CA PASS §2a/§3a + Option A §3b) |
| 1 | Platform tools only | Agents call **platform tools only** — not raw DB, not arbitrary HTTP to internal APIs |
| 2 | Tool allowlist deny-by-default | Each tool declares FieldClasses it may `Read` / `ShareOutbound`; undeclared tools **denied** |
| 3 | Server-side scrub | Every tool response / context pack runs `IFieldPolicy.Evaluate` (or equivalent); denied fields stripped **before** model context |
| 4 | No LoginEmail | No LoginEmail tool; LoginEmail never in agent context packs; LoginEmail remains User-only (OwnAgent Deny held from Stage A) |
| 5 | ShareOutbound Accept-gated | Share tools (e.g. `share_contact_email` or equivalent) require `HasAcceptGrant` / Stage B AcceptGrant **server-side**; deny regardless of prompt text |
| 6 | Cross-agent mediated | If any cross-agent messaging at MVP: mediated; payloads cannot include denied FieldClasses; prompt cannot escalate |
| 7 | Reject prompt-only / parallel ACL | Explicitly **reject** system-prompt soft guidance as sole control; **reject** separate agent ACL tables that drift from FieldPolicy |
| 8 | Dual wall all FieldClasses | Open-ended registry (CEO examples ≠ exhaustive); dual wall for **all** FieldClasses via same Evaluate |
| 9 | Soft OTel/audit/idempotent | Where hard-wall deny/scrub or offer-touching tool paths hit SA-REV-MVP-C hooks — Spec notes touchpoints only; **no 5th Story** |
| 10 | Scope label / OUT | Documented as Stage C **#18 remainder** dual-wall bind; mature vault → **V3**; MCP/public marketplace OUT; Gate #26 backlog; #27 HOLD; $0 |

---

## 1. Agent runtime gateway (platform tools only)

| Item | Spec lock |
|------|-----------|
| Boundary | Separate **agent runtime gateway** — agents invoke **platform tools only** |
| Forbidden | Raw DB access; arbitrary HTTP to internal APIs; privileged back doors that skip Evaluate |
| Bind | Same Domain FieldPolicy as API/DB wall (defense #2) |

---

## 2. Tool allowlist (deny-by-default)

| Item | Spec lock |
|------|-----------|
| Declaration | Each allowlisted tool declares FieldClasses it may `Read` and/or `ShareOutbound` |
| Undeclared | Tools not on allowlist → **deny** |
| Examples | Concrete MVP allowlist may use locked FieldClass **examples** (LoginEmail, ContactEmail, DisplayName, StrategyBody, etc.) — examples ≠ exhaustive; do **not** invent new FieldClasses |
| Actions | Align Option A actions: `Read` · `Write` · `List` · `ShareOutbound` as applicable to tool I/O |

---

## 3. Server-side scrub before model context

| Item | Spec lock |
|------|-----------|
| Path | Every tool response / context pack runs `IFieldPolicy.Evaluate(principal, fieldClass, action, resourceContext)` (or equivalent) |
| Effect | Denied fields **stripped before** model / agent context |
| Same policy | Must call the **same** IFieldPolicy as API/DB wall — no parallel scrub table |

---

## 4. LoginEmail + ShareOutbound + cross-agent

| Rule | Spec lock |
|------|-----------|
| LoginEmail | User-only; **no** LoginEmail tool; **never** in agent context packs; OwnAgent Deny held |
| ContactEmail ShareOutbound | Share tool Allow **only if** Accept recorded (`HasAcceptGrant` / Stage B AcceptGrant); else deny |
| Prompt escalation | Prompt text / injection **cannot** grant FieldClasses Evaluate denies |
| Cross-agent (if any) | Mediated gateway path; payloads scrubbed; no denied FieldClasses |

---

## 5. Soft OTel / audit / idempotent-offers (weave only)

| Touchpoint | Spec note (not a Story) |
|------------|-------------------------|
| Hard-wall deny | Soft: emit audit/OTel signal if SA-REV-MVP-C hooks already on path |
| Scrub strip | Soft: observability touchpoint when denied fields stripped |
| Offer-touch tools | Soft: idempotent-offers / audit weave where share/offer-write tools touch AcceptGrant paths |
| Invent | **Do not** invent a 5th Story or new observability product surface |

---

## 6. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-checklist.md`  
**Prior SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Rule:** Spec QA must **not** PASS until Spec-step Security checklist delivered + Security QA confirms.

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Separate agent runtime gateway | Agents → platform tools only; not raw DB; not arbitrary internal HTTP | Locked #0/#1; §1 |
| 2 | Tool allowlist deny-by-default | Each tool declares Read/ShareOutbound FieldClasses; undeclared denied | Locked #2; §2 |
| 3 | Server-side scrub before model context | Every tool response/context pack → same `IFieldPolicy.Evaluate`; denied stripped before model | Locked #3; §3 |
| 4 | No LoginEmail tool / never in agent context | LoginEmail User-only; OwnAgent Deny held; no LoginEmail in context packs | Locked #4; §4 |
| 5 | ShareOutbound Accept-gated; prompt cannot escalate | Share tools require AcceptGrant server-side; deny regardless of prompt; injection cannot grant Evaluate denies | Locked #5; §4 |
| 6 | Cross-agent mediated exfil posture | Any cross-agent messaging mediated; payloads cannot include denied FieldClasses; gateway+scrub not model trust | Locked #6; §4 |
| 7 | Reject prompt-only / parallel ACL | Reject system-prompt sole control; reject separate agent ACL tables | Locked #7; Locked decisions |
| 8 | Dual wall all FieldClasses; consume tip | Open-ended registry; dual wall all classes; consume A/B tip; soft #41 OUT closes with #66 under this wall (not Stage B claim) | Locked #8/#10; §7 OUT; Sources |
| 9 | OUT / Gate / spend | Mature vault → V3; MCP/marketplace OUT; Gate #26 backlog; #27 HOLD; no Cognito/MM/DC4; PoC $0; cost/critical → COO → CEO | Locked #10; §7 OUT; §8 Host |
| 10 | Traceability + handshake | Cites #67 AC + Option A §3b + SA Security PASS; keep #66/#68/#69 separate; parent #18 not Field-captured here; Spec QA PASS only after Security QA | Sources; this §6; Constraints |

---

## 7. Explicit OUT

| OUT | Note |
|-----|------|
| Thin Assistant product UX / strategy interpretation (#66) | Sibling — wall is mandatory binding for that runtime |
| A8 meters/budgets (#68) | Sibling |
| Basic UI / bot surface (#69) | Sibling |
| Inventing a 5th Story for OTel/audit/idempotent | Soft Spec weave only (§5) |
| Mature PII vault retention/erasure (A9 mature) | → **V3** |
| MCP breadth; public OpenAPI; public tool marketplace | OUT |
| Inventing spend / Cognito / MotorMarket / DC4 | OUT |
| Unlocking #26 before Stage C delivery; #27 | #26 backlog; #27 HOLD |
| Rewriting parent #18 Spec as Field capture; rewriting Option A | Framing Spec separate; Option A cite-only |
| Claiming Stage B delivered agent wall | Soft #41 OUT closes with #66 under this wall |

---

## 8. Host / cost

Extend O10 modular monolith with agent runtime gateway + tool allowlist + scrub on existing Domain `IFieldPolicy`. Local/$0; ECS Express sketch only; App Runner excluded; no AWS/IdP/vault/LLM provision. PoC **$0**. Any named LLM/API spend → escalate **COO → CEO**.

---

## 9. Acceptance mapping (issue #67 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Agents call **platform tools only** (separate agent runtime gateway) — not raw DB, not arbitrary HTTP to internal APIs | Locked #0/#1; §1 |
| **Tool allowlist** (deny-by-default): each tool declares FieldClasses it may Read / ShareOutbound; undeclared tools denied | Locked #2; §2 |
| **Server-side scrub:** every tool response / context pack runs `IFieldPolicy.Evaluate` (or equivalent); denied fields stripped **before** model context | Locked #3; §3 |
| **No LoginEmail** in agent context packs / tools — LoginEmail remains User-only (not even OwnAgent) | Locked #4; §4 |
| **ShareOutbound** tool (e.g. `share_contact_email` or equivalent) allows ContactEmail share **only if Accept recorded** on that offer/negotiation; else deny regardless of prompt text | Locked #5; §4 |
| Cross-agent messaging (if any at MVP) is **mediated**; payloads cannot include denied FieldClasses; prompt text cannot escalate rights | Locked #6; §4 |
| Automated tests (or equivalent): allowlisted tool OK with scrub; denied field stripped; LoginEmail never in context; pre-Accept share deny; stranger/cross-tenant deny; unauth deny; **reject prompt-only soft guidance as sole control** | §9.1 below; Spec QA + Dev Plan Done-lists |
| Documented as Stage C **#18 remainder** dual-wall bind — mature vault → **V3**; MCP/public tool marketplace invent → out | Locked #10; §7 OUT |
| Soft Spec weave only (not a separate Story): where hard-wall deny/scrub or offer-touching tool paths hit SA-REV-MVP-C **OTel / audit / idempotent-offers** hooks, Spec notes touchpoints — no new product surface invented | Locked #9; §5 |

### 9.1 Automated tests detail (binding)

| Case | Expect |
|------|--------|
| Allowlisted tool + scrub | OK; response/context passes Evaluate; allowed fields only |
| Denied field in tool response | Stripped **before** model context |
| LoginEmail | Never present in agent context packs / tool outputs |
| Pre-Accept ShareOutbound | Deny regardless of prompt text |
| Stranger / cross-tenant | Deny; no private-field leakage |
| Unauthenticated | Deny; no private fields in errors |
| Prompt-only soft guidance as sole control | **Rejected** as architecture/control (tests assert gateway+Evaluate path, not prompt-only) |

---

## Done-list

### Spec QA

- [ ] DOC-FLOW path; binding sources only; Option A §3b + CA PASS hard-wall pick A cited
- [ ] Locks gateway + allowlist deny-by-default + same-Evaluate scrub
- [ ] No LoginEmail; ShareOutbound Accept-gated; prompt cannot escalate; cross-agent mediated
- [ ] Rejects prompt-only sole control and parallel agent ACL tables
- [ ] Dual wall all FieldClasses; soft #41 OUT closes with #66 under wall (not Stage B claim)
- [ ] Soft OTel/audit/idempotent weave only — no 5th Story
- [ ] **§9 Acceptance mapping complete** — all issue #67 AC bullets present as rows
- [ ] **Automated tests AC** in §9 map + §9.1 detail
- [ ] **Security 1–10** bound (§6) — ask Security QA before PASS
- [ ] Separate from #66/#68/#69/#18 framing; Gate #26 backlog; #27 HOLD; $0
- [ ] BA AC locked cleared
- [ ] **HOLD triad CLOSE / Dev Plan ping** until Spec Security QA PASS (Chief)

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Implement agent runtime gateway (platform tools only)
- [ ] Encode tool allowlist deny-by-default with FieldClass Read/ShareOutbound declarations
- [ ] Server-side scrub every tool response/context via same IFieldPolicy.Evaluate
- [ ] No LoginEmail tool/context; ShareOutbound Accept-gated server-side
- [ ] Mediated cross-agent if any; reject prompt-only / parallel ACL
- [ ] Soft weave OTel/audit/idempotent touchpoints where path hits — no 5th Story
- [ ] **Automated tests** per §9 / §9.1
- [ ] Do not implement §7 OUT

**Next:** Spec QA verify → ask Security QA → confirm to Chief Spec only. Triad CLOSE after Spec Security QA PASS (Chief). Gate #26 backlog; #27 HOLD; PoC $0. Soft #41 OUT closes with sibling #66 under this wall.
