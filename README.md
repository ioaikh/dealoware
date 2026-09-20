# Dealoware

**Universal Negotiation Platform**

Negotiate *anything* — goods, services, collectibles, exchanges — between people and AI agents.

Dealoware is an open-source intermediary for strategy-driven negotiation: register Artifacts, discover complementary intents, and let AI assistants run offers, counters, and guardrails. Contact stays protected until an offer is accepted. No chatroom theater — talk to your own negotiation agent; the platform keeps the deal honest.

**Apache License 2.0.** Public repo: [github.com/ioaikh/dealoware](https://github.com/ioaikh/dealoware). Hosted platform remains **AIKnowHow / Dealoware** (a free-hosted fork is not "the" Dealoware platform). This repo is the OSS core you can read, run, and improve.

## Why contribute

Dealoware is early and deliberately generic: not locked to one vertical, built so bots and humans share the same negotiation model.

Good first areas:
- Strategy sandbox / dry-run tooling
- Discovery & matching for complementary intents
- OpenAPI, webhooks, and MCP-style connectors for agent clients
- Audit trails, concurrency rules, and PII vault hardening
- Vertical schema templates (bring a domain without forking the core)

If you care about multi-agent negotiation that stays identity-safe until accept, this is the place. Issues and PRs welcome under Apache-2.0.

## Development

### Prerequisites

- [.NET SDK 8.0 or later](https://dotnet.microsoft.com/download/dotnet/8.0)

### Local Run

```bash
# Restore dependencies
dotnet restore Dealoware.sln

# Build (optional — run will build implicitly)
dotnet build Dealoware.sln

# Run the API
dotnet run --project src/Dealoware.Api

# In another terminal, verify the health endpoint
curl http://localhost:5287/health
# Expected: {"status":"ok"}
```

### Running Tests

```bash
# Run all tests (xUnit integration tests)
dotnet test Dealoware.sln

# Run only PoC negotiation scenario tests (S1-S14)
dotnet test --filter "FullyQualifiedName~PocNegotiationScenarioTests"

# Run specific scenario (e.g., S9 non-complementary intents)
dotnet test --filter "FullyQualifiedName~S9_"
```

#### Newman/Postman Tests (Optional)

```bash
# Install Newman (first time only)
npm install

# Start API in one terminal
dotnet run --project src/Dealoware.Api

# Run Newman tests in another terminal
./scripts/run-newman.sh
```

See [docs/qa/2026-09-20__qa__guide__poc-test-suite.md](docs/qa/2026-09-20__qa__guide__poc-test-suite.md) for full test documentation.

### Authentication (PoC)

The API uses API key and/or JWT authentication. All protected endpoints require a valid credential in the `Authorization` header.

#### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DEALOWARE_JWT_SIGNING_KEY` | JWT signing key (min 32 chars) | Development placeholder |
| `DEALOWARE_JWT_LIFETIME_MINUTES` | JWT token lifetime | 60 |

> **Security:** Never commit real signing keys. Use environment variables for production.

#### Register a Participant (Bootstrap)

```bash
curl -X POST http://localhost:5287/auth/register \
  -H "Content-Type: application/json" \
  -d '{"displayName": "My Participant"}'
# Returns 201 with sub, apiKey (shown once only), apiKeyPrefix, createdAt
```

**Response:**
```json
{
  "sub": "participant:550e8400-e29b-41d4-a716-446655440000",
  "displayName": "My Participant",
  "apiKey": "dlw_AbCdEfGh_...rest...",
  "apiKeyPrefix": "AbCdEfGh",
  "createdAt": "2026-09-11T10:00:00Z"
}
```

> **Important:** Store the API key securely—it cannot be retrieved again.

#### Issue JWT Token

```bash
curl -X POST http://localhost:5287/auth/token \
  -H "Content-Type: application/json" \
  -d '{"apiKey": "dlw_AbCdEfGh_..."}'
# Returns 200 with accessToken, tokenType, expiresIn, sub
```

#### Using Authentication

**Option 1: API Key**
```bash
curl http://localhost:5287/artifacts \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..."
```

**Option 2: JWT Bearer Token**
```bash
curl http://localhost:5287/artifacts \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Option 3: API Key as Bearer**
```bash
curl http://localhost:5287/artifacts \
  -H "Authorization: Bearer dlw_AbCdEfGh_..."
```

#### Rotate API Key

```bash
curl -X POST http://localhost:5287/auth/rotate-key \
  -H "Authorization: ApiKey dlw_OldKey_..."
# Returns 200 with new apiKey, apiKeyPrefix, revokedKeyPrefix
```

#### Revoke Credentials

```bash
# Revoke API key
curl -X POST http://localhost:5287/auth/revoke \
  -H "Authorization: ApiKey dlw_CurrentKey_..." \
  -H "Content-Type: application/json" \
  -d '{"apiKey": "dlw_KeyToRevoke_..."}'

# Revoke JWT by JTI
curl -X POST http://localhost:5287/auth/revoke \
  -H "Authorization: ApiKey dlw_CurrentKey_..." \
  -H "Content-Type: application/json" \
  -d '{"tokenJti": "jwt-id-from-token"}'
```

#### OIDC Principal Mapping

The Participant `sub` claim is OIDC-shaped and maps directly to `Artifact.OwnerParticipantId`:

| Claim | Source | Example |
|-------|--------|---------|
| `sub` | Participant registration | `participant:550e8400-e29b-41d4-a716-446655440000` |
| `iss` | Always | `dealoware` |
| `aud` | Always | `dealoware-api` |

### Artifact API (PoC)

The Artifact API provides create/get/list-own operations for the core Artifact model (D1-D5).

**Authentication:** All artifact endpoints require a valid `Authorization` header (fail-closed). The authenticated principal's `sub` claim becomes the artifact owner.

#### Create Artifact

```bash
curl -X POST http://localhost:5287/artifacts \
  -H "Content-Type: application/json" \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..." \
  -d '{
    "entities": [
      {
        "name": "2020 Toyota Camry",
        "description": "Well-maintained sedan",
        "properties": [
          {"name": "mileage", "type": "number", "value": "45000"},
          {"name": "color", "type": "string", "value": "silver"}
        ],
        "facts": ["Single owner", "Clean title", "Regular maintenance"]
      }
    ],
    "intent": "sell",
    "values": [
      {"amount": 22000.00, "currency": "USD"}
    ],
    "locations": ["Los Angeles, CA"],
    "timePeriods": [
      {"start": "2026-09-01T00:00:00Z", "end": "2026-12-31T23:59:59Z"}
    ]
  }'
# Returns 201 with artifact including server-generated id
# OwnerParticipantId = authenticated principal's sub claim
```

#### Get Artifact

```bash
curl http://localhost:5287/artifacts/{artifact-id} \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..."
# Returns 200 if owned, 404 if not found or not owned (no cross-owner leak)
```

#### List Own Artifacts

```bash
curl http://localhost:5287/artifacts \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..."
# Returns 200 with array of artifacts owned by authenticated principal (empty if none)
```

#### Health Check (No Auth Required)

```bash
curl http://localhost:5287/health
# Returns 200 {"status":"ok"} — no authentication required
```

#### Error Responses

- **401 Unauthorized**: Missing or invalid `Authorization` header
- **400 Bad Request**: Validation errors (e.g., missing entities, duplicate currency)
- **404 Not Found**: Artifact not found or not owned by requester (authn ≠ authz)

### Negotiation API (PoC - D7-D10)

The Negotiation API enables 1:1 negotiations between two participants around an Artifact.

**Key Concepts:**
- **1:1 Negotiations**: Exactly two parties (partyA and partyB) negotiate around exactly one Artifact
- **Complementary Intents**: Parties must have complementary intents (buy↔sell, provide↔consume, rent↔rent)
- **Offers**: Each party can place offers; one open offer per side at a time
- **Accept/Decline/Counter**: Only the offer recipient (toParticipantId) can accept, decline, or counter

**Authentication:** All negotiation/offer endpoints require a valid `Authorization` header (fail-closed). Non-party requests return 404 (no information leak).

#### Create Negotiation (D7)

```bash
curl -X POST http://localhost:5287/negotiations \
  -H "Content-Type: application/json" \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..." \
  -d '{
    "artifactId": "artifact-uuid-here",
    "counterpartyParticipantId": "participant:other-uuid",
    "callerIntent": "sell",
    "counterpartyIntent": "buy",
    "startsAt": "2026-09-20T00:00:00Z",
    "endsAt": "2026-09-30T23:59:59Z"
  }'
