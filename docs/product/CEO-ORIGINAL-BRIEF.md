# Dealoware — CEO original specification (verbatim)

**Status:** Canonical **original** CEO brief (source of truth for intent).  
**Provenance:** Ivan Onuchin message `[t160u]` in Bot Manager chat, ~2026-09-09 (Dealoware project kickoff).  
**Processed summary:** `product/PRODUCT-BRIEF.md` (and GitHub `docs/product/PRODUCT-BRIEF.md`) — Product Team maintains; must not contradict this original.  
**Doc flow:** Original → `product/CEO-ORIGINAL-BRIEF.md`; summary → `product/PRODUCT-BRIEF.md`. Conflicts escalate PM → Product → CEO.

---

## Verbatim CEO text

The task is to develop Universal Negotiation Platform - "Dealoware", which will provide backend API endpoints and minimal UI to negotiate different Artifacts.

Platform can be used by real people as well as AI Agents.

The negotiation is happening around Artifact which has the following properties:

1. `Subject` (what is negotiated, such as car, house, boat, tv, phone etc, collectible items such as coin, pictures, post stamps  etc, services, such as cleaning, fixing, construction, etc and many other). Subject can also be a collection of entities. Each entity may have: 

- `Name`, 

- `Description`, 

- `Properties (property name, type, value).

- `Facts` - facts about artifact (collection of strings) discovered during communication/negotiation about this artifact.

2. `Intent` (what to do with subject, such as: buy, sell, rent, exchange, provide, consume, etc)

3. `Value` - some value descriptor (can be in USD, or in 'chicken eggs', or in 'ounce of gold' - any kind of things). The value is an amount and 'currency', which can be not only formal currency. Could be a collection of none, one or multiple values in different currencies (but one clear value in one currency). 

4. `Location` - where in geographical location (or in universe of space) this negotiation is happening. May have no location, one or multiple.

5. `Time` - start and end time when this negotiation can happen. Could be none, one or multiple periods.

The `Participant` has to be a registered on the Platform to be able to use it. 

The Negotiation can happen around Artifact and between people with complimentary intents (one is buying and another is selling).

During the Negotiation both sides can provide offers to each other.

When one side received the offer - it can "Accept", "Decline", "Counteroffer" it.

Any side can close Negotiation at any time. All Open offers will be cancelled.

Negotiation may have start and end (expiration) date.

The Platform should allow for registered Participant:

1. Manage Negotiator's Artifacts (CRUD).

2. Discover others Negotiators Artifacts (Instant Search and Saved Search - Market monitoring)

3. Manage Negotiation Strategies (CRUD), where strategy can define:

- when to start negotiation for discovered artifact (Can be quite free description. For example: if today is end of months, and there is rain in New York, etc)

- how to start negotiation for discovered artifact (for example: offer 20% less than market price in USD)

- what to demand on each step (for example: offer in the middle of what they offering and 10% less than market price)

- what is concession on each step (for example: up to $1 USD; or another example: offer free 3 year warranty on engine, if needed extend warranty to 5 years)

- when Accept offer (for example: if offer reaches 15% below market price - accept)

- when Decline offer (for example: if offer is over market price)

- when make Counteroffer 

- when close Negotiation (for example: after 5 unaccepted offers, or another example: if there is a snow in Miami)

- guardrails, which help to cutoff negotiations which are clearly out of acceptable limits (for example: buy new car for $1, or sell snow in Alaska)

4. Initiate Negotiations and manage Offers (place offer, accept, reject, counter)

5. Extensive notifications.

6. Communication with your AI Negotiation agent - no direct communication. 

7. AI Negotiation agents should protect user identity until successful negotiations. Contact exchange on accepted offer.

8. Analytics of performance of Artifacts, Negotiations and Offers.

9. Ability to connect your own model to run your own AI Negotiation Agent/Assistant

The Platform owner functionality:

1. Full user management, Users, Permissions, Roles.

2. Full artifacts management

3. Full negotiations/offers management

4. Platform global analytics

5. Platform health monitoring

6. Platform notifications

7. Efficiency, Cost monitoring and controls

8. Integration for many reasoning LLM models 

9. Integrations with SSO providers

10. Simple, high performance core, preferably .net, hosted on AWS.

11. Security and PII concerns addressed

What else - suggest.

The Platform provides AI Assistant to conduct negotiation on behalf of Participant.

AI Assistant is following selected Strategy to run whole process from starting negotiation for discovered Artifacts.

The distribution is through integration with agentic system, from chats to bots such as Claude or Grok-bot. As well as basic UI.

---

## Later CEO decisions (not in the original paragraph; recorded for Product)

These were confirmed by Ivan after the original brief. They belong in the **summary** (`PRODUCT-BRIEF.md`) and must not overwrite the verbatim section above.

1. **License:** Apache License 2.0 (Ivan, license choice).
2. **Code:** public GitHub https://github.com/ioaikh/dealoware ; hosted platform remains AIKnowHow / Dealoware (not “free hosted fork as the platform”).
3. **Separation:** Keep MotorMarket / DC4 live systems, inventory, SFTP, and test logins out of Dealoware work.
4. Early platform additions agreed in Product summary (matching, audit trail, concurrency, HITL, sandbox, vertical templates, webhooks/OpenAPI/MCP, LLM metering, PII vault, prohibited categories, OpenTelemetry, intermediary settlement policy, MVP 1:1 only, idempotent APIs) — Product must re-check these against this original when refreshing the summary.
