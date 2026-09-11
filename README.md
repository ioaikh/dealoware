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

### Artifact API (PoC)

The Artifact API provides create/get/list-own operations for the core Artifact model (D1-D5).

**Authentication (PoC interim):** Use the `X-PoC-Owner-Id` header to identify the participant. All artifact operations require this header.

> **Future:** When #5 auth is implemented, the API will prefer a validated JWT `sub` claim if available, falling back to `X-PoC-Owner-Id` for backward compatibility.

#### Create Artifact

```bash
curl -X POST http://localhost:5287/artifacts \
  -H "Content-Type: application/json" \
  -H "X-PoC-Owner-Id: participant-123" \
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
```

#### Get Artifact

```bash
curl http://localhost:5287/artifacts/{artifact-id} \
  -H "X-PoC-Owner-Id: participant-123"
# Returns 200 if owned, 404 if not found or not owned (no cross-owner leak)
```

#### List Own Artifacts

```bash
curl http://localhost:5287/artifacts \
  -H "X-PoC-Owner-Id: participant-123"
# Returns 200 with array of artifacts owned by this participant (empty array if none)
```

#### Error Responses

- **401 Unauthorized**: Missing `X-PoC-Owner-Id` header
- **400 Bad Request**: Validation errors (e.g., missing entities, duplicate currency)
- **404 Not Found**: Artifact not found or not owned by requester

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
