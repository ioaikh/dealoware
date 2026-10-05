# Architecture SoR — Core admin dashboard

**Status:** Senior Architect proposal Soft HOLD SoR for Core admin. Design Spec only. HOLD build, deploy, Stories, code, CDK, and spend until harden live QA PASS after app image bake, then a separate unlock. PoC **$0**. No provision.  
**Date:** 2026-10-05  
**Author:** Dealoware Senior Architect  
**Brief from:** Chief Architect → Senior Architect → Architecture QA (Architecture always Security-critical)  
**Moment ID:** **SA-REV-CORE-ADMIN** (this deliverable)  
**DOC-FLOW:** `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md`  
**Host:** `admin.core.dealoware.com` only  
**App target:** .NET 10 (retarget in flight). This SoR does not implement the retarget. Historical PoC notes that say `net8.0` stay as delivery evidence.  
**SA Security checklist:** `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` — **ISSUED**. § Security answers points 1–10 **MET**. HOLD Architecture QA PASS until Security Soft HOLD SoR CLEAR + Security QA confirms 1–10 via qa-confirm Soft HOLD SoR only (handshake SoR = **qa-confirm only**; do not invent a points-review Soft HOLD SoR). Spec Security 10/10 also bound in design.

| Field | Value |
|-------|-------|
| Date | 2026-10-05 |
| Story / epic / phase | Core admin dashboard (Design Spec track; no Spec issue invent) |
| Binding Product | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` (patched 2026-10-05 13:06) |
| Binding Spec | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` |
| Spec QA | `verification/2026-10-05__spec__verification__core-admin-dashboard.md` — PASS |
| Spec Security qa-confirm | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` — PASS 10/10 |
| SA Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` — **ISSUED**; answers 1–10 MET |
| Expected SA Security QA Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (qa-confirm only) |
| Host shape | Same Core modular monolith; separate admin route surface at `admin.core.dealoware.com`. App Runner OUT. Provision HOLD. |
| Cost | PoC **$0** · HOLD provision · HOLD spend · HOLD build |
| Principal name | **CoreOwner** (Product meaning: owner of Core — not platform human-user admin) |
| Security | Architecture always Security-critical · SA checklist ISSUED · answers 1–10 MET · HOLD Arch QA until Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only |

## Sources

| Source | Role |
|--------|------|
| Product Core admin scope | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` — binding IN/OUT, AC1–AC9, four must-covers |
| Spec | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` — Design Spec; §§8–11 left mechanism picks to SA |
| Spec QA PASS | `verification/2026-10-05__spec__verification__core-admin-dashboard.md` |
| Spec Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` — Spec pts 1–10 |
| Spec Security qa-confirm | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` — PASS 10/10 |
| Prior Step 3 SA (reconcile) | `architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` — keep Option A shape; drop O1; replace platform-admin host |
| FieldPolicy dual wall | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3a/§3b — **cite; do not rewrite**; no parallel admin ACL |
| H2/H3 DB posture (optional) | `architecture/2026-10-05__sa__architecture__h2-h3-db-tls-login-split-check.md` — VerifyFull + migrations/app login split; no CDK / account invent |
| SA options brief template | `architecture/templates/sa-architecture-options-brief-template.md` |
| CEO host standing rule | `admin.core.dealoware.com` = Core admin; platform hosts are not Core; Core may run outside AWS; no CDK / bot-platform / AWS account info in this SoR |

**Locks (cite; do not change):** Host `admin.core.dealoware.com` only · CoreOwner ≠ Participant UI · FieldPolicy dual wall; no parallel admin ACL · Drop O1 human users · HOLD build/deploy/Stories/code/CDK/spend until harden live QA PASS after app image bake · SSO/IdP as delivered OUT · inbound bot connector after this · settlement OUT · MotorMarket/DC4 OUT · App Runner OUT · PoC **$0**.

---

## 1. Purpose

Architecture Soft HOLD SoR for the Core admin dashboard: Core owner list / view / edit / delete on Participants (not human users), Artifacts, negotiations, and offers, plus overall Core counts, at `admin.core.dealoware.com`.

