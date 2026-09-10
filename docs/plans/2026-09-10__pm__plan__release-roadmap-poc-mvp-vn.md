# Dealoware — General release plan (PoC → MVP → V1–V5)

| Field | Value |
|-------|-------|
| **Status** | in-dev / awaiting PMQA |
| **Date** | 2026-09-10 |
| **Author** | Dealoware Product / CPM (PM plan) |
| **DOC-FLOW** | `plans/` · `YYYY-MM-DD__pm__plan__{slug}.md` |
| **Scope** | Stage roadmap only — no Stories opened; no spend/procurement |

---

## Sources cited

| Source | Path | Role |
|--------|------|------|
| CEO original (primary checklist) | `product/CEO-ORIGINAL-BRIEF.md` | Verbatim Artifact/Negotiation/Participant/Owner/AI/Distribution intent + Later CEO decisions |
| Product summary | `product/PRODUCT-BRIEF.md` | Processed goals; claims lock; early platform additions A1–A14; MotorMarket/DC4 separation |
| SA architecture options | `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` | PoC YES modular monolith; AWS = host shape not prod; MVP soft risks |
| SA Architecture QA | `verification/2026-09-10__sa__verification__poc-feasibility-roadmap.md` | Verdict **PASS** |

---

## Claims lock (all stages)

Applies to every stage (PoC through V5) and all public/marketing language:

- **Intermediary only** — platform does not buy/sell/own Artifacts by default.
- **No settlement/checkout** unless later explicit Product/CEO decision.
- **No fake traction / savings % / “at scale”** without evidence.

---

## Consult acknowledgements

Feasibility consults acknowledged (itemized; no invented requirements):

| Role | Acknowledgement |
|------|-----------------|
| **Chief Product** | Stage boundaries locked (PoC / MVP / V1–V5); claims lock; **A8 split Option 1** (A8-minimum meters + hard budgets at MVP; mature metering/owner cost UI at V3). |
| **Chief Architect** | PoC **YES** as modular monolith (.NET); Architecture QA **PASS**; deliverable paths: `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md`, `verification/2026-09-10__sa__verification__poc-feasibility-roadmap.md`. |
| **Chief Developer** | PoC/MVP implementable; **O10** from PoC (simple high-performance .NET core; AWS target host shape); **OIDC-shaped** auth from PoC/MVP; **provider abstraction** introduced at MVP (BYO breadth → V4). |
| **Chief DevOps** | PoC AWS = target host shape **not** prod deploy; MVP minimal footprint; cost/critical names listed for escalate-only (no procure in this plan). |
| **Dealoware Agents** | One first-party bot / basic UI at MVP is OK **without** MCP or public OpenAPI package. |

---

## Cost / critical names (escalate only — do not procure)

Escalate PM → COO → CEO. This plan does **not** authorize spend or purchases.

| Name | Why critical |
|------|----------------|
| AWS hosting baseline | Host shape / later deploy cost |
| LLM API spend | Assistant runtime from MVP thin onward |
| Managed DB + backups | Persistence durability |
| Public HTTPS edge | External API/UI exposure |
| Paid observability beyond free CloudWatch | A11 / O5 maturity path |
| SSO IdP at O9 | Platform-owner SSO (V3) |
| KMS / secrets for PII vault | A9 mature vault (V3) |
| Managed search if SQL outgrown | Discovery scale beyond MVP instant search |
| Paid plugins blocked | Standing non-spend unless escalated |

---

## 1. Purpose

Define the **locked primary-stage placement** of every Dealoware capability ID from the CEO original checklist (D*/P*/O*/X*/L*) plus post-original early additions (A*), so engineering and Product share one release roadmap from **PoC → MVP → V1 → V2 → V3 → V4 → V5**.

This plan:

- Does **not** invent product requirements beyond locked Product + CPM stage intent and cited sources.
- Does **not** open Stories.
- Does **not** spend or buy anything.
- Keeps MotorMarket / DC4 live systems out of Dealoware work.
- States explicitly that **by V5 the original CEO brief is fully covered**.

