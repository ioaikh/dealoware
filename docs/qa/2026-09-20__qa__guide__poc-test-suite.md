# PoC Test Suite Guide

| Field | Value |
|-------|-------|
| **Date** | 2026-09-20 |
| **Type** | Guide |
| **Scope** | PoC automated test suite |
| **Companion** | [Scenario Guide](../product/2026-09-20__product__guide__poc-negotiation-scenarios.md) |

## Overview

This document describes how to run the automated test suite for the Dealoware PoC. The suite covers all negotiation scenarios (S1-S14) from the [PoC Negotiation Scenarios Guide](../product/2026-09-20__product__guide__poc-negotiation-scenarios.md).

## Test Suite Components

### 1. xUnit Integration Tests (`tests/Dealoware.Api.Tests/`)

C# integration tests using `WebApplicationFactory` to test the API in-memory.

| Test File | Coverage |
|-----------|----------|
| `PocNegotiationScenarioTests.cs` | S1-S14 scenario coverage (bootstrap, happy path, negatives) |
| `NegotiationEndpointTests.cs` | Negotiation endpoint edge cases |
| `IdentitySealTests.cs` | Identity seal / PII leak-proof tests |
| `ArtifactEndpointTests.cs` | Artifact CRUD operations |
| `AuthEndpointTests.cs` | Authentication flow tests |

### 2. Postman Collection (`postman/`)

Newman-runnable Postman collection for interactive and CI testing.

| Collection | Purpose |
|------------|---------|
| `Dealoware-PoC-Negotiations.postman_collection.json` | End-to-end negotiation flows |

## Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 16+ (for Newman/Postman tests only)

### Run xUnit Tests (Recommended)

```bash
# Run all tests
dotnet test

# Run only PoC scenario tests
dotnet test --filter "FullyQualifiedName~PocNegotiationScenarioTests"

# Run specific scenario (e.g., S9 non-complementary intents)
dotnet test --filter "FullyQualifiedName~S9_"

# Run with verbose output
dotnet test --verbosity normal
```

### Run Newman/Postman Tests

```bash
# Install dependencies (first time only)
npm install

# Start API in one terminal
dotnet run --project src/Dealoware.Api

# Run Newman in another terminal
./scripts/run-newman.sh

# Or with npm
npm run test:newman
```

## Scenario Coverage Matrix

| Scenario | Description | xUnit Test | Postman Folder |
|----------|-------------|------------|----------------|
| S1.1 | Register Seller | `S1_1_RegisterSeller_*` | 01-Bootstrap |
| S1.2 | Register Buyer | `S1_2_RegisterBuyer_*` | 01-Bootstrap |
| S2.1 | Create Sell Artifact | `S2_1_SellerCreatesArtifact_*` | 01-Bootstrap |
| S3.1 | Create Negotiation (sell↔buy) | `S3_1_SellerCreatesNegotiation_*` | 02-Happy-Path-Deal |
| S4.1 | Seller Places Offer | `S4_1_SellerPlacesOffer_*` | 02-Happy-Path-Deal |
| S5.1 | Buyer Counters | `S5_1_BuyerCounters_*` | 02-Happy-Path-Deal |
| S6.1 | Seller Accepts Counter | `S6_1_SellerAcceptsCounter_*` | 02-Happy-Path-Deal |
| S6.2 | View Final State | `S6_2_ViewFinalNegotiationState_*` | 02-Happy-Path-Deal |
| S7.1 | Buyer Declines | `S7_1_BuyerDeclinesOffer_*` | 03-Alternate-Decline |
| S8.1 | Close Negotiation | `S8_1_CloseNegotiation_*` | 04-Alternate-Close |
| S9.1 | Non-Complementary Intents | `S9_1_NonComplementaryIntents_*` | 05-Negative-Cases |
| S10 | Non-Party GET → 404 | `S10_NonPartyGet_*` | 05-Negative-Cases |
| S11 | Duplicate Open Offer | `S11_SecondOpenOfferSameSide_*` | 05-Negative-Cases |
| S12 | Mutate After Close | `S12_MutationsAfterClose_*` | 05-Negative-Cases |
| S13 | Expiration via EndsAt | `S13_ExpiredNegotiation_*` | 06-Optional-Expiration |
| S14.1 | Provide ↔ Consume | `S14_1_ProvideConsume_*` | 07-Optional-Other-Intents |
| S14 | Complementary pairs (buy↔sell, provide↔consume) | `S14_ComplementaryPairs_*` | Theory |

## Intent Pairs in This Suite

Per QA brief (LOCKED), this suite covers **buy↔sell** and **provide↔consume** only:

| Caller Intent | Counterparty Intent | Use Case |
|---------------|---------------------|----------|
| `buy` | `sell` | Product purchase |
| `sell` | `buy` | Product sale |
| `provide` | `consume` | Service offering |
| `consume` | `provide` | Service seeking |

**Invalid pairs** (return 400):
- Same intent on both sides (buy↔buy, sell↔sell, etc.)
- Cross-category mismatches (e.g., `buy↔provide`, `sell↔consume`)

## CI Integration

Tests run automatically on push/PR via GitHub Actions:

```yaml
# .github/workflows/ci.yml
- Run: dotnet test
```

## Troubleshooting

### Tests fail with connection refused

Ensure the API is running for Newman tests:
```bash
dotnet run --project src/Dealoware.Api
curl http://localhost:5287/health  # Should return {"status":"ok"}
```

### xUnit tests hang

Check if EF Core migrations are pending:
```bash
dotnet ef database update --project src/Dealoware.Infrastructure
```

### Newman reports 401 Unauthorized

The collection auto-captures API keys. Run folders in order starting with `01-Bootstrap`.

## Adding New Tests

1. **New scenario**: Add test method to `PocNegotiationScenarioTests.cs` with `S{N}_` prefix
2. **New edge case**: Add to appropriate `*Tests.cs` file
3. **New Postman request**: Add to appropriate folder in collection, include Test scripts

## See Also

- [Scenario Map](../../tests/ScenarioMap.md) — S* → test method mapping
- [PoC Negotiation Scenarios](../product/2026-09-20__product__guide__poc-negotiation-scenarios.md)
- [Postman Collection](../../postman/Dealoware-PoC-Negotiations.postman_collection.json)
- [IntentComplement.cs](../../src/Dealoware.Domain/Negotiations/IntentComplement.cs)
