# Design: Release Readiness (0.1.0) — metadata, correctness, public API shape

**Date:** 2026-08-30
**Status:** Approved
**Type:** Pre-release cleanup. Breaking changes are allowed — nothing has ever been published to nuget.org (verified: `api.nuget.org/v3-flatcontainer/mobizon.net/index.json` → 404) and the repo has no tags.
**Branch:** `feature/release-readiness` (off `develop`)
**Target version:** **`0.1.0`** — first public release (tag `v0.1.0`).
**Source:** Full-library code review of 2026-08-30 (DX, release readiness, API coverage vs. mobizon.kz / mobizon.gmbh docs).

## Problem

The library is functionally complete (all publicly documented Mobizon API v1 methods plus four undocumented modules are covered; 246 tests green; 0 warnings) but cannot be published as-is:

1. **Package metadata is wrong** — project/repository URLs point at `github.com/mobizon/mobizon-dotnet` (404); the package calls itself "Official"; CHANGELOG claims a `1.0.0` that was never published; CI cannot version a tag build (shallow clone + no tags).
2. **Security/correctness defects** — the API key travels in the query string (logged verbatim by `HttpClient` logging/proxies); non-2xx responses and timeouts surface as opaque exceptions; date formatting is culture-sensitive; `Links.DeleteAsync` swallows partial failure; Polly retries non-idempotent SMS sends; `ContactCards` misses `ConfigureAwait(false)`.
3. **Public API shape is un-idiomatic** — 11 namespaces (README quick start does not compile), `TaskStatus` collides with `System.Threading.Tasks.TaskStatus`, IDs/flags/dates typed inconsistently (`string`/`int?` where `long`/`bool?`/`DateTime?`/enums belong), `IMobizonClient : IDisposable` + transient DI, a phantom `TaskQueueStatus.Id`, `SmsMessageParameters` lacks `shortenLinks`.

Once `0.1.0` ships, every item in (3) becomes a breaking change. Fix all of it now.

## Goals

- Ship `Mobizon.Net`, `Mobizon.Contracts`, `Mobizon.Net.Extensions.DependencyInjection`, `Mobizon.Net.Extensions.Polly`, `Mobizon.Net.Webhooks`, `Mobizon.Net.Webhooks.AspNetCore` as **`0.1.0`** with truthful metadata, SourceLink + symbols, from a tag-driven CI.
- No secret in URLs; actionable exceptions (HTTP status, body snippet, timeout distinguished from caller cancellation).
- Culture-proof wire formatting.
- One flat public namespace per package; consistent types; no BCL name collisions.
- README/CHANGELOG/XML docs that match the code.

## Non-goals

- New API coverage beyond `params[shortenLinks]` (everything documented is already covered).
- Source-generated JSON / AOT / trimming.
- Removing the spec-kit tooling directories (`.specify/`, `specs/`, `.claude/commands/`) — dev tooling, not shipped.
- `samples/Mobizon.Net.Playground` (git-ignored, not in the solution) — will need manual fixes after this refactor; out of scope.
- `tools/Mobizon.Net.ApiCapture` keeps its own raw URL builder (it intentionally mirrors the documented curl form).

## Decisions

### D1 — Package metadata & repo hygiene

| Item | Decision |
|---|---|
| `PackageProjectUrl`, `RepositoryUrl` | `https://github.com/gberikov/Mobizon.Net` |
| `Authors` | `Gany Berikov` |
| `Copyright` | `Copyright (c) 2026 Gany Berikov` (LICENSE line updated to match) |
| `Company` | removed |
| `Description` (Mobizon.Net) | `Unofficial .NET SDK for the Mobizon SMS gateway REST API v1: messages, campaigns, short links, balance, background tasks, contact groups and cards, number stop-list, sender IDs (alphanames).` |
| README first paragraph | starts with "An **unofficial** .NET SDK…" |
| Symbols / SourceLink | `IncludeSymbols=true`, `SymbolPackageFormat=snupkg`, `PublishRepositoryUrl=true`, `EmbedUntrackedSources=true`, `Microsoft.SourceLink.GitHub` 8.0.0 (PrivateAssets=all), `ContinuousIntegrationBuild=true` when `GITHUB_ACTIONS=true` |
| Empty `tests/Mobizon.Net.IntegrationTests` | deleted (project + `.sln` entry) |
| `.claude/settings.local.json` | untracked (`git rm --cached`) and added to `.gitignore` |
| Dead code | `MobizonApiClient.SendJsonAsync` (no callers) removed |
| CHANGELOG | rewritten: single `## [0.1.0] - <release date>` section describing the first public release; the fictitious `[1.0.0] - 2026-02-24` entry is removed |

