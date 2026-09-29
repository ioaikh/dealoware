# Dev Plan — MVP Stage C Basic UI surface X2 (#69)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner PRIORITY **#69 ONLY** — Basic UI surface X2 (partial)  
**Issue:** https://github.com/ioaikh/dealoware/issues/69  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A · `stage:mvp` · Stage C · X2 partial  
**DOC-FLOW:** `plans/2026-09-28__devplan__plan__mvp-stage-c-basic-ui-first-party-bot-x2.md`  
**Constraints:** **#69 ONLY** — do **not** draft #66/#67/#68. Keep siblings separate (cross-ref only). Spec surface pick locked = **basic UI** (X2 MVP minimum). Exactly-one first-party bot is **OUT of Spec minimum** — do **not** schedule bot channel work; no multi-channel marketplace invent. No privileged back doors; no UI-only filtering as security; when UI invokes #66, path binds #67 hard wall (no client prompt-only soft wall). May surface #68 budget status **minimally** — not platform-owner admin. No MCP; no public OpenAPI (OpenAPI/webhooks → **V1**; MCP → **V5**). Soft observability weave only — no 5th Story / Marketing eng Story. Gate **#26** backlog; Gate **#27** HOLD. PoC **$0**; spend → COO → CEO. No Cognito/MM/DC4; distinct from #7. SD HOLD until Dev Plan QA + Security QA PASS + Chief unlock.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md` | Basic UI X2 contracts; Locked #0–#9; §8 AC + §8.1 tests; OUT; Security §5 |
| Spec QA (Spec gate PASS) | `verification/2026-09-28__spec__verification__mvp-stage-c-basic-ui-first-party-bot-x2.md` | Spec-side bind; Sec10 weave intact; surface = basic UI |
| Spec Security PASS | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan (SoR PR #72) |
| Spec Security points-review | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-points-review.md` | Senior PASS 10/10 |
| Spec Security checklist (upstream) | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-checklist.md` | Spec-step Security 1–10 |
| Dev Plan Security checklist (binding) | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-devplan-checklist.md` | Dev Plan-step handshake points **1–10** — woven in §6 |
| SA Security PASS | `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md` | Architecture-step Security PASS prior (X2 pick A; no wall bypass) |
| CA PASS Stage C delta (X2 pick A) | `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` | One first-party bot and/or basic UI; Spec chose UI-only minimum |
| Option A tip | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | FieldPolicy / dual wall consume |
| BA note #69 | `plans/2026-09-28__ba__note__story-69-basic-ui-first-party-bot.md` | Spec-ready refine; Soft note; UI and/or bot — Spec locks UI |
| Sibling #66 Spec — cross-ref only | `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md` | Thin Assistant — **do not implement** |
| Sibling #67 Spec — cross-ref only | `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md` | Hard wall — bind when Assistant used; **do not implement** |
| Sibling #68 Spec — cross-ref only | `specs/2026-09-28__spec__spec__mvp-stage-c-a8-minimum-meters-budgets.md` | Meters/budgets — optional minimal status surface; **do not implement** |
| Roadmap X2 | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | X2 MVP one bot/UI; OpenAPI → V1; MCP → V5 |
| Issue #69 | https://github.com/ioaikh/dealoware/issues/69 | Binding AC + OUT |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Option A · Stage C named slice; Spec/SD framing HOLD as whole |
| Gate #26 (backlog) | https://github.com/ioaikh/dealoware/issues/26 | Do **not** open now |
| Gate #27 (HOLD) | https://github.com/ioaikh/dealoware/issues/27 | Do **not** unlock |
| DOC-FLOW | `meta/DOC-FLOW.md` | Naming / path |

**Product alignment:** CEO Stage C — **X2** MVP **partial**. Spec surface pick = **basic UI only** (exactly-one first-party bot **OUT** of Spec minimum). OpenAPI/webhooks → **V1**; MCP breadth → **V5**. Marketing research-only. Conflicts → escalate PM → Product → CEO. Cost/critical → COO → CEO. Gate #26 backlog; #27 HOLD.

---

## 2. Restated understanding (short)

Executable plan for **SD only**: deliver a **basic authenticated Participant UI** against the existing O10 API under FieldPolicy — sufficient to exercise MVP Participant flows already on path (auth, artifacts, discovery, negotiation/offers, Strategy, thin Assistant invocation **as siblings land**). Spec locks minimum surface = **basic UI**; exactly-one first-party bot is **OUT of this Spec’s minimum** — do **not** schedule bot channel deliverables or invent a multi-channel marketplace. UI is **not** a security boundary: **no** privileged back doors; **no** UI-only filtering as security; when UI invokes thin Assistant (**#66**), tool/agent path **must** bind **#67** hard wall + scrub (no client prompt-only soft wall). May surface **#68** budget status **minimally** to respect cutoff — **not** platform-owner admin / mature cost UI (V3). Soft observability only — may exercise sibling paths that emit Soft OTel/audit/idempotent hooks when #66/#67/#68 land; **do not** invent a 5th Story or Marketing eng Story. Require #5 validated principal on protected actions; unauth → **401**; wrong principal → **403**/**404**; no private-field leakage via UI payloads. Deliver automated tests per Spec §8.1. **OUT:** first-party bot as required deliverable; public OpenAPI/webhooks; MCP; owner-admin / V3 cost UI; multi-bot marketplace; MM/DC4; Cognito; Gate #26 open; #27 unlock; rewriting Stage A/B ACL; implementing #66/#67/#68 under this Story. PoC/MVP **$0**. No product code in this artifact — SD instructions only. **#69 ONLY** — do not draft or implement #66/#67/#68.

---

## 3. Locked decisions (Spec locks → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 0 | SA X2 pick A + Spec surface = **basic UI only** | SA allows bot **and/or** UI; **this Spec locks minimum = basic UI**. Exactly-one first-party bot is **OUT of Spec minimum** — do **not** schedule bot channel work; no multi-channel invent | Steps 1, 3, 9–10; Explicit OUT |
| 1 | No privileged back doors | UI must **not** bypass API FieldPolicy or #67 hard wall; **no** UI-only filtering as security | Steps 1, 4, 6–7 |
| 2 | Authn fail-closed | Unauth protected actions → **401**; wrong principal → **403**/**404**; no private-field leakage via UI payloads | Steps 2, 5, 7 |
| 3 | Assistant path binds #67 | When UI invokes #66, tool/agent path binds hard wall + scrub; no client prompt-only soft wall | Steps 4, 6–7 |
| 4 | Budget status minimal (#68) | May surface budget status **minimally** — **not** platform-owner admin / mature cost UI (V3) | Steps 3, 7; Explicit OUT |
| 5 | No MCP / no public OpenAPI | Explicit OUT for this Story; OpenAPI/webhooks → **V1**; MCP → **V5** | Steps 9–10; Explicit OUT |
| 6 | Consume tip authz | Exercise existing auth (#5) + FieldPolicy; do **not** rewrite Stage A/B ACL Stories | Steps 4, 8 |
| 7 | Soft observability | Soft weave only — may exercise sibling emit hooks; **no** 5th Story / Marketing eng Story | Steps 8–9; Soft weave note |
| 8 | Scope label | Documented **X2** MVP **partial** | Steps 9–10; Explicit OUT |
| 9 | OUT / Gate / spend | Gate #26 backlog; #27 HOLD; no Cognito/MM/DC4; PoC $0; spend → COO → CEO | Steps 9–10; Cost/critical |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 modular monolith — Spec §7 Host).  
**Prerequisite:** Stage A/B on tip (Field ACL, account list fail-closed, discovery, Strategy, ContactEmail share) + PoC Artifact (#4) + Participant auth (#5) delivered. Consume — do **not** rewrite those Specs or merge Stories.  
**Sibling HOLD:** #66 Thin Assistant runtime; #67 Agent/tool hard wall; #68 A8 minimum meters/budgets — **cross-ref only**; separate plans/Stories. **Do not draft or implement here.** When UI invokes Assistant (as #66 lands), bind #67; optional minimal #68 status only.  
**Hold:** Gate #26; Gate #27; unlocking #18 Spec/SD as a whole; Cognito/MM/DC4; first-party bot channel; public OpenAPI; MCP — do **not** invent.  
**Distinct from #7:** This Story is Stage C X2 basic UI — **not** PoC UI / #7 invent.

### Step 1 — Confirm host layout; basic UI placement; keep health open; no bot channel

- Keep **`Dealoware.Api` as the only runnable** project (API host).
- Keep **`GET /health`** contract unchanged (**Auth: none**) if any host/DI touch is required for wiring.
- Place **basic authenticated Participant UI** against existing O10 API under FieldPolicy (Spec §1 / §7) — sufficient to exercise MVP flows on path.
- **Do not** schedule or deliver a first-party bot channel under this Story (Spec minimum = **basic UI only**; bot OUT of Spec minimum).
- **Do not** invent multi-bot / multi-channel marketplace.
- Continue Minimal APIs; built-in ASP.NET Core DI only for API host.
- Do **not** add Cognito/IdP SDK PackageReferences, vault/KMS spend modules, or MM/DC4 refs.
- Do **not** open gate #26, unlock #27, unlock #18 Spec/SD as a whole, or invent #66/#67/#68 Stories under this plan.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable (API host)
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none) if touched
- [ ] Basic UI surface documented as Spec-locked minimum (X2 partial)
- [ ] No first-party bot channel tasks scheduled/executed under this Story
- [ ] No Cognito/IdP/AWS/MM provision tasks; gate #26 not opened; #27 not unlocked; #18 Spec/SD not unlocked as a whole

### Step 2 — Authn fail-closed on protected UI actions (#5 required)

Per Spec §3 Authn + Locked #2; Security Dev Plan point **2**:

| Item | Plan lock |
|------|-----------|
| Principal | Validated **#5** (or successor) principal required on all protected UI actions |
| Unauthenticated / invalid | Fail closed **`401`**; error/UI payloads omit private fields / contact/PII / FieldClass secrets / auth secrets |
| Wrong principal | Fail closed **`403`** or **`404`**; uniform deny; no private-field leakage |
| Auth header / session | Reuse #5 `Authorization` patterns against API — do **not** invent Cognito/SSO/password/cookie productization |
| Health | `GET /health` remains Auth none |

**Verify tasks (required):**

- [ ] Unauthenticated protected UI action → **401**; no private fields in body/payload
- [ ] Invalid credential → fail closed **401**; no private leak
- [ ] Wrong principal / cross-tenant via UI → **403**/**404**; no private leak
- [ ] Auth’d principal reaches protected UI flows (happy path wired)
- [ ] `GET /health` remains open

### Step 3 — Basic UI exercises MVP Participant flows on path (no new security plane)

Per Spec §1 Surface pick + Locked #0/#4; Security Dev Plan points **4** and **5**:

| Item | Plan lock |
|------|-----------|
| Surface | **Basic UI** — authenticated Participant UI sufficient to exercise MVP flows already on path |
| Flows | Auth, artifacts, discovery, negotiation/offers, Strategy, thin Assistant invocation (**as siblings #66/#67/#68 land**) |
| First-party bot | **OUT of Spec minimum** — do **not** deliver bot channel under this Story |
| Budget status (#68) | Optional **minimal** display to respect cutoff — **not** platform-owner admin / mature cost UI (V3) |
| Marketplace | Do **not** invent multi-bot / multi-channel marketplace |
| Do not invent | New ACL Stories, public OpenAPI package, MCP surface, owner-admin suite |

**Acceptance:**

- [ ] Auth’d Participant can exercise MVP flows on path via basic UI
- [ ] No first-party bot channel delivered under this Story
- [ ] If budget status shown: minimal cutoff respect only — not owner-admin privilege
- [ ] No multi-channel marketplace / OpenAPI package / MCP surface invented

### Step 4 — No UI-only security; FieldPolicy server-side; bind #67 when Assistant invoked

Per Spec §2 Authz + Locked #1/#3/#6; Security Dev Plan points **1**, **3**, and **7**:

| Item | Plan lock |
|------|-----------|
| Security plane | Existing authz + FieldPolicy (**API wall**) — UI is **not** a security boundary |
| Forbidden | Privileged back doors; client-side-only field filtering as sole control |
| Assistant invoke | When UI uses #66, path binds **#67** gateway + scrub — **no** client prompt-only soft wall |
| Consume tip | Exercise existing auth (#5) + FieldPolicy on tip; do **not** rewrite Stage A/B ACL Stories |
| Do not | Implement #66/#67/#68 product under this Story — bind/cross-ref only when siblings land |

**Acceptance:**

- [ ] Critical UI paths enforce server-side FieldPolicy — not UI-only filter
- [ ] No privileged back-door routes or client-only security controls as sole gate
- [ ] When Assistant invoke UI is wired (sibling #66 landed): path binds #67 hard wall + scrub
- [ ] No client prompt-only soft wall as substitute for #67
- [ ] Stage A/B ACL Specs not rewritten; #66/#67/#68 not implemented under this Story

### Step 5 — Uniform deny / wrong-principal fail-closed (no leak via UI payloads)

Per Spec §3 + Locked #2; Security Dev Plan point **2**:

| Item | Plan lock |
|------|-----------|
| Wrong-principal / cross-tenant via UI | Fail-closed with **uniform deny** (**403**/**404**) |
| Leakage | **No** private fields / FieldClass secrets / other Participants’ inventory in UI error/payload bodies |
| Align | #18 / Stage A/B spirit — uniform deny; no existence leak via secret-bearing bodies |
| Do not invent | Multi-tenant admin UI; Cognito productization |

**Verify tasks (required):**

- [ ] Wrong principal / cross-tenant via UI → fail-closed; uniform deny
- [ ] Deny/error payloads contain no private fields / FieldClass secrets / auth secrets / other Participants’ private data
- [ ] Logging does not dump raw tokens / private payloads on deny paths

### Step 6 — Soft observability weave only (no 5th Story)

Per Spec §4 Soft OTel + Locked #7:

| Item | Plan lock |
|------|-----------|
| Soft | UI may exercise sibling paths that emit Soft OTel/audit/idempotent hooks when #66/#67/#68 land |
| Invent forbid | **Do not** invent observability product, Marketing eng Story, or a **5th Story** for OTel/audit/idempotent on this surface |
| Soft weave one-liner | Soft observability = exercise sibling emit hooks only — no new Story |

**Acceptance:**

- [ ] No 5th Story / Marketing eng Story / observability product tasks under this Story
- [ ] Handoff notes document Soft weave only (cite Spec §4)

### Step 7 — Automated tests (binding Spec §8 / §8.1)

Schedule automated tests (or equivalent evidence) covering:

| Case | Expected |
|------|----------|
| Unauthenticated protected UI action | Fail-closed (**401**); no private fields |
| Wrong principal / cross-tenant via UI | Deny (**403**/**404**); no private-field leakage |
| FieldPolicy / authz on chosen surface | Critical paths enforce server-side FieldPolicy — not UI-only filter |
| Assistant invoke (when #66 land) | Path binds #67 hard wall (no client-only soft wall) |
| Budget status (if shown) | Minimal cutoff respect only — not owner-admin privilege |

**Acceptance:**

- [ ] Test suite covers all Spec §8.1 cases
- [ ] Failures assert status + **absence** of secret/private fields in payloads and deny bodies
- [ ] Tests exercise real authz/FieldPolicy paths (not mocked-away client-only filters)

### Step 8 — Consume tip authz; siblings cross-ref only; Soft weave only

| Item | Plan lock |
|------|-----------|
| #5 / FieldPolicy tip | **Consume** existing auth + FieldPolicy — do **not** rewrite Stage A/B ACL Stories |
| #66 | Thin Assistant — **OUT** of this Story’s implement scope; cross-ref; bind when UI invokes |
| #67 | Hard wall — **OUT** of implement scope; **bind** when Assistant used; cross-ref only |
| #68 | Meters/budgets — optional **minimal** status surface only; **OUT** of implement scope for meters product |
| #18 | Parent Spec/SD framing remains **HOLD** as a whole — do **not** unlock under this Story |
| Soft observability | Soft weave only — no 5th Story |

**Acceptance:**

- [ ] Plan/handoff cites tip authz consume + #66/#67/#68 cross-ref only
- [ ] No Stage A/B rewrite / #66/#67/#68 implement / #18-unlock / 5th-Story tasks under this Story

### Step 9 — Explicit OUT (do **not** implement Spec §6)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Exactly-one first-party bot as required deliverable | **OUT of Spec minimum** (UI-only pick) — do **not** schedule bot channel |
| Public OpenAPI / webhooks package | A7 / X2 remainder → **V1** |
| MCP breadth | → **V5** |
| Platform-owner admin suite; mature cost UI | → **V3** |
| Multi-bot / multi-channel marketplace | OUT |
| MotorMarket / DC4; spend invent | OUT |
| Unlocking #26 early; #27 | #26 backlog; #27 HOLD |
| Inventing a 5th Story for OTel/audit/idempotent | Soft on #66/#67/#68 as applicable |
| Marketing eng Story | Marketing research-only |
| Rewriting Stage A/B ACL Stories | Consume tip only |
| Implementing #66 / #67 / #68 under this Story | Cross-ref / bind only |
| Cognito / SSO / IdP / vault / KMS spend | Out |
| Distinct-from-#7 invent | Do not conflate with PoC #7 |

### Step 10 — Secrets / cost / zero MM-DC4; local $0

**Secrets (reuse #5 / FieldPolicy patterns):**

- [ ] No committed secrets / API keys / cloud credentials / real connection strings
- [ ] Connection strings + signing material = env / placeholders only
- [ ] No tokens in query strings or UI-leaked bodies
- [ ] No logging of raw tokens / keys / credentials / secret FieldClass values
- [ ] No Cognito/SSO/IdP/vault SDK PackageReferences added by this Story

**Host / spend:**

- [ ] Remains **local / $0**
- [ ] ECS Express Mode **sketch only** if README already mentions — not deployed by this Story
- [ ] **No** App Runner; **no** AWS account / resource provision that creates spend
- [ ] If spend ever proposed → escalate **COO → CEO**

**Zero MM/DC4:**

- [ ] Grep/search: no MotorMarket / DC4 project references, packages, shared libs, schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 11 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] Basic UI exercises MVP Participant flows on path (auth, artifacts, discovery, negotiation/offers, Strategy, Assistant as siblings land)
- [ ] Spec minimum = **basic UI only**; no first-party bot channel delivered under this Story
- [ ] #5 principal on protected actions; unauth → **401**; wrong principal → **403**/**404**
- [ ] No UI-only security; FieldPolicy server-side; no privileged back doors
- [ ] When Assistant invoked: path binds #67 hard wall (no client prompt-only soft wall)
- [ ] Budget status (if any) minimal only — not owner-admin
- [ ] Automated tests per Spec §8 / §8.1
- [ ] Soft observability weave only — no 5th Story / Marketing eng Story
- [ ] Tip authz consumed not rewritten; #66/#67/#68 not implemented under this Story
- [ ] `GET /health` still Auth none if host touched
- [ ] Nothing from Step 9 / Spec §6 OUT implemented; gate #26 not opened; #27 not unlocked; #18 Spec/SD not unlocked as a whole
- [ ] Secrets hygiene + zero MM/DC4 + local/$0; no Cognito; no MCP; no public OpenAPI
- [ ] #69 ONLY — no sibling Story drafts/impl; Product conflicts escalated if any
- [ ] No product code left undocumented; handoff notes cite Spec + this plan

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec Locked #0 / §1 | Surface = **basic UI**; bot OUT of Spec minimum | Steps 1, 3, 9–10; Explicit OUT |
| Spec §2 + Locked #1 | No privileged back doors; no UI-only security | Steps 1, 4, 6–7 |
| Spec §3 + Locked #2 | Unauth 401; wrong principal 403/404; no private leak | Steps 2, 5, 7 |
| Spec §2 + Locked #3 | When UI invokes #66, bind #67; no client soft wall | Steps 4, 6–7 |
| Spec §3 + Locked #4 | Budget status minimal (#68); not owner-admin | Steps 3, 7; Explicit OUT |
| Spec Locked #5 + §6 OUT | No MCP; no public OpenAPI; → V1 / V5 | Steps 9–10; Explicit OUT |
| Spec Locked #6 | Consume tip authz; don’t rewrite Stage A/B | Steps 4, 8 |
| Spec §4 + Locked #7 | Soft observability weave only; no 5th Story | Steps 6, 8–9 |
| Spec Locked #8/#9 + §6 OUT | X2 partial; Gate #26 backlog; #27 HOLD; $0 | Steps 9–10; Cost/critical |
| Spec §7 Host / cost | Basic UI vs O10; local/$0; ECS sketch | Steps 1, 10 |
| Spec §5 Security Spec 1–10 | Upstream Spec Security bind | §6 Security Dev Plan-step woven 1–10 |
| Spec §8 row 1 / Issue AC | Basic UI (Spec pick); no multi-channel invent | Steps 1, 3, 9 |
| Spec §8 row 2 / Issue AC | Authz/FieldPolicy; bind #67 when Assistant used | Steps 4, 7 |
| Spec §8 row 3 / Issue AC | No MCP; no public OpenAPI | Steps 9–10 |
| Spec §8 row 4 / Issue AC | Unauth fail-closed; no private leak | Steps 2, 5, 7 |
| Spec §8 row 5 / §8.1 / Issue AC | Automated tests critical authz paths | Step 7 |
| Spec §8 row 6 / Issue AC | X2 MVP partial; OpenAPI→V1; MCP→V5 | Steps 9–10 |
| Spec §8 row 7 / Issue AC | Soft #68 budget status minimal | Steps 3, 7 |
| #66 / #67 / #68 Specs cross-ref only | OUT implement — bind/cross-ref | Steps 4, 8–9 |
| #5 / tip FieldPolicy consume | Validated principal + API wall | Steps 2, 4, 8 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-qa-confirm.md` (points 1–10 MET; SoR PR #72)  
**Spec Security points-review:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-points-review.md` (Senior PASS 10/10)  
**Spec Security checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-checklist.md`  
**SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Spec Security binding:** Spec §5 points 1–10  
**Spec SoR CLEAR:** tip `main` @ `f133e90` / PR **#73**  
**Surface pick (binding):** Spec-locked **basic UI** — exactly-one first-party bot **OUT of Spec minimum**

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **No privileged back doors** — Plan requires basic UI must **not** bypass API FieldPolicy or agent/tool hard wall (#67); **no** UI-only filtering as security; verify | Steps 1, 4 forbid UI bypass of FieldPolicy/#67; no UI-only filtering as security; Step 7 FieldPolicy tests; Locked #1 | Steps 1, 4, 7, 11; Locked #1 |
| 2 | **Authn fail-closed on protected actions** — Plan schedules unauthenticated protected actions → fail-closed (**401**); wrong principal → **403**/**404**; no private-field leakage via UI payloads; verify | Step 2 requires #5 principal; unauth → **401**; wrong principal → **403**/**404**; Step 5 deny hygiene; Step 7 tests; Locked #2 | Steps 2, 5, 7, 11; Locked #2 |
| 3 | **Assistant path binds #67** — Plan requires when the UI invokes thin Assistant (#66), tool/agent path binds hard wall + scrub; **rejects** prompt-only soft wall invent on the client; verify | Step 4 binds #67 when UI invokes #66; no client prompt-only soft wall; Step 7 Assistant-bind case; Locked #3 | Steps 4, 7, 11; Locked #3 |
| 4 | **Budget status minimal only (#68)** — Plan may schedule budget status **minimally** to respect cutoff — **not** platform-owner admin / mature cost UI (**V3**); verify no admin-suite invent | Step 3 optional minimal status; not owner-admin / V3; Step 7 budget-status case; Step 9 OUT; Locked #4 | Steps 3, 7, 9; Locked #4; Explicit OUT |
| 5 | **Surface pick = basic UI** — Plan schedules Spec-locked **basic UI** minimum; exactly-one first-party bot **not** required for this minimum; no multi-bot marketplace invent; verify | Spec + plan lock = **basic UI only**; bot **OUT** of Spec minimum; Steps 1, 3 no bot channel; no multi-channel invent; Locked #0 | Steps 1, 3, 9; Explicit OUT; Locked #0 |
| 6 | **OUT locked** — Plan keeps **X2 MVP partial**; public OpenAPI/webhooks → **V1**; MCP breadth → **V5**; Soft OTel/audit/idempotent = weave only on named plans #66/#67/#68 (no 5th Story / no observability product invent) | Step 6 Soft observability weave only — no 5th Story; Step 9 OUT: X2 partial; OpenAPI→V1; MCP→V5; Locked #5/#7/#8 | Steps 6, 9–10; Explicit OUT; Locked #5/#7/#8 |
| 7 | **Consume tip authz** — Plan exercises existing auth (#5) + FieldPolicy paths already on tip; does not rewrite Stage A/B ACL Stories; verify | Steps 4, 8 consume #5 + FieldPolicy; no Stage A/B rewrite; Locked #6 | Steps 4, 8, 11; Locked #6 |
| 8 | **No Gate unlock / no invent** — Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/SSO/MM/DC4/vault invent; no Marketing eng Story; keep #66/#67/#68 separate plans | Steps 9–10 OUT: Gate #26 backlog; #27 HOLD; no Cognito/MM/DC4; no Marketing eng / 5th Story; siblings cross-ref only; Locked #9 | Steps 8–10; Explicit OUT; Locked #9 |
| 9 | **Cost / spend** — PoC **$0**; any spend → COO → CEO | Step 10 local/$0; spend → COO → CEO; Locked #9 | Step 10; Cost/critical; Locked #9 |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then | See handshake note below. Done-list requires Security QA confirm before PASS; SD HOLD | Handshake note; Done-list Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Senior Security → Security QA confirms.** **SD stays HOLD** until Dev Plan QA + Security QA PASS + Chief Dev Planner unlock (+ CPM per ops). First-party bot remains **OUT of Spec minimum** — surface = **basic UI only**.

---

## 7. Explicit OUT

Mirror Spec §6 / issue #69 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Exactly-one first-party bot as required deliverable | **OUT of Spec minimum** (UI-only pick) — **do not schedule bot channel work** |
| Public OpenAPI / webhooks package | A7 / X2 remainder → **V1** |
| MCP breadth | → **V5** |
| Platform-owner admin suite; mature cost UI | → **V3** |
| Multi-bot / multi-channel marketplace | OUT |
| MotorMarket / DC4; spend invent | OUT |
| Unlocking Gate **#26** early; Gate **#27** | #26 backlog; #27 HOLD |
| Inventing a 5th Story for OTel/audit/idempotent | Soft on #66/#67/#68 as applicable |
| Marketing eng Story | Marketing research-only |
| Rewriting Stage A/B ACL Stories | Consume tip only |
| Implementing #66 / #67 / #68 under this Story | Cross-ref / bind only — **#69 ONLY** |
| Cognito / SSO / IdP / vault / KMS spend | Out |
| Unlocking #18 Spec/SD as a whole | HOLD |
| Conflating with PoC #7 | Distinct Story — do not invent #7 work here |

---

## 8. Cost/critical

**#69 must not procure AWS / Cognito / IdP / vault / LLM spend.** Any paid AWS provision, Cognito/SSO/IdP spend, vault/KMS, LLM/API spend, or other spend proposal → escalate **COO → CEO**. Remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, Cognito user-pool, App Runner, MCP host, public OpenAPI publish, or vault tasks. ECS Express Mode remains **README sketch only** if present. Gate **#26** stays backlog — do not open. Gate **#27** HOLD. Do **not** unlock #18 Spec/SD as a whole.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-28__devplan__plan__mvp-stage-c-basic-ui-first-party-bot-x2.md`
- [ ] Spec coverage §§1–7 + §8 AC + §8.1 + issue #69 AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] **#69 ONLY** — #66/#67/#68 **cross-ref only** (not drafted/implemented); surface = **basic UI only**; first-party bot **OUT** of Spec minimum; Gate #26 backlog; #27 HOLD; #18 Spec/SD HOLD as whole
- [ ] Consumes tip authz (#5 + FieldPolicy) — **no rewrite** / Story merge of Stage A/B
- [ ] No privileged back doors; no UI-only security; #67 bind when Assistant used; optional minimal #68 status
- [ ] Automated tests scheduled per Spec §8 / §8.1
- [ ] Soft observability weave only — no 5th Story / Marketing eng Story
- [x] **Security Dev Plan-step points 1–10 all woven** with cites (table §6) — **HOLD PASS** until Senior Security → Security QA confirms
- [ ] No Cognito/SSO/IdP/vault/AWS spend instructions; no MCP; no public OpenAPI
- [ ] No invented Stories / requirements; no multi-channel marketplace
- [ ] No MotorMarket / DC4
- [ ] Explicit OUT respected (Spec §6) — especially bot OUT of Spec minimum
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on Dev Plan-step 1–10 **before** Dev Plan QA PASS to Chief Dev Planner
- [ ] **SD HOLD** until Dev Plan QA + Security QA PASS + Chief unlock
- [ ] **HOLD PASS** until Senior Security → Security QA confirms Dev Plan-step 1–10 (weave complete; handshake open)

**Next:** Dev Plan QA verifies with evidence → ask Senior Security → Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). **Dev Plan QA must NOT PASS until Security QA confirms.** SD starts only after Chief Dev Planner unlock (+ CPM per ops). Parent may run #66/#67/#68 separately — **this plan does not draft them**. Surface = **basic UI only**; first-party bot **OUT of Spec minimum**.

---

## 10. Done-list for SD (after plan QA PASS + Security QA confirm + Chief unlock)

- [ ] Step 1: Confirm host; basic UI placement; no bot channel; keep `GET /health` Auth none if touched
- [ ] Step 2: #5 principal on protected UI actions; unauth → **401**; wrong principal → **403**/**404**; deny hygiene
- [ ] Step 3: Basic UI exercises MVP flows on path; optional minimal #68 budget status only
- [ ] Step 4: No UI-only security; FieldPolicy server-side; bind #67 when Assistant invoked
- [ ] Step 5: Wrong-principal / cross-tenant → uniform deny; no private leak via UI payloads
- [ ] Step 6: Soft observability weave only — no 5th Story
- [ ] Step 7: Automated tests per Spec §8 / §8.1
- [ ] Step 8: Consume tip authz; do **not** implement #66/#67/#68; do **not** unlock #18
- [ ] Step 9: Do not implement Spec §6 OUT; do not open #26; do not unlock #27; do not schedule bot channel
- [ ] Step 10: Secrets hygiene; local/$0; zero MM/DC4; no Cognito; no MCP; no public OpenAPI; ECS sketch only
- [ ] Step 11: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
