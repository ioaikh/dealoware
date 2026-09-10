# Dealoware — Universal Negotiation Platform

**Canonical original:** `product/CEO-ORIGINAL-BRIEF.md` (verbatim CEO text + appendix of Later CEO decisions).  
**This file:** Processed Product summary. Must not contradict the original. Conflicts escalate PM → Product → CEO.

## One-liner
Backend API + minimal UI for negotiating any **Artifact** between registered **Participants** (humans or AI agents), with strategy-driven AI Assistants that protect identity until accept.

## Core model
### Artifact
- **Subject** — what is negotiated (e.g. car, house, boat, TV, phone; collectibles such as coins, pictures, postage stamps; services such as cleaning, fixing, construction; and many others). Subject can also be a collection of entities. Each entity may have:
  - Name
  - Description
  - Properties (property name, type, value)
  - Facts — strings about the Artifact discovered during communication/negotiation
- **Intent** — buy, sell, rent, exchange, provide, consume, …
- **Value** — amount + “currency” (USD, chicken eggs, ounce of gold, …); none, one, or multiple values in different currencies; at most one clear value per currency.
- **Location** — geographical (or universe of space); none, one, or many.
- **Time** — start and end when negotiation can happen; none, one, or many periods.

### Participant
Must be registered on the Platform to use it.

### Negotiation
Around an Artifact between Participants with complementary intents (e.g. one buying, another selling). During Negotiation both sides can provide Offers. Receiver may Accept, Decline, or Counteroffer. Either side may Close Negotiation at any time (all open offers cancelled). Negotiation may have start and end (expiration) dates.

### Strategy (per Participant)
CRUD Negotiation Strategies. A Strategy can define (free-form conditions allowed; short examples from original):
- **When to start** negotiation for a discovered Artifact (e.g. if today is end of month and there is rain in New York)
- **How to start** (e.g. offer 20% less than market price in USD)
- **What to demand** on each step (e.g. offer in the middle of what they are offering and 10% less than market price)
- **Concessions** on each step (e.g. up to $1 USD; or offer free 3-year warranty on engine, extend to 5 years if needed)
- **When Accept** (e.g. if offer reaches 15% below market price)
- **When Decline** (e.g. if offer is over market price)
- **When Counteroffer**
- **When Close** (e.g. after 5 unaccepted offers; or if there is snow in Miami)
- **Guardrails** — cutoff negotiations clearly out of acceptable limits (e.g. buy new car for $1; sell snow in Alaska)

## AI Assistant
The Platform provides an AI Assistant to conduct negotiation on behalf of the Participant. The Assistant follows the selected Strategy end-to-end: from starting negotiation for discovered Artifacts through offers and close. Identity is protected until successful negotiation; contact exchange happens on accepted offer. Participants communicate with their own AI Negotiation agent only — no direct human↔human chat on-platform. Participants may connect their own model to run their own AI Negotiation Agent/Assistant.

## Participant capabilities
1. CRUD own Artifacts  
2. Discover others’ Artifacts (instant search + saved search / market monitoring)  
3. CRUD Negotiation Strategies  
4. Initiate negotiations & manage offers (place, accept, reject, counter)  
5. Extensive notifications  
6. Communication with own AI Negotiation agent only (no direct communication)  
7. AI protects identity until success; contact exchange on accept  
8. Analytics on Artifacts / Negotiations / Offers  
9. Bring-your-own model for own AI Negotiation Agent/Assistant  

## Platform-owner capabilities
1. Full user management — users, permissions, roles  
2. Full artifacts management  
3. Full negotiations/offers management  
4. Platform global analytics  
5. Platform health monitoring  
6. Platform notifications  
7. Efficiency, cost monitoring and controls  
8. Integration for many reasoning LLM models  
9. Integrations with SSO providers  
10. Simple, high-performance core — prefer **.NET**, hosted on **AWS**  
11. Security and PII addressed  

## Distribution (original)
Through integration with agentic systems — from chats to bots such as Claude or Grok Bot — as well as basic UI.

## Later CEO decisions (appendix; not in original paragraph)
Confirmed by Ivan after the original brief; recorded in `CEO-ORIGINAL-BRIEF.md` appendix:
1. **License:** Apache License 2.0  
2. **Code:** public GitHub https://github.com/ioaikh/dealoware ; hosted platform remains AIKnowHow / Dealoware (not “free hosted fork as the platform”)  
3. **Separation:** Keep MotorMarket / DC4 live systems, inventory, SFTP, and test logins out of Dealoware work  

## Claims lock (Product/CEO constraint; not in verbatim original)
- Platform is an **intermediary**; it does not buy/sell/own Artifacts by default.
- No online settlement/checkout unless explicitly added later.
- Do not claim traction, savings %, or “at scale” without evidence.

## Agreed early platform additions (post-original — “What else — suggest”; re-check vs original)
Agreed after the original brief (2026-09-09). Labeled as post-original agreements; do not treat as original verbatim:
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

## Source control & licensing (updated 2026-09-10; reconciled with Later CEO decisions)
- **Code:** public open source on **GitHub** — https://github.com/ioaikh/dealoware (contributions welcome).
- **License:** Apache License 2.0
- **Hosted platform:** remains AIKnowHow / Dealoware (not open for free-hosting fork as “the” platform).
- **Visibility:** public repo (private GitLab-only io@/pu@ is retired for Dealoware OSS).
- Keep separate from MotorMarket/dc4 repos and feeds

## Relationship to MotorMarket / DC4
Related ideas (intermediary negotiation, strategies, guardrails, contact on accept) but **generic** — not vehicle-only. Dealoware must not share MotorMarket live inventory, test logins, or SFTP feeds.
