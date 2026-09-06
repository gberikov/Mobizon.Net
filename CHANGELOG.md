# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Remediation of the 2026-09-06 code review (`docs/handoffs/2026-09-06-review-remediation.md`). The theme is
"never report a success that did not happen, and never leak a payload while reporting a failure".

### Fixed

- **API errors were masked by deserialization errors.** The envelope (`code`, `message`) is now read before `data`
  is bound to the result type, so `{"code":1,"data":{"text":["required"]}}` on `Campaigns.CreateAsync` throws
  `MobizonApiException` (`RawCode = 1`, `FieldErrors["text"]`) instead of a JSON error about `long`.
- **False success.** HTTP 500 with `{}` on `CreateAsync` used to return id `0`. Now: a non-2xx status never succeeds
  (an error envelope keeps its API code and the HTTP status; anything else is a `MobizonException`), a missing or
  non-numeric `code` is a protocol error, a success envelope without a payload is a protocol error (except for the
  contact-card reads observed to answer `data:null`), and create-style calls reject a zero/negative/missing id.
- **Multi-batch `AddRecipientsAsync` outcome.** 501+ recipients with batch codes 0 then 99 reported `NoneAdded`.
  The outcome is now aggregated: any accepted batch plus any rejected batch is `PartiallyAdded`; `NoneAdded` only
  when every batch was rejected. Entries accumulate in one list (linear) instead of re-concatenating per batch.
- **`FirstOrDefaultAsync`/`FirstAsync` ignored the selected page offset.** `Take(50).Page(2).FirstOrDefaultAsync()`
  sent `currentPage=2&pageSize=1` (offset 2). With an explicit `Page` the configured page is fetched and its head
  returned; without one, a single item of page 0 is requested (unchanged).
- **`SingleOrDefaultAsync`/`SingleAsync`** now check uniqueness across the whole query (page 0, size 2, plus the
  server-side total) and ignore `Page`/`Take`. **`CountAsync`** always queries page 0.
- **Payload leaks in diagnostics.** Exception messages no longer quote the response body or field values (neither in
  `Message`, `InnerException` nor `ToString()`); they carry the operation, HTTP status and JSON path instead. The
  JSON reader's own exception is no longer attached for non-JSON bodies because it quotes the offending token.
- `MobizonClient(HttpClient, null)` threw `NullReferenceException`; now `ArgumentNullException`.
- Polly back-off delay could overflow `long` for large `RetryCount`; computed in floating point and validated.

### Added

- `MobizonApiException.FieldErrors` — per-field validation messages from the error `data` object (nested paths
  flattened as `mobile.value`); empty when the API sent none or sent another shape.
- `AddRecipientsResult.IsQueued` and `AddRecipientsProgress` (attached to any exception thrown by a multi-batch
  send via `Exception.Data`, read with `AddRecipientsProgress.FromException`): confirmed entries, confirmed recipient
  count and the size of the in-flight batch whose outcome is unknown.
- `MobizonClientOptions.AllowInsecureHttp`; `Validate()` now checks the URL, `ApiVersion` and `Timeout`.
- `MobizonClient.MaxResponseContentBufferSize` (16 MB) applied to SDK-owned `HttpClient`s (owning constructor and DI).
- `MobizonResilienceOptions.Validate()`; called by `AddMobizonResilience`.
- `AlphanameData.StatusUpdated`, `GlobalComment`, `PartnerComment`, `Documents` (`AlphanameDocument`).
- `WebhookFieldItem.FieldTypeKind` (`WebhookFieldType?`: `TextString`, `Email`, `Mobile`; `null` for anything
  else while `FieldType` keeps the raw string).
- `MessageService.GetSmsStatusMaxIds` (100) and argument guards on public methods (`ArgumentNullException` /
  `ArgumentException` before any HTTP call).
- `docs/coverage-matrix.md`: per-endpoint verification status (official docs / capture / unverified).

### Changed (behaviour; review before upgrading)

- **`ApiUrl` must be `https://`.** An `http://` endpoint now fails `Validate()` unless `AllowInsecureHttp = true`.
  Relative URLs, non-http(s) schemes, query strings, fragments and user info are rejected. A path prefix is allowed.
- **`Timeout`** must be positive or `Timeout.InfiniteTimeSpan`; zero/negative no longer silently means "leave the
  default". The owning constructor and DI apply the value unconditionally (infinite included).
- **Response code 100 is opt-in per operation.** Only `campaign/send` and `campaign/addRecipients` accept it; any
  other operation answering 100 throws `MobizonApiException(RawCode = 100)` instead of returning a half-bound result.
  Group/file `addRecipients` no longer accept 98/99 (those are synchronous-batch codes).
