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

### Project Structure

```
Dealoware.sln
├── src/
│   ├── Dealoware.Api          # ASP.NET Core Minimal APIs (runnable host)
│   ├── Dealoware.Domain       # Domain layer (placeholder)
│   ├── Dealoware.Application  # Application layer (placeholder)
│   └── Dealoware.Infrastructure # Infrastructure layer (placeholder)
└── tests/
    └── Dealoware.Api.Tests    # Integration tests
```

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
