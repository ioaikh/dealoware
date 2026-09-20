# Ops note — SA architecture-review process gaps (COO lock)

**Date:** 2026-09-20  
**Author:** Dealoware COO  
**Status:** LOCKED for Bot Manager ORG-OPS write (CEO ask via Bot Manager)  
**No spend. No MotorMarket.**

## 1. ORG-OPS current section — PASS/FAIL

**Section:** `## Post-milestone SA architecture review (CEO 2026-09-20)` in `ops/ORG-OPS.md`

| Check | Verdict | Notes |
|-------|---------|-------|
| Post-milestone trigger (CPM routes after significant milestone) | **PASS** | Correct floor; first instance PoC #3–#8 noted |
| Deliverable shape (intent / deviations / fit / plan-or-escalate) | **PASS** | Complete; Security handshake called out |
| Next-phase HOLD until CA PASS | **PASS** | Matches executed first instance |
| Coverage of Q1–Q4 (propose moments / GitHub issue / EOD / stage-split) | **FAIL (incomplete)** | Additive delta required — do **not** delete the post-milestone floor |

**(A)** Section is correct/complete **as the post-milestone floor**.  
**(B)** Ops wording fixes = **additive** subsections below (no rewrite of §§ Trigger / SA Team must deliver / Gate).

---

## 2. Q1–Q4 lock

### Q1 — When to engage SA for architecture review?
**ACCEPT with Ops edits**

- **Keep:** Post-milestone CPM-triggered review (current ORG-OPS) as **mandatory floor**.
- **Add:** During the **SA architecture phase** for a Story/epic/phase, **Chief Architect must propose key review moments** (named milestones/stages) in the architecture options / stage plan — not only after CPM notices a closed milestone.
- Rationale: closes the SA-idle gap (Specs citing old baselines); SA owns architecture timing; CPM still owns routing.

### Q2 — How do we not miss the review step?
**ACCEPT**

- **PM schedules each SA architecture-review as a GitHub issue** on `ioaikh/dealoware` (system of record), linked to the milestone/phase (parent Stories / milestone label).
- Suggested labels: `type:chore` (or `type:story`) + `gate:sa-arch-review` + stage/milestone labels; status labels same as pipeline.
- PM channel mirror on every flip (CEO 2026-09-20 visibility rule).
- When the review issue enters pipeline, **CPM arms a 15m delivery watch** (same CPM-hosted rule); delete on CLOSE.
- Rationale: chat ≠ delivery; GitHub issue is the hard artifact.

### Q3 — Who ensures we did not miss it (including “never scheduled”)?
**ACCEPT with Ops edits**

- **CPM owns an end-of-day check** (America/New_York):  
  (i) Did SA propose any new phase/review moments since last EOD?  
  (ii) Is a GitHub issue filed for each proposed architecture review?
- Failure mode = never filing the issue — EOD check closes that.
- **Secondary (not a replacement):** Ops 6h sample may flag missing `gate:sa-arch-review` issues as evidence in ops reports.
- Bot Manager continues to audit **watch arm/delete only** — does not own EOD content.
- Rationale: accountability stays with CPM (pipeline owner); Ops audits, does not host.

### Q4 — Large/long tasks?
**ACCEPT**

- **SA must split** long/large architecture work into **stages**, each with its own architecture review so corrections apply to remaining stages.
- Next-stage unlock **HOLD** until Chief Architect PASS (Architecture QA + Security handshake on that SA step) or CEO answers escalations.
- Same deliverable shape as post-milestone review (intent / deviations / fit / plan-or-escalate).
- Rationale: prevents one late mega-review after irreversible delivery.

---

## 3. Draft ORG-OPS delta (approve Bot Manager to write)

Insert **after** the existing Gate subsection (before `### Related`), or as sibling subsections under the same `## Post-milestone SA architecture review` heading — prefer renaming the H2 to:

`## SA architecture reviews (CEO 2026-09-20; gaps locked 2026-09-20)`

Keep existing Trigger / Deliverable / Gate text. **Add:**

### Proposed review moments (SA-owned timing)
- During SA architecture phase, **Chief Architect proposes named review moments** (milestones/stages) in the architecture options / stage plan and hands them to **CPM** for scheduling.
- Post-milestone review remains **mandatory** when a significant milestone closes (CPM declares or CEO names).

### GitHub issue = system of record
- **PM** files one GitHub issue per SA architecture-review, linked to the milestone/phase.
- Labels include `gate:sa-arch-review`. PM channel mirror + CPM 15m watch apply like other in-pipeline work.

### CPM end-of-day miss-check
- Daily (America/New_York), **CPM** verifies: (i) new SA-proposed review moments captured; (ii) each has a GitHub issue. PRIORITY-nudge SA/PM if a moment exists without an issue.

### Stage-split for large/long work
- **SA** splits large/long work into stages; each stage has its own architecture review before next-stage unlock (HOLD until CA PASS or CEO answers).

Also update first-instance line: PoC #3–#8 review **PASS** 2026-09-20 (CEO escalations none); #18 unlock remains CEO gate.

---

## 4. Follow-on updates (Ops Executive may draft; COO presents)

| Item | Needed? | Owner |
|------|---------|-------|
| CPM agent profile — EOD SA-review miss-check | **Yes** | Bot Manager / UpdateAgent after lock |
| CPM EOD routine (`@daily` local evening) | **Yes** | Bot Manager create; CPM-hosted |
| SA brief / Chief Architect intake template — “proposed review moments” + stage-split required | **Yes** | Chief Architect + Ops Executive draft |
| PMQA bounce rule — bounce next-phase unlock without closed `gate:sa-arch-review` when required | **Yes** | CPM brief to PMQA |
| Ops performance model complementary checklist | Optional | Ops Executive on next Brief |

---

## 5. Decision for Bot Manager

- **COO APPROVES** Bot Manager to write the ORG-OPS delta from §3 (no spend).
- After write: mirror to GitHub `docs/ops/ORG-OPS.md` per DOC-FLOW; report to CEO.
- Do **not** unlock #18 in this change — still CEO unlock.