This deliverable is **design Spec only**. It does not unlock build or deploy. It does not invent Stories, code, CDK, spend, or provision. It reconciles prior Step 3 Option A to Core and makes the four Spec must-cover mechanism picks (auth, confirm-before-delete, audit integrity, soft-delete + cascades).

---

## 2. Options + tradeoffs

| Option | Pros | Cons | Pick |
|--------|------|------|------|
| **A. Same Core modular-monolith host; separate Core admin route surface (admin API + thin admin UI) at `admin.core.dealoware.com`; distinct CoreOwner principal; FieldPolicy dual wall (Option A §3a/§3b); no parallel admin ACL** | Matches Spec §7; reuses Step 3 Option A shape for Core; one deployable Core app; clear role boundary vs Participant UI and vs platform admin | Does not invent SSO, second mesh, or platform human-user admin | **Recommended** |
| B. Reuse Participant UI as admin | Fast surface reuse | Collapses role boundary; Product/Spec OUT | **Reject** |
| C. SSO / IdP as delivered for this slice | Mature IdP narrative | O9-style OUT; Spec still requires Core owner auth without treating SSO as delivered | **Reject** (auth still required — see §4.3) |
| D. Separate admin microservice / admin mesh | Isolation theater | Invents ops cost, second cluster, second ACL risk; HOLD provision | **Reject** |
| E. Host at `admin.platform.dealoware.com` or platform-owner-on-platform surface | Matches old Step 3 naming | Wrong host; platform hosts are not Core; Product OUT | **Reject** |
| F. App Runner admin host | Historically simple PaaS | App Runner OUT | **Reject** |

**Pick: Option A** — design Soft HOLD SoR only. HOLD invent Stories. HOLD build. HOLD provision. HOLD invent SSO as delivered. HOLD invent platform-admin host.

**Reconcile prior Step 3**

| Prior Step 3 | This Core admin SoR |
|--------------|---------------------|
| Option A: same app host, separate admin route, distinct owner principal, FieldPolicy dual wall, no parallel admin ACL | **Keep** |
| O1 registered human users | **Drop** |
| Platform-admin / PlatformOwner host naming | **Replace** with `admin.core.dealoware.com` and principal **CoreOwner** |
| O2 Artifacts / O3 negotiations/offers | **Keep and expand** to list/view/edit/delete |
| Participants | **IN** (not human users) |

---

## 3. Recommended architecture design (Option A)

### 3.1 Host and route surface

| Concern | Design |
|---------|--------|
| Public admin host | `admin.core.dealoware.com` only (admin UI + admin API for this Spec) |
| Not this SoR | `admin.platform.dealoware.com`, `platform.dealoware.com`, `api.platform.dealoware.com`, `admin.platform.dealoware.com` |
| App shape | Same Negotiation Core modular monolith; **separate** admin route surface (prefix/area for admin API + thin admin UI) |
| Privacy | Core functionality stays public. This SoR contains no CDK details, bot-platform internals, or AWS account information |
| Provision | HOLD. Host shape inherits Core modular monolith constraints only. App Runner OUT |

### 3.2 Principal / role boundary

| Concern | Design |
|---------|--------|
| Principal class name in code | **`CoreOwner`** — Product meaning: owner of Core. Not “PlatformOwner-on-Core” as the Product meaning, and not a Participant principal |
| Distinct from | Participant UI; Participant principal used by negotiation fabric clients; platform human-user admin |
| FieldPolicy | All admin reads and writes bind Domain `IFieldPolicy.Evaluate` for principal **CoreOwner** (Option A §3a/§3b dual wall). **Cite; do not rewrite.** No parallel admin ACL table that drifts from FieldPolicy |
| Dual wall on admin | API/DB fail-closed for admin routes (§3a). If any admin agent/tool path is invented later, §3b hard wall still binds the same Evaluate. This SoR does **not** deliver admin agents |
| Fail-closed | Missing CoreOwner claim → deny: no entity list, no edit, no delete, no stats beyond a safe unauthorized response. Denied FieldClass → no write and no dump of denied fields into UI/error payloads |

