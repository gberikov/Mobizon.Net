# Phase 0 Research: Mobizon Webhooks

All Technical Context unknowns were resolved during `/speckit.clarify` (3 decisions) and the constitution amendment. This document records the remaining design decisions and their rationale.

## D1. Signature algorithm & verification

- **Decision**: Recompute `SHA1(eventId|attempt|eventCreateTs|secretKey)` as a lowercase hex string and compare to the payload's `sign` using a constant-time comparison. Verification fails closed on any missing input, parse error, or mismatch.
- **Rationale**: This is exactly the algorithm documented by Mobizon (separator `|`, SHA1, fields in that order). Constant-time comparison prevents timing side channels (FR-002). Fail-closed satisfies FR-003 and the "missing signature material" edge case.
- **Implementation note**: Use `System.Security.Cryptography.SHA1` (BCL, no dependency). Compare via a fixed-time byte/char comparison (e.g. accumulate XOR differences) rather than `string.Equals`. The signing string MUST be built from the **raw** `eventCreateTs` string as received (D4), never a reparsed/reformatted value.
- **Alternatives considered**: HMAC-SHA1 — rejected, Mobizon uses a plain salted SHA1 concatenation, not HMAC. `string ==` comparison — rejected (timing leak).

## D2. Package boundaries & dependency reuse

- **Decision**: Three locations. (1) `Mobizon.Contracts` holds event DTOs, the `WebhookEventType` enum, and the three service interfaces. (2) `Mobizon.Net.Webhooks` (netstandard2.0) holds the verify/parse/processor implementations and its own internal JSON converters. (3) `Mobizon.Net.Webhooks.AspNetCore` (net8.0) holds request-binding + handler glue.
- **Rationale**: Principle II (contracts separated) + Principle I (dependency-free core, extensions opt-in) + Principle VIII (webhook packages registered). The webhook core must NOT reference `Mobizon.Net` (the API client) — that would invert the intended dependency direction and drag in the HTTP client.
- **Consequence — converter duplication**: The existing `SmsStatusConverter` and `MobizonDateTimeConverter` live in `Mobizon.Net/Internal/Converters` and are `internal` to the API-client package. Since the webhook core cannot depend on `Mobizon.Net`, it gets its **own** small internal converters (D3, D4). Accepted as minor, intentional duplication.
- **Alternatives considered**:
  - Promote the converters into `Mobizon.Contracts` for reuse — rejected for this feature: larger refactor touching the existing API-client code, out of scope; can be done later if duplication grows.
  - Put webhook code inside `Mobizon.Net` — rejected: violates Principle VIII (separate packages) and Principle I (dependency-free core / opt-in extensions).

## D3. SMS delivery status mapping (reuse vs forward-compat)

- **Decision**: Reuse the existing `SmsStatus` enum from `Mobizon.Contracts` (FR-010). Provide a webhook-specific, **non-throwing** converter: known strings (`DELIVRD`, `UNDELIV`, `REJECTD`, `EXPIRD`, etc.) map to enum values; the typed `Status` is exposed alongside a `StatusRaw` string, and an unrecognised status does not throw.
- **Rationale**: Consumers handle webhook statuses the same way as polled statuses (FR-010). But the production `SmsStatusConverter` throws on unknown values, which conflicts with forward-compatibility (FR-011/FR-012) — a future final-status string must not crash the handler. Keeping `StatusRaw` preserves fidelity.
- **Note**: SMS delivery webhooks fire only on **final** statuses (`DELIVRD`/`UNDELIV`/`REJECTD`/`EXPIRD` per Mobizon), but the converter accepts the full set defensively.
- **Alternatives considered**: Reuse the throwing converter — rejected (breaks FR-011/012). New webhook-only status enum — rejected (FR-010 mandates the existing representation).

## D4. Timestamp representation

- **Decision**: Expose every timestamp (`eventCreateTs`, `statusUpdateTs`, `confirmationTs`, `unsubscribeTs`) as a parsed `DateTimeOffset?`, AND keep the original `eventCreateTs` string verbatim on the envelope (`EventCreateTsRaw`) for signature use. Empty/absent values (e.g. `confirmationTs: ""`) parse to `null` rather than failing.
- **Rationale**: Q3 clarification. Mobizon timestamps are `yyyy-MM-dd HH:mm:ss` with no offset; parse as the documented Mobizon timezone-agnostic local value into `DateTimeOffset` for convenience while preserving the exact signed bytes for verification (avoids any round-trip/format mismatch that would break the signature — FR-019/FR-020).
- **Implementation note**: Custom `JsonConverter<DateTimeOffset?>` using exact format `yyyy-MM-dd HH:mm:ss`, returning `null` for empty string / `null` / missing.
- **Alternatives considered**: Raw strings only — rejected (poor DX). `DateTime` — rejected in favor of `DateTimeOffset` per Q3.

## D5. Event model shape (polymorphic vs generic)

