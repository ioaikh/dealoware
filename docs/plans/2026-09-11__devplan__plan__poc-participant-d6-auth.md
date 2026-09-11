# Dev Plan — PoC Participant minimal register/auth (D6)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-11  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner — PRIORITY PoC #5 Participant D6  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**IDs:** **D6** · `stage:poc` · enables later **O9** SSO (V3) shape-only  
**DOC-FLOW:** `plans/2026-09-11__devplan__plan__poc-participant-d6-auth.md`  
**Constraints:** Keep **#4 and #5 separate** (coordinate fail-closed handoff; do not merge Stories). No product code beyond SD instructions. No MotorMarket/DC4. No Cognito/SSO/IdP/AWS spend (cost/critical → CPM). No invent requirements. Product conflicts → PM → Product → CEO.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Principal model, authn contracts, authz boundary, host, Security §5, OUT §6, AC §7 |
| Spec QA (Spec-side PASS; Security handshake cleared) | `verification/2026-09-11__spec__verification__poc-participant-d6-auth.md` | Spec-side bind; gate unlocked after Security QA |
| Spec Security PASS | `verification/2026-09-11__security__verification__poc-auth-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan |
| Dev Plan Security checklist (must weave 1–10) | `verification/2026-09-11__security__verification__poc-auth-devplan-checklist.md` | Dev Plan-step handshake points **1–10** |
| #4 Spec (cross-ref; authz stays on #4) | `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md` | Owner-scope checks remain on Artifact APIs; `sub` → `ownerParticipantId` |
| #4 Dev Plan (coordinate fail-closed handoff; do not merge Stories) | `plans/2026-09-11__devplan__plan__poc-artifact-d1-d5.md` | Parallel plan — handoff so Artifact create/get/list-own fail closed once auth middleware lands |
| Host Spec (O10) | `specs/2026-09-10__spec__spec__poc-o10-scaffold.md` | Extend modular monolith; keep `GET /health` Auth none; local/$0; ECS Express Mode sketch |
| Product auth lock | `plans/2026-09-10__ba__note__story-5-poc-auth.md` | PoC = API key/JWT; password @ MVP |
| Issue #5 | https://github.com/ioaikh/dealoware/issues/5 | Binding AC + OUT + Decisions |

**Product alignment:** Participant must be registered to use the Platform. PoC auth = **API key / JWT** (Product lock). OIDC-shaped claims for later **O9** SSO without shipping IdP. Password + fuller registration UX @ MVP. Conflicts → escalate PM → Product → CEO.

---

## 2. Restated understanding (short)

Executable plan for **SD only**: extend O10 modular monolith with a **Participant principal** (OIDC-shaped **`sub`** mapping to Artifact `ownerParticipantId`); bounded PoC **bootstrap/register** surface (abuse note; no O1 RBAC); **issue + validate** API key and/or JWT only (OUT password/cookie/SSO/Cognito/social); transport via **`Authorization` header** only (forbid query/body tokens); secrets/signing via **env only** (never commit/log raw tokens/keys; README placeholders); PoC lifecycle **issue + validate + simple invalidate/rotate** note (no master key in source); protect Artifact create/get/list-own — **fail closed 401/403** once wired; **coordinate with #4** (no silent anonymous owner; do not merge Stories); **authn ≠ authz** — keep #4 owner-scope checks after middleware; **local/$0**; ECS Express Mode sketch only; **no Cognito/IdP provision**. Explicit OUT per Spec §6. No product code in this plan artifact — instructions for SD only. **Keep #4/#5 separate.**

---

## 3. Locked decisions (Spec locks 1–8 → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 1 | Register/bootstrap | Can register or bootstrap a Participant principal | Steps 2–3 |
| 2 | PoC mechanism | **API key and/or JWT** issue + validate only | Steps 4–5 |
| 3 | Claims | OIDC-compatible principal claim **shape only** (e.g. `sub`); no SSO IdP | Steps 2, 8 |
| 4 | Protected APIs | Authenticated calls required for Artifact create/get/list-own (and Negotiation PoC APIs when #6 lands) | Steps 7–8 |
| 5 | UX | No full UX registration; **no password** productization in PoC (MVP) | Steps 3, 11 |
| 6 | Host | Extend O10; local/$0; ECS Express Mode sketch; no AWS IdP/Cognito provision | Steps 1, 9–10 |
| 7 | Authn ≠ authz | Valid token ≠ owns Artifact; owner checks stay in #4 Spec | Steps 7–8 |
| 8 | OUT | SSO O9; platform-owner roles O1; Strategy/AI; MM/DC4; cookie session productization; social login | Steps 11–12 |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 solution — Spec §4 / Host Spec O10).  
**Prerequisite:** O10 scaffold present (`Dealoware.sln`, `src/Dealoware.Api` only runnable, Domain / Application / Infrastructure as delivered by #3).  
**Parallel Story:** #4 Artifact Dev Plan remains separate — coordinate handoff so Artifact APIs fail closed once this auth middleware lands; do **not** merge Stories or invent #4 domain here.

### Step 1 — Extend O10 solution (host layout; keep health open)

- Keep **`Dealoware.Api` as the only runnable** project.
- Keep **`GET /health`** contract unchanged (**Auth: none**; no DB required for health — O10 Spec §2).
- Extend **Domain / Application / Infrastructure** as needed for Participant principal, credential issue/validate, and auth middleware (fill placeholders where Spec requires).
- Do **not** add a second host / worker / BFF / gateway.
- Controllers not required; continue Minimal APIs in `Dealoware.Api`.
- Built-in ASP.NET Core DI only.
- Do **not** add Cognito/IdP SDK PackageReferences or provision AWS IdP resources.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none)
- [ ] No second host; no Cognito/IdP/AWS provision tasks

### Step 2 — Participant principal model (OIDC-shaped `sub`)

Implement principal model per Spec §1:

| Field / claim | Notes |
|---------------|-------|
| Stable subject id | OIDC-shaped **`sub`** (or Spec-equivalent documented claim) — maps to Participant id used as Artifact **`ownerParticipantId`** (#4) |
| Display/bootstrap metadata | Minimal only as needed to register/bootstrap (no PII vault; identity-seal remains #7) |
| Credential | PoC: issued **API key** and/or **JWT** (signing material env-only — Step 6) |

Public DTOs must **not** require contact/PII fields (Spec §1).

**Acceptance:**

- [ ] Principal has stable `sub` (or documented equivalent)
- [ ] Mapping `sub` → Artifact `ownerParticipantId` is documented for #4 consume path
- [ ] No PII vault / identity-seal Story invented here

### Step 3 — Bootstrap / register (bounded PoC surface)

Per Spec §2 Bootstrap / register:

| Item | Plan lock |
|------|-----------|
| Purpose | Create a Participant principal and issue PoC credential |
| Mechanism | Documented bootstrap/register endpoint(s) — **no** full UX productization |
| Abuse | Bound for PoC: document rate/abuse note (e.g. local-only / limited bootstrap; reject unbounded open mass creation without control) |
| Privilege | **No** platform-owner roles (**O1 out**) |
| Success | Returns principal id (`sub`) + issued credential material **once** (API key and/or JWT) — **never log raw secrets** |
| Authn on bootstrap | Unauthenticated bootstrap allowed **only** as this bounded PoC surface |

**Acceptance:**

- [ ] Bootstrap/register creates principal + issues credential once
- [ ] Abuse/rate note documented (PoC bound)
- [ ] No O1 platform-owner RBAC
- [ ] Raw credential material not logged

### Step 4 — Issue + validate API key and/or JWT only

Per Spec §2 Issue / validate + Locked #2:

| Item | Plan lock |
|------|-----------|
| Issue | Issue API key and/or JWT bound to `sub` |
| Validate | Protected endpoints validate credential **before** handler logic |
| **OUT of this Story** | Password UX; cookie session productization; SSO IdP; social login; **Cognito provision** |

**Acceptance:**

- [ ] Issue path produces API key and/or JWT bound to `sub`
- [ ] Validate path rejects invalid/missing credentials on protected routes
- [ ] No password/cookie/SSO/Cognito/social implementation scheduled

### Step 5 — Authorization header transport

Per Spec §2 Transport:

| Item | Plan lock |
|------|-----------|
| Credential location | **`Authorization` header** (e.g. `Bearer <jwt>` or documented API-key scheme) |
| **Forbidden** | Query strings; Artifact request bodies as token carriers |
| Local | HTTP OK for PoC |
| Prod TLS | Deferred (O10 local-until-spend) |

Tasks and tests must **not** pass tokens via query or body.

**Acceptance:**

- [ ] Clients/docs use `Authorization` header only
- [ ] Tests forbid query-string / body token passing
- [ ] No prod TLS delivery claimed

### Step 6 — Secrets / signing via env only

Per Spec §4 Host secrets + Spec §2 Issue:

- Signing keys / API key material via **env / placeholders only**
- **Never commit** real secrets/keys to `appsettings*`, README, Dockerfile, launchSettings, source, comments
- **Never log raw tokens/keys**
- README documents **placeholders** only (env var names + example placeholder values)

**Acceptance:**

- [ ] Signing/API-key material from env only
- [ ] No committed real secrets
- [ ] No raw token/key logging
- [ ] README placeholders present

### Step 7 — Lifecycle (issue + validate + simple invalidate/rotate)

Per Spec §2 Issue / validate lifecycle:

| Item | Plan lock |
|------|-----------|
| Issue | Supported (Step 4) |
| Validate | Supported (Step 4) |
| Invalidate / rotate | At least as **PoC note**: re-issue + simple revoke list OK |
| **Forbidden** | Perpetual undocumented shared **master key in source** |

**Acceptance:**

- [ ] Issue + validate delivered
- [ ] Simple invalidate/re-issue or rotate note documented (and minimal revoke path if SD implements list)
- [ ] No hard-coded master key in source

### Step 8 — Protect Artifact create/get/list-own (fail closed; coordinate #4)

Per Spec §2 Protected API surface + Spec §3 Authz boundary + Dev Plan Security checklist points 1 & 8:

| Endpoint class | Authn required |
|----------------|----------------|
| Artifact `POST /artifacts`, `GET /artifacts/{id}`, `GET /artifacts` (list-own) | **Yes** (once wired with #4) |
| Negotiation / Offer PoC APIs (#6 later) | **Yes** when those land — **do not invent #6 here** |
| `GET /health` (O10) | **No** (unchanged liveness) |
| Bootstrap/register | Bounded unauthenticated PoC surface (Step 3) |

**Handoff with #4 (do not merge Stories):**

1. Add auth middleware / filter that validates API key/JWT and resolves principal `sub`.
2. Wire so Artifact create/get/list-own **require** validated principal.
3. Unauthenticated → **fail closed `401` / `403`** — **no silent anonymous owner**.
4. After auth middleware, **#4 owner-scope checks remain** (`ownerParticipantId` == caller) — **authn ≠ authz** (valid token ≠ owns Artifact).
5. Map `#5.sub` (or equivalent) → `#4.ownerParticipantId` for #4 consume path (coordinate with #4 Dev Plan Steps 4–5; do not re-implement Artifact domain in #5).
6. Until middleware is live, #4 may keep interim `X-PoC-Owner-Id` bridge per #4 plan — once wired, prefer #5 principal and fail closed without anonymous owner.

