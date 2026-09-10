# Dealoware — GitHub issue tracking

| Field | Value |
|-------|-------|
| **Status** | ready for CEO OK (pre-PoC) |
| **Date** | 2026-09-10 |
| **Owner** | CPM / PM Team |
| **Repo** | https://github.com/ioaikh/dealoware |
| **Related** | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` · `meta/DOC-FLOW.md` · CQ gate (ORG-OPS) |

---

## 1. Purpose

Canonical how Dealoware uses **GitHub Issues** for Stories, bugs, CQ refactors, and pipeline status — before any PoC Stories are opened.

**Rule:** Do **not** open PoC Stories until CEO (Ivan) OKs this setup (via Bot Manager).

---

## 2. What we configured

### 2.1 Labels (Dealoware taxonomy)

Keep GitHub defaults for community hygiene; **Dealoware process uses the prefixed labels below.**

**Type (exactly one):**
- `type:story` — BA Story / feature
- `type:bug` — defect
- `type:cq-refactor` — CQ-driven refactor work
- `type:chore` — ops/chore non-feature
- `type:docs` — documentation only

**Stage (exactly one when known):**
- `stage:poc` · `stage:mvp` · `stage:v1` · `stage:v2` · `stage:v3` · `stage:v4` · `stage:v5`

**Pipeline status (exactly one):**
- `status:backlog`
- `status:ready-for-dev`
- `status:in-dev`
- `status:cq-pending` — SD done; awaiting CQ gate
- `status:cq-refactor` — CQ refactor path in flight
- `status:ready-for-ba-verify`
- `status:done`

**CQ gate visibility:**
- `cq:needed` — set when SD marks task complete / entering CQ
- `cq:no-refactor` — Chief CQ confirmed no refactor; may proceed
- `cq:refactor-spec-ready` — refactor spec ready for Dev Plan → SD → QA

### 2.2 Milestones

Open milestones: **PoC**, **MVP**, **V1**, **V2**, **V3**, **V4**, **V5** (aligned to release roadmap).

Assign every Story/bug/cq-refactor to the milestone matching its `stage:*` label.

### 2.3 Issue templates

Under `.github/ISSUE_TEMPLATE/` (repo):
- **Story** — BA intake / ready-for-dev Stories
- **Bug** — defects with repro
- **CQ refactor** — links parent SD issue + refactor spec path under `specs/`

### 2.4 Projects

**Not enabled.** GitHub Projects V2 needs project scopes not present on the current token. Tracking = **labels + milestones + templates**. Escalate to COO → CEO if Ivan wants Projects boards later (scopes/org settings; no spend assumed).

---

## 3. Who opens what

| Who | Opens | Labels (minimum) | Notes |
|-----|-------|------------------|-------|
| **CBA** | Stories (`type:story`) | stage + `status:backlog` (or ready-for-dev when handed to CPM) | Prefer `type:story` (not bare `story`) going forward |
| **CPM / Senior PM** | Pipeline status updates; may open `type:chore` | status:* | Does not invent requirements |
| **CQ (via PM)** | `type:cq-refactor` when refactor needed | stage + `status:cq-refactor` + `cq:refactor-spec-ready` | Spec path in body: `specs/YYYY-MM-DD__cq__spec__refactor-{slug}.md` |
| **QA / anyone** | `type:bug` | stage if known + `status:backlog` | One issue per distinct bug |
| **Doc / PM** | `type:docs` | as needed | DOC-FLOW still owns KB index |

---

## 4. Status flow (happy path)

```
backlog → ready-for-dev → in-dev → cq-pending
    ├─ cq:no-refactor → ready-for-ba-verify → done
    └─ cq:refactor-spec-ready → (Dev Plan → SD → QA tests) → cq-pending/re-check → …
```

1. BA ready Story → CPM schedules → `status:ready-for-dev` → `status:in-dev`.
2. SD task complete → set `status:cq-pending` + `cq:needed`; **CPM assigns CQ Team** (never skip).
3. CQ **no refactor** → `cq:no-refactor`; clear `cq:needed`; proceed Doc / `status:ready-for-ba-verify` as applicable.
4. CQ **refactor** → open/link `type:cq-refactor`; set `cq:refactor-spec-ready` + `status:cq-refactor`; CPM schedules Dev Plan → SD → QA tests-pass; then re-enter CQ as needed → continue.

---

## 5. Links to other systems

| System | Link |
|--------|------|
| Release stages | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` |
| Doc publish | `meta/DOC-FLOW.md` — KB under `/workspace/dealoware-kb/`; GitHub mirror `docs/` |
| CQ gate | ORG-OPS / CQ Team — CPM owns post-SD gate |
| Product | Align goals; conflicts → CPM → Chief Product → CEO |
| Cost/critical | → COO → CEO |

**Non-goals:** MotorMarket / DC4 live systems, inventory, SFTP, test logins.

---

## 6. Verification checklist (setup)

- [x] Dealoware type/stage/status/cq labels created on `ioaikh/dealoware`
- [x] Milestones PoC–V5 created
- [x] Issue templates PR merged (`.github/ISSUE_TEMPLATE/`) — https://github.com/ioaikh/dealoware/pull/1
- [x] Mirrored to `docs/ops/` on GitHub (PR #1); Doc Team index pending
- [ ] CEO OK via Bot Manager before first PoC Story

---

## 7. Claims / process locks

- No inventing Stories or requirements (BA / Product).
- Intermediary claims lock unchanged (Product).
- Apache-2.0 / public repo / hosted AIKnowHow standing (release roadmap L1–L3).
