# PoC Scenario → Test Method Mapping

Maps scenario IDs from [docs/product/2026-09-20__product__guide__poc-negotiation-scenarios.md](../docs/product/2026-09-20__product__guide__poc-negotiation-scenarios.md) to automated test methods in `Dealoware.Api.Tests/PocNegotiationScenarioTests.cs`.

## Intent Pairs in This Suite

- `buy` ↔ `sell`
- `provide` ↔ `consume`

## Scenario Coverage

| Scenario | Priority | Test Method(s) | Description |
|----------|----------|----------------|-------------|
| **S1.1** | P0 | `S1_1_RegisterSeller_Returns201WithCredentials` | Register Seller participant |
| **S1.2** | P0 | `S1_2_RegisterBuyer_Returns201WithCredentials` | Register Buyer participant |
| **S2.1** | P0 | `S2_1_SellerCreatesArtifact_Returns201WithId` | Seller creates sell artifact |
| **S3.1** | P0 | `S3_1_SellerCreatesNegotiation_Returns201Open` | Create negotiation (sell↔buy) |
| **S4.1** | P0 | `S4_1_SellerPlacesOffer_Returns201Open` | Seller places first offer |
| **S5.1** | P0 | `S5_1_BuyerCounters_Returns201SupersedesOriginal` | Buyer counters offer |
| **S6.1** | P0 | `S6_1_SellerAcceptsCounter_Returns200Accepted` | Seller accepts counter |
| **S6.2** | P0 | `S6_2_ViewFinalNegotiationState_ContainsAcceptedOffer` | View final negotiation state |
| **S1→S6** | P0 | `Scenario_S1_to_S6_HappyPathDeal` | **E2E happy path** (register×2 → artifact → negotiation → offer → counter → accept) |
| **S7.1** | P0 | `S7_1_BuyerDeclinesOffer_Returns200Declined` | Buyer declines offer |
| **S8.1** | P0 | `S8_1_CloseNegotiation_Returns200Closed` | Close negotiation (cancels offers) |
| **S9.1** | P0 | `S9_1_NonComplementaryIntents_Returns400` | Non-complementary intents → 400 |
| **S9** | P0 | `S9_NonComplementaryPairs_AllReturn400` | Theory: multiple non-complementary pairs |
| **S10** | P0 | `S10_NonPartyGet_Returns404NotFound` | Non-party GET → 404 (no info leak) |
| **S11** | P0 | `S11_SecondOpenOfferSameSide_Returns400` | Duplicate open offer → 400 |
| **S12** | P0 | `S12_MutationsAfterClose_Return409` | Mutations after close → 409 |
| **S13** | P1 | `S13_ExpiredNegotiation_MutationsReturn409_GETReturnsOK` | Expiration via endsAt |
| **S14.1** | P1 | `S14_1_ProvideConsume_Returns201` | Provide↔Consume negotiation |
| **S14** | P1 | `S14_ComplementaryPairs_BuySellProvideConsume_Succeed` | Theory: buy↔sell, provide↔consume |

## Running Specific Scenarios

```bash
# Run all PoC scenario tests
dotnet test --filter "FullyQualifiedName~PocNegotiationScenarioTests"

# Run E2E happy path
dotnet test --filter "FullyQualifiedName~Scenario_S1_to_S6_HappyPathDeal"

# Run specific scenario (e.g., S9)
dotnet test --filter "FullyQualifiedName~S9_"

# Run all P0 scenarios (S1-S12)
dotnet test --filter "FullyQualifiedName~PocNegotiationScenarioTests&FullyQualifiedName~S[1-9]_|S1[0-2]_"
```

## Out of Scope

Per QA brief (LOCKED):
- rent↔rent tests (removed from suite)
- Multi-complement matching
- Newman CI nightly wiring