**Acceptance:**

- [ ] Artifact create/get/list-own require validated principal once wired
- [ ] Missing/invalid credential → 401/403 fail closed (no silent anonymous owner)
- [ ] #4 owner-scope authz remains after middleware
- [ ] `sub` → `ownerParticipantId` mapping documented for #4
- [ ] Stories #4 and #5 remain separate (no merged domain delivery)
- [ ] `GET /health` stays open (Auth none)

### Step 9 — Local/$0 + ECS Express sketch (no Cognito/IdP)

| Item | Plan lock |
|------|-----------|
| Local | PoC **local / $0** |
| AWS | ECS Express Mode **sketch only** (README); **no** App Runner; **no** AWS Cognito/IdP provision |
| Spend | Any Cognito/IdP/AWS spend proposal → escalate **CPM → COO → CEO** |

**Acceptance:**

- [ ] No Cognito/IdP/AWS provision tasks in this Story
- [ ] README retains local/$0 + ECS Express Mode sketch (no App Runner)

### Step 10 — Zero MM/DC4 verify

- [ ] Grep/search: no MotorMarket / DC4 project references, package names, shared libraries
- [ ] No MM/DC4 schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 11 — Explicit OUT (do **not** implement Spec §6)

SD must **not** deliver any of:

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
| Inventing #4 Artifact domain or #6 Negotiation inside this Spec/plan | Keep separate |

