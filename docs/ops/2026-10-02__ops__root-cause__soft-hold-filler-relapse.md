# Ops root-cause — Soft HOLD Soft HOLD Soft HOLD filler relapse

| Field | Value |
|-------|-------|
| **Date** | 2026-10-02 (America/New_York) |
| **Owner** | Dealoware COO (Ops Team Chief) |
| **Trigger** | CEO via Bot Manager — Soft HOLD Soft HOLD Soft HOLD filler relapsed on CPM/Senior PM status pings landing in PMQA/BM chats |
| **Related proposal** | `ops/2026-10-02__ops__proposal__org-ops-soft-hold-ping-lock.md` |
| **Canonical charter** | `ops/ORG-OPS.md` (root stub points here) |
| **Status** | CEO CONFIRMED 2026-10-02 ~2:27pm ET — ORG-OPS Soft HOLD ping lock applied (Soft HOLDs OK with reason; no invent; no Soft HOLD Soft HOLD Soft HOLD repeats) |
| **Locks** | PoC $0 · no MotorMarket · no silent restructure of live ORG-OPS |

## Evidence

1. **ORG-OPS gap (SoR):** Canonical `ops/ORG-OPS.md` (205 lines) § “PM pipeline ownership + visibility (CEO 2026-09-20)” (~L112–138) locks channel mirror on every Story status flip (issue link + label + next triad + evidence pointer), evidence-gated “in progress” language, GitHub unlock SoR, and CPM 15m watches. **It does not contain** any Soft HOLD naming rule, no-loop / anti-filler rule, re-ACK ban, or “one evidence ping per real change” rule.
2. **Chat-only prior ban (never chartered):** Memory — 2026-09-28 ~10:07 PM ET CEO (via Bot Manager): no message loops; Soft Soft CLOSE Soft HOLD / Soft HOLD Soft HOLD Soft HOLD filler and re-ACK spam banned; one evidence close per real tip/PASS change; quiet Stage C closeout; PoC $0; COO Soft HOLD silence (no re-ACK). Grep of `dealoware-kb` (`ops/`, reports, INDEX twins) finds **no** ORG-OPS subsection or ops-note that locks that Sept 28/29 no-loop directive into the charter.
3. **Relapse pattern (2026-10-02 PM Team):** CPM / Senior PM / PMQA status lines on Spec Step 3 [#148](https://github.com/ioaikh/dealoware/issues/148) and Step 4 [#155](https://github.com/ioaikh/dealoware/issues/155) repeatedly stack `Soft HOLD Soft HOLD Soft HOLD` tokens on FLIP, ACK, and PMQA PASS/BOUNCE pings even when the Soft HOLD *set* did not change (same AWS / Marketing / invent Stories / Steps 4–5 / multi-provider holds restated as boilerplate). Example shape: “Soft HOLD Spec Soft HOLD until CA PASS Soft HOLD Soft HOLD invent Stories Soft HOLD Soft HOLD AWS Soft HOLD …” on nearly every line.
4. **Mandatory mirror still valid:** Same channel correctly posts FLIP/CLOSE with issue link + labels + tip/PR evidence (e.g. #148 CLOSE @ tip `ff707ae` / PR #154). The relapse is **noise around** the mirror, not absence of the mirror. PMQA bounce on false BM delete-audit claim (`ops/reports/…148…watch-delete.md` 404) shows evidence gates still work when content is checked.
5. **Secondary contamination:** KB docs/qa/ops weaves still carry `Soft Soft CLOSE Soft HOLD` / stacked Soft HOLD phrasing from Stage C (2026-09-28), so agents treat the stacked token as house style rather than banned filler.

## Root cause (one paragraph)

The Sept 28/29 CEO no-loop / anti-filler ban was **chat-directed only** and never written into `ops/ORG-OPS.md`, so it did not become standing Ops law for CPM/Senior PM prompt loops. Meanwhile ORG-OPS *does* require a PM channel mirror on every status flip and CPM 15m watch ticks that “keep PM channel mirror current,” which agents satisfy by restating the full Soft HOLD inventory on every ACK/FLIP/PASS line. Without a charter rule that Soft HOLDs are named **only when the Soft HOLD set flips** (or on the first flip of a Story) and that quiet is required when nothing changed, the default behavior relapsed to Soft HOLD Soft HOLD Soft HOLD boilerplate and re-ACK spam — visible wherever those pings land (PMQA, Bot Manager, COO).

## What is *not* the root cause

- Not a failure of the mandatory flip mirror itself (issue / label / next triad / evidence still present on real flips).
- Not a spend/provision invent (PoC $0 held).
- Not MotorMarket / #18 unlock invent.

## Fix direction (proposal only)

Add a minimal ORG-OPS subsection under PM pipeline ownership that locks:

- **One evidence ping per real change** (status flip / new tip·PASS / CLOSE / bounce with new evidence / Soft HOLD-set change).
- **Soft HOLD named only on flip** (or when the Soft HOLD set itself changes) — not restated as Soft HOLD Soft HOLD Soft HOLD on every line.
- **Ban** Soft HOLD Soft HOLD Soft HOLD / Soft Soft CLOSE Soft HOLD filler and re-ACK spam; **quiet** when nothing changed.
- **Do not weaken** CEO mandatory PM flip mirror.

Exact proposed text: `ops/2026-10-02__ops__proposal__org-ops-soft-hold-ping-lock.md`. Soft HOLD invent applying patch until CEO confirms via Bot Manager.

## Soft HOLDs

- Soft HOLD invent applying live ORG-OPS edit/commit/push until CEO confirm.
- Soft HOLD invent Stories / AWS provision / Marketing publish / Step 5 invent (unchanged by this ops draft).
- PoC $0 · no MotorMarket.
