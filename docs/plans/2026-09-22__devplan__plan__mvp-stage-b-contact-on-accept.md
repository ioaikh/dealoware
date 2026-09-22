# Dev Plan — MVP Stage B: Contact on accept — identity seal → contact (#42 ONLY)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-22  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner — PRIORITY Stage B #42 ONLY (Contact on accept · P7 / A9 minimum)  
**Issue:** https://github.com/ioaikh/dealoware/issues/42  
**IDs:** Stage B · **P7** / **A9** MVP **minimum** · parent **#18** Option A · prior **#7** · gate **#25** backlog  
**DOC-FLOW:** `plans/2026-09-22__devplan__plan__mvp-stage-b-contact-on-accept.md`  
**Constraints:** **#42 ONLY** — do **not** draft/implement #40/#41 (cross-ref only). ShareOutbound **ContactEmail** on Accept only; keep **#7** seal elsewhere (extend, do not rewrite). Separate from #40/#41. ECS Express sketch; PoC **$0**; no Cognito/MM/DC4; gate **#25** backlog; do **not** unlock #18 Spec/SD as a whole. Stage C agent share-tool HOLD. SD HOLD until Dev Plan QA + Security PASS + CPM unlock. Cost/critical → CPM.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md` | Pre-Accept seal; HasAcceptGrant; ShareOutbound(ContactEmail) after Accept; ContactEmail/LoginEmail rows; Locked #0–#10; §7 AC + §7.1 tests; OUT; Security §5 |
| Spec QA (Spec gate PASS; Security handshake cleared) | `verification/2026-09-22__spec__verification__mvp-stage-b-contact-on-accept.md` | Spec-side PASS; SoR PR #45 Spec + Spec verify; Security SoR PR #44 |
| Spec Security PASS | `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan (SoR PR #44 @ `48ee31c`) |
| Dev Plan Security checklist (must weave 1–10) | `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-devplan-checklist.md` | Dev Plan-step handshake points **1–10** |
| SA Option A (Stage B) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | ContactEmail OwnAgent Read; ShareOutbound only after Accept; HasAcceptGrant; LoginEmail User-only |
| BA note #42 | `plans/2026-09-22__ba__note__story-42-contact-on-accept.md` | Spec-ready refine; P7/A9 minimum; extend #7 |
| PoC #7 Spec — extend only | `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md` | Precursor seal stub — **extend**, do **not** rewrite |
| PoC #7 Dev Plan — consume pattern | `plans/2026-09-20__devplan__plan__poc-identity-seal-stub.md` | Format + seal omit patterns; do **not** merge Stories |
| Stage A #31 Spec — consume FieldPolicy | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | ShareOutbound Deny until this Story enables grant path |
| Stage A #31 Dev Plan — consume | `plans/2026-09-21__devplan__plan__mvp-stage-a-field-acl-registry.md` | IFieldPolicy + ContactEmail ShareOutbound Deny baseline |
| Stage A #32 Spec — cross-ref only | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | List isolation distinct — do **not** merge |
| Sibling #40 Spec — cross-ref only | `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md` | Discovery omit ContactEmail — **separate Story** |
| Sibling #41 Spec — cross-ref only | `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md` | Strategy ACL — **separate Story** |
| Host Spec (O10) | `specs/2026-09-10__spec__spec__poc-o10-scaffold.md` | Extend modular monolith; keep `GET /health` Auth none; local/$0; ECS Express sketch |
| Issue #42 | https://github.com/ioaikh/dealoware/issues/42 | Binding AC + OUT |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Option A dual wall; Stage B named slice; Spec/SD HOLD as whole |
| DOC-FLOW | `meta/DOC-FLOW.md` | Naming / path |

**Product alignment:** CEO Stage B named slice — **P7** / **A9** MVP **minimum** identity-until-accept + contact on accept. Mature PII vault → **V3**. Stage C HOLD. Gate **#25** backlog. Siblings #40/#41 separate. Conflicts → escalate PM → Product → CEO. Cost/critical → CPM.

---

## 2. Restated understanding (short)

Executable plan for **SD only** (#42 ONLY): extend O10 modular monolith (consume #5/#6/#7/#31 — do **not** rewrite those Specs or merge Stories) so that (1) **pre-Accept** Negotiation/Offer APIs continue to **omit** counterparty contact PII (extend #7 seal; **no regression**); (2) User Accept on **that** offer/negotiation persists an **Accept-grant** record exposed as `resourceContext.HasAcceptGrant` (or equivalent) to `IFieldPolicy.Evaluate`; (3) **ShareOutbound(ContactEmail)** may release ContactEmail to the **authorized counterparty only after Accept** on that offer/negotiation — before Accept → **Deny all** (consume #31 Deny until grant path); (4) encode Stage B ContactEmail rows (User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny) and keep **LoginEmail User-only** (never shared on Accept; OwnAgent Deny); (5) authn/stranger fail-closed with uniform deny bodies and no private-field leakage; (6) automated tests per Spec §7 / §7.1. Host: local/$0; ECS Express sketch only; no Cognito/IdP/vault/KMS/AWS provision; no MM/DC4. **OUT:** mature vault; LoginEmail-on-Accept; Stage C agent `share_contact_email` tool; #40/#41; gate #25 open; #18 Spec/SD unlock; Cognito/MM/DC4. No product code in this artifact — SD instructions only.

---

## 3. Locked decisions (Spec locks #0–#10 → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 0 | SA Stage B contact | ContactEmail ShareOutbound only after Accept; HasAcceptGrant in resourceContext; LoginEmail User-only | Steps 2–5 |
| 1 | Pre-Accept seal held | Until Accept, Neg/Offer APIs **omit** counterparty contact PII; **extend #7** — **no regression** | Steps 2, 7, 10 |
| 2 | Accept grant | User Accept → `resourceContext.HasAcceptGrant` (or equivalent) usable by FieldPolicy | Steps 3, 7 |
| 3 | ShareOutbound after Accept only | Release ContactEmail to counterparty **only** when Accept recorded on **that** offer/negotiation | Steps 4, 7 |
| 4 | Before Accept | ShareOutbound(ContactEmail) **Deny** for all principals (fail-closed; align #31) | Steps 4, 7 |
| 5 | ContactEmail Stage B rows | User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny | Steps 5, 7 |
| 6 | LoginEmail never on Accept | LoginEmail remains User-only (OwnAgent Deny; **not** shared on Accept) | Steps 5, 7 |
| 7 | Authn / stranger | Unauth deny; stranger deny ContactEmail post-Accept; uniform deny; no private-field leakage | Steps 6, 7 |
| 8 | Extend #7 under ACL | Do **not** rewrite #7 CLOSED history; API/DB ShareOutbound + Accept grant only (agent share-tool = Stage C HOLD) | Steps 1–2, 8–10 |
| 9 | Scope label | Documented **P7** / **A9** MVP **minimum**; mature vault → **V3**; distinct from #31/#32 | Steps 8, 10 |
| 10 | OUT | Mature vault/KMS; LoginEmail-on-Accept; Stage C agent share tool; #40/#41; gate #25 open; #18 Spec/SD unlock; Cognito/MM/DC4 | Steps 8–10; Explicit OUT |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 modular monolith — Spec §4 / Host Spec O10).  
**Prerequisite:** PoC #5/#6/#7 delivered; Stage A #31 Field ACL registry delivered (ContactEmail ShareOutbound Deny baseline); #32 list isolation delivered (do **not** merge).  
**Sibling HOLD:** #40 Instant search · #41 Minimal Strategy — **cross-ref only**; **do not draft or implement** in this plan.  
**Hold:** Gate **#25** backlog; Stage C agent share-tool; #18 Spec/SD as a whole; Cognito/MM/DC4 — do **not** invent.

### Step 1 — Extend O10 host; keep health open; consume #31 FieldPolicy (no second host)

- Keep **`Dealoware.Api` as the only runnable** project.
- Keep **`GET /health`** contract unchanged (**Auth: none**; no DB required for health — O10 Spec §2).
- Extend **Domain / Application / Infrastructure / Api** as needed for Accept-grant persistence + FieldPolicy ShareOutbound evaluation + DTO projection — **consume #31** `FieldClass` / `IFieldPolicy`; do **not** rewrite #31 registry Story.
- Do **not** add a second host / worker / BFF / gateway.
- Controllers not required; continue Minimal APIs in `Dealoware.Api`.
- Built-in ASP.NET Core DI only.
- Do **not** add Cognito/IdP SDK PackageReferences, vault/KMS SDKs, settlement/payment SDKs, or MM/DC4 refs.
- Do **not** open gate #25; do **not** unlock #18 Spec/SD; do **not** schedule Stage C agent/tool hard wall.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none)
- [ ] No second host; no Cognito/SSO/vault/KMS/AWS/MM provision tasks; gate #25 not opened

### Step 2 — Pre-Accept seal held (extend #7; no regression) — Security weave **1**, **7**

Per Spec §1 + Locked #1/#8; Dev Plan Security checklist point **1** (Pre-Accept seal tasks) + point **7** (Extend #7 under ACL):

| Rule | Plan lock |
|------|-----------|
| Negotiation / Offer APIs | Continue to **omit** counterparty contact PII until Accept |
| Opaque ids | Participant opaque ids as needed (align #7 stub) |
| Regression | **No regression** of #7 seal behavior on pre-Accept paths |
| #7 history | Extend seal → contact under ACL; do **not** rewrite #7 CLOSED Spec/claims/history |

**Tasks:**

1. Audit Neg/Offer (and party Participant views on those paths) serializers — confirm pre-Accept paths still omit ContactEmail / contact PII (consume #7 + #31 Deny).
2. Ensure opaque participant ids remain the only counterparty identity surface pre-Accept.
3. Do **not** edit #7 CLOSED Spec/plan artifacts; document this Story as extend-only.

**Acceptance:**

- [ ] Pre-Accept Neg/Offer responses omit counterparty ContactEmail / contact PII
- [ ] Opaque ids retained where Spec requires
- [ ] #7 CLOSED history untouched; no rewrite of #7 Spec/claims
- [ ] Regression covered by Step 7 automated tests (pre-Accept no leak)

### Step 3 — Persist Accept grant (HasAcceptGrant) for FieldPolicy — Security weave **2**

Per Spec §2 HasAcceptGrant + Locked #2; Dev Plan Security checklist point **2** (Accept-grant tasks):

| Item | Plan lock |
|------|-----------|
| Trigger | User Accept recorded on **that** offer/negotiation |
| Context | `resourceContext.HasAcceptGrant` (or equivalent Accept-grant record) available to `IFieldPolicy.Evaluate` |
| Scope | Grant is **negotiation/offer-scoped** — not a global contact reveal |
| Persistence | Persist Accept-grant record (DB/domain) when Accept succeeds; clear/absent when no Accept on that resource |

**Tasks:**

1. On successful Accept path (#6 lifecycle consume — do **not** rewrite offer state machine), write Accept-grant for that offer/negotiation.
2. Populate `resourceContext.HasAcceptGrant` (or equivalent) when building FieldPolicy evaluation context for ContactEmail ShareOutbound / counterparty projection.
3. Ensure grant is **absent/false** before Accept and for unrelated offers/negotiations.

**Acceptance:**

- [ ] Accept on offer/negotiation → HasAcceptGrant (or equivalent) true for **that** resource only
- [ ] Pre-Accept / other resources → HasAcceptGrant false/absent
- [ ] FieldPolicy can read grant from resourceContext (verified in Steps 4–7)
- [ ] No global “reveal all contacts” flag invented

### Step 4 — ShareOutbound(ContactEmail) only after Accept; Deny before — Security weave **3**

Per Spec §2 ShareOutbound rules + Locked #3/#4; Dev Plan Security checklist point **3** (ShareOutbound-after-Accept tasks):

| When | ShareOutbound(ContactEmail) |
|------|----------------------------|
| Before Accept | **Deny** for all principals (fail-closed; align #31) |
| After Accept on that offer/negotiation | May **Allow** release of ContactEmail to **authorized counterparty** per product rules |
| Stranger / Unauth | Deny (including post-Accept for strangers) |

Channel locked to **ContactEmail** ShareOutbound — do **not** invent extra PII channels.

**Tasks:**

1. Update #31 ContactEmail **ShareOutbound** policy from Stage A Deny-all to: Allow only when `HasAcceptGrant` **and** caller is authorized counterparty (party) for that offer/negotiation.
2. Wire counterparty Neg/Offer (post-Accept) DTO projection to evaluate ShareOutbound(ContactEmail) / Read under grant — release ContactEmail **only** when Allow.
3. Before Accept: ShareOutbound remains Deny for all principals regardless of UI/prompt.
4. Do **not** implement Stage C agent `share_contact_email` tool plane.

**Acceptance:**

- [ ] Before Accept: ShareOutbound(ContactEmail) Deny all principals
- [ ] After Accept on that offer/negotiation: authorized counterparty may receive ContactEmail via ShareOutbound grant
- [ ] Stranger / Unauth still Deny post-Accept
- [ ] No extra PII ShareOutbound channels; no Stage C agent share tool

### Step 5 — Encode ContactEmail + LoginEmail Stage B policy rows — Security weave **4**, **5**

Per Spec §3 matrix + Locked #5/#6; Dev Plan Security checklist points **4** (ContactEmail policy-row tasks) + **5** (LoginEmail never-on-Accept tasks):

| FieldClass | User | OwnAgent | Counterparty | Stranger / Unauth | ShareOutbound |
|------------|------|----------|--------------|-------------------|---------------|
| ContactEmail | R/W | **Read** (not ShareOutbound by default) | Deny until ShareOutbound grant | Deny | **Only after Accept** on that offer/negotiation |
| LoginEmail | R/W | **Deny all** | Deny | Deny | **Never** on Accept |

**Explicit:** Do **not** conflate LoginEmail with ContactEmail. Accept must **never** project LoginEmail to counterparty/OwnAgent/stranger.

**Acceptance:**

- [ ] ContactEmail: User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny
- [ ] ShareOutbound(ContactEmail) gated by Accept grant (Step 4)
- [ ] LoginEmail remains User-only; OwnAgent Deny; **never** shared on Accept
- [ ] Tests in Step 7 assert LoginEmail absent from Accept / counterparty post-Accept responses

### Step 6 — Authn / stranger fail-closed; uniform deny; no private-field leakage — Security weave **6**

Per Spec Locked #7; Spec §7.1; Dev Plan Security checklist point **6**:

| Item | Plan lock |
|------|-----------|
| Authn | Validated #5 principal required on protected Neg/Offer / Accept-grant / ContactEmail projection paths |
| Unauthenticated | Fail closed **`401`/`403`** — bodies omit ContactEmail / LoginEmail / private fields |
| Stranger | Deny ContactEmail **including post-Accept** (no Accept grant for non-party) |
| Uniform deny | Prefer consistent deny status/body shape (align PoC/#32 consistency if already used) — **no** private-field leakage in errors |
| Health | `GET /health` remains Auth none |
| Do not invent | Cognito / SSO / IdP / password / cookie productization inside #42 |

**Verify tasks (required):**

- [ ] Unauthenticated Neg/Offer / Accept-touched surfaces → 401/403; no ContactEmail/LoginEmail/private fields
- [ ] Stranger post-Accept → ContactEmail still denied; uniform deny body without private-field leakage
- [ ] `GET /health` remains open
- [ ] No Cognito/SSO inventing

### Step 7 — Automated tests (binding Spec §7 / §7.1) — Security weave **1–6** evidence

Per Spec §7 + §7.1; Dev Plan Security checklist points **1–6** verify:

| Case | Expect |
|------|--------|
| Pre-Accept | No contact PII leak to counterparty on Negotiation/Offer APIs (extend #7; no regression) |
| Post-Accept | ContactEmail available to **authorized counterparty only** via ShareOutbound grant on that offer/negotiation |
| Stranger | Still **deny** ContactEmail (including post-Accept) |
| LoginEmail | **Never** shared on Accept |
| Unauthenticated | Deny; uniform deny bodies; **no** private-field leakage |
| Before Accept ShareOutbound | Deny for all principals |

**Acceptance:**

- [ ] Automated tests (or equivalent Spec-binding evidence) cover all §7.1 cases above
- [ ] Failures assert status + **absence** of ContactEmail / LoginEmail / private fields where denied
- [ ] Pre-Accept seal regression covered; post-Accept counterparty-only covered

### Step 8 — Document P7/A9 minimum; Explicit OUT gate; siblings separate — Security weave **7**, **8**

Per Spec Locked #8/#9/#10; §6 OUT; Dev Plan Security checklist points **7** (Extend #7 under ACL) + **8** (OUT locked):

**Document (required):**

- Story delivers **P7** / **A9** MVP **minimum** (identity-until-accept + ContactEmail on Accept).
- Extends **#7** under ACL — does **not** rewrite #7.
- Distinct from Field ACL registry (**#31**) and account-list isolation (**#32**).
- Mature PII vault retention/erasure **out → V3**.
- Agent `share_contact_email` tool plane = **Stage C HOLD**.
- Siblings **#40** / **#41** = separate Stories (cross-ref only — **not** implemented here).

**Do not implement (Spec §6 / issue #42 OUT):**

| OUT | Note |
|-----|------|
| Mature PII vault retention/erasure | A9 mature → **V3** / KMS spend |
| LoginEmail shared on Accept | Never — User-only |
| Extra PII contact channels beyond ContactEmail ShareOutbound | Out |
| Rewriting #7 CLOSED history | Extend only |
| Stage C agent/tool hard wall / `share_contact_email` tool plane | HOLD |
| Instant search (#40); Strategy ACL (#41) | Siblings — cross-ref only |
| Gate #25 open / unlock before Stage B delivery | Backlog |
| Unlocking #18 Spec/SD as a whole; #26–#27 | HOLD |
| Cognito/SSO inventing | Out |
| MotorMarket / DC4 | Out |

**Acceptance:**

- [ ] P7/A9 minimum + #7 extend-only documented in SD handoff notes
- [ ] Nothing from Spec §6 / issue #42 OUT implemented
- [ ] #40/#41 not drafted/implemented; gate #25 not opened; #18 Spec/SD not unlocked

### Step 9 — Local/$0; ECS Express sketch; secrets hygiene; zero MM/DC4 — Security weave **9**

Per Spec §4 Host/cost; Locked #9; Dev Plan Security checklist point **9** (Cost / spend):

**Secrets (reuse #5 patterns):**

- [ ] No committed secrets / API keys / cloud credentials / vault keys
- [ ] Signing material + connection strings = env / placeholders only
- [ ] No tokens in **query strings** or Negotiation/Offer **bodies**
- [ ] No logging of **raw** tokens / keys / credentials / ContactEmail / LoginEmail / contact PII
- [ ] Examples use placeholders only
- [ ] No Cognito/SSO/IdP/vault/KMS SDK PackageReferences added by this Story

**Host / spend:**

- [ ] Remains **local / $0** (PoC $0 lock)
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
- [ ] Pre-Accept seal held — Neg/Offer omit counterparty contact PII; #7 not rewritten; no regression
- [ ] HasAcceptGrant (or equivalent) persisted and available to FieldPolicy for that offer/negotiation only
- [ ] ShareOutbound(ContactEmail) Allow only after Accept for authorized counterparty; Deny before Accept for all
- [ ] ContactEmail Stage B rows encoded; LoginEmail never on Accept; fields not conflated
- [ ] Unauth / stranger fail-closed; uniform deny; no private-field leakage
- [ ] Automated tests cover Spec §7 / §7.1 cases
- [ ] Documented as P7/A9 MVP minimum; mature vault → V3; distinct from #31/#32; extends #7 under ACL
- [ ] Secrets hygiene + zero MM/DC4 + local/$0 + ECS Express sketch; no Cognito/SSO/vault/KMS provision
- [ ] Nothing from Step 8 / Spec §6 OUT implemented; #40/#41 not invented; gate #25 not opened; #18 Spec/SD not unlocked; Stage C HOLD
- [ ] #5/#6/#7/#31 consumed only (not rewritten); no invent Stories; Product conflicts escalated if any

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec §1 Pre-Accept seal | Omit counterparty contact PII; opaque ids; extend #7; no regression | Steps 2, 7, 10 |
| Spec §2 HasAcceptGrant | Accept → resourceContext HasAcceptGrant for FieldPolicy | Steps 3, 7 |
| Spec §2 ShareOutbound rules | ContactEmail ShareOutbound only after Accept; Deny before | Steps 4, 7 |
| Spec §3 ContactEmail / LoginEmail matrix | Stage B rows; LoginEmail never on Accept | Steps 5, 7 |
| Spec §4 Cross-refs + host/cost | Consume #31; distinct #32; #40/#41 separate; Stage C HOLD; local/$0; ECS sketch | Steps 1, 8–9 |
| Spec §5 Security Spec checklist 1–10 | Upstream Spec Security bind | Security table §6; all steps |
| Spec §6 Explicit OUT | Vault→V3; LoginEmail-on-Accept; Stage C; #40/#41; #25; #18; Cognito; MM/DC4 | Step 8; Explicit OUT |
| Spec §7 / Issue AC: Until Accept omit contact PII | Locked #1; §1 | Steps 2, 7 |
| Spec §7 / Issue AC: Accept grant HasAcceptGrant | Locked #2; §2 | Steps 3, 7 |
| Spec §7 / Issue AC: On Accept ShareOutbound(ContactEmail) | Locked #3; §2 | Steps 4, 7 |
| Spec §7 / Issue AC: Before Accept Deny all | Locked #4; §2 | Steps 4, 7 |
| Spec §7 / Issue AC: ContactEmail Stage B rows | Locked #5; §3 | Steps 5, 7 |
| Spec §7 / Issue AC: LoginEmail User-only | Locked #6; §3 | Steps 5, 7 |
| Spec §7 / §7.1 Automated tests | Pre-Accept no leak; post-Accept counterparty only; stranger deny; LoginEmail never; unauth deny | Step 7 |
| Spec §7 / Issue AC: P7/A9 min; vault→V3; distinct #31/#32; extend #7 | Locked #8/#9/#10; §6 | Steps 8, 10 |
| #31 Spec consume | FieldPolicy + ShareOutbound Deny until grant path | Steps 1, 4–5 |
| #7 Spec extend-only | Seal held; no rewrite | Steps 2, 8, 10 |
| O10 Host Spec | Extend monolith; keep health Auth none | Steps 1, 9 |

---

## 6. Security Dev Plan-step binding (weave 1–10)

**Binding checklist:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-qa-confirm.md` (points 1–10 MET; SoR PR #44)  
**Spec Security binding:** Spec §5 points 1–10

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Pre-Accept seal tasks** — omit counterparty contact PII until Accept; extend #7; no regression tests | Step 2 schedules omit + opaque ids + #7 extend-only; Step 7 tests pre-Accept no leak | Steps **2**, **7**, **10**; Locked #1 |
| 2 | **Accept-grant tasks** — HasAcceptGrant (or equivalent) in resourceContext on User Accept for that offer/negotiation | Step 3 persists Accept-grant scoped to that offer/negotiation; exposes to FieldPolicy | Steps **3**, **7**; Locked #2 |
| 3 | **ShareOutbound-after-Accept tasks** — ContactEmail ShareOutbound only with Accept grant; before Accept → Deny all; verify | Step 4 enables ShareOutbound only with HasAcceptGrant + authorized counterparty; Deny before; Step 7 verifies | Steps **4**, **7**; Locked #3/#4 |
| 4 | **ContactEmail policy-row tasks** — User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny | Step 5 encodes Stage B ContactEmail matrix; Step 7 tests rows | Steps **5**, **7**; Locked #5 |
| 5 | **LoginEmail never-on-Accept tasks** — LoginEmail User-only; never shared on Accept; distinct from ContactEmail | Step 5 binds LoginEmail User-only / OwnAgent Deny; Step 7 asserts never on Accept | Steps **5**, **7**; Locked #6 |
| 6 | **Authn / stranger fail-closed tasks** — Unauth deny; stranger deny post-Accept; uniform deny bodies; verify | Step 6 authn fail-closed + stranger deny + uniform bodies; Step 7 unauth/stranger cases | Steps **6**, **7**; Locked #7 |
| 7 | **Extend #7 under ACL** — extend seal→contact; do not rewrite #7 history or invent #18 Spec/SD | Steps 2/8 document extend-only; Explicit OUT holds #18 Spec/SD; no #7 rewrite | Steps **2**, **8**, **10**; Locked #8; Explicit OUT |
| 8 | **OUT locked** — P7/A9 minimum; mature vault → V3; gate #25 backlog; no Cognito/MM inventing | Step 8 OUT gate + Product alignment; Sources cite #42 only for this Story; #40/#41 separate | Steps **8–10**; Locked #9/#10; Explicit OUT |
| 9 | **Cost / spend** — PoC **$0**; no IdP/vault provision tasks | Step 1/9 host + secrets; Cost/critical; ECS Express sketch only; no Cognito/vault/KMS provision | Steps **1**, **9–10**; Locked #9; Cost/critical |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms; SD stays HOLD until then | See handshake note below. Done-list for Dev Plan QA requires Security QA confirm before PASS | Handshake note; Done-list for Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Security QA confirms. SD stays HOLD until Dev Plan QA + Security PASS + CPM unlock.**

---

## 7. Explicit OUT

Mirror Spec §6 / issue #42 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Mature PII vault retention/erasure | A9 mature → **V3** / KMS spend |
| LoginEmail shared on Accept | Never — User-only |
| Extra PII contact channels beyond ContactEmail ShareOutbound | Out |
| Rewriting #7 CLOSED history | Extend only |
| Stage C agent/tool hard wall / `share_contact_email` tool plane | HOLD |
| Instant search (#40); Strategy ACL (#41) | Siblings — cross-ref only; **do not draft** |
| Gate #25 open / unlock before Stage B delivery | Backlog |
| Unlocking #18 Spec/SD as a whole; #26–#27 | HOLD |
| Cognito / SSO IdP inventing | No spend |
| MotorMarket / DC4 | Zero |
| AWS provision / App Runner / prod deploy | Local/$0; ECS Express sketch |

---

## 8. Cost/critical

**#42 must not procure AWS / Cognito / SSO / vault / KMS spend.** Any paid AWS provision, Cognito/SSO/IdP spend, vault/KMS spend, or other spend proposal → escalate **CPM → COO → CEO**. Remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, Cognito user-pool, vault/KMS, App Runner, or settlement tasks. ECS Express Mode remains **README sketch only**.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-22__devplan__plan__mvp-stage-b-contact-on-accept.md`
- [ ] Spec coverage §§1–6 + issue #42 AC mapped to plan steps (§5)
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] **#42 ONLY** — #40/#41 cross-ref only (not drafted); #7 extend-only (not rewritten); #31 consumed; #32 distinct; #18 Spec/SD HOLD; gate #25 backlog
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6)
- [ ] ShareOutbound ContactEmail on Accept only; LoginEmail never on Accept; HasAcceptGrant scheduled
- [ ] No Cognito/SSO/vault/KMS/AWS spend instructions; PoC $0; ECS Express sketch only
- [ ] No invented Stories / requirements; no MotorMarket / DC4
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner; **SD HOLD** until then + CPM unlock

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock (+ CPM per ops).

---

## 10. Done-list for SD (after plan QA PASS + Security PASS + Chief confirm)

- [ ] Step 1: Extend O10; Api only runnable; keep `GET /health` Auth none; consume #31 FieldPolicy; no second host / Cognito / vault
- [ ] Step 2: Pre-Accept seal held — omit counterparty contact PII; extend #7; no regression; no #7 rewrite
- [ ] Step 3: Persist Accept grant (HasAcceptGrant) usable by FieldPolicy for that offer/negotiation
- [ ] Step 4: Enable ShareOutbound(ContactEmail) only after Accept for authorized counterparty; Deny before Accept for all
- [ ] Step 5: Encode ContactEmail / LoginEmail Stage B policy rows; never share LoginEmail on Accept
- [ ] Step 6: Authn / stranger fail-closed; uniform deny; no private-field leakage
- [ ] Step 7: Automated tests per Spec §7 / §7.1
- [ ] Step 8: Document P7/A9 minimum; do not implement Spec §6 OUT; do not draft #40/#41; do not open #25; do not unlock #18
- [ ] Step 9: Secrets hygiene; local/$0; ECS Express sketch; zero MM/DC4; no Cognito/SSO/vault/KMS
- [ ] Step 10: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
