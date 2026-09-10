# PoC GitHub backlog (draft for CEO OK → open on GitHub)

| Field | Value |
|-------|-------|
| **Status** | draft — awaiting approval to create GitHub Issues on `ioaikh/dealoware` |
| **Source** | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` PoC primary IDs |
| **IDs covered** | D1–D10, P4, O10, L1–L3 (+ identity-seal stub supporting later P7/A9) |
| **Labels** | `stage:poc` + milestone **PoC** + `status:backlog` + `type:story` or `type:chore` |

## Ordered backlog (implementable tasks)

### 1. [PoC] .NET solution scaffold + minimal API host (O10) — `type:story`
- Runnable .NET modular monolith + health/ping; AWS host shape not prod deploy
- Out: prod AWS, Strategy/AI, multi-party

### 2. [PoC] Artifact core model D1–D5 — `type:story`
- Subject/Intent/Value/Location/Time + minimal create/get API
- Out: saved search, strategies

### 3. [PoC] Participant minimal register/auth (D6) — `type:story`
- OIDC-shaped principal; auth on PoC APIs
- Out: full UX, SSO O9, admin O1

### 4. [PoC] 1:1 Negotiation + Offers (D7–D9, P4) + expiration (D10) — `type:story`
- Place/Accept/Decline/Counter/Close; thin expiration; strictly 1:1
- Out: Strategy/AI, contact-on-accept, multi-party

### 5. [PoC] Identity-seal stub (no contact exchange) — `type:story`
- No contact PII leak; precursor to MVP P7/A9
- Out: real contact release, mature vault

### 6. [PoC] Standing license/repo/hosted posture (L1–L3) — `type:chore`
- Apache-2.0, public repo posture, hosted stays AIKnowHow; MotorMarket separation noted

## Rules
- CBA may refine Stories after open
- CQ gate after each SD task
- No mega-issue; no inventing beyond roadmap
- TEST #2 dry-run OK (Bot Manager marking `status:done`)

## Next
CEO/Bot Manager: approve creating these 6 Issues on GitHub → CPM opens + returns links.