### D2 — CI (`.github/workflows/ci.yml`)

- `actions/checkout@v4` with `fetch-depth: 0` (MinVer needs tag history).
- `actions/setup-dotnet@v4` with both `8.0.x` and `10.0.x` (multi-target below).
- `permissions: contents: read` at workflow level.
- `dotnet nuget push "*.nupkg" --skip-duplicate` (the matching `.snupkg` files are pushed automatically by the NuGet client when they sit next to the `.nupkg`).
- Publish job unchanged otherwise (tag `v*` → nuget.org with `NUGET_API_KEY`).

### D3 — Target frameworks

| Project | Was | Becomes |
|---|---|---|
| `Mobizon.Contracts`, `Mobizon.Net`, `Mobizon.Net.Webhooks`, `Mobizon.Net.Extensions.DependencyInjection`, `Mobizon.Net.Extensions.Polly` | `netstandard2.0` | `netstandard2.0;net8.0` |
| `Mobizon.Net.Webhooks.AspNetCore` | `net8.0` | `net8.0;net10.0` |
| test projects, sample, tool | `net8.0` | unchanged |

`System.Text.Json` `PackageReference` is conditioned on `'$(TargetFramework)' == 'netstandard2.0'` (in-box on net8.0). `HttpRequestMessage.Properties` is obsolete on net5+ → the one place that needs a per-request marker uses `#if NET5_0_OR_GREATER` / `HttpRequestOptions`.

### D4 — Transport: API key in the body, all methods POST, User-Agent

- Every request is `POST {ApiUrl}/service/{module}/{method}?output=json&api={ApiVersion}`. The `apiKey` is a **form field** (or a multipart string part). Mobizon documents that any parameter, including `apiKey`, may be sent as a POST parameter.
- `user/getOwnBalance` switches from GET to POST (documented as accepting POST like every other method).
- `MobizonApiClient.SendAsync` loses its `HttpMethod` parameter: `SendAsync<T>(string module, string apiMethod, IDictionary<string,string>? parameters, CancellationToken ct = default, int[]? extraSuccessCodes = null)`.
- Every request carries `User-Agent: Mobizon.Net/<InformationalVersion without +sha>` (set per request, because the `HttpClient` may be shared/injected).
- Read-only API methods are marked idempotent on the outgoing `HttpRequestMessage` (see D6). Rule: method name starts with `get` or equals `list` (case-insensitive). Verified against the full method set: `getSMSStatus, get, getInfo, getlinks, getstats, list, getownbalance, getstatus, getgroups, getcardscount` are reads; `sendsmsmessage, create, delete, send, addrecipients, update, setgroups` are writes.

### D5 — Exceptions

- `MobizonException` gains `HttpStatusCode? StatusCode { get; }` and a constructor `(string message, HttpStatusCode? statusCode, Exception? innerException = null)`.
- `MobizonApiException` gains an optional `HttpStatusCode? statusCode` constructor argument, passed to the base.
- `SendCoreAsync` flow:
  1. `HttpClient.SendAsync` — `OperationCanceledException` **when the caller's token is cancelled** → rethrow unchanged; any other `OperationCanceledException` (i.e. `HttpClient.Timeout`) → `MobizonException("Request to Mobizon API timed out after {Timeout}.", statusCode: null, inner)`; other exceptions → `MobizonException("Failed to send request to Mobizon API: …", null, inner)` (unchanged message).
  2. Read body; on failure → `MobizonException("Failed to read Mobizon API response", response.StatusCode, inner)`.
  3. Deserialize the envelope. On `JsonException` **or** a `null` result: if `!IsSuccessStatusCode` → `MobizonException($"Mobizon API returned HTTP {(int)code} ({code}) with a non-JSON body: {first 200 chars}", code, inner)`; else → `MobizonException($"Failed to deserialize Mobizon API response (HTTP {(int)code}): {first 200 chars}", code, inner)`.
  4. A successfully parsed envelope with a non-success code → `MobizonApiException(rawCode, message, response.StatusCode)` (unchanged semantics; HTTP status now attached).
- `HttpResponseMessage` is disposed after the body is read.

### D6 — Polly: retry only idempotent requests

