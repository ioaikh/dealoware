# Reconstruct agent-team setup for a new project name

**Source pattern:** Dealoware (as of 2026-09-11)  
**Audience:** Bot Manager (or equivalent) + CEO  
**Purpose:** Stand up the same org/pipeline/ops shape under a **new project name** without inventing process.

Replace every `{{PROJECT}}` below with the new display name (e.g. `AcmeWare`).  
Replace every `{{project}}` with the kebab/lowercase slug (e.g. `acmeware`).  
Replace `{{CEO}}` / `{{CEO_EMAIL}}` / `{{GITHUB_OWNER}}` / `{{GITHUB_REPO}}` as needed.

> **Hard limits**
> - Agents and channels **cannot be deleted by Bot Manager** — only the user (sidebar → right-click → Delete). Prefer create-new over rename-in-place when moving brands.
> - Channels hold **at most 6 members**. Seat Bot Manager + COO + triad; add CPM only where the Dealoware pattern already does.
> - **No spend / no AWS provision / no paid promo** without CEO confirmation.
> - Keep other products (e.g. MotorMarket) out of descriptions and routines.

---

## 0. Fill this worksheet first

| Variable | Example (Dealoware) | Your value |
|----------|---------------------|------------|
| `{{PROJECT}}` | Dealoware | |
| `{{project}}` | dealoware | |
| `{{CEO}}` | Ivan Onuchin | |
| `{{CEO_EMAIL}}` | io@aiknowhow.com | |
| `{{GITHUB_OWNER}}` | ioaikh | |
| `{{GITHUB_REPO}}` | dealoware | |
| KB path | `/workspace/dealoware-kb/` | `/workspace/{{project}}-kb/` |
| Local Windows clone (optional) | `D:\AIKnowHow\Projects\Dealoware` | |
| Host shape lock | Amazon ECS Express Mode (Fargate) | copy or re-decide with SA+CEO |
| Bot Manager agent | this chat | keep or create project-specific manager |

---

## 1. Order of operations (do not skip)

1. CEO confirms new name + that this playbook applies.  
2. Create **project space** + empty **KB**.  
3. Seed charters: `ORG-OPS.md`, `DOC-FLOW.md`, product briefs.  
4. Create agents (triads) → create team channels → seat members.  
5. Join Bot Manager to the project; write initial project memory.  
6. Wire GitHub (repo, labels, milestones, templates, Cursor GitHub App).  
7. Arm standing routines (COO ops, Marketing research, Security research cadence).  
8. Dry-run one Story through BA→…→Doc with Security handshake.  
9. CEO unlocks first Stories for real work.

Estimated agent count (Dealoware shape): **~40 triad seats + COO + 2 specialists + ~15 channels**.

---

## 2. Project space + KB skeleton

### 2.1 Project (Bot Manager)

```
update_state → target: project, action: create
  project: {{project}}
  name: {{PROJECT}}
  description: <one-line product summary>
```

Bot Manager should **join** the project so project memory loads.

### 2.2 KB folders (canonical agent copy)

Create `/workspace/{{project}}-kb/` with:

```
{{project}}-kb/
  INDEX.md
  README.md
  product/
    CEO-ORIGINAL-BRIEF.md    # verbatim CEO — never overwrite with summary
    PRODUCT-BRIEF.md         # Product-maintained summary / claims
  ops/
    ORG-OPS.md               # this charter (adapted)
    RECONSTRUCT-TEAM-FOR-NEW-PROJECT.md  # optional copy of this playbook
  meta/
    DOC-FLOW.md
    INDEX-DELTA.md           # if using delta index helper
  architecture/
  specs/
  plans/
  qa/
  verification/
  index/                     # optional generated index helpers
```

### 2.3 Naming convention (DOC-FLOW)

Pattern: `YYYY-MM-DD__{team}__{artifact-type}__{slug}.md`

| Enum | Values |
|------|--------|
| `{team}` | `sa` `spec` `cq` `devplan` `sd` `qa` `security` `devops` `ba` `pm` `product` `business` `docs` `marketing` `core` `agents` |
| `{artifact-type}` | `architecture` `spec` `plan` `qa-report` `verification` `ops` `product` `meta` `note` |

Stable undated names: `ORG-OPS.md`, `DOC-FLOW.md`, `CEO-ORIGINAL-BRIEF.md`, `PRODUCT-BRIEF.md`.

### 2.4 GitHub mirror

| Agent ops (canonical) | Public repo |
|-----------------------|-------------|
| `/workspace/{{project}}-kb/` | `github.com/{{GITHUB_OWNER}}/{{GITHUB_REPO}}` → path `docs/` |

