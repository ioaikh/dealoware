# Product scope — Core admin dashboard

| Field | Value |
|-------|--------|
| Written by | Dealoware Chief Product |
| Date | 2026-10-05 |
| Status | Goals + acceptance criteria. Ivan OK 1:06pm ET with edit/delete expansion. Updated 1:27pm ET via BM/CPM: all-status lists + filters, search by name, single superadmin sign-in. CEO final 1:30pm ET: CAPTCHA = Cloudflare Turnstile only (AWS WAF CAPTCHA OUT). SES for reset emails. Soft HOLD invent password. Soft HOLD build and Soft HOLD Stories until Spec/SA CLEAR + docs push. Soft HOLD build and deploy until harden live QA PASS after app image bake. |
| Audience | CPM → Bot Manager → Spec → SA (re-QA with Security QA after this patch) |
| Host | `admin.core.dealoware.com` (Core admin). Not `admin.platform.dealoware.com`. |
| Sequence | Inbound bot connector comes **after** this. |
| Soft HOLD build / deploy | Until harden live QA PASS after app image bake. Soft HOLD Stories until Spec/SA CLEAR + docs push. |
| Soft HOLD invent AWS | Stands. SES + Turnstile are named dependencies + cost only; do not invent AWS account details. Soft HOLD invent password. |
| PoC | Stays at $0. No code, no CDK, no spend, no provision from this note. |

## Goal

Give Ivan a Core admin dashboard at `admin.core.dealoware.com` so he can see, **edit**, and **delete** Core data: Participants, Artifacts, negotiations, and offers, plus overall stats — with **all-status lists**, **sort/filter/paging**, **search by name**, and a **single system superadmin** sign-in.

This is the Core owner surface. It is not the platform admin. Human users are not listed here. View-only is not enough.

## Host rule

| Host | Role |
|------|------|
| `admin.core.dealoware.com` | This scope. Core admin. |
| `admin.platform.dealoware.com` | Out of this scope. Platform admin. |
| `platform.dealoware.com` and its APIs | Out of this scope. User-facing platform where bots live. |

Core functionality stays public and may run somewhere other than AWS. CDK deployment details, the bot platform, and AWS account information stay private.

## Base

- Admin console / platform-owner stats on the core: `/workspace/dealoware-architecture-mvp-v1-v2.md` and `/workspace/dealoware-hosting-architecture-final.md` section 7.
- Prior Step 3 admin architecture (reconcile, do not assume platform admin): `/workspace/dealoware-h6b/docs/architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` and KB twin `architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md`.
- Two-part requirements: `product/2026-10-03__product__note__two-part-platform-requirements-delta.md` — on the core, the owner view counts Participants, negotiations, and offers; human users are not listed inside the core.
- CEO host standing rule (COO relay): Core admin is `admin.core.dealoware.com`; platform hosts are not Core.
- CEO OK 1:06pm ET via BM/CPM: expand from view-only to **edit and delete** on Participants, Artifacts, negotiations, and offers.
- CEO via BM 1:27pm ET / CPM: all-status negotiation and offer lists with sort/filter/paging; search by name on every table; single system superadmin `io@aiknowhow.com` with secure bootstrap (no password in code, docs, or chat).

## Reconcile vs prior Step 3

Prior Step 3 Spec scope (`product/2026-10-02__product__note__step3-admin-dashboard-spec-scope.md`) and Spec/architecture under `docs/specs/2026-10-02__spec__spec__platform-owner-admin-dashboard.md` and `docs/architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` named a platform-owner admin with O1 registered users, O2 artifacts, O3 negotiations/offers.

This Core admin scope does **not** assume that platform-admin host or O1 human-user list:

