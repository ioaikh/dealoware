# PoC Negotiation Scenarios

| Field | Value |
|-------|-------|
| **Date** | 2026-09-20 |
| **Type** | Guide |
| **Scope** | PoC API validation |
| **Companion** | [Postman Collection](../../postman/Dealoware-PoC-Negotiations.postman_collection.json) |

## Purpose

This document catalogs all negotiation scenarios for the Dealoware PoC, mapping happy paths and important error/edge cases to real API endpoints. Use this alongside the [Postman collection](../../postman/Dealoware-PoC-Negotiations.postman_collection.json) to exercise the full negotiation flow.

## Prerequisites

1. **Run the API locally:**
   ```bash
   dotnet run --project src/Dealoware.Api
   ```

2. **Verify health:**
   ```bash
   curl http://localhost:5287/health
   # Expected: {"status":"ok"}
   ```

3. **Base URL:** `http://localhost:5287`

4. **Authentication:** All protected endpoints require `Authorization` header:
   - `Authorization: ApiKey dlw_...` (API key)
   - `Authorization: Bearer eyJ...` (JWT)

## Complementary Intent Pairs

Negotiations require complementary intents between the two parties:

| Caller Intent | Counterparty Intent | Use Case |
|---------------|---------------------|----------|
| `buy` | `sell` | Product purchase |
| `sell` | `buy` | Product sale |
| `provide` | `consume` | Service offering |
| `consume` | `provide` | Service seeking |
| `rent` | `rent` | Rental (bidirectional) |

---

## Scenario Catalog

### S1: Bootstrap — Register Seller and Buyer

**Goal:** Create two participants (Seller and Buyer) and capture their credentials.

**Actors:** None (bootstrap)

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 1.1 | POST | `/auth/register` | None | `{"displayName":"Seller"}` | 201, returns `sub`, `apiKey` |
| 1.2 | POST | `/auth/register` | None | `{"displayName":"Buyer"}` | 201, returns `sub`, `apiKey` |

**Notes:**
- `apiKey` is shown only once — store securely
- `sub` format: `participant:{uuid}`
- Capture both `sellerApiKey`, `sellerSub`, `buyerApiKey`, `buyerSub` for subsequent scenarios

**Example Response:**
```json
{
  "sub": "participant:550e8400-e29b-41d4-a716-446655440000",
  "displayName": "Seller",
  "apiKey": "dlw_AbCdEfGh_...",
  "apiKeyPrefix": "AbCdEfGh",
  "createdAt": "2026-09-20T10:00:00Z"
}
```

---

### S2: Seller Creates Sell Artifact

**Goal:** Seller creates an artifact representing an item for sale.

**Actors:** Seller

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 2.1 | POST | `/artifacts` | Seller | See below | 201, returns artifact with `id` |

**Request Body:**
```json
{
  "entities": [
    {
      "name": "2020 Toyota Camry",
      "description": "Well-maintained sedan, single owner",
      "properties": [
        {"name": "mileage", "type": "number", "value": "45000"},
        {"name": "color", "type": "string", "value": "silver"}
      ],
      "facts": ["Clean title", "Regular maintenance"]
    }
  ],
  "intent": "sell",
  "values": [{"amount": 22000.00, "currency": "USD"}],
  "locations": ["Los Angeles, CA"]
}
```

**Notes:**
- `ownerParticipantId` is set from authenticated principal's `sub`
- Capture `artifactId` for negotiation creation

---

### S3: Create Negotiation (Sell ↔ Buy)

**Goal:** Seller initiates a negotiation with the Buyer around the artifact.

**Actors:** Seller (initiator), Buyer (counterparty)

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 3.1 | POST | `/negotiations` | Seller | See below | 201, returns negotiation with `status: "Open"` |

**Request Body:**
```json
{
  "artifactId": "{{artifactId}}",
  "counterpartyParticipantId": "{{buyerSub}}",
  "callerIntent": "sell",
  "counterpartyIntent": "buy"
}
```

**Response (key fields):**
```json
{
  "id": "...",
  "artifactId": "...",
  "partyAParticipantId": "participant:...",
  "partyBParticipantId": "participant:...",
  "partyAIntent": "sell",
  "partyBIntent": "buy",
  "status": "Open",
  "identitySealed": true
}
```

**Notes:**
- Caller becomes `partyA`, counterparty becomes `partyB`
- `identitySealed: true` — no contact PII exposed (PoC stub)
- Capture `negotiationId` for offers

---

### S4: Seller Places Offer

**Goal:** Seller places the first offer in the negotiation.

