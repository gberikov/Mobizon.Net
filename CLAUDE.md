# Mobizon.Net — working notes

An unofficial .NET SDK for the Mobizon SMS gateway REST API v1. This file records the conventions that are not
obvious from the code. `README.md` is the user-facing documentation; keep it and the XML docs in step with any
behaviour change.

Everything committed here is in English: source, comments, XML documentation, tests, fixtures and docs. The
library is used internationally, so English is the only language in the repository.

## Layout

```text
src/Mobizon.Contracts                          DTOs, enums, exceptions, service interfaces (no HTTP)
src/Mobizon.Net                                HTTP client, services, JSON converters, contact-card query
src/Mobizon.Net.Extensions.DependencyInjection AddMobizon() for IServiceCollection
src/Mobizon.Net.Extensions.Polly               AddMobizonResilience(): retry + circuit breaker
src/Mobizon.Net.Webhooks                       signature verification and typed parsing of inbound events
src/Mobizon.Net.Webhooks.AspNetCore            MapMobizonWebhook() endpoint helpers
tests/Mobizon.Net.Tests                        SDK tests (MockHttp; no network)
tests/Mobizon.Net.Webhooks.Tests               webhook tests
samples/Mobizon.Net.ConsoleSample              one runnable sample per endpoint
tools/Mobizon.Net.ApiCapture                   captures and sanitizes live responses into test fixtures
docs/coverage-matrix.md                        per-endpoint coverage and how each field was verified
docs/api-shapes.md                             response shapes as actually captured on 2026-06-24
```

## Targets and build settings

Six shipped packages target `netstandard2.0` and `net8.0`, except `Mobizon.Net.Webhooks.AspNetCore`, which
targets `net8.0` and `net10.0`. Tests, sample and capture tool are `net8.0`.

`global.json` pins the SDK to 10.0.4xx or newer; `Mobizon.Net.Webhooks.AspNetCore` targets `net10.0`, so an
older SDK cannot build the solution.

`Directory.Build.props` applies to every project: `LangVersion` 14.0, nullable reference types enabled,
warnings as errors, XML documentation generated (`CS1591` suppressed), MinVer versioning from `v*` tags,
SourceLink and symbol packages.

The language version is a compiler setting only — the emitted assemblies still target `netstandard2.0` and
`net8.0`, and a consumer on .NET 8 or .NET Framework is unaffected. A few features need BCL types the older
targets lack, and those fail loudly at compile time on the `netstandard2.0` leg: `record`, `init` and
`required` need `IsExternalInit`, `RequiredMemberAttribute`, `CompilerFeatureRequired` and
`SetsRequiredMembers` declared as internal shims; `allows ref struct` needs .NET 9 and cannot be polyfilled.
Everything syntax-only — primary constructors, file-scoped namespaces, collection expressions, the `field`
keyword, null-conditional assignment — compiles on both targets as is.

`Directory.Packages.props` holds every package version (central package management): a `PackageReference` in
a csproj carries no `Version`. Shipped packages stay on 8.0.x because that version is the floor a consumer
inherits; the sample, the capture tool and the test packages are free to track current.

```bash
dotnet build                          # the solution is Mobizon.Net.slnx (XML format, SDK 9.0.200+)
dotnet test                           # 517 tests, no network required
dotnet build -c Release
```

## Conventions that matter

**Transport.** The API key is a body field on every request and must never appear in a URL. Every call is a
POST. The envelope (`code`, `message`) is parsed before `data` is bound to a result type, because an error
response puts field errors where the success payload would be. Response code 100 is opt-in per operation.

**Errors.** `MobizonApiException` for an API error code, `MobizonException` for transport and protocol
failures; a caller's `CancellationToken` is never wrapped. Exception messages carry the operation, HTTP status
and JSON path — never the response body or a field value, in `Message`, `InnerException` or `ToString()`. SMS
text, phone numbers and one-time codes must not reach a log through an exception.

**Never lose data.** A value the SDK does not recognise is preserved, not dropped: an unknown contact `type` or
`gender` keeps its raw string and is echoed back on update, an unknown webhook event becomes
`UnknownWebhookEvent`, and an unknown field value leaves the typed property null beside a raw one. A payload
shape the SDK cannot map is a protocol error, never a silent null or zero. Public DTOs stay domain types;
`JsonElement` is fine internally and, for a genuinely unknown webhook event, on `UnknownWebhookEvent.RawData`.

**Formatting.** All wire values go through `ApiFormat` and are culture-invariant. A test that touches dates or
enum casing should run under `tr-TR` as well.

**Ownership.** Streams passed in by the caller are read but never closed. An `HttpClient` the SDK creates gets
the configured timeout and response-size cap; an injected one is left alone. Writes are not retried by default,
and multipart uploads are never retried.

**Verification.** `docs/coverage-matrix.md` distinguishes "confirmed by documentation", "confirmed by capture"
and "unverified". Do not upgrade a claim there without evidence, and do not invent enum values or fields.

## Fixtures and captures

Fixtures in `tests/Mobizon.Net.Tests/Payloads/` come from the capture tool, which writes raw responses to
`artifacts/api-captures/` (git-ignored, may contain personal data). Sanitize before copying:

```bash
dotnet run --project tools/Mobizon.Net.ApiCapture              # read-only calls
dotnet run --project tools/Mobizon.Net.ApiCapture -- --send    # also sends a real SMS and spends balance
dotnet run --project tools/Mobizon.Net.ApiCapture -- --sanitize
```

The sanitizer walks the parsed JSON, redacts by field name and value shape, keeps each value's JSON type and
re-parses its output. It is best effort: read the diff before committing a fixture. Never commit a raw capture,
a real API key, or `appsettings.Development.json`.
