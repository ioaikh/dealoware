# BA business verification — Story #41 Minimal Strategy create/edit (P3 partial)

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-22 (BA verify executed ~2:42 PM ET)  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/41  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/51 (**MERGED** @ main `43adb2831d1b41633e64e2c93cceb3686037026c`; MergedAt 2026-09-22 2:18:53 PM ET)  
**CI (main @ merge):** https://github.com/ioaikh/dealoware/actions/runs/35766339885 — **SUCCESS**  
**Method:** Business-intent AC check from Story #41 body + merged PR #51 evidence on **main** (`gh` remote reads of StrategyEndpoints / Strategy.cs / StrategyCrudTests / FieldPolicy StrategyBody; no clone) + KB Product QA / Security Doc+ProductQA confirms / Doc weave (not code review). No invented requirements. Stage B named slice only. **Soft lock: Assistant OUT of #41 (Stage C)** — OwnAgent = API policy only; do not invent Assistant scope. Sibling **#40/#42 OUT**. Soft CLOSE SoR CLEAR (#59/#60/#61 @ `68c2196…`). PoC $0. No MotorMarket/DC4.

## Binding AC (Story #41 — do not invent)

1. Owning Participant can **create**, **edit**, **get**, and **list own** a **minimal** Strategy (query-plane owner-scoped)
2. **StrategyBody** via Stage A `IFieldPolicy`: **User** R/W; **OwnAgent** R/W when acting for owner; **Counterparty / Stranger / Unauth Deny**
3. Counterparty views of shared 1:1 Negotiation **never** expose StrategyBody / private Strategy fields
4. Cross-tenant / IDOR fail-closed; unauth → **401**; wrong principal → **403** or **404**; uniform deny; no private-field leakage
5. Automated tests: owner OK; OwnAgent allowed StrategyBody (**API policy row — not Assistant runtime**); CP/stranger/unauth deny
6. Documented as **P3** MVP **minimal** CRUD — free-form → **V1**; A5 sandbox → **V4**; thin Assistant → Stage C / **X1** (OUT)

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | Owner create/edit/get/list-own Strategy; query-plane owner-scoped | **PASS** | Main `StrategyEndpoints` CRUD (POST/GET/PUT/PATCH/DELETE `/strategies`); `StrategyRepository.GetByIdForOwnerAsync` owner WHERE. Facts: `Strategy_Create/Get/List/Update/Patch/Delete_OwnerOK_*`, `Strategy_GetByIdForOwner_QueryPlaneFilter_*`; stranger empty/404. Product QA AC1 MET @ `43adb283…`. |
| 2 | StrategyBody via IFieldPolicy: User R/W; OwnAgent R/W owner; CP/Stranger/Unauth Deny | **PASS** | `FieldClass.StrategyBody` + `FieldPolicy.EvaluateStrategyBody()`; Facts `FieldPolicy_StrategyBody_*`, `FieldPolicy_StrategyBodyMatrix` Theory. Product QA AC2 MET; Security ProductQA pt2 MET. |
| 3 | Counterparty Neg views never expose StrategyBody | **PASS** | Facts: `Negotiation_Response_NeverExposesStrategyBody`, `NegotiationList_Response_NeverExposesStrategyBody`. Product QA AC3 MET; Security pt3 MET. |
| 4 | Cross-tenant IDOR fail-closed; unauth 401; no private leak | **PASS** | Facts: `Strategy_*_StrangerDeny_*`, `Strategy_Get_IDORDeny_*`, `Strategy_*_UnauthDeny_*`, `Strategy_DenyError_NoPrivateFields` (404 wrong principal — Spec 403\|404 OK). Product QA AC4 MET. |
| 5 | Tests: owner OK; OwnAgent policy (not Assistant runtime); CP/stranger/unauth deny | **PASS** | `tests/Dealoware.Api.Tests/StrategyCrudTests.cs` on main (~30–33 Facts/Theories; size 22831 via `gh`); OwnAgent Facts are policy-only (`FieldPolicy_StrategyBody_OwnAgentOK_*`). CI SUCCESS run 35766339885. Product QA AC5 MET. Soft: no live `dotnet` — CI + inventory equivalent. |
| 6 | P3 minimal only — free-form V1; A5 V4 OUT | **PASS** | Opaque/text StrategyBody CRUD only; no free-form evaluation engine / sandbox invent. Story OOS + Doc weave + Product QA AC6 MET; Security pts 7–8 MET. |