### 3.3 Core owner auth / sign-in (Spec §8 — SA pick)

| Pick | Decision |
|------|----------|
| Mechanism | **Password credential issued to the Core owner** (non-SSO). Credential verified against a Core-stored password hash. Magic link and SSO/IdP as delivered stay OUT of this slice |
| Why | Meets Spec §8 (verified Core owner identity → CoreOwner claim; sessions expire; fail-closed; SSO not delivered). Simplest maintainable non-SSO path for a single Core owner |
| Credential storage | Password hash only in Core data store. Auth secrets never appear in admin API response bodies, audit snapshots, logs, metrics, or traces |
| Session store | **Server-side session** row in Core Postgres, referenced by an **HttpOnly Secure SameSite=Strict** session cookie on `admin.core.dealoware.com` |
| Session lifetime | Absolute max **8 hours** from login. Idle timeout **30 minutes** with sliding renewal on authenticated admin requests. After either expiry, re-sign-in is required before any admin action |
| Role claim attach | On successful password verify, session is created with **CoreOwner** role claim (and Core owner identity id). FieldPolicy uses that claim. No session is issued without CoreOwner |
| Fail-closed | Non-owner identity, missing claim, expired/invalid session → deny all admin routes (safe unauthorized). No entity list, edit, delete, or stats |
| OUT | SSO / IdP as delivered; Cognito as delivered; password reset product breadth beyond Core owner ops; sharing CoreOwner credential via bots |

### 3.4 Confirm before delete (Spec §9 — SA pick)

| Pick | Decision |
|------|----------|
| Admin UI pattern | **Modal confirm** on the entity detail (or list row action). Modal names entity type + identity (display name and id) and shows the cascade summary from §3.6 when cascades apply. Cancel / dismiss leaves data unchanged and writes **no** delete audit row |
| API-only clients | **Required confirm token.** Flow: (1) `POST …/delete-intent` (or equivalent) returns `confirmToken`, entity identity, and cascade summary; (2) `DELETE` (or `POST …/delete`) must present that `confirmToken`. A single blind DELETE without a valid token is rejected. Token is single-use, short-lived (e.g. **5 minutes**), bound to actor + entity + intended cascade set |
| Scope | Every delete of Participant, Artifact, negotiation, or offer — UI and API |

### 3.5 Audit log integrity (Spec §10 — SA pick)

| Pick | Decision |
|------|----------|
| Storage shape | **Append-only audit table** in Core Postgres (same Core DB). Rows are insert-only. No update/delete API or UI path for audit rows |
| Required fields | Who (CoreOwner identity) · When (UTC stored; admin display in a clear timezone) · Entity type + entity id · Action (`Edit` or `Delete`) · Before snapshot · After snapshot (delete after-state shows soft-deleted marker) |
| Cascade rows | Each cascaded soft-delete writes its own audit row, tied to the same admin action id / correlation id |
| Transaction pairing | **Same database transaction** as the mutation: insert audit row(s) + apply edit/soft-delete (+ cascades). If any audit insert fails, **roll back** the whole mutation. Admin mutations are never silent |
| Read access | CoreOwner may **read** audit under FieldPolicy. Sensitive FieldClasses inside snapshots are projected/redacted per FieldPolicy. Audit cannot be edited or deleted from admin UI/API |
| Large field bodies | Snapshot values larger than **4 KiB** are truncated in the audit row; store truncated text + content length + content hash so who/when/what remains complete without dumping full StrategyBody-class bodies |
| Retention | **Retain indefinitely** for this first Core admin slice. Purge / archival is a later Product/SA follow-on — not inventable here as auto-delete from admin |

### 3.6 Soft-delete default + cascades (Spec §11 — SA pick)

