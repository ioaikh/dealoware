# Spec — MVP Stage C: Basic UI surface X2 (#69)

**Status:** Senior Spec draft; BA AC locked; Spec QA HOLD until Spec-step Security PASS + Chief clear  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY Stage C Specs #67 → #66 → #68 → #69 → parent #18 (CEO Stage C unlock; CA PASS delta)  
**Issue:** https://github.com/ioaikh/dealoware/issues/69  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A · `stage:mvp` · Stage C · X2 partial  
**DOC-FLOW:** `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md`  
**Constraints:** Separate Spec from #66 / #67 / #68 / parent #18 framing (cross-ref only). **Surface pick locked:** **basic UI** as X2 MVP minimum (sufficient to exercise MVP Participant flows on path). Exactly-one first-party bot **not** required for this minimum — SA pick A is bot **and/or** UI; Spec chooses UI-only minimum to avoid multi-channel invent. No UI-only security; bind #67 when Assistant used. No MCP; no public OpenAPI. PoC **$0**. Gate **#26** backlog until Stage C delivery; Gate **#27** HOLD. OpenAPI/webhooks → **V1**; MCP → **V5**. No Marketing eng Story; no 5th Story for OTel/audit/idempotent. **BA AC locked.** Cite CA PASS X2 pick A. Spec QA must **not** PASS until Spec-step Security checklist delivered + Security QA confirms. **Triad CLOSE / Dev Plan unlock ping HOLD** until Spec Security QA PASS (Chief clears). Docs SoR PR soft — do **not** HOLD Spec content on docs merge.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #69 AC + OUT | https://github.com/ioaikh/dealoware/issues/69 | Binding acceptance |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Stage C framing |
| CA PASS Stage C delta (X2 pick A) | `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` | One first-party bot and/or basic UI; no wall bypass |
| BA note #69 | `plans/2026-09-28__ba__note__story-69-basic-ui-first-party-bot.md` | Spec picks UI and/or exactly one bot; Soft note |
| Spec Security checklist (#69) | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-checklist.md` | Points **1–10** |
| SA Security PASS trio | `verification/2026-09-28__security__verification__mvp-stage-c-sa-checklist.md`, `...-sa-points-review.md`, `...-sa-qa-confirm.md` | Architecture-step Security PASS prior |
| Option A tip | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | FieldPolicy / dual wall consume |
| Sibling #66 / #67 / #68 Specs | `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md`, `...-agent-tool-hardwall-scrubber.md`, `...-a8-minimum-meters-budgets.md` | Cross-ref only — do not merge |
| Roadmap X2 | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | X2 MVP one bot/UI; OpenAPI → V1; MCP → V5 |

**Product alignment:** CEO Stage C — **X2** MVP **partial**. Spec surface pick = **basic UI**. OpenAPI/webhooks → **V1**; MCP breadth → **V5**. Marketing research-only. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | SA X2 pick A + Spec surface choice | SA allows one first-party bot **and/or** basic UI. **This Spec locks minimum surface = basic UI** — sufficient to exercise MVP Participant flows already on path (auth, artifacts, discovery, negotiation/offers, Strategy, thin Assistant as siblings land). Exactly-one first-party bot is **OUT of this Spec’s minimum** (not required; do not invent multi-channel marketplace). |
| 1 | No privileged back doors | UI must **not** bypass API FieldPolicy or agent/tool hard wall (#67); **no** UI-only filtering as security |
| 2 | Authn fail-closed | Unauthenticated protected actions → **401**; wrong principal → **403**/**404**; no private-field leakage via UI payloads |
| 3 | Assistant path binds #67 | When UI invokes thin Assistant (#66), tool/agent path binds hard wall + scrub; no prompt-only soft wall on the client |
| 4 | Budget status minimal (#68) | May surface budget status **minimally** to respect cutoff — **not** platform-owner admin / mature cost UI (V3) |
| 5 | No MCP / no public OpenAPI | Explicit OUT for this Story |
| 6 | Consume tip authz | Exercises existing auth (#5) + FieldPolicy paths on tip; does not rewrite Stage A/B ACL Stories |
| 7 | Soft observability | Does not invent observability product; may exercise paths that emit Soft OTel/audit/idempotent hooks when #66/#67/#68 land |
| 8 | Scope label | Documented **X2** MVP **partial**; OpenAPI/webhooks → **V1**; MCP → **V5** |
| 9 | OUT / Gate / spend | Gate #26 backlog; #27 HOLD; no Cognito/MM/DC4; no Marketing eng Story; no 5th Story; PoC $0 |

---

## 1. Surface pick — basic UI (X2 minimum)

| Item | Spec lock |
|------|-----------|
| Chosen surface | **Basic UI** — authenticated Participant UI sufficient to exercise MVP flows on path |
| Flows covered | Auth, artifacts, discovery, negotiation/offers, Strategy, thin Assistant invocation (as siblings land) |
| First-party bot | **Not** in this Spec’s locked minimum (SA and/or allows either; Spec chose UI-only to keep minimum single-surface) |
| Marketplace | Do **not** invent multi-bot / multi-channel marketplace |

---

## 2. Authz / FieldPolicy — no UI-only security

| Item | Spec lock |
|------|-----------|
| Security plane | Existing authz + FieldPolicy (API wall) — UI is not a security boundary |
| Assistant invoke | When UI uses #66, path binds **#67** gateway + scrub |
| Forbidden | Privileged back doors; client-side-only field filtering as sole control |

---

## 3. Authn / deny hygiene + budget status

| Item | Spec lock |
|------|-----------|
| Unauthenticated protected actions | Fail-closed (**401**) |
| Wrong principal | **403** or **404**; uniform deny |
| Payloads | **No** private-field leakage via UI responses |
| Budget status (#68) | Optional **minimal** display to respect cutoff — not owner admin suite |

---

## 4. Soft OTel / audit / idempotent (no invent)

| Touchpoint | Spec note (not a Story) |
|------------|-------------------------|
| UI exercises sibling paths | Soft: may emit hooks already on #66/#67/#68 paths when those land |
| Invent | **Do not** invent observability product or 5th Story on this surface |

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-spec-checklist.md`  
**Prior SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Rule:** Spec QA must **not** PASS until Spec-step Security checklist delivered + Security QA confirms.

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | No privileged back doors | UI must not bypass API FieldPolicy or #67 hard wall; no UI-only filtering as security | Locked #1; §2 |
| 2 | Authn fail-closed on protected actions | Unauth → 401; wrong principal → 403/404; no private-field leakage via UI payloads | Locked #2; §3 |
| 3 | Assistant/bot path binds #67 | When UI invokes #66, tool/agent path binds hard wall + scrub; no client prompt-only soft wall | Locked #3; §2 |
| 4 | Budget status minimal only (#68) | May surface budget status minimally — not platform-owner admin / mature cost UI (V3) | Locked #4; §3 |
| 5 | Exactly one first-party bot and/or basic UI | Spec picks **basic UI** as minimum; no multi-bot marketplace invent | Locked #0; §1 |
| 6 | OUT locked | X2 MVP partial; OpenAPI/webhooks → V1; MCP → V5; no inventing observability product | Locked #5/#8; §6 OUT |
| 7 | Consume tip authz | Exercises existing auth (#5) + FieldPolicy; does not rewrite Stage A/B ACL Stories | Locked #6; Sources |
| 8 | No Gate unlock / no invent | Gate #26 backlog; #27 HOLD; no Cognito/SSO/MM/DC4; no Marketing eng Story; no 5th Story | Locked #9; §6 OUT |
| 9 | Cost / spend | PoC $0; any spend → COO → CEO | Locked #9; §7 Host |
| 10 | Traceability + handshake | Cites #69 AC + roadmap X2 + SA Security PASS; keep #66/#67/#68 separate; Spec QA PASS only after Security QA | Sources; this §5; Constraints |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Exactly-one first-party bot as required deliverable | Not in this Spec’s locked minimum (UI-only pick) |
| Public OpenAPI / webhooks package | A7 / X2 remainder → **V1** |
| MCP breadth | → **V5** |
| Platform-owner admin suite; mature cost UI | → **V3** |
| Multi-bot / multi-channel marketplace | OUT |
| MotorMarket / DC4; spend invent | OUT |
| Unlocking #26 early; #27 | #26 backlog; #27 HOLD |
| Inventing a 5th Story for OTel/audit/idempotent | Soft on #66/#67/#68 as applicable |
| Marketing eng Story | Marketing research-only |
| Rewriting Stage A/B ACL Stories | Consume tip only |

---

## 7. Host / cost

Basic UI against existing O10 API under FieldPolicy. Local/$0; ECS Express sketch only; App Runner excluded. PoC **$0**. Any spend → escalate **COO → CEO**.

---

## 8. Acceptance mapping (issue #69 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Deliver **basic UI** and/or **exactly one** first-party bot channel sufficient to exercise MVP Participant flows already on path (auth, artifacts, discovery, negotiation/offers, Strategy, thin Assistant as siblings land) — Spec picks UI and/or bot without inventing a multi-channel marketplace | Locked #0; §1 — **pick = basic UI** |
| Surface respects existing authz / FieldPolicy — **no** UI-only filtering as security; agent/bot path binds hard-wall sibling **#67** when Assistant is used | Locked #1/#3; §2 |
| **No** MCP; **no** public OpenAPI package required for this Story | Locked #5; §6 OUT |
| Unauthenticated protected actions fail-closed; no private-field leakage via UI/bot payloads | Locked #2; §3 |
| Automated tests or equivalent evidence for critical authz paths on the chosen surface | §8.1 below; Spec QA + Dev Plan Done-lists |
| Documented as **X2** MVP **partial** — OpenAPI/webhooks → **V1**; MCP breadth → **V5** | Locked #8; §6 OUT |
| Soft: budget status (**#68**) may appear **minimally** if needed to respect cutoff — **not** platform-owner admin / mature cost UI | Locked #4; §3 |

### 8.1 Automated tests detail (binding)

| Case | Expect |
|------|--------|
| Unauthenticated protected UI action | Fail-closed (**401**); no private fields |
| Wrong principal / cross-tenant via UI | Deny (**403**/**404**); no private-field leakage |
| FieldPolicy / authz on chosen surface | Critical paths enforce server-side FieldPolicy — not UI-only filter |
| Assistant invoke (when #66 land) | Path binds #67 hard wall (no client-only soft wall) |
| Budget status (if shown) | Minimal cutoff respect only — not owner-admin privilege |

---

## Done-list

### Spec QA

- [ ] DOC-FLOW path; binding sources only; CA PASS X2 pick A + surface pick **basic UI** locked
- [ ] No privileged back doors; no UI-only security; #67 bind when Assistant used
- [ ] Unauth fail-closed; no private-field leakage; no MCP/OpenAPI
- [ ] Budget status minimal only (#68); Soft observability weave only — no 5th Story
- [ ] **§8 Acceptance mapping complete** — all issue #69 AC bullets present as rows
- [ ] **Automated tests AC** in §8 map + §8.1 detail
- [ ] **Security 1–10** bound (§5) — ask Security QA before PASS
- [ ] Separate from #66/#67/#68/#18 framing; Gate #26 backlog; #27 HOLD; $0
- [ ] BA AC locked cleared
- [ ] **HOLD triad CLOSE / Dev Plan ping** until Spec Security QA PASS (Chief)

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Implement basic UI exercising MVP Participant flows on path
- [ ] Enforce authz/FieldPolicy server-side — no UI-only security
- [ ] Bind #67 when Assistant invoked; optional minimal #68 budget status
- [ ] No MCP / no public OpenAPI package
- [ ] **Automated tests** per §8 / §8.1 for critical authz paths
- [ ] Do not implement §6 OUT

**Next:** Spec QA verify → ask Security QA → confirm to Chief Spec only. Triad CLOSE after Spec Security QA PASS (Chief). Surface pick = basic UI. Gate #26 backlog; PoC $0.
