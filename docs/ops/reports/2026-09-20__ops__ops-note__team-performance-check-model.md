# Ops note — How Ops checks Dealoware team / member performance

| Field | Value |
|-------|-------|
| **Date** | 2026-09-20 (~11:35 ET) |
| **Author** | Dealoware COO (Ops Team Chief) |
| **Status** | **LOCKED** — CEO requirement via Bot Manager 2026-09-20 (widget ack skipped; treat as locked review content) |
| **Charter** | `ops/ORG-OPS.md` |
| **Related** | Brief #1 baseline `ops/reports/2026-09-19__ops__ops-report__brief-1-baseline.md` (Ops QA PASS) |
| **Spend** | PoC **$0** — no critical/cost execute without CEO OK |

---

## Short answer

Ops performance checks are **systematic, not ad hoc**. Every Ops review (~6h cadence) uses this model:

**COO briefs → Ops Executive reviews (message-by-message where traffic exists) → Ops QA verifies → COO presents material findings/proposals to CEO.**

Bot Manager owns `dealoware-coo-ops-review` automation; Ops does **not** create a duplicate COO routine.

---

## Standing process (who / how often)

| Step | Owner | Cadence | Output |
|------|-------|---------|--------|
| Itemized Ops brief | **COO** | ~every 6h | Brief id, window since last review, teams in scope, standing watches |
| Full communication + gate review | **Ops Executive** | Per brief | Ops report + done-list under `ops/reports/` |
| Verify evidence | **Ops QA** | Per brief | PASS/BOUNCE to **COO only** (never skip to CEO/Bot Manager) |
| Present / propose | **COO** | When material | Structured pack → CEO |
| Chief 24h efficiency | **Each team Chief** | Daily | Issues / idle / bounce patterns → COO (ORG-OPS) |

**Window:** For each team, review work **since the last Ops review** (or since last material activity if idle was documented).

**Idle teams:** If no messages in window, document last-activity evidence (store/audit/mtime/publish proxies) — do **not** invent chat — then still run gate/artifact checks if Stories moved via GitHub/KB.

**Individuals:** Scored via triad function (Chief / Senior Execute / QA) with message-level evidence — not personality scores.

---

## 1. Full communication review (mandatory — each team, every Ops review)

**Scope:** Complete communication for all issues that team worked since last review. **Read every message** in that team's channel(s) (and attached cross-posts into that work) for the window.

Score each team on:

### 1.a Internal (among triad members)

For each member / message pattern, assess with evidence (quote or pointer: who / when / channel):

| Dimension | Question |
|-----------|----------|
| **Productive** | Did every message contribute to the end result? |
| **Efficient** | Was every message necessary? |
| **Quality** | Did communication improve end-result quality? |
| **Completeness** | Anything else that should have been done? |
| **Functional** | Did each message serve that agent’s main function (Chief / Execute / QA)? |

### 1.b External (cross-team)

| Dimension | Question |
|-----------|----------|
| **Intake** | Was the initial task captured completely with full shared understanding? |
| **During work** | Did the team/members communicate appropriately with other teams (Security, PM, cost control, Doc, Product, etc.)? |
| **Handoff back** | When complete, was the result fully explained to the other side, and did the other side confirm understanding? |

**Report shape (per team):** Short score notes (PASS / PARTIAL / FAIL) + concrete message evidence + named gaps. No invented dialogue.

---

## 2. Improvement proposals (from §1 — mandatory when gaps found)

| # | Scope | Requirement |
|---|-------|-------------|
| **2.a** | Among team members | Propose fixes that improve triad communication; run proposal through the **whole team** (Chief owns adoption brief) |
| **2.b** | Among different teams | Propose cross-team communication / handoff fixes |
| **2.c** | Specialty ↔ common roles | Propose fixes between specialty roles (arch / spec / dev-plan / SD / QA / CQ / Doc / Marketing / …) and common roles (PM, Security, cost control, COO/CEO gates) |

**Safety:** Proposals must not regress normal flows. **No spend/critical execute** — escalate via COO → CEO.

---

## 3. Complementary checklist (still run each review)

### 3.A Per team process

| # | Check | Pass if |
|---|-------|---------|
| T1 | Triad etiquette | Senior → team QA → Chief only; no skip-Chief to PM/CEO |
| T2 | Product alignment | Align to Product; conflicts PM → Product → CEO |
| T3 | Cost/critical gate | No spend/AWS/paid promo without COO→CEO |
| T4 | MotorMarket bleed | No Dealoware team using MotorMarket live systems |
| T5 | Doc hygiene | DOC-FLOW paths/naming when deliverables produced |
| T6 | Idle documented | Idle windows have last-activity evidence |

### 3.B Per role signals

| Role | Signals |
|------|---------|
| **Chief** | Itemized briefs; Security ask; HOLD PASS until Security QA; 24h efficiency when due |
| **Senior** | Done-list matches brief; Security woven; revises on bounce; no invented requirements |
| **QA** | Evidence PASS/BOUNCE; asks Security QA before step PASS; never skips Chief |

### 3.C Per Story gate

Pipeline: BA → SA → Spec → Dev Plan → SD → Product QA → CQ → Doc → BA verify → `status:done`

| Gate | Must see |
|------|----------|
| Security handshake | Checklist → Senior weave → Security QA confirm **before** step QA PASS |
| CQ post-SD | Assessment / `cq:*` before Product QA / Doc advance |
| Product QA | AC + Security QA; `qa/` report |
| Doc | Doc + Doc Security when critical; INDEX delta healthy |
| BA verify | BAQA → CBA confirm before eng `done` |
| Latency | Soft: long sit in `in-dev` / `cq-pending` / `ready-for-ba-verify` without owner |

### 3.D Cadence / platform health

| Signal | Source |
|--------|--------|
| 6h COO ops-review | Bot Manager automation (ok / usage_limit / gaps) |
| Security research 02:00 / 12:00 ET | Chief Security routines |
| Marketing daily | Marketing routine + claims/cost hold |
| INDEX hourly weekday | Chief Docs hourly + `meta/rebuild-index-delta.sh` |

---

## 4. Signals Ops uses (and does not invent)

**Uses:** every in-scope team-channel message body; cross-team handoff threads; KB artifacts; GitHub labels/PRs; automation `runs.json`; Chief 24h notes.

**Does not:** invent chat; mutate labels/spend; toggle automations without CEO/COO path; skip §1 when messages exist.

---

## 5. Open gaps carried from Brief #1 (separate from this lock)

| Gap | Status |
|-----|--------|
| #5 stall | **Closed** 2026-09-20 |
| INDEX hourly disabled / frozen | Still open — dry-run then re-enable (CEO) |
| Cadence usage_limit / midday gaps | Still open — Bot Manager sequencing; **no** duplicate COO routine |
| ORG-OPS stub path in automation prompt | Still open — point to `ops/ORG-OPS.md` |

---

## 6. Next cadence commitment

**Brief #2 onward:** COO will brief Ops Executive to execute **§1 full communication review + §2 proposals + §3 checklists** for every staffed Dealoware team with activity (or idle-documented). Ops QA verifies against this locked model. COO presents material items to CEO.

---

## Explicit attestation

PoC **$0**. Marketing public ship / paid promo remain CEO-gated. Ops audits process and communication quality; PM still owns throughput scheduling.