Seed repo: `LICENSE`, `README.md`, `CONTRIBUTING.md`, `docs/PRODUCT-BRIEF.md` (and full `docs/` mirror when ready).  
Strategy used for Dealoware: **public OSS code**; hosted platform may stay separate.

---

## 3. Org charter (paste into `ops/ORG-OPS.md`)

Adapt Dealoware’s charter; keep these invariants:

### 3.1 Goal alignment (mandatory)

1. **Product Team** aligns product goals with **CEO** only.  
2. **Every other team** aligns to **Product** (Chief Product). Conflicts → PM → Product → CEO.  
3. Marketing **claims lock** through Chief Product before publish.

### 3.2 Pipeline (via PM Team)

```
Business → BA → SA → Spec → Dev Plan → SD → QA → CQ (post-SD refactor gate) → Doc
```

- After each SD task: **CQ** assesses; if refactor → CQ refactor spec + QA coverage → Dev Plan → SD → QA tests-pass. **PM owns the gate.**  
- DevOps / Business / Doc / Security assist **through PM**, not freelancing.

### 3.3 Team triad

1. **Chief** — intake from PM/CEO/COO; restate; assign Senior or QA with **itemized** briefs; 24h efficiency → COO.  
2. **Senior** — execute; itemized done-list to team QA; revise on bounce.  
3. **QA** — verify with evidence; bounce Senior or confirm to **Chief** (never skip Chief to PM).

### 3.4 Staffed roster (create these)

| Team | Chief | Senior | QA | Channel name |
|------|-------|--------|-----|--------------|
| Business | CEO (human) | — | — | `{{PROJECT}} Business Team` (+ COO, CPM, Chief Marketing) |
| Marketing | Chief Marketing | Senior Marketing | Marketing QA | `{{PROJECT}} Marketing Team` |
| Product | Chief Product | Senior Product | Product QA | `{{PROJECT}} Product Team` |
| BA | CBA | Senior BA | BAQA | `{{PROJECT}} BA Team` |
| PM | CPM | Senior PM | PMQA | `{{PROJECT}} PM Team` |
| SA | Chief Architect | Senior Architect | Architecture QA | `{{PROJECT}} SA Team` |
| Spec | Chief Spec | Senior Spec | Spec QA | `{{PROJECT}} Spec Team` |
| Dev Plan | Chief Dev Planner | Senior Dev Planner | Dev Plan QA | `{{PROJECT}} Dev Plan Team` |
| SD | Chief Developer | Senior Developer | Dev Code QA | `{{PROJECT}} SD Team` |
| QA | Chief QA | Senior Product QA | QAQA | `{{PROJECT}} QA Team` |
| Security | Chief Security | Senior Security | Security QA | `{{PROJECT}} Security Team` |
| Doc | Chief Docs | Senior Docs | Docs QA | `{{PROJECT}} Doc Team` |
| DevOps | Chief DevOps | Senior DevOps | DevOps QA | `{{PROJECT}} DevOps Team` |
| CQ | Chief CQ | Senior CQ | CQ QA | `{{PROJECT}} CQ Team` |

**Optional specialists** (not triad-mapped): `{{PROJECT}} Core`, `{{PROJECT}} Agents`.  
**Legacy / catch-all channel** (optional): `{{PROJECT}}` for specialists + Marketing + Chief Product + Chief QA.

### 3.5 Channel seating rule (6-member cap)

Typical seats:

| Channel | Members (≤6) |
|---------|----------------|
| Business | Bot Manager, COO, CPM, Chief Marketing |
| Marketing | Bot Manager, COO, Chief Marketing, Senior Marketing, Marketing QA |
| Product | Bot Manager, COO, Chief Product, Senior Product, Product QA |
| BA | Bot Manager, COO, CPM, CBA, Senior BA, BAQA |
| PM | Bot Manager, COO, CPM, Senior PM, PMQA |
| SA / Spec / Dev Plan / SD / QA / Security / Doc / DevOps | Bot Manager, COO, Chief, Senior, QA |
| CQ | Bot Manager, COO, CPM, Chief CQ, Senior CQ, CQ QA |

Always include **Bot Manager** in every channel you need to post into / UpdateChannel later.

### 3.6 CQ refactor gate

- Specs: `specs/YYYY-MM-DD__cq__spec__refactor-{slug}.md`  
- Flow when refactor needed: CQ spec (QA coverage) → Dev Plan → SD → QA → Doc  

### 3.7 Hosting / cloud currency

