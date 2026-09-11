# Spec — PoC Participant minimal register/auth (D6)

**Status:** Senior Spec — Security-bound draft ready for Spec QA  
**Date:** 2026-09-11  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY #5 Participant D6; Security handshake woven before Spec QA PASS  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**IDs:** **D6** · `stage:poc` · enables later **O9** SSO (V3) shape-only  
**DOC-FLOW:** `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md`  
**Constraints:** Keep **#4 and #5 Specs separate**. No product code. No MotorMarket/DC4. No AWS spend / no Cognito IdP. No invent Stories. Spec QA must not PASS until Security QA confirms Spec-step points. Cost/critical → COO → CEO.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #5 AC + OUT + Decisions | https://github.com/ioaikh/dealoware/issues/5 | Binding acceptance; API key/JWT lock |
| Product auth lock | `plans/2026-09-10__ba__note__story-5-poc-auth.md` | PoC = API key/JWT; password @ MVP |
| Architecture feasibility auth | `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` | Minimal Participant; OIDC-compatible claim shape |
| Product · Participant | `product/PRODUCT-BRIEF.md` | Must be registered to use the Platform |
| Host Spec (O10) | `specs/2026-09-10__spec__spec__poc-o10-scaffold.md` | Extend modular monolith; local/$0; ECS Express sketch |
| Spec Security checklist (binding) | `verification/2026-09-11__security__verification__poc-auth-spec-checklist.md` | Spec-step points **1–10** |
| Artifact Spec (parallel; authz) | `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md` | Owner checks remain on #4 APIs |

