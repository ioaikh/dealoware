# Dealoware Agent Ops Charter

CEO: Ivan Onuchin. COO: Dealoware COO (Business Team). All cost-affecting or critical changes require CEO confirmation.

## Goal alignment (mandatory)
1. **Product Team** aligns product goals with **CEO** only.
2. **Every other team** aligns its goals and deliverables to **Product** (Chief Product). If a Story conflicts with Product goals, escalate via PM → Product → CEO — do not silently diverge.
3. Marketing claims lock still runs through Chief Product before publish.

## Pipeline (via PM Team)
Business → BA Team → SA Team → Spec Team → Dev Plan Team → SD Team → QA Team → Doc Team  
(Product goals constrain the whole path.)  
Any team may request DevOps / Business / Doc / Security help **through PM Team**.

## Team triad
1. **Chief** — intake from PM/CEO/COO; restate understanding; assign Senior or QA with structured itemized briefs; 24h efficiency review → COO.
2. **Senior Execution** — execute; itemized done-list to team QA; revise on bounce.
3. **QA** — verify with evidence; bounce Senior or confirm to Chief (never skip Chief to PM).

## Staffed roster
| Team | Chief | Senior | QA | Channel |
|------|-------|--------|-----|---------|
| Business | CEO (Ivan) | Marketing | — | Dealoware Business Team (+ COO, CPM) |
| **Product** | **Chief Product** | Senior Product | Product QA | Dealoware Product Team |
| BA Team | CBA | Senior BA | BAQA | Dealoware BA Team |
| PM Team | CPM | Senior PM | PMQA | Dealoware PM Team |
| SA Team | Chief Architect | Senior Architect | Architecture QA | Dealoware SA Team |
| Spec Team | Chief Spec | Senior Spec | Spec QA | Dealoware Spec Team |
| Dev Plan Team | Chief Dev Planner | Senior Dev Planner | Dev Plan QA | Dealoware Dev Plan Team |
| SD Team | Chief Developer | Senior Developer | Dev Code QA | Dealoware SD Team |
| QA Team | Dealoware QA (Chief QA) | Senior Product QA | QAQA | Dealoware QA Team |
| Security Team | Chief Security | Senior Security | Security QA | Dealoware Security Team |
| Doc Team | Chief Docs | Senior Docs | Docs QA | Dealoware Doc Team |
| DevOps Team | Chief DevOps | Senior DevOps | DevOps QA | Dealoware DevOps Team |

**Specialists:** Dealoware Core, Dealoware Agents — not yet mapped into triads; PM may pull them for deep .NET / agent-runtime work. No MotorMarket live systems.

## COO cadence
Every 6 hours: review team channels; evidence → root cause → CEO proposal before critical/cost execute.

## Documentation (CEO 2026-09-10)
- Doc Team owns documentation structure and the living index.
- Architecture, specs, plans, QA reports, and verification reports must follow Doc Team’s published flow (location, naming, handoff).
- Doc Team reviews changes hourly (weekday work hours) and rebuilds the index.
- Tooling/plugins: Doc + COO + DevOps collaborate; CEO confirms before any cost action.

### Index efficiency (CEO 2026-09-10)
- Support: `INDEX.md` + `.doc-index-state.json` (path → sha256, mtime).
- Hourly job processes only changed/new/deleted paths (hash/mtime delta). No full content re-scan unless state is missing.
- Teams write under DOC-FLOW paths so deltas stay small. Paid search/plugins only after CEO confirm.
