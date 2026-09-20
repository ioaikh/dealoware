# Dev Plan — PoC Identity-seal stub

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-20  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner — PRIORITY PoC #7 Identity-seal stub  
**Issue:** https://github.com/ioaikh/dealoware/issues/7  
**IDs:** PoC seal stub · supports later **P7** / **A9** (MVP)  
**DOC-FLOW:** `plans/2026-09-20__devplan__plan__poc-identity-seal-stub.md`  
**Constraints:** Stub only; build on #5/#6 (extend; do not rewrite); **#8 backlog** (do not invent); no Cognito/SSO/vault/KMS; no inventing **#18** participant-data-isolation; no MM/DC4; no product code beyond SD instructions; cost/critical → CPM

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md` | Seal stub rules, DTO constraints, Accept non-release, leak-proof verification, host, Security §5, OUT §6, AC §7 |
| Spec QA (Spec-side PASS; Security handshake cleared) | `verification/2026-09-20__spec__verification__poc-identity-seal-stub.md` | Spec-side bind; gate unlocked after Security QA |
| Spec Security PASS | `verification/2026-09-20__security__verification__poc-identity-seal-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan |
| Dev Plan Security checklist (must weave 1–10) | `verification/2026-09-20__security__verification__poc-identity-seal-devplan-checklist.md` | Dev Plan-step handshake points **1–10** |
| Participant Spec (#5) — consume only | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Authn principal fail-closed; public DTO guidance; do **not** rewrite |
| Negotiation Spec (#6) — consume only | `specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md` | Accept path; party-only authz; no contact fields; do **not** rewrite |
| #5 Dev Plan (consume auth patterns; do not merge Stories) | `plans/2026-09-11__devplan__plan__poc-participant-d6-auth.md` | Authorization-header / secrets hygiene patterns |
| #6 Dev Plan (consume negotiation patterns; do not merge Stories) | `plans/2026-09-20__devplan__plan__poc-negotiation-offers-d7-d10.md` | Negotiation/Offer API + party authz patterns already delivered |
| Host Spec (O10) | `specs/2026-09-10__spec__spec__poc-o10-scaffold.md` | Extend modular monolith; keep `GET /health` Auth none; local/$0; ECS Express Mode sketch |
| Issue #7 | https://github.com/ioaikh/dealoware/issues/7 | Binding AC + OUT + CPM unlock |

**Product alignment:** Identity protected until successful negotiation; contact exchange on accept is **MVP (P7/A9)** — PoC delivers **stub only**. Mature PII vault retention/erasure deferred (A9 mature → V3). Conflicts → escalate PM → Product → CEO. **#8 remains backlog — do not invent.** **#18** + participant-data-isolation = **post-PoC only** — do not expand scope.

---

## 2. Restated understanding (short)

Executable plan for **SD only**: extend O10 modular monolith (consume #5/#6 — do **not** rewrite) so Negotiation / Offer (and public Participant views on those paths) **omit counterparty contact PII** — **opaque ids only**; Accept / Decline / Counter / Close remain **state-only** (no contact release — extends #6 responses); optional `identitySealed` placeholder if Spec allows; **leak-proof verification** (automated tests and/or explicit notes) on happy-path create/get/place/Accept/Decline/Counter/Close; reuse **#5 authn fail-closed** + **#6 party-only authz** (narrow — **no #18** tenancy expansion); document stub as precursor to MVP **P7/A9**; local/$0; ECS Express sketch; **no** Cognito/SSO/vault/KMS provision; **OUT** #8, real contact release, mature vault, Strategy/AI, #18, Cognito inventing, MM/DC4. No product code in this plan artifact — instructions for SD only.

---

## 3. Locked decisions (Spec locks 1–9 → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 1 | No contact PII in APIs | Negotiation / Offer (and public Participant) API responses do **not** expose counterparty contact PII | Steps 2–4, 6–7 |
| 2 | Stub precursor | Documented as precursor to MVP identity-until-accept + contact-on-accept (P7/A9) | Steps 5, 8, 10 |
| 3 | Verification | Spec requires tests **or** explicit verification notes proving no contact leak on PoC happy-path flows | Step 7 |
| 4 | Public DTOs | Opaque ids only; **no** contact/PII fields on public Participant / Negotiation / Offer DTOs | Steps 2–3 |
| 5 | Accept path | Accept changes offer/negotiation state only — **no** contact release in PoC (extends #6) | Steps 4, 7 |
| 6 | Seal representation | Sealed flag / placeholder OK; **no** mature vault | Step 5 |
| 7 | Authn | Reuse #5 principal on protected APIs; fail closed | Step 6 |
| 8 | Host | Extend O10; local/$0; ECS Express sketch; no AWS provision | Steps 1, 9 |
| 9 | OUT | Real contact release; mature PII vault; Strategy/AI; **#8** backlog; **#18** + participant-data-isolation post-PoC; Cognito/SSO inventing; MM/DC4; AWS spend | Steps 8–10; Explicit OUT |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 solution — Spec §4 / Host Spec O10).  
**Prerequisite:** O10 scaffold present; **#5 Participant auth** and **#6 Negotiation/Offers** delivered (consume only — do **not** rewrite #5/#6 Specs or merge Stories).  
**Hold:** **#8** stays backlog; **#18** + participant-data-isolation post-PoC — do **not** invent. Real contact-on-accept = MVP **P7**/**A9** — do **not** invent.

### Step 1 — Extend O10 solution (host layout; keep health open)

- Keep **`Dealoware.Api` as the only runnable** project.
- Keep **`GET /health`** contract unchanged (**Auth: none**; no DB required for health — O10 Spec §2).
- Extend **Domain / Application / Infrastructure / Api DTO serialization** as needed for seal-stub DTO omit rules, optional seal placeholder, and leak-proof verify — **consume #5/#6**; do **not** rewrite negotiation lifecycle or auth middleware.
- Do **not** add a second host / worker / BFF / gateway.
- Controllers not required; continue Minimal APIs in `Dealoware.Api`.
- Built-in ASP.NET Core DI only.
- Do **not** add Cognito/IdP SDK PackageReferences, vault/KMS SDKs, settlement/payment SDKs, or MM/DC4 refs.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none)
- [ ] No second host; no Cognito/SSO/vault/KMS/AWS/MM provision tasks

### Step 2 — Public / party DTOs: opaque ids only (no contact PII fields)

Per Spec §1 DTO table + Locked #1/#4; extend #5/#6 response shapes without rewriting lifecycle:

| DTO surface | Allowed | Forbidden |
|-------------|---------|-----------|
| Participant (public or counterparty view on Neg/Offer paths) | Opaque id; non-contact bootstrap metadata only if already in #5 and non-PII | Contact PII fields (email, phone, address, name-as-contact, etc.) |
| Negotiation get (party) | Negotiation ids, party opaque ids, status, dates, Artifact id | Counterparty contact |
| Offer get / list embedded | Offer ids, from/to opaque ids, status, thin terms | Contact PII; tokens/secrets |
| Accept / Decline / Counter / Close responses | Offer/Negotiation state after action | Any new contact/PII fields |

**Persistence note (Spec §1):** PoC **need not** persist real contact fields; if any contact-like column exists for future MVP, it must **never** be serialized on PoC public/party API DTOs.

**Tasks:**

1. Audit Negotiation / Offer / public Participant response serializers on #6 paths.
2. Remove or never map contact/PII field names from public/party DTOs.
3. Ensure party identifiers remain **opaque ids** (`sub` / Participant id) only.

**Acceptance:**

- [ ] Negotiation get / Offer get / list embedded expose opaque ids only — no contact PII fields
- [ ] Public Participant views on Neg/Offer paths omit contact PII
- [ ] No contact-like columns serialized on PoC public/party DTOs
- [ ] #5/#6 Specs/plans not rewritten; Stories not merged

### Step 3 — Reinforce Participant public DTO omit contact (#5 consume)

Per Spec §1 Relation to #5 — reinforces public Participant DTO omit contact; does **not** invent auth:

- Consume #5 principal / public DTO guidance.
- On any Participant representation returned via Neg/Offer paths, omit contact/PII.
- Do **not** schedule register/bootstrap changes, Cognito, or password UX.

**Acceptance:**

- [ ] Counterparty Participant views on Neg/Offer paths = opaque id (+ non-PII bootstrap metadata only if already in #5)
- [ ] No new auth mechanism invented; #5 consumed only

### Step 4 — Accept / Decline / Counter / Close remain state-only (extend #6 responses)

Per Spec §2 Accept path + Locked #5; Security Dev Plan points 4–5:

| Action | Plan lock |
|--------|-----------|
| `POST /offers/{id}/accept` success body | Offer → `Accepted` (+ other open Offers cancelled per #6) — **zero** contact fields |
| Decline / Counter / Close | State changes only — **no** contact/identity reveal payloads |
| Side effects | No “release contact” event; no PII enrichment of DTOs; no email/phone returned to either party |
| MVP handoff | Contact-on-accept is **P7/A9** — out of PoC; stub documents the boundary only |

**Explicit:** Do **not** schedule contact-exchange endpoints, Accept payload contact fields, or identity-reveal tasks. Extend #6 response contracts — do **not** rewrite offer lifecycle.

**Acceptance:**

- [ ] Accept response = state only; **no** contact/PII fields
- [ ] Decline / Counter / Close responses = state only; **no** contact smuggling
- [ ] No “release contact” event or PII enrichment wired
- [ ] #6 lifecycle unchanged except DTO/response omit reinforcement

### Step 5 — Optional `identitySealed` placeholder (stub only)

Per Spec §1 Flag / placeholder + Locked #6; Security soft note: must **not** become a contact-release signal.

| Item | Plan lock |
|------|-----------|
| Optional | `identitySealed: true` (or equivalent documented placeholder) on Negotiation/Offer/Participant public views |
| Scope | Stub only — documents sealed posture |
| Forbidden | Using the flag as a contact-release trigger; mature vault; KMS; retention/erasure Story |

**Acceptance:**

- [ ] If implemented: placeholder documented as stub-only; never triggers contact release
- [ ] If omitted: SD documents that Spec allows optional placeholder and that omit-contact DTOs alone satisfy seal stub
- [ ] No vault/KMS/retention/erasure work scheduled

### Step 6 — Reuse #5 authn fail-closed + #6 party-only authz (narrow — no #18)

Per Spec §5 Security points 1–2; Dev Plan Security checklist points 1–2:

**Authn (reuse #5):**

| Item | Plan lock |
|------|-----------|
| Principal | Validated #5 principal required on all #7-touched Negotiation/Offer reads/writes that can expose another party’s data |
| Unauthenticated | Fail closed **`401`** / **`403`** — **no** anonymous identity/contact surfaces |
| Transport | Reuse #5 `Authorization` header hygiene — no tokens in query/body |
| Health | `GET /health` remains open (Auth none) |
| Do not invent | Cognito / SSO / IdP / password / cookie inside #7 |

**Authz (reuse #6; narrow):**

| Item | Plan lock |
|------|-----------|
| Allowed | Only the two 1:1 Negotiation parties |
| Non-party | **`404` preferred** (or `403`) **without** leaking contact / identity / other Negotiations |
| Scope | **Do not** schedule full **#18** tenancy / Strategy / participant-data-isolation work |

**Verify tasks (required):**

- [ ] Unauthenticated Neg/Offer reads/writes on seal-touched surfaces → `401`/`403`
- [ ] Non-party cannot view/mutate that Negotiation/Offer; 404 preferred without contact/identity/cross-Negotiation leak
- [ ] `GET /health` remains open
- [ ] No #18 tenancy/Strategy isolation tasks scheduled

### Step 7 — Leak-proof verification (tests and/or notes)

Per Spec §3 Leak-proof verification + Locked #3; Security Dev Plan point 7.

SD / QA must prove no contact leak on PoC happy paths. Spec accepts **either**:

1. **Automated tests** asserting Accept / Negotiation get / Offer get responses contain no contact PII field names/values; **or**
2. **Explicit verification notes** in Spec QA / Product QA evidence listing happy-path calls checked and confirming absence of contact PII

**Minimum happy paths to cover:**

- Create Negotiation → get Negotiation (both parties)
- Place Offer → get Offer
- Accept Offer → inspect Accept response + subsequent Negotiation/Offer gets
- Decline / Counter / Close (ensure no contact smuggling on those responses)

Also verify: responses/logs for PoC surfaces do **not** emit contact/PII (reuse #5 never-log-raw-tokens hygiene; do not log contact fields).

**Acceptance:**

- [ ] Automated tests **and/or** explicit verification notes cover all minimum happy paths above
- [ ] Evidence proves absence of counterparty contact PII field names/values
- [ ] No contact/PII in PoC surface logs for those flows

### Step 8 — Document as precursor to MVP P7/A9; Explicit OUT gate

Per Spec Locked #2/#6; Product alignment; Security Dev Plan points 5–6, 8:

**Document (required):**

- Stub is precursor to MVP **identity-until-accept + contact-on-accept (P7/A9)**.
- PoC does **not** implement real contact release, mature vault, or KMS.
- Boundary noted in SD handoff / README note as Spec requires (stub only).

**Do not implement (Spec §6 / issue #7 OUT):**

| OUT | Note |
|-----|------|
| Real contact release on Accept | MVP **P7** / **A9** |
| Mature PII vault (retention/erasure) | A9 mature → V3 |
| Strategy / AI Assistant | Separate Stories |
| **#8** (any Story beyond seal stub) | **NOT unlocked** — backlog |
| **#18** Strategy/account authz + participant-data-isolation | **Post-PoC only** — do not expand |
| Mature contact-on-accept behavior | MVP **P7**/**A9** |
| Cognito / SSO IdP inventing | No spend |
| MotorMarket / DC4 | Zero |
| AWS provision / App Runner / prod deploy | Local/$0; ECS Express sketch |
| Rewriting #5/#6 Specs | Extend only |

Also forbid: any PoC endpoint or Accept response that returns counterparty contact; vault/KMS modules; Cognito/SSO modules; MM/DC4 packages.

**Acceptance:**

- [ ] Precursor-to-P7/A9 documented in SD handoff notes
- [ ] Nothing from Spec §6 / issue #7 OUT implemented
- [ ] #8 not unlocked; #18 not invented

### Step 9 — Local/$0; ECS Express sketch; secrets hygiene; zero MM/DC4

Per Spec §4 Host; Locked #8; Security Dev Plan point 9:

**Secrets (reuse #5):**

- [ ] No committed secrets / API keys / cloud credentials / vault keys
- [ ] Signing material + connection strings = env / placeholders only
- [ ] No tokens in **query strings** or Negotiation/Offer **bodies**
- [ ] No logging of **raw** tokens / keys / credentials / contact PII
- [ ] Examples use placeholders only
- [ ] No Cognito/SSO/IdP/vault/KMS SDK PackageReferences added by this Story

**Host / spend:**

- [ ] PoC remains **local / $0**
- [ ] README retains **Amazon ECS Express Mode** (Fargate) **sketch only** — not deployed by this Story
- [ ] **No** App Runner; **no** AWS account / resource / Cognito / vault / KMS provision instructions that create spend
- [ ] If spend ever proposed → escalate **CPM → COO → CEO**

**Zero MM/DC4:**

- [ ] Grep/search: no MotorMarket / DC4 project references, package names, shared libraries
- [ ] No MM/DC4 schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 10 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] O10 extended; Api only runnable; `GET /health` unchanged (Auth none)
- [ ] Public/party Negotiation / Offer / Participant DTOs expose **opaque ids only** — no contact PII fields
- [ ] Accept / Decline / Counter / Close responses = **state only** — no contact release (extends #6)
- [ ] Optional `identitySealed` placeholder either stub-documented or omitted with note — never a release signal; no vault/KMS
- [ ] Leak-proof tests **and/or** verification notes cover create/get/place/Accept/Decline/Counter/Close happy paths
- [ ] No contact/PII in PoC surface responses or logs for those flows
- [ ] #5 authn fail-closed reused; unauthenticated → 401/403; health open
- [ ] #6 party-only authz reused (narrow); non-party → 404 preferred without contact/identity/cross-Negotiation leak; **no #18**
- [ ] Stub documented as precursor to MVP P7/A9
- [ ] Secrets hygiene + zero MM/DC4 + local/$0 + ECS Express sketch; no Cognito/SSO/vault/KMS provision
- [ ] Nothing from Step 8 / Spec §6 OUT implemented; **#8** not invented; **#18** not invented
- [ ] #5/#6 consumed only (not rewritten); no invent Stories; Product conflicts escalated if any

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec §1 Seal stub + DTO constraints | Opaque ids; forbid contact PII; optional seal flag; no vault | Steps 2–3, 5 |
| Spec §2 #6 Accept path non-release | Accept state-only; zero contact fields; no release event | Steps 4, 7 |
| Spec §3 Leak-proof verification | Tests and/or notes on happy paths | Step 7 |
| Spec §4 Host / runtime | Extend O10; local/$0; ECS sketch; secrets | Steps 1, 9 |
| Spec §5 Security Spec checklist 1–10 | Upstream Spec Security bind | Security table §6; all steps |
| Spec §6 Explicit OUT | Real release; vault; Strategy/AI; #8; #18; Cognito; MM/DC4 | Step 8; Explicit OUT |
| Spec §7 / Issue AC: Negotiation/Offer APIs do not expose counterparty contact PII | Spec §1 DTOs; §2 Accept | Steps 2–4, 7 |
| Issue AC: Stub documented as precursor to MVP P7/A9 | Locked #2; Product alignment; §6 OUT | Steps 5, 8, 10 |
| Issue AC: Tests or notes proving no contact leak | Spec §3 | Step 7 |
| #5 Spec consume | Authn fail-closed; public DTO omit contact | Steps 3, 6, 9 |
| #6 Spec consume | Accept path; party-only authz; no contact fields | Steps 2, 4, 6 |
| O10 Host Spec | Extend monolith; keep health Auth none | Steps 1, 9 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-20__security__verification__poc-identity-seal-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-20__security__verification__poc-identity-seal-spec-qa-confirm.md` (points 1–10 MET)  
**Spec Security binding:** Spec §5 points 1–10

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Authn fail-closed tasks** — Plan requires #5 principal on all #7-touched Negotiation/Offer reads/writes; include verify unauthenticated → 401/403 (no anonymous identity/contact surfaces) | Step 6 requires validated #5 principal on seal-touched Neg/Offer APIs; verify unauthenticated → 401/403; health stays open; Locked #7 | Steps 6, 10; Locked #7 |
| 2 | **Party-only authz verify (narrow)** — non-party cannot view/mutate; 404 preferred without contact/identity/cross-Negotiation leak; **do not** schedule full #18 tenancy/Strategy isolation | Step 6 party-only authz + verify tasks; explicitly excludes #18 expansion | Steps 6, 8, 10; Locked #9; Explicit OUT |
| 3 | **DTO omit contact/PII tasks** — Negotiation/Offer (and public Participant views on those paths) expose **opaque ids only** — no email/phone/name-as-contact/address | Steps 2–3 schedule DTO audit/omit; forbid contact field serialization | Steps 2–3, 10; Locked #1/#4 |
| 4 | **Accept path = state-only** — Accept/Decline/Counter/Close (and any seal flag) must **not** include contact release or identity reveal payloads | Step 4 extends #6 responses as state-only; Step 5 seal flag must not release contact | Steps 4–5, 7, 10; Locked #5 |
| 5 | **No contact-exchange surface** — exclude any PoC endpoint or Accept response that returns counterparty contact; real release deferred to MVP **P7**/**A9** | Step 4 forbids release event/PII enrichment; Step 8 OUT lists real contact release as P7/A9 | Steps 4, 8, 10; Locked #5/#9; Explicit OUT |
| 6 | **Stub precursor only** — document stub as precursor to MVP identity-until-accept + contact-on-accept without scheduling vault/KMS or MVP release behavior | Step 5 optional placeholder stub-only; Step 8 documents precursor; no vault/KMS tasks | Steps 5, 8, 10; Locked #2/#6 |
| 7 | **No-leak evidence tasks** — automated tests and/or explicit verification notes proving happy-path create/get/place/Accept/Decline/Counter/Close do **not** expose counterparty contact PII | Step 7 schedules tests and/or notes on all Spec §3 minimum happy paths; log hygiene | Steps 7, 10; Locked #3 |
| 8 | **No inventing OUT** — cite #7 AC+OUT only: no Strategy/AI, mature vault, settlement, Cognito/SSO/IdP, MM/DC4; **#8** stays backlog; **#18** stays out of PoC | Step 8 Explicit OUT gate; Sources cite #7 only; Locked #9 | Steps 8–10; Explicit OUT; Locked #9 |
| 9 | **Host / secrets / no spend** — Local/$0; reuse #5 Authorization-header hygiene; ECS Express sketch only; **no** Cognito/SSO/IdP provision tasks | Step 1/9 host + secrets + zero MM/DC4; Cost/critical; no Cognito/vault/KMS provision | Steps 1, 6, 9–10; Locked #8; Cost/critical |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms these points | See handshake note below. Done-list for Dev Plan QA requires Security QA confirm before PASS | Handshake note; Done-list for Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-20__security__verification__poc-identity-seal-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Security QA confirms.**

---

## 7. Explicit OUT

Mirror Spec §6 / issue #7 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Real contact release on Accept | MVP **P7** / **A9** |
| Mature PII vault (retention/erasure) | A9 mature → V3 |
| Strategy / AI Assistant | Separate Stories |
| **#8** (any Story beyond seal stub) | **NOT unlocked** — backlog |
| **#18** Strategy/account authz + participant-data-isolation | **Post-PoC only** — do not expand |
| Mature contact-on-accept behavior | MVP **P7**/**A9** |
| Cognito / SSO IdP inventing | No spend |
| MotorMarket / DC4 | Zero |
| AWS provision / App Runner / prod deploy | Local/$0; ECS Express sketch |
| Rewriting #5/#6 Specs | Extend / consume only |
| Vault / KMS provision | No spend; not PoC |

---

## 8. Cost/critical

**#7 must not procure AWS / Cognito / SSO / vault / KMS spend.** Any paid AWS provision, Cognito/SSO/IdP spend, vault/KMS spend, or other spend proposal → escalate **CPM → COO → CEO**. PoC remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, Cognito user-pool, vault/KMS, App Runner, or settlement tasks. ECS Express Mode remains **README sketch only**.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-20__devplan__plan__poc-identity-seal-stub.md`
- [ ] Spec coverage §§1–6 + issue #7 AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] Builds on #5/#6 consume-only — **no rewrite**; **#8 backlog** — **not invented**; **#18** out of PoC
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6)
- [ ] Stub only — no real contact release / vault / KMS / Cognito inventing
- [ ] No Cognito/SSO/vault/KMS/AWS spend instructions
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock (+ CPM per ops).

---

## 10. Done-list for SD (after plan QA PASS + Chief confirm)

- [ ] Step 1: Extend O10 (Domain/Application/Infrastructure/Api DTO as needed; Api only runnable; keep `GET /health` Auth none)
- [ ] Step 2: Public/party Negotiation/Offer/Participant DTOs — opaque ids only; no contact PII fields
- [ ] Step 3: Reinforce Participant public DTO omit contact (#5 consume only)
- [ ] Step 4: Accept/Decline/Counter/Close responses state-only — no contact release (extend #6)
- [ ] Step 5: Optional `identitySealed` placeholder stub-only (or omit with note); no vault/KMS
- [ ] Step 6: Reuse #5 authn fail-closed + #6 party-only authz (narrow; no #18); verify unauthenticated/non-party
- [ ] Step 7: Leak-proof tests and/or verification notes on create/get/place/Accept/Decline/Counter/Close
- [ ] Step 8: Document precursor to MVP P7/A9; do not implement Spec §6 OUT; do not invent #8/#18
- [ ] Step 9: Secrets hygiene (#5 patterns); local/$0; ECS Express sketch; zero MM/DC4; no Cognito/SSO/vault/KMS
- [ ] Step 10: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