**Actors:** Seller

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 4.1 | POST | `/negotiations/{{negotiationId}}/offers` | Seller | See below | 201, returns offer with `status: "Open"` |

**Request Body:**
```json
{
  "amount": 22000.00,
  "currency": "USD",
  "terms": "Cash only, as-is condition"
}
```

**Response (key fields):**
```json
{
  "id": "...",
  "negotiationId": "...",
  "fromParticipantId": "participant:...",
  "toParticipantId": "participant:...",
  "status": "Open",
  "amount": 22000.00,
  "currency": "USD",
  "terms": "Cash only, as-is condition",
  "identitySealed": true
}
```

**Notes:**
- `fromParticipantId` = Seller, `toParticipantId` = Buyer
- Only one open offer per side allowed
- Capture `offerId` for accept/decline/counter

---

### S5: Buyer Counters

**Goal:** Buyer counters the Seller's offer with a lower price.

**Actors:** Buyer (offer recipient)

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 5.1 | POST | `/offers/{{offerId}}/counter` | Buyer | See below | 201, returns new counter-offer with `status: "Open"` |

**Request Body:**
```json
{
  "amount": 18000.00,
  "currency": "USD",
  "terms": "Financing available, inspection required"
}
```

**Outcome:**
- Original offer → `status: "Superseded"`
- New counter-offer created with `status: "Open"`
- Counter-offer `fromParticipantId` = Buyer, `toParticipantId` = Seller

**Notes:**
- Only the offer recipient (`toParticipantId`) can counter
- Capture `counterOfferId` for accept

---

### S6: Seller Accepts Counter (Deal Path)

**Goal:** Seller accepts the Buyer's counter-offer, completing the deal.

**Actors:** Seller (counter-offer recipient)

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 6.1 | POST | `/offers/{{counterOfferId}}/accept` | Seller | None | 200, returns offer with `status: "Accepted"` |
| 6.2 | GET | `/negotiations/{{negotiationId}}` | Seller | None | 200, negotiation with accepted offer in `offers[]` |

**Outcome:**
- Accepted offer → `status: "Accepted"`
- All other open offers in negotiation → `status: "Cancelled"`
- Negotiation remains `Open` (no auto-close on accept in PoC)

**Notes:**
- Only the offer recipient (`toParticipantId`) can accept
- **No contact/PII released** — PoC stub; contact exchange is MVP (P7/A9)
- `identitySealed: true` remains on response

---

### S7: Alternate — Buyer Declines Offer

**Goal:** Buyer declines the Seller's offer without countering.

**Actors:** Buyer (offer recipient)

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 7.1 | POST | `/offers/{{offerId}}/decline` | Buyer | None | 200, returns offer with `status: "Declined"` |

**Outcome:**
- Offer → `status: "Declined"`
- Negotiation remains `Open` — parties can place new offers

**Notes:**
- Only the offer recipient can decline
- After decline, either party can place a new offer

---

### S8: Alternate — Close Negotiation

**Goal:** Either party closes the negotiation, canceling all open offers.

**Actors:** Seller or Buyer

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 8.1 | POST | `/negotiations/{{negotiationId}}/close` | Seller | None | 200, returns negotiation with `status: "Closed"` |

**Outcome:**
- Negotiation → `status: "Closed"`
- All open offers → `status: "Cancelled"`
- Post-close mutations → 409 Conflict

---

### S9: Negative — Non-Complementary Intents → 400

**Goal:** Verify that non-complementary intents are rejected.

**Actors:** Seller

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 9.1 | POST | `/negotiations` | Seller | See below | 400, validation error |

**Request Body (invalid — both want to sell):**
```json
{
  "artifactId": "{{artifactId}}",
  "counterpartyParticipantId": "{{buyerSub}}",
  "callerIntent": "sell",
  "counterpartyIntent": "sell"
}
```

**Expected Error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "errors": ["Intents are not complementary: 'sell' and 'sell'. Valid pairs: buy↔sell, provide↔consume, rent↔rent"]
  }
}
```

---

### S10: Negative — Non-Party GET → 404

**Goal:** Verify that a non-party cannot view a negotiation (no information leak).

**Actors:** Third party (not Seller or Buyer)

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 10.1 | POST | `/auth/register` | None | `{"displayName":"Observer"}` | 201 |
| 10.2 | GET | `/negotiations/{{negotiationId}}` | Observer | None | 404 (not 403) |

**Notes:**
- Returns 404 (not 403) to prevent information leakage about existence
- Same behavior for offers: non-recipient cannot see or act on offers

---

### S11: Negative — Second Open Offer Same Side → 400

**Goal:** Verify that a party cannot have two open offers simultaneously.

**Actors:** Seller

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 11.1 | POST | `/negotiations/{{negotiationId}}/offers` | Seller | First offer | 201 |
| 11.2 | POST | `/negotiations/{{negotiationId}}/offers` | Seller | Second offer | 400, validation error |

**Expected Error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "errors": ["You already have an open offer in this negotiation"]
  }
}
```

