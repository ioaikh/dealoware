# Spec — MVP Stage B: Contact on accept — identity seal → contact (#42)

**Status:** Senior Spec — Security-bound; BA AC locked; triad CLOSE HOLD for Spec Security QA only  
**Date:** 2026-09-22  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY Stage B Specs #40 + #41 + #42 (CEO Stage B unlock)  
**Issue:** https://github.com/ioaikh/dealoware/issues/42  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A (CEO ACCEPTED) · `stage:mvp` · Stage B  
**Prior:** https://github.com/ioaikh/dealoware/issues/7 · PoC identity-seal stub (CLOSED) — **extend**, do not rewrite  
**DOC-FLOW:** `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md`  
**Constraints:** Separate Spec from #40 / #41 (cross-ref only). Extend #7 under ACL; do **not** rewrite #7 CLOSED history. Consume Stage A #31 ShareOutbound Deny until this Story enables grant path. Do not invent Stage C agent share-tool / vault/KMS / LoginEmail-on-Accept. PoC **$0**. Gate **#25** stays **backlog** until Stage B delivery — do **not** open/unlock now. Do **not** unlock #18 Spec/SD as a whole. **BA AC locked.** Cite SA Option A ContactEmail ShareOutbound-after-Accept / HasAcceptGrant. Spec QA must **not** PASS until Security QA confirms. **Triad CLOSE / Dev Plan unlock ping HOLD** until Spec Security QA PASS (Chief clears). Stage C HOLD. No MM/DC4; no Cognito inventing.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #42 AC + OUT | https://github.com/ioaikh/dealoware/issues/42 | Binding acceptance |
| Parent #18 CEO locks | https://github.com/ioaikh/dealoware/issues/18 | Option A dual wall; Stage B named slice |
| Option A architecture (Stage B) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | ContactEmail OwnAgent Read; ShareOutbound only after Accept; HasAcceptGrant; LoginEmail User-only |
| BA note #42 | `plans/2026-09-22__ba__note__story-42-contact-on-accept.md` | Spec-ready refine; P7/A9 minimum; extend #7 |
| Spec Security checklist | `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-checklist.md` | Points **1–10** |
| PoC #7 Spec | `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md` | Precursor seal stub — extend, do not rewrite |
| Stage A #31 Spec | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | ShareOutbound Deny until this Story |
| Stage A #32 Spec | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | List isolation distinct |
| Sibling #40 / #41 Specs | `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md`, `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md` | Cross-ref only — do not merge |