- **Decision**: An abstract `MobizonWebhookEvent` base carrying the common envelope, with concrete subclasses per event type (`SmsDeliveryReportEvent`, `FormSubmissionEvent`, `FormContactConfirmationEvent`, `FormContactUnsubscribeEvent`) and an `UnknownWebhookEvent` (exposes the raw `JsonElement` data). The parser returns the base type; consumers `switch` on it (C# pattern matching) or read `EventType`.
- **Rationale**: Idiomatic, discoverable, and forward-compatible — an unknown `eventType` yields `UnknownWebhookEvent` with the envelope still populated and the signature still verifiable (FR-011, US3).
- **Alternatives considered**: `MobizonWebhookEvent<TData>` generic — rejected: the parser can't know `TData` until it reads `eventType`, forcing an awkward `object`/cast API. A single class with nullable per-type properties — rejected (sprawling, ambiguous).

## D6. Combined verify+parse entry point

- **Decision**: Provide two overloads. `Process(string body, string secretKey)` for the single-secret case, and `Process(string body, Func<MobizonWebhookEvent,string> secretSelector)` for the per-webhook-secret case (the selector receives the parsed event and returns the secret, typically keyed by `WebhookId` — FR-004a). Both return `WebhookProcessResult { WebhookProcessStatus Status; MobizonWebhookEvent? Event; bool IsAuthentic }` where `Status ∈ { Ok, ParseError, SignatureMismatch }`. Order: parse envelope → (select secret) → verify signature → parse typed data. `Event` is populated for both `Ok` and `SignatureMismatch`; null only on `ParseError`. Parse errors are also surfaced via `WebhookParseException` from the lower-level `IWebhookParser.Parse`, distinct from signature mismatch ("malformed body" vs "bad signature").
- **Rationale**: FR-016 single combined operation; encodes the recommended verify→parse→ack→process flow and lets the ASP.NET layer map `Ok`→200, `SignatureMismatch`→403, `ParseError`→400. The selector overload avoids the chicken-and-egg of needing a secret before reading `webhookId`: the envelope is parsed (untrusted) to pick the secret, which then gates authenticity.
- **Alternatives considered**: Throw on mismatch — rejected (exceptions for expected control flow); a single bool return — rejected (can't distinguish 400 vs 403).

## D7. ASP.NET Core integration surface

- **Decision**: Minimal-API endpoint helper `endpoints.MapMobizonWebhook(pattern, handler)` plus `services.AddMobizonWebhooks(options)`. `MobizonWebhookOptions.SecretKeyResolver` is `Func<IServiceProvider, MobizonWebhookEvent, string>` so the secret can be chosen per `WebhookId` (FR-004a); a single shared secret is the trivial case (ignore the event arg). The helper reads the request body async, parses, resolves the secret from the parsed event, verifies, returns `Results.Ok()` / `Results.StatusCode(403)` / `Results.BadRequest()`, and invokes the consumer's `Func<MobizonWebhookEvent, CancellationToken, Task>` only on `Ok`. Verify+parse complete before the status is returned (FR-018a) so acknowledgement fits Mobizon's 5-second window.
- **Rationale**: FR-018; matches the constitution's DI convention (`AddMobizon()`-style). Async only where there is real I/O (request stream). Targets `net8.0` so modern minimal APIs/`IResult` are available; keeps these types out of the netstandard core.
- **Alternatives considered**: MVC `ControllerBase` base class — rejected (heavier, less flexible than minimal-API + DI). Middleware that swallows the pipeline — rejected (less explicit than an endpoint).

## D8. Testing strategy

- **Decision**: Store the official sample payloads from the Mobizon docs as JSON fixtures under `tests/Mobizon.Net.Webhooks.Tests/Payloads/`. Tests (written first, TDD) assert: full-field parsing per event type (SC-002), accept-valid / reject-tampered / reject-missing signature (SC-003), unknown `eventType` and extra fields do not throw (SC-004), empty timestamp → null. ASP.NET helper tested via `WebApplicationFactory` for the 200/403/400 mapping.
- **Rationale**: Principle III (TDD, ≥90%), and the payloads are the contract's ground truth. No outbound HTTP, so no `MockHttpMessageHandler`.

## Resolved unknowns summary

| Unknown | Resolution |
|---------|-----------|
| Integration scope | Core primitives + separate ASP.NET package (clarify Q1) |
| Inbound formats | JSON only (clarify Q2) |
| Timestamp type | `DateTimeOffset?` + preserved raw string (clarify Q3) |
| Constitution conflict | Amended to v1.2.0, Principle VIII added |
| Status converter reuse | Reuse `SmsStatus` enum; new non-throwing webhook converter (D3) |
| Converter duplication | Accepted; webhook core has own internal converters (D2) |
| ASP.NET target framework | `net8.0` (D7) |
| Per-webhook secrets | Resolver keyed by parsed `WebhookId`; `Process` selector overload (D6, D7, FR-004a) |
| Ack timing | Verify+parse synchronous before HTTP status; handler defers heavy work (D7, FR-018a) |