### Step 12 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] O10 extended; Api only runnable; `GET /health` unchanged (Auth none)
- [ ] Participant principal with OIDC-shaped `sub`; maps to Artifact `ownerParticipantId`
- [ ] Bootstrap/register bounded (abuse note; no O1 RBAC); credential issued once; no raw secret logging
- [ ] API key and/or JWT issue + validate only; OUT password/cookie/SSO/Cognito/social
- [ ] `Authorization` header transport; tests forbid query/body tokens
- [ ] Secrets/signing env-only; never commit/log raw tokens/keys; README placeholders
- [ ] Lifecycle: issue + validate + simple invalidate/rotate note; no master key in source
- [ ] Artifact create/get/list-own protected fail-closed 401/403 once wired; coordinated with #4 (no silent anonymous owner)
- [ ] Authn ≠ authz — #4 owner-scope checks remain after middleware
- [ ] Local/$0; ECS Express sketch; no Cognito/IdP/AWS provision
- [ ] Zero MM/DC4 gate passed (Step 10)
- [ ] Nothing from Step 11 / Spec §6 OUT implemented
- [ ] No invent Stories; #4/#5 remain separate; Product conflicts escalated if any

---

## 5. Spec §§1–7 + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec §1 | Participant principal model (`sub`, metadata, credential) | Steps 2, 6 |
| Spec §2 | Authn contracts — bootstrap, issue/validate, transport, protected surface | Steps 3–5, 7–8 |
| Spec §3 | Authz boundary (authn ≠ authz; #4 owns owner checks; `sub` → `ownerParticipantId`) | Steps 2, 8 |
| Spec §4 | Host / runtime (extend O10; local/$0; ECS sketch; secrets env) | Steps 1, 6, 9–10 |
| Spec §5 | Security Spec checklist binding 1–10 | Security table §6; all steps |
| Spec §6 | Explicit OUT | Step 11 |
| Spec §7 | Acceptance mapping (issue #5 AC) | This table + Steps 2–8 |
| Issue AC: Register or bootstrap Participant principal | Spec §2 Bootstrap | Steps 2–3 |
| Issue AC: API key / JWT issue/validate | Spec §2 Issue/validate; Locked #2 | Steps 4–5, 7 |
| Issue AC: Authenticated calls for Artifact / Negotiation PoC APIs | Spec §2 Protected API surface | Step 8 |
| Issue AC: OIDC-compatible claim shape (no SSO IdP) | Spec §1; Locked #3 | Steps 2, 8, 11 |
| Issue AC: No full UX / no password in PoC | Locked #5; Spec §6 OUT | Steps 3, 11 |
| Product BA lock | API key/JWT; password @ MVP | Steps 4, 11 |
| #4 Spec / Dev Plan cross-ref | Consume principal; fail-closed handoff; keep separate | Steps 8, 11–12 |
| O10 Host Spec | Extend monolith; keep health Auth none; local/$0; ECS sketch | Steps 1, 9 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-11__security__verification__poc-auth-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-11__security__verification__poc-auth-spec-qa-confirm.md` (points 1–10 MET)  
**Spec Security binding:** Spec §5 points 1–10

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Protect Artifact APIs** — require validated principal on Artifact create/get/list-own; unauthenticated → fail closed (401/403); coordinate with #4 (no silent anonymous owner) | Step 8 wires middleware; fail closed 401/403; handoff with #4 Dev Plan; health stays open | Steps 1, 8, 12; Locked #4 |
| 2 | **Mechanism tasks only** — schedule API key and/or JWT issue+validate only; **Out:** password UX, cookie sessions, SSO IdP, social login, Cognito provision | Steps 4–5 issue/validate only; Step 11 OUT lists password/cookie/SSO/social/Cognito | Steps 4–5, 11; Locked #2/#8 |
| 3 | **OIDC-shaped `sub`** — map stable `sub` (or Spec-equivalent) → Participant id used as Artifact `ownerParticipantId` | Step 2 principal model; Step 8 mapping for #4 consume | Steps 2, 8; Locked #3 |
| 4 | **Secrets / signing material** — keys/signing secrets via env only; never commit; no logging raw tokens/keys; README placeholders | Step 6 full secrets gate; Step 3 never log raw secrets on bootstrap | Steps 3, 6, 12; Spec §4 |
| 5 | **Authorization header transport** — `Authorization` header (or Spec-documented equivalent); forbid query-string/body token passing in tasks/tests | Step 5 transport lock + test forbid | Steps 5, 12; Spec §2 Transport |
| 6 | **Lifecycle (PoC-minimal)** — issue + validate + at least simple invalidate/re-issue or rotate note; no hard-coded master key in source | Step 7 lifecycle; forbid master key | Steps 4, 7, 12; Spec §2 |
| 7 | **Bootstrap/register bounds** — document seed/bootstrap and/or register surface with PoC abuse note; no platform-owner RBAC (O1 out) | Step 3 abuse note + no O1 privilege | Steps 3, 11; Locked #8 |
| 8 | **Authn ≠ authz** — keep #4 owner-scope checks after auth middleware (valid token ≠ owns Artifact) | Step 8 explicitly retains #4 owner checks after middleware; cite Spec §3 | Steps 8, 12; Locked #7; #4 Spec/Plan |
| 9 | **Local/$0 + no IdP spend** — no AWS Cognito/IdP provision tasks; ECS Express Mode sketch only; local host | Step 9 + Cost/critical; Step 11 OUT Cognito | Steps 9–11; Locked #6; Cost/critical |
| 10 | **Handshake close** — Dev Plan QA must not PASS until Security QA confirms these points | See handshake note below. Done-list for Dev Plan QA requires Security QA confirm before PASS | Handshake note; Done-list for Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-11__security__verification__poc-auth-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **QA must not PASS until Security QA confirms.**

---

## 7. Explicit OUT

Mirror Spec §6 / issue #5 Out of scope — SD must not implement:

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
| Inventing #4 Artifact domain or #6 Negotiation inside this plan | Keep separate |

---

## 8. Cost/critical

**#5 must not procure AWS Cognito / managed IdP / paid AWS.** Any Cognito/IdP/AWS spend proposal → escalate **CPM → COO → CEO**. PoC remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, Cognito user-pool provision, or App Runner tasks. ECS Express Mode remains **README sketch only**.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-11__devplan__plan__poc-participant-d6-auth.md`
- [ ] Spec coverage §§1–7 + issue #5 AC + Product BA lock mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] Product alignment Participant D6 / API key/JWT (`PRODUCT-BRIEF` + BA note); issue #5
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6)
- [ ] #4/#5 remain **separate** — fail-closed handoff coordinated; Stories not merged
- [ ] No Cognito/SSO/IdP/AWS spend instructions
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock.

---

## 10. Done-list for SD (after plan QA PASS + Chief confirm)

- [ ] Step 1: Extend O10 solution (Domain/Application/Infrastructure as needed; Api only runnable; keep `GET /health` Auth none)
- [ ] Step 2: Participant principal model with OIDC-shaped `sub` → maps to Artifact `ownerParticipantId`
- [ ] Step 3: Bootstrap/register bounded PoC surface (abuse note; no O1 RBAC); issue credential once; never log raw secrets
- [ ] Step 4: Issue + validate API key and/or JWT only; OUT password/cookie/SSO/Cognito/social
- [ ] Step 5: `Authorization` header transport; forbid query/body tokens in tasks/tests
- [ ] Step 6: Secrets/signing via env only; never commit/log raw tokens/keys; README placeholders
- [ ] Step 7: Lifecycle issue + validate + simple invalidate/rotate note; no master key in source
- [ ] Step 8: Protect Artifact create/get/list-own — fail closed 401/403 once wired; coordinate with #4 (no silent anonymous owner); keep #4 owner-scope after middleware (authn ≠ authz)
- [ ] Step 9: Local/$0; ECS Express Mode sketch; no Cognito/IdP/AWS provision
- [ ] Step 10: Zero MM/DC4 verify passed
- [ ] Step 11: Do not implement Spec §6 OUT
- [ ] Step 12: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
