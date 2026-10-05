# Architecture SoR — Core admin dashboard

**Status:** Soft HOLD SoR **amended** from cleared Product (CEO deltas). Design Spec only. Soft HOLD Spec invent against this Soft HOLD SoR until Soft HOLD SoR CLEAR. Soft HOLD Stories, build, deploy, code, CDK, spend, and provision until harden live QA PASS after app image bake, then a separate unlock. Soft HOLD invent password. Soft HOLD invent AWS account IDs. PoC **$0**.  
**Date:** 2026-10-05 (amend 13:35 ET)  
**Author:** Dealoware Senior Architect  
**Brief from:** Chief Architect → Senior Architect → Architecture QA (Architecture always Security-critical) · COO A2 critical path  
**Moment ID:** **SA-REV-CORE-ADMIN** (this deliverable)  
**DOC-FLOW:** `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md`  
**Host:** `admin.core.dealoware.com` only  
**App target:** .NET 10 (retarget in flight). This SoR does not implement the retarget. Historical PoC notes that say `net8.0` stay as delivery evidence.  
**SA Security checklist:** `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` — **ISSUED v2** (pts 1–14). §6 Security answers points 1–14 **MET**. Soft HOLD Architecture QA formal PASS until Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only (handshake Soft HOLD SoR = **qa-confirm only**; do not invent a points-review Soft HOLD SoR).

| Field | Value |
|-------|-------|
| Date | 2026-10-05 |
| Story / epic / phase | Core admin dashboard (Design Spec track; Soft HOLD invent Spec issue close) |
| Binding Product | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` (cleared; 1:27 lists/search/superadmin + 1:30 Turnstile-only) |
| Spec input (not QA'd) | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` v2.1 — Chief Spec CLEAR; Spec QA pending. Soft HOLD Spec invent until Soft HOLD SoR CLEAR; if Soft HOLD SoR differs, Spec reconciles later |
| Prior Spec QA / Spec Security | Prior PASS paths covered pre-expansion only — do not treat as covering this amend |
| SA Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` — **ISSUED v2**; answers 1–14 **MET** |
| Expected SA Security QA Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (qa-confirm only) |
| CFO Soft HOLD cost note | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` — Turnstile **$0/mo**; SES **under $0.01/mo** at assumed 5 emails/mo; Soft HOLD invent spend; Finance QA **PASS** (`finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md`) |
| Host shape | Same Core modular monolith; separate admin route surface at `admin.core.dealoware.com`. App Runner OUT. Provision HOLD |
| Cost | PoC **$0** · Soft HOLD invent spend · Soft HOLD invent provision · Soft HOLD build |
| Principal name | **CoreOwner** (single system superadmin `io@aiknowhow.com`) |
| Security | Architecture always Security-critical · SA checklist ISSUED v2 · answers 1–14 MET · Soft HOLD Arch QA until Security Soft HOLD SoR CLEAR + qa-confirm Soft HOLD SoR only |

## Sources

