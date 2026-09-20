# Ops note — PM pipeline visibility + CPM 15-minute watches

| Field | Value |
|-------|-------|
| **Date** | 2026-09-20 |
| **Status** | **LOCKED** (CEO) |
| **Charter** | `ops/ORG-OPS.md` § PM pipeline ownership + visibility |

## Rules
1. **PM channel mirror** on every Story status flip (issue + label + next triad + evidence pointer).
2. **Evidence-gated “in progress”** (fresh artifact ≤60–90m or say assigned/waiting/stalled).
3. **CPM owns pipeline push-through:** arm Bot Manager 15m delivery watch at pipeline entry; delete watch on CLOSED `status:done`.

## Watch tick intent
Check actual progress (commits/PR/gates) → nudge owning gate if stalled → update PM mirror → CEO only on material change or named blocker.

## PoC
$0. No duplicate COO ops-review routine.
