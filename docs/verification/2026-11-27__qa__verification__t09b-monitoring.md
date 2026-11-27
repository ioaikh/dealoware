# T09b: monitoring, the engine part (offline steps 1 and 2)

| Field | Value |
|-------|-------|
| Scope | T09b of the T09 implementation plan, steps 1 and 2: the request ID, JSON console logs, the log-privacy test, and the tracing spike with its outcome |
| Branch | `claude/t09b-monitoring`, from `main` at `ac883aa` |
| The other part | The alerts, alarms, health check, budget and the release and smoke-test checks are in the private infrastructure repository, with its own record. The tracing spike's full answer, which names the hosting, is written there. |
| Date | 2026-11-27 |
| Result | **Built and tested.** Independent review: fix first; the two engine items are fixed with tests (below), re-review pending. |

## What was built

| Plan step | What | Where |
|-----------|------|-------|
| 1 | **The request ID.** Every request has one: the trace ID of a valid incoming W3C `traceparent`, or a new one when there is none or it is invalid (bad shape, all zeros, not hex). It is returned in the reply's `traceparent` header, also on an answer refused by a later gate (the middleware runs first). The host's own request activity carries it; when no activity exists the middleware makes one with the same parent rule. | `src/Dealoware.Api/Observability/RequestIdMiddleware.cs`, `Program.cs` |
| 1 | **JSON console logs.** One JSON object per line: `Time` (UTC, ISO 8601), `Level`, `Category`, `Message`, `TraceId` (inside a request), and for an exception `ExceptionType` (outermost first) and `StackTrace`. Never written: the entry's named values, its scopes (which carry the request path and connection details) and the exception's message. Control characters are escaped, so a message cannot break the line. Two safety nets on the message, for what the framework writes at levels the engine does not normally log: a URL's query string becomes `?<query>`, and email-shaped text becomes `<email>`. The start-up rate-limit line is now a log line too. | `src/Dealoware.Api/Observability/JsonLineConsoleFormatter.cs`, `ObservabilityExtensions.cs` |
| 1 | **The log-privacy test.** Requests carrying an email address, a bearer token, an API key and a password (in a query string, JSON bodies and the `Authorization` header, on the engine and on the admin sign-in) go through the engine with every category captured at Debug and up, through the engine's own formatter. None of the values appears, plain or URL-encoded. A second case logs an entry that carries them in its scope, its values and its exception message: none appears. | `tests/Dealoware.Api.Tests/RequestIdAndLogTests.cs` (`LogPrivacyTests`), `FormatterCapture.cs` |
| 2 | **Tracing (the spike's "yes" branch, see below).** The OpenTelemetry SDK (1.21.0) for incoming HTTP requests and the PostgreSQL driver's own activity source. Off unless `OTEL_TRACES_EXPORTER=otlp`: unset or `none` registers no tracer at all. When on, spans go over OTLP to the endpoint, protocol and headers the standard `OTEL_EXPORTER_OTLP_*` settings name. 5% of traces are sampled (`Tracing:SampleRatio`); every span is recorded, and one that ends in error (an HTTP 5xx, an exception) is exported even when not sampled. The engine names no tracing service. | `src/Dealoware.Api/Observability/ObservabilityExtensions.cs`, `Dealoware.Api.csproj` |

## The spike (plan step 2), offline

The question: can the engine send its traces from its one container, with no second container (no collector)?

- **Offline, the engine side works.** With `OTEL_TRACES_EXPORTER=otlp`, the engine's own process sent its spans over OTLP/HTTP (protobuf) to a local stand-in receiver on the loopback address, with the `Authorization` header given in `OTEL_EXPORTER_OTLP_HEADERS`. No second process was involved. Test: `TracingExportTests.Traces_WhenSwitchedOn_AreSentOverOtlpHttp_WithTheConfiguredHeader`.
- **Not tested here:** whether the receiving service accepts what the engine sends. That needs a deployment, and is not part of the offline steps.
- **Answer: yes, from one container, with conditions on the receiving side.** The conditions name the hosting, so they are written in the infrastructure repository's record, for the owner's decision. Until they are met, tracing stays off, and the request ID in the logs is what follows a request.

## Tests run

| Test | Where | Result |
|------|-------|--------|
| `RequestIdAndLogTests` (9: a new ID without `traceparent`; a valid one kept; three invalid ones replaced; the ID on a refused answer; log lines are JSON with exactly the five fields and the request's ID; one line per entry outside a request, with no `TraceId`; the engine uses the JSON formatter) | Windows, SQLite | 9 passed |
| `LogPrivacyTests` (2) and `LogRedactionTests` (6) | Windows, SQLite | 8 passed |
| `TracingExportTests` (5: off by default; off with `none`; sent over OTLP/HTTP with the header when on; the sampler; a failed span marked for export) | Windows, and a Linux .NET 10 SDK container | 5 passed. The send test failed once on Linux CI: the flush could run before the request's span had stopped, since the host stops it after the client has the answer. Fixed (`85ede73`): the test waits for its own span (by trace ID) to stop, then flushes. 20 runs in a row passed on Windows and 20 on Linux. |
| The full suite | Windows, SQLite, no PostgreSQL | 2528 passed, 19 skipped, 6 failed: the five known failures below, and `AdminAuditPostgresAppendOnlyTests.SecR2_AdminAuditPostgres_InsertWorks_UpdateDeleteTruncateRaise`, which asserts that a PostgreSQL `DB_HOST` is set and runs in CI's PostgreSQL job. None is in code this change touches. |

The local Windows baseline has five known failures outside this work (`AdminTotpTests.SecSc6_NewServiceInstanceKeepsFactorLock` and four `PostmanCollectionHygieneTests.RunNewmanScript_*`).

## Found on the way

- The framework's request line ("Request starting ... POST http://.../auth/register?email=...") carries the query string. The engine does not log it at its normal levels (`Microsoft.AspNetCore` is at Warning), but the privacy test captures Debug and up, and caught it. The formatter now removes query strings and email-shaped text from every message.
- `Program.cs` is stored with CRLF line endings in the repository, unlike most files. The first commit of this branch rewrote it with LF; it was put back before anything was pushed, so the diff shows only the lines that changed.

## Independent review: fix first

The review found no risk to the live stack. Its two engine items are fixed; each new test fails on the code before the fix.

| Item | Finding | Fix | Commit |
|------|---------|-----|--------|
| 1 | The `traceparent` header was missing on 500 answers: it was set before the rest of the pipeline ran, and a fault cleared it (the host's own 500 on the engine host; `Response.Clear()` in the admin host's 500). | The header is set when the answer starts (`Response.OnStarting`), so a clear does not remove it. Outside Development, a fault that reaches the request-ID middleware (the engine host has no other handler) is logged, type and stack only, and answered 500 with no body. Tests (`RequestIdOnFaultTests`, Production host): an endpoint throws on the engine host and on the admin host; each answer is 500, carries `traceparent`, and shows nothing of the fault; the engine's log line has the exception's type and not its message. | `a066b3e` |
| 3 | The formatter's comment said exception messages are never written, but EF Core's error events put a whole exception's text, with the database's words (which can quote a value), inside their message. | For `Microsoft.EntityFrameworkCore*` categories the message is cut at its first line break; the comment now says exactly what is and is not written. Tests: `EfCoreMessageTests` (two EF categories cut, another category kept whole). | `8627b42` |

Tests after the fixes: the new and related tests (request ID, logs, privacy, redaction, EF Core, tracing, admin security headers, admin host start-up, health) 81 passed; the full suite 2533 passed, 19 skipped, 6 failed: the same six as before (the five known ones and the one that needs PostgreSQL).

## What is not done here

- Steps 3 to 5 (the stack's alerts and the release and smoke-test checks) are in the infrastructure repository.
- Steps 6 to 8 (the deployment side and the alert test) are not offline work and wait for the owner's recorded OK.
- The log metric of the infrastructure part matches `Level` = `Error` or `Critical` in these JSON lines, so it counts only once an engine with this change is released.