**Notes:**
- One-open-per-side rule enforced
- Party must wait for their offer to be accepted/declined/countered before placing another

---

### S12: Negative — Mutate After Close → 409

**Goal:** Verify that closed negotiations reject mutations.

**Actors:** Seller, Buyer

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 12.1 | POST | `/negotiations/{{negotiationId}}/close` | Seller | None | 200 |
| 12.2 | POST | `/negotiations/{{negotiationId}}/offers` | Buyer | Any | 409 Conflict |
| 12.3 | POST | `/negotiations/{{negotiationId}}/close` | Seller | None | 409 Conflict |

**Expected Error:**
```json
{
  "error": "Negotiation is Closed, cannot place offer"
}
```

---

### S13: Optional — Expiration via EndsAt

**Goal:** Verify that negotiations expire when `endsAt` is reached.

**Actors:** Seller, Buyer

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 13.1 | POST | `/negotiations` | Seller | Include `endsAt` in past | 201 (or set short window) |
| 13.2 | POST | `/negotiations/{{negotiationId}}/offers` | Seller | Any | 409, "Negotiation has expired" |
| 13.3 | GET | `/negotiations/{{negotiationId}}` | Seller | None | 200, `status: "Expired"` |

**Create with short expiration:**
```json
{
  "artifactId": "{{artifactId}}",
  "counterpartyParticipantId": "{{buyerSub}}",
  "callerIntent": "sell",
  "counterpartyIntent": "buy",
  "endsAt": "2026-09-20T00:00:00Z"
}
```

**Notes:**
- Expiration is checked on any mutating operation (place, accept, decline, counter, close)
- GET still works — parties can view expired negotiations
- All open offers → `Cancelled` on expiration

---

### S14: Optional — Provide ↔ Consume and Rent ↔ Rent

**Goal:** Verify alternative complementary intent pairs work.

**Actors:** Provider, Consumer

#### Provide ↔ Consume (Service)

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 14.1 | POST | `/negotiations` | Provider | `callerIntent: "provide"`, `counterpartyIntent: "consume"` | 201 |

#### Rent ↔ Rent (Bidirectional)

| Step | Method | Endpoint | Auth | Body | Expected |
|------|--------|----------|------|------|----------|
| 14.2 | POST | `/negotiations` | Landlord | `callerIntent: "rent"`, `counterpartyIntent: "rent"` | 201 |

---

## Quick Reference: Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success (GET, accept, decline, close) |
| 201 | Created (register, create artifact/negotiation/offer, counter) |
| 400 | Validation error (non-complementary intents, duplicate open offer) |
| 401 | Unauthorized (missing or invalid auth) |
| 404 | Not found OR not a party (no information leak) |
| 409 | Conflict (closed, expired, offer not open) |

## Quick Reference: Offer Status Transitions

```
Open → Accepted (recipient accepts)
Open → Declined (recipient declines)
Open → Superseded (recipient counters → creates new Open offer)
Open → Cancelled (negotiation closed/expired, or another offer accepted)
```

## Quick Reference: Negotiation Status Transitions

```
Open → Closed (party calls close)
Open → Expired (endsAt reached on any operation)
```

---

## Postman Collection

Import the [Postman collection](../../postman/Dealoware-PoC-Negotiations.postman_collection.json) to run these scenarios interactively:

1. **Import:** File → Import → select `postman/Dealoware-PoC-Negotiations.postman_collection.json`
2. **Set `baseUrl`:** Edit collection variables if not `http://localhost:5287`
3. **Run folders in order:**
   - `01-Bootstrap` (captures credentials)
   - `02-Happy-Path-Deal` (full negotiation → accept)
   - `03-Alternate-Decline`
   - `04-Alternate-Close`
   - `05-Negative-Cases`

The collection uses Tests scripts to automatically capture and propagate IDs between requests.

---

## Out of Scope (PoC)

- **Contact release on accept** — MVP P7/A9, not PoC
- **Real PII vault** — MVP
- **Participant data isolation (#18)** — MVP+
- **Strategy CRUD** — MVP P3
- **Cognito/SSO integration** — post-MVP

See [README Local Run](../../README.md#local-run) for setup instructions.