---

## 2. Stage definitions

| Stage | Intent (Product + CPM lock) |
|-------|-----------------------------|
| **PoC** | Prove Artifact core + 1:1 Negotiation/Offer lifecycle + minimal register/auth + identity-seal **stub**; minimal API; .NET preferred; AWS = **target host shape**, not production deploy. |
| **MVP** | Strictly **1:1**. Full registration/auth; Artifact CRUD; basic instant discovery; complete offer cycle with identity-until-accept + contact on accept; minimal Strategy CRUD + thin Strategy-driven AI Assistant; basic UI and/or one first-party bot; idempotent offers; audit; OTel/logging hooks; A8-minimum meters + hard budgets; MVP-light cost/efficiency tags; basic health. |
| **V1** | Saved search/monitoring; fuller Strategy CRUD + stronger Assistant (still 1:1); notifications; participant analytics; partial OpenAPI/webhooks; harden audit/OTel/idempotency. |
| **V2** | First-class matching; concurrency rules; HITL escalation; multi-party / multi-Artifact. |
| **V3** | Platform-owner admin (users/permissions/roles, artifacts, negotiations/offers); SSO; mature metering/owner cost UI; mature PII vault (retention/erasure). |
| **V4** | Multi-LLM + BYO model; strategy sandbox; vertical templates. |
| **V5** | MCP/agentic breadth; prohibited categories/ToS; settlement-policy doc (intermediary/no escrow); remaining owner analytics/notifications/efficiency maturity; Security/PII remainder; standing L1–L3 called out early remain in force. |

---

## 3. Capability → stage matrix

**Rule:** Every ID has **exactly one primary stage**. “Extends” notes partial earlier delivery or later maturity — they do **not** create a second primary.

**Label:** Rows marked **post-original** are A* early platform additions from `PRODUCT-BRIEF.md` (not verbatim CEO original paragraph).

