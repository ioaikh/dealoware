# Dealoware — Universal Negotiation Platform

## One-liner
Backend API + minimal UI for negotiating any **Artifact** between registered **Participants** (humans or AI agents), with strategy-driven AI Assistants that protect identity until accept.

## Core model
### Artifact
- **Subject** — what is negotiated (goods, services, collectibles, collections). Entities may have Name, Description, Properties (name/type/value), Facts (strings discovered during negotiation).
- **Intent** — buy, sell, rent, exchange, provide, consume, …
- **Value** — amount + “currency” (USD, eggs, gold oz, …); zero, one, or many distinct currencies; at most one clear value per currency.
- **Location** — geo/universe; none, one, or many.
- **Time** — start/end windows; none, one, or many periods.

### Participant
Must be registered to use the platform.

### Negotiation
Between complementary intents around an Artifact. Either side may Offer; receiver may Accept / Decline / Counteroffer. Either side may Close (cancels open offers). Optional start/expiration. Identity protected until accepted offer → contact exchange.

### Strategy (per Participant)
Defines when/how to start, demands, concessions, accept/decline/counter/close rules, and hard guardrails. Free-form conditions allowed (dates, weather, market, …) evaluated by the AI Assistant.

## Participant capabilities
1. CRUD own Artifacts  
2. Discover others’ Artifacts (instant + saved search / market monitoring)  
3. CRUD Negotiation Strategies  
4. Initiate negotiations & manage offers  
5. Extensive notifications  
6. Talk to own AI Negotiation agent only (no direct human↔human chat on-platform)  
7. AI protects identity until success; contact exchange on accept  
8. Analytics on Artifacts / Negotiations / Offers  
9. Bring-your-own model for own AI Negotiation Agent/Assistant  

## Platform-owner capabilities
1. Users / permissions / roles  
2. Full artifacts management  
3. Full negotiations/offers management  
4. Global analytics  
5. Health monitoring  
6. Platform notifications  
7. Efficiency & LLM cost controls  
8. Multi-LLM reasoning integrations  
9. SSO  
10. Simple high-performance core — prefer **.NET on AWS**  
11. Security & PII  

## Distribution
Agentic systems (chats, Claude, Grok Bot, …) + basic UI. OpenAPI / webhooks / MCP-style connectors for bots.

## Relationship to MotorMarket / DC4
Related ideas (intermediary negotiation, strategies, guardrails, contact on accept) but **generic** — not vehicle-only. Dealoware must not share MotorMarket live inventory, test logins, or SFTP feeds.

## Claims lock (early)
- Platform is an **intermediary**; it does not buy/sell/own Artifacts by default.
- No online settlement/checkout unless explicitly added later.
- Do not claim traction, savings %, or “at scale” without evidence.

## Agreed early platform additions (2026-09-09)
1. Matching — first-class complementary-intent discovery
2. Immutable audit trail for offers/decisions
3. Concurrency rules for open offers / exclusive negotiate
4. Human-in-the-loop escalation before accept/close
5. Strategy sandbox (dry-run)
6. Vertical schema templates (not hardcoded to one vertical)
7. Webhooks + OpenAPI + MCP for bot distribution
8. LLM cost metering per Participant + hard budgets
9. PII vault — contacts sealed until Accept; retention/erasure
10. Prohibited categories / ToS
11. OpenTelemetry across negotiate steps
12. Explicit settlement policy: intermediary only / no escrow (unless added later)
13. MVP is strictly 1:1; multi-party / multi-Artifact is v2
14. Idempotent APIs for agent clients that retry

## Source control
- Host: GitLab (AIKnowHow)
- Visibility: **private**
- Access: only `io@aiknowhow.com` and `pu@aiknowhow.com`
- Keep separate from MotorMarket/dc4 repos and feeds

## Distribution / licensing strategy (updated 2026-09-10)
- **Code:** public open source on **GitHub** (contributions welcome).
- **Hosted platform:** remains AIKnowHow / Dealoware (not open for free-hosting fork as “the” platform).
- **Access:** public repo (no longer private GitLab-only io@/pu@).
- **License:** Apache License 2.0
