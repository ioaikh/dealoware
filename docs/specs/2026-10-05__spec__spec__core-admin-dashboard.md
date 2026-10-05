# Spec — Core admin dashboard

| Field | Value |
|-------|--------|
| Written by | Dealoware Senior Spec |
| Date | 2026-10-05 |
| Status | Design Spec (KB only). Chief Spec CLEAR for Spec QA. Spec Security points 1–10 bound (§13). Spec QA Soft HOLD PASS until Security QA confirms. SA after Spec QA on this track. |
| Brief | Chief Spec CLEAR for Spec QA (after two small fixes). Product scope note patched **2026-10-05 13:06** is binding authority. |
| Host | `admin.core.dealoware.com` (Core admin). Not `admin.platform.dealoware.com`. |
| Binding Product | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` (patched **2026-10-05 13:06**) — **binding authority** for IN/OUT, edit/delete, AC1–AC9, and the four required Spec/SA designs. CPM relay of CEO OK is history only; cite the note. |
| App target | .NET 10 (retarget in flight on public PR #1 from commit `1c9d10b`). This Spec does not implement the retarget. |
| Build / deploy | Held until harden live QA PASS after the app image bake. Opening Spec is not a build unlock. |
| PoC | $0. No Stories, no code, no CDK, no spend, no provision from this Spec. |

---

## Sources (cite only)

| Source | Path / role |
|--------|-------------|
| Product Core admin scope | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` — **binding authority** for IN/OUT, edit/delete, AC1–AC9, and Spec/SA must-cover (auth, confirm-before-delete, audit log, delete semantics). Patched 2026-10-05 13:06. |
| CEO OK (history) | CEO OK 2026-10-05 1:06pm ET via CPM — now carried in the Product scope note; **not** a separate authority. |
| Spec Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` | Spec-step points **1–10** (bind; do not invent twin) |
| Prior Step 3 admin architecture (reconcile) | `architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` — keep Option A shape where it fits Core; drop O1 human users and platform-admin host |
| Prior Step 3 admin Spec (reconcile) | `specs/2026-10-02__spec__spec__platform-owner-admin-dashboard.md` — same reconcile rules |
| Two-part platform delta | `product/2026-10-03__product__note__two-part-platform-requirements-delta.md` — Core owner counts Participants / negotiations / offers; human users are not listed inside Core |
| CEO host standing rule | `admin.core.dealoware.com` is Core admin; platform hosts are not Core. Core stays public and may run outside AWS. No CDK details, bot-platform internals, or AWS account information in this Spec. |
| FieldPolicy dual wall | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3a/§3b — cite; do not rewrite; no parallel admin access list |

Do not cite old public-repo issue or PR numbers from the pre-recreate history as live items on the current public repo.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 1 | Host | Core admin is only at `admin.core.dealoware.com`. Platform admin and platform app hosts are out of this Spec. |
| 2 | Role | Core owner role only. Not the Participant UI. Fields follow FieldPolicy for that role. No parallel admin access list. |
| 3 | Entity surface | List, view, edit, and delete Participants (not human users), Artifacts, negotiations, and offers. |
| 4 | Stats | Overall counts from Core data (§6): Participants, open negotiations, offers, accepts, declines. No charts product. No warehouse. |
| 5 | Must-cover | Admin auth/sign-in; confirm before every delete; audit log of every admin edit and delete; delete semantics (soft vs hard + cascades). Exact mechanism choices marked for SA where noted. |
| 6 | Sequence | Inbound bot connector comes after this. SA comes after Spec QA on this track. |
| 7 | Hold | Build, deploy, Stories, code, CDK, spend held. PoC $0. Spec does not implement .NET 10 retarget. |

---

## 1. Purpose

Specify a Core owner admin dashboard at `admin.core.dealoware.com` so the Core owner can list, view, edit, and delete Core data (Participants, Artifacts, negotiations, offers) and see overall Core counts.

This is design Spec only. It does not unlock build or deploy. It does not invent Stories, code, CDK, spend, or product requirements beyond `product/2026-10-05__product__note__core-admin-dashboard-scope.md`.

---

## 2. Reconcile vs prior Step 3 admin

Prior Step 3 named a platform-owner admin with human registered users (O1), Artifacts (O2), negotiations/offers (O3), and a generic platform-owner surface.

| Prior Step 3 item | This Core admin Spec |
|-------------------|----------------------|
| O1 registered human users | **Drop.** Human users are not listed on Core. |
| O2 Artifacts | **Keep and expand.** List, view, edit, delete. |
| O3 negotiations / offers | **Keep and expand.** List, view, edit, delete. |
| Participants | **IN** (not human users). List, view, edit, delete. |
| Overall stats | **IN** as Core counts (see §6). |
| Platform-admin / platform-owner host | **Replace.** Host is `admin.core.dealoware.com` only. |
| Option A: same app host, separate admin route, distinct owner principal, FieldPolicy dual wall, no parallel admin ACL | **Keep** as the working Spec shape; SA confirms after Spec QA. |
| Participant UI as admin | **Out.** |
| SSO / IdP as delivered | **Out** as delivered; Core owner auth/sign-in is still required (§8). |

---

## 3. Host and role

| Concern | Spec requirement |
|---------|------------------|
| Host | Admin UI and admin API for this Spec are served only as Core admin at `admin.core.dealoware.com`. |
| Not this Spec | `admin.platform.dealoware.com`, `platform.dealoware.com`, and their APIs. |
| Role | Core owner only. Separate from Participant principal and Participant UI. |
| Fields | Visibility and mutation follow FieldPolicy for the Core owner role (Option A §3a/§3b dual wall). No parallel admin access list that drifts from Domain FieldPolicy. |
| Privacy | Core stays public. This Spec must not contain CDK details, bot-platform internals, or AWS account information. |

**SA to confirm:** how the Core owner principal/claim is named in code (for example CoreOwner vs PlatformOwner-on-Core) while keeping the Product meaning: owner of Core, not platform human-user admin.

---

## 4. Entities — list, view, edit, delete

For each of the four entity types below, the Core owner can list, open a detail view, edit allowed fields, and delete (subject to confirm §9, audit §10, and delete semantics §11). All reads and writes are fail-closed through FieldPolicy for the Core owner role.

### 4.1 Participants

- List and view **Participants**, not human users.
- Edit Participant fields that FieldPolicy allows the Core owner to change.
- Delete a Participant only after confirm; cascades per §11.

### 4.2 Artifacts

- List, view, edit, and delete Artifacts on Core.
- FieldPolicy applies; no parallel Artifact admin ACL.

### 4.3 Negotiations

- List, view, edit, and delete negotiations on Core.
- Open negotiations are in scope for edit and delete; delete behavior for an open negotiation is defined in §11.

### 4.4 Offers

- List, view, edit, and delete offers on Core.
- Accept and decline outcomes remain visible in stats (§6); this Spec does not add settlement or checkout.

---

## 5. Edit rules

| Rule | Spec requirement |
|------|------------------|
| Allowed fields | Only fields FieldPolicy allows the Core owner to write. Spec does not invent a second permission matrix. |
| Fail-closed | Missing Core owner role → no edit. Denied FieldClass → no write and no dump into UI error payloads beyond a safe deny. |
| Audit | Every successful admin edit is written to the audit log (§10) with before and after. |
| SA to confirm | Exact editable field sets per entity (within FieldPolicy), concurrency/conflict behavior, and validation errors shown in the admin UI. |

---

## 6. Overall stats

| Stat | Source |
|------|--------|
| Participants count | Core data |
| Open negotiations count | Core data |
| Offers count | Core data |
| Accepts count | Core data |
| Declines count | Core data |

No charts product. No warehouse. Counts are queried from Core. Later cost views, CloudWatch period stats, and closed-deal analytics stay out unless Product expands this scope.

---

## 7. Architecture shape (for SA after Spec QA)

Working Spec shape carried from prior Step 3 Option A where it still fits:

- Same Core application host as the Negotiation Core modular monolith (not a separate admin mesh).
- Separate Core admin route surface (admin API + thin admin UI) at `admin.core.dealoware.com`.
- Distinct Core owner principal / role claim.
- FieldPolicy dual wall; no parallel admin ACL.
- Reject: reuse Participant UI as admin; SSO/IdP as delivered for this slice; separate admin microservice; platform-admin host.

**SA to confirm** the Option A carry-forward and any Core-specific adjustments after Spec QA. This Spec does not lock host provision, CDK, or spend.

---

## 8. Admin auth and sign-in (must-cover)

Product requires that the Core owner can sign in to Core admin. The scope note is silent on mechanism, so this Spec states the requirements and leaves the exact mechanism to SA.

| Requirement | Spec lock |
|-------------|-----------|
| Who can sign in | Only the Core owner role. |
| Role binding | Sign-in must bind a verified Core owner identity to the Core owner principal/claim used by FieldPolicy. |
| Fail-closed | If the signed-in principal is not Core owner, admin routes return deny (no entity list, no edit, no delete, no stats beyond a safe unauthorized response). |
| Session expiry | Admin sessions expire. After expiry, the user must sign in again before any admin action. |
| Not delivered here | SSO / IdP as a delivered product (O9-style) stays out. Auth for Core owner is still required. |
| Secrets / account data | Do not put AWS account information, CDK details, or bot-platform internals in this Spec or in public admin copy. |

**SA to pick (explicit):** exact sign-in mechanism (for example password credential issued to Core owner, magic link, or another non-SSO mechanism that meets the table above), session store and lifetime, and how the Core owner role claim is attached at login. Spec QA and SA must not treat auth as optional polish.

---

## 9. Confirm before delete (must-cover)

| Requirement | Spec lock |
|-------------|-----------|
| Scope | Every delete of a Participant, Artifact, negotiation, or offer. |
| Behavior | Delete does not run until the Core owner gives an explicit confirm in the admin UI (or equivalent admin API confirm step that cannot be skipped by a single blind request in the UI flow). |
| Content | Confirm must name the entity type and enough identity for the owner to know what will be deleted (for example display name / id). |
| Cascades | Confirm copy must state the cascade summary from §11 when cascades apply (especially open negotiations and related offers). |
| Cancel | Dismiss / cancel leaves data unchanged and writes no delete audit row. |

**SA to confirm:** confirm UX pattern (modal vs dedicated page) and whether API-only clients need a confirm token.

---

## 10. Audit log (must-cover)

Every successful Core admin **edit** and **delete** is recorded.

| Field | Required |
|-------|----------|
| Who | Core owner identity that performed the action |
| When | Timestamp (UTC stored; display in admin in a clear timezone) |
| What entity | Entity type + entity id |
| Action | Edit or delete |
| Before | Snapshot of relevant fields before the change (delete: state at delete time) |
| After | Snapshot after edit; for delete, after-state shows deleted/soft-deleted marker per §11 |

| Rule | Spec lock |
|------|-----------|
| Coverage | Admin edits and deletes only for this Spec’s four entity types (plus any cascade rows SA ties to the same action). |
| Integrity | The audit log **cannot** be edited or deleted from the Core admin UI or admin API. |
| Access | Core owner may **read** the audit log in admin. No parallel ACL. FieldPolicy applies to any sensitive fields inside snapshots. |
| Fail-closed | If audit write fails, the edit/delete must not commit (or must roll back) so admin mutations are not silent. **SA to confirm** the exact transaction pairing. |

**SA to confirm:** storage shape (append-only table vs event log), retention, and how large field bodies are truncated in snapshots without losing who/when/what.

---

## 11. Delete semantics (must-cover)

### 11.1 Soft vs hard

Product leaves soft vs hard to Spec and SA. This Spec’s recommendation:

| Choice | Spec recommendation |
|--------|---------------------|
| **Default** | **Soft delete** for Participants, Artifacts, negotiations, and offers. |
| Reason | Preserves audit and support recovery; keeps referential history; avoids irreversible wipe of Core history on first admin slice. |
| Hard delete | Not the default. May be designed later only as an explicit SA/Product follow-on with a separate confirm path. |

**SA to confirm** the soft-delete marker (for example `DeletedAt`), list defaults (hide soft-deleted rows; optional “include deleted” toggle), and whether hard delete is deferred entirely.

### 11.2 Cascades (all four directions)

Product requires Spec/SA to define cascades among **offers, negotiations, Artifacts, and Participants**. Spec recommendations below; SA confirms after Spec QA.

| When deleting → effect on | Offers | Negotiations | Artifacts | Participants |
|---------------------------|--------|--------------|-----------|--------------|
| **Offer** | Soft-delete that offer. | Parent negotiation stays unless separately deleted. | No change. | No change. |
| **Negotiation** | Soft-delete **all child offers** of that negotiation. | Soft-delete the negotiation. If **open**, confirm must warn it will leave active lists with its offers. | No automatic Artifact delete. | No change to Participant records. |
| **Artifact** | No direct offer wipe. Offers stay with their negotiations unless those negotiations are cascaded by another rule SA confirms. | **Default recommendation:** **block** Artifact delete while any non-deleted negotiation references it; admin error lists those negotiations. SA may instead pick detach or cascade soft-delete of referencing negotiations (and then their offers). | Soft-delete the Artifact when allowed. | No change. |
| **Participant** | Soft-delete offers on negotiations cascaded below. | **Default recommendation:** soft-delete negotiations that Participant owns or participates in (open and closed), with confirm text stating counts. Open negotiations follow §11.3. | **Default recommendation:** do **not** auto-delete Artifacts solely because a Participant is deleted; block or list references for SA to confirm (Artifact may be shared). | Soft-delete the Participant. Human users are out of Core admin (no human-user delete). |

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
- List, view, edit, delete Participants (not human users)
- List, view, edit, delete Artifacts
- List, view, edit, delete negotiations and offers
- Overall Core stats: Participants, open negotiations, offers, accepts, declines
- Core owner role + FieldPolicy; no parallel admin ACL
- Admin auth/sign-in requirements (§8)
- Confirm before every delete (§9)
- Audit log of admin edits and deletes; log not editable/deletable from admin (§10)
- Delete semantics: soft-delete default + cascades (§11)
- App target awareness: .NET 10 (Spec does not implement retarget)

### OUT

- Human-user list on Core
- Platform admin (`admin.platform.dealoware.com`) and platform human-user admin
- Inbound bot connector (comes after)
- Participant UI reused as admin
- Settlement / escrow / checkout
- MotorMarket / DC4
- Metering UI / AI-token meters on Core
- Cost views / warehouse / charts product
- SSO / IdP as delivered
- Stories, code, CDK, spend, provision, deploy (held until harden live QA PASS after app image bake, then separate unlock)
- AWS account information, CDK details, bot-platform internals in this Spec

---


---

## 13. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md`  
**Rule:** Spec QA must **not** PASS until Security QA confirms these points. Design unchanged — answers cite existing sections only. SA after Spec QA. No build. PoC **$0**.

