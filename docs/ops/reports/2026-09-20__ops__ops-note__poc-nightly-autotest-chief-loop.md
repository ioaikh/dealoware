# Ops note — PoC nightly auto-tests (CEO 2026-09-20)

**Status:** LOCKED for QA Team (PoC until MVP)  
**Owner triad:** Dealoware QA (Chief) · Senior Product QA (Executive) · QAQA  
**Cron host:** Bot Manager (arms/deletes only; does not invent cases)  
**Contract:** `docs/product/2026-09-20__product__guide__poc-negotiation-scenarios.md` + Postman collection  
**#18 / MVP unlock:** unchanged (CEO gate)

## 1. Initial set (one-time)
Whole QA Team implements auto-tests covering the PoC scenarios guide as the **initial suite**. Suite lives in repo (prefer runnable from CI / local `dotnet` + HTTP against local API, and/or Newman against Postman). Evidence: green run + paths in QA report under `docs/qa/` or `qa/`.

## 2. Nightly cadence (PoC until MVP)

### 2.1 Chief QA (every scheduled night)
1. Inventory **which auto-tests currently exist** (paths, case ids mapped to scenarios S*).
2. Review **outcome of previous auto-test run** (PASS/FAIL, flakes, duration).
3. Diff **code changes since last run** (commits/PRs on `main`) that may require test adds/updates.
4. Based on (1)–(3), propose a **set of tests to implement/adjust** → hand itemized brief to **Senior Product QA (Executive)**.
5. **Cross-functional ask** (Security, Product, CPM/cost-as-needed, other Chiefs as relevant): share current auto-test list; ask if more tests are needed.
6. **Filter rigorously:** implement only tests that are (a) valuable and easy, or (b) critical for safe / efficient / reliable Dealoware platform function. Decline nice-to-haves with a one-line reason in the nightly note.

### 2.2 Senior Product QA — Executive (draft pending CEO confirm if more detail arrives)
- Execute Chief’s itemized add/adjust list; keep suite aligned to scenarios guide.
- Run or ensure nightly suite execution; file evidence artifact.
- Bounce unclear scope back to Chief; do not expand beyond approved list.

### 2.3 QAQA (draft pending CEO confirm if more detail arrives)
- Verify suite still maps to scenarios guide; spot-check new cases.
- Confirm Chief’s cross-functional filter was applied (no uncritical bloat).
- PASS/FAIL the nightly QA package before CPM mirror.

## 3. Bot Manager
- Host nightly routine (America/New_York); wake Chief QA process (or QA Team channel) at schedule.
- Audit only: routine armed; do not invent test cases.
- Until MVP: keep this PoC suite alive; revisit ownership at MVP.

## 4. Out of scope (PoC)
- Multi-complement intent matching (MVP+)
- #18 product pipeline
- MotorMarket
