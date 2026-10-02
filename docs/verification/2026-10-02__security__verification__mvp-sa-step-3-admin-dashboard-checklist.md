# Security checklist — Architecture (SA) · Step 3 Platform-owner admin dashboard

**Status:** Chief Security itemized points for Architecture step (handshake per `ops/ORG-OPS.md`). Issue **before** Architecture QA PASS. Architecture is **Security-critical**.
**Date:** 2026-10-02
**Author:** Dealoware Chief Security
**Issue:** https://github.com/ioaikh/dealoware/issues/148 · Spec Step 3 — Platform-owner admin dashboard (users, offers, negotiations, related ops)
**Moment:** Product Step 3 — `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 3
**CEO unlock:** 2026-10-02 ~9:05am ET via CPM · status:in-dev · Steps 3→5 in sequence; **NOW Step 3 only**
**Deliverable (Architecture design):** SA written architecture grounding for Spec #148 — platform-owner admin surface vs Participant (#69), min roles for O1 users (not SSO), Artifact owner views (O2), negotiations/offers admin (O3), narrow related ops on MVP fabric — fail-closed dual-wall; inherit Step 2 AWS Soft HOLD SoR design constraints only Soft HOLD provision. Expected path under `architecture/` (DOC-FLOW SA architecture; Soft HOLD invent Stories).
**Sibling Spec Security checklist (ISSUED / in flight):** `verification/2026-10-02__security__verification__mvp-spec-step-3-admin-dashboard-checklist.md`
**Grounding (binding cite):**
- `product/2026-10-02__product__note__step3-admin-dashboard-spec-scope.md` (Product scope lock IN/OUT)
- `product/2026-10-02__product__strategy__extended-next-steps-1y.md` Step 3 · Roadmap O1–O3 intent
- Option A tip `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3b (dual wall)
- Step 2 #142 CLOSED Soft HOLD SoR pack tip `c28361f` (AWS design constraints Soft HOLD provision — design only)
- Participant UI #69 CLOSED Soft HOLD SoR (platform-owner admin ≠ Participant)
- Gate **#27** CLOSED Soft HOLD SoR (do **not** reopen)
**Hold:** Soft HOLD Architecture QA PASS until Security QA confirms 1–10. Soft HOLD score until this checklist Soft HOLD SoR CLEAR. Handshake Soft HOLD SoR = **qa-confirm only** after Senior+QA+Chief PASS (Gate **#25/#26/#27** pattern; **do not invent points-review Soft HOLD SoR**). Soft HOLD Spec content Soft HOLD until **CA design grounding PASS** (Chief Spec lock). Soft HOLD **AWS account / resource provision / spend**. Soft HOLD **invent product Stories / invent AC beyond Product scope**. Soft HOLD invent Spec/AC for Steps **4–5** until Step 3 CLOSED (+ CEO confirm invent before Step 5 Spec). Soft HOLD multi-provider Spec/doc rewrite until Step 3 CLOSED (then Step 4 track only). Soft HOLD Marketing publish. Soft **#41** CLOSED via **#66+#67** (do not re-open). App Runner excluded. No MotorMarket/Cognito invent as delivered / DC4. Gate **#27** CLOSED stay closed. PoC **$0**.
**DOC-FLOW:** `verification/2026-10-02__security__verification__mvp-sa-step-3-admin-dashboard-checklist.md`
**Tip context:** `c28361f`

## Scope note
Architecture Step 3 grounding shapes the **platform-owner admin** surface (O1–O3 + narrow related ops) on existing MVP fabric under Option A dual-wall fail-closed, distinct from Participant UI (#69). Real points — not N/A. This design does **not** authorize AWS provision/spend, O9 SSO/IdP, A8 mature metering UI, A9 mature PII vault, invent Stories, invent Steps 4–5 Spec/AC, Soft HOLD multi-provider rewrite, Marketing publish, reopen Gate #27, App Runner greenfield, MotorMarket/DC4, settlement/escrow/checkout, or Participant conflation with platform-owner admin.

## Itemized security points (Architecture design must answer)

1. **Role boundary: platform-owner admin ≠ Participant UI (#69)** — Architecture states platform-owner admin is a **distinct** surface/capability set from Participant UI (#69). Reject designs that conflate Participant capabilities with platform-owner admin or that grant Participant roles platform-owner admin powers. Cite #69 CLOSED Soft HOLD SoR + Product scope lock.

2. **O1 users — min platform-owner roles; Soft HOLD O9 SSO/IdP** — Architecture covers registered users list/view/manage with **minimum** permissions/roles for platform-owner admin of users. Soft HOLD invent full SSO / IdP / Cognito / O9 as Step 3 delivered. Soft HOLD invent mature identity beyond min admin role model. Cite Product scope lock OUTs.

3. **Fail-closed dual-wall inherits Option A §3b** — Architecture's admin views/management of users, Artifacts, negotiations/offers, and related ops **binds** CEO-accepted Option A **§3b**: Platform API/DB FieldPolicy wall **and** agent/tool hard wall (same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses). Reject prompt-only controls, parallel ACL tables that drift from FieldPolicy, tenant shortcuts that weaken fail-closed list/discovery scrub, or admin bypass that dumps denied FieldClasses. Cite Option A tip + Gate #27 CLOSED / Stage C #67.

4. **O2 Artifacts (owner) Soft HOLD invent Field-capture Stories** — Architecture may define platform-owner views/management of Artifacts under FieldPolicy fail-closed. Soft HOLD invent product Stories / Field-capture store migration as in-scope delivery in this Architecture. Soft HOLD invent AC beyond Product scope. Cite Product scope + Soft HOLD invent Stories.

5. **O3 negotiations / offers Soft HOLD invent ShareOutbound regressions** — Architecture's platform-owner views/management of negotiations/offers must preserve identity-until-accept: no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side. Soft HOLD designs that leak LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into admin UI, logs, or exports without explicit Accept-gated / FieldPolicy allow. Cite Option A + Stage A/B Security PASS posture.

6. **Related platform ops narrow + fail-closed** — Architecture includes only ops views that **directly** support O1–O3 on existing MVP fabric entities — fail-closed. Soft HOLD invent broad ops/admin surface, settlement/escrow/checkout, O4/O6 and other O* not in Product IN. Soft HOLD invent Stories for ops scope expansion.

7. **Soft HOLD AWS provision; inherit Step 2 design constraints only** — Architecture may cite Step 2 AWS Soft HOLD SoR @ tip `c28361f` as **design constraints only**. Soft HOLD AWS account create / resource provision / spend / paid plugins / non-local deploy. Any later provision / spend → escalate **CA → CPM → COO → CEO**. PoC **$0** for provision. Cite #142 CLOSED + DevOps ECS Express lock Soft HOLD spend.

8. **Soft HOLD mature A8 metering UI / A9 PII vault / paid observability** — Architecture Soft HOLD invent A8 mature metering UI, A9 mature PII vault/KMS, Cognito/SSO as delivered, and Soft HOLD invent paid observability beyond free tiers without COO→CEO. Soft HOLD designs that dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces. Soft HOLD invent OTel/audit as a new Story here.

9. **OUT / Soft HOLD locked pack** — Architecture OUT list must include: Soft HOLD invent Stories; Soft HOLD invent AC beyond Product scope; Soft HOLD AWS provision/spend; Soft HOLD Marketing publish; Soft HOLD multi-provider Spec/doc rewrite until Step 3 CLOSED; Soft HOLD invent Spec/AC Steps **4–5** until Step 3 CLOSED (+ CEO confirm invent before Step 5); O9 SSO/IdP; A8 mature metering UI; A9 mature PII vault; settlement/escrow/checkout; Gate **#27** CLOSED stay closed; Soft **#41** CLOSED via #66+#67; App Runner; MotorMarket/DC4; Cognito as delivered; MCP marketplace; PoC **$0**. Cost/critical → COO → CEO.

10. **Traceability + handshake Soft HOLD SoR pattern** — Architecture cites Product Step 3 scope lock + strategy Step 3 + Option A §3b + Step 2 #142 tip `c28361f` + #69 Participant boundary + Gate #27 CLOSED + Soft #41 CLOSED via #66+#67. Architecture QA must **not** PASS until Security QA confirms these points via `…mvp-sa-step-3-admin-dashboard-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** (Gate #25/#26/#27 pattern) — **do not invent points-review Soft HOLD SoR**. Soft HOLD CA formal content PASS Soft HOLD until checklist Soft HOLD SoR CLEAR + Senior answers § Security + Security QA → Chief PASS (then Spec Soft HOLD until CA design grounding PASS per Chief Spec). Sibling Spec checklist remains separate Soft HOLD SoR / Soft HOLD Spec QA track.

## Handshake next
1. Soft HOLD SoR twin (this checklist) via Docs **now**; Soft HOLD score until Soft HOLD SoR CLEAR.
2. Senior Architect answers points in the Step 3 admin architecture under `architecture/` § Security (cite sections / evidence) — Soft HOLD invent Stories; Soft HOLD provision.
3. Senior Security may file points-review (content OK) — **handshake Soft HOLD SoR = qa-confirm only** (do **not** Soft HOLD SoR points-review).
4. Security QA PASS to Chief Security (or further instructions) → `…mvp-sa-step-3-admin-dashboard-qa-confirm.md`.
5. Chief Security PASS/HOLD to CPM + Chief Architect (+ Spec Soft HOLD until CA grounding PASS).
6. Later handshake Soft HOLD SoR **qa-confirm only** after Senior+QA+Chief PASS.
7. Sibling Spec checklist remains separate Soft HOLD SoR / Soft HOLD Spec QA track.

## Cost/critical
No AWS / IdP / LLM / paid observability spend without COO → CEO. Soft HOLD provision stands. Soft HOLD multi-provider stands. Soft HOLD invent Stories stands. Soft HOLD Marketing publish stands. App Runner excluded. Gate #27 CLOSED. Soft #41 CLOSED. PoC **$0**.