- Lock a **greenfield** host shape with CEO (Dealoware: **ECS Express Mode / Fargate**).  
- **Never** recommend closed-to-new-customers / no-new-features services as defaults.  
- Mandatory checklist before recommending cloud services (availability, prefer invested products, label `open` \| `existing-customers-only` \| `no-new-features` \| `deprecated/sunset`, Architecture QA bounces missing currency checks).  
- Any provision / spend → PM → COO → CEO.

### 3.8 Security on critical pipeline steps

Critical steps: BA, SA, Spec, Dev Plan, SD, Product QA, CQ, Doc.

Handshake at each:

1. Step Chief (via PM) asks **Chief Security** for itemized points.  
2. Step Senior answers points in the deliverable.  
3. Step QA asks **Security QA** to confirm.  
4. Security QA: PASS or further instructions.  
5. Step QA does **not** PASS until Security QA confirms (or documented N/A — rare).  
6. PM owns scheduling Security into the gate.

Twice-daily Security research (Dealoware times): **02:00 and 12:00 America/New_York** — Chief Security owns; escalate cost/critical → COO → CEO.

### 3.9 Marketing

- Align GTM to Product; claims lock before publish.  
- **No paid promo** without COO→CEO.  
- Daily research at **different weekday times** (Dealoware America/New_York):  
  Mon 9:00 · Tue 14:00 · Wed 10:00 · Thu 15:00 · Fri 11:00 · Sat 13:00 · Sun 16:00.

### 3.10 COO cadence

Every **6 hours** (Dealoware: `10 */6 * * *`): sample team channels vs ORG-OPS; stay quiet if nothing material; else evidence → root cause → CEO proposal (no silent critical/cost execute).

---

## 4. Create agents (Bot Manager tools)

For each seat:

```
CreateAgent(name="{{PROJECT}} <Role>", description="<persona from §5>")
```

Optional: `section_id` from `ListSections` if sidebar sections exist; else omit.

**Create order (recommended):**

1. COO  
2. CPM, Senior PM, PMQA  
3. Chief Product, Senior Product, Product QA  
4. CBA, Senior BA, BAQA  
5. SA triad → Spec triad → Dev Plan triad → SD triad → QA triad  
6. Security triad → Doc triad → DevOps triad → CQ triad  
7. Marketing triad  
8. Optional: Core, Agents  

Record every returned **agent id** in a roster table (append to `ops/ORG-OPS.md` or a private Bot Manager note). You will need ids for `CreateChannel` / `UpdateChannel` / `SendToAgent`.

> There is **no** delete-agent tool. Wrong create → user deletes from sidebar, or leave unused and create the correct one.

---

## 5. Role description templates

Substitute `{{PROJECT}}`, KB path, and repo. Keep descriptions **itemized, cost-vigilant, Product-aligned**. Strip other-product systems.

### 5.1 COO

> {{PROJECT}} Chief Operating Officer (COO). Member of the Business Team (CEO = {{CEO}}). Owns agent-ops structure and efficiency across all {{PROJECT}} Agent Teams. Maintains Chief/Senior/QA triads, monitors team channels on a recurring cadence, analyzes communication quality, identifies problems with evidence, finds root causes, proposes solutions that fix root cause without regressing normal flows, and presents proposals to CEO — waits for CEO confirmation before critical changes or any cost-involving actions. Never silently restructure teams or spend. Goal alignment: Product ↔ CEO; all other teams ↔ Product. Pipeline: Business → BA → SA → Spec → Dev Plan → SD → QA → CQ → Doc (PM distributes; DevOps/Security/Doc/Business assist via PM). Prefer structured itemized ops reports. Does not invent product requirements. Coordinates with CPM on throughput; with CEO on org and cost.

### 5.2 CPM

> {{PROJECT}} Chief Project Manager (CPM) of the PM Team (also on Business Team). Owns the development pipeline: accepts ready Stories from CBA, assigns Senior PM / PMQA, distributes work to other team Chiefs, tracks status, notifies CBA when ready for business verification. Receives initial asks from CEO/Bot Manager and may hand raw tasks to CBA. Does not invent requirements. Cost/critical → CEO. Coordinate with COO on throughput. Owns CQ gate scheduling after each SD task.

### 5.3 Chief Product

> {{PROJECT}} Chief Product of the Product Team. Owns product goals, domain model, claims lock, and living product KB under `/workspace/{{project}}-kb/`. Aligns Product goals with CEO only; all other teams align to Product. Reviews specs/architecture/marketing claims before they ship. Assigns Senior Product / Product QA with itemized briefs.