| Pick | Decision |
|------|----------|
| Marker | `DeletedAt` (UTC nullable). Soft-deleted when `DeletedAt IS NOT NULL` |
| List default | Hide soft-deleted rows. Optional admin toggle **include deleted** returns soft-deleted rows for support recovery |
| Hard delete | **Deferred entirely.** Not available on Core admin UI/API in this SoR. Later explicit SA/Product follow-on only, with a separate confirm path |

**Cascade picks (confirm Spec recommendations):**

| When deleting | Effect | Rationale |
|---------------|--------|-----------|
| **Offer** | Soft-delete that offer only. Parent negotiation unchanged. Artifacts/Participants unchanged | Spec default; preserves negotiation history |
| **Negotiation** | Soft-delete the negotiation **and all child offers**. Artifacts and Participants unchanged. If negotiation is **open**, confirm must warn it will leave active lists with its offers and state child-offer count | Spec default + §11.3 |
| **Artifact** | **Block** while any non-deleted negotiation references it. Admin error lists those negotiation ids. Soft-delete the Artifact only when no such reference remains. No automatic offer wipe | Spec recommended default; avoids orphaning live negotiations. Detach/cascade alternatives rejected for this slice |
| **Participant** | Soft-delete the Participant. Soft-delete negotiations that Participant owns or participates in (open and closed) **and** those negotiations’ offers. Confirm text states negotiation count + offer count. **Do not** auto-delete Artifacts solely because the Participant is deleted; if Artifacts remain referenced only by soft-deleted negotiations, Artifact rows stay until separately deleted under the Artifact rule | Spec recommended default; Artifacts may be shared / retain referential history |

**Open negotiation (Spec §11.3) — confirmed:**

1. Confirm names the negotiation as open and states child-offer count.  
2. Soft-delete negotiation + child offers in one transaction with audit rows.  
3. After commit, the negotiation is excluded from open-negotiation stats.  
4. Does not delete Artifacts or other Participants unless a separate §3.6 rule applies and was confirmed.

Shared cascade rules: confirm summarizes counts; each cascaded soft-delete is audited; settlement/escrow side effects are OUT.

### 3.7 Editable field sets (within FieldPolicy)

All writes are fail-closed through FieldPolicy for **CoreOwner**. This SoR does **not** invent a second permission matrix. Exact writable sets below are the operational fields Core admin may attempt; Domain FieldPolicy is the deny/allow authority.

| Entity | CoreOwner may edit (if FieldPolicy Write allows) | Never writable via admin body |
|--------|--------------------------------------------------|-------------------------------|
| **Participant** | DisplayName; operational Active/Suspended (or equivalent status) when present as a FieldPolicy-classified field | Auth secrets (password hash, API key full, refresh). LoginEmail is **not** free-form rewritten here as a convenience dump; any LoginEmail change stays a dedicated fail-closed rotation path under FieldPolicy, not a generic edit form field that leaks into errors/logs |
| **Artifact** | Title/Name; Description; owner ParticipantId reassignment only when FieldPolicy + resource rules allow | Raw storage credentials; denied FieldClasses |
| **Negotiation** | Admin-correctable status fields FieldPolicy allows; EndAt / expiry when FieldPolicy allows | Silent rewrite of party membership without going through defined cascade/confirm rules; settlement fields (OUT) |
| **Offer** | Terms/amount fields FieldPolicy allows; admin-correctable status when FieldPolicy allows | Fabricating Accept/Decline as a side-effect outside recorded offer outcome semantics used by stats |

**Reads:** CoreOwner list/detail projections omit FieldClasses denied for CoreOwner. LoginEmail / ContactEmail / StrategyBody / auth secrets follow Option A FieldPolicy — no dump into logs, metrics, traces, or unauthorized UI.

### 3.8 Concurrency, validation, stats

