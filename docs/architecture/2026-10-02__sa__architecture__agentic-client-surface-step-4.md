# Architecture options SoR — Spec #155 / Product Step 4 provider-neutral agentic client surface

**Status:** Senior Architect **proposal SoR** for Spec #155 / Product Step 4. Design SoR only. HOLD Architecture QA PASS until SoR CLEAR + Security QA confirms via qa-confirm SoR only (**no** invent points-review SoR). HOLD invent Stories. HOLD build / implementation / spend until separately named CEO unlock. Quiet. PoC **$0**.
**Date:** 2026-10-02
**Author:** Dealoware Senior Architect
**Brief from:** Chief Architect → Senior Architect → Architecture QA (Architecture always Security-critical)
**Issue:** https://github.com/ioaikh/dealoware/issues/155
**Moment IDs:** **SA-REV-STEP4-CLIENT** (this deliverable) · HOLD next until CA design grounding PASS + Arch QA + Security SoR CLEAR
**DOC-FLOW:** `architecture/2026-10-02__sa__architecture__agentic-client-surface-step-4.md`
**Security checklist:** ISSUED (`verification/2026-10-02__security__verification__mvp-sa-step-4-agentic-client-checklist.md`) · § Security 1–10 MET (§6 answers cite existing Option A design)
**Expected Security QA SoR:** `verification/2026-10-02__security__verification__mvp-sa-step-4-agentic-client-qa-confirm.md` (handshake SoR = **qa-confirm only**)

| Field | Value |
|-------|-------|
| Date | 2026-10-02 |
| Story / epic / phase | Spec #155 / Product Step 4 — provider-neutral agentic client surface (negotiation fabric) |
| Binding Product sources | `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 4 (strategy What is the bound) · Product Spec scope lock note `product/2026-10-02__product__note__step4-provider-neutral-client-spec-scope.md` (aligns to strategy What; cite strategy if note not yet SoR CLEAR) · roadmap cite `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` OpenAPI/webhooks → V1 (A7/X2 extend); MCP → V5; multi-LLM/BYO → V4 |
| Host shape | Amazon **ECS Express Mode (Fargate)** HOLD provision — inherit AWS Step 2 Option A SoR; **App Runner OUT** |
| Cost | PoC **$0** · HOLD provision · HOLD spend · HOLD build |
| Tip context | Step 3 #148 CLOSED tip `ff707ae` · AWS SoR #142 tip `c28361f` |
| Status | Senior Architect proposal SoR · § Security 1–10 MET |
| Author | Dealoware Senior Architect |
| Brief from | CA |
| Security | Architecture always Security-critical · checklist ISSUED · § Security 1–10 MET · HOLD Arch QA PASS until Security QA qa-confirm SoR only |

## Sources

| Source | Role |
|--------|------|
| Issue #155 | Spec Step 4 — provider-neutral agentic client surface; `status:in-dev`; Soft HOLD multi-provider lifts for **Step 4 Spec track only** (design SoR — not implementation, not spend) |
| Strategy Step 4 | `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 4 — binding What |
| Product Spec scope lock | `product/2026-10-02__product__note__step4-provider-neutral-client-spec-scope.md` — Spec IN/OUT aligned to strategy What |
| Roadmap cite | OpenAPI/webhooks → **V1**; MCP → **V5**; multi-LLM/BYO → **V4** |
| AWS design SoR #142 | `architecture/2026-10-02__sa__architecture__aws-cloud-design-step-2.md` @ tip `c28361f` — host constraints only; **cite; do not rewrite**; HOLD provision; App Runner OUT |
| Option A secrets/ACL | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3a/§3b dual wall + FieldPolicy — **cite; do not rewrite**; all clients bind the same wall |
| Step 3 admin SoR #148 | `architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` @ tip `ff707ae` — PlatformOwner admin ≠ this surface; **cite; do not rewrite**; HOLD invent conflating PlatformOwner admin with agentic client |
| Participant UI #69 | CLOSED SoR — human Participant UI is not this client surface; HOLD invent replacing #69 |
| Soft #41 | CLOSED via **#66+#67** (provider-neutral Dealoware API + hard wall) — do not reopen; this SoR extends that API as the client surface, it does not invent a second provider plane |
| Gate #27 | CLOSED — do not reopen |
| SA options brief template | `architecture/templates/sa-architecture-options-brief-template.md` |