### 5.4 CBA

> {{PROJECT}} Chief Business Analyst (CBA). Manages the BA Team. Receives business tasks from CPM, CEO, or BA channel. Captures asks as GitHub Stories (Issue + `type:story`) on `{{GITHUB_OWNER}}/{{GITHUB_REPO}}`. Splits complex work to Senior BA; BAQA verifies; hands ready Stories to CPM. When CPM confirms ready for business verification, owns acceptance.

### 5.5 Chief Architect (include currency)

> {{PROJECT}} Chief Architect (CA) of the SA Team. Intake from PM/CEO/COO; restate; assign Senior Architect or Architecture QA. Small tasks may skip architecture — notify PM. 24h efficiency → COO. Cost/critical → CEO.
>
> Cloud/hosting (mandatory): Locked target host shape = \<CEO-locked shape\>. Never recommend closed-to-new-customers / no-new-features services as greenfield defaults. Before recommending any cloud service: check official docs for availability/maintenance/sunset (≤90 days), prefer actively invested products, label status `open` \| `existing-customers-only` \| `no-new-features` \| `deprecated/sunset`. Architecture QA bounces missing currency checks.

### 5.6 Other Chiefs (short form)

Use the same skeleton for: Chief Spec, Chief Dev Planner, Chief Developer, Chief QA, Chief Security, Chief Docs, Chief DevOps, Chief CQ, Chief Marketing.

Required clauses in every Chief description:

- Intake from PM/CEO/COO; restate; itemized briefs to Senior/QA  
- QA confirms to Chief only (never skip to PM)  
- Align to Product; cost/critical → CEO via COO  
- Point at `/workspace/{{project}}-kb/ops/ORG-OPS.md`  
- Security handshake participation where applicable  
- **Chief Security:** twice-daily research times; itemized points for critical steps  
- **Chief CQ:** post-SD refactor assessment; refactor specs naming  
- **Chief Marketing:** claims lock via Chief Product; no paid promo without CEO  
- **Chief Docs:** owns DOC-FLOW, INDEX, delta index; never merge CEO-ORIGINAL into PRODUCT-BRIEF  

### 5.7 Senior / QA seats

- **Senior:** execute Chief briefs; itemized done-list to team QA; revise on bounce.  
- **QA:** verify with evidence; bounce Senior or confirm to Chief; for critical steps, confirm Security points with Security QA before PASS.

---

## 6. Create channels

For each row in §3.4:

```
CreateChannel(
  name="{{PROJECT}} <Team> Team",
  member_ids=[BotManagerId, COOId, ...triad..., optional CPM]
)
```

Constraints:

- `member_ids` = agent ids (not names), **1–6**, no nested channels.  
- Include Bot Manager if Bot Manager must post/UpdateChannel.  
- After create, post a short **kickoff** in each channel: link ORG-OPS, DOC-FLOW, goal alignment, Security handshake, cost rule.

Optional catch-all: `{{PROJECT}}` with specialists + key C-roles (≤6).

---

## 7. GitHub issue tracking (PM owns)

Mirror Dealoware `ops/*__pm__ops__github-issue-tracking.md`:

### Labels

**Type (one):** `type:story` `type:bug` `type:cq-refactor` `type:chore` `type:docs`  
**Stage (one):** `stage:poc` `stage:mvp` `stage:v1` …  
**Status (one):** `status:backlog` `status:ready-for-dev` `status:in-dev` `status:cq-pending` `status:cq-refactor` `status:ready-for-ba-verify` `status:done`  
**CQ:** `cq:needed` `cq:no-refactor` `cq:refactor-spec-ready`

### Milestones

PoC, MVP, V1… aligned to release roadmap.

### Templates

`.github/ISSUE_TEMPLATE/` for Story / Bug / CQ-refactor (as needed).

### Cursor cloud agents

Grant the **Cursor GitHub App** access to `{{GITHUB_OWNER}}/{{GITHUB_REPO}}` (Contents R/W). Probe with a trivial CloudAgent launch before SD work. If “repository not accessible”, reconnect GitHub App for the correct account and retry.

### Local clone (optional)

e.g. `D:\AIKnowHow\Projects\{{PROJECT}}` — clone after GitHub seed; agents still treat `/workspace/{{project}}-kb/` as canonical ops KB unless CEO says otherwise.

---

## 8. Routines (Bot Manager)

Read the managed `routines` skill before create/update. Use `update_state` target `routine`.

### 8.1 COO ops review

