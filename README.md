# Dealoware

**Universal Negotiation Platform**

Negotiate *anything* — goods, services, collectibles, exchanges — between people and AI agents.

Dealoware is an open-source intermediary for strategy-driven negotiation: register Artifacts, discover complementary intents, and let AI assistants run offers, counters, and guardrails. Contact stays protected until an offer is accepted. No chatroom theater — talk to your own negotiation agent; the platform keeps the deal honest.

**Apache License 2.0.** Hosted platform remains AIKnowHow. This repo is the OSS core you can read, run, and improve.

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
dotnet test Dealoware.sln
```

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
- [Contributing](CONTRIBUTING.md)

## License

[Apache License 2.0](LICENSE)
