# Spec — PoC Identity-seal stub (no contact exchange)

**Status:** Senior Spec — Security-bound draft ready for Spec QA  
**Date:** 2026-09-20  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY #7 Identity-seal stub (CEO unlock)  
**Issue:** https://github.com/ioaikh/dealoware/issues/7  
**IDs:** PoC seal stub · supports later **P7** / **A9** (MVP)  
**DOC-FLOW:** `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md`  
**Constraints:** No product code. No MotorMarket/DC4. No AWS spend / no invent Stories. Keep **separate** from #5/#6 (extend; do not rewrite). PoC local/$0; ECS Express Mode sketch only. Cost/critical → COO → CEO. Spec QA must not PASS until Security QA confirms Spec-step points. **#8 NOT unlocked**; #18 + participant-data-isolation = post-PoC only — do not expand scope.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #7 AC + OUT | https://github.com/ioaikh/dealoware/issues/7 | Binding acceptance |
| Product identity / contact | `product/PRODUCT-BRIEF.md` | Identity until accept; contact on accept (MVP) |
| Architecture feasibility | `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` | Seal stub; public DTOs omit contact; opaque ids; accept does **not** release contact in PoC |
| Release roadmap | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | PoC seal stub; P7/A9 MVP |
| Participant Spec (#5) | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Principal; public DTO guidance |
| Negotiation Spec (#6) | `specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md` | Accept path; no contact fields |
| Spec Security checklist (binding) | `verification/2026-09-20__security__verification__poc-identity-seal-spec-checklist.md` | Spec-step points **1–10** |

**Product alignment:** Identity protected until successful negotiation; contact exchange on accept is **MVP (P7/A9)** — PoC delivers **stub only**. Mature PII vault retention/erasure deferred (A9 mature → V3). Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 1 | No contact PII in APIs | Negotiation / Offer (and public Participant) API responses do **not** expose counterparty contact PII |
| 2 | Stub precursor | Documented as precursor to MVP identity-until-accept + contact-on-accept (P7/A9) |
| 3 | Verification | Spec requires tests **or** explicit verification notes proving no contact leak on PoC happy-path flows |
| 4 | Public DTOs | Opaque ids only; **no** contact/PII fields on public Participant / Negotiation / Offer DTOs |
| 5 | Accept path | Accept changes offer/negotiation state only — **no** contact release in PoC (extends #6) |
| 6 | Seal representation | Sealed flag / placeholder OK; **no** mature vault |
| 7 | Authn | Reuse #5 principal on protected APIs; fail closed |
| 8 | Host | Extend O10; local/$0; ECS Express sketch; no AWS provision |
| 9 | OUT | Real contact release; mature PII vault; Strategy/AI; **#8** backlog; **#18** + participant-data-isolation post-PoC; Cognito/SSO inventing; MM/DC4; AWS spend |

---

## 1. Seal stub rules + DTO constraints

### What “sealed” means in PoC

| Rule | Spec lock |
|------|-----------|
| Counterparty identity in API payloads | Opaque Participant id (`sub` / Participant id) only — **no** email, phone, address, name-as-contact, or other contact PII fields |
| Storage | PoC **need not** persist real contact fields; if any contact-like column exists for future MVP, it must **never** be serialized on PoC public/party API DTOs |
| Flag / placeholder | Optional `identitySealed: true` (or equivalent documented placeholder) on Negotiation/Offer/Participant public views — stub only |
| Vault | **Not** delivered — no retention/erasure/KMS vault Story |

### Public / party DTOs (binding)

| DTO surface | Allowed | Forbidden |
|-------------|---------|-----------|
| Participant (public or counterparty view) | Opaque id; non-contact bootstrap metadata only if already in #5 and non-PII | Contact PII fields |
| Negotiation get (party) | Negotiation ids, party opaque ids, status, dates, Artifact id | Counterparty contact |
| Offer get / list embedded | Offer ids, from/to opaque ids, status, thin terms | Contact PII; tokens/secrets |
| Accept response | Offer/Negotiation state after Accept | Any new contact/PII fields |

### Relation to #5 / #6

| Spec | This Spec adds |
|------|----------------|
| #5 | Does not invent auth; reinforces public Participant DTO omit contact |
| #6 | Makes Accept non-release a first-class #7 AC with leak-proof verification; does not rewrite offer lifecycle |

---

## 2. #6 Accept path — non-release (binding)

| Step | Requirement |
|------|-------------|
| `POST /offers/{id}/accept` success body | Offer → `Accepted` (+ other open Offers cancelled per #6) — **zero** contact fields |
| Side effects | No “release contact” event, no PII enrichment of DTOs, no email/phone returned to either party |
| MVP handoff | Contact-on-accept is **P7/A9** — out of PoC; stub documents the boundary only |

---

## 3. Leak-proof verification requirement (AC)

SD / QA must prove no contact leak on PoC happy paths. Spec accepts **either**:

1. **Automated tests** asserting Accept / Negotiation get / Offer get responses contain no contact PII field names/values; **or**
2. **Explicit verification notes** in Spec QA / Product QA evidence listing happy-path calls checked and confirming absence of contact PII

Minimum happy paths to cover:

- Create Negotiation → get Negotiation (both parties)
- Place Offer → get Offer
- Accept Offer → inspect Accept response + subsequent Negotiation/Offer gets
- Decline / Counter / Close (ensure no contact smuggling on those responses)

---

## 4. Host / runtime

| Item | Lock |
|------|------|
| Solution | Extend O10 modular monolith; no new host |
| Local | PoC **local / $0** |
| AWS | ECS Express Mode sketch only; no App Runner; no AWS provision / Cognito |
| Secrets | Env/placeholders; never commit secrets; never log raw tokens (#5 hygiene) |

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-20__security__verification__poc-identity-seal-spec-checklist.md`  
Spec QA must not PASS until **Security QA** confirms. **#8** stays backlog. Do **not** pull #18 account-authz / participant-data-isolation into PoC.

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Authn fail-closed | #7-touched Negotiation/Offer reads/writes that can expose another party’s data require validated #5 principal; unauthenticated → 401/403 | §4 Host; Locked #7; #5/#6 cross-ref |
| 2 | Authorization — party-only (narrow) | Only the two 1:1 Negotiation parties; non-party → 404 preferred (or 403) without leaking contact/identity/other Negotiations; **no** #18 tenancy expansion | §1 DTOs; Locked #1; §6 OUT |
| 3 | Public DTOs omit contact/PII | Opaque ids only; no email/phone/real name/address/contact PII on PoC Negotiation/Offer (and public Participant views on those paths) | §1 DTO table; Locked #4 |
| 4 | Accept path does not release contact | Accept/Decline/Counter/Close (+ seal flags) change **state only** — no contact/identity reveal on Accept | §2 Accept; Locked #5 |
| 5 | No contact-exchange surface | Forbid PoC endpoint or Accept payload that returns counterparty contact; real release = MVP P7/A9 | §2; §6 OUT |
| 6 | Stub as precursor (documented) | Document precursor to MVP identity-until-accept + contact-on-accept; do not implement MVP/vault/KMS | Locked #2/#6; Product alignment |
| 7 | No-leak evidence | Tests and/or verification notes on happy-path flows proving no counterparty contact PII | §3 Leak-proof verification |
| 8 | No inventing OUT | Cite #7 AC+OUT only; no Strategy/AI, mature vault, settlement, Cognito/SSO, MM/DC4; **#8** backlog | §6 OUT; Locked #9 |
| 9 | Secrets / host / no spend | Reuse #5 Authorization-header hygiene; local/$0; ECS Express sketch; no Cognito/SSO/IdP | §4 Host; Locked #8 |
| 10 | Traceability + handshake | Cites issue #7 AC+OUT only; Spec QA PASS only after Security QA confirm | Sources; this §5; Done-list |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Real contact release on Accept | MVP **P7** / **A9** |
| Mature PII vault (retention/erasure) | A9 mature → V3 |
| Strategy / AI Assistant | Separate Stories |
| **#8** (any Story beyond seal stub) | **NOT unlocked** — backlog |
| **#18** Strategy/account authz + participant-data-isolation | **Post-PoC only** — do not expand Spec |
| Mature contact-on-accept behavior | MVP **P7**/**A9** |
| Cognito / SSO IdP inventing | No spend |
| MotorMarket / DC4 | Zero |
| AWS provision / App Runner / prod deploy | Local/$0; ECS Express sketch |
| Rewriting #5/#6 Specs | Extend only |

---

## 7. Acceptance mapping (issue #7 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Negotiation/Offer APIs do not expose counterparty contact PII | §1 DTO constraints; §2 Accept |
| Stub documented as precursor to MVP P7/A9 | Locked #2; Product alignment; §6 OUT |
| Tests or notes proving no contact leak | §3 Leak-proof verification |

---

## Done-list (Spec QA / Security QA / Dev Plan / SD)

### Spec QA (Security woven)

- [ ] DOC-FLOW: `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md`
- [ ] Binding sources only; extends #5/#6 without rewrite
- [ ] DTO omit contact; Accept non-release; sealed flag optional; no vault
- [ ] Leak-proof test/notes requirement present
- [ ] **Security checklist points 1–10** bound with cites (§5)
- [ ] Scope: seal stub only — **#8 not unlocked**; no #18 / participant-data-isolation expansion
- [ ] Explicit OUT + AC mapping; no MM/DC4; local/$0
- [ ] **Ask Security QA** confirm Spec-step 1–10 **before** PASS to Chief Spec

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Ensure Participant/Negotiation/Offer DTOs omit contact PII (opaque ids only)
- [ ] Accept path returns state only — no contact release
- [ ] Optional sealed flag/placeholder
- [ ] Automated tests **or** verification notes per §3
- [ ] Do not implement §6 OUT

**Next:** Spec QA verifies → ask Security QA confirm Spec-step 1–10 → Spec QA confirm to **Chief Spec only** (never skip Chief).