# Returns 201 with negotiation including status "Open"
# Caller = partyA (from principal sub), counterparty = partyB
```

**Complementary Intent Pairs (PoC):**

The PoC supports ~40 intent tokens across 20 directional pairs. Common examples:

| Seeker Intent | Provider Intent | Use Case |
|--------------|-----------------|----------|
| buy | sell | Product purchase |
| order | fulfill | E-commerce |
| consume | provide | Service consumption |
| rent | rent out | Short-term rental |
| lease | lease out | Long-term lease |
| borrow | lend | Temporary possession |
| visit | host | Hospitality |
| see | show | Demos/viewings |
| attend | present | Events |
| access | grant | Permissions |
| subscribe | publish | Content subscriptions |
| seek | offer | General fallback |

See [Complementary Intent Pairs](docs/product/2026-09-20__product__guide__poc-negotiation-scenarios.md#complementary-intent-pairs) for the full catalog.

#### Get Negotiation

```bash
curl http://localhost:5287/negotiations/{negotiation-id} \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..."
# Returns 200 with negotiation + embedded offers (if party)
# Returns 404 if not found or not a party (no leak)
```

#### Place Offer (D8 - P4)

```bash
curl -X POST http://localhost:5287/negotiations/{negotiation-id}/offers \
  -H "Content-Type: application/json" \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..." \
  -d '{
    "amount": 1500.00,
    "currency": "USD",
    "terms": "Payment within 30 days"
  }'
