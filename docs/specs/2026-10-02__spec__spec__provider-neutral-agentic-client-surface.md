# Spec — Spec Step 4: Provider-neutral agentic client surface (negotiation fabric) — #155

**Status:** Senior Spec — Security-bound design Soft HOLD SoR; Spec QA Soft HOLD until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED  
**Date:** 2026-10-02  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec CLEAR WRITE #155 (CA design grounding PASS Soft HOLD SoR CLEAR tip **`90120c3`** PR **#167**; Spec Security checklist ISSUED)  
**Issue:** https://github.com/ioaikh/dealoware/issues/155 · `status:in-dev` · Spec Step 4 — Provider-neutral agentic client surface (design Soft HOLD SoR Soft HOLD build) — **do not close #155**  
**DOC-FLOW:** `specs/2026-10-02__spec__spec__provider-neutral-agentic-client-surface.md`  
**Constraints:** **Design Soft HOLD SoR Spec only.** Soft HOLD invent Stories · Soft HOLD invent AC beyond strategy What · Soft HOLD build/implementation/spend until separately named CEO unlock · Soft HOLD invent connector auth/rate limits ahead of CEO · Soft HOLD invent MCP V5 · Soft HOLD invent multi-LLM/BYO V4 · Soft HOLD Marketing publish · Soft HOLD AWS provision/spend · Soft HOLD invent Spec/AC Step 5 · Gate **#27 CLOSED** · Soft **#41 CLOSED** via **#66+#67** · **App Runner OUT** · MotorMarket/DC4 OUT · settlement/escrow OUT · PlatformOwner admin (#148 tip **`ff707ae`**) **≠** this surface · Participant UI (#69) **≠** invent as this API surface rewrite · Soft HOLD multi-provider Soft HOLD **lifts for this Spec track only** (design Soft HOLD SoR — not impl/spend) · PoC **$0** · spend → **COO → CEO**. Tips: CA design grounding PASS Soft HOLD SoR **`90120c3`** (PR **#167**) · architecture Soft HOLD SoR lineage **`6dc48d7`** (#162) / **`c1b4e8c`** (#163) · Security qa-confirm Soft HOLD SoR PASS **#165** @ **`5d93bdf`** · Arch QA Soft HOLD SoR PASS **#166** @ tip **`eb11262`** · AWS inherit Soft HOLD provision **`c28361f`** · admin Soft HOLD SoR **`ff707ae`**. Spec QA Soft HOLD until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED. Handshake Soft HOLD SoR = **qa-confirm only** — do **not** invent points-review Soft HOLD SoR. Done-list to **Chief Spec** (not Spec QA yet). Quiet filler Soft HOLD invent Soft HOLD.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #155 In-scope + Soft HOLD/OUT | https://github.com/ioaikh/dealoware/issues/155 | Binding Spec Step 4 acceptance framing; stays OPEN |
| CA design grounding PASS Soft HOLD SoR | `verification/2026-10-02__ca__verification__mvp-sa-step-4-agentic-client-design-grounding-pass.md` @ tip **`90120c3`** (PR **#167**; Soft HOLD SoR twin `docs/verification/…`) | Binding CA PASS Soft HOLD SoR — Spec WRITE Soft HOLD LIFTED |
| Product Spec scope lock | `product/2026-10-02__product__note__step4-provider-neutral-client-spec-scope.md` | Binding Spec IN/OUT (strategy What) |
| Product Step 4 strategy | `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 4 | Strategy What bound |
| CA PASS Option A Soft HOLD SoR | `architecture/2026-10-02__sa__architecture__agentic-client-surface-step-4.md` lineage tips **`6dc48d7`** (#162) / **`c1b4e8c`** (#163) | Binding architecture design Soft HOLD SoR (Option A) |
| Arch QA Soft HOLD SoR PASS | `verification/2026-10-02__sa__verification__mvp-sa-step-4-agentic-client-review.md` @ tip **`eb11262`** (PR **#166**) | Architecture QA PASS |
| SA Security Soft HOLD SoR CLEAR | `verification/2026-10-02__security__verification__mvp-sa-step-4-agentic-client-qa-confirm.md` @ tip **`5d93bdf`** (PR **#165**) | SA-step Security QA PASS 10/10; qa-confirm only |
| Spec Security checklist | `verification/2026-10-02__security__verification__mvp-spec-step-4-agentic-client-surface-checklist.md` | Spec-step points **1–10** (bind Soft HOLD SoR path only — **do not invent twin**) |
| Security checklist Soft HOLD SoR CLEAR | PRs **#159+#160** | Soft HOLD SoR checklist CLEAR on main |
| AWS Step 2 Soft HOLD SoR #142 | `specs/2026-10-02__spec__spec__aws-cloud-architecture-design-ecs-express.md` @ tip **`c28361f`** | Host design constraints Soft HOLD provision |
| Admin #148 Soft HOLD SoR | `specs/2026-10-02__spec__spec__platform-owner-admin-dashboard.md` @ tip **`ff707ae`** | PlatformOwner admin ≠ this surface |
| Soft #41 CLOSED / #66+#67 | Stage C Specs thin Assistant + hard wall | Same Dealoware API extended as only client surface; do not reopen |
| Option A §3a/§3b | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Dual wall cite — do not rewrite |
| Gate #27 CLOSED | Gate **#27 CLOSED** Soft HOLD SoR | Identity/ACL gate CLOSED |
| Participant UI #69 | `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md` | Human Participant UI ≠ this client-surface Spec |

**Product alignment:** Spec success = written provider-neutral client-surface contract + V1 OpenAPI/webhooks framing + one reference client path **designed**; OUT list explicit; **no build/implementation invent**; PoC $0. Soft HOLD multi-provider lifts for this Spec-track design Soft HOLD SoR only. Conflicts → escalate PM → Product → CEO. Cost/critical → COO → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | Design Soft HOLD SoR only | Soft HOLD invent Stories · Soft HOLD invent AC beyond strategy What · Soft HOLD build/impl/spend · Soft HOLD claim connectors/SDK as delivered |
| 1 | Architecture pick | **Option A** — same Dealoware negotiation/assistant API (#66+#67) as the **only** client surface; Soft HOLD invent Grok-only / forked weaker external path / second API |
| 2 | Principals | First-party bots + external agentic clients are first-class clients under **Participant** principal; Soft HOLD invent PlatformOwner (#148) as this surface |
| 3 | Dual wall | Option A **§3b** FieldPolicy dual wall on **every** client (API/DB FieldPolicy **and** agent/tool hard wall; same Domain `IFieldPolicy.Evaluate`). Soft HOLD invent parallel ACL / prompt-only / client-class bypass |
| 4 | Identity seal | Identity seal **equal** on the bot path vs other clients of the same API; LoginEmail never in agent/model context; ContactEmail ShareOutbound Accept-gated |
| 5 | Contract framing | Written client-surface contract: auth · Participant principal · FieldPolicy/hard wall all clients · identity seal equal on bot path. Soft HOLD invent connector auth/rate limits ahead of CEO |
| 6 | Delivery stage | **V1 OpenAPI + webhooks**; Soft HOLD invent MCP V5 · Soft HOLD invent multi-LLM/BYO V4 as Step 4 |
| 7 | Reference path | **One** reference client path **designed not built**; Soft HOLD invent Stories / running-client AC |
| 8 | External naming | Example client classes only; Soft HOLD invent live partners / Marketing as delivered partners |
| 9 | Host inherit | Cite AWS #142 @ tip **`c28361f`** design constraints only Soft HOLD provision; **App Runner OUT** |
| 10 | Soft HOLD multi-provider | Soft HOLD multi-provider Soft HOLD **lifts for this Spec track only** (design Soft HOLD SoR — not impl/spend) |
| 11 | Gates | Gate **#27 CLOSED** · Soft **#41 CLOSED** via **#66+#67** — do not reopen |
| 12 | Spend | PoC **$0**; any later provision/spend → COO → CEO |

---

## 1. Purpose

Turn Product Step 4 strategy What + scope lock + CA PASS Option A Soft HOLD SoR into a **Security-bound provider-neutral agentic client surface Spec Soft HOLD SoR** for Issue #155: one negotiation/assistant API where first-party bots and external agentic clients are first-class clients of the **same** API; written client-surface contract; one reference client path **designed** (not built); V1 OpenAPI/webhooks framing.

This Spec is **design Soft HOLD SoR only**. Soft HOLD invent Stories. Soft HOLD invent AC beyond strategy What. Soft HOLD build/implementation/spend. Soft HOLD invent connector auth/rate limits. Soft HOLD invent V4/V5. Soft HOLD Marketing. Soft HOLD AWS provision. Soft HOLD Step 5. Soft HOLD invent live partners. Soft HOLD invent PlatformOwner admin as this surface. Soft HOLD invent Grok-only fork. PoC **$0**.

**#155 stays OPEN** (`status:in-dev`) — do **not** close from this Spec.

---

## 2. Architecture pick — Option A

| Option | Spec disposition |
|--------|------------------|
| **A. Same Dealoware negotiation/assistant API (#66+#67) as the only client surface; Participant principal; §3b dual wall every client; identity seal equal on bot path; V1 OpenAPI+webhooks; reference path designed not built; example client classes only** | **PICK** — Soft HOLD SoR only; Soft HOLD invent Stories Soft HOLD build |
| B. Grok-only / provider-locked special path | **OUT / Reject** |
| C. Separate weaker external API / forked ACL | **OUT / Reject** |
| D. PlatformOwner admin (#148) as this surface | **OUT / Reject** — admin ≠ client surface |
| E. App Runner client host Soft HOLD invent | **OUT / Reject** — App Runner OUT |
| F. Cognito/SSO / MCP V5 / multi-LLM V4 as Step 4 delivered | **OUT / Reject** |

**Pick: Option A.** Soft HOLD invent Stories. Soft HOLD build. Soft HOLD invent second API / Grok-only / admin fold-in.

**Soft HOLD multi-provider Soft HOLD lifts for this Spec track only** (design Soft HOLD SoR — not implementation, not spend).

---

## 3. Same API — first-party bots + external agentic clients

| Concern | Spec lock | Soft HOLD |
|---------|-----------|-----------|
| API identity | One provider-neutral negotiation/assistant API — extend Soft #41 CLOSED via #66+#67 Dealoware API | Soft HOLD invent Grok-only Soft HOLD invent second API Soft HOLD reopen Soft #41 |
| Client classes | First-party bots **and** external agentic clients are **first-class** clients of the **same** API | Soft HOLD invent weaker external path Soft HOLD invent live partners |
| Principal | **Participant** principal for this surface | Soft HOLD invent PlatformOwner (#148) as this surface Soft HOLD invent Participant UI (#69) rewrite as this Spec |
| Example classes | External systems named as **example client classes only** | Soft HOLD invent live partners Soft HOLD invent Marketing partner claims |

---

## 4. Client-surface contract

| Element | Spec lock | Soft HOLD |
|---------|-----------|-----------|
| Auth | Contract names auth model for clients of the same API | Soft HOLD invent connector auth schemes Soft HOLD invent partner-specific Secrets Soft HOLD ahead of CEO |
| Participant principal | Binding for this surface | Soft HOLD invent PlatformOwner conflation |
| FieldPolicy / hard wall | Bind on **all** clients (bot + external) | Soft HOLD invent client-class bypass Soft HOLD invent parallel ACL |
| Identity seal | **Equal** on the bot path vs other clients of the same API | Soft HOLD invent identity shortcuts for agentic clients |
| Rate limits / quotas | May name Soft HOLD TBD boundaries only | Soft HOLD invent rate-limit AC Soft HOLD invent live-connector quotas ahead of CEO |

---

## 5. Dual wall + identity seal (Option A §3b)

- Cite Option A **§3a/§3b** dual wall (do not rewrite): API/DB FieldPolicy + agent/tool hard wall; same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses.
- Applies to **every** client of this surface — first-party bot path and external agentic clients alike.
- Soft HOLD invent prompt-only controls, parallel ACL tables that drift, tenant shortcuts that weaken fail-closed list/discovery scrub, or client-class bypass that dumps denied FieldClasses.
- Identity-until-accept: no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side.
- Soft HOLD leak LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into prompts, logs, webhooks, or exports without explicit Accept-gated / FieldPolicy allow.
- Soft HOLD weaken Participant dual wall for “external convenience.”

---

## 6. V1 OpenAPI / webhooks + reference client path

| Surface | Spec lock | Soft HOLD |
|---------|-----------|-----------|
| Delivery framing | **V1 OpenAPI + webhooks** as Spec target surface | Soft HOLD invent MCP V5 Soft HOLD invent multi-LLM/BYO V4 as Step 4 |
| Reference client path | **One** reference path **designed** (documented how an external agentic client attaches via the same API) | Soft HOLD invent build Soft HOLD invent SDK ship Soft HOLD invent deploy Soft HOLD invent Stories Soft HOLD invent AC requiring a running client |

---

## 7. Host inherit (AWS Step 2 Soft HOLD provision @ tip `c28361f`)

| Layer | Inherit Soft HOLD SoR | Soft HOLD |
|-------|----------------------|-----------|
| Compute / data / secrets / obs | Cite #142 Soft HOLD SoR @ tip **`c28361f`** — design constraints only | Soft HOLD invent provision Soft HOLD invent App Runner Soft HOLD invent second cluster Soft HOLD invent paid keys as delivered |
| Relation to admin | Soft HOLD SoR #148 @ tip **`ff707ae`** remains PlatformOwner admin — **≠** this client surface | Soft HOLD invent fold admin into this Spec |

---

## 8. Explicit IN / OUT / Soft HOLD

### IN (this design Soft HOLD SoR)

- Provider-neutral agentic client surface Spec Soft HOLD SoR Option A for Spec #155 / Product Step 4
- Same Dealoware API (#66+#67) as only client surface; first-party bots + external agentic clients first-class under Participant principal
- Written client-surface contract (auth · Participant · FieldPolicy/hard wall all clients · identity seal equal on bot path)
- V1 OpenAPI/webhooks framing
- One reference client path designed not built
- Example client classes only
- Soft HOLD multi-provider Soft HOLD lifts for this Spec track only (design Soft HOLD SoR)
- Spec Security checklist answers 1–10 MET (§10)
- Binding cites: Product scope + strategy Step 4 + CA PASS Option A + Arch QA Soft HOLD SoR PASS #166 @ `eb11262` + SA Security Soft HOLD SoR PASS #165 @ `5d93bdf` + Spec checklist Soft HOLD SoR path

### OUT

- invent Stories Soft HOLD
- invent AC beyond strategy What
- build / implementation / spend Soft HOLD until separately named CEO unlock
- invent connector auth / rate limits ahead of CEO
- MCP V5 · multi-LLM/BYO V4
- Marketing publish Soft HOLD
- AWS account / resource provision / spend Soft HOLD
- invent Spec/AC Step **5** until Step 4 CLOSED + explicit CEO invent-confirm
- settlement / escrow / checkout
- reopen Gate **#27** / Soft **#41**
- MotorMarket / DC4 · App Runner
- Grok-only / forked weaker external API
- PlatformOwner admin (#148) as this surface
- Cognito as delivered · live partners as delivered
- PoC **$0** claim as provisioned spend

### Soft HOLD

- Soft HOLD build Soft HOLD invent Stories Soft HOLD invent AC beyond strategy What Soft HOLD
- Soft HOLD invent connector auth/rate limits Soft HOLD
- Soft HOLD AWS provision Soft HOLD Marketing Soft HOLD Step 5 Soft HOLD
- Soft HOLD Spec QA PASS until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED
- Soft HOLD Spec Step 4 formal CLOSE Soft HOLD until checklist Soft HOLD SoR CLEAR + content Chief PASS + Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX PASS
- Gate **#27 CLOSED** · Soft **#41 CLOSED** via **#66+#67**

---

## 9. Security Spec checklist binding (points 1–10)

**Binding checklist Soft HOLD SoR (do not invent twin):** `verification/2026-10-02__security__verification__mvp-spec-step-4-agentic-client-surface-checklist.md`  
**Prior SA Security PASS:** `verification/2026-10-02__security__verification__mvp-sa-step-4-agentic-client-qa-confirm.md` @ tip **`5d93bdf`** (PR **#165**; 1–10 MET; qa-confirm only)  
**CA design grounding PASS Soft HOLD SoR:** `verification/2026-10-02__ca__verification__mvp-sa-step-4-agentic-client-design-grounding-pass.md` @ tip **`90120c3`** (PR **#167**)
**CA PASS arch Soft HOLD SoR:** `architecture/2026-10-02__sa__architecture__agentic-client-surface-step-4.md` lineage **`6dc48d7`** / **`c1b4e8c`**  
**Arch QA Soft HOLD SoR PASS:** `verification/2026-10-02__sa__verification__mvp-sa-step-4-agentic-client-review.md` @ tip **`eb11262`** (PR **#166**)  
**Rule:** Spec QA must **not** PASS until Spec-step Security QA confirms these points via Soft HOLD SoR `…mvp-spec-step-4-agentic-client-surface-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** — **do not invent points-review Soft HOLD SoR**.

| # | Security point | Spec requirement / answer | Spec section cites |
|---|----------------|---------------------------|--------------------|
| 1 | Provider-neutral same-API first-class clients Soft HOLD Grok-only | **MET.** One provider-neutral negotiation/assistant API; first-party bots + external agentic clients first-class on the **same** API. Soft HOLD invent Grok-only Soft HOLD invent forked weaker external path Soft HOLD invent impl/spend/live partner wiring. Soft HOLD multi-provider Soft HOLD lifts for this design Soft HOLD SoR only. | Locked #1/#10; §2 Option A; §3 Same API; §8 IN/OUT |
| 2 | Written client-surface contract Soft HOLD invent connector auth / rate limits | **MET.** Contract covers auth, Participant principal, FieldPolicy/hard wall on all clients, identity seal equal on bot path. Soft HOLD invent connector auth Soft HOLD invent rate-limit regimes Soft HOLD invent partner-specific auth Soft HOLD ahead of CEO Soft HOLD invent live partner credentials. | Locked #5; §4 Client-surface contract; §8 OUT |
| 3 | Fail-closed dual-wall on all clients (Option A §3b) | **MET.** Binds Option A §3b on every client (bot + external). Soft HOLD invent prompt-only Soft HOLD invent parallel ACL Soft HOLD invent client-class bypass Soft HOLD invent dump denied FieldClasses. Cite Option A + Gate #27 CLOSED / #67 + #148 tip `ff707ae`. | Locked #3; §5 Dual wall; Sources |
| 4 | Identity seal equal on the bot path Soft HOLD invent identity shortcuts | **MET.** Identity seal equal on bot path; no contact/PII on public Participant DTOs; LoginEmail never in agent/model context; ContactEmail ShareOutbound Accept-gated. Soft HOLD invent leaks into prompts/logs/webhooks/exports without Accept-gated / FieldPolicy allow. | Locked #4; §5 Identity seal; §8 OUT |
| 5 | One reference client path designed Soft HOLD invent build | **MET.** One reference client path **designed not built**. Soft HOLD invent implementation Soft HOLD invent SDK ship Soft HOLD invent Stories Soft HOLD invent AC requiring a running client. | Locked #7; §6 Reference path; §8 Soft HOLD build |
| 6 | V1 OpenAPI/webhooks Soft HOLD invent V4/V5 | **MET.** Delivery primary framing **V1 OpenAPI/webhooks**. Soft HOLD invent multi-LLM/BYO V4 Soft HOLD invent MCP V5 as Step 4. | Locked #6; §6; §8 OUT |
| 7 | External = example client classes only Soft HOLD invent live partners | **MET.** Example client classes only. Soft HOLD invent live partners Soft HOLD invent Marketing as delivered partners Soft HOLD invent partner Secrets Soft HOLD invent AC requiring a named live partner. | Locked #8; §3; §8 OUT |
| 8 | Soft HOLD AWS provision; inherit Step 2 design constraints only | **MET.** Cites #142 Soft HOLD SoR @ tip **`c28361f`** design constraints only Soft HOLD provision Soft HOLD invent App Runner Soft HOLD invent spend Soft HOLD invent build Soft HOLD. PoC **$0**. Escalate COO→CEO. | Locked #9/#12; §7 Host inherit; §8 Soft HOLD |
| 9 | OUT / Soft HOLD locked pack | **MET.** OUT/Soft HOLD pack includes invent Stories; invent AC beyond strategy What; Soft HOLD build/impl/spend; invent connector auth/rate limits; invent V4/V5; Marketing; AWS provision; invent Spec/AC Step 5; settlement/escrow; Gate #27 CLOSED; Soft #41 CLOSED; App Runner; MotorMarket/DC4; Cognito as delivered; PoC $0. | Locked #0/#11/#12; §8; header Constraints |
| 10 | Traceability + handshake Soft HOLD SoR pattern | **MET.** Cites strategy Step 4 + Option A §3b + #148 tip `ff707ae` + #142 tip `c28361f` + Gate #27 CLOSED + Soft #41 CLOSED via #66+#67 + CA design grounding PASS Soft HOLD SoR tip `90120c3` (#167) + CA PASS lineage `6dc48d7`/`c1b4e8c` + Arch QA Soft HOLD SoR PASS #166 @ `eb11262` + SA Security Soft HOLD SoR PASS #165 @ `5d93bdf` + Spec checklist Soft HOLD SoR (no invent twin). Spec QA Soft HOLD until Soft HOLD SoR `…mvp-spec-step-4-agentic-client-surface-qa-confirm.md` PASS + MERGED. Handshake Soft HOLD SoR = **qa-confirm only**. Done-list → Chief Spec. #155 stays OPEN. | This §9; Sources; §8 Soft HOLD; §12 Done-list |

---

## 10. Acceptance mapping — Product / Issue #155

**Binding:** Product Spec scope lock IN/OUT + Issue #155 In-scope + Soft HOLD/OUT. Design only — Soft HOLD invent Stories Soft HOLD invent AC beyond strategy What.

| Product / Issue #155 requirement | Spec lock | Spec section |
|----------------------------------|-----------|--------------|
| One provider-neutral negotiation/assistant API; first-party bots + external first-class on same API | Option A same API Soft HOLD invent Grok-only Soft HOLD invent second API | Locked #1; §2/#3 |
| Written client-surface contract: auth · Participant · FieldPolicy/hard wall all clients · identity seal equal on bot path | §4/#5 Soft HOLD invent connector auth Soft HOLD invent identity shortcuts | Locked #4/#5; §4/#5 |
| One reference client path designed (not built) | Designed Soft HOLD invent build Soft HOLD invent Stories | Locked #7; §6 |
| V1 OpenAPI/webhooks framing | V1 Soft HOLD invent V4/V5 | Locked #6; §6 |
| External = example client classes only | Soft HOLD invent live partners Soft HOLD invent Marketing partners | Locked #8; §3 |
| Soft HOLD invent Stories Soft HOLD invent AC beyond strategy What Soft HOLD | Explicit Soft HOLD | Locked #0; §8 |
| Soft HOLD build/impl/spend Soft HOLD | Explicit Soft HOLD until separately named CEO unlock | Locked #0; §8 |
| Soft HOLD invent connector auth/rate limits Soft HOLD | Explicit Soft HOLD | Locked #5; §4/#8 |
| Soft HOLD invent MCP V5 Soft HOLD invent multi-LLM/BYO V4 Soft HOLD | Explicit OUT | Locked #6; §8 OUT |
| Soft HOLD Marketing Soft HOLD AWS provision Soft HOLD Step 5 Soft HOLD | Explicit Soft HOLD/OUT | Locked #0/#9; §7/#8 |
| Soft HOLD multi-provider Soft HOLD lifts for this Spec track only (design Soft HOLD SoR) | Explicit Soft HOLD lift scope | Locked #10; §2 |
| Gate #27 CLOSED · Soft #41 CLOSED via #66+#67 | Explicit OUT reopen | Locked #11; §8 |
| App Runner OUT · MotorMarket/DC4 OUT · settlement OUT · PoC $0 | Explicit OUT | Locked #9/#12; §8 |
| Admin #148 ≠ this surface · #69 ≠ invent as this surface | Explicit | Locked #2; §2/#3 |
| Soft HOLD AWS inherit tip `c28361f` design-only | Explicit Soft HOLD provision | Locked #9; §7 |

**Product AC mapping gaps:** **None.** Soft HOLD inventing AC beyond strategy What — no gaps invented to fill.

---

## 11. Verification evidence (design Soft HOLD SoR only)

| Evidence | Path / ref | Role |
|----------|------------|------|
| CA design grounding PASS Soft HOLD SoR | `verification/2026-10-02__ca__verification__mvp-sa-step-4-agentic-client-design-grounding-pass.md` @ tip **`90120c3`** (PR **#167**) | Binding CA PASS Soft HOLD SoR (Spec WRITE Soft HOLD LIFTED) |
| CA PASS Option A Soft HOLD SoR | `architecture/2026-10-02__sa__architecture__agentic-client-surface-step-4.md` lineage **`6dc48d7`** / **`c1b4e8c`** | Binding design Soft HOLD SoR |
| Arch QA Soft HOLD SoR PASS | `verification/2026-10-02__sa__verification__mvp-sa-step-4-agentic-client-review.md` @ tip **`eb11262`** (PR **#166**) | Arch QA PASS |
| SA Security Soft HOLD SoR CLEAR | `verification/2026-10-02__security__verification__mvp-sa-step-4-agentic-client-qa-confirm.md` @ tip **`5d93bdf`** (PR **#165**) | SA-step 1–10 MET; qa-confirm only |
| Spec Security checklist Soft HOLD SoR | `verification/2026-10-02__security__verification__mvp-spec-step-4-agentic-client-surface-checklist.md` | Spec-step points 1–10 (this Spec binds; Soft HOLD SoR CLEAR #159+#160) |
| Soft HOLD Spec-step Security QA | Soft HOLD SoR `…mvp-spec-step-4-agentic-client-surface-qa-confirm.md` (expected; Soft HOLD until ISSUED + MERGED) | Spec QA Soft HOLD until PASS + MERGED |
| AWS Soft HOLD SoR inherit | `specs/2026-10-02__spec__spec__aws-cloud-architecture-design-ecs-express.md` @ tip **`c28361f`** | Design constraints Soft HOLD provision |
| Admin Soft HOLD SoR boundary | `specs/2026-10-02__spec__spec__platform-owner-admin-dashboard.md` @ tip **`ff707ae`** | Admin ≠ this surface |

No SD product automated tests / Stories invented. Soft HOLD invent build Soft HOLD provision Soft HOLD.

---

## 12. Spec QA Done-list Soft HOLD

Spec QA Soft HOLD until **all** of:

1. Spec-step Security QA PASS on points 1–10 (Soft HOLD SoR `…mvp-spec-step-4-agentic-client-surface-qa-confirm.md`)
2. Soft HOLD SoR qa-confirm **MERGED** (handshake Soft HOLD SoR = qa-confirm only — do **not** invent points-review Soft HOLD SoR)
3. Chief Spec clear after Security PASS

**Done-list content (for Spec QA when cleared):**

- [ ] Design Soft HOLD SoR covers strategy What / Product IN (Issue #155)
- [ ] Option A pick locked; same API; Participant principal; §3b dual wall every client; identity seal equal on bot path
- [ ] V1 OpenAPI/webhooks + one reference client path designed not built; example client classes only
- [ ] Soft HOLD multi-provider Soft HOLD lifts for this Spec track only (design Soft HOLD SoR — not impl/spend)
- [ ] Host inherit cites #142 @ tip **`c28361f`**; admin #148 @ tip **`ff707ae`** ≠ this surface; App Runner OUT
- [ ] §9 Security Spec checklist binding 1–10 with Spec section cites; Soft HOLD SoR checklist path only (no invent twin)
- [ ] §10 Acceptance mapping complete; Soft HOLD invent AC beyond strategy What
- [ ] Tips **`6dc48d7`/`c1b4e8c`** + **`5d93bdf`** + **`eb11262`** + **`c28361f`** + **`ff707ae`** present
- [ ] Gate #27 CLOSED / Soft #41 CLOSED held; PoC $0; #155 stays OPEN

---

## 13. Done-list → Chief Spec

- [x] Design Spec written at DOC-FLOW path
- [x] Option A same-API client surface bound from CA PASS Soft HOLD SoR lineage **`6dc48d7`/`c1b4e8c`**
- [x] Participant principal; dual wall §3b every client; identity seal equal on bot path
- [x] V1 OpenAPI/webhooks + reference path designed not built; example client classes only
- [x] Soft HOLD / OUT pack locked (invent Stories, invent AC beyond strategy What, Soft HOLD build/impl/spend, invent connector auth/rate limits, V4/V5, Marketing, AWS provision, Step 5, Gate #27, Soft #41, App Runner, MotorMarket/DC4, admin ≠ this surface, PoC $0)
- [x] Spec Security checklist points 1–10 answered with Spec section cites (§9)
- [x] Acceptance mapping for Product / Issue #155 (no invent Stories / invent AC beyond strategy What; **no Product AC mapping gaps**)
- [x] Tips CA PASS Soft HOLD SoR **`90120c3`** (#167) + **`6dc48d7`/`c1b4e8c`** + Soft HOLD SoR PASS **`5d93bdf`** (#165) + Arch QA Soft HOLD SoR PASS **`eb11262`** (#166) + **`c28361f`** + **`ff707ae`** + Spec checklist Soft HOLD SoR path cited
- [ ] Spec QA Soft HOLD until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED
- [ ] Soft HOLD SoR twin under `docs/specs/…` (this Spec Soft HOLD SoR PR) — Relates to #155; do not close #155

**Confirm to Chief Spec** when draft ready for Spec QA (after Spec-step Security PASS gate). Soft HOLD Spec QA until then. Soft HOLD invent Stories Soft HOLD invent build Soft HOLD. PoC **$0**. Quiet.
