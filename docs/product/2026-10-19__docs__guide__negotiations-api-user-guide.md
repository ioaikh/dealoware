# Dealoware Core API: negotiations user guide

This guide walks a seller and a buyer through one negotiation on the Dealoware Core API. It follows the folder order of the Postman collection `postman/Dealoware-PoC-Negotiations.postman_collection.json`. Every endpoint claim cites a file and line on main `cb372f9b`.

## Hosts

### Local default

The collection's `baseUrl` defaults to local Development at `http://localhost:5287`. Start the API locally and run the collection as it is. Swagger is enabled only in the Development environment (`src/Dealoware.Api/Program.cs:265`), so you can also browse it locally.

### Live override

The live Core API host is `https://api.core.dealoware.com`. The environment file `postman/Dealoware-api-core.postman_environment.json` is the only route to production. Import it in Postman, or run `newman run postman/Dealoware-PoC-Negotiations.postman_collection.json -e postman/Dealoware-api-core.postman_environment.json`.

> **Warning:** a run against the live host creates real participants and writes to production. Use the live override only after Ivan's go through BM (T-G-08).

Put API keys only in Postman's local current value, and never export or commit a filled environment, because the secret type only masks the value on screen.

Swagger is not available in production, so the Postman collection is the HTTP reference for the live host.

## Authentication

The `Authorization` header is the only accepted transport for credentials. It takes one of two schemes (`src/Dealoware.Api/Auth/AuthHelper.cs:21`, `src/Dealoware.Api/Auth/AuthHelper.cs:22`):

```http
Authorization: ApiKey <your-api-key>
```

```http
Authorization: Bearer <your-access-token>
```

`POST /auth/register` returns an API key once, at registration (`src/Dealoware.Api/Endpoints/AuthEndpoints.cs:23`). `POST /auth/token` exchanges an API key for a Bearer token (`src/Dealoware.Api/Endpoints/AuthEndpoints.cs:30`). The request body is:

```json
{ "apiKey": "<your-api-key>" }
```

Keep keys and tokens in the environment's secret slots (`sellerApiKey`, `buyerApiKey`, `observerApiKey`, `authDemoApiKey`, `accessToken`). Never commit filled values.

## Rate limits and HTTP 429

Register and token requests are rate limited per client (`src/Dealoware.Api/Endpoints/AuthEndpoints.cs:25`, `src/Dealoware.Api/Endpoints/AuthEndpoints.cs:32`). The default limits are 10 register requests and 30 token requests per 60 seconds (`src/Dealoware.Api/RateLimiting/AuthRateLimitOptions.cs:17`, `src/Dealoware.Api/RateLimiting/AuthRateLimitOptions.cs:18`). A rejected request gets HTTP 429 with a `Retry-After` header that gives the wait in whole seconds (`src/Dealoware.Api/RateLimiting/AuthRateLimiting.cs:98`, `src/Dealoware.Api/RateLimiting/AuthRateLimiting.cs:99`). Wait that many seconds before trying again, and don't retry in a loop. The collection scripts record the header and do not retry.

## Walkthrough in collection order

### 00-Health

Check that the host is up with `GET /health`. No credentials are needed (`src/Dealoware.Api/Program.cs:316`).

### 01-Auth

1. Register the seller with `POST /auth/register` and store the returned key as `sellerApiKey` (`src/Dealoware.Api/Endpoints/AuthEndpoints.cs:23`).
2. Register the buyer the same way and store `buyerApiKey`.
3. Exchange the seller key for a Bearer token with `POST /auth/token` and store `accessToken` (`src/Dealoware.Api/Endpoints/AuthEndpoints.cs:30`).

### 02-Artifact

The seller describes what they're selling.

- Create an artifact with `POST /artifacts` (`src/Dealoware.Api/Endpoints/ArtifactEndpoints.cs:34`).
- Read it back with `GET /artifacts/{id}` (`src/Dealoware.Api/Endpoints/ArtifactEndpoints.cs:40`).
- List your own artifacts with `GET /artifacts/` (`src/Dealoware.Api/Endpoints/ArtifactEndpoints.cs:46`).

### 03-Negotiation

- The seller opens a negotiation with `POST /negotiations` (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:35`).
- List negotiations with `GET /negotiations/`. The collection sends this one with the Bearer token (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:30`).
- Read one negotiation with `GET /negotiations/{id}` (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:42`). The `offers` field is left out of the response until the negotiation has at least one offer, so treat a missing `offers` as an empty list (`src/Dealoware.Application/Negotiations/Mapping/NegotiationMapper.cs:31`).

### 04-Offer

- The seller places an offer with `POST /negotiations/{id}/offers` (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:48`).
- List offers with `GET /offers/` (`src/Dealoware.Api/Endpoints/OfferEndpoints.cs:32`).
- The buyer reads the offer with `GET /offers/{id}` (`src/Dealoware.Api/Endpoints/OfferEndpoints.cs:37`).

### 05-Counter

The buyer answers with `POST /offers/{id}/counter` (`src/Dealoware.Api/Endpoints/OfferEndpoints.cs:57`).

### 06-Accept-or-Decline

- The seller accepts the counter with `POST /offers/{id}/accept` (`src/Dealoware.Api/Endpoints/OfferEndpoints.cs:43`). The accept response carries the contact-on-accept fields described in the collection tests.
- Check the final state with `GET /negotiations/{id}` (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:42`).
- To see a decline, open a fresh negotiation, place an offer, and have the buyer call `POST /offers/{id}/decline` (`src/Dealoware.Api/Endpoints/OfferEndpoints.cs:50`).

### 07-Close

Open a fresh negotiation, place an offer, then end it with `POST /negotiations/{id}/close` (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:56`). Closing cancels the open offer.

### 08-Negative-Cases

These requests show the errors you should expect:

- Intents that don't match return 400 from `POST /negotiations` (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:35`).
- A third party who isn't in the negotiation gets 404 from `GET /negotiations/{id}` (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:42`). The collection registers this observer with `POST /auth/register` (`src/Dealoware.Api/Endpoints/AuthEndpoints.cs:23`).
- A second offer from the same side returns 400 from `POST /negotiations/{id}/offers` (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:48`).
- An offer after close returns 409 from the same route, and closing twice returns 409 from `POST /negotiations/{id}/close` (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:56`).

### 09-Expiration

A negotiation created already expired rejects new offers with 409 (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:48`). You can still read it with `GET /negotiations/{id}` (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:42`).

### 10-Other-Intents

The same create call also supports Provide with Consume and Rent with Rent Out pairs (`src/Dealoware.Api/Endpoints/NegotiationEndpoints.cs:35`).

### 11-Optional-Rotate-and-Revoke

These steps use a separate participant so the main keys keep working.

- Register the demo participant with `POST /auth/register` (`src/Dealoware.Api/Endpoints/AuthEndpoints.cs:23`).
- Replace its key with `POST /auth/rotate-key` (`src/Dealoware.Api/Endpoints/AuthEndpoints.cs:43`).
- Revoke a key with `POST /auth/revoke` (`src/Dealoware.Api/Endpoints/AuthEndpoints.cs:37`).

## Out of scope

Admin routes are out of scope for this guide.
