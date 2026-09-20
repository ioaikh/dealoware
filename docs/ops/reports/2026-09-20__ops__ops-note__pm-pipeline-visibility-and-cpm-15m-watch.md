# Ops note — PM pipeline visibility + CPM 15-minute watches

| Field | Value |
|-------|-------|
| **Date** | 2026-09-20 |
| **Status** | **LOCKED** (CEO) — host flip post-#7 |
| **Charter** | `ops/ORG-OPS.md` § PM pipeline ownership + visibility |

## Rules
1. **PM channel mirror** on every Story status flip (issue + label + next triad + evidence pointer).
2. **Evidence-gated “in progress”** (fresh artifact ≤60–90m or say assigned/waiting/stalled).
3. **CPM owns pipeline push-through and hosts the 15m delivery watch** (arm at pipeline entry on CPM routines; delete on CLOSED `status:done`).
4. **Bot Manager audits only** (armed at entry? deleted on close?) — does not host the cron.

## Watch tick intent (CPM)
Check actual progress (commits/PR/gates) → nudge owning gate if stalled → update PM mirror → CEO only on material change or named blocker.

## History
- Pre-#7 / through #7 close: Bot Manager hosted the cron; CPM accountable.
- After #7 CLOSED (2026-09-20): CEO flipped host to CPM so BM is not occupied every 15m.

## PoC
$0. No duplicate COO ops-review routine.