**Product alignment:** CEO Stage B named slice — **P7** / **A9** MVP **minimum** identity-until-accept + contact on accept. Mature PII vault → V3. Stage C HOLD. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | SA Stage B contact | ContactEmail ShareOutbound only after Accept; HasAcceptGrant in resourceContext; LoginEmail User-only (cite SA ContactEmail / ShareOutbound rows) |
| 1 | Pre-Accept seal held | Until Accept, Negotiation/Offer APIs continue to **omit** counterparty contact PII (opaque ids as needed); **extend #7** — **no regression** |
| 2 | Accept grant | User Accept on that offer/negotiation → resourceContext **HasAcceptGrant** (or equivalent) usable by FieldPolicy |
| 3 | ShareOutbound after Accept only | **ShareOutbound(ContactEmail)** may release ContactEmail to counterparty **only** when Accept is recorded on **that** offer/negotiation |
| 4 | Before Accept | ShareOutbound(ContactEmail) **Deny** for all principals (fail-closed) — aligns Stage A #31 Deny until this Story |
| 5 | ContactEmail Stage B rows | **User** R/W; **OwnAgent Read** allowed (not ShareOutbound by default); **Counterparty** Deny until ShareOutbound grant; **Stranger / Unauth** Deny |
| 6 | LoginEmail never on Accept | LoginEmail remains **User-only** (OwnAgent Deny; **not** shared on Accept); do not conflate with ContactEmail |
| 7 | Authn / stranger | Unauthenticated → deny; stranger still deny ContactEmail post-Accept; uniform deny bodies; no private-field leakage |
| 8 | Extend #7 under ACL | Do **not** rewrite #7 CLOSED history; deliver API/DB ShareOutbound-after-Accept + Accept grant (agent share-tool plane = Stage C HOLD) |
| 9 | Scope label | Documented **P7** / **A9** MVP **minimum**; mature vault → **V3**; distinct from #31 registry and #32 list isolation |
| 10 | OUT | Mature vault/KMS; LoginEmail-on-Accept; Stage C agent share tool; discovery (#40); Strategy (#41); gate #25 open; #18 Spec/SD unlock; Cognito/MM/DC4 |

---

## 1. Pre-Accept seal (extend #7)

| Rule | Spec lock |
|------|-----------|
| Negotiation / Offer APIs | Continue to **omit** counterparty contact PII until Accept |
| Opaque ids | Participant opaque ids as needed (align #7 stub) |
| Regression | **No regression** of #7 seal behavior on pre-Accept paths |
| #7 history | Extend seal → contact under ACL; do **not** rewrite #7 CLOSED Spec/claims |

---

## 2. Accept grant + ShareOutbound(ContactEmail)

### HasAcceptGrant

| Item | Spec lock |
|------|-----------|
| Trigger | User Accept recorded on **that** offer/negotiation |
| Context | `resourceContext.HasAcceptGrant` (or equivalent Accept-grant record) available to `IFieldPolicy.Evaluate` |
| Scope | Grant is negotiation/offer-scoped — not a global contact reveal |

### ShareOutbound rules

| When | ShareOutbound(ContactEmail) |
|------|----------------------------|
| Before Accept | **Deny** for all principals (fail-closed) |
| After Accept on that offer/negotiation | May **Allow** release of ContactEmail to authorized counterparty per product rules |
| Stranger / Unauth | Deny (including post-Accept for strangers) |

Channel locked to **ContactEmail** ShareOutbound — do not invent extra PII channels.

---

## 3. ContactEmail + LoginEmail policy rows (Stage B)

| FieldClass | User | OwnAgent | Counterparty | Stranger / Unauth | ShareOutbound |
|------------|------|----------|--------------|-------------------|---------------|
| ContactEmail | R/W | **Read** (not ShareOutbound by default) | Deny until ShareOutbound grant | Deny | **Only after Accept** on that offer/negotiation |
| LoginEmail | R/W | **Deny all** | Deny | Deny | **Never** on Accept |

Do **not** conflate LoginEmail with ContactEmail.

---

## 4. Cross-refs (non-merge) + host / cost

| Spec | Relationship |
|------|--------------|
| #7 | Precursor seal stub — extend under ACL; do not rewrite |
| #31 | Stage A ShareOutbound Deny until this Story enables grant path; consume FieldPolicy |
| #32 | Account-list isolation distinct — do not merge |
| #40 | Discovery must omit ContactEmail (sibling) — separate Story |
| #41 | Strategy ACL separate Story |
| Stage C | Agent `share_contact_email` tool plane HOLD — this Story is API/DB ShareOutbound + Accept grant |

**Host / cost:** Extend O10 modular monolith (Accept-grant record + FieldPolicy ShareOutbound evaluation + DTO projection). Local/$0; ECS Express sketch only; App Runner excluded; no AWS/IdP/vault provision. Mature PII vault out → V3. PoC **$0**.

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-checklist.md`

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Pre-Accept seal held | Omit counterparty contact PII until Accept; extend #7; no regression | Locked #1; §1 |
| 2 | Accept grant record | HasAcceptGrant (or equivalent) in resourceContext for FieldPolicy | Locked #2; §2 HasAcceptGrant |
| 3 | ShareOutbound only after Accept | ContactEmail to counterparty only with Accept on that offer/negotiation; before Accept Deny all | Locked #3/#4; §2 ShareOutbound rules |
| 4 | ContactEmail policy rows (Stage B) | User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny | Locked #5; §3 |
| 5 | LoginEmail never on Accept | LoginEmail User-only; OwnAgent Deny; not shared on Accept | Locked #6; §3 |
| 6 | Authn / stranger fail-closed | Unauth deny; stranger deny ContactEmail post-Accept; uniform deny; no private-field leakage | Locked #7; §2; §4 Host |
| 7 | Extend #7 under ACL, don’t rewrite | Seal → contact under #18/#31 FieldPolicy; #7 history intact; #18 Spec/SD HOLD | Locked #8; §1; §4; Constraints |
| 8 | OUT locked | P7/A9 minimum; mature vault → V3; distinct #31/#32; gate #25 backlog; no Cognito/SSO/MM | Locked #9/#10; §6 OUT |
| 9 | Cost / spend | PoC $0; no IdP/vault required to accept Spec (mature vault out) | §4 Host / cost |
| 10 | Traceability + handshake | Cites #42 AC + #18 Option A Stage B + #7 precursor; Spec QA PASS only after Security QA; keep #40/#41 separate | Sources; this §5; Constraints |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Mature PII vault retention/erasure | A9 mature → **V3** / KMS spend |
| LoginEmail shared on Accept | Never — User-only |
| Extra PII contact channels beyond ContactEmail ShareOutbound | Out |
| Rewriting #7 CLOSED history | Extend only |
| Stage C agent/tool hard wall / share_contact_email tool plane | HOLD |
| Instant search (#40); Strategy ACL (#41) | Siblings — cross-ref only |
| Gate #25 open / unlock before Stage B delivery | Backlog |
| Unlocking #18 Spec/SD as a whole; #26–#27 | HOLD |
| Cognito/SSO inventing | Out |
| MotorMarket / DC4 | Out |

---

## 7. Acceptance mapping (issue #42 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| **Until Accept:** Negotiation/Offer APIs continue to **omit** counterparty contact PII (seal held; opaque participant ids as needed — extend #7 stub behavior, do not regress) | Locked #1; §1 |
| **Accept grant:** When User Accept is recorded on that offer/negotiation, resourceContext carries **HasAcceptGrant** (or equivalent Accept-grant record) usable by FieldPolicy | Locked #2; §2 HasAcceptGrant |
| **On Accept:** authorized **ShareOutbound(ContactEmail)** (or equivalent) may release **ContactEmail** to the counterparty per product rules — only with Accept recorded on **that** offer/negotiation | Locked #3; §2 ShareOutbound rules |
| **Before Accept:** ShareOutbound(ContactEmail) **Deny** for all principals regardless of caller prompt/UI — fail-closed (align Stage A #31 ShareOutbound Deny until this Story enables the grant path) | Locked #4; §2 ShareOutbound rules |
| **ContactEmail** policy rows (Stage B): **User** Read/Write; **OwnAgent Read** allowed (not ShareOutbound by default); **Counterparty** Deny until ShareOutbound grant; **Stranger / Unauth** Deny | Locked #5; §3 |
| **LoginEmail** remains **User-only** (OwnAgent Deny; not shared on Accept) — do **not** conflate LoginEmail with ContactEmail | Locked #6; §3 |
| Automated tests (or equivalent evidence): pre-Accept no contact leak to counterparty; post-Accept ContactEmail available to authorized counterparty only; stranger still deny; LoginEmail never shared on Accept; unauthenticated deny; uniform deny bodies without private-field leakage | §7.1 below; Spec QA + Dev Plan/SD Done-lists |
| Documented as **P7** / **A9** MVP **minimum**; mature PII vault retention/erasure **out → V3**; distinct from account-list isolation (**#32**) and Field ACL registry (**#31**); extends **#7** under ACL (does not rewrite #7) | Locked #8/#9/#10; §6 OUT |

### 7.1 Automated tests detail (binding)

| Case | Expect |
|------|--------|
| Pre-Accept | No contact PII leak to counterparty on Negotiation/Offer APIs |
| Post-Accept | ContactEmail available to **authorized counterparty only** via ShareOutbound grant on that offer/negotiation |
| Stranger | Still **deny** ContactEmail (including post-Accept) |
| LoginEmail | **Never** shared on Accept |
| Unauthenticated | Deny; uniform deny bodies; **no** private-field leakage |

---

## Done-list

### Spec QA

- [ ] DOC-FLOW path; binding sources only; SA ContactEmail ShareOutbound / HasAcceptGrant Stage B rows cited
- [ ] Locks pre-Accept seal held (extend #7, no regression)
- [ ] HasAcceptGrant + ShareOutbound(ContactEmail) only after Accept; before Accept Deny all
- [ ] ContactEmail User R/W + OwnAgent Read + Counterparty Deny until grant; LoginEmail never on Accept
- [ ] Do not rewrite #7; consume #31; distinct from #32
- [ ] **§7 Acceptance mapping complete** — all issue #42 AC bullets present as rows
- [ ] **Automated tests AC** in §7 map + §7.1 detail (pre-Accept no leak; post-Accept counterparty only; stranger deny; LoginEmail never; unauth deny)
- [ ] **Security 1–10** bound (§5) — ask Security QA before PASS
- [ ] Separate from #40/#41; Stage C OUT; gate #25 backlog; #18 Spec/SD HOLD; $0
- [ ] BA AC locked cleared
- [ ] **HOLD triad CLOSE / Dev Plan ping** until Spec Security QA PASS (Chief)

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Keep pre-Accept seal (omit contact PII); no #7 regression
- [ ] Persist Accept grant (HasAcceptGrant) usable by FieldPolicy
- [ ] Enable ShareOutbound(ContactEmail) only after Accept on that offer/negotiation; Deny before Accept
- [ ] Encode ContactEmail / LoginEmail Stage B policy rows; never share LoginEmail on Accept
- [ ] **Automated tests** per §7 / §7.1
- [ ] Do not implement §6 OUT

**Next:** Spec QA verify → ask Security QA → confirm to Chief Spec only. Triad CLOSE after Spec Security QA PASS (Chief). Gate #25 backlog; do not unlock #18 Spec/SD; Stage C HOLD; PoC $0.