| ID | Primary stage | Extends / deps / notes | Origin |
|----|---------------|------------------------|--------|
| **D1** | PoC | Artifact Subject (name, description, properties, facts) | Original |
| **D2** | PoC | Artifact Intent | Original |
| **D3** | PoC | Artifact Value (amount + currency; 0..n) | Original |
| **D4** | PoC | Artifact Location (0..n) | Original |
| **D5** | PoC | Artifact Time periods (0..n) | Original |
| **D6** | PoC | Minimal register/auth; **completes** as full registration/auth at MVP | Original |
| **D7** | PoC | 1:1 Negotiation; complementary intents; place Offer | Original |
| **D8** | PoC | Accept / Decline / Counter | Original |
| **D9** | PoC | Close Negotiation (cancels open offers) | Original |
| **D10** | PoC | Negotiation expiration (thin OK in PoC) | Original |
| **P1** | MVP | Artifact CRUD (participant manage); PoC may already persist create/get | Original |
| **P2** | MVP | **Instant search** at MVP; **saved-search / market monitoring completes V1** | Original |
| **P3** | V1 | Fuller Strategy CRUD; **MVP = minimal CRUD + thin assistant only** (partial) | Original |
| **P4** | PoC | Offer cycle **starts** PoC; MVP completes participant initiate/manage | Original |
| **P5** | V1 | Extensive notifications | Original |
| **P6** | MVP | Communication with own AI only (no direct human↔human on-platform) | Original |
| **P7** | MVP | Identity-until-accept + contact on accept; **PoC = seal stub only** | Original |
| **P8** | V1 | Participant analytics (Artifacts / Negotiations / Offers) | Original |
| **P9** | V4 | BYO model; **provider abstraction introduced MVP** | Original |
| **O1** | V3 | Full user management — users, permissions, roles | Original |
| **O2** | V3 | Full artifacts management (owner) | Original |
| **O3** | V3 | Full negotiations/offers management (owner) | Original |
| **O4** | V5 | Platform global analytics | Original |
| **O5** | MVP | Basic health; **mature boards → V5** | Original |
| **O6** | V5 | Platform notifications (owner) | Original |
| **O7** | MVP | MVP-light tags/budgets/kill-switch; **mature → V5** | Original |
| **O8** | V4 | Integration for many reasoning LLM models | Original |
| **O9** | V3 | SSO providers; **OIDC-shaped auth from PoC/MVP** | Original |
| **O10** | PoC | Simple high-performance core, prefer .NET, AWS host shape; **continues all stages** | Original |
| **O11** | V5 | Security and PII remainder; **MVP seal via A9** | Original |
| **X1** | V1 | Stronger Strategy-driven AI Assistant (1:1); **MVP thin partial** | Original |
| **X2** | MVP | One first-party bot and/or basic UI; **OpenAPI/webhooks → V1**; **MCP breadth → V5** | Original |
| **L1** | PoC | Apache License 2.0 — **standing all stages** | Later CEO |
| **L2** | PoC | Public GitHub `https://github.com/ioaikh/dealoware` — **standing all stages** | Later CEO |
| **L3** | PoC | Hosted remains AIKnowHow / Dealoware; **non-goal = free-hosted fork as “the” platform** — **standing all stages** | Later CEO |
| **A1** | V2 | Matching — first-class complementary-intent discovery | Post-original |
| **A2** | MVP | Immutable audit trail; **harden V1** | Post-original |
| **A3** | V2 | Concurrency rules for open offers / exclusive negotiate | Post-original |
| **A4** | V2 | Human-in-the-loop escalation before accept/close | Post-original |
| **A5** | V4 | Strategy sandbox (dry-run) | Post-original |
| **A6** | V4 | Vertical schema templates | Post-original |
| **A7** | V1 | OpenAPI/webhooks at V1; **MCP breadth extends V5** | Post-original |
| **A8** | MVP | **A8-minimum** per-Participant meters + hard budgets (cutoff); **mature V3** (Option 1) | Post-original |
| **A9** | MVP | Seal + contact-on-accept minimum; **mature PII vault (retention/erasure) V3** | Post-original |
| **A10** | V5 | Prohibited categories / ToS | Post-original |
| **A11** | MVP | OpenTelemetry / logging hooks across negotiate steps | Post-original |
| **A12** | V5 | Settlement policy doc: intermediary only / no escrow (unless added later) | Post-original |
| **A13** | V2 | Multi-party / multi-Artifact (MVP remains strictly 1:1) | Post-original |
| **A14** | MVP | Idempotent APIs for offer writes / agent retries | Post-original |

### Primary-ID count check

| Family | IDs | Count |
|--------|-----|-------|
| D* | D1–D10 | 10 |
| P* | P1–P9 | 9 |
| O* | O1–O11 | 11 |
| X* | X1–X2 | 2 |
| L* | L1–L3 | 3 |
| A* | A1–A14 | 14 |
| **Total** | | **49** — each exactly once as primary |

| Primary stage | IDs |
|---------------|-----|
| PoC | D1, D2, D3, D4, D5, D6, D7, D8, D9, D10, P4, O10, L1, L2, L3 |
| MVP | P1, P2, P6, P7, O5, O7, X2, A2, A8, A9, A11, A14 |
| V1 | P3, P5, P8, X1, A7 |
| V2 | A1, A3, A4, A13 |
| V3 | O1, O2, O3, O9 |
| V4 | P9, O8, A5, A6 |
| V5 | O4, O6, O11, A10, A12 |

---

## 4. Per-stage scope

### PoC

**Deliverables (IN)**

- Artifact core **D1–D5** (Subject / Intent / Value / Location / Time).
- Minimal Participant register/auth **D6** (OIDC-shaped principal claims OK).
- 1:1 Negotiation + Offer place / Accept / Decline / Counter / Close **D7–D9**; expiration **D10** (thin OK).
- Participant offer-cycle start **P4** (primary).
- Identity-seal **stub only** (no contact exchange) — supports later **P7** / **A9**.
- Minimal API; **.NET** preferred modular monolith (**O10** primary).
- AWS = **target host shape**, not production deploy.
- Standing license / public repo / hosted-platform posture **L1–L3**.

