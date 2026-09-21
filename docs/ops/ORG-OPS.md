# Dealoware Agent Ops Charter

CEO: Ivan Onuchin. COO: Dealoware COO (Ops Team Chief; also Business Team). All cost-affecting or critical changes require CEO confirmation.

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
| Business | CEO (Ivan) | — | — | Dealoware Business Team (+ COO, CPM, Chief Marketing) |
| **Ops** | **COO (Chief)** | Ops Executive | Ops QA | Dealoware Ops Team |
| **Marketing** | **Chief Marketing** | Senior Marketing | Marketing QA | Dealoware Marketing Team |
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
Every 6 hours: Ops Team reviews team channels (COO briefs → Ops Executive samples/analyzes → Ops QA verifies → COO presents); evidence → root cause → CEO proposal before critical/cost execute.

### Ops performance-check model (CEO 2026-09-20)
- Locked rubric: `ops/reports/2026-09-20__ops__ops-note__team-performance-check-model.md`.
- Every Ops review (each team, since last review): **read every message** — score internal (Productive / Efficient / Quality / Completeness / Functional) and external (Intake / During work / Handoff confirmation); then improvement proposals (within team / cross-team / specialty↔common roles). Plus gate/role/process checklists in that note.
- Bot Manager owns `dealoware-coo-ops-review`; do **not** duplicate on COO. No spend/critical without CEO OK.

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

## Security on critical pipeline steps (CEO 2026-09-10)
Security is woven into **every critical step** below — not Chief-only review.

### Critical steps (always)
1. **Task formulation (BA)**
2. **Architecture (SA)**
3. **Specs**
4. **Dev Plan**
5. **Implementation (SD)**
6. **QA (product verification)**
7. **Refactoring (CQ)**
8. **Documentation (Doc)**

### Handshake (mandatory at each critical step)
1. **Step Chief** asks **Chief Security** (via PM assist) for an **itemized checklist of points** for that Story/step.
2. **Senior** of the step answers those points in their deliverable.
3. **Step QA** asks **Security QA** whether all points were properly addressed (evidence).
4. **Security QA** replies: **confirm PASS** or **further instructions** (bounce to Senior and/or Step Chief).
5. Step QA **does not PASS** to their Chief until Security QA confirms (or Security marks N/A with evidence for truly empty scope — rare; default is real points).
6. **PM owns** scheduling Security into the gate; teams do not silently skip.

### Twice-daily Security research (CEO named times)
- **Chief Security** runs (or assigns) research **daily at 02:00 and 12:00 America/New_York**.
- Analyze recent feedback + **trusted-source web research** for relevant security issues; address findings the same day via briefs/guardrails/Story notes as appropriate.
- Escalate cost/critical findings → COO → CEO.

### PoC note
Scaffold Stories (e.g. O10 host/health) still get a Security checklist (may be thin); they do **not** skip the handshake.


## PM pipeline ownership + visibility (CEO 2026-09-20)

### Channel mirror (closes Ops Brief #2 gap)
- All Story pipeline routing remains **through PM** (CPM owns).
- For every Story status flip (`in-dev` / `cq-pending` / `ready-for-ba-verify` / `done` / Spec·DevPlan·SD·QA·CQ·Doc unlock/close), **CPM or Senior PM** posts a short pointer in the **PM Team channel**: issue link + label + next triad + **evidence pointer** (PR URL, commit SHA, or DOC-FLOW path).
- GitHub remains system of record; the PM channel is the Ops-auditable twin. No “working on it” without evidence.

### Evidence-gated status language
- Bot Manager / CPM / Chiefs must **not** equate intake or chat with delivery.
- Say **in progress** only with a **fresh evidence artifact ≤ ~60–90 minutes** (new commit, undraft PR, or gate PASS file). Otherwise: `assigned` / `waiting on <named owner>` / `stalled since <time>`.

### Unlock evidence = GitHub SoR (DOC-FLOW) — CEO/BM/COO 2026-09-21 post-#31
- **GitHub (`ioaikh/dealoware`) is the system of record** for Spec / Dev Plan / SD unlock evidence.
- KB paths under dealoware-kb (box) may be working drafts; they **do not alone** clear a Spec→Dev Plan or Dev Plan→SD unlock.
- Required unlock evidence: files on `main` under DOC-FLOW mirror (`docs/plans/`, `docs/verification/`, `docs/specs/`, …) **or** an open/merged PR that lands those paths, cited in the issue + PM mirror.
- **One-time exceptions** (KB triad only) require: (1) linked `docs/` catch-up PR or main land, and (2) **PMQA explicit accept** of a one-time exception. Never treat KB=SoR as standing policy.
- Root cause locked after #31: Dev Plan triad existed on KB while GitHub `docs/` 404 — SD unlock paused until PR #33 MERGED + PMQA exception.

### CPM 15-minute delivery watches (mandatory while in pipeline) — CEO flip 2026-09-20 post-#7
- When a Story **enters the development pipeline** (leaves backlog into Spec→…→BA), **CPM is personally responsible** for pushing it through to close.
- At pipeline entry, **CPM arms a 15-minute evidence-based delivery watch on CPM’s own routines** (CPM-hosted cron). Pattern proven on #6/#7; do **not** ask Bot Manager to host it.
- Each tick (CPM): check **what actually landed** (commits/PR/gates), PRIORITY-nudge the owning gate if stalled, keep PM channel mirror current, notify CEO only on material flips or named blockers.
- When the issue is **CLOSED** with `status:done`, **CPM deletes that issue’s 15-minute watch** from CPM’s own routines.
- **Bot Manager audits only:** at pipeline entry, confirm CPM armed the watch; after CLOSE, confirm it was deleted; PRIORITY-nudge CPM if missing. Bot Manager does **not** host the delivery-watch cron. Do not invent a second COO ops-review routine.

