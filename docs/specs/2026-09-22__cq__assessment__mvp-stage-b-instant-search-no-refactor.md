# CQ Assessment — MVP Stage B Instant search #40 / PR #53 — NO REFACTOR

**Author:** Dealoware Senior CQ  
**Date:** 2026-09-22  
**Verdict:** **cq:no-refactor**  
**Issue:** https://github.com/ioaikh/dealoware/issues/40  
**PR:** https://github.com/ioaikh/dealoware/pull/53 · HEAD `92607cd60122f088f41e5149673419add2bdd522` (MERGEABLE/CLEAN)  
**Confirm path:** CQ QA → Chief CQ → CPM gate  
**Constraints:** Stage B P2 instant only; discovery ≠ owner inventory; no StrategyBody/LoginEmail/ContactEmail in payloads; #41/#42 siblings out; saved-search→V1; Stage C OUT; PoC $0; no MM/DC4.

## Verdict

No refactor requirement spec. Instant discovery endpoint + discovery-safe DTO omit secrets; separate from #32 inventory. Soft notes **non-gate** (incl. Soft HEAD drift / Soft SoR Docs process).

## Evidence

1. **API:** `GET /search/artifacts?q=&limit=` — AuthHelper → 401; missing/whitespace `q` → 400; limit clamped 1–100 (default 50).
2. **DTO:** `DiscoverableArtifactResponse` — D1–D5 + id/createdAt only; **omits** OwnerParticipantId, LoginEmail, ContactEmail, StrategyBody, auth secrets, nested Ids.
3. **Query-plane:** `SearchDiscoverableAsync` matches Intent/Entity Name/Description/Property Name/Value — **no** OwnerParticipantId filter (contrast `GetByOwnerAsync`).
4. **Separation:** Separate route/method/DTO from owner inventory; tests cover inventory empty vs search hits / no private dump.
5. **Tests:** DiscoverySearchTests 25 Facts (unauth, secrets omit, separation, query/limit, health).
6. **OUT:** no #41/#42 invent, saved-search, Stage C, Cognito, MM/DC4 in PR product code.
7. **Peer:** Security + Dev Code QA PASS claimed (KB cite soft HEAD `7973da3…`; live tip `92607cd6…` — security sources byte-identical per evidence pack). CI SUCCESS @ tip.

## Soft notes (explicitly non-gate)

| Note | Why non-gate |
|------|----------------|
| Soft HEAD cite `7973da3` ≠ live tip `92607cd6` | Alternate history; feature sources identical; assess tip |
| Soft SoR Docs (#54/#55) / prior CONFLICTING | Process/land note — not code refactor (SoR CLEAR / MERGEABLE now) |
| Structural DTO omit vs Spec Locked #6 IFieldPolicy Evaluate | SD/Security soft MET; Stage B discovery path |
| `callerParticipantId` unused; own artifacts can appear in search | Expected for public discovery |
| Locations/Values/Facts/TimePeriods not in Where (still projected) | Search coverage soft; Product can extend later |
| DefaultLimit test asserts 200 only (not default 50) | Test hygiene |
| TotalCount = page size; Take then reorder | MVP instant OK |

## Affected functionality (QA coordination)

1. `GET /search/artifacts` auth + query validation  
2. Discovery results omit owner/secrets/StrategyBody  
3. Discovery ≠ `GetByOwner` inventory  
4. `/health` open  
5. Zero #41/#42/saved-search/Stage C/Cognito/MM invent  

## Done-list for CQ QA

- [ ] Instant search; secrets omit; discovery≠inventory
- [ ] Soft notes non-gate (HEAD drift, SoR Docs, structural omit, Locations Where gap)
- [ ] Affected-functionality complete
- [ ] No OUT invent
- [ ] Confirm PASS to Chief CQ only