- **Multipart uploads are never retried by the Polly package**, even with `RetryNonIdempotentRequests = true`.
- **Uploaded streams are never closed by the SDK** (`ContactCard.Photo`, `RecipientsFile`); the caller owns them.
  Previously the stream could be disposed by `HttpClient` on .NET Framework.
- `Campaigns.DeleteAsync`, `Links.UpdateAsync`, `ContactGroups.UpdateAsync`, contact-card `setgroups`/`delete`,
  stop-list range create/delete validate the envelope without deserializing the meaningless `data:true`.
- `campaign/addRecipients` `data` that is neither an array nor a task id, and `link/getStats` counters that are not
  32-bit integers, are protocol errors (`MobizonException`) instead of a silent empty result or `0`. PHP's empty
  `[]` and `null`/`""` counters are still accepted as "no data"/`0`.
- Numeric and enum converters no longer include the offending wire value in their messages.
- The JSON body is parsed straight from the buffered response stream (no intermediate UTF-16 string). Structural
  improvement; not benchmarked.

### Migration

- `http://` endpoints: switch to `https://` (Mobizon serves both) or set `AllowInsecureHttp = true` for a local test server.
- `Timeout = TimeSpan.Zero` as "no timeout": use `System.Threading.Timeout.InfiniteTimeSpan`.
- Code that expected an empty `AddRecipientsResult`/zeroed link statistics on odd payloads now gets a `MobizonException`.
- Tests asserting on body snippets in `MobizonException.Message` need updating; assert on operation/status/path.

## [0.1.0] - 2026-08-31

First public release.

### Packages

- `Mobizon.Net` — client for the Mobizon SMS gateway REST API v1 (`netstandard2.0`, `net8.0`)
- `Mobizon.Contracts` — interfaces, DTOs, enums, exceptions
- `Mobizon.Net.Extensions.DependencyInjection` — `AddMobizon()` on `IServiceCollection` (`IHttpClientFactory`)
- `Mobizon.Net.Extensions.Polly` — `AddMobizonResilience()`: retry for read-only calls, circuit breaker for all
- `Mobizon.Net.Webhooks` — signature verification and typed parsing of inbound webhooks
- `Mobizon.Net.Webhooks.AspNetCore` — `AddMobizonWebhooks()` / `MapMobizonWebhook()` (`net8.0`, `net10.0`)

### API coverage

- Message: SendSmsMessage (incl. `shortenLinks`, `validity`, `deferredToTs`, `mclass`), GetSMSStatus, List
- Campaign: Create, AddRecipients (auto-batched by 500, file upload), Send, Get, GetInfo, GetLinks, List, Delete
- Link: Create, Get (by id / code / short URL), GetLinks, GetStats (period-major grid transposed per link), List, Update, Delete
- User: GetOwnBalance · Taskqueue: GetStatus · Alphaname: List
- ContactCard: LINQ-style `Where/OrderBy/Take/Page` query, Find, Add, Update, Remove, SetGroups, GetGroups
- ContactGroup: List, Create, Update, Delete, GetCardsCount · NumberStopList: List, AddNumber, AddNumberRange, Delete
- Webhook events: `sms-delivery-report`, `form-submission`, `form-contact-confirmation`, `form-contact-unsubscribe` (+ forward-compatible `UnknownWebhookEvent`)

### Behaviour worth knowing

- Contact-card filters spell fields and values exactly as the write path and the API's responses do: `gender` is
  lowercase (`male`), `birth_date` is date-only, and the address postal code is `address.postalcode`.
- A contact-card filter expression that cannot be expressed on the wire throws `NotSupportedException` instead of
  being silently mis-sent or crashing: the member must be on the left-hand side, `!= null` and `!= Gender.Undefined`
  have no "not empty" operator to map to, and a closed-over `ids.Contains(x.GroupId)` is not a `contain` filter.
- `ContactCard.Photo`/`PhotoFileName` make the (already implemented) multipart photo upload reachable from
  `AddAsync`/`UpdateAsync`. One-shot and write-only: the call consumes the stream and resets both properties to
  `null`; the API never returns a photo.
- The API key is sent in the POST body, never in the URL; every request is a POST and carries `User-Agent: Mobizon.Net/<version>`.
- All wire formatting is culture-invariant.
- `MobizonApiException` for API error codes; `MobizonException` (with `StatusCode`) for transport errors, `HttpClient` timeouts and non-JSON responses. Caller cancellation is never wrapped.
- Codes 98/99/100 are folded into `AddRecipientsResult.Outcome` and `CampaignSendResult.IsQueued` instead of throwing.
- Polly retries only `get*`/`list` calls unless `RetryNonIdempotentRequests = true`.
- Webhook verification fails closed: no secret / no signature → `SignatureMismatch`.
