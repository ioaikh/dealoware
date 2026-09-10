# Dealoware Agent Ops Charter

CEO: Ivan Onuchin. COO: Dealoware COO (Business Team). All cost-affecting or critical changes require CEO confirmation.

## Goal alignment (mandatory)
1. **Product Team** aligns product goals with **CEO** only.
2. **Every other team** aligns its goals and deliverables to **Product** (Chief Product). If a Story conflicts with Product goals, escalate via PM → Product → CEO — do not silently diverge.
3. Marketing claims lock still runs through Chief Product before publish.

## Pipeline (via PM Team)
Business → BA Team → SA Team → Spec Team → Dev Plan Team → SD Team → QA Team → **CQ Team** (post-task refactor gate) → Doc Team
(After each SD task completes: CQ assesses; if refactor needed → refactor spec → Dev Plan → SD → QA tests-pass; PM owns the gate.)  
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
| **CQ Team** | **Chief CQ** | Senior CQ | CQ QA | Dealoware CQ Team |

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


## Code Quality / refactor gate (CEO 2026-09-10)
- **CQ Team** assesses code for refactoring **after each SD task completes**.
- If refactor needed: CQ writes a **refactor requirement specification** before the pipeline moves forward.
- CQ coordinates with **QA Team** so functionality that could be affected by the refactor is covered by tests.
- **PM (CPM)** owns this gate end-to-end.
- Flow when refactor required: CQ refactor spec (QA coverage confirm) → **Dev Plan** → **SD** → **QA** verifies all tests passed → then Doc / continue.
- Refactor specs land under `specs/` (or `plans/` if PM schedules) per DOC-FLOW naming, e.g. `YYYY-MM-DD__cq__spec__refactor-{slug}.md`.

## Hosting / cloud currency (CEO 2026-09-10)
- **Locked target host shape:** Amazon **ECS Express Mode (Fargate)**. Do **not** recommend or sketch AWS App Runner for Dealoware (closed to new customers 2026-04-30; no new features).
- PoC/MVP host shape = sketch only until CEO OK; **any AWS provision / spend → escalate PM → COO → CEO** before acting.

### Mandatory checklist before recommending any AWS (or other cloud) service
1. **Current availability:** Check official AWS docs / product page for “new customers”, “maintenance mode”, “no new features”, or sunset notices dated within the last 90 days (or newer if known).
2. **Prefer actively invested services** AWS (or vendor) recommends for *new* workloads.
3. **Label status explicitly** in architecture options: `open` | `existing-customers-only` | `no-new-features` | `deprecated/sunset`.
4. **Never** list a closed-to-new-customers or no-new-features service as a default/sketch option for greenfield Dealoware work.
5. Architecture QA **bounces** if a cited service lacks a currency check or cites a closed/maintenance-mode service without labeling and excluding it from greenfield options.
6. When unsure: consult DevOps via PM; still escalate spend before provision.