| Concern | Design |
|---------|--------|
| Concurrency | Optimistic concurrency via row `Version` (or `UpdatedAt` ETag). Stale write → **409 Conflict** with safe message; no silent overwrite |
| Validation errors | Field-level safe messages in admin UI. Denied FieldClass values are **not** echoed. Missing CoreOwner → unauthorized, not a validation dump |
| Stats predicates | All five counts exclude soft-deleted rows (`DeletedAt IS NULL`): Participants; open negotiations (`Status = Open` and not soft-deleted); offers; accepts; declines. No charts. No warehouse |
| DB posture (cite only) | H2/H3 check: app→DB TLS VerifyFull; migrations vs app login split; no master on long-lived API. This SoR does not invent CDK, account IDs, or provision |

### 3.9 What this surface is not

| Not this | Cite |
|----------|------|
| Platform admin | `admin.platform.dealoware.com` — OUT |
| Human-user list on Core | Product/Spec OUT (O1 dropped) |
| Participant UI as admin | Spec/Product OUT |
| Inbound bot connector | Comes after this track |
| Settlement / escrow / checkout | OUT |
| SSO / IdP as delivered | OUT; password CoreOwner auth still required (§3.3) |

---

## 4. Explicit IN / OUT / HOLD

### IN

- Core admin at `admin.core.dealoware.com`
- Option A shape: same Core modular monolith; separate admin route surface; **CoreOwner** principal; FieldPolicy dual wall; no parallel admin ACL
- List / view / edit / delete Participants (not human users), Artifacts, negotiations, offers
- Overall Core stats: Participants, open negotiations, offers, accepts, declines (soft-deleted excluded)
- CoreOwner password auth + server-side session (§3.3)
- Modal confirm + API confirm token (§3.4)
- Append-only audit table; same-transaction pairing; not editable/deletable from admin (§3.5)
- Soft-delete (`DeletedAt`) + cascade picks (§3.6); hard delete deferred
- Editable field sets + concurrency + stats predicates (§3.7–§3.8)
- App target awareness: .NET 10 (SoR does not implement retarget)
- Proposed SA-REV-CORE-ADMIN moment for CA → CPM

### OUT

- Human-user list on Core
- Platform admin (`admin.platform.dealoware.com`) and platform human-user admin
- Inbound bot connector (comes after)
- Participant UI reused as admin
- Settlement / escrow / checkout
- SSO / IdP as delivered
- Stories, code, CDK, spend, provision, deploy
- AWS account information, CDK details, bot-platform internals in this SoR
- Charts product / warehouse / cost CloudWatch period views
- MotorMarket / DC4
- App Runner
- Parallel admin ACL
- Separate admin microservice / admin mesh
- Hard delete in this slice
- Admin agents as delivered

### HOLD

| Item | Disposition |
|------|-------------|
| Build / deploy / Stories / code / CDK / spend / provision | Until harden live QA PASS after app image bake, then a separate unlock |
| Architecture QA PASS | Until Soft HOLD SoR CLEAR + Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only (answers 1–10 MET) |
| CA design grounding PASS | After Architecture QA PASS + Security CLEAR path |
| Spec WRITE invent beyond Product/Spec | Not from SA |
| Audit purge / archival policy | Later Product/SA follow-on |
| Hard delete path | Later explicit SA/Product follow-on |
| Magic-link or SSO as delivered | OUT / hold invent as delivered; password path is the pick |

---

## 5. Proposed SA architecture-review moments (CA → CPM · `gate:sa-arch-review`)

| Moment ID | Name | Trigger (when) | Scope under review | Next stage HOLD until |
|-----------|------|----------------|--------------------|------------------------|
| **SA-REV-CORE-ADMIN** | Core admin dashboard Soft HOLD SoR | This deliverable — Soft HOLD SoR written | Option A Core admin route + CoreOwner principal + auth/session + confirm-delete + audit integrity + soft-delete/cascades + editable fields/stats + IN/OUT/HOLD + Spec Security 10/10 constraints addressed | Soft HOLD SoR CLEAR + Security Soft HOLD SoR CLEAR + Security QA **qa-confirm only** (answers 1–10 MET) + Architecture QA PASS + **CA design grounding PASS** before Spec invent beyond this track / any build unlock |
| HOLD next | Until CA PASS path | Not inventable as open now | HOLD invent Stories / build / provision | CA design grounding PASS + Arch QA + Security CLEAR path |

