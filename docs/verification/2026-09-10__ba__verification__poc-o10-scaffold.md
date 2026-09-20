# BA business verification — Story #3 PoC O10 scaffold

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-10  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/3  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/9 (`cursor/poc-o10-scaffold-71de`)  
**Docs PR:** https://github.com/ioaikh/dealoware/pull/10  
**Method:** Business-intent AC check from Story body + PR evidence + README/KB (not code review). No invented requirements.

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | Runnable .NET solution / modular-monolith repo layout for Dealoware core API | **PASS** | PR #9: `Dealoware.sln`; `src/Dealoware.Api` (net8.0 Web, only runnable host); Domain / Application / Infrastructure placeholders; `tests/Dealoware.Api.Tests`. Matches Spec Option A / Story AC. |
| 2 | Health / ping endpoint | **PASS** | `Program.cs`: `MapGet("/health")` → `Results.Ok(new { status = "ok" })`. `HealthEndpointTests` asserts HTTP 200 + JSON `status=ok`. |
| 3 | Documented local run steps | **PASS** | PR #9 `README.md` Development: .NET SDK 8+, `dotnet restore`, `dotnet run --project src/Dealoware.Api`, curl `localhost:5287/health` expect `{"status":"ok"}`, `dotnet test`. |
| 4 | No MotorMarket / DC4 live-system dependencies | **PASS** | Api csproj = framework + project refs only (no MM/DC4 packages). PR #9 OUT list + Product QA report (`qa/2026-09-10__qa__qa-report__poc-o10-scaffold.md`) tree/csproj/diff scan clean. |
| 5 | AWS host shape = ECS Express Mode (not App Runner); PoC local/$0 until spend escalate; not prod deploy | **PASS** | README AWS callout: ECS Express Mode (Fargate); App Runner NOT target; PoC local/$0; no AWS provision; spend → CPM→COO→CEO. Optional Dockerfile marked local-only (no IAM/Secrets Manager/ECR promo/signing). |

## Out of scope held

| OOS item (Story #3) | Held? | Evidence |
|---------------------|-------|----------|
| Prod AWS deploy / App Runner | Yes | README + Dockerfile + PR OUT |
| Spendful AWS until CEO escalate | Yes | README local/$0 + escalate path |
| Strategy engine | Yes | Not in PR file set / Program.cs sole route |
| AI Assistant | Yes | No LLM packages in Api csproj; placeholders inert |
| Platform-owner suite | Yes | PR OUT; scaffold only |
| Multi-party / multi-Artifact | Yes | PR OUT |
| Settlement / checkout / escrow | Yes | PR OUT |
| Discovery / search | Yes | PR OUT |
| Domain Stories #4–#7 beyond empty libs | Yes | Empty Domain/App/Infra markers only |

## Soft gaps (non-blocking for BA business verify)

- Live `dotnet run` / `curl` not re-executed on this BA pass — relies on static PR evidence + prior Product QA / SD / Security PASS (same soft gap noted in Product QA report).
- PRs #9/#10 still **OPEN** (not merged) at verify time — eng status ready-for-ba-verify per CBA; merge is PM/eng concern after CBA confirm.

## Recommendation to CBA

**PASS** — deliverable meets Story #3 business AC and OOS. Hand to BAQA for verify-QA; eng `done` remains HOLD until CBA final confirm after BAQA.