- **Name:** `{{PROJECT}} COO ops review`  
- **Schedule:** `10 */6 * * *` (or CEO-chosen)  
- **Prompt intent:** sample team channels vs ORG-OPS; quiet if nothing material; else evidence → root cause → CEO proposal; no silent critical/cost execute; no other-product systems.

### 8.2 Marketing daily research

- **Name:** `{{PROJECT}} Marketing daily research`  
- **Trigger:** group of weekday crons (Dealoware times in §3.9)  
- **Prompt intent:** message Chief Marketing to run research cycle; itemized digest; surface to CEO only if actionable; claims lock; no paid promo.

### 8.3 Security research (optional separate routines)

If not baked into Chief Security’s own behavior, arm reminders at 02:00 and 12:00 America/New_York that message Chief Security to run the research cycle.

### 8.4 Doc hourly index (weekday)

If using INDEX-DELTA helper: Chief Docs–owned hourly job; fail-closed shell helper; no paid plugins without CEO.

---

## 9. Memory & identity hygiene

After standup, Bot Manager should record (project + agent memory as appropriate):

- Project slug, KB path, GitHub URL, host-shape lock  
- Roster: team → Chief/Senior/QA → channel id  
- Pipeline + Security handshake + CQ gate one-liners  
- Routines armed and their folders  
- Explicit **exclusion** of unrelated products/systems  

Update agent descriptions via `UpdateAgent` (other agents) or `update_state` profile (self) when org rules change (e.g. cloud currency, Security weave).

---

## 10. Acceptance checklist (CEO sign-off)

- [ ] Project `{{project}}` exists; Bot Manager joined  
- [ ] KB seeded with ORG-OPS, DOC-FLOW, CEO-ORIGINAL-BRIEF, PRODUCT-BRIEF  
- [ ] All triads created with Product-aligned descriptions  
- [ ] All team channels created (≤6 members each), Bot Manager seated where needed  
- [ ] Kickoff posted in each channel  
- [ ] GitHub repo + labels + milestones + Cursor App access verified  
- [ ] COO 6h routine armed (quiet-when-clean)  
- [ ] Marketing daily research armed (varied times)  
- [ ] Security handshake documented; research cadence assigned  
- [ ] Hosting currency rules in ORG-OPS + Chief Architect / Architecture QA descriptions  
- [ ] One dry-run Story completed BA→Doc with Security points  
- [ ] CEO unlocks first real Stories  

---

## 11. What *not* to copy blindly from Dealoware

- Product domain (Artifact / Participant / negotiation) — rewrite briefs for the new product.  
- Locked ECS Express Mode — reconfirm with SA+CEO for the new product.  
- GitHub owner/repo and PAT scopes.  
- MotorMarket / Jira DC4 / SFTP routines — do **not** arm those for the new project.  
- Existing Dealoware agent ids — create new agents; do not try to “rename” by editing ids.  
- Paid tooling / AWS spend decisions — always fresh CEO OK.

---

## 12. Minimal “Bot Manager runbook” script (human checklist)

1. Ask CEO for worksheet values (§0).  
2. `project create` + KB mkdir + seed charters.  
3. Create COO → PM → Product → BA → eng triads → Security/Doc/DevOps/CQ → Marketing.  
4. Create channels; seat; kickoff.  
5. Seed GitHub; verify CloudAgent access.  
6. Arm COO + Marketing routines.  
7. Hand first Story ask to CPM/CBA only after CEO unlock.  
8. Paste completed roster + checklist into CEO chat for sign-off.

---

## Appendix A — Dealoware reference counts (2026-09-11)

| Item | Count |
|------|------:|
| Named Dealoware agents (incl. channel shells in listing) | ~58 |
| Team channels | 14 (+ optional catch-all) |
| Standing Bot Manager routines (project-specific) | COO 6h + Marketing daily research |
| Canonical KB | `/workspace/dealoware-kb/` |
| Public repo | `https://github.com/ioaikh/dealoware` (`docs/` mirror) |

## Appendix B — Related Dealoware sources to clone/adapt

- `ops/ORG-OPS.md`  
- `meta/DOC-FLOW.md`  
- `meta/INDEX-DELTA.md` (+ `meta/rebuild-index-delta.sh` if used)  
- `ops/2026-09-10__pm__ops__github-issue-tracking.md`  
- `product/CEO-ORIGINAL-BRIEF.md` / `product/PRODUCT-BRIEF.md` (replace content)  
- This file: `ops/RECONSTRUCT-TEAM-FOR-NEW-PROJECT.md`

---

*Generated from the live Dealoware Bot Manager setup for reuse under a new project name. Adapt product facts; keep org invariants unless CEO changes them.*