**Out of scope**

- Strategy engine; AI Assistant runtime.
- Saved-search / market monitoring.
- Platform-owner suite.
- Multi-party / multi-Artifact.
- Settlement / checkout / escrow.
- Contact release on accept (MVP).
- Full registration/auth productization (MVP completes **D6**).

### MVP (strictly 1:1)

**Deliverables (IN)**

- Full registration/auth (completes **D6**).
- Artifact CRUD **P1**.
- Basic discovery **instant search** (**P2** partial; saved-search → V1).
- Complete offer cycle + identity-until-accept + contact on accept **P7** (PoC seal stub → real seal).
- Minimal Strategy CRUD + thin Strategy-driven AI Assistant (**P3** / **X1** partial — full free-form → V1+).
- Basic UI and/or **one** first-party bot channel (**X2** partial).
- **P6** — communication with own AI only.
- **A14** idempotent offers; **A2** audit log; **A11** OTel/logging hooks.
- **A8-minimum** per-Participant meters + hard budgets (cutoff) — Option 1.
- **A9** seal + contact-on-accept minimum.
- **O5** basic health; **O7** MVP-light tags/budgets/kill-switch; **O10** continues.
- Provider abstraction introduced (for later **P9** / **O8**).

**Out of scope**

- Multi-party / multi-Artifact.
- Full free-form strategy.
- Full platform-owner admin.
- SSO (IdP) — shape only until **O9** V3.
- Multi-LLM / BYO breadth.
- AWS **production** deploy.
- Escrow / settlement / checkout.
- MCP; public OpenAPI package.
- Saved-search completion (V1).

### V1

**Deliverables (IN)**

- Saved search / market monitoring (rest of **P2**).
- Fuller Strategy CRUD + stronger Assistant, still 1:1 (**P3** / **X1** fuller).
- **P5** notifications; **P8** participant analytics.
- **A7** partial — OpenAPI / webhooks (MCP breadth → V5).
- Harden **A2** / **A11** / **A14**.

**Out of scope**