- `MobizonResilienceOptions.RetryNonIdempotentRequests` (`bool`, default `false`).
- The retry policy is attached via `AddPolicyHandler(Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>>)`: requests marked idempotent (D4) — or any request when `RetryNonIdempotentRequests == true` — get the retry policy; others get `Policy.NoOpAsync<HttpResponseMessage>()`.
- Circuit breaker is unchanged and applies to every request; it stays a single instance per registration.
- Marker plumbing: `internal static class RequestMarkers { MarkIdempotent(HttpRequestMessage); IsIdempotent(HttpRequestMessage) }` in `Mobizon.Net.Internal`, exposed to the Polly package via `InternalsVisibleTo("Mobizon.Net.Extensions.Polly")`.

### D7 — Culture-proof formatting

`internal static class ApiFormat` in `Mobizon.Net.Internal` is the single place that renders values for the wire:

```csharp
DateTimeFormat = "yyyy-MM-dd HH:mm:ss";  DateFormat = "yyyy-MM-dd";
string DateTime(DateTime)  // InvariantCulture
string Date(DateTime)      // InvariantCulture
string Int(long)           // InvariantCulture
string Bool(bool)          // "1" / "0"
string Sort(SortDirection) // "ASC" / "DESC"
string Gender(Gender?)     // "male" / "female" / ""
```

Every `DateTime.ToString("yyyy…")` in the services goes through `ApiFormat`. A regression test runs a request under a culture whose default calendar is not Gregorian (`th-TH` with `ThaiBuddhistCalendar` set explicitly) and asserts the Gregorian year on the wire.

`MobizonDateTimeConverter` (read side) additionally accepts the date-only form `yyyy-MM-dd` (needed for `expirationDate`).

### D8 — Namespaces

| Was | Becomes |
|---|---|
| `Mobizon.Contracts.Models.{Alphanames,Campaigns,Common,ContactCards,ContactGroups,Links,Messages,StopLists,TaskQueues,Users}` | `Mobizon.Contracts` |
| `Mobizon.Contracts.Services` (API service interfaces, `IMobizonClient`) | `Mobizon.Contracts` |
| `Mobizon.Contracts.Exceptions` (`MobizonException`, `MobizonApiException`) | `Mobizon.Contracts` |
| `Mobizon.Contracts.Models.Webhooks`, `IWebhookParser`, `IWebhookProcessor`, `IWebhookSignatureVerifier`, `WebhookParseException` | `Mobizon.Contracts.Webhooks` |
| `Mobizon.Net.ContactCards` (`ContactCardSet`, `ContactCardQuery`) | `Mobizon.Net` |
| `Mobizon.Net.Internal.*`, `Mobizon.Net.Services`, `Mobizon.Net.Webhooks*`, `Mobizon.Net.Extensions.*` | unchanged |

Folder layout is left as-is (namespaces need not mirror folders). XML `cref="Exceptions.…"` references become `cref="…"`.

### D9 — Type and naming fixes (public surface)