**Product alignment:** Participant must be registered. PoC auth = **API key / JWT** (Product lock). OIDC-shaped claims for later **O9** SSO without shipping IdP. Password + fuller registration UX @ MVP. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 1 | Register/bootstrap | Can register or bootstrap a Participant principal |
| 2 | PoC mechanism | **API key and/or JWT** issue + validate only |
| 3 | Claims | OIDC-compatible principal claim **shape only** (e.g. `sub`); no SSO IdP |
| 4 | Protected APIs | Authenticated calls required for Artifact create/get/list-own (and Negotiation PoC APIs when #6 lands) |
| 5 | UX | No full UX registration; **no password** productization in PoC (MVP) |
| 6 | Host | Extend O10; local/$0; ECS Express Mode sketch; no AWS IdP/Cognito provision |
| 7 | Authn ≠ authz | Valid token ≠ owns Artifact; owner checks stay in #4 Spec |
| 8 | OUT | SSO O9; platform-owner roles O1; Strategy/AI; MM/DC4; cookie session productization; social login |

---

## 1. Participant principal model

| Field / claim | Notes |
|---------------|-------|
| Stable subject id | OIDC-shaped **`sub`** (or equivalent documented claim) — maps to Participant id used as Artifact `ownerParticipantId` |
| Display/bootstrap metadata | Minimal only as needed to register/bootstrap (no PII vault; identity-seal remains #7) |
| Credential | PoC: issued **API key** and/or **JWT** (signing material env-only) |

Public DTOs must not require contact/PII fields (feasibility identity-seal stub guidance — no vault Story here).

---

## 2. Authn contracts

### Bootstrap / register

| Item | Contract |
|------|----------|
| Purpose | Create a Participant principal and issue PoC credential |
| Mechanism | Documented bootstrap/register endpoint(s) — **no** full UX productization |
| Abuse | Bound for PoC: document rate/abuse note (e.g. local-only / limited bootstrap; reject unbounded open mass creation without control) |
| Privilege | **No** platform-owner roles (O1 out) |
| Success | Returns principal id (`sub`) + issued credential material **once** (API key and/or JWT) — never log raw secrets |

### Issue / validate

| Item | Contract |
|------|----------|
| Issue | Issue API key and/or JWT bound to `sub` |
| Validate | Protected endpoints validate credential before handler logic |
| Lifecycle | Support invalidate/rotate at least as PoC note (re-issue + simple revoke list OK) |
| Forbidden | Perpetual undocumented shared master key in source |

### Transport

| Item | Contract |
|------|----------|
| Credential location | **`Authorization` header** (e.g. `Bearer <jwt>` or documented API-key scheme) |
| Forbidden | Query strings; Artifact request bodies as token carriers |
| Local | HTTP OK for PoC |
| Prod TLS | Deferred (O10 local-until-spend) |

### Protected API surface (authn)

Unauthenticated calls **fail closed** (`401` / `403`) — no silent anonymous owner.

| Endpoint class | Authn required |
|----------------|----------------|
| Artifact `POST /artifacts`, `GET /artifacts/{id}`, `GET /artifacts` (list-own) | **Yes** (once wired with #4) |
| Negotiation / Offer PoC APIs (#6 later) | **Yes** when those land |
| `GET /health` (O10) | **No** (unchanged liveness) |
| Bootstrap/register | Unauthenticated bootstrap allowed only as bounded PoC surface per §2 |

---

## 3. Authz boundary (cross-Spec)

| Concern | Owner Spec |
|---------|------------|
| Valid principal / token | **#5** (this Spec) |
| Owns this Artifact (`ownerParticipantId`) | **#4** Artifact Spec |
| Mapping | `#5.sub` (or equivalent) → `#4.ownerParticipantId` |

Do not collapse authn and authz into one check that skips owner-scope on #4.

---

## 4. Host / runtime

| Item | Lock |
|------|------|
| Solution | Implement in O10 modular monolith (`Dealoware.Api` + Domain/Application/Infrastructure as needed) |
| Local | PoC **local / $0** |
| AWS | ECS Express Mode **sketch only**; **no** App Runner; **no** AWS Cognito/IdP provision without COO→CEO |
| Secrets | Signing keys / API key material via **env / placeholders**; never commit real secrets; **never log raw tokens/keys** |

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-11__security__verification__poc-auth-spec-checklist.md`  
Spec QA must not PASS until **Security QA** confirms.

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Authn required on protected APIs | Artifact create/get/list-own require validated principal; fail closed 401/403; Negotiation later | §2 Protected API surface; Locked #4 |
| 2 | Mechanism lock | API key and/or JWT only; OUT password UX, cookie sessions, SSO IdP, social | Locked #2/#8; §2 Issue/validate |
| 3 | OIDC-shaped claims (shape only) | `sub` (or equiv) maps to owner id; no IdP shipped | §1 Principal model; Locked #3 |
| 4 | Secret handling | Env/placeholders; never commit keys; never log raw tokens | §4 Host; §2 Issue |
| 5 | Transport | Authorization header; not query/Artifact body; local HTTP OK; prod TLS deferred | §2 Transport |
| 6 | Lifecycle | Issue + validate; PoC revoke/rotate note; no master key in source | §2 Issue/validate |
| 7 | Register/bootstrap bounded | Abuse/rate note; seed/bootstrap OK if documented; no platform-owner privilege | §2 Bootstrap; Locked #8 |
| 8 | Authn ≠ authz | Owner checks remain on #4 APIs | §3 Authz boundary; Artifact Spec cross-ref |
| 9 | Zero MM/DC4 + no scope creep | No MM/DC4 federation; no Cognito/IdP spend; local/$0; ECS Express sketch | §4 Host; §6 OUT |
| 10 | Traceability + handshake | Cites issue #5, BA note, feasibility; Spec QA PASS only after Security QA | Sources; this §5; Done-list |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Full UX registration productization | MVP |
| Password-based productized login | MVP with fuller reg |
| SSO IdP (**O9**) | V3; shape only now |
| Platform-owner users/roles (**O1**) | V3 |
| Cookie session productization / social login | Out of PoC |
| Strategy / AI / settlement | Unrelated Stories |
| MotorMarket / DC4 identity | Zero |
| AWS Cognito / managed IdP provision | No spend default |
| Inventing #4 Artifact domain or #6 Negotiation inside this Spec | Keep separate |

---

## 7. Acceptance mapping (issue #5 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Register or bootstrap Participant principal | §2 Bootstrap |
| API key / JWT issue/validate | §2 Issue/validate; Locked #2 |
| Authenticated calls for Artifact / Negotiation PoC APIs | §2 Protected API surface |
| OIDC-compatible claim shape (no SSO IdP) | §1; Locked #3 |
| No full UX / no password in PoC | Locked #5; §6 OUT |

---

## Done-list (Spec QA / Security QA / Dev Plan / SD)

### Spec QA

- [ ] DOC-FLOW: `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md`
- [ ] Binding sources only; Product API key/JWT lock
- [ ] Register/bootstrap + issue/validate + Authorization header transport
- [ ] OIDC-shaped `sub` only; no IdP
- [ ] Protected Artifact APIs fail closed; authn ≠ authz (#4 owns owner checks)
- [ ] **Security points 1–10** bound with cites (§5)
- [ ] Explicit OUT; no MM/DC4; local/$0; no Cognito spend
- [ ] Keep separate from #4 Spec
- [ ] **Ask Security QA** confirm Spec-step 1–10 **before** PASS to Chief Spec

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Participant bootstrap/register + API key/JWT issue/validate
- [ ] Wire Authorization header authn on Artifact create/get/list-own
- [ ] Map `sub` → `ownerParticipantId` for #4 authz
- [ ] Env-only secrets; no raw token logging; PoC revoke/rotate note
- [ ] Do not implement §6 OUT

**Next:** Spec QA → Security QA confirm → Spec QA confirm to **Chief Spec only**.