| # | Security point | Spec answer | Spec section cites |
|---|----------------|-------------|--------------------|
| 1 | Host + role boundary | **MET.** Admin only at `admin.core.dealoware.com`. Core owner role only. Distinct from Participant UI and from `admin.platform.dealoware.com`. Human-user list on Core is OUT. | Locked #1–#2; §3; §12 OUT |
| 2 | Core owner sign-in and session | **MET.** Core owner auth/sign-in required; binds verified Core owner identity to principal/claim FieldPolicy uses. Sessions expire; after expiry no admin action until sign-in again. SSO/IdP as delivered OUT; auth still required. SA picks mechanism after Spec QA. | §8 |
| 3 | Fail-closed deny for non-owner | **MET.** Non–Core-owner principal: admin routes deny — no entity list, no edit, no delete, no stats beyond safe unauthorized. Missing role → no mutation. Denied FieldClass → no write and no dump of denied fields into UI/error payloads. | §3; §5; §8 |
| 4 | FieldPolicy only; no parallel admin ACL | **MET.** All reads/writes for Participants, Artifacts, negotiations, and offers bind Domain FieldPolicy for Core owner (Option A §3a/§3b dual wall). Parallel admin access list that drifts is rejected. | Locked #2; §3; §4; §5; §7 |
| 5 | Confirm before every delete | **MET.** Delete of Participant, Artifact, negotiation, or offer does not run until explicit confirm naming entity type + identity and cascade summary when cascades apply. Cancel leaves data unchanged and writes no delete audit row. | §9; §11.2 |
| 6 | Audit log integrity | **MET.** Every successful admin edit and delete records who / when / entity type+id / action / before / after. Audit log cannot be edited or deleted from Core admin UI or admin API. Core owner may read under FieldPolicy. If audit write fails, edit/delete must not commit (or must roll back). SA confirms storage/transaction pairing after Spec QA. | §10 |
| 7 | Soft-delete default + cascades | **MET.** Soft-delete recommended default for all four entity types; hard delete only as later explicit SA/Product follow-on with separate confirm. Offer: soft-delete offer only. Negotiation: soft-delete negotiation + child offers; open negotiation confirm must warn. Artifact: defined rule (recommended default: block while non-deleted negotiation references it). Participant: defined cascade to negotiations/offers (recommended default: soft-delete those negotiations and their offers with confirm counts). Cascades cover offers, negotiations, Artifacts, and Participants. SA confirms markers and exact cascade picks after Spec QA. | §11 |
| 8 | Edit audit + FieldPolicy write surface | **MET.** Every successful admin edit audited with before/after. Only FieldPolicy-allowed fields writable. No second permission matrix. | §5; §10 |
| 9 | OUT / Soft HOLD pack | **MET.** OUT includes platform admin hosts; human-user list on Core; Participant UI as admin; inbound bot connector (comes after); settlement/escrow/checkout; SSO/IdP as delivered; Stories / code / CDK / spend / provision / deploy (held until harden live QA PASS after app image bake, then separate unlock); AWS account information / CDK details / bot-platform internals in Spec. PoC $0. Cost/critical → COO → CEO. | Header; §12 OUT |
| 10 | Traceability + handshake | **MET.** Spec cites Product scope note (patched 13:06, binding authority) + Option A FieldPolicy dual wall + §§8–11 must-cover + this checklist. Spec QA must not PASS until Security QA confirms these points. SA waits on Spec QA for this track. | Sources; §8–§11; this §13 |

