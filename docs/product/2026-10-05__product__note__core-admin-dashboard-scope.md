# Product scope — Core admin dashboard

| Field | Value |
|-------|--------|
| Written by | Dealoware Chief Product |
| Date | 2026-10-05 |
| Status | Goals + acceptance criteria. Ivan OK 1:06pm ET with edit/delete expansion. Soft HOLD build and deploy until harden live QA PASS after app image bake. |
| Audience | CPM → Bot Manager → Spec → SA |
| Host | `admin.core.dealoware.com` (Core admin). Not `admin.platform.dealoware.com`. |
| Sequence | Inbound bot connector comes **after** this. |
| Soft HOLD build / deploy | Until harden live QA PASS after app image bake. Ivan OK already. |
| PoC | Stays at $0. No code, no CDK, no spend, no provision from this note. |

## Goal

Give Ivan a Core admin dashboard at `admin.core.dealoware.com` so he can see, **edit**, and **delete** Core data: Participants, Artifacts, negotiations, and offers, plus overall stats.

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

## Reconcile vs prior Step 3

Prior Step 3 Spec scope (`product/2026-10-02__product__note__step3-admin-dashboard-spec-scope.md`) and Spec/architecture under `docs/specs/2026-10-02__spec__spec__platform-owner-admin-dashboard.md` and `docs/architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` named a platform-owner admin with O1 registered users, O2 artifacts, O3 negotiations/offers.

This Core admin scope does **not** assume that platform-admin host or O1 human-user list:

| Prior Step 3 item | This Core admin scope |
|-------------------|----------------------|
| O1 registered users (human) | **OUT.** Human users live on the platform / user subsystem. The Core lists Participants only. |
| O2 artifacts (owner) | **IN.** List, view, **edit**, and **delete** Artifacts on the Core. |
| O3 negotiations / offers | **IN.** List, view, **edit**, and **delete** negotiations and offers on the Core. |
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
5. **Overall stats** for Core entities: Participants, open negotiations, offers, accepts, and declines (queried from Core data). No charts product. No warehouse.
6. Role = Core owner / platform-owner on the Core. Separate from the Participant UI. Fields follow FieldPolicy for that role. No parallel admin access list.
7. Soft HOLD build and deploy until harden live QA PASS after app image bake.

## Spec / SA must cover (required, not optional)

Product locks these as in-scope for Spec and SA. Exact design is theirs. Do not treat them as optional polish.

| Topic | Product requirement |
|-------|---------------------|
| Admin auth / sign-in | Core owner role can sign in to Core admin. Spec/SA define how. Soft HOLD invent SSO as delivered (O9 stays out). |
| Confirm before delete | Delete of a Participant, Artifact, negotiation, or offer requires an explicit confirm before it runs. |
| Audit log | Admin edits and deletes are recorded in an audit log Spec/SA define. |
| Delete semantics | Spec/SA define soft vs hard delete and cascades to related offers / negotiations / Artifacts / Participants. Product does not pick soft vs hard here. |

## OUT / Soft HOLD

| Item | Disposition |
|------|-------------|
| Build, deploy, CDK, provision, spend | Soft HOLD until harden live QA PASS after app image bake. Ivan OK already. |
| Inbound bot connector (Grok / Muse / dots) | After this. Not in this scope. |
| `admin.platform.dealoware.com` and platform human-user admin | OUT |
| Human user list inside the Core | OUT |
| Participant UI (#69) reuse as admin | OUT |
| O9 SSO / IdP as delivered | Soft HOLD / OUT (auth/sign-in for Core owner is still required above) |
| A8 mature metering UI / AI-token meters on the Core | OUT (Core does not consume AI tokens) |
| CloudWatch cost view, day/month period stats, closed-deal counts | Later versions in the architecture base; not this first Core admin slice unless Ivan expands this doc |
| Settlement / escrow / checkout | OUT |
| MotorMarket / DC4 | OUT |
| Marketing publish | Soft HOLD |
| Stories, code, CDK, spend from this note alone | OUT until Soft HOLD lifts |

## Acceptance criteria

AC1. Ivan can open the Core admin at `admin.core.dealoware.com` (not `admin.platform`).

AC2. He can list, open, **edit**, and **delete** **Participants**. The list is Participants, not human users.

AC3. He can list, open, **edit**, and **delete** **Artifacts**.

AC4. He can list, open, **edit**, and **delete** **negotiations** and **offers**.

AC5. He can see **overall stats**: counts of Participants, open negotiations, offers, accepts, and declines from Core data. No charts product. No warehouse.

AC6. Admin screens are not the Participant UI. Field visibility follows FieldPolicy for the Core owner role. No parallel admin ACL.

AC7. Spec/SA deliver (required): Core owner admin auth/sign-in; confirm before delete; audit log of admin edits and deletes; delete semantics (soft vs hard and cascades). Product does not invent those designs in this note.

AC8. This scope does not deliver the inbound bot connector, platform admin, human registration, payments, or token allowance.

AC9. Soft HOLD build and deploy holds until harden live QA PASS after app image bake. Ivan OK already. PoC stays at $0 until that gate and a separate spend unlock.

## Success for this note

Ivan OK received with edit/delete expansion. Spec → SA may proceed on this path. Soft HOLD build and deploy until harden live QA PASS after app image bake. Opening Spec is not a build unlock.

## Not in this note

No Stories. No code. No CDK. No spend. No provision. PoC $0.
