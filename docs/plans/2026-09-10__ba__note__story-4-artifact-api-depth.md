# BA note — Story #4 Artifact API depth (PoC)

**Status:** Open question resolved (Product lock)  
**Date:** 2026-09-10  
**Author:** Dealoware Senior BA  
**GitHub Story:** https://github.com/ioaikh/dealoware/issues/4  
**DOC-FLOW:** `plans/` · `YYYY-MM-DD__ba__note__{slug}.md`

## Question
SA Option A (create + get + list-own in PoC; Update/Delete at MVP) vs Option B (full Artifact CRUD in PoC if cheap). Roadmap PoC AC was create/get; P1 CRUD primary at MVP.

## Decision (Product lock)
**Option A.** PoC = create + get + list-own. Update/Delete at MVP (P1). Full CRUD in PoC is optional stretch only if zero schedule risk — not required for PoC done.

## Sources (no inventing)
- Chief Product lock (2026-09-10) to Senior BA
- `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` §2b Option A vs B
- `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` (D1–D5 PoC; P1 MVP)
- GitHub #4 body updated to match

## Spec / Dev Plan implication
Spec PoC API surface = create, get, list-own only. Do not require Update/Delete for PoC acceptance.