## 14. Acceptance mapping (Product AC1–AC9)

Binding: Product note AC1–AC9. AC7 is the four required designs. AC9 is the build/deploy hold.

| ID | Product requirement | Spec coverage |
|----|---------------------|---------------|
| AC1 | Open Core admin at `admin.core.dealoware.com` (not `admin.platform`) | §3 |
| AC2 | List, open, edit, delete Participants (not human users) | §4.1, §5, §9–§11 |
| AC3 | List, open, edit, delete Artifacts | §4.2, §5, §9–§11 |
| AC4 | List, open, edit, delete negotiations and offers | §4.3–§4.4, §5, §9–§11 |
| AC5 | Overall stats: Participants, open negotiations, offers, accepts, declines; no charts; no warehouse | §6 |
| AC6 | Not Participant UI; FieldPolicy for Core owner; no parallel admin ACL | §3, §5, §7 |
| AC7 | Four required designs: (1) Core owner auth/sign-in; (2) confirm before delete; (3) audit log of admin edits/deletes; (4) delete semantics (soft vs hard + cascades among offers, negotiations, Artifacts, Participants) | §8 auth; §9 confirm; §10 audit; §11 delete semantics |
| AC8 | Does not deliver inbound bot connector, platform admin, human registration, payments, or token allowance | §12 OUT |
| AC9 | Build and deploy held until harden live QA PASS after app image bake; PoC $0 until that gate and a separate spend unlock | Header; §12 OUT |