| Prior Step 3 item | This Core admin scope |
|-------------------|----------------------|
| O1 registered users (human) | **OUT.** Human users live on the platform / user subsystem. The Core lists Participants only. |
| O2 artifacts (owner) | **IN.** List, view, **edit**, and **delete** Artifacts on the Core. |
| O3 negotiations / offers | **IN.** List, view, **edit**, and **delete** negotiations and offers on the Core (all statuses; see Lists). |
| Participants | **IN.** List, view, **edit**, and **delete** Participants (not human users). |
| Overall stats | **IN.** Counts on Core entities (see AC). |
| Host assumed as generic platform-owner surface | **Replaced.** Host is `admin.core.dealoware.com` only. |
| Participant UI (#69) | **OUT.** Separate from this admin. |
| Platform admin (`admin.platform`) | **OUT** of this note. |

## IN

1. Core admin at `admin.core.dealoware.com`.
2. List, view, **edit**, and **delete** **Participants** (not human users).
3. List, view, **edit**, and **delete** **Artifacts**.
4. List, view, **edit**, and **delete** **negotiations** and **offers**.
5. **Lists (all statuses):** Negotiations and offers lists include **every** status (open, accepted, declined, expired, withdrawn, soft-deleted when soft delete exists, and any other Core statuses Spec names). Soft-deleted rows are available via an explicit toggle (or equivalent), not hidden permanently from admin. Sort and filter on: status, created/updated, participant, artifact, value/price, negotiation id. **Server-side paging** required.
6. **Search by name** on every table: Participants, Artifacts, negotiations, offers. Product meaning of "name":
   - **Participants:** the Participant's display name / identifier shown in Core.
   - **Artifacts:** the Artifact's name / title / subject string.
   - **Negotiations:** searchable composite of artifact/subject name **and** participant names on that negotiation.
   - **Offers:** searchable composite of artifact/subject name, offering participant name, and related negotiation id (id remains filterable separately).
7. **Overall stats** for Core entities: Participants, open negotiations, offers, accepts, and declines (queried from Core data). No charts product. No warehouse.
8. **Single system superadmin** sign-in (see Spec / SA must cover). Role = Core owner / platform-owner on the Core. Separate from the Participant UI. Fields follow FieldPolicy for that role. No parallel admin access list of multiple humans.
9. Soft HOLD build and deploy until harden live QA PASS after app image bake.

## Spec / SA must cover (required, not optional)

Product locks these as in-scope for Spec and SA. Exact design is theirs except where Product names a fixed value. Do not treat them as optional polish. **Re-QA Spec and SA with Security QA** after this patch.

| Topic | Product requirement |
|-------|---------------------|
| Superadmin identity | **One** system superadmin. Login email: `io@aiknowhow.com`. Email + password. |
| Bootstrap / password | Seeded via secure **one-time** bootstrap. **No password in code, docs, Spec text, chat, or commit history.** Initial password is set only via a reset / bootstrap link delivered out-of-band. |
| 2FA | **Required.** TOTP authenticator app. Spec decides whether email OTP is an allowed fallback. |
| Password reset | Email link **plus** 2FA code. |
| CAPTCHA | Required on login and password-reset pages. Provider locked: **Cloudflare Turnstile only**. AWS WAF CAPTCHA is **OUT**. reCAPTCHA is **OUT**. Spec notes Turnstile cost. |
| Session / abuse | Lockout / rate limit on failed logins; session lifetime; audit of logins and password resets (in addition to edit/delete audit). |
| Mail dependency | Password reset / bootstrap email uses **SES**. Flag as **dependency + cost**. Soft HOLD invent AWS: do not invent account IDs or provision from this note. Soft HOLD invent password. |
| Confirm before delete | Delete of a Participant, Artifact, negotiation, or offer requires an explicit confirm before it runs. |
| Audit log | Admin edits and deletes are recorded in an audit log Spec/SA define (plus login/reset audit above). |
| Delete semantics | Spec/SA define soft vs hard delete and cascades to related offers / negotiations / Artifacts / Participants. Product does not pick soft vs hard here. Soft-deleted rows must still be listable via the soft-deleted toggle when soft delete is chosen. |
| Lists / search / paging | Spec/SA define API and UI for all-status lists, sort/filter fields above, server-side paging, and name search per Product definitions. |

## OUT / Soft HOLD

| Item | Disposition |
|------|-------------|
| Build, deploy, CDK, provision, spend | Soft HOLD until harden live QA PASS after app image bake. |
| Invent AWS account details / provision | Soft HOLD invent AWS. Dependencies may be named; accounts and spend are not. |
| Inbound bot connector (Grok / Muse / dots) | After this. Not in this scope. |
| `admin.platform.dealoware.com` and platform human-user admin | OUT |
| Human user list inside the Core | OUT |
| Participant UI (#69) reuse as admin | OUT |
| Multiple admin humans / ACL of operators | OUT — single system superadmin only in this slice |
| O9 SSO / IdP as delivered | Soft HOLD / OUT (email + password + TOTP is the required path above) |
| A8 mature metering UI / AI-token meters on the Core | OUT (Core does not consume AI tokens) |
| CloudWatch cost view, day/month period stats, closed-deal counts | Later versions in the architecture base; not this first Core admin slice unless Ivan expands this doc |
| Settlement / escrow / checkout | OUT |
| MotorMarket / DC4 | OUT |
| Marketing publish | Soft HOLD |
| AWS WAF CAPTCHA / reCAPTCHA | OUT — Turnstile only |
| Invent password for `io@aiknowhow.com` | Soft HOLD invent password — bootstrap via reset link only |
| Stories, code, CDK, spend from this note alone | Soft HOLD Stories until Spec/SA CLEAR + docs push. Soft HOLD build until harden live QA PASS after app image bake. |
| Password values in Spec, docs, or chat | **Forbidden** |

## Acceptance criteria

AC1. Ivan can open the Core admin at `admin.core.dealoware.com` (not `admin.platform`).

AC2. He can list, open, **edit**, and **delete** **Participants**. The list is Participants, not human users. He can **search by Participant name**.

AC3. He can list, open, **edit**, and **delete** **Artifacts**. He can **search by Artifact name**.

AC4. He can list, open, **edit**, and **delete** **negotiations** and **offers**. Lists include **all statuses** (including soft-deleted via toggle when soft delete exists). He can **sort and filter** on status, created/updated, participant, artifact, value/price, and negotiation id. Lists use **server-side paging**. He can **search by name** using the Product definitions above.

AC5. He can see **overall stats**: counts of Participants, open negotiations, offers, accepts, and declines from Core data. No charts product. No warehouse.

AC6. Admin screens are not the Participant UI. Field visibility follows FieldPolicy for the Core owner role. No parallel multi-user admin ACL — **one** system superadmin.

AC7. Spec/SA deliver (required): single superadmin `io@aiknowhow.com` with email+password, secure one-time bootstrap (no password in code/docs/chat), required TOTP 2FA (Spec decides email OTP fallback), password reset via email link + 2FA, CAPTCHA on login/reset via **Cloudflare Turnstile only** (cost noted; AWS WAF CAPTCHA and reCAPTCHA OUT), lockout/rate limit, session lifetime, login/reset audit; confirm before delete; audit log of admin edits and deletes; delete semantics (soft vs hard and cascades); all-status lists, sort/filter, server-side paging, and name search. Product does not invent SES/Turnstile account secrets, passwords, or AWS details in this note. Soft HOLD invent password.

AC8. This scope does not deliver the inbound bot connector, platform admin, human registration, payments, or token allowance.

AC9. Soft HOLD build and deploy holds until harden live QA PASS after app image bake. Soft HOLD invent AWS holds. PoC stays at $0 until that gate and a separate spend unlock.

## Success for this note

Product path updated for lists, search, and superadmin. Spec → SA proceeds on this path and **re-QAs with Security QA**. Soft HOLD build and Soft HOLD Stories until Spec/SA CLEAR + docs push (and Soft HOLD build/deploy until harden live QA PASS after app image bake). Opening or revising Spec is not a build unlock. Soft HOLD invent password.

## Repo mirror

Push path for Ivan review in Cursor: `docs/product/2026-10-05__product__note__core-admin-dashboard-scope.md` on `ioaikh/dealoware` `main` (same layout as commit `2d93e94`).

## Not in this note

Soft HOLD Stories until Spec/SA CLEAR + docs push. No code. No CDK. No spend. No provision. No password values. Soft HOLD invent password. PoC $0.
