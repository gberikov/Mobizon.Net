# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - UNRELEASED

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
