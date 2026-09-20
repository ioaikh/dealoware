# BA business verification — Story #4 Artifact D1–D5

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-11  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/4  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/11 (MERGED)  
**Docs PR:** https://github.com/ioaikh/dealoware/pull/12  
**Method:** Business-intent AC check from Story body + merged PR evidence + KB QA (not code review). No invented requirements. Product lock Option A.

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | D1 Subject — entities with name/description/properties/facts; collection OK | **PASS** | `SubjectEntity` (name, description, `EntityProperty` name/type/value, facts); `Artifact.Entities` 1..n collection. |
| 2 | D2 Intent | **PASS** | `Artifact.Intent` string (buy/sell/rent/…); validator + create path. |
| 3 | D3 Value — amount + currency; 0..n; ≤1 clear value per currency | **PASS** | `ArtifactValue` amount+currency (uppercase normalize); 0..n; duplicate currency → 400 (validator). |
| 4 | D4 Location 0..n | **PASS** | `Artifact.Locations` list of strings; 0..n. |
| 5 | D5 Time periods start/end; 0..n | **PASS** | `TimePeriod` Start/End; `Artifact.TimePeriods` 0..n. |
| 6 | PoC API: create, get, list-own (persisted; owner-scoped) | **PASS** | `ArtifactEndpoints`: POST `/artifacts`, GET `/{id}`, GET `/` list-own; EF persist; owner via `X-PoC-Owner-Id`; get non-owned → 404. |
| 7 | Update/Delete NOT required for PoC done (MVP P1) | **PASS** | No Put/Patch/Delete routes on Artifact endpoints; Product lock Option A held. |

## Product lock / OOS

| Item | Held? | Evidence |
|------|-------|----------|
| Option A (create/get/list-own only) | Yes | Endpoints map only those three; Story Decisions section |
| Saved search / market monitoring | Yes | Not in PR surface |
| Strategy / AI | Yes | Not implemented |
| Discovery / instant search | Yes | No search routes |
| Update/Delete as required | Yes | Explicitly absent (P1 @ MVP) |
| MotorMarket / DC4 | Yes | Product QA tree/package scan clean; no MM/DC4 deps |

## Soft gaps (non-blocking)

- No live `dotnet`/`curl` re-run on this BA pass (same soft gap as Product QA).
- Docs PR #12 still OPEN at verify time; impl PR #11 MERGED.

## Recommendation to CBA

**PASS** — deliverable meets Story #4 business AC, Product lock Option A, and OOS. Hand to BAQA for verify-QA; eng `done` HOLD until CBA final confirm after BAQA.