| Source | Role |
|--------|------|
| Product Core admin scope | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` — binding IN/OUT, AC1–AC9, lists/search/superadmin |
| Spec draft v2.1 (input) | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` — Chief Spec CLEAR; not Spec-QA'd; Soft HOLD Spec invent until Soft HOLD SoR CLEAR |
| CFO Soft HOLD cost note | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` — Turnstile $0/mo; SES under $0.01/mo assumed; Soft HOLD invent spend; Finance QA **PASS** (`finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md`) |
| Prior Step 3 SA (reconcile) | `architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` — keep Option A shape; drop O1; replace platform-admin host |
| FieldPolicy dual wall | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3a/§3b — **cite; do not rewrite**; no parallel admin ACL |
| H2/H3 DB posture (optional) | `architecture/2026-10-05__sa__architecture__h2-h3-db-tls-login-split-check.md` — VerifyFull + migrations/app login split; no CDK / account invent |
| CEO host standing rule | `admin.core.dealoware.com` = Core admin; platform hosts are not Core; Core may run outside AWS; no CDK / bot-platform / AWS account info in this SoR |

**Locks (cite; do not change):** Host `admin.core.dealoware.com` only · CoreOwner ≠ Participant UI · FieldPolicy dual wall; no parallel admin ACL · Drop O1 human users · Soft HOLD build/deploy/Stories/code/CDK/spend until harden live QA PASS after app image bake · Soft HOLD invent password · Soft HOLD invent AWS account IDs · SSO/IdP as delivered OUT · AWS WAF CAPTCHA OUT · reCAPTCHA OUT · inbound bot connector after this · settlement OUT · MotorMarket/DC4 OUT · App Runner OUT · PoC **$0**.

---

## 1. Purpose

Architecture Soft HOLD SoR for the Core admin dashboard: Core owner list / view / edit / delete on Participants (not human users), Artifacts, negotiations, and offers, plus overall Core counts, at `admin.core.dealoware.com` — with **all-status lists**, **sort/filter**, **server-side paging**, **name search**, and **single system superadmin** auth (email + password + required TOTP; Turnstile; SES behind mail interface).

This deliverable is **design Spec only**. It does not unlock build or deploy. Soft HOLD invent Stories, code, CDK, spend, provision, or password values. Soft HOLD Spec invent against this Soft HOLD SoR until Soft HOLD SoR CLEAR.

---

## 2. Options + tradeoffs

| Option | Pros | Cons | Pick |
|--------|------|------|------|
| **A. Same Core modular-monolith host; separate Core admin route surface (admin API + thin admin UI) at `admin.core.dealoware.com`; distinct CoreOwner principal; FieldPolicy dual wall (Option A §3a/§3b); no parallel admin ACL** | Matches Product/Spec; reuses Step 3 Option A shape for Core; one deployable Core app; clear role boundary vs Participant UI and vs platform admin | Does not invent SSO, second mesh, or platform human-user admin | **Recommended** |
| B. Reuse Participant UI as admin | Fast surface reuse | Collapses role boundary; Product OUT | **Reject** |
| C. SSO / IdP as delivered for this slice | Mature IdP narrative | O9-style OUT; Product requires email + password + TOTP | **Reject** |
| D. Separate admin microservice / admin mesh | Isolation theater | Invents ops cost, second cluster, second ACL risk; Soft HOLD invent provision | **Reject** |
| E. Host at `admin.platform.dealoware.com` | Matches old Step 3 naming | Wrong host; platform hosts are not Core; Product OUT | **Reject** |
| F. App Runner admin host | Historically simple PaaS | App Runner OUT | **Reject** |
| G. AWS WAF CAPTCHA or reCAPTCHA | Alternate CAPTCHA | CEO OUT — Turnstile only | **Reject** |

**Pick: Option A** — Soft HOLD SoR only. Soft HOLD invent Stories. Soft HOLD build. Soft HOLD invent provision. Soft HOLD invent SSO as delivered. Soft HOLD invent platform-admin host.

**Reconcile prior Step 3**

| Prior Step 3 | This Core admin SoR |
|--------------|---------------------|
| Option A: same app host, separate admin route, distinct owner principal, FieldPolicy dual wall, no parallel admin ACL | **Keep** |
| O1 registered human users | **Drop** |
| Platform-admin / PlatformOwner host naming | **Replace** with `admin.core.dealoware.com` and principal **CoreOwner** |
| O2 Artifacts / O3 negotiations/offers | **Keep and expand** to list/view/edit/delete + all-status lists + name search |
| Participants | **IN** (not human users) |

---

## 3. Recommended architecture design (Option A)

### 3.1 Host and route surface

| Concern | Design |
|---------|--------|
| Public admin host | `admin.core.dealoware.com` only (admin UI + admin API for this Soft HOLD SoR) |
| Not this SoR | `admin.platform.dealoware.com`, `platform.dealoware.com`, `api.platform.dealoware.com` |
| App shape | Same Negotiation Core modular monolith; **separate** admin route surface (prefix/area for admin API + thin admin UI) |
| Privacy | Core functionality stays public. This SoR contains no CDK details, bot-platform internals, or AWS account information |
| Provision | Soft HOLD invent provision. Host shape inherits Core modular monolith constraints only. App Runner OUT |

### 3.2 Principal / role boundary

| Concern | Design |
|---------|--------|
| Principal class name in code | **`CoreOwner`** — Product meaning: owner of Core. Not a Participant principal |
| Single system superadmin | Login email **`io@aiknowhow.com` only**. No multi-admin operator ACL |
| Distinct from | Participant UI; Participant principal; platform human-user admin |
| FieldPolicy | All admin reads and writes bind Domain `IFieldPolicy.Evaluate` for principal **CoreOwner** (Option A §3a/§3b dual wall). **Cite; do not rewrite.** No parallel admin ACL |
| Dual wall on admin | API/DB fail-closed for admin routes (§3a). Soft HOLD invent admin agents in this Soft HOLD SoR |
| Fail-closed | Missing CoreOwner claim → deny: no entity list, no edit, no delete, no stats beyond a safe unauthorized response. Denied FieldClass → no write and no dump of denied fields into UI/error payloads |

### 3.3 Core owner auth / sign-in (Product must-cover · Spec draft §8 locks)

| Pick | Decision |
|------|----------|
| Identity | **One** system superadmin. Login email: `io@aiknowhow.com`. Email + password. Soft HOLD invent password — **no password values** in Soft HOLD SoR, docs, Spec text, chat, or commit history |
| Bootstrap | Seed CoreOwner identity only. Initial password set **only** via out-of-band **one-time** bootstrap/reset link (time-limited; Spec draft: expire ≤ **24h**, single-use; SA may tighten). Soft HOLD invent embedding any password or live link in Soft HOLD SoR |
| TOTP 2FA | **Required** authenticator-app TOTP for every login after enrollment. First interactive admin session cannot start until TOTP is enrolled |
| Email OTP | **NOT** a 2FA fallback (CEO). Soft HOLD invent standing email OTP as 2FA |
| Recovery | One-time **recovery codes** only (hashed at rest). Not standing email OTP as 2FA |
| Password reset | Email link **plus** TOTP (or recovery code path under the same 2FA rule). Soft HOLD invent password-only reset without 2FA |
| CAPTCHA | **Cloudflare Turnstile only** on login + password-reset pages. Cost: **$0/mo** (CFO Soft HOLD cost note). AWS WAF CAPTCHA **OUT**. reCAPTCHA **OUT**. Soft HOLD invent any other CAPTCHA provider. Soft HOLD invent WAF CAPTCHA / WAF rate-based ALB notes in this Soft HOLD SoR |
| Credential storage | Password hash + TOTP secret (encrypted/protected) + hashed recovery codes in Core data store. Auth secrets never appear in admin API response bodies, audit snapshots, logs, metrics, or traces |
| Session store | **Server-side session** row in Core Postgres, referenced by an **HttpOnly Secure SameSite=Strict** session cookie on `admin.core.dealoware.com` |
| Session lifetime | Absolute max **8 hours** from login. Idle timeout **30 minutes** with sliding renewal on authenticated admin requests. After either expiry, full re-sign-in (password + TOTP) before any admin action |
| Lockout / rate limit | Account: **5** failed logins in **15 minutes** → **30-minute** account lock. IP: **20** failed logins in **15 minutes** → IP throttle for the same window class. Failures include bad password / bad TOTP / locked (reason class only; no secret material) |
| Role claim attach | On successful password + TOTP verify, session is created with **CoreOwner** role claim. No session without CoreOwner |
| Fail-closed | Non-owner identity, missing claim, expired/invalid session, locked account → deny all admin routes (safe unauthorized) |
| Mail | Bootstrap and password-reset email via **SES**, delivered through a **mail interface** (port/adapter). Core application code must **not** depend on AWS SES SDK types directly so Core may run off AWS. Soft HOLD invent AWS account IDs, region, SES identity, from-address, or provision from Soft HOLD SoR |
| CFO Soft HOLD cost | Cite `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md`: Turnstile **$0/mo**; SES **under $0.01/mo** at assumed 5 emails/mo; Soft HOLD invent spend; Finance QA **PASS** (`finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md`); not spend/provision approval |
| OUT | SSO / IdP as delivered; Cognito as delivered; multi-admin ACL; email OTP as 2FA; password values anywhere; WAF CAPTCHA; reCAPTCHA |

### 3.4 Confirm before delete

| Pick | Decision |
|------|----------|
| Admin UI pattern | **Modal confirm** on the entity detail (or list row action). Modal names entity type + identity (display name and id) and shows the cascade summary from §3.6 when cascades apply. Cancel / dismiss leaves data unchanged and writes **no** delete audit row |
| API-only clients | **Required confirm token.** Flow: (1) `POST …/delete-intent` returns `confirmToken`, entity identity, and cascade summary; (2) `DELETE` (or `POST …/delete`) must present that `confirmToken`. Blind DELETE without a valid token is rejected. Token is single-use, short-lived (**5 minutes**), bound to actor + entity + intended cascade set |
| Scope | Every delete of Participant, Artifact, negotiation, or offer — UI and API |

### 3.5 Audit log integrity

| Pick | Decision |
|------|----------|
| Storage shape | **Append-only audit table** in Core Postgres (same Core DB). Rows are insert-only. No update/delete API or UI path for audit rows |
| Mutation fields | Who (CoreOwner identity) · When (UTC stored) · Entity type + entity id · Action (`Edit` or `Delete`) · Before snapshot · After snapshot (delete after-state shows soft-deleted marker) |
| Auth events (also audited) | Login success; login failure (reason class only); password reset request; password reset completion; TOTP enrollment; TOTP change/re-enrollment; recovery code use (success) |
| Cascade rows | Each cascaded soft-delete writes its own audit row, tied to the same admin action id / correlation id |
| Transaction pairing | **Same database transaction** as the mutation: insert audit row(s) + apply edit/soft-delete (+ cascades). If any audit insert fails, **roll back** the whole mutation |
| Soft-deleted list toggle | Using the include-soft-deleted toggle is **not** audited (Spec draft lock). Toggle is **CoreOwner / superadmin only** |
| Read access | CoreOwner may **read** audit under FieldPolicy. Sensitive FieldClasses inside snapshots are projected/redacted per FieldPolicy. Audit cannot be edited or deleted from admin UI/API |
| Large field bodies | Snapshot values larger than **4 KiB** are truncated; store truncated text + content length + content hash |
| Retention | **Retain indefinitely** for this first Core admin slice. Purge / archival is a later Product/SA follow-on |

### 3.6 Soft-delete default + cascades

| Pick | Decision |
|------|----------|
| Marker | `DeletedAt` (UTC nullable). Soft-deleted when `DeletedAt IS NOT NULL` |
| List default | Hide soft-deleted rows. Explicit admin toggle **include soft-deleted** returns soft-deleted rows (CoreOwner only; using toggle not audited — §3.5) |
| Hard delete | **Deferred entirely.** Not available on Core admin UI/API in this Soft HOLD SoR |

**Cascade picks:**

| When deleting | Effect |
|---------------|--------|
| **Offer** | Soft-delete that offer only. Parent negotiation unchanged. Artifacts/Participants unchanged |
| **Negotiation** | Soft-delete the negotiation **and all child offers**. Artifacts and Participants unchanged. If negotiation is **open**, confirm must warn and state child-offer count |
| **Artifact** | **Block** while any non-deleted negotiation references it. Admin error lists those negotiation ids. Soft-delete Artifact only when no such reference remains |
| **Participant** | Soft-delete the Participant. Soft-delete negotiations that Participant owns or participates in **and** those negotiations’ offers. Confirm states negotiation count + offer count. **Do not** auto-delete Artifacts |

**Open negotiation:** confirm names open + child-offer count; soft-delete negotiation + child offers in one transaction with audit rows; excluded from open-negotiation stats after commit.

Shared cascade rules: confirm summarizes counts; each cascaded soft-delete is audited; settlement/escrow side effects are OUT.

### 3.7 Editable field sets (within FieldPolicy)

All writes are fail-closed through FieldPolicy for **CoreOwner**. This Soft HOLD SoR does **not** invent a second permission matrix.

| Entity | CoreOwner may edit (if FieldPolicy Write allows) | Never writable via admin body |
|--------|--------------------------------------------------|-------------------------------|
| **Participant** | DisplayName; operational Active/Suspended (or equivalent) when FieldPolicy-classified | Auth secrets (password hash, API key full, refresh). LoginEmail changes only via dedicated fail-closed rotation — not a generic edit dump |
| **Artifact** | Title/Name; Description; owner ParticipantId reassignment only when FieldPolicy + resource rules allow | Raw storage credentials; denied FieldClasses |
| **Negotiation** | Admin-correctable status fields FieldPolicy allows; EndAt / expiry when FieldPolicy allows | Silent rewrite of party membership without cascade/confirm; settlement fields (OUT) |
| **Offer** | Terms/amount fields FieldPolicy allows; admin-correctable status when FieldPolicy allows | Fabricating Accept/Decline outside recorded offer outcome semantics used by stats |

**Reads:** CoreOwner list/detail projections omit FieldClasses denied for CoreOwner. Auth secrets / StrategyBody follow Option A FieldPolicy — no dump into logs, metrics, traces, or unauthorized UI.

### 3.8 Concurrency, validation, stats

| Concern | Design |
|---------|--------|
| Concurrency | Optimistic concurrency via row `Version` (or `UpdatedAt` ETag). Stale write → **409 Conflict** with safe message; no silent overwrite |
| Validation errors | Field-level safe messages in admin UI. Denied FieldClass values are **not** echoed. Missing CoreOwner → unauthorized, not a validation dump |
| Stats predicates | All five counts exclude soft-deleted rows (`DeletedAt IS NULL`): Participants; open negotiations (`Status = Open` and not soft-deleted); offers; accepts; declines. No charts. No warehouse |
| DB posture (cite only) | H2/H3 check: app→DB TLS VerifyFull; migrations vs app login split; no master on long-lived API. Soft HOLD invent CDK, account IDs, or provision |

### 3.9 Lists, sort/filter, paging, name search (Product IN 5–6)

| Concern | Design |
|---------|--------|
| Negotiation statuses listed | **Every** Core status on the list surface: `Open`, `Accepted`, `Declined`, `Expired`, **`Withdrawn`** (mapped as a first-class Core negotiation/offer status enum value — not a UI-only label), plus soft-deleted via toggle. Soft HOLD invent inventing statuses beyond Product + Core domain; Spec reconciles naming if Soft HOLD SoR differs |
| Offer statuses listed | Same all-status rule: open / accepted / declined / expired / withdrawn / soft-deleted via toggle (+ any other Core offer statuses Spec names) |
| Soft-deleted rows | Default list excludes them. Explicit **include soft-deleted** toggle (CoreOwner only). Using the toggle is **not** audited |
| Sort / filter fields | Status; created/updated; participant; artifact; value/price; negotiation id |
| Server-side paging | **Required.** **Offset/limit** paging (SA pick over cursor for this first Core admin slice — simpler for admin UI sort/filter; cursor may follow later). Default page size **50**; max page size **200**. Soft HOLD invent client-side full-table load |
| Name search — Participants | Case-insensitive contains on Participant display name / Core identifier |
| Name search — Artifacts | Case-insensitive contains on Artifact title / name / subject |
| Name search — Negotiations | Case-insensitive contains on composite of artifact/subject name **and** participant names on that negotiation |
| Name search — Offers | Case-insensitive contains on composite of artifact/subject name, offering participant name, and related negotiation id (id remains filterable separately) |
| Search rules | Combines with sort/filter/paging; empty search = no name constraint; parameterized only (no injection); FieldPolicy still binds hit payloads (denied fields never included) |

### 3.10 What this surface is not

| Not this | Cite |
|----------|------|
| Platform admin | `admin.platform.dealoware.com` — OUT |
| Human-user list on Core | Product OUT (O1 dropped) |
| Participant UI as admin | Product OUT |
| Inbound bot connector | Comes after this track |
| Settlement / escrow / checkout | OUT |
| SSO / IdP as delivered | OUT; email + password + TOTP required (§3.3) |
| AWS WAF CAPTCHA / reCAPTCHA | OUT — Turnstile only |
| Password values in Soft HOLD SoR / docs / chat | Forbidden · Soft HOLD invent password |

---

## 4. Explicit IN / OUT / HOLD

### IN

- Core admin at `admin.core.dealoware.com`
- Option A shape: same Core modular monolith; separate admin route surface; **CoreOwner** principal; FieldPolicy dual wall; no parallel admin ACL
- Single system superadmin `io@aiknowhow.com`; email + password + required TOTP; recovery codes hashed; bootstrap via one-time link; Turnstile only; SES behind mail interface; lockout + session timers; login/reset/2FA audit (§3.3, §3.5)
- List / view / edit / delete Participants (not human users), Artifacts, negotiations, offers
- All-status lists + sort/filter + offset/limit paging (default 50 / max 200) + name search (§3.9)
- Overall Core stats: Participants, open negotiations, offers, accepts, declines (soft-deleted excluded)
- Modal confirm + API confirm token (§3.4)
- Append-only audit; same-transaction pairing; not editable/deletable from admin (§3.5)
- Soft-delete (`DeletedAt`) + cascade picks (§3.6); hard delete deferred
- Editable field sets + concurrency + stats predicates (§3.7–§3.8)
- CFO Soft HOLD cost cite for SES + Turnstile
- App target awareness: .NET 10 (Soft HOLD SoR does not implement retarget)
- Proposed SA-REV-CORE-ADMIN moment for CA → CPM

### OUT

- Human-user list on Core
- Platform admin (`admin.platform.dealoware.com`) and platform human-user admin
- Inbound bot connector (comes after)
- Participant UI reused as admin
- Settlement / escrow / checkout
- SSO / IdP as delivered
- Email OTP as 2FA fallback
- AWS WAF CAPTCHA / reCAPTCHA / any CAPTCHA other than Turnstile
- Password values in Soft HOLD SoR, docs, Spec text, chat, or commit history
- Stories, code, CDK, spend, provision, deploy
- AWS account information, CDK details, bot-platform internals in this Soft HOLD SoR
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
| Soft HOLD invent password | Stands — bootstrap/reset link only |
| Soft HOLD invent AWS account IDs / provision | Stands — SES named via mail interface + CFO Soft HOLD cost cite only |
| Soft HOLD invent spend | CFO Soft HOLD cost note is estimate only; Finance QA **PASS** (`finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md`); Soft HOLD invent spend still; not spend approval |
| Soft HOLD Spec invent | Until Soft HOLD SoR CLEAR enough for Spec; Spec draft v2.1 is input; Spec reconciles later if Soft HOLD SoR differs |
| Architecture QA formal PASS | Until Soft HOLD SoR CLEAR + Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only (checklist ISSUED v2; answers 1–14 MET) |
| CA design grounding PASS | After Architecture QA PASS + Security CLEAR path on this amend |
| Audit purge / archival policy | Later Product/SA follow-on |
| Hard delete path | Later explicit SA/Product follow-on |
| Cursor paging | Deferred; offset/limit is the Soft HOLD SoR pick for this slice |

---

## 5. Proposed SA architecture-review moments (CA → CPM · `gate:sa-arch-review`)

| Moment ID | Name | Trigger (when) | Scope under review | Next stage HOLD until |
|-----------|------|----------------|--------------------|------------------------|
| **SA-REV-CORE-ADMIN** | Core admin dashboard Soft HOLD SoR | This deliverable — Soft HOLD SoR amended for CEO deltas | Option A Core admin + CoreOwner + auth (Turnstile/TOTP/SES mail interface) + lists/search/paging + confirm-delete + audit + soft-delete/cascades + CFO Soft HOLD cost cite | Soft HOLD SoR CLEAR + Security Soft HOLD SoR CLEAR + Security QA **qa-confirm only** + Architecture QA PASS + **CA design grounding PASS** before Spec invent beyond Soft HOLD SoR CLEAR / any build unlock |
| HOLD next | Until CA PASS path | Not inventable as open now | Soft HOLD invent Stories / build / provision | CA design grounding PASS + Arch QA + Security CLEAR path |

**Rules:** Do not unlock Stories, build, provision, or spend from SA alone. Soft HOLD until harden live QA PASS after app image bake, then a separate unlock.

---

## 6. Security answers (Architecture always-critical handshake)

**Checklist:** `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` — **ISSUED v2** (CEO expansion; pts 1–14).  
**Expected Security QA Soft HOLD SoR:** `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (handshake Soft HOLD SoR = **qa-confirm only** — **no invent points-review Soft HOLD SoR**). Prior pre-expansion Soft HOLD SoR qa-confirm **superseded**.  
**Soft HOLD Architecture QA formal PASS** until Soft HOLD SoR CLEAR + Security Soft HOLD SoR CLEAR + Security QA confirms points 1–14 via that qa-confirm Soft HOLD SoR only.  
**Status:** Soft HOLD SoR §6 rebound; Security answers points 1–14 **MET**.