- Matching as first-class (**A1** → V2).
- Multi-party (**A13** → V2).
- Platform-owner admin suite (**O1–O3** → V3).
- Mature PII vault retention/erasure (**A9** mature → V3).
- Multi-LLM / BYO (**O8** / **P9** → V4).
- MCP breadth (**X2**/**A7** remainder → V5).

### V2

**Deliverables (IN)**

- **A1** matching (complementary-intent discovery).
- **A3** concurrency rules.
- **A4** HITL escalation.
- **A13** multi-party / multi-Artifact.

**Out of scope**

- Owner admin / SSO / mature vault (V3).
- Sandbox / vertical templates / multi-LLM / BYO (V4).
- MCP / ToS categories / settlement-policy doc / owner analytics-notifications maturity (V5).

### V3

**Deliverables (IN)**

- Platform-owner admin **O1–O3**.
- **O9** SSO.
- **A8** mature metering / owner cost UI.
- **A9** mature PII vault (retention / erasure).

**Out of scope**

- Multi-LLM + BYO + sandbox + vertical templates (V4).
- MCP breadth; prohibited categories; settlement-policy doc; remaining owner O4/O6/O7 maturity; O11 remainder (V5).

### V4

**Deliverables (IN)**

- **O8** multi-LLM.
- **P9** BYO model (builds on MVP provider abstraction).
- **A5** strategy sandbox.
- **A6** vertical templates.

**Out of scope**

- MCP/agentic breadth; A10; A12; remaining owner maturity; O11 remainder (V5).

### V5

**Deliverables (IN)**

- MCP / agentic breadth (rest of **X2** / **A7**).
- **A10** prohibited categories / ToS.
- **A12** settlement policy — intermediary / no escrow (documentation; not escrow product).
- Remaining owner maturity: **O4**, **O6**, and mature **O5**/**O7** boards/controls.
- **O11** Security / PII remainder.
- Standing **L1–L3** remain in force (called out from PoC).

**Out of scope**

- Nothing from the **original CEO brief** left uncovered (see §6).
- Still **non-goals**: MotorMarket/DC4 live systems; free-hosted fork as “the” platform; settlement/checkout product unless later explicit.

---

## 5. Dependencies / risks

| Item | Dependency / risk | Stage impact |
|------|-------------------|--------------|
| Offer lifecycle correctness | Close cancels open offers; 1:1 until V2 | PoC core; must not regress |
| Identity seal → contact on accept | PoC stub → MVP **P7**/**A9**; mature vault needs KMS/secrets (escalate) | Block mature **A9** claims until V3 |
| Thin Assistant + **A8-minimum** | LLM spend without hard budgets is cost-critical | MVP must ship cutoff; escalate spend names |
| Provider abstraction at MVP | Enables **P9**/**O8** without rewrite | Defer BYO breadth to V4 |
| OIDC-shaped auth early | Enables **O9** SSO without auth rewrite | PoC/MVP auth shape |
| Instant search vs saved-search | Instant = MVP; monitors = V1 | Do not treat **P2** complete at MVP |
| Matching / multi-party | **A1**/**A13** after 1:1 hardened | V2 only |
| Free-form Strategy + sandbox | Soft risk if pulled into MVP | Fuller **P3**/**X1** at V1; **A5** at V4 |
| OpenAPI/webhooks vs MCP | Agents OK with one bot without MCP/public OpenAPI at MVP | **A7** V1; MCP V5 |
| AWS prod | PoC/MVP = shape / minimal footprint only | Prod deploy decision separate (escalate) |
| MotorMarket / DC4 | Live inventory, SFTP, logins out | Standing non-goal all stages |
| Claims lock | Intermediary; no settlement unless explicit | **A12** documents policy at V5; no escrow product implied |

---

## 6. Explicit statement — CEO brief coverage by V5

**By V5, the original CEO brief in `product/CEO-ORIGINAL-BRIEF.md` is fully covered** as primary or completed-via-extends delivery:

| Original area | IDs | Covered by |
|---------------|-----|------------|
| Artifact Subject/Intent/Value/Location/Time | D1–D5 | PoC |
| Registered Participant | D6 | PoC → completes MVP |
| Negotiation + Offers + Close + expiration | D7–D10, P4 | PoC (MVP completes participant manage) |
| Participant capabilities 1–9 | P1–P9 | MVP / V1 / V4 per matrix (extends noted) |
| Platform-owner capabilities 1–11 | O1–O11 | PoC (**O10**) / MVP (**O5**, **O7**) / V3 (**O1–O3**, **O9**) / V4 (**O8**) / V5 (**O4**, **O6**, **O11** + mature O5/O7) |
| AI Assistant following Strategy | X1 | MVP thin → V1 primary |
| Distribution: agentic bots + basic UI | X2 | MVP one bot/UI → V1 OpenAPI/webhooks → V5 MCP |
| Later CEO decisions (license, public GitHub, hosted posture) | L1–L3 | PoC primary; standing all stages |

Post-original early additions **A1–A14** are scheduled through V5 per the matrix; they do not replace the original brief checklist.

---

## 7. Non-goals

- **MotorMarket / DC4 live systems** — no live inventory, SFTP feeds, or test logins in Dealoware work (CEO later decision / PRODUCT-BRIEF).
- **Free-hosted fork as “the” platform** — public Apache-2.0 code is welcome; **hosted platform remains AIKnowHow / Dealoware** (**L3**).
- Settlement / checkout / escrow **product** unless later explicit (claims lock; **A12** is policy documentation at V5).
- Inventing Stories, traction, savings %, or “at scale” claims in this plan.
- Procurement or spend authorized by this document (cost names = escalate only).

---

## Status metadata

| Field | Value |
|-------|-------|
| **Document status** | in-dev / awaiting PMQA |
| **Stories opened** | None |
| **Spend / buy** | None — escalate-only cost names listed above |
| **Next gate** | PMQA review of primary-stage matrix + claims lock + non-goals |