## Out of scope held

| OOS item (Story #41 + locks) | Held? | Evidence |
|------------------------------|-------|----------|
| Thin/full **Assistant** runtime (Stage C / **X1**) | **Yes** | **Soft Assistant OUT** — OwnAgent StrategyBody R/W = API policy Facts only; no Assistant/tool runtime in PR #51; Doc weave Constraints; Security pts 5+7 MET |
| Free-form Strategy conditions engine (**V1**) | **Yes** | Minimal opaque/text body only; PR OUT; weave; Product QA AC6 |
| Strategy sandbox (**A5** → **V4**) | **Yes** | Not in PR; weave Constraints; Security pt8 |
| Instant search (**#40**) | **Yes** | #40 OUT — separate BA verify; tracks not merged |
| Contact-on-accept (**#42**) | **Yes** | #42 OUT — separate BA verify |
| Stage C / #26 hard wall / unlocking **#25** / **#18** Spec/SD | **Yes** | HOLD in weave Explicit separations; Security pts 7–8; task soft lock |
| MotorMarket / DC4 | **Yes** | Story + PR OUT; weave; Security pts 7–9 |
| Cognito / SSO / IdP inventing | **Yes** | PoC $0; Security pt9 |
| PoC/MVP spend (**$0**) | **Yes** | No IdP/vault/AWS; Product QA Sec pt9; Doc weave PoC $0 |

## Soft gaps (non-blocking for BA business verify)

- No live `dotnet restore|build|test` on evidence box — accepted; CI SUCCESS run 35766339885 + `StrategyCrudTests` inventory via `gh`.
- Soft Assistant OUT — OwnAgent ACL rows are API policy evidence only (Stage C); **do not invent Assistant scope** from this Story. Non-blocking for P3 minimal CRUD AC.
- Soft CLOSE SoR CLEAR via docs PRs #59/#60/#61 @ `68c2196…` — eng HOLD until CBA confirm.

## Prior gate chain (evidence, not re-scored here)

| Gate | Result | Path / link |
|------|--------|-------------|
| Spec QA | PASS | `verification/2026-09-22__spec__verification__mvp-stage-b-minimal-strategy-crud.md` + Spec Security confirm |
| Dev Plan QA | PASS | `verification/2026-09-22__devplan__verification__mvp-stage-b-minimal-strategy-crud.md` + DevPlan Security confirm |
| SD / Dev Code QA | PASS | `verification/2026-09-22__sd__verification__mvp-stage-b-minimal-strategy-crud.md` |
| SD Security | PASS (1–10 MET) | `verification/2026-09-22__security__verification__mvp-stage-b-strategy-sd-qa-confirm.md` |
| CQ | `cq:no-refactor` | Eng context + Product QA header |
| Product QA | PASS | `qa/2026-09-22__qa__qa-report__mvp-stage-b-minimal-strategy-crud.md` |
| Product QA Security | PASS (1–10 MET) | `verification/2026-09-22__security__verification__mvp-stage-b-strategy-productqa-qa-confirm.md` |
| Doc Security weave | PASS (overall Doc step) | `ops/2026-09-22__docs__ops__mvp-stage-b-strategy-doc-security-weave.md` |
| Doc Security QA | PASS (1–10 MET) | `verification/2026-09-22__security__verification__mvp-stage-b-strategy-doc-qa-confirm.md` |
| Soft CLOSE SoR | CLEAR | PRs #59/#60/#61 @ `68c219699fff9e8afccd491510e587ca087bf024` |

## Recommendation to CBA

**PASS** — deliverable meets Story #41 business AC (owner-scoped minimal Strategy CRUD on query plane; StrategyBody FieldClass ACL User R/W + OwnAgent R/W policy + CP/Stranger/Unauth Deny; Negotiation DTOs never expose StrategyBody; IDOR/unauth fail-closed without private leak; StrategyCrudTests + CI SUCCESS; P3 minimal with free-form→V1 / A5→V4 / Assistant→Stage C OUT), and OOS held (**Assistant soft OUT**; #40/#42 separate; #25/#18 HOLD; MotorMarket; Cognito; PoC $0). Soft gaps non-blocking. Hand to BAQA for verify-QA; eng `done` HOLD until CBA final confirm after BAQA. Do **not** CLOSE issue #41, invent Assistant/Stage C scope, unlock #25, or merge sibling tracks from this step. Stage B named slice only. PoC $0.