| # | Security point | Architecture answer | Cite |
|---|----------------|---------------------|------|
| 1 | Host + single superadmin CoreOwner | **MET.** Admin only at `admin.core.dealoware.com`. Principal **CoreOwner** = single system superadmin login email **`io@aiknowhow.com`**. CoreOwner ≠ Participant UI ≠ platform admin. O1 human users OUT. Multi-admin operator ACL rejected. | §2 Option A; §3.1; §3.2; §3.10; §4 OUT |
| 2 | Email + password + one-time bootstrap; Soft HOLD invent password | **MET.** Email + password for that superadmin. Bootstrap via single-use out-of-band link (≤24h; SA may tighten). Soft HOLD invent password — **no password values** in Soft HOLD SoR/docs/chat. Soft HOLD invent SSO/IdP as delivered. | §3.3; §4 OUT/HOLD |
| 3 | TOTP required; no email OTP fallback; hashed recovery codes | **MET.** TOTP required before first interactive admin session and on every login after. Email OTP **not** a 2FA fallback (CEO). Recovery = one-time recovery codes stored **hashed** only (never plaintext in Soft HOLD SoR/logs/UI dump). | §3.3 |
| 4 | Password reset = email link + 2FA | **MET.** Reset requires email link **and** TOTP (or hashed recovery-code path under the same 2FA rule). Soft HOLD invent password-only reset without 2FA. | §3.3 |
| 5 | Turnstile only + lockout / rate limit + session | **MET.** CAPTCHA on login + password-reset = **Cloudflare Turnstile only**. AWS WAF CAPTCHA OUT; reCAPTCHA OUT. Account lockout **5 fails/15m → 30m**; IP **20 fails/15m**. Session **8h absolute / 30m idle**; after expiry full re-auth (password + TOTP). HttpOnly Secure SameSite=Strict cookie. Auth secrets never in API bodies/audit/logs/metrics/traces. | §3.3; §4 OUT |
| 6 | SES behind mail interface; Soft HOLD invent AWS | **MET.** Bootstrap/reset mail via **SES** through a **mail interface**; Core must not depend on AWS SES SDK types directly. Soft HOLD invent AWS account IDs, region, SES identity, from-address, or provision. Cite CFO Soft HOLD cost note (Turnstile $0/mo; SES under $0.01/mo assumed; Finance QA **PASS** via `finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md`) — Soft HOLD invent spend / not spend approval. | §3.3; Sources; §4 HOLD |
| 7 | Audit: login/reset/2FA + edit/delete; soft-deleted toggle not audited | **MET.** Append-only audit for successful admin edits/deletes (+ cascades) **and** login success/failure (reason class only), password reset request/completion, TOTP enroll/change, recovery-code use. Audit cannot be edited/deleted from admin UI/API. Same-transaction pairing for mutations (audit fail → roll back). Soft-deleted toggle use **not** audited; toggle **CoreOwner only**. No secret dump in snapshots; bodies >4 KiB truncated with length + hash. | §3.5 |
| 8 | Fail-closed dual-wall FieldPolicy; no parallel admin ACL | **MET.** All admin reads/writes bind Domain `IFieldPolicy.Evaluate` for **CoreOwner** (Option A §3a/§3b; cite, do not rewrite). Missing/invalid/expired session or missing TOTP → deny all admin routes. Denied FieldClass → no write and no dump. Parallel multi-operator ACL rejected. Soft HOLD invent admin agents. | §3.2; §3.3; §3.7; §4 OUT |
| 9 | Confirm-before-delete + cascade summary integrity | **MET.** UI modal confirm names type + identity + cascade summary; cancel → no mutation and no delete audit. API confirm token required (single-use, **5 minutes**, bound to actor + entity + cascade set); blind DELETE rejected. | §3.4; §3.6 |
| 10 | Soft-delete + cascades | **MET.** Soft-delete marker `DeletedAt` default; hard delete deferred. Offer → offer only. Negotiation → negotiation + child offers; open warn. Artifact → **block** while non-deleted negotiation references it. Participant → Participant + negotiations + those offers (confirm counts); do not auto-delete Artifacts. Settlement OUT. | §3.6; §4 OUT |
| 11 | All-status lists + name search + server-side paging (safe) | **MET.** Negotiations/offers all statuses (incl. **Withdrawn** + soft-deleted via toggle). Sort/filter locked fields. Offset/limit paging default **50** / max **200**. Name search per Product meanings on all four tables. Soft HOLD invent client-only full-table dump. Search/filter parameterized (no injection); denied FieldClass never in hit payloads. Stats exclude soft-deleted; Soft HOLD invent charts/warehouse. | §3.8; §3.9; §4 OUT |
| 12 | Edit write surface FieldPolicy-only; concurrency fail-closed | **MET.** Only FieldPolicy-allowed fields writable; no second permission matrix. Optimistic concurrency; stale write → **409 Conflict** safe message. LoginEmail / ContactEmail / StrategyBody / auth secrets follow FieldPolicy — no dump into errors/logs/metrics/traces. | §3.7; §3.8 |
| 13 | OUT / Soft HOLD pack | **MET.** Soft HOLD invent Stories / code / CDK / spend / provision / deploy until harden live QA PASS after app image bake then separate unlock. Soft HOLD invent password. Soft HOLD invent AWS account IDs. Platform admin hosts OUT. Human-user list OUT. Participant UI as admin OUT. Multiple admin humans OUT. Email OTP fallback OUT. WAF CAPTCHA / reCAPTCHA OUT. App Runner OUT. Inbound bot connector after. Settlement OUT. SSO/IdP as delivered OUT. PoC **$0**. Cost/critical → COO → CEO. | §4 OUT/HOLD; §3.10; header Locks |
| 14 | Traceability + handshake Soft HOLD SoR | **MET.** Soft HOLD SoR cites Product (1:27 + 1:30) + Spec draft v2.1 input + CFO Soft HOLD cost (Finance QA PASS) + Option A §3a/§3b + mechanism picks §§3.3–3.9. Architecture QA must **not** formal PASS until Security QA confirms via Soft HOLD SoR `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** — do not invent points-review Soft HOLD SoR. Prior pre-expansion Soft HOLD SoR qa-confirm **superseded**. Soft HOLD Spec invent / Soft HOLD Spec Security final until Soft HOLD SoR CLEAR. Soft HOLD build. Not a build unlock. | Sources; this §6; §5 |

---

## 7. Done-list → Architecture QA

- [x] Options cover CA brief + Product scope (Option A pick; Step 3 reconciled)
- [x] Host locked to `admin.core.dealoware.com`; principal named **CoreOwner**; single superadmin `io@aiknowhow.com`
- [x] Option A §3a/§3b dual wall cited — no parallel admin ACL
- [x] Auth amended: TOTP required; no email OTP fallback; recovery codes hashed; Turnstile only; SES behind mail interface; lockout; session; Soft HOLD invent password (§3.3)
- [x] Lists/search/paging concrete (§3.9); Withdrawn mapped; offset/limit 50/200
- [x] Confirm-delete, audit (incl. login/reset), soft-delete/cascades stated
- [x] CFO Soft HOLD cost note cited
- [x] Explicit IN / OUT / HOLD; Soft HOLD Spec invent noted
- [x] Review moments table present for CA → CPM (`gate:sa-arch-review`)
- [x] §6 Security answers points 1–14 **MET** vs ISSUED v2 checklist; Soft HOLD Arch QA until Security Soft HOLD SoR CLEAR + qa-confirm Soft HOLD SoR only
- [x] No CDK / AWS account / bot-platform internals / password values; PoC $0; Soft HOLD Stories/code
- [ ] Architecture QA review (formal PASS after Security CLEAR path)
- [ ] Confirm to Chief only after Soft HOLD SoR CLEAR + Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only + Architecture QA PASS

---

## 8. CEO questions / open risks left HOLD

| Item | Note |
|------|------|
| None blocking this Soft HOLD SoR amend | CEO deltas bound; Spec draft v2.1 used as input |
| Soft HOLD | Spec invent until Soft HOLD SoR CLEAR; Spec reconciles if Soft HOLD SoR differs |
| Soft HOLD | Build/deploy/spend until harden live QA PASS after app image bake |
| Soft HOLD | Architecture QA formal PASS until Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only (v2 pts 1–14 MET) |
| Soft HOLD | Invent password; invent AWS account IDs; invent spend (CFO Soft HOLD cost note estimate only; Finance QA **PASS** via `finance-out/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md`) |
| Deferred (not blocking) | Hard delete follow-on; audit purge/archival; cursor paging; SSO as delivered |

---

## 9. Disposition

Soft HOLD SoR §6 rebound to SA Security checklist **ISSUED v2** points 1–14 **MET**. Soft HOLD Spec invent until Soft HOLD SoR CLEAR. Soft HOLD Architecture QA formal PASS until Security Soft HOLD SoR CLEAR + Security QA qa-confirm Soft HOLD SoR only (**no** invent points-review Soft HOLD SoR; prior pre-expansion Soft HOLD SoR qa-confirm superseded). Design Spec only. Soft HOLD invent Stories, code, CDK, spend, provision, deploy, password values, and AWS account IDs. Soft HOLD build. PoC **$0**. Principal **CoreOwner** (`io@aiknowhow.com`) at `admin.core.dealoware.com`. Ready for Senior Security score.