**Rules:** Do not unlock Stories, build, provision, or spend from SA alone. HOLD until harden live QA PASS after app image bake, then a separate unlock.

---

## 6. Security answers (Architecture always-critical handshake)

**Checklist:** `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` (**ISSUED**).
**Expected Security QA Soft HOLD SoR:** `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (handshake Soft HOLD SoR = **qa-confirm only** — **no invent points-review Soft HOLD SoR**).
**HOLD Architecture QA PASS** until Soft HOLD SoR CLEAR + Security Soft HOLD SoR CLEAR + Security QA confirms points 1–10 via that qa-confirm Soft HOLD SoR only.
**Spec Security:** Spec checklist pts 1–10 PASS via Spec Security qa-confirm remain binding for the Spec gate; this §6 binds the **SA** checklist.

| # | Security point | Architecture answer | Cite |
|---|----------------|---------------------|------|
| 1 | Host + role boundary | **MET.** Admin only at `admin.core.dealoware.com`. Principal **CoreOwner** (owner of Core). **CoreOwner ≠ Participant UI ≠ platform admin.** O1 human users on Core OUT. Reject Participant-UI-as-admin and `admin.platform.dealoware.com` host. | §2 Option A / reject B/E; §3.1; §3.2; §3.9; §4 OUT |
| 2 | CoreOwner auth / session (sign-in mechanism pick) | **MET.** Password non-SSO Core owner auth; password hash only in Core store; server-side session + HttpOnly Secure SameSite=Strict cookie; **8h absolute / 30m idle**; CoreOwner claim at login; after expiry re-sign-in before any admin action. Auth secrets never in admin API bodies, audit snapshots, logs, metrics, or traces. Soft HOLD invent SSO/IdP/Cognito as delivered. Fail-closed on missing/invalid/expired session. | §3.3; §4 OUT SSO |
| 3 | Fail-closed dual-wall FieldPolicy; no parallel admin ACL | **MET.** All admin reads/writes bind Domain `IFieldPolicy.Evaluate` for **CoreOwner** (Option A §3a/§3b dual wall; cite, do not rewrite). Missing CoreOwner → deny all admin routes (no list/edit/delete/stats beyond safe unauthorized). Denied FieldClass → no write and no dump into UI/error payloads. Parallel admin ACL rejected. Soft HOLD invent admin agents in this Soft HOLD SoR. | §3.2; §3.3; §3.7; §4 OUT Parallel admin ACL |
| 4 | Confirm-before-delete + cascade summary integrity | **MET.** UI: modal confirm naming type + identity + cascade summary when cascades apply; cancel → no mutation and no delete audit. API: confirm token required (single-use, short-lived e.g. 5 minutes, bound to actor + entity + cascade set); blind DELETE rejected. | §3.4; §3.6 |
| 5 | Audit append-only integrity | **MET.** Append-only audit for every successful admin edit/delete (+ cascade rows). Who/when/type+id/action/before/after. Cannot edit or delete audit from admin UI/API. Same-transaction pairing: audit insert fail → roll back mutation. FieldPolicy redacts sensitive snapshot fields; bodies >4 KiB truncated with length + hash (no StrategyBody dump). | §3.5 |
| 6 | Soft-delete + cascades | **MET.** Soft-delete marker `DeletedAt` default; hard delete deferred (not on this admin surface). Offer → soft-delete offer only. Negotiation → soft-delete negotiation + child offers; open negotiation confirm warns. Artifact → **block** while any non-deleted negotiation references it. Participant → soft-delete Participant + their negotiations + those offers (confirm counts); do not auto-delete Artifacts. Settlement side effects OUT. | §3.6; §4 OUT Hard delete / settlement |
| 7 | Edit write surface FieldPolicy-only; concurrency fail-closed | **MET.** Only FieldPolicy-allowed fields writable; no second permission matrix. Optimistic concurrency via row `Version` / `UpdatedAt` ETag; stale write → **409 Conflict** safe message, no silent overwrite. Denied FieldClass values not echoed. LoginEmail / ContactEmail / StrategyBody / auth secrets follow FieldPolicy — no dump into errors/logs/metrics/traces. | §3.7; §3.8 |
| 8 | Stats / list scrub soft-deleted; no warehouse invent | **MET.** List defaults hide soft-deleted rows; optional include-deleted toggle only. All five Core stats exclude soft-deleted rows (`DeletedAt IS NULL`): Participants; open negotiations; offers; accepts; declines. Soft HOLD invent charts product or warehouse. | §3.6 List default; §3.8 Stats; §4 OUT Charts/warehouse |
| 9 | OUT / Soft HOLD pack | **MET.** Soft HOLD invent Stories / code / CDK / spend / provision / deploy Soft HOLD until harden live QA PASS after app image bake then separate unlock. No AWS account / CDK / bot-platform internals in Soft HOLD SoR. App Runner OUT. Platform admin hosts OUT. Inbound bot connector after. Settlement/escrow/checkout OUT. SSO/IdP as delivered OUT. Human-user list on Core OUT. Participant UI as admin OUT. PoC **$0**. Cost/critical → COO → CEO. | §4 OUT / HOLD; header Locks; §3.9 |
| 10 | Traceability + handshake Soft HOLD SoR | **MET.** Cites Product scope + Spec + Spec Security PASS + Spec QA PASS + Spec Security qa-confirm + Option A §3a/§3b + Step 3 reconcile + mechanism picks §§3.3–3.6 + Arch QA interim content PASS (`verification/2026-10-05__sa__verification__core-admin-dashboard-review.md`). Architecture QA must **not** formal PASS until Security QA confirms via `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** — do not invent points-review Soft HOLD SoR. Soft HOLD build stands. Not a build unlock. | Sources; this §6; §5; Arch QA interim; checklist DOC-FLOW |