**Locks (cite; do not change):** Gate **#27 CLOSED** · Soft **#41 CLOSED** via **#66+#67** · HOLD AWS provision/spend · HOLD invent Stories · HOLD invent AC beyond strategy What · HOLD build/implementation/spend until separately named CEO unlock · HOLD invent connector auth / rate limits ahead of CEO · MCP breadth (V5) OUT · multi-LLM/BYO (V4) OUT · HOLD Marketing · HOLD Step 5 until Step 4 CLOSED + CEO invent-confirm · settlement/escrow/checkout OUT · MotorMarket/DC4 OUT · App Runner OUT · PoC **$0**. Soft HOLD multi-provider lifts **only** for this Step 4 Spec-track design SoR — not for implementation, not for spend, not for Steps outside #155.

---

## 1. Purpose

Architecture options SoR for Spec #155 / Product Step 4: a **provider-neutral agentic client surface** on the negotiation fabric. First-party bots and external agentic clients are first-class clients of the **same** negotiation/assistant API (not a Grok-only path). This SoR is the written client-surface contract shape plus **one reference client path designed** (not built). Primary delivery framing is **V1 OpenAPI/webhooks**.

This deliverable is **design SoR only**. HOLD invent Stories. HOLD invent AC beyond strategy What. HOLD build. HOLD provision. HOLD spend. HOLD Architecture QA PASS until SoR CLEAR + Security QA confirms via **qa-confirm SoR only**.