# Returns 201 with offer status "Open"
# Caller = fromParticipantId, other party = toParticipantId
# 400 if caller already has an open offer (one-open-per-side)
# 409 if negotiation is Closed or Expired
```

#### Accept Offer (D8)

```bash
curl -X POST http://localhost:5287/offers/{offer-id}/accept \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..."
# Returns 200 with offer status "Accepted"
# All other open offers in negotiation → Cancelled
# Caller must be offer's toParticipantId (recipient)
# NO contact/PII in response — see #7 for contact exchange
```

#### Decline Offer (D8)

```bash
curl -X POST http://localhost:5287/offers/{offer-id}/decline \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..."
# Returns 200 with offer status "Declined"
# Caller must be offer's toParticipantId (recipient)
```

#### Counter Offer (D8)

```bash
curl -X POST http://localhost:5287/offers/{offer-id}/counter \
  -H "Content-Type: application/json" \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..." \
  -d '{
    "amount": 1300.00,
    "currency": "USD",
    "terms": "Revised terms"
  }'
# Returns 201 with new counter-offer status "Open"
# Prior offer → "Superseded"
# Caller must be offer's toParticipantId (recipient)
# 400 if caller already has another open offer
```

#### Close Negotiation (D9)

```bash
curl -X POST http://localhost:5287/negotiations/{negotiation-id}/close \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..."
# Returns 200 with negotiation status "Closed"
# ALL open offers → Cancelled
# Post-close mutations → 409 Conflict
```

#### Negotiation Expiration (D10)

If `endsAt` is set on a negotiation and the current time exceeds it, the negotiation automatically becomes **Expired** on any mutating operation (or GET):

- **Expired negotiations**: All open offers → Cancelled
- **Post-expiry mutations**: place/accept/decline/counter/close → 409 Conflict
- **GET still works**: Party can still view the expired negotiation

```bash
# Creating a negotiation with expiration
curl -X POST http://localhost:5287/negotiations \
  -H "Content-Type: application/json" \
  -H "Authorization: ApiKey dlw_AbCdEfGh_..." \
  -d '{
    "artifactId": "artifact-uuid",
    "counterpartyParticipantId": "participant:uuid",
    "callerIntent": "sell",
    "counterpartyIntent": "buy",
    "endsAt": "2026-09-25T23:59:59Z"
  }'