### Related
- Ops performance model: `ops/reports/2026-09-20__ops__ops-note__team-performance-check-model.md`

## SA architecture reviews (CEO 2026-09-20; gaps locked 2026-09-20)

After any **significant delivery milestone** (examples: PoC complete, MVP slice complete, major epic closed, host/shape change shipped), **CPM must route an SA Team architecture review** before the next phase unlock. Spec citing old architecture ≠ SA ownership.

### Trigger
- CPM declares the milestone closed (evidence: all in-scope Stories `status:done`, or CEO names the milestone).
- At that moment CPM opens an **SA post-milestone review** task to **Chief Architect** (via PM → SA Team) and arms a normal CPM delivery watch until the review closes.

### SA Team must deliver (Chief Architect owns; Senior Architect executes; Architecture QA verifies)
1. **Intent check** — Confirm delivered work matches the original SA architecture intent for that scope (cite binding SA docs). Explicit PASS / FAIL per major area.
2. **Deviation analysis** — If anything differs from the original plan: document the deviation, why it changed (evidence from Spec/DevPlan/SD/PRs), root cause, and what else must be adjusted for the SD stage just delivered (code, docs, tests, follow-up Stories).
3. **Fit to future architecture** — Compare actual implementation to the planned future architecture (MVP+ / Vn). Itemize:
   - must **change**
   - must **remove**
   - must **add**
   - keep-as-is
4. **Plan update or escalate** — If collected facts are consistent and clear with overall product goals: **update future architecture / roadmap docs** (DOC-FLOW) and hand done-list to Architecture QA → Chief Architect → CPM. If anything is inconsistent, unclear, or SA has questions / suggestions / concerns: **stop and ask CEO** (via CPM → Bot Manager / COO). Do not silently guess.

### Gate
- Next phase / next epic unlock is **HOLD** until Chief Architect PASS (Architecture QA confirm) **or** CEO answers SA’s escalations.
- Security handshake applies on this SA step (critical Architecture gate).
- First instance: **PoC #3–#8 closed 2026-09-20** — SA review **PASS** 2026-09-20 (CEO escalations none). **#18 / MVP unlock remains CEO gate** (not unlocked by this section).

### Proposed review moments (SA-owned timing)
- During the **SA architecture phase** for a Story/epic/phase, **Chief Architect must propose named review moments** (milestones/stages) in the architecture options / stage plan and hand them to **CPM** for scheduling.
- Post-milestone review remains **mandatory** when a significant milestone closes (CPM declares or CEO names) — that is the floor; SA-proposed moments are additive.

### GitHub issue = system of record
- **PM** files **one GitHub issue per SA architecture-review**, linked to the milestone/phase (parent Stories / milestone).
- Labels include `gate:sa-arch-review` (plus stage/milestone and normal status labels).
- PM Team channel mirror on every status flip; when the review issue enters the pipeline, **CPM arms a 15-minute delivery watch** (CPM-hosted) and deletes it on CLOSE — Bot Manager audits arm/delete only.

### CPM end-of-day miss-check
- Daily (America/New_York), **CPM** verifies: (i) any new SA-proposed review moments since last EOD; (ii) each has a GitHub issue with `gate:sa-arch-review`.
- If a moment exists without an issue: PRIORITY-nudge SA/PM to file it the same day.
- Ops 6h sample may flag missing review issues as secondary evidence only — does **not** replace CPM EOD ownership.

### Stage-split for large/long work
- **SA must split** long/large architecture work into **stages**; each stage gets its own architecture review (same deliverable shape: intent / deviations / fit / plan-or-escalate) so corrections apply to remaining stages.
- Next-stage unlock is **HOLD** until Chief Architect PASS (Architecture QA + Security handshake) or CEO answers escalations.

### Related
- Binding PoC SA baselines: `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md`, `architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md`
- COO lock note: `ops/reports/2026-09-20__ops__ops-note__sa-arch-review-process-gaps-lock.md`

## Marketing Team (CEO 2026-09-10)
- Triad: Chief Marketing / Senior Marketing / Marketing QA.
- Aligns GTM/public positioning to **Product** (claims lock via Chief Product before publish).
- First standing work: research Dealoware from CEO-ORIGINAL-BRIEF / PRODUCT-BRIEF / KB+GitHub docs; marketing research for high-attention promotion; itemized recommendations. **No paid promo** without COO→CEO confirm.
- Cost/critical → CEO via COO.

### Marketing daily research cadence (CEO 2026-09-10)
- **At least daily** market/attention research, with **different clock times by weekday** (America/New_York) so sources vary.
- Standing schedule (Bot Manager routine “Dealoware Marketing daily research”): Mon 9:00 · Tue 14:00 · Wed 10:00 · Thu 15:00 · Fri 11:00 · Sat 13:00 · Sun 16:00.
- Cycle: **Chief Marketing** owns → **Senior Marketing** executes → **Marketing QA** verifies → Chief presents via COO to CEO when actionable.
- Product **claims lock** before any public claim; **no paid promo** without COO→CEO confirm.
