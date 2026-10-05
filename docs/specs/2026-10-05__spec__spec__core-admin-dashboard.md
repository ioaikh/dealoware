# Spec — Core admin dashboard

| Field | Value |
|-------|--------|
| Written by | Dealoware Senior Spec |
| Date | 2026-10-05 |
| Revision | **v2** / **v2.1** as before. **v2.2** — reconcile to amended SA Soft HOLD SoR `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` (adopt SA picks where Spec left open; locked Spec values unchanged). KB only; no PR. |
| Status | Design Spec (KB only). **v2.2, Chief Spec CLEAR.** Handed to Spec QA. Spec Security checklist **v2 points 1–15** bound (§13). Spec QA must **not** PASS until Security QA confirms v2. SA Soft HOLD SoR formally cleared (CA grounding PASS). |
| Brief | Chief Spec CLEAR on v2.2 (hashed-IP decision bound). Spec QA + Spec Security QA v2. |
| Host | `admin.core.dealoware.com` (Core admin). Not `admin.platform.dealoware.com`. |
| Binding Product | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` — **13:31 (includes 1:27pm patch + 1:30pm CEO final)** — **binding authority**. Earlier 13:06 text is superseded. CPM/BM relays are history only; cite the note. |
| Binding SA Soft HOLD SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` (amended) — adopt SA mechanism picks where Spec left open; cite SA sections. Locked Spec values unchanged unless Chief Spec resolves a conflict. |
| App target | .NET 10 (retarget in flight on public PR #1 from commit `1c9d10b`). This Spec does not implement the retarget. |
| Build / deploy | Held until harden live QA PASS after the app image bake. Opening or revising Spec is not a build unlock. Soft HOLD invent AWS: name dependencies + cost only; no account IDs, no provision. |
| PoC | $0. No Stories, no code, no CDK, no spend, no provision from this Spec. **Never write a password value** in Spec, docs, or chat. |

---

## Sources (cite only)

| Source | Path / role |
|--------|-------------|
| Product Core admin scope | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` — **binding authority: 13:31 (includes 1:27pm patch + 1:30pm CEO final)**. IN 5–8; must-cover (Turnstile only; SES); AC2–AC4, AC6–AC7; Product OUT pack. |
| CEO expansions (history) | CEO OK 1:06pm edit/delete; 1:27pm lists/search/superadmin; **1:30pm** Turnstile-only + SES — carried in Product **13:31** note; **not** separate authority. |
| Spec Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` — **v2 points 1–15** bound in §13. |
| SA Soft HOLD SoR (amended) | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` — binding for mechanism picks Spec left open (§3.3–§3.9). |
| SA Security qa-confirm | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (SA checklist v2 pts 1–14) — SA Security path evidence; not Spec Security PASS. |
| Arch QA interim content PASS | `verification/2026-10-05__sa__verification__core-admin-dashboard-ceo-amend-review.md` — interim content PASS. |
| CA design grounding PASS | `verification/2026-10-05__ca__verification__core-admin-dashboard-design-grounding-pass.md` — Chief Architect formal CLEAR on SA Soft HOLD SoR. |
| CFO Soft HOLD cost | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` (Finance QA PASS) — Turnstile $0/mo; SES under $0.01/mo assumed; Soft HOLD invent spend. |
| Prior Step 3 admin architecture (reconcile) | `architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` — keep Option A shape where it fits Core; drop O1 human users and platform-admin host |
| Prior Step 3 admin Spec (reconcile) | `specs/2026-10-02__spec__spec__platform-owner-admin-dashboard.md` — same reconcile rules |
| Core negotiation/offer statuses (cite) | `specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md` and `product/2026-10-03__product__note__two-part-platform-requirements-delta.md` — Negotiation: Open / Closed / Expired; Offer: Open / Accepted / Declined / Superseded / Cancelled |
| Two-part platform delta | `product/2026-10-03__product__note__two-part-platform-requirements-delta.md` — Core owner counts Participants / negotiations / offers; human users are not listed inside Core |
| CEO host standing rule | `admin.core.dealoware.com` is Core admin; platform hosts are not Core. Core stays public and may run outside AWS. No CDK details, bot-platform internals, or AWS account information in this Spec. |
| FieldPolicy dual wall | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3a/§3b — cite; do not rewrite; no parallel admin access list |

Do not cite old public-repo issue or PR numbers from the pre-recreate history as live items on the current public repo.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 1 | Host | Core admin is only at `admin.core.dealoware.com`. Platform admin and platform app hosts are out of this Spec. |
| 2 | Role | **One** system superadmin (`io@aiknowhow.com`) = principal **`CoreOwner`** (SA §3.2). Not the Participant UI. Fields follow FieldPolicy for that role. **No** operator list / multi-admin ACL. |
| 3 | Entity surface | List, view, edit, and delete Participants (not human users), Artifacts, negotiations, and offers — with all-status lists, sort/filter/paging, and name search (§4, §4.5–§4.6). |
| 4 | Stats | Overall counts from Core data (§6): Participants, open negotiations, offers, accepts, declines. No charts product. No warehouse. |
| 5 | Must-cover | Superadmin sign-in (§8 — locked, not open to SA mechanism pick); confirm before every delete (§9); audit log including login/reset/2FA events (§10); delete semantics soft vs hard + cascades (§11); lists/search/paging (§4.5–§4.6). |
| 6 | Sequence | Inbound bot connector comes after this. SA comes after Spec QA on this track. |
| 7 | Hold | Build, deploy, Stories, code, CDK, spend, provision held. Soft HOLD invent AWS. PoC $0. Spec does not implement .NET 10 retarget. **Password values forbidden** in Spec/docs/chat. |

---

## 1. Purpose

Specify a Core owner admin dashboard at `admin.core.dealoware.com` so the single system superadmin can list (all statuses, sort/filter/page, search by name), view, edit, and delete Core data (Participants, Artifacts, negotiations, offers) and see overall Core counts — with locked secure sign-in (§8).

This is design Spec only. It does not unlock build or deploy. It does not invent Stories, code, CDK, spend, AWS account details, or password values. Binding Product: `product/2026-10-05__product__note__core-admin-dashboard-scope.md` — **13:31 (includes 1:27pm patch + 1:30pm CEO final)**.

---

## 2. Reconcile vs prior Step 3 admin

Prior Step 3 named a platform-owner admin with human registered users (O1), Artifacts (O2), negotiations/offers (O3), and a generic platform-owner surface.

| Prior Step 3 item | This Core admin Spec |
|-------------------|----------------------|
| O1 registered human users | **Drop.** Human users are not listed on Core. |
| O2 Artifacts | **Keep and expand.** List, view, edit, delete + name search. |
| O3 negotiations / offers | **Keep and expand.** List (all statuses), view, edit, delete + sort/filter/paging + name search. |
| Participants | **IN** (not human users). List, view, edit, delete + name search. |
| Overall stats | **IN** as Core counts (see §6). |
| Platform-admin / platform-owner host | **Replace.** Host is `admin.core.dealoware.com` only. |
| Option A: same app host, separate admin route, distinct owner principal, FieldPolicy dual wall, no parallel admin ACL | **Keep** as the working Spec shape; SA confirms after Spec QA. |
| Participant UI as admin | **Out.** |
| SSO / IdP as delivered | **Out.** Required path is email + password + TOTP (§8). |

---

## 3. Host and role

| Concern | Spec requirement |
|---------|------------------|
| Host | Admin UI and admin API for this Spec are served only as Core admin at `admin.core.dealoware.com`. |
| Not this Spec | `admin.platform.dealoware.com`, `platform.dealoware.com`, and their APIs. |
| Role | **One** system superadmin. Login email: `io@aiknowhow.com`. Maps to Core owner role. Separate from Participant principal and Participant UI. |
| Operator list | **None.** No multi-human admin ACL. Multiple admins are OUT (§12). |
| Fields | Visibility and mutation follow FieldPolicy for the Core owner role (Option A §3a/§3b dual wall). No parallel admin access list that drifts from Domain FieldPolicy. |
| Privacy | Core stays public. This Spec must not contain CDK details, bot-platform internals, AWS account information, or **any password value**. |

**Principal name (SA pick adopted):** **`CoreOwner`** (SA §3.2). Product meaning unchanged: single system superadmin of Core, not platform human-user admin.

---

## 4. Entities — list, view, edit, delete

For each of the four entity types below, the Core owner can list, open a detail view, edit allowed fields, and delete (subject to confirm §9, audit §10, and delete semantics §11). All reads and writes are fail-closed through FieldPolicy for the Core owner role. Lists, filters, paging, and name search follow §4.5–§4.6.

### 4.1 Participants

- List and view **Participants**, not human users.
- **Search by name** per Product: Participant display name / identifier shown in Core (§4.6).
- Edit Participant fields that FieldPolicy allows the Core owner to change.
- Delete a Participant only after confirm; cascades per §11.

### 4.2 Artifacts

- List, view, edit, and delete Artifacts on Core.
- **Search by name** per Product: Artifact name / title / subject string (§4.6).
- FieldPolicy applies; no parallel Artifact admin ACL.

### 4.3 Negotiations

- List, view, edit, and delete negotiations on Core.
- Lists include **all statuses** (§4.5), including soft-deleted via explicit toggle (§11).
- **Search by name** per Product: composite of artifact/subject name **and** participant names on that negotiation (§4.6).
- Open negotiations are in scope for edit and delete; delete behavior for an open negotiation is defined in §11.

### 4.4 Offers

- List, view, edit, and delete offers on Core.
- Lists include **all statuses** (§4.5), including soft-deleted via explicit toggle (§11).
- **Search by name** per Product: composite of artifact/subject name, offering participant name, and related negotiation id (§4.6). Negotiation id remains separately filterable (§4.5).
- Accept and decline outcomes remain visible in stats (§6); this Spec does not add settlement or checkout.

### 4.5 Lists — all statuses, sort, filter, paging (AC4)

**Statuses on negotiation and offer lists**

Product requires every status on negotiation and offer lists, including soft-deleted via an explicit toggle. Spec names statuses as follows (do **not** invent beyond Product + cited Core sources):

| Source | Statuses |
|--------|----------|
| Product IN 5 (required list surface) | open, accepted, declined, expired, withdrawn, soft-deleted (via toggle) |
| Core PoC Spec / two-part delta (additional cited Core statuses) | Negotiation: **Closed**. Offer: **Superseded**, **Cancelled**. |

| Spec rule | Lock |
|-----------|------|
| Default list | Show non–soft-deleted rows across all non-deleted statuses above. |
| Soft-deleted rows | Available only through an **explicit** “include soft-deleted” (or equivalent) toggle — not hidden permanently from admin, and not mixed into the default list without the toggle. Soft-delete marker **`DeletedAt`** (SA §3.6). Ties to §11. |
| Soft-deleted toggle visibility | Toggle is **visible only to the superadmin** / **CoreOwner** (SA §3.5–§3.9). |
| Soft-deleted toggle audit | Using the toggle is a **read**. It is **not** written to the audit log (§10; SA §3.5). |
| **Withdrawn** | Product IN 5 name. **SA pick adopted (SA §3.9):** `Withdrawn` is a **first-class Core status enum value** on the negotiation/offer list surface (not a UI-only label; not mapped to `Cancelled`). |
| Other statuses | Core PoC cites also Negotiation **Closed**; Offer **Superseded**, **Cancelled** — keep on the list surface. Do not invent beyond Product + cited Core + SA Soft HOLD SoR. |

**Sort and filter (server-side)**

Sort and filter on all of:

- status
- created / updated
- participant
- artifact
- value / price
- negotiation id

**Paging**

| Rule | Spec lock |
|------|-----------|
| Style | **Server-side** paging required. |
| Default page size | **50** rows (locked; matches SA §3.9). |
| Maximum page size | **200** rows (locked; matches SA §3.9). |
| Paging token | **Offset/limit** (SA pick adopted, SA §3.9). Cursor paging deferred (SA HOLD). |
| Combine | Paging applies to the filtered + searched result set (§4.6). |
| No client-side full dump | Paging and search run on the **server only**. **No** client-side paging that loads whole tables into the browser. |
| Parameterized inputs | Sort, filter, and search inputs are **parameterized** (no string-concat injection into queries). |
| FieldPolicy on results | List/search result payloads **never** include fields FieldPolicy denies. |

### 4.6 Name search (AC2, AC3, AC4)

Search is available on **every** table. Product definitions of “name” — word for word:

| Table | Searchable “name” (Product) |
|-------|----------------------------|
| Participants | the Participant's display name / identifier shown in Core |
| Artifacts | the Artifact's name / title / subject string |
| Negotiations | searchable composite of artifact/subject name **and** participant names on that negotiation |
| Offers | searchable composite of artifact/subject name, offering participant name, and related negotiation id (id remains filterable separately) |

| Rule | Spec lock |
|------|-----------|
| Matching | **Case-insensitive**. |
| Combine | Search **combines** with status/sort/filter (§4.5) and with server-side paging. Empty search = no name constraint. |
| Server-only | Search runs on the **server only** with paging (§4.5). No client-side full-table load. |
| Parameterized | Search/filter inputs are **parameterized** — no injection. |
| Fail-closed | Search results still bind FieldPolicy; denied fields are **never** included in search hit payloads. |
| Match shape | **Case-insensitive contains** (SA pick adopted, SA §3.9). Indexing strategy remains an implementation detail under that rule. |

---

## 5. Edit rules

| Rule | Spec requirement |
|------|------------------|
| Allowed fields | Only fields FieldPolicy allows the Core owner to write. Spec does not invent a second permission matrix. |
| Fail-closed | Missing Core owner role → no edit. Denied FieldClass → no write and no dump into UI error payloads beyond a safe deny. |
| Audit | Every successful admin edit is written to the audit log (§10) with **before and after** values, limited to fields FieldPolicy allows. |
| No secrets | **No** password or other secret values in edit forms, error payloads, or audit snapshots. |
| Concurrency (SA pick adopted) | Optimistic concurrency via row `Version` (or `UpdatedAt` ETag). Stale write → **409 Conflict** with safe message; no silent overwrite (SA §3.8). |
| Editable field sets | Within FieldPolicy only — see SA Soft HOLD SoR §3.7 for CoreOwner edit surface (Spec does not invent a second matrix). |
| SA residual | Validation error copy in admin UI (field-level safe messages; denied FieldClass values not echoed). |

---

## 6. Overall stats

| Stat | Source |
|------|--------|
| Participants count | Core data |
| Open negotiations count | Core data |
| Offers count | Core data |
| Accepts count | Core data |
| Declines count | Core data |

No charts product. No warehouse. Counts are queried from Core. **Stats predicates (SA pick adopted, SA §3.8):** all five counts exclude soft-deleted rows (`DeletedAt IS NULL`); open negotiations = `Status = Open` and not soft-deleted. Later cost views, CloudWatch period stats, and closed-deal analytics stay out unless Product expands this scope.

---

## 7. Architecture shape (for SA after Spec QA)

Working Spec shape carried from prior Step 3 Option A where it still fits:

- Same Core application host as the Negotiation Core modular monolith (not a separate admin mesh).
- Separate Core admin route surface (admin API + thin admin UI) at `admin.core.dealoware.com`.
- Distinct **`CoreOwner`** principal / role claim bound to the single system superadmin (SA §3.2).
- FieldPolicy dual wall; no parallel admin ACL; no operator list.
- Reject: reuse Participant UI as admin; SSO/IdP as delivered for this slice; separate admin microservice; platform-admin host; multiple admin humans.

**SA to confirm** the Option A carry-forward and any Core-specific adjustments after Spec QA. This Spec does not lock host provision, CDK, or spend.

---

## 8. Admin auth and sign-in (must-cover) — locked

Product **13:31 (includes 1:27pm patch + 1:30pm CEO final)** locks identity and security controls. This section is **not** left open for SA to pick an alternate mechanism. SA implements the locked design; SA confirms concrete libraries and storage shapes only.

### 8.1 Identity

| Requirement | Spec lock |
|-------------|-----------|
| Who | **One** system superadmin. Login email: `io@aiknowhow.com`. Email + password. |
| Operator list | **None.** No multi-admin ACL. |
| Role binding | Successful sign-in binds the verified superadmin identity to the Core owner principal/claim FieldPolicy uses. |
| Fail-closed | If the signed-in principal is not Core owner / not this superadmin, admin routes deny (no entity list, no edit, no delete, no stats beyond a safe unauthorized response). |
| Password values | **Forbidden** in code, docs, Spec text, chat, or commit history. Spec never states a password. |

### 8.2 Bootstrap (first password + TOTP)

| Requirement | Spec lock |
|-------------|-----------|
| Bootstrap | **Single-use** bootstrap link with a **time limit**, delivered **out-of-band** (not embedded in Spec/docs/chat). |
| First password | Superadmin sets the password **only** through that link. No seeded password string anywhere. |
| TOTP enrollment | **Required before the first session.** First interactive admin session cannot start until TOTP is enrolled. |
| Link lifetime | Bootstrap link expires after **24 hours** and is single-use. **SA may tighten** but not lengthen without Product change. |

### 8.3 TOTP 2FA (required)

| Requirement | Spec lock |
|-------------|-----------|
| TOTP | **Required** authenticator-app TOTP for every login after enrollment. |
| Email OTP fallback | **NOT allowed.** Chief Spec decision: reset already depends on email; email OTP as 2FA fallback would let one compromised mailbox take over the account. |
| Recovery | **One-time recovery codes** shown **once** at TOTP enrollment, stored **hashed**. Each code is single-use. Email OTP is not a recovery path. |
| SA to confirm | TOTP library, digits/period, recovery-code count (recommend **10**), and hash algorithm for recovery codes. |

### 8.4 Password reset

| Requirement | Spec lock |
|-------------|-----------|
| Factors | Reset requires **both** (1) email reset link (**single-use**, **short time limit**) **and** (2) a valid **2FA code** (TOTP or unused recovery code). |
| Link lifetime | Reset link expires after **1 hour** and is single-use. |
| No password in channel | Reset email carries only the link; never a password value. |

### 8.5 CAPTCHA

| Requirement | Spec lock |
|-------------|-----------|
| Where | **Required** on login and password-reset pages (and bootstrap password-set page). |
| Provider | **Cloudflare Turnstile only** (CEO final call 2026-10-05 1:30pm ET). Cost **$0** now (free). |
| Alternative | **None.** CEO final 2026-10-05 1:30pm: Turnstile only. |
| Accounts | Do **not** invent Turnstile account IDs or site keys in this Spec. Flag as dependency; cost is **$0**. |
| Abuse controls | App-level lockout and rate limits (§8.6) stay **required** either way. |

### 8.6 Lockout and rate limiting

| Control | Spec lock (concrete) |
|---------|----------------------|
| Per-account failed login | After **5** failed password or 2FA attempts for the account within **15 minutes**, lock that account for **30 minutes** (or until successful unlock via completed password-reset flow). |
| Per-IP failed login | After **20** failed login attempts from one IP within **15 minutes**, throttle that IP for **30 minutes** (HTTP 429 / equivalent). |
| Reset endpoints | Same per-IP window applies to reset-request submissions (**20** / **15 min** → **30 min** throttle). |
| Raw IP retention (Chief Spec decision) | Raw client IP may live **only** in **short-lived rate-limit counters**, which expire when the **15-** and **30-minute** windows end. Raw IP is **never** written to the audit log, other logs, or metrics. |
| SA to confirm | Exact counters (sliding vs fixed window) and whether CAPTCHA failures count toward the same budgets. |

### 8.7 Session lifetime

| Timer | Spec lock |
|-------|-----------|
| Idle timeout | **30 minutes** without activity → session ends; sign-in required again. |
| Absolute timeout | **8 hours** from session start → session ends even if active; sign-in required again. |
| After expiry | No admin action until full sign-in (password + TOTP). |
| Session store (SA pick adopted) | **Server-side session** row in Core Postgres, referenced by an **HttpOnly Secure SameSite=Strict** session cookie on `admin.core.dealoware.com` (SA §3.3). |
| Idle renewal (SA pick adopted) | Sliding renewal on authenticated admin requests within the idle window; absolute max still **8 hours** from login (SA §3.3). |

### 8.8 Auth events in audit log

Every one of the following is written to the audit log (§10):

- Login **success**
- Login **failure** (include reason class: bad password / bad 2FA / locked / rate-limited — no secret material)
- Password **reset request**
- Password **reset completion**
- TOTP **enrollment**
- TOTP **change** / re-enrollment
- Recovery code **use** (success)

### 8.9 Mail delivery dependency

| Item | Spec lock |
|------|-----------|
| Need | Bootstrap and password-reset email delivery. |
| Chosen provider | **Amazon SES** (CEO approved 2026-10-05 1:29pm ET via CPM) for reset and bootstrap emails. |
| Abstraction | Deliver mail through a **mail interface** (port/adapter). Core application code must **not** depend on AWS SDKs or SES types directly, so Core may run somewhere other than AWS. |
| Flag | **Dependency + cost** only. Soft HOLD invent AWS: **no** AWS account IDs, **no** region, **no** SES identity / from-address details, **no** provisioning, **no** spend from this Spec. |
| PoC | Remains **$0** until separate spend unlock. |
| CFO Soft HOLD cost (cite) | `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` — Turnstile **$0/mo**; SES **under $0.01/mo** at assumed 5 emails/mo; Finance QA PASS; Soft HOLD invent spend (not spend approval). |

### 8.10 Out of this auth design

- SSO / IdP as delivered
- Multiple admin humans / operator ACL
- Email OTP as 2FA or fallback
- Password values in Spec, docs, chat, code, or commit history
- CAPTCHA providers other than Cloudflare Turnstile (see §12 OUT)
- Direct Core dependency on AWS SES SDK (must use mail interface)

---

## 9. Confirm before delete (must-cover)

| Requirement | Spec lock |
|-------------|-----------|
| Scope | Every delete of a Participant, Artifact, negotiation, or offer. |
| Behavior | Delete does not run until the Core owner gives an explicit confirm in the admin UI (or equivalent admin API confirm step that cannot be skipped by a single blind request in the UI flow). |
| Content | Confirm must name the entity type and enough identity for the owner to know what will be deleted (for example display name / id). |
| Cascades | Confirm copy must state the cascade summary from §11 when cascades apply (especially open negotiations and related offers). |
| Cancel | Dismiss / cancel leaves data unchanged and writes no delete audit row. |

**SA picks adopted (SA §3.4):**
- Admin UI: **modal confirm** on entity detail (or list row action).
- API-only: **required confirm token** — `POST …/delete-intent` returns `confirmToken` + identity + cascade summary; delete must present token. Blind DELETE rejected. Token single-use, **5 minutes**, bound to actor + entity + intended cascade set.


---

## 10. Audit log (must-cover)

Every successful Core admin **edit** and **delete** is recorded, **plus** the sign-in / reset / 2FA events in §8.8.

| Field | Required (entity edit/delete) |
|-------|-------------------------------|
| Who | Core owner identity that performed the action |
| When | Timestamp (UTC stored; display in admin in a clear timezone) |
| What entity | Entity type + entity id |
| Action | Edit or delete |
| Before | Snapshot of FieldPolicy-allowed fields before the change (delete: state at delete time) |
| After | Snapshot of FieldPolicy-allowed fields after edit; for delete, after-state shows deleted/soft-deleted marker per §11 |

| Field | Required (auth events §8.8) |
|-------|----------------------------|
| Who | Login email / subject when known; else anonymous + IP class |
| When | Timestamp (UTC) |
| Action | login_success / login_failure / reset_request / reset_complete / totp_enroll / totp_change / recovery_code_use |
| Context | Outcome, failure reason class, and **IP as keyed HMAC-SHA256 only** (see IP rule below) — **never** password, TOTP secret, recovery code plaintext, or raw IP. |

| Rule | Spec lock |
|------|-----------|
| Coverage | Admin edits and deletes for this Spec’s four entity types (plus cascade rows SA ties to the same action) **and** auth events in §8.8. |
| Edit snapshots | Every successful **edit** records before and after for FieldPolicy-allowed fields only. |
| No secrets | Audit snapshots, edit forms, and error payloads must **not** contain passwords, TOTP secrets, recovery codes, or other secrets. |
| Soft-deleted toggle | Using the soft-deleted list toggle (§4.5) is a **read** and is **not** written to the audit log. |
| Auth audit IP (Chief Spec decision) | Auth audit stores IP **only** as a keyed **HMAC-SHA256** of the client IP using a **server-side secret key**, plus the failure reason class. The key is a **deployment secret** — it never appears in docs or this Spec, and it can be rotated. Raw IP is **not** written to the audit log. A plain **unkeyed** hash is **not allowed** (IPv4 space is small enough to brute-force). SA should mirror this in a later Soft HOLD SoR touch. |
| Integrity | The audit log **cannot** be edited or deleted from the Core admin UI or admin API. |
| Access | Core owner may **read** the audit log in admin. No parallel ACL. FieldPolicy applies to any sensitive fields inside snapshots. |
| Fail-closed / transaction pairing (SA pick adopted) | **Same database transaction** as the mutation: insert audit row(s) + apply edit/soft-delete (+ cascades). If any audit insert fails, **roll back** the whole mutation (SA §3.5). Auth-state changes that require audit (reset completion, TOTP enrollment) follow the same fail-closed rule. |
| Storage (SA pick adopted) | **Append-only audit table** in Core Postgres; insert-only; no update/delete API or UI path (SA §3.5). |
| Large snapshots (SA pick adopted) | Values larger than **4 KiB** truncated; store truncated text + content length + content hash (SA §3.5). |
| Retention (SA pick adopted) | **Retain indefinitely** for this first Core admin slice; purge/archival later (SA §3.5). |
| Cascade rows (SA pick adopted) | Each cascaded soft-delete writes its own audit row, tied to the same admin action / correlation id (SA §3.5). |

---

## 11. Delete semantics (must-cover)

### 11.1 Soft vs hard

Product leaves soft vs hard to Spec and SA. This Spec’s recommendation:

| Choice | Spec recommendation |
|--------|---------------------|
| **Default** | **Soft delete** for Participants, Artifacts, negotiations, and offers. |
| Marker (SA pick adopted) | **`DeletedAt`** (UTC nullable). Soft-deleted when `DeletedAt IS NOT NULL` (SA §3.6). |
| Reason | Preserves audit and support recovery; keeps referential history; avoids irreversible wipe of Core history on first admin slice. |
| Hard delete (SA pick adopted) | **Deferred entirely.** Not available on Core admin UI/API in this slice (SA §3.6). |
| List tie-in | Soft-deleted rows remain listable via the **explicit soft-deleted toggle** in §4.5 (**CoreOwner / superadmin-only**; toggle use is a read and is **not** audited). Default lists exclude them (`DeletedAt IS NULL`). |

### 11.2 Cascades (all four directions)

Product requires Spec/SA to define cascades among **offers, negotiations, Artifacts, and Participants**. Spec recommendations below; SA confirms after Spec QA.

| When deleting → effect on | Offers | Negotiations | Artifacts | Participants |
|---------------------------|--------|--------------|-----------|--------------|
| **Offer** | Soft-delete that offer. | Parent negotiation stays unless separately deleted. | No change. | No change. |
| **Negotiation** | Soft-delete **all child offers** of that negotiation. | Soft-delete the negotiation. If **open**, confirm must warn it will leave active lists with its offers. | No automatic Artifact delete. | No change to Participant records. |
| **Artifact** | No direct offer wipe. | **Block** while any non-deleted negotiation references it; admin error lists those negotiation ids (SA §3.6). Soft-delete Artifact only when no such reference remains. | Soft-delete the Artifact when allowed. | No change. |
| **Participant** | Soft-delete offers on negotiations cascaded below. | Soft-delete negotiations that Participant owns or participates in **and** those negotiations’ offers; confirm states negotiation + offer counts (SA §3.6). Open negotiations follow §11.3. | **Do not** auto-delete Artifacts (SA §3.6). | Soft-delete the Participant. Human users are out of Core admin (no human-user delete). |

Shared rules for every cascade path:

- Confirm before delete must summarize cascade counts (offers / negotiations / Artifacts / Participants affected).
- Each cascaded soft-delete is audited (§10).
- Stats after commit exclude soft-deleted rows (SA confirms exact count predicates).
- Settlement / escrow side effects are out.

### 11.3 Open negotiation

Deleting an **open** negotiation (directly or via Participant cascade) must:

1. Require confirm that names it as open and states child-offer count.
2. Soft-delete the negotiation and its offers.
3. Audit the delete and cascade deletes (SA confirms transaction design).
4. Remove it from “open negotiations” stats after commit.
5. Not delete Artifacts or other Participants unless a separate cascade rule in §11.2 applies and was confirmed.

---

## 12. Explicit IN / OUT

### IN

- Core admin at `admin.core.dealoware.com`
- List, view, edit, delete Participants (not human users) + name search (§4.6)
- List, view, edit, delete Artifacts + name search (§4.6)
- List, view, edit, delete negotiations and offers — **all statuses**, sort/filter, server-side paging (§4.5), name search (§4.6)
- Overall Core stats: Participants, open negotiations, offers, accepts, declines
- **One** system superadmin `io@aiknowhow.com` + FieldPolicy; no parallel admin ACL / no operator list
- Locked admin auth/sign-in (§8): bootstrap, TOTP required, no email OTP fallback, reset = email link + 2FA, **Cloudflare Turnstile only** ($0), lockout/rate limit, session timers, auth audit events
- Confirm before every delete (§9)
- Audit log of admin edits/deletes **and** login/reset/2FA events; log not editable/deletable from admin (§10)
- Delete semantics: soft-delete default + cascades + soft-deleted list toggle (§11, §4.5)
- Mail delivery: **Amazon SES** chosen provider behind a **mail interface** (Core not AWS-coupled); dependency + cost flag only; no account/region/identity/provision details
- App target awareness: .NET 10 (Spec does not implement retarget)

### OUT

- Human-user list on Core
- Platform admin (`admin.platform.dealoware.com`) and platform human-user admin
- **Multiple admin humans / operator ACL**
- Inbound bot connector (comes after)
- Participant UI reused as admin
- Settlement / escrow / checkout
- MotorMarket / DC4
- Metering UI / AI-token meters on Core
- Cost views / warehouse / charts product
- **SSO / IdP as delivered**
- **Email OTP as 2FA or fallback**
- **Password values in Spec, docs, chat, code, or commit history** (**forbidden**)
- Stories, code, CDK, spend, provision, deploy (held until harden live QA PASS after app image bake, then separate unlock)
- **AWS WAF CAPTCHA and reCAPTCHA: OUT**; any other CAPTCHA provider OUT — **Turnstile only** (Product OUT; CEO final 1:30pm)
- Invent AWS account information, region, SES identity, CDK details, bot-platform internals, or Turnstile/mail vendor account IDs in this Spec

---

## 13. Security Spec checklist binding (v2 points 1–15)

**Binding checklist:** `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` (**v2**)  
**Binding Product:** `product/2026-10-05__product__note__core-admin-dashboard-scope.md` — **13:31 (includes 1:27pm patch + 1:30pm CEO final)**  
**Status:** **v2.2, Chief Spec CLEAR.** Spec checklist v2 pts 1–15 bound. Spec QA Soft HOLD PASS until Security QA confirms v2.  
**Rule:** Spec QA must **not** PASS until Security QA confirms **v2** points 1–15. Prior Spec Security PASS (v1 1–10) is **superseded**. SA after Spec QA. No build. PoC **$0**. No password values.

| # | Security point (checklist v2) | Spec answer | Spec section cites |
|---|-------------------------------|-------------|--------------------|
| 1 | Host + single superadmin role | **MET.** Admin only at `admin.core.dealoware.com`. One system superadmin `io@aiknowhow.com`. Distinct from Participant UI and `admin.platform`. Human-user list OUT. Multiple admin humans / operator ACL OUT. | Locked #1–#2; §3; §12 OUT |
| 2 | Email + password; secure one-time bootstrap | **MET.** Email + password for that superadmin. Single-use timed out-of-band bootstrap link; password set only via link. **No password** in code/docs/Spec/chat/commit history. SSO/IdP as delivered OUT. | §8.1; §8.2; §12 OUT |
| 3 | TOTP 2FA required; no email OTP fallback | **MET.** TOTP required before first session and on every login after. Email OTP **NOT allowed**. Recovery = one-time codes shown once at enrollment, stored **hashed**. | §8.3; §8.10 |
| 4 | Password reset = email link + 2FA | **MET.** Reset requires single-use timed email link **and** 2FA (TOTP or unused recovery code). No reset that skips 2FA. | §8.4 |
| 5 | CAPTCHA (Turnstile only) + lockout / rate limit + session | **MET.** CAPTCHA on login, reset, and bootstrap password-set pages. **Cloudflare Turnstile only** ($0). AWS WAF CAPTCHA / any other CAPTCHA OUT. App lockout 5/15min→30min per account; 20/15min→30min per IP. Idle session **30 min**; absolute **8 h**; after expiry full re-auth (password + TOTP). | §8.5; §8.6; §8.7; §12 OUT |
| 6 | Login / reset audit + edit/delete audit | **MET.** Audit covers login success/failure, reset request/completion, TOTP enroll/change, recovery-code use, and every successful admin edit/delete. Auth audit IP = keyed **HMAC-SHA256** only (Chief Spec decision); raw IP only in short-lived rate-limit counters (§8.6). Soft-deleted toggle use not audited. Mutation audit fail → roll back. | §8.6; §8.8; §4.5; §10 |
| 7 | Fail-closed deny for non-superadmin | **MET.** Missing/invalid/expired session, missing TOTP step, or principal not the single superadmin → deny all admin routes. Denied FieldClass → no write and no dump into UI/errors. | §3; §5; §8.1 |
| 8 | FieldPolicy only; no parallel multi-user admin ACL | **MET.** All reads/writes bind Domain FieldPolicy for Core owner / superadmin (Option A §3a/§3b). Parallel multi-operator ACL rejected. | Locked #2; §3; §4; §5; §7 |
| 9 | Confirm before every delete | **MET.** Delete of Participant, Artifact, negotiation, or offer requires explicit confirm naming type + identity + cascade summary when cascades apply. Cancel → unchanged + no delete audit row. | §9; §11.2 |
| 10 | Soft-delete + cascades; soft-deleted toggle (superadmin-only) | **MET.** Soft-delete marker **`DeletedAt`**; hard delete deferred entirely. Cascades per §11 / SA §3.6. Soft-deleted toggle **CoreOwner-only**; toggle use is a **read** and is **not** audited. Stats use `DeletedAt IS NULL`. | §4.5; §6; §11; SA §3.6–§3.8 |
| 11 | All-status lists + name search + paging (safe) | **MET.** All statuses incl. first-class **`Withdrawn`** + soft-deleted toggle (CoreOwner-only, not audited); sort/filter; **server-side offset/limit** paging (default 50 / max 200); name search case-insensitive contains per Product. **No** client-side full-table load. Parameterized inputs; FieldPolicy on results. | §4.5; §4.6; SA §3.9 |
| 12 | Edit write surface FieldPolicy-only | **MET.** Only FieldPolicy-allowed fields writable; every successful edit audited with before/after (FieldPolicy-allowed fields only). No second permission matrix. No password/secret dump in edit forms, errors, or audit snapshots. | §5; §10 |
| 13 | SES mail + Turnstile as dependency + cost only | **MET.** Bootstrap/reset mail uses **Amazon SES** behind a **mail interface** (dependency + cost; no account/region/identity/provision). CAPTCHA is **Turnstile only** ($0); no Turnstile account invented in Spec. | §8.5; §8.9; §12 |
| 14 | OUT / Soft HOLD pack | **MET.** Platform admin hosts; human-user list; Participant UI as admin; multiple admin humans; inbound connector (after); settlement; SSO/IdP; email OTP fallback; **AWS WAF CAPTCHA and reCAPTCHA OUT (Turnstile only)**; Stories/code/CDK/spend/provision/deploy held; AWS account IDs / CDK / bot-platform internals OUT; **password values Forbidden**. PoC $0. | Header; §8; §12 OUT |
| 15 | Traceability + re-QA handshake | **MET.** Spec cites Product **13:31 (includes 1:27pm patch + 1:30pm CEO final)** + Option A FieldPolicy dual wall + Chief Spec decisions (no email OTP; hashed recovery; Turnstile only; SES behind mail interface; soft-deleted toggle superadmin-only / not audited; app lockout/rate limits) + this **checklist v2**. Spec QA must **not** PASS until Security QA confirms v2. Soft HOLD build. Not a build unlock. | Sources; this §13; Product 13:31 |


## 14. Acceptance mapping (Product AC1–AC9)

Binding: Product note AC1–AC9 — **13:31 (includes 1:27pm patch + 1:30pm CEO final)**. AC7 is the expanded required designs. AC9 is the build/deploy + invent-AWS hold.

| ID | Product requirement | Spec coverage |
|----|---------------------|---------------|
| AC1 | Open Core admin at `admin.core.dealoware.com` (not `admin.platform`) | §3 |
| AC2 | List, open, edit, delete Participants (not human users); **search by Participant name** | §4.1, §4.6, §5, §9–§11 |
| AC3 | List, open, edit, delete Artifacts; **search by Artifact name** | §4.2, §4.6, §5, §9–§11 |
| AC4 | List, open, edit, delete negotiations and offers; **all statuses** (soft-deleted via toggle); **sort/filter** on status, created/updated, participant, artifact, value/price, negotiation id; **server-side paging**; **search by name** per Product definitions | §4.3–§4.6, §5, §9–§11 |
| AC5 | Overall stats: Participants, open negotiations, offers, accepts, declines; no charts; no warehouse | §6 |
| AC6 | Not Participant UI; FieldPolicy for Core owner; **one** system superadmin; no parallel multi-user admin ACL | §3, §5, §7, §8.1 |
| AC7 | Required designs: single superadmin `io@aiknowhow.com` email+password; secure one-time bootstrap (no password in code/docs/chat); required TOTP (email OTP **not** allowed per Spec); recovery codes; password reset via email link + 2FA; CAPTCHA on login/reset (**Cloudflare Turnstile only**, $0); mail via **SES** behind mail interface; lockout/rate limit; session lifetime; login/reset/2FA audit; confirm before delete; audit log of edits/deletes; delete semantics; all-status lists, sort/filter, paging, name search. No invented mail/CAPTCHA/AWS accounts. | §4.5–§4.6; §8; §9; §10; §11 |
| AC8 | Does not deliver inbound bot connector, platform admin, human registration, payments, or token allowance | §12 OUT |
| AC9 | Build and deploy held until harden live QA PASS after app image bake; Soft HOLD invent AWS; PoC $0 until that gate and a separate spend unlock | Header; §12 OUT |

---

## 15. Done-list → Chief Spec

- [x] Design Spec **v2 / v2.1** at `specs/2026-10-05__spec__spec__core-admin-dashboard.md` (KB only; no PR)
- [x] Binding Product cited as **13:31 (includes 1:27pm patch + 1:30pm CEO final)**
- [x] Status was v2.1 CLEAR; **v2.2** reconciles to amended SA Soft HOLD SoR (draft → Chief Spec)
- [x] Lists: all-status + soft-deleted toggle; sort/filter fields; server-side paging default **50** / max **200**; cursor vs offset → SA; tied to §11 (§4.5)
- [x] Name search: Product definitions word-for-word; case-insensitive; combines with filters + paging (§4.6)
- [x] §8 rewrite: single superadmin `io@aiknowhow.com`; bootstrap; TOTP required; email OTP **not** allowed; recovery codes; reset = link + 2FA; CAPTCHA (**Turnstile only**, $0); lockout + session timers; auth audit; **SES** behind mail interface (dependency + cost; no account/region/identity); **no password values**
- [x] v2.1: CEO 1:29pm SES behind mail interface + CEO 1:30pm Turnstile-only CAPTCHA ($0); no other CAPTCHA provider
- [x] §10 audit: + login/reset/2FA events; still non-editable/non-deletable from admin; fail-closed on audit write
- [x] §12 IN/OUT: multiple admins OUT; SSO/IdP OUT; password values forbidden
- [x] §13: Security checklist **v2 points 1–15** bound with section cites (v1 1–10 numbering removed)
- [x] Explicit: soft-deleted toggle superadmin-only + not audited (§4.5, §11); server-only paging/search + parameterized + FieldPolicy (§4.5–§4.6); edit before/after FieldPolicy-only + no secrets (§5, §10)
- [x] §12 OUT: AWS WAF CAPTCHA and reCAPTCHA OUT; any other CAPTCHA OUT — Turnstile only
- [x] §14 AC1–AC9 map updated; §15 done-list updated
- [x] Host locked; Step 3 reconcile; soft-delete + cascades; .NET 10 note unchanged; no CDK/AWS account/bot-platform internals
- [x] Handed to Spec QA; Chief Security pinged that §13 is bound to v2 1–15
- [x] **v2.2** adopted SA open picks: offset/limit; `Withdrawn` first-class; `DeletedAt` + stats predicates; same-txn audit pairing; append-only audit; confirm modal+token; CoreOwner; session cookie store; contains search; CFO Soft HOLD cost cite
- [x] Locked Spec values checked vs SA — **no conflicts** (lockout, sessions, page size, email OTP OUT, Turnstile only, SES behind mail interface, toggle CoreOwner-only + not audited)
- [x] Chief Spec decision bound: auth audit IP = keyed HMAC-SHA256; raw IP only in short-lived rate-limit counters; unkeyed hash forbidden (§8.6, §10)
- [x] Status: **v2.2, Chief Spec CLEAR**
- [x] CA grounding PASS cited; SA Soft HOLD SoR formally cleared
- [x] Handed to Spec QA; Chief Security pinged for Spec Security QA v2
- [ ] Spec QA Soft HOLD PASS until Security QA confirms checklist **v2**

**Confirm:** **v2.2, Chief Spec CLEAR.** Path `specs/2026-10-05__spec__spec__core-admin-dashboard.md`. Spec QA Soft HOLD until Security QA confirms v2. No password values. Soft HOLD build. PoC $0.