```

#### Exercise Full Negotiation Flow

```bash
# 1. Register two participants
PARTICIPANT_A=$(curl -s -X POST http://localhost:5287/auth/register \
  -H "Content-Type: application/json" \
  -d '{"displayName": "Seller"}')
API_KEY_A=$(echo $PARTICIPANT_A | jq -r '.apiKey')
SUB_A=$(echo $PARTICIPANT_A | jq -r '.sub')

PARTICIPANT_B=$(curl -s -X POST http://localhost:5287/auth/register \
  -H "Content-Type: application/json" \
  -d '{"displayName": "Buyer"}')
API_KEY_B=$(echo $PARTICIPANT_B | jq -r '.apiKey')
SUB_B=$(echo $PARTICIPANT_B | jq -r '.sub')

# 2. Create an artifact (seller)
ARTIFACT=$(curl -s -X POST http://localhost:5287/artifacts \
  -H "Content-Type: application/json" \
  -H "Authorization: ApiKey $API_KEY_A" \
  -d '{
    "entities": [{"name": "Used Car", "description": "2020 Toyota Camry"}],
    "intent": "sell",
    "values": [{"amount": 22000, "currency": "USD"}]
  }')
ARTIFACT_ID=$(echo $ARTIFACT | jq -r '.id')

# 3. Create a negotiation (seller initiates)
NEGOTIATION=$(curl -s -X POST http://localhost:5287/negotiations \
  -H "Content-Type: application/json" \
  -H "Authorization: ApiKey $API_KEY_A" \
  -d "{
    \"artifactId\": \"$ARTIFACT_ID\",
    \"counterpartyParticipantId\": \"$SUB_B\",
    \"callerIntent\": \"sell\",
    \"counterpartyIntent\": \"buy\"
  }")
NEGOTIATION_ID=$(echo $NEGOTIATION | jq -r '.id')

# 4. Seller places first offer
OFFER_A=$(curl -s -X POST http://localhost:5287/negotiations/$NEGOTIATION_ID/offers \
  -H "Content-Type: application/json" \
  -H "Authorization: ApiKey $API_KEY_A" \
  -d '{"amount": 22000, "currency": "USD", "terms": "Cash only"}')
OFFER_A_ID=$(echo $OFFER_A | jq -r '.id')

# 5. Buyer counters
COUNTER=$(curl -s -X POST http://localhost:5287/offers/$OFFER_A_ID/counter \
  -H "Content-Type: application/json" \
  -H "Authorization: ApiKey $API_KEY_B" \
  -d '{"amount": 18000, "currency": "USD", "terms": "Financing available"}')
COUNTER_ID=$(echo $COUNTER | jq -r '.id')

# 6. Seller accepts the counter
curl -X POST http://localhost:5287/offers/$COUNTER_ID/accept \
  -H "Authorization: ApiKey $API_KEY_A"

# 7. View final negotiation state
curl http://localhost:5287/negotiations/$NEGOTIATION_ID \
  -H "Authorization: ApiKey $API_KEY_A" | jq
```

#### PoC Playbook

For comprehensive scenario coverage and interactive testing:

- **[PoC Negotiation Scenarios](docs/product/2026-09-20__product__guide__poc-negotiation-scenarios.md)** — Full catalog of happy paths, error cases, and edge cases with expected outcomes
- **[Postman Collection](postman/Dealoware-PoC-Negotiations.postman_collection.json)** — Import into Postman to run scenarios interactively

**Quick Postman Setup:**
1. Import: File → Import → `postman/Dealoware-PoC-Negotiations.postman_collection.json`
2. Set `baseUrl` variable if not `http://localhost:5287`
3. Run folders in order: `01-Bootstrap` → `02-Happy-Path-Deal` → etc.

#### Negotiation Error Responses

- **401 Unauthorized**: Missing or invalid `Authorization` header
- **400 Bad Request**: Non-complementary intents, same party A and B, already has open offer
- **404 Not Found**: Negotiation/Offer not found OR caller is not a party (no information leak)
- **409 Conflict**: Negotiation Closed/Expired, offer not Open, illegal state transition