| Type / member | Was | Becomes | Notes |
|---|---|---|---|
| `TaskStatus` (enum) | `Mobizon.Contracts.Models.TaskQueues.TaskStatus` | `BackgroundTaskStatus` | BCL collision |
| `TaskQueueStatus.Id` | `long Id` | **removed** | API returns only `{progress,status}` |
| `IMobizonClient` | `: IDisposable` | no base interface | `MobizonClient` keeps `IDisposable` |
| `ILinkService.DeleteAsync` | `Task` | `Task<DeleteResult>` | |
| `DeleteContactGroupResult` | own type | **replaced by** `DeleteResult { IReadOnlyList<long> Processed; IReadOnlyList<long> NotProcessed; }` in `Mobizon.Contracts` | used by Links and ContactGroups |
| `IContactCardSet.SetGroupsAsync` | `(long id, IReadOnlyList<string> groupIds, ct)` | `(long id, IReadOnlyList<long> groupIds, ct)` | |
| `ContactGroupRef.Id` | `string?` | `long?` | |
| `AddRecipientsRequest.RecipientGroups` | `IReadOnlyList<string>?` | `IReadOnlyList<long>?` | `RecipientContacts` stays `string` — the API accepts `{cardId}:{fieldKey}` |
| `CampaignCriteria.Groups` | `IReadOnlyList<string>?` | `IReadOnlyList<long>?` | |
| `CampaignCriteria.Type` | `int?` | `CampaignType?` | |
| `MessageListRequest.WithNumberInfo` | `int?` | `bool?` | |
| `ICampaignService.GetInfoAsync` | `(long id, int? getFilledTplCampaignText = null, ct)` | `(long id, bool? fillTemplateText = null, ct)` | wire `1`/`0` |
| `BalanceResult.Balance` | `string` | `decimal` | `StringToDecimalConverter` already registered |
| `ContactCard.Gender`, `ContactCardFields.Gender`, `CreateContactCardRequest.Gender`, `UpdateContactCardRequest.Gender` | `string?` | `Gender?` | read via tolerant enum converter (`"male"`, `"MALE"`, `""` → `Male`/`null`); written as lower-case (`male`/`female`, matches the observed stored value). **Assumption to verify on the live API via the Playground; if rejected, switch `ApiFormat.Gender` to upper-case.** |
| `ContactTypeConverter` | `ContactType`-specific | generalised `TolerantStringEnumConverter<TEnum>` registered for `ContactType` and `Gender` | identical read semantics |
| `LinkData.ExpirationDate`, `LinkData.RealExpirationDate` | `string?` | `DateTime?` | read via `MobizonDateTimeConverter` (date-only form) |
| `CreateLinkRequest.ExpirationDate`, `UpdateLinkRequest.ExpirationDate` | `string?` | `DateTime?` | written `yyyy-MM-dd` |
| `GetLinkStatsRequest.DateFrom`, `DateTo` | `string?` | `DateTime?` | written `yyyy-MM-dd HH:mm:ss` (documented format) |
| `GetLinkStatsRequest.Ids` | unchecked | `1..5` items, else `ArgumentException` | documented API limit |
| `SmsMessageParameters.ShortenLinks` | — | `bool? ShortenLinks` → `params[shortenLinks]` | documented parameter, missing |
| `ICampaignService.GetLinksAsync(long campaignId, ct)` | — | added; same endpoint as `ILinkService.GetLinksAsync` (`link/getlinks`) | discoverability |
| `SortDirection` | `ASC`, `DESC` | `Ascending`, `Descending` | wire via `ApiFormat.Sort` |
| `PaginationRequest.PageSize` default | `20` | `25` | API default |
| `IContactCardQuery` / `IContactCardSet` | mutable builder; `Skip(int)` translated to a page | **immutable** builder (each call returns a new query); `Skip` **replaced by** `Page(int pageIndex)` (zero-based); `Take(int)` = page size (default 25) | removes the "skip rounds to a page" trap |
| `IContactGroupService.GetCardsCountAsync` XML doc | says pass `"-1"` | says pass `null` for ungrouped cards | |
| `IContactCardSet.UpdateAsync` XML doc | silent | states full-replace semantics: unset fields are cleared on the server (except `Address`, which is only sent when non-null) | |

### D10 — README / CHANGELOG / sample

- README: "unofficial"; quick start with `using Mobizon.Contracts; using Mobizon.Net;` only; every snippet compiles against the new surface; sections updated for: `Balance` decimal, `TaskQueueStatus` fields, `Links.DeleteAsync` result, `DateTime` link fields, `ContactCards` `Page/Take`, webhooks (select **JSON** format in the control panel; no-secret webhooks → use `IWebhookParser` directly, `WebhookProcessor` always fails closed), DI (`IMobizonClient` is not disposable; transient), Polly (retries only read-only calls by default; `RetryNonIdempotentRequests`), error handling (`StatusCode`, timeout → `MobizonException`), User-Agent, API key sent in the POST body.
- CHANGELOG: `[0.1.0]` only.
- Console sample: command-driven (`dotnet run -- balance`, `dotnet run -- send <phone> <text>`, …) instead of commented-out blocks; adds a `di-balance` command demonstrating `AddMobizon` + `AddMobizonResilience`.

## Verification

- `dotnet build -c Release` → 0 warnings (TreatWarningsAsErrors stays on) for every TFM.
- `dotnet test -c Release` → all green; new tests cover: apiKey in body, User-Agent, non-JSON 5xx body, timeout vs. cancellation, Thai calendar, idempotent-only retry, `DeleteResult`, `Balance` decimal, `Gender` enum round-trip, link `DateTime` round-trip, `ShortenLinks`, `GetStats` id limit, `Page/Take`, `BackgroundTaskStatus`.
- `dotnet pack -c Release` → 6 `.nupkg` + 6 `.snupkg`; nuspec `projectUrl`/`repository url` = `https://github.com/gberikov/Mobizon.Net`; description contains "Unofficial".
- `git tag v0.1.0` on the merge commit → CI publishes `0.1.0` (manual, after user approval).
