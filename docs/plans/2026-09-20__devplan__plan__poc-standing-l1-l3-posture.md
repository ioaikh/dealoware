# Dev Plan — PoC Standing L1–L3 posture (chore)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-20  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner — PRIORITY PoC #8 Standing L1–L3  
**Issue:** https://github.com/ioaikh/dealoware/issues/8 · `type:chore`  
**IDs:** **L1 L2 L3** · `stage:poc` · `type:chore` · standing **all stages**  
**DOC-FLOW:** `plans/2026-09-20__devplan__plan__poc-standing-l1-l3-posture.md`  
**Constraints:** Chore only; **no** product inventing; keep separate from #3–#7 feature plans; no Cognito/SSO/prod hosted inventing; no MM/DC4 live systems/inventory/SFTP/test logins; secrets hygiene; PoC **$0**; cost/critical → CPM

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-20__spec__spec__poc-standing-l1-l3-posture.md` | L1 LICENSE; L2 public-repo docs; L3 hosted non-goal; MM/DC4 separation; Security §5; OUT §6; AC §7 |
| Spec QA (Spec-side PASS; Security handshake cleared) | `verification/2026-09-20__spec__verification__poc-standing-l1-l3-posture.md` | Spec-side bind; gate unlocked after Security QA |
| Spec Security PASS | `verification/2026-09-20__security__verification__poc-l1-l3-posture-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan |
| Dev Plan Security checklist (must weave 1–10) | `verification/2026-09-20__security__verification__poc-l1-l3-posture-devplan-checklist.md` | Dev Plan-step handshake points **1–10** |
| Product Later CEO decisions (cite only) | `product/PRODUCT-BRIEF.md` | Apache-2.0; public GitHub; hosted AIKnowHow/Dealoware; MM/DC4 separation — do **not** overwrite CEO original |
| Release roadmap (cite only) | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | L1–L3 standing all stages |
| Issue #8 | https://github.com/ioaikh/dealoware/issues/8 | Binding AC + OUT + CPM unlock · `type:chore` |

**Product alignment:** Later CEO decisions — license Apache-2.0; public repo `https://github.com/ioaikh/dealoware`; hosted platform remains AIKnowHow / Dealoware (free-hosted fork is **not** “the” platform); MotorMarket/DC4 live systems out. Conflicts → escalate PM → Product → CEO. **#3–#7 feature Stories stay separate** — cross-ref only; do **not** rewrite those Specs/plans/AC. No product code beyond SD instructions for docs/LICENSE.

---

## 2. Restated understanding (short)

Executable plan for **SD only** on **chore** issue **#8**: ensure standing posture artifacts exist and are correct — **(L1)** Apache License 2.0 `LICENSE` at repo root (standard SPDX/text; no proprietary inventing); **(L2)** README and/or `docs/` clearly state public GitHub posture pointing to canonical `https://github.com/ioaikh/dealoware`; **(L3)** document that a free-hosted fork is **not** “the” Dealoware platform and that hosted remains **AIKnowHow / Dealoware** (no ownership inventing); note **MotorMarket/DC4** live systems / inventory / SFTP / test logins **OUT**; enforce **secrets hygiene** on all chore deliverables (no secrets in LICENSE/README/docs); PoC **$0** — no Cognito/SSO/prod hosted inventing; keep entirely separate from **#3–#7** feature work. **No product code** in this plan artifact — SD instructions for docs/LICENSE only.

---

