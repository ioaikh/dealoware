# Participant data isolation (authz / tenancy) — outside PoC

| Field | Value |
|-------|-------|
| **Date** | 2026-09-20 |
| **Source** | CEO intake (Ivan) via Bot Manager |
| **GitHub** | [#18](https://github.com/ioaikh/dealoware/issues/18) |
| **Stage** | MVP+ (`stage:mvp`) — **not PoC** |
| **Status** | Captured · backlog |

## Decision

Keep **[#7](https://github.com/ioaikh/dealoware/issues/7)** as the PoC **identity-seal stub** only (omit contact/PII on public DTOs; opaque ids; no contact exchange on Accept in PoC).

Capture a **separate** requirement for keeping **other users' private information** secret: Strategies, lists of offers/negotiations, and other account-scoped data that an unauthorized party must not see. That work is **outside PoC** → [#18](https://github.com/ioaikh/dealoware/issues/18).

## Two threat models (do not conflate)

1. **Identity-seal (PoC #7 → MVP P7/A9)** — In an *active* negotiation, the counterparty must not learn real-world identity/contact until Accept.
2. **Participant data isolation (#18)** — Unauthenticated callers, wrong principals, and even a counterparty must not read another Participant's private Strategy, full account history, or unrelated offers/negotiations (IDOR / tenancy / authz).

## PoC boundary

- Strategy CRUD is **out of PoC** (MVP **P3**); seal of Strategy text is therefore not a #7 deliverable.
- #5 auth + #6 negotiation already imply principals and shared-negotiation scoping; remaining cross-tenant gaps close under **#18**, not by expanding #7.

## Next owners

- **CBA / Product:** refine #18 AC against PRODUCT-BRIEF when Strategy CRUD is scheduled.
- **CPM:** do not pull #18 into PoC pipeline; keep #7 narrow.
- **Security:** review #18 at Spec/SD/Product QA when unlocked.