---

## 7. Done-list → Architecture QA

- [x] Options cover CA brief + Product scope + Spec (Option A pick; Step 3 reconciled)
- [x] Host locked to `admin.core.dealoware.com`; principal named **CoreOwner**
- [x] Option A §3a/§3b dual wall cited — no parallel admin ACL
- [x] Must-design picks concrete: auth (§3.3), confirm-delete (§3.4), audit (§3.5), soft-delete/cascades (§3.6)
- [x] Editable fields, concurrency, stats predicates stated
- [x] Explicit IN / OUT / HOLD
- [x] Review moments table present for CA → CPM (`gate:sa-arch-review`)
- [x] SA Security checklist pts 1–10 **MET** with section cites (checklist ISSUED); Spec Security 10/10 still mapped; HOLD Arch QA until Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only
- [x] No CDK / AWS account / bot-platform internals; PoC $0; no Stories/code
- [ ] Architecture QA review
- [ ] Confirm to Chief only after Soft HOLD SoR CLEAR + Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only + Architecture QA PASS

---

## 8. CEO questions / open risks left HOLD

| Item | Note |
|------|------|
| None blocking this Soft HOLD SoR | Four must-covers have concrete picks |
| HOLD | Build/deploy/spend until harden live QA PASS after app image bake |
| HOLD | Architecture QA PASS until Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only |
| Deferred (not blocking) | Hard delete follow-on; audit purge/archival; magic-link/SSO as delivered |

---

## 9. Disposition

Soft HOLD SoR amended: § Security answers points 1–10 **MET** against ISSUED SA checklist (rebinding to Chief Security amended itemization 1–10). HOLD Architecture QA PASS until Soft HOLD SoR CLEAR + Security Soft HOLD SoR CLEAR + Security QA confirms 1–10 via qa-confirm Soft HOLD SoR only (**no** invent points-review Soft HOLD SoR). Design Spec only. HOLD build, deploy, Stories, code, CDK, spend, and provision. PoC **$0**. Principal **CoreOwner** at `admin.core.dealoware.com`. FieldPolicy dual wall cited; no parallel admin ACL. Ready for Senior Security score.
