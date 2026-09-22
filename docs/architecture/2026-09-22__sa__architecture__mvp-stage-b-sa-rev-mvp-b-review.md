# Architecture review — Gate #25 SA-REV-MVP-B (Stage B #40+#41+#42 delivery)

**Status:** Senior Architect post-delivery review for Architecture QA.  
**Date:** 2026-09-22  
**Author:** Dealoware Senior Architect  
**Moment ID:** SA-REV-MVP-B · Gate https://github.com/ioaikh/dealoware/issues/25  
**Stories:** #40 Instant discovery · #41 Minimal Strategy CRUD · #42 Contact-on-accept  
**Baselines:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` (CEO ACCEPTED Option A — Stage B row) · `architecture/2026-09-20__sa__architecture__mvp-proposed-arch-review-moments.md` · Gate #24 PASS `architecture/2026-09-21__sa__architecture__mvp-stage-a-sa-rev-mvp-a-review.md` · Specs `specs/2026-09-22__spec__spec__mvp-stage-b-{instant-search-discovery,minimal-strategy-crud,contact-on-accept}.md`  
**Actual:** `ioaikh/dealoware` `main` — **Impl MERGED:** PR **#53** (#40) · PR **#51** (#41) · PR **#52** (#42); **Doc SoR** through PR **#61** @ `68c2196` (Soft CLOSE SoR PRs **#59+#60+#61**); BA SoR follow-on PR **#62** = **non-blocking** for this gate; Gate issue **in-dev** https://github.com/ioaikh/dealoware/issues/25  
**HOLD:** Stage C Spec/eng / #18 product unlock until **CA PASS** + Architecture QA; gates **#26/#27** backlog; agent/tool hard-wall **impl** Stage C; soft **Assistant OUT** on #41 (OwnAgent = API policy only — do not claim Stage C agent wall delivered); PoC **$0**; App Runner excluded; ECS Express sketch; no MotorMarket  
**DOC-FLOW:** `architecture/2026-09-22__sa__architecture__mvp-stage-b-sa-rev-mvp-b-review.md`  
**Security checklist:** `verification/2026-09-22__security__verification__mvp-stage-b-sa-rev-mvp-b-checklist.md`

## Sources

| Source | Role |
|--------|------|
| CPM — Stage B delivery CLOSED #40+#41+#42 (CBA BA PASS); soft #41 Assistant OUT held | Gate trigger |
| Option A Stage B row | Binding SA intent (StrategyBody ACL; ContactEmail OwnAgent Read; ShareOutbound after Accept; discovery no secret leak) |
| Gate #24 SA-REV-MVP-A CA PASS | Stage A Field ACL + list fail-closed consumed |
| Specs #40/#41/#42 | Spec locks for delivery review |
| `main` SearchEndpoints · StrategyEndpoints · FieldAcl · AcceptGrant · tests | Delivery evidence |
| PRs #53 / #51 / #52 | Impl merge evidence (per Security checklist / CPM) |
| Doc SoR PR **#61** @ `68c2196` (Soft CLOSE SoR **#59+#60+#61**) | Doc / Soft CLOSE SoR evidence (CPM + Senior PM) |
| BA SoR PR **#62** | Follow-on BA weave — **non-blocking** for Gate #25 |
| Spec→Doc Security PASS | `verification/2026-09-22__security__verification__mvp-stage-b-{discovery,strategy,contact-on-accept}-doc-qa-confirm.md` |
| Soft #41 Assistant OUT | Stage C scope — OwnAgent API policy only; agent wall **not** delivered |

**Note:** GitHub #25 issue body still quotes moments trigger text (instant search + Strategy CRUD + contact-on-accept); issue remains **in-dev** for this gate. **Effective review scope** per CPM + Security checklist = Stage B named slice **#40+#41+#42**. Soft #41 Assistant OUT = Stage C scope (do not claim delivered). Soft doc lag / BA #62 non-blocking called in §2.

---

## 1. Intent check

| Area | Verdict | Baseline cite | Evidence on `main` |
|------|---------|---------------|-------------------|
| **#40 Instant discovery — separate surface** | **PASS** | Option A Stage B discovery row; Spec #40 locked decisions 0–4 | `SearchEndpoints` `/search/artifacts` — separate from owner `GET /artifacts`; `DiscoverableArtifactResponse` omits OwnerParticipantId / LoginEmail / ContactEmail / StrategyBody; `DiscoverySearchTests` (`Search_IsSeparateFromOwnerInventory`, `Search_DoesNotDumpPrivateInventory`) |
| **#40 Auth fail-closed + no secret leak** | **PASS** | Spec #40 authn + omit secrets | Unauth/invalid → **401** (`Search_Unauth_Returns401`); results omit secrets (`Search_Results_NoLoginEmail`, `NoContactEmail`, `NoStrategyBody`, `NoOwnerParticipantId`); error bodies scrubbed (`Search_Unauth_NoPrivateFieldsInError`) |
| **#41 Owner-scoped Strategy CRUD** | **PASS** | Spec #41 query plane; Option A StrategyBody | `StrategyEndpoints` `/strategies` owner-scoped via `GetByOwnerAsync` / owner checks; stranger get/update/delete → **404** no private fields (`Strategy_Get_StrangerDeny_Returns404NoPrivateFields`); unauth → **401** |
| **#41 StrategyBody FieldClass ACL** | **PASS** | Option A StrategyBody row; Spec #41 matrix | `FieldClass.StrategyBody` registered; `FieldPolicy.EvaluateStrategyBody` User R/W + OwnAgent R/W; Counterparty/Stranger/Unauth Deny; `StrategyMapper` projects StrategyBody only when Evaluate Read allows; unit tests `FieldPolicy_StrategyBody_*` |
| **#41 Assistant runtime OUT (soft)** | **PASS** | Soft HOLD through delivery; Spec #41 OwnAgent = API policy only | Strategy endpoints header documents **OUT: Assistant runtime (Stage C/X1)**; OwnAgent rows exercised as `FieldPrincipal.Agent` API policy only — **no** agent gateway / tool scrubber package delivered |
| **#42 Seal until Accept (extend #7)** | **PASS** | Spec #42 extend #7; Option A ShareOutbound | Pre-Accept Neg/Offer omit contact PII (`PreAccept_GetNegotiation_NoContactEmail_NoLoginEmail`); `IdentitySealed:true` held; IdentitySeal regression covered by `IdentitySealTests` + Stage B suite — **#7 stub extended, not rewritten** |
| **#42 ShareOutbound(ContactEmail) after Accept** | **PASS** | Option A ContactEmail ShareOutbound; Spec #42 HasAcceptGrant | `AcceptGrant.CreatePair` persisted on Accept; `FieldPolicy.EvaluateShareOutbound` allows ContactEmail only when `HasAcceptGrant` + Counterparty; `AcceptOfferResponse.CounterpartyContactEmail` / `IncludesContactEmail`; tests `Accept_CounterpartyReceivesContactEmail_LoginEmailNever`, `PostAccept_ShareOutbound_Allow_FieldPolicy`, `PreAccept_ShareOutbound_Deny_FieldPolicy` |
| **#42 LoginEmail never shared** | **PASS** | Option A LoginEmail User-only | Accept response never includes LoginEmail; FieldPolicy ShareOutbound LoginEmail always false; tests assert login secrets absent post-Accept |
| **Hosting / $0 / no MM** | **PASS** | Option A / ORG-OPS | Unchanged local/$0; no App Runner greenfield; no MM/DC4 in Stage B delivery paths |

**Overall:** Stage B named-slice architecture intent **met** on `main`. Soft Assistant OUT held correctly.

---

## 2. Deviations

| Deviation | Original | Actual | Why | Root cause | Adjustments |
|-----------|----------|--------|-----|------------|-------------|
| Gate #25 issue body trigger text | Moments generic Stage B wording | Effective scope = #40+#41+#42 per CPM + Security checklist | Issue body not refreshed at delivery-close | Template lag | **Update plans:** CPM refresh #25 body to cite #40+#41+#42 + this review (chore; not CEO) |
| Soft Assistant OUT on #41 | Option A dual wall end-state includes agent wall | Stage B delivers OwnAgent **API policy** only; agent/tool hard wall still Stage C | Soft HOLD through delivery (CA brief) | Staged dual-wall delivery | **Keep HOLD** — do **not** claim Stage C agent wall delivered; map to Stage C / #26 |
| FieldAclTests Stage A ShareOutbound comment | Older Stage A test comment: ContactEmail ShareOutbound Deny “(Stage B HOLD)” | Stage B delivered ShareOutbound-after-Accept; Stage A suite comment may lag | Test suite comment not rewritten | Soft doc lag in Stage A tests | **Update plans:** SD/CQ optional comment refresh — non-blocking; Stage B tests are authoritative for ShareOutbound |
| BA SoR PR #62 after Soft CLOSE | Soft CLOSE SoR complete at #59+#60+#61 | PR #62 BA weave landed after Soft CLOSE | Follow-on BA SoR | Pipeline timing | **Non-blocking** for Gate #25 (CPM/PM) — cite only |

No agent hard-wall / Stage C / #18 whole unlock claimed (correct HOLD). Soft #41 Assistant OUT = **Stage C scope**.

---

## 3. Fit to future architecture

| Action | Item |
|--------|------|
| **Keep** | Domain `FieldAcl` + `IFieldPolicy` as single policy source (Stage A + Stage B rows) |
| **Keep** | Discovery as **separate** surface from owner inventory (#32) |
| **Keep** | Owner-scoped Strategy CRUD + StrategyBody projection via Evaluate |
| **Keep** | AcceptGrant + ShareOutbound(ContactEmail) only with HasAcceptGrant |
| **Keep** | LoginEmail User-only never ShareOutbound |
| **Add** (Stage C HOLD) | Agent/tool gateway scrubbers binding same Evaluate; no LoginEmail in agent context; share-tool plane |
| **Add** (Stage C / product HOLD) | Thin Assistant runtime consuming OwnAgent policy (not invent here) |
| **Change** | #25 GitHub description to match Stage B #40+#41+#42 delivery (ops) |
| **Remove / do not claim** | Cognito/SSO, vault spend, MM/DC4, saved-search/A1/A5, Stage C / #18 whole as delivered; App Runner greenfield |

---

## 4. Disposition

**Update plans (no CEO escalations):**

| File / item | Action |
|-------------|--------|
| This review | Authoritative SA-REV-MVP-B result |
| GitHub #25 body | CPM refresh scope to #40+#41+#42 + link this review |
| Option A / Gate #24 / Stage B Specs | Remain baselines; no rewrite required for PASS |
| Stage C Spec/eng / #18 product unlock | **HOLD until CA PASS** on this review (+ Architecture QA / Security handshake) |
| Soft Assistant OUT | Documented; Stage C agent wall remains HOLD |

**CEO questions:** None.

---

## 5. Security answers (Architecture always-critical)

Checklist: `verification/2026-09-22__security__verification__mvp-stage-b-sa-rev-mvp-b-checklist.md`  
Architecture QA must **not** PASS until Security QA confirms these points.

| # | Point | Answer | Cite |
|---|-------|--------|------|
| 1 | Discovery payload scrub / no secret leak (#40) | **MET.** Instant search is a separate authenticated surface; results use `DiscoverableArtifactResponse` (D1–D5 only); StrategyBody / LoginEmail / ContactEmail / OwnerParticipantId / private inventory / auth secrets omitted; unauth **401**; uniform deny scrubbed. Discovery ≠ #32 owner inventory. | `SearchEndpoints.cs`; `DiscoverableArtifactResponse.cs`; `DiscoverySearchTests` (Unauth 401; NoLoginEmail/NoContactEmail/NoStrategyBody/NoOwnerParticipantId; SeparateFromOwnerInventory); Spec #40; Option A Stage B discovery row; PR **#53** |
| 2 | StrategyBody ACL owner + OwnAgent only (#41) | **MET.** StrategyBody FieldClass User R/W + OwnAgent R/W; Counterparty/Stranger/Unauth Deny; ShareOutbound Deny for StrategyBody; owner-scoped CRUD query plane; stranger/IDOR **404** no StrategyBody leak; Negotiation DTOs do not expose StrategyBody (Strategy surface only + FieldPolicy). | `FieldClass.StrategyBody`; `FieldPolicy.EvaluateStrategyBody`; `StrategyMapper`; `StrategyEndpoints`; `StrategyCrudTests` (owner CRUD; StrangerDeny 404; FieldPolicy_StrategyBody_*); Spec #41; Option A StrategyBody row; PR **#51** |
| 3 | OwnAgent = API policy only (soft Assistant OUT) | **MET.** OwnAgent StrategyBody R/W exercised only as `FieldPrincipal.Agent` against `IFieldPolicy` / API projection. **No** Assistant runtime, tool gateway, or agent scrubber delivered. Stage C agent/tool hard-wall **not** claimed. Soft Assistant OUT held through delivery. | StrategyEndpoints OUT comment; FieldPolicy OwnAgent rows; CA soft HOLD; Spec #41 locked decision 5; checklist Hold note |
| 4 | ContactEmail ShareOutbound-after-Accept only (#42) | **MET.** ShareOutbound(ContactEmail) only when `resourceContext.HasAcceptGrant` and principal Counterparty; Deny before Accept; ContactEmail rows User R/W / OwnAgent Read / Counterparty Deny until grant / Stranger Unauth Deny. Accept persists `AcceptGrant` pair. | `FieldPolicy.EvaluateShareOutbound`; `AcceptGrant.CreatePair`; `AcceptOfferResponse`; `StageBContactOnAcceptTests` (PreAccept Deny; PostAccept Allow; Accept_CounterpartyReceivesContactEmail); Spec #42; Option A ShareOutbound row; PR **#52** |
| 5 | LoginEmail never shared | **MET.** LoginEmail remains User-only (OwnAgent Deny); ShareOutbound(LoginEmail) always false; Accept response never includes LoginEmail; discovery and Strategy paths omit LoginEmail. Distinct from ContactEmail. | `FieldPolicy.EvaluateLoginEmail` + EvaluateShareOutbound LoginEmail false; `AcceptOfferResponse` docs; tests assert login secrets absent; Spec #42 locked decision 6 |
| 6 | Seal until Accept + #7 extend-only (#42) | **MET.** Pre-Accept Neg/Offer omit counterparty contact PII with IdentitySealed true; Accept enables ContactEmail share via grant. PoC **#7** identity-seal **extended** under ACL — not rewritten; no regression of stub-as-delivered intent. | `StageBContactOnAcceptTests` PreAccept_*; `IdentitySealTests`; Spec #42 extend #7; Option A distinguish #7 stub |
| 7 | Fail-closed IDOR / unauth (all three) | **MET.** Unauth **401** on search / strategies / accept; wrong principal / stranger **404** (Strategy IDOR; PostAccept stranger Neg); ContactEmail still Deny for strangers post-Accept; uniform deny bodies without private leakage across #40/#41/#42. | DiscoverySearchTests; StrategyCrudTests; StageBContactOnAcceptTests (PostAccept_StrangerCannotAccessNegotiation; Unauth_*); Specs #40–#42 |
| 8 | Consume Stage A / Option A; do not merge or invent | **MET.** Stage B consumes #31 Field ACL registry + #32 list fail-closed + Gate #24 PASS; adds StrategyBody + ShareOutbound-after-Accept + discovery surface as **complementary** named slices — not one merged surface; Stage A posture not rewritten. | Gate #24 review PASS; FieldClass registry extended with StrategyBody; Specs cross-ref non-merge; Option A Stage B Story split |
| 9 | No inventing / no spend / no MM | **MET.** Review does **not** claim agent/tool hard-wall, Assistant runtime, Cognito/SSO/IdP, mature vault, Stage C share-tool, saved-search, A1/A5, or MM/DC4 as Stage B–delivered. PoC **$0**; App Runner excluded; ECS Express sketch only. | Header HOLD; §3 Fit; Specs OUT tables; ORG-OPS host shape |
| 10 | Traceability + handshake | **MET.** Cites Option A Stage B row + Gate #24 PASS + Specs #40–#42 + `main` impl PRs **#53/#51/#52** + Doc SoR PR **#61** @ `68c2196` (Soft CLOSE SoR **#59+#60+#61**; BA PR **#62** non-blocking) + Spec→Doc Security PASS paths per area. Residual gaps → Stage C HOLD (agent wall / soft Assistant OUT) — **no guessing**. Architecture QA awaits Security QA confirm before PASS. | This §5; checklist DOC-FLOW; Spec→Doc confirms under `verification/2026-09-22__security__verification__mvp-stage-b-*-doc-qa-confirm.md`; CPM/PM SoR enrichment |

---

## Done-list (Architecture QA)

- [ ] §§1–4 complete vs CA brief (intent / deviations / fit / disposition)
- [ ] §5 answers checklist points 1–10 + **Security QA confirm before PASS**
- [ ] Confirm to **Chief Architect only**; do **not** unlock Stage C Spec/eng, #18 product, or invent Stories
- [ ] Soft Assistant OUT / Stage C agent wall **not** claimed delivered