---

## 15. Done-list → Chief Spec

- [x] Design Spec written at `specs/2026-10-05__spec__spec__core-admin-dashboard.md` (KB only; no PR)
- [x] Host locked to `admin.core.dealoware.com`; platform admin out
- [x] Product scope note cited as authority (edit/delete IN; four required designs); patched note 13:06
- [x] Reconciled prior Step 3: kept Option A-compatible shape; dropped O1 human users and platform-admin host
- [x] Must-cover sections: auth/sign-in; confirm before delete; audit log; delete semantics (soft default; cascades all four directions + open negotiation)
- [x] Acceptance mapping to Product AC1–AC9 (AC7 = four designs; AC9 = build hold)
- [x] IN/OUT list; build/deploy/Stories/code/CDK/spend held; PoC $0
- [x] .NET 10 noted; Spec does not implement retarget
- [x] No CDK / AWS account / bot-platform internals in Spec
- [x] SA-after-Spec-QA items marked explicitly
- [x] Handed to Spec QA (Chief Spec CLEAR for content)
- [x] Spec Security checklist points 1–10 answered with section cites (§13)
- [ ] Spec QA PASS Soft HOLD until Security QA confirms points 1–10

**Confirm:** Spec Security points 1–10 bound in §13. Spec QA Soft HOLD PASS until Security QA confirms. No build. PoC $0. SA after Spec QA.