Role boundary: this surface is a **Participant-principal client** of the Dealoware API. It is **not** the PlatformOwner admin surface (Step 3 #148) and **not** the Participant UI (#69).

---

## 2. Options + tradeoffs

| Option | Pros | Cons | Pick |
|--------|------|------|------|
| **A. Same Dealoware negotiation/assistant API (#66+#67) as the only client surface; first-party bots and external agentic clients are first-class clients of that API under Participant principal; FieldPolicy / hard wall (Option A §3b) on every client; identity seal equal on the bot path; contract written as V1 OpenAPI + webhooks; one reference client path designed, not built** | Simplest maintainable; reuses delivered provider-neutral API; one wall; no second mesh; matches strategy What | Does not ship connectors, keys, or rate limits (those stay HOLD); reference path is design only | **Recommended** |
| B. Grok-only special path, external clients later | Fast for one bot | Violates "same API / not Grok-only"; invents a privileged client class | **Reject** |
| C. MCP as the Step 4 primary surface | Broad agent attach story | MCP breadth is **V5**, not this step | **Reject** (defer V5) |
| D. Multi-LLM / BYO model plane as Step 4 | Sounds like "provider-neutral" | Client-surface neutrality ≠ V4 BYO / multi-LLM; invents model breadth | **Reject** (defer V4) |
| E. Separate client gateway / mesh / second cluster | Isolation theater | Invents ops, spend, and a second ACL; HOLD provision | **Reject** |
| F. App Runner client host | Historically simple PaaS | App Runner OUT; inherit Step 2 host only | **Reject** |
| G. Reuse PlatformOwner admin (#148) or Participant UI (#69) as the agentic client surface | Surface reuse | Collapses role boundaries; admin ≠ client; #69 is human UI | **Reject** |
| H. Build reference client / live partner connectors now | Visible demo | Build Soft HOLD until separately named CEO unlock; partners are example classes only | **Reject** |

**Pick: Option A** — design SoR only. HOLD invent Stories. HOLD build. HOLD provision. HOLD invent a Grok-only path, MCP primary, BYO plane, second cluster, or admin conflation.

**Soft HOLD DEFER (not IN):** connector auth beyond contract minimum · rate limits / quotas / paid API keys · MCP (V5) · multi-LLM/BYO (V4) · live partner integrations · Step 5 earn-access.

---

## 3. Recommended architecture design (Option A)

### 3.1 One API, two client classes, one principal

| Concern | Design shape SoR | HOLD |
|---------|------------------|------|
| API | The existing provider-neutral Dealoware negotiation/assistant API (**#66+#67**) is the **only** client surface. No parallel "bot API". | HOLD invent a second public API |
| Client classes | **First-party bots** and **external agentic clients** are first-class clients of that same API | HOLD invent a Grok-only special path |
| Principal | **Participant** principal (the client acts for a Participant). PlatformOwner is not a client of this surface | HOLD invent PlatformOwner admin powers on this API |
| Example classes | External systems named only as **example client classes** (not live partners, not integrated brands) | HOLD invent partner claims |
| Build | Contract + reference path are **design**. No connector, SDK, or hosted client in this SoR | HOLD build until separately named CEO unlock |

### 3.2 Client-surface contract (written shape for Spec)

| Clause | Design shape | HOLD |
|--------|--------------|------|
| Auth | Contract names that every client authenticates as a Participant-bound client of the same API. Mechanism beyond that minimum is **Soft HOLD TBD** — not invented AC | HOLD invent connector auth, API-key product, or IdP ahead of CEO |
| Participant principal | Every call carries Participant principal. No anonymous fabric writes. No PlatformOwner claim on this surface | HOLD invent admin conflation |
| FieldPolicy / hard wall | **All** clients — first-party and external — bind Option A §3a/§3b: API/DB FieldPolicy **and** agent/tool hard wall; same Domain `IFieldPolicy.Evaluate` for all FieldClasses | HOLD invent a thinner wall for bots |
| Identity seal | Identity-until-accept holds **equally** on the bot/agent path and the human path: LoginEmail never in agent/model context; ContactEmail ShareOutbound Accept-gated server-side; no contact/PII on public Participant DTOs | HOLD invent a bot-path seal exception |
| Delivery framing | **V1 OpenAPI + webhooks** is the contract shape Spec describes (roadmap A7/X2 extend) | HOLD invent MCP as this step's surface |

### 3.3 One reference client path (designed, not built)

Document one path an **example** external agentic client class would use:

1. Authenticate as a Participant-bound client of the same API (auth mechanism Soft HOLD TBD — boundary named, not invented).
2. Act only with Participant principal.
3. Call negotiation/assistant operations; FieldPolicy fail-closed on list/get; hard wall on any tool/agent side effects.
4. Receive webhook (or equivalent V1 callback) for state the client is allowed to see — same FieldPolicy allow, no denied FieldClass in the payload.
5. Identity seal identical to the human Participant path.

The path is a **design narrative for Spec**. It is not a built client, not a live partner, and not a Story.

### 3.4 What this surface is not

| Not this | Cite |
|----------|------|
| PlatformOwner admin | Step 3 SoR #148 @ `ff707ae` — admin route surface stays separate |
| Participant UI | #69 CLOSED — human UI is not the agentic client contract |
| MCP marketplace / MCP-primary attach | V5 |
| Multi-LLM / bring-your-own model | V4 |
| Settlement / escrow / checkout | Claims lock |

### 3.5 Host inherit (constraints only)

| Layer | Inherit | HOLD |
|-------|---------|------|
| Compute | Amazon **ECS Express Mode (Fargate)** — same service as MVP fabric, HOLD provision | HOLD invent second cluster; HOLD invent App Runner |
| Data | Managed Postgres HOLD provision — same domain model | HOLD invent store Stories |
| Secrets | Secrets Manager / task role HOLD provision | HOLD invent mature vault or paid API keys as delivered |
| Observability | CloudWatch HOLD paid backends HOLD PII in logs | HOLD invent paid APM; HOLD dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses |
| Provision | Cite AWS SoR #142 @ `c28361f` — do not rewrite | HOLD AWS account/resource provision/spend |

### 3.6 Security binding

- Cite Option A §3a/§3b (do not rewrite). The client surface does not get its own ACL table.
- Fail-closed for every client class. A first-party bot is not a trusted bypass.
- HOLD dump denied FieldClasses into webhooks, logs, metrics, or traces.
- Gate **#27 CLOSED** / Soft **#41 CLOSED** via **#66+#67** — do not reopen. This SoR uses that API; it does not reopen the provider question.
- § Security answers 1–10 **MET** (checklist ISSUED; see §6). Handshake SoR is qa-confirm only.

---

## 4. Explicit IN / OUT / HOLD

### IN (this SoR)

- Architecture options SoR Option A for Spec #155 / strategy Step 4 What
- One provider-neutral negotiation/assistant API; first-party bots and external agentic clients as first-class clients of the same API
- Written client-surface contract shape: auth boundary, Participant principal, FieldPolicy/hard wall on all clients, identity seal equal on the bot path
- One reference client path **designed** (not built)
- Delivery primary framing: **V1** OpenAPI/webhooks
- External systems as **example client classes only**
- Host inherit cite: AWS SoR #142 @ `c28361f` (constraints only)
- Proposed SA-REV-STEP4-CLIENT moment for CA → CPM
- Soft HOLD multi-provider lift **scoped to this Step 4 Spec-track design SoR only**

### OUT

- invent Stories
- invent AC beyond strategy What
- build / implementation / SD connector Stories
- spend / paid API keys / hosted demo
- invent connector auth, rate limits, or quotas ahead of CEO (name as Soft HOLD TBD only)
- MCP breadth (V5)
- multi-LLM / BYO (V4)
- live partner integrations or brand-as-partner claims
- Marketing publish
- AWS account / resource provision / spend
- Step 5 until Step 4 CLOSED + CEO invent-confirm
- settlement / escrow / checkout
- MotorMarket / DC4
- App Runner
- reopen Gate **#27** / Soft **#41**
- conflate PlatformOwner admin (#148) or Participant UI (#69) with this surface
- Grok-only special path
- PoC **$0** claimed as provisioned spend

### HOLD

- HOLD build / implementation / spend until a **separately named** CEO unlock
- HOLD invent Stories
- HOLD invent AC beyond strategy What
- HOLD connector auth / rate limits / paid keys ahead of CEO
- HOLD AWS provision (escalate CA → CPM → COO → CEO)
- HOLD Marketing
- HOLD Step 5
- HOLD MCP (V5) and multi-LLM/BYO (V4)
- Soft HOLD multi-provider remains for implementation and for any track other than this Step 4 design SoR
- HOLD Architecture QA PASS until SoR CLEAR + Security QA **qa-confirm SoR only**
- HOLD Spec content until **CA design grounding PASS**
- Gate **#27 CLOSED** · Soft **#41 CLOSED** via **#66+#67**

---

## 5. Proposed SA architecture-review moments (CA → CPM · `gate:sa-arch-review`)

| Moment ID | Name | Trigger (when) | Scope under review | Next stage HOLD until |
|-----------|------|----------------|--------------------|------------------------|
| **SA-REV-STEP4-CLIENT** | Spec #155 / Product Step 4 provider-neutral agentic client surface | This deliverable — SoR written | Option A same-API client surface + Participant principal + contract shape + reference path designed + V1 OpenAPI/webhooks framing + inherit dual wall + host constraints + IN/OUT/HOLD | HOLD next until **CA design grounding PASS** + Architecture QA + Security SoR CLEAR (**qa-confirm only**; **no** invent points-review SoR) |

**Rules:** Do not unlock invent Stories, build, or provision from SA alone. Soft HOLD multi-provider lift does not unlock implementation or spend.

---

## 6. Security (Architecture always-critical handshake)

**Checklist:** ISSUED (`verification/2026-10-02__security__verification__mvp-sa-step-4-agentic-client-checklist.md`).

**HOLD Architecture QA PASS until Security QA confirms** via expected SoR `verification/2026-10-02__security__verification__mvp-sa-step-4-agentic-client-qa-confirm.md` (**qa-confirm only**).

### § Security answers 1–10 MET

| # | Point | Status | Cite |
|---|-------|--------|------|
| 1 | Provider-neutral same-API first-class clients HOLD Grok-only | **MET** | §2 Option A pick; §3.1 same API, two client classes, Participant principal |
| 2 | Written client-surface contract HOLD invent connector auth / rate limits | **MET** | §3.2 contract clauses (auth boundary, Participant principal, FieldPolicy/hard wall, identity seal); auth mechanism Soft HOLD TBD |
| 3 | Fail-closed dual-wall on all clients (Option A §3a/§3b) | **MET** | §3.2 FieldPolicy/hard wall clause; §3.6 cite Option A §3a/§3b; fail-closed all client classes |
| 4 | Identity seal equal on the bot path HOLD invent identity shortcuts | **MET** | §3.2 identity seal clause; §3.3 step 5 reference path; LoginEmail never in agent/model context; ContactEmail Accept-gated server-side |
| 5 | One reference client path designed HOLD invent build | **MET** | §3.3 reference path designed not built; HOLD build until separately named CEO unlock |
| 6 | V1 OpenAPI/webhooks HOLD invent V4/V5 | **MET** | §3.2 delivery framing V1 OpenAPI/webhooks; §4 OUT MCP (V5) / multi-LLM/BYO (V4) |
| 7 | External systems = example client classes only HOLD invent live partners | **MET** | §3.1 example classes row; §4 OUT live partner integrations |
| 8 | HOLD AWS provision; inherit Step 2 design constraints only | **MET** | §3.5 host inherit cites #142 @ `c28361f` constraints only; HOLD provision |
| 9 | OUT / HOLD locked pack | **MET** | §4 explicit IN/OUT/HOLD; OUT list matches checklist pack |
| 10 | Traceability + handshake SoR pattern | **MET** | Sources table; §5 review moments; §6 handshake; cites strategy Step 4, #66+#67, #69, #148 @ `ff707ae`, #142 @ `c28361f`, #27 CLOSED, Soft #41 CLOSED |

---

## 7. Done-list → Architecture QA

- [x] Options cover CA brief + strategy Step 4 What
- [x] Same API / not Grok-only; Participant principal; dual wall; identity seal; reference path designed; V1 framing
- [x] PlatformOwner admin (#148) and Participant UI (#69) explicitly not this surface
- [x] Host inherit cites AWS SoR #142 @ `c28361f` (HOLD provision; App Runner OUT)
- [x] Explicit IN / OUT / HOLD match strategy What + CA brief
- [x] Review moments table present for CA → CPM (`gate:sa-arch-review`)
- [x] § Security answers 1–10 MET — checklist ISSUED; answers cite existing Option A design (§6)
- [ ] HOLD Architecture QA PASS until SoR CLEAR + Security QA qa-confirm SoR only
- [ ] HOLD Spec content until CA design grounding PASS
- [ ] Confirm to Chief only after that path — HOLD invent Stories · HOLD build

---

## 8. CEO questions

None. Build, spend, connector auth, and paid keys stay HOLD until a separately named CEO unlock. This SoR does not ask for one.

---

## 9. Disposition

SoR ready for **CA design grounding** review. HOLD Spec content until that PASS. HOLD invent Stories. HOLD build / implementation / spend. HOLD provision. HOLD Architecture QA PASS until SoR CLEAR + Security QA qa-confirm SoR only (**no** invent points-review SoR). Soft HOLD multi-provider lifted **only** for this Step 4 Spec-track design SoR. Gate **#27 CLOSED**. Soft **#41 CLOSED** via **#66+#67**. Step 3 tip `ff707ae`. AWS tip `c28361f`. PoC **$0**. Quiet.
