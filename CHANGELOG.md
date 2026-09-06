# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Remediation of two code reviews, the second of which reviewed the result of the first. The theme is "never
report a success that did not happen, never lose data that did arrive, and never leak a payload while reporting
a failure".

### Fixed (second review pass)

- **A placeholder could redirect a message.** Placeholder values share the `recipients[i][…]` wire namespace with
  the phone number, so `Placeholders["recipient"]` replaced the destination. The reserved name, empty names and
  names containing `[`/`]` are now rejected with `ArgumentException`, and the whole list is validated before the
  first batch is sent.
- **The documented `groups` string broke campaign deserialization.** `campaign/get` documents `groups` as
  `"12,34"`, while the SDK expected an array, so the whole campaign failed to parse. Both shapes are accepted now
  (and a non-numeric element is a protocol error, not a dropped ID).
- **Campaign settings nested in `extra` were lost.** Real responses put `validity`, `mclass` and
  `trackShortLinkRecipients` inside `extra`; the SDK only read the top-level placement the documentation
  describes, so `Validity` came back `null` for a real campaign. Both placements now work, and the object is
  exposed as `CampaignExtra`.
- **`campaign/list` discarded statistics.** The API documents list items as `campaign/getInfo` objects, so items
  are read as `CampaignInfo` and callers no longer need a `GetInfoAsync` call per campaign to see counters.
- **A missing add-recipients payload counted as success.** `code 0` with `data:null` produced
  `Outcome = AllAdded, Entries = null`; a synchronous batch must now describe its recipients.
- **The "tolerant" contact-card converter discarded real data.** Any array and any scalar became `null`, so
  `"email":"alice@example.com"` silently vanished and the next update wrote an empty value over it. Only `[]`,
  `""` and `null` mean "unset" now; a bare string maps onto `Value` (the shape the write path sends); anything
  else is a protocol error. Unknown `type`/`gender` strings are preserved in `TypeRaw`/`GenderRaw` and echoed
  back on update instead of being cleared.
- **Fixture sanitizer produced invalid JSON and missed personal data.** Regex substitution turned
  `{"id":12345678}` into the unquoted token `7000000XXXX`, and left e-mail addresses, Latin names and short
  one-time codes untouched. It now walks the parsed JSON, redacts by field name and value shape while keeping
  each value's JSON type, and re-parses its own output.
- **Capture tool leaked the API key into the URL** and mis-polled TaskQueue: it treated a synchronous
  `campaign/send` result as a task id and passed it as `ids[0]` instead of `id`. The key is a body field now, the
  endpoint must be absolute HTTPS, and only a genuine code-100 response is polled.
- `First`/`Count`/`Single` on contact cards asked for 1 or 2 items; the documented list endpoints accept 25, 50 or
  100, so these probes now request 25.

### Fixed

- Group/file recipient loads now require code 100 and a positive task ID; missing, null, array and non-positive
  task payloads fail with a protocol error. Synchronous batches reject queued responses and scalar task payloads,
  preserving confirmed progress if an earlier batch completed.
- Contact-card filter values now execute numeric and user-defined casts before formatting instead of discarding
  the conversion (for example, `(long)1.9` correctly sends `1`).
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
  returned; without one, the first page is requested.
- **`SingleOrDefaultAsync`/`SingleAsync`** now check uniqueness across the whole query (first page plus the
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
- `CampaignExtra` and `CampaignData.Extra`; `ContactFieldInfo.TypeRaw`, `MobileFieldInfo.TypeRaw`,
  `ContactCard.GenderRaw` (typed value plus the API's own spelling, as the webhook DTOs already do).

### Changed (behaviour; review before upgrading)

- **`ApiUrl` must be `https://`.** An `http://` endpoint now fails `Validate()` unless `AllowInsecureHttp = true`.
  Relative URLs, non-http(s) schemes, query strings, fragments and user info are rejected. A path prefix is allowed.
- **`Timeout`** must be positive or `Timeout.InfiniteTimeSpan`; zero/negative no longer silently means "leave the
  default". The owning constructor and DI apply the value unconditionally (infinite included).
- **Response code 100 is opt-in per operation.** Only `campaign/send` and group/file `campaign/addRecipients` accept it; any
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

### Changed (second review pass; review before upgrading)

- **`campaign/list` items are `CampaignInfo` instances.** The method signature is unchanged
  (`MobizonListResult<CampaignData>`), so existing code compiles; cast an item to read its counters.
- **`ContactCard.Gender` and `ContactFieldInfo.Type`/`MobileFieldInfo.Type` are computed** over new raw string
  properties. Reading and assigning them is unchanged; JSON now round-trips the API's own spelling.
- **Contact fields with an unexpected non-empty shape throw** instead of reading as `null`.
- **`RawMobizonApi.BuildUrl` no longer takes an API key** (capture tool, not a shipped package).
- Contact-card `First`/`Count`/`Single` fetch 25 rows instead of 1 or 2.

### Migration

- `http://` endpoints: switch to `https://` (Mobizon serves both) or set `AllowInsecureHttp = true` for a local test server.
- `Timeout = TimeSpan.Zero` as "no timeout": use `System.Threading.Timeout.InfiniteTimeSpan`.
- Code that expected an empty `AddRecipientsResult`/zeroed link statistics on odd payloads now gets a `MobizonException`.
- Tests asserting on body snippets in `MobizonException.Message` need updating; assert on operation/status/path.
- Code passing `Placeholders["recipient"]` must rename that placeholder: it now throws instead of silently
  changing the destination number.
- Code reading `Validity`/`MessageClass`/`TrackShortLinkRecipients` as always-null workarounds can drop them.
- A link update that relied on "null preserves the expiration date" must now pass the existing date explicitly;
  per the documentation an omitted date makes the link permanent.

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