## 3. Locked decisions (Spec locks 1–6 → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 1 | L1 | Apache License 2.0 `LICENSE` present and correct at repo root | Steps 1, 8 |
| 2 | L2 | Public-repo posture clear in README/docs pointing to `https://github.com/ioaikh/dealoware` | Steps 2, 8 |
| 3 | L3 | Document non-goal: free-hosted fork ≠ “the” platform; hosted stays AIKnowHow / Dealoware | Steps 3, 8 |
| 4 | Separation | MotorMarket/DC4 live systems, inventory, SFTP, test logins **out** of Dealoware work (noted in docs) | Steps 4, 5, 8 |
| 5 | Chore boundary | No domain API work (#3–#7); no marketing claims; no license/ownership change | Steps 6, 7, 8; Explicit OUT |
| 6 | Spend | PoC **$0** AWS/IdP for this chore; ECS Express sketch remains host-shape note only if docs mention AWS | Steps 5, 7, 8; Cost/critical |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (docs/LICENSE chore only — **no** product feature code).  
**Prerequisite:** Spec triad PASS + Spec Security QA PASS + CPM Dev Plan unlock; this plan QA + Security QA confirm + Chief Dev Planner unlock before SD starts.  
**Hold:** **No** product inventing; **no** merge with #3–#7 feature plans; **no** Cognito/SSO/prod hosted inventing; **no** MM/DC4 live systems/inventory/SFTP/test logins in deliverables.

### Step 1 — L1: Apache-2.0 LICENSE at repo root

Per Spec §1 L1 + Locked #1; Security Dev Plan checklist point **1**.

| Item | Plan lock |
|------|-----------|
| File | Repo root `LICENSE` (canonical path) |
| Text | Apache License 2.0 — standard Apache-2.0 SPDX/text (present and correct) |
| Forbidden | Inventing commercial/proprietary license claims for the public OSS repo; substituting a different license; inventing dual-license without CEO/Product decision |
| Evidence | SD verifies file exists at root and is Apache-2.0 (header / SPDX / full standard text) |

**Tasks:**

1. Check whether root `LICENSE` already exists and is Apache-2.0.
2. If missing or incorrect: add or replace with **standard Apache License 2.0** text only (no proprietary substitute).
3. Do **not** invent copyright-holder or ownership claims beyond what Product Later CEO / existing repo already establish — no license **change** inventing (Spec Locked #5 / OUT).

**Acceptance:**

- [ ] Root `LICENSE` present
- [ ] Content is Apache License 2.0 (correct SPDX/text)
- [ ] No proprietary / commercial license inventing
- [ ] No secrets committed in `LICENSE`

### Step 2 — L2: README/docs point to canonical public GitHub

Per Spec §2 L2 + Locked #2; Security Dev Plan checklist point **2**.

| Item | Plan lock |
|------|-----------|
| Canonical URL | `https://github.com/ioaikh/dealoware` |
| Docs | README and/or `docs/` clearly state **public** GitHub posture with that URL |
| Forbidden | Conflicting “private-only” claims; invented dual-canonical-repo claims |

**Tasks:**

1. Ensure README (and/or a short `docs/` posture note) states the repo is public on GitHub at `https://github.com/ioaikh/dealoware`.
2. Remove or avoid any wording that invents a private-only or second-canonical public repo.
3. Cross-ref only — do **not** rewrite #3–#7 feature docs as part of this chore.

**Acceptance:**

- [ ] README and/or `docs/` state public GitHub posture
- [ ] Canonical URL `https://github.com/ioaikh/dealoware` present and correct
- [ ] No private-only or dual-canonical inventing
- [ ] No secrets committed in README/`docs/` for this chore

### Step 3 — L3: Document free-hosted fork ≠ “the” platform; hosted stays AIKnowHow/Dealoware

Per Spec §3 L3 + Locked #3; Security Dev Plan checklist point **4**.

| Item | Plan lock |
|------|-----------|
| Wording | Document that a **free-hosted fork is not “the” Dealoware platform** |
| Hosted | Hosted platform remains **AIKnowHow / Dealoware** |
| Location | README and/or product/docs posture section — align with `product/PRODUCT-BRIEF.md`; **do not** overwrite `product/CEO-ORIGINAL-BRIEF.md` |
| Forbidden | Inventing new hosted ownership; requiring Cognito/SSO or paid AWS host as #8 deliverable |

**Tasks:**

1. Add or verify a short posture note in README and/or `docs/` stating: free-hosted fork ≠ “the” platform; hosted remains AIKnowHow / Dealoware.
2. Cite Product Later CEO decision / PRODUCT-BRIEF alignment — do **not** invent ownership transfer.
3. Do **not** schedule Cognito/SSO, App Runner, prod ECS, or “official hosted SaaS” provision as #8 work (see Step 7).

**Acceptance:**

- [ ] Non-goal wording present: free-hosted fork is not “the” platform
- [ ] Hosted remains AIKnowHow / Dealoware (documented)
- [ ] No ownership inventing; CEO original not overwritten
- [ ] No IdP / paid-host provision tasks under this chore

### Step 4 — MM/DC4 OUT — no live systems/inventory/SFTP/test logins in chore deliverables

Per Spec §4 Separation + Locked #4; Security Dev Plan checklist point **6**.

| Item | Plan lock |
|------|-----------|
| Standing non-goal | Keep MotorMarket / DC4 **live systems**, inventory, SFTP feeds, and test logins **out** of Dealoware Spec/docs/code for this chore |
| Docs | Explicit separation note in README/docs (cite Product Later CEO decision) |
| Deliverables | Chore PRs must **not** add MM/DC4 live coupling, inventory dumps, SFTP configs, or test logins |

**Tasks:**

1. Add or verify an explicit MM/DC4 separation note in README/docs.
2. Grep/search chore deliverables: no MotorMarket / DC4 live system refs, inventory dumps, SFTP credentials/paths, or test logins introduced by this Story.
3. Document verify result in SD handoff notes.

**Acceptance:**

- [ ] Separation note present in README/docs
- [ ] No MM/DC4 live systems / inventory / SFTP / test logins in chore deliverables
- [ ] Grep/search verify documented in SD handoff

### Step 5 — Secrets hygiene on chore deliverables

Per Spec §4 secrets row + §5 point 3; Security Dev Plan checklist point **3**.

| Item | Plan lock |
|------|-----------|
| Scope | LICENSE, README, `docs/`, and any other files touched by this chore |
| Forbidden | Secrets, API keys, cloud credentials, MM/DC4 test logins, SFTP credentials, inventory dumps |
| Allowed | Placeholders only if examples are needed (prefer none for this chore) |

**Tasks:**

1. Before commit: review every file touched by #8 — ensure no secrets/API keys/cloud creds/MM-DC4 logins/SFTP/inventory dumps.
2. Do not embed credentials in LICENSE/README/`docs/` prose or code fences.
3. If any example would need a secret → use placeholder or omit.

**Acceptance:**

- [ ] No secrets committed in LICENSE / README / docs for this chore
- [ ] No API keys, cloud creds, MM/DC4 logins, SFTP credentials, or inventory dumps in deliverables
- [ ] SD handoff notes confirm secrets hygiene check

### Step 6 — Keep separate from #3–#7 feature work; no product inventing

Per Spec Locked #5 + §5 points 7–8; Security Dev Plan checklist points **7–8**.

| Item | Plan lock |
|------|-----------|
| Scope | Cite issue **#8 AC+OUT only** |
| Forbidden | Marketing claims; new domain APIs; Strategy/AI/settlement/identity-seal inventing under this chore; rewriting #3–#7 Specs/plans/AC |
| Allowed | Cross-refs to #3–#7 as “separate feature Stories”; parallel execution note (issue Dependencies) |

**Tasks:**

1. Implement only L1–L3 docs/LICENSE posture — **no** product feature code, controllers, DTOs, or domain APIs.
2. Do **not** open or rewrite #3–#7 Specs/plans as part of this chore (cross-ref only).
3. Do **not** invent marketing claims or change license/hosted ownership beyond documenting existing CEO posture.

**Acceptance:**

- [ ] Deliverables are docs/LICENSE chore only — no product feature inventing
- [ ] #3–#7 Specs/plans/AC not rewritten; Stories not merged
- [ ] No marketing claims; no domain API work under #8

### Step 7 — PoC $0; no Cognito/SSO/prod hosted inventing

Per Spec Locked #6 + §5 points 5/9; Security Dev Plan checklist points **5** and **9**.

| Item | Plan lock |
|------|-----------|
| Spend | PoC **$0** AWS/IdP for this chore |
| Excluded | Cognito/SSO; prod App Runner/ECS spend; “official hosted SaaS” as #8 deliverables |
| Host note | If docs mention AWS host shape: **ECS Express Mode** sketch only — not deployed by this Story |
| Escalation | If spend proposed → **CPM → COO → CEO** |

**Tasks:**

1. Do **not** schedule Cognito user pools, SSO IdP, App Runner, prod ECS, or paid AWS provision under #8.
2. Do **not** invent “official hosted SaaS” deliverables; L3 docs only (Step 3).
3. Affirm local/$0 — this chore is documentation/LICENSE only.

**Acceptance:**

- [ ] No Cognito/SSO/IdP provision tasks
- [ ] No prod App Runner/ECS spend or “official hosted SaaS” as #8 deliverables
- [ ] PoC remains **$0** for this chore
- [ ] Any spend proposal would escalate CPM → COO → CEO (none scheduled)

### Step 8 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] **L1:** Root `LICENSE` is Apache-2.0 present/correct; no proprietary inventing
- [ ] **L2:** README/docs state public GitHub posture with `https://github.com/ioaikh/dealoware`
- [ ] **L3:** Free-hosted fork ≠ “the” platform; hosted stays AIKnowHow/Dealoware; no ownership inventing
- [ ] **MM/DC4 OUT:** Separation noted; no live systems/inventory/SFTP/test logins in deliverables
- [ ] **Secrets hygiene:** No secrets/API keys/cloud creds/MM-DC4 logins/SFTP/inventory in LICENSE/README/docs
- [ ] **Chore only:** No product inventing; no marketing claims; no domain APIs; separate from #3–#7
- [ ] **Spend:** PoC $0; no Cognito/SSO/prod hosted inventing
- [ ] Nothing from Spec §6 / issue #8 OUT implemented
- [ ] Product conflicts (if any) escalated PM → Product → CEO

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec §1 L1 | Apache-2.0 `LICENSE` at repo root present/correct | Step 1 |
| Spec §2 L2 | Public GitHub posture; canonical `https://github.com/ioaikh/dealoware` | Step 2 |
| Spec §3 L3 | Free-hosted fork ≠ “the” platform; hosted AIKnowHow/Dealoware | Step 3 |
| Spec §4 MM/DC4 separation + secrets | Live systems/inventory/SFTP/test logins OUT; never commit secrets | Steps 4–5 |
| Spec §5 Security Spec checklist 1–10 | Upstream Spec Security bind | Security table §6; all steps |
| Spec §6 Explicit OUT | Marketing; feature inventing; license/ownership change; Cognito/SSO; #3–#7 rewrite; MM/DC4 | Steps 6–7; Explicit OUT |
| Spec §7 / Issue AC: L1 LICENSE Apache-2.0 | Spec §1 | Step 1 |
| Issue AC: L2 Public repo posture in README/docs | Spec §2 | Step 2 |
| Issue AC: L3 Free-hosted fork non-goal; hosted AIKnowHow/Dealoware | Spec §3 | Step 3 |
| Issue AC: Separation MotorMarket/DC4 noted | Spec §4 | Step 4 |
| Issue OUT | Marketing; inventing product features; changing license or hosted ownership | Steps 6–7; Explicit OUT |
| Locked #5 / #6 | Chore boundary; PoC $0 | Steps 6–7 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-spec-qa-confirm.md` (points 1–10 MET)  
**Spec Security binding:** Spec §5 points 1–10

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **L1 LICENSE task** — Plan schedules verify/add Apache-2.0 `LICENSE` at repo root; forbids proprietary license inventing | Step 1 schedules verify/add Apache-2.0 root `LICENSE`; forbids proprietary/commercial inventing | Steps 1, 8; Locked #1 |
| 2 | **L2 Public-repo docs task** — Plan schedules README/docs stating canonical `https://github.com/ioaikh/dealoware`; forbids private-only inventing | Step 2 schedules README/`docs/` public posture + canonical URL; forbids private-only / dual-canonical inventing | Steps 2, 8; Locked #2 |
| 3 | **Public-repo secrets hygiene** — Plan requires chore deliverables never commit secrets, API keys, cloud creds, MM/DC4 test logins, SFTP credentials, or inventory dumps | Step 5 requires secrets hygiene on LICENSE/README/docs; Step 4 forbids MM/DC4 secrets; Step 8 self-verify | Steps 4–5, 8; Locked #4 |
| 4 | **L3 Hosted non-goal docs** — Plan schedules documentation that free-hosted fork ≠ “the” platform; hosted remains AIKnowHow/Dealoware — without ownership inventing or IdP provision tasks | Step 3 schedules L3 posture wording; forbids ownership inventing and IdP provision under #8 | Steps 3, 7–8; Locked #3 |
| 5 | **No hosted-platform inventing** — Plan excludes Cognito/SSO, prod App Runner/ECS spend, and “official hosted SaaS” as #8 deliverables; PoC runtime stays local/$0 | Step 7 excludes Cognito/SSO/prod spend/hosted SaaS; Cost/critical affirms $0 | Steps 7–8; Locked #6; Cost/critical; Explicit OUT |
| 6 | **MotorMarket / DC4 separation** — Plan explicitly excludes MM/DC4 live systems, inventory, SFTP, and test logins from Dealoware chore work | Step 4 schedules separation note + grep verify; forbids live coupling in deliverables | Steps 4–5, 8; Locked #4; Explicit OUT |
| 7 | **No inventing product features** — Plan cites #8 AC+OUT only: no marketing claims; no new domain APIs; no Strategy/AI/settlement/identity-seal under this chore | Step 6 cites #8 AC+OUT only; forbids marketing/domain APIs/Strategy/AI/settlement/identity-seal inventing | Steps 6, 8; Locked #5; Explicit OUT |
| 8 | **Cross-story non-merge** — Plan keeps L1–L3 separate from #3–#7 feature plans except cross-refs; does not rewrite those Stories | Step 6 keeps chore separate from #3–#7; cross-ref only; Constraints header | Steps 6, 8; Locked #5; Explicit OUT |
| 9 | **Cost / spend guardrail** — Plan affirms PoC **$0** AWS/IdP for this chore; cost/critical → COO → CEO if spend proposed | Step 7 + Cost/critical §8 affirm $0; escalate CPM → COO → CEO if spend proposed | Steps 7–8; Locked #6; Cost/critical |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms these points | See handshake note below. Done-list for Dev Plan QA requires Security QA confirm before PASS | Handshake note; Done-list for Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-20__security__verification__poc-l1-l3-posture-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Security QA confirms.**

---

## 7. Explicit OUT

Mirror Spec §6 / issue #8 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Marketing claims | Issue OUT |
| Inventing product features / domain APIs | #3–#7 stay separate |
| Changing license or hosted-platform ownership | Document existing posture only — not invent change |
| Cognito/SSO / paid AWS host as #8 deliverable | $0 PoC |
| Strategy / AI / settlement / identity-seal inventing | Wrong Story |
| MotorMarket/DC4 live coupling or secrets in repo | Standing separation |
| Rewriting #3–#7 Specs/plans/AC | Cross-ref only |
| Prod App Runner / ECS spend / “official hosted SaaS” | Not #8 deliverables |
| AWS account create / IaC apply / IdP provision | No spend |

---

## 8. Cost/critical

**#8 must not procure AWS / Cognito / SSO / IdP spend.** Any paid AWS provision, Cognito/SSO/IdP spend, or other spend proposal → escalate **CPM → COO → CEO**. PoC remains **$0** for this chore until that path clears. This plan schedules **no** AWS account create, IaC apply, Cognito user-pool, App Runner, prod ECS, or hosted-SaaS tasks. If docs mention host shape, **ECS Express Mode** remains **sketch only**. Cost/critical → CPM.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-20__devplan__plan__poc-standing-l1-l3-posture.md`
- [ ] Spec coverage §§1–6 + issue #8 AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] **Chore only** — L1 LICENSE + L2 README/docs + L3 hosted non-goal + MM/DC4 separation; **no product inventing**
- [ ] Keep separate from **#3–#7** feature plans (cross-ref only)
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6)
- [ ] No Cognito/SSO/prod hosted inventing; PoC **$0**
- [ ] No MM/DC4 live systems/inventory/SFTP/test logins in scheduled deliverables
- [ ] Secrets hygiene required on chore deliverables
- [ ] Explicit OUT respected (Spec §6 / issue #8 OUT)
- [ ] No product code in this artifact (SD instructions for docs/LICENSE only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock (+ CPM per ops).

---

## 10. Done-list for SD (after plan QA PASS + Chief confirm)

- [ ] Step 1: Ensure root `LICENSE` is Apache-2.0 present/correct (no proprietary inventing)
- [ ] Step 2: README/docs state public GitHub URL `https://github.com/ioaikh/dealoware`
- [ ] Step 3: README/docs state hosted non-goal + AIKnowHow/Dealoware (no ownership inventing)
- [ ] Step 4: README/docs note MM/DC4 separation; no live systems/inventory/SFTP/test logins in deliverables
- [ ] Step 5: Secrets hygiene — no secrets committed in LICENSE/README/docs
- [ ] Step 6: Chore only — no product inventing; keep separate from #3–#7
- [ ] Step 7: PoC $0; no Cognito/SSO/prod hosted inventing
- [ ] Step 8: Complete self-verify checklist before handoff
- [ ] Do not implement Spec §6 / issue #8 OUT
- [ ] Hand off to CQ gate (never skip CQ)