### Identity Seal (PoC Stub)

The identity-seal feature protects counterparty contact information until a deal is accepted. In PoC, this is implemented as a **stub** — the foundation for MVP contact-on-accept (P7/A9).

#### Current Behavior (PoC)

- **Opaque IDs only**: All Negotiation/Offer responses expose participant identifiers in opaque format (`participant:{uuid}`) — no email, phone, address, or other contact PII
- **`identitySealed: true`**: All Negotiation and Offer responses include this flag, indicating contact is protected
- **State-only responses**: Accept/Decline/Counter/Close return offer/negotiation state only — no contact release event, no PII enrichment
- **Leak-proof design**: DTOs are designed without contact fields; tests verify no PII leaks on any happy-path flow

#### Example Response

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "artifactId": "660e8400-e29b-41d4-a716-446655440001",
  "partyAParticipantId": "participant:770e8400-e29b-41d4-a716-446655440002",
  "partyBParticipantId": "participant:880e8400-e29b-41d4-a716-446655440003",
  "partyAIntent": "sell",
  "partyBIntent": "buy",
  "status": "Open",
  "identitySealed": true,
  "createdAt": "2026-09-20T10:00:00Z"
}
```

#### MVP Roadmap (P7/A9)

The PoC stub prepares for MVP contact-on-accept:

| Feature | PoC (Current) | MVP (P7/A9) |
|---------|---------------|-------------|
| Identity protection | Stub: opaque IDs, `identitySealed: true` | Real: encrypted contact vault |
| Contact on accept | Not implemented | Contact released to both parties |
| PII vault | Not implemented | Secure storage with retention/erasure |
| Audit trail | Not implemented | Contact access logging |

#### Out of Scope (PoC)

- Real contact release on accept
- Mature PII vault (retention/erasure → post-MVP)
- Strategy/AI features
- Cognito/SSO integration
- MotorMarket/DC4 integration
- AWS resource provisioning

#### Verification

Run identity-seal leak-proof tests:

```bash
dotnet test --filter "FullyQualifiedName~IdentitySealTests"
```

These tests verify:
- No contact PII field names in any response JSON
- No email/phone patterns in response content
- `identitySealed: true` present on all Negotiation/Offer responses
- Accept/Decline/Counter/Close return state only

### Project Structure

```
Dealoware.sln
├── src/
│   ├── Dealoware.Api          # ASP.NET Core Minimal APIs (runnable host)
│   ├── Dealoware.Domain       # Domain layer (Artifact aggregate, D1-D5)
│   ├── Dealoware.Application  # Application layer (DTOs, validation, mapping)
│   └── Dealoware.Infrastructure # Infrastructure layer (EF Core + SQLite)
└── tests/
    └── Dealoware.Api.Tests    # Integration tests
```

### Database

The PoC uses SQLite for local persistence. The database file (`dealoware.db`) is created automatically on first run.

**Connection string precedence:**
1. `ConnectionStrings:DefaultConnection` in appsettings.json
2. `DEALOWARE_CONNECTION_STRING` environment variable
3. Default: `Data Source=dealoware.db`

> **Note:** Never commit real credentials. Use environment variables or secure configuration for production.

### AWS Host Shape (Callout Only)

- **Target:** Amazon ECS Express Mode (Fargate)
- **Not deployed by this Story** — PoC stays local/$0 until spend approved
- **App Runner is NOT the target platform**
- No AWS account or resource provision in this Story
- Any paid AWS provision → escalate CPM → COO → CEO

The optional `Dockerfile` is for local `docker build`/`docker run` only — not for prod IAM, Secrets Manager, ECR promotion, or image signing.

## Docs

- [Product brief](docs/product/PRODUCT-BRIEF.md)
- [PoC Negotiation Scenarios](docs/product/2026-09-20__product__guide__poc-negotiation-scenarios.md)
- [Postman Collection](postman/Dealoware-PoC-Negotiations.postman_collection.json)
- [Contributing](CONTRIBUTING.md)

## License

[Apache License 2.0](LICENSE)
