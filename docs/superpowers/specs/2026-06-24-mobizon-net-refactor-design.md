# Mobizon.Net — API-Contract & DX Refactor (Design Spec)

- **Date:** 2026-06-24
- **Status:** Approved (design); pending spec review
- **Branch:** `feature/api-contract-refactor`
- **Author:** Gany Berikov + Claude
- **Drives:** implementation plan (via `writing-plans`)

---

## 1. Context & problem

`Mobizon.Net` is a strongly-typed, async-first .NET SDK for the Mobizon SMS REST API
(`netstandard2.0` core + `net8.0` ASP.NET integration), split into `Mobizon.Contracts`,
`Mobizon.Net`, DI/Polly/Webhooks packages, tests, and a console sample.

Two review passes (verified against the official Mobizon docs across regions and the
official PHP/Node client libraries) found that several methods **deserialize the wrong
response shape** and several **public contracts disagree with the real API**. These cause
runtime deserialization errors or silent data loss. They went uncaught because
`CampaignServiceTests` is empty and `Mobizon.Net.IntegrationTests` contains no `.cs` tests.
Documentation (README, some XML examples) also drifted from the code, and a few
infra items are broken (CI never runs; package version is hardcoded).

This refactor aligns the SDK with the real API, closes coverage gaps, hardens
infra/DX, and adds a test foundation that prevents contract regressions.

## 2. Goals

- Correct every response model and request contract to match the **real** Mobizon API.
- Close coverage gaps (`recipientsFile`, `shortenLinks`, `Link/List` criteria, `Link/Get`
  by id/code/shortLink, `alphaname` module).
- Fix infrastructure & DX (README, CI, versioning, DI lifetime, webhook endpoint, sample).
- Establish fixture-based tests built from **captured real responses** so contracts can't
  silently break again.
- Reduce structural debt (adopt the unused `BracketNotationSerializer`, unify enum↔code
  mapping, consistent naming).

## 3. Non-goals / out of scope

- No new transport features (no sync API, no GET fallback).
- No change to the webhook **signature** scheme (SHA1 is dictated by Mobizon; verifier is
  already correct/constant-time/fail-closed).
- Idempotency/replay persistence stays the consumer's responsibility (documented).
- No multi-region behavioral changes beyond `ApiUrl`.

## 4. Constraints & decisions (locked)

| # | Decision | Choice |
|---|----------|--------|
| C1 | Publication status | **Pre-release — break public contracts freely**, ship as 1.0 |
| C2 | Scope/sequencing | **Phased, correctness first** (Phase 0→4) |
| C3 | Source of truth for response shapes | **Capture real API responses first**, build fixtures from them |
| C4 | List response modeling | **Generic `MobizonListResult<T>`** (migrate Message/ContactCard onto it) |
| C5 | `Link/Get` shape | **Three explicit methods**: `GetByIdAsync` / `GetByCodeAsync` / `GetByShortLinkAsync` |
| C6 | `BracketNotationSerializer` | **Adopt** it across services (remove hand-rolled bracket building) |
| C7 | Capture harness location | **`tools/Mobizon.Net.ApiCapture`** (gitignored output) |
| C8 | `alphaname` module | **In scope** (user has a registered Sender ID) |

## 5. Verified findings → fix matrix

Correctness (runtime failures / data loss):

| Area | Real API (verified) | Current SDK | Fix |
|------|---------------------|-------------|-----|
| `Campaign/List` | `data = { items, totalItemCount }` | `IReadOnlyList<CampaignData>` (`CampaignService.cs:99`,`ICampaignService.cs:88`) | return `MobizonListResult<…>` |
| `Link/List` | `{ items, totalItemCount }` | bare array (`LinkService.cs:102`) | `MobizonListResult<LinkData>` |
| `Link/GetStats` | `{ items, totals }` (`totals`, not `totalItemCount`) | bare array (`LinkService.cs:80`) | new `LinkStatsResult { Items, Totals }` |
| `Link/Get` | accepts `id` \| `code` \| `shortLink` | only `code` sent (`LinkService.cs:56,61`) | 3 methods (C5) |
| `Link/Update` | identified by `id`; fields `status/expirationDate/comment` (no `fullLink`) | requires `Code`, sends `data[fullLink]` (`UpdateLinkRequest.cs:11`,`LinkService.cs:132,136`) | key by `Id`, drop `FullLink` |
| `LinkData.clickCnt` | field name `clickCnt` (+`redirectCnt`,`moderatorStatus`,`createTs`,`moderatorComment`) | `Clicks` w/o `[JsonPropertyName]` → always 0 (`LinkData.cs:42`) | map `clickCnt`, add missing fields |

Coverage gaps:

| Area | Real API | Current SDK | Fix |
|------|----------|-------------|-----|
| `Campaign/AddRecipients` | supports `recipientsFile` upload | only file *settings*, never multipart (`AddRecipientsRequest.cs:93`,`CampaignService.cs:321`) | add `RecipientsFile`; route via existing `SendMultipartAsync` |
| `Campaign/AddRecipients` | one recipient type per call | no validation; can send mixed (`CampaignService.cs:185,290`) | pre-flight XOR validation |
| `Campaign/Create` | `data[shortenLinks]` (separate from tracking) | missing (`CreateCampaignRequest.cs:72`) | add `ShortenLinks` |
| `Link/List` | criteria (status, moderatorStatus, code, fullLink, comment, date/click ranges) | only pagination+sort (`LinkListRequest`) | add criteria |
| `alphaname` | `alphaname/list` (signatures) | absent | new `IAlphaNameService.ListAsync` + model |
| list-item models | `MessageInfo`: `uuid`,`countryA2`,`operatorName`,`segUserBuy`; `CampaignData` vs `CampaignInfo` for List items | partial | audit vs captures |

Infra / DX / docs:

| Item | Issue | Fix |
|------|-------|-----|
| CI | triggers `main`; repo uses `master`/`develop` → CI never runs (`ci.yml:5-11`) | target `master`+`develop` |
| Versioning | hardcoded `1.0.0` while publishing on `v*` tags (`Directory.Build.props:3`) | MinVer (version from git tag) |
| DI lifetime | README "scoped" vs code `AddSingleton` (`ServiceCollectionExtensions.cs:74`); singleton holds one factory client | typed client `AddHttpClient<IMobizonClient, MobizonClient>` |
| Shared HttpClient | ctor mutates injected `HttpClient.Timeout` (`MobizonClient.cs:91-92`) — can throw / mutate shared | apply timeout only when `ownsHttpClient` |
| README | non-compiling examples; wrong response-code table; 5/8 services; ContactCards LINQ absent; "Contracts zero deps" false; Polly example wrong | full rewrite + verified examples |
| XML docs | `ApiUrl` examples include `/service/` → `BuildUrl` doubles it (`MobizonClientOptions.cs:25`, DI/Polly) | remove `/service/` |
| Webhook endpoint | invokes handler before 200; no body-size cap (`WebhookEndpointRouteBuilderExtensions.cs:62,70`) | body-size limit; JSON problem responses; document verify→enqueue→200 |
| Sample | `Program.cs:74` runs ContactCards while `:94` says "uncomment"; `appsettings.Development.json` Content not conditional | comment out; `Condition="Exists()"` |

Structure / naming:

| Item | Issue | Fix |
|------|-------|-----|
| `BracketNotationSerializer` | dead code, but tested; services hand-roll `criteria[]`/`pagination[]`/`sort[]` | adopt across services |
| enum↔code mapping | `SmsStatusToApiCode`/`CampaignCommonStatusToApiCode` duplicated in service (`MessageService.cs:169-198`) | consolidate near converters |
| module name casing | `LinkService` uses lowercase `"link"/"get"`; others PascalCase | unify |
| `LinkStatsResult` | name models a single point but is used as the collection | rename point → `LinkStatPoint`; wrapper takes the name |
| `MobizonResponse.Data` | `default!` hides null (`MobizonResponse.cs:28`) | document invariant + guard in `MobizonApiClient` |
| `ContactCardQuery.Where` | second `Where` overwrites (`ContactCardQuery.cs:35`) | combine predicates or document single-Where; document `Skip` page semantics |

## 6. Architecture decisions

**Generic list envelope (C4).**
`MobizonListResult<T> { IReadOnlyList<T> Items; int TotalItemCount; }` in
`Mobizon.Contracts.Models.Common`. All `*/List` endpoints return it. `MessageListResponse`
and `ContactCardListResponse` are replaced by `MobizonListResult<MessageInfo>` /
`MobizonListResult<ContactCardData>` (their `Items`/`TotalItemCount` members are unchanged, so
`ContactCardQuery` keeps working).
`Link/GetStats` is **not** this shape — it gets a dedicated
`LinkStatsResult { IReadOnlyList<LinkStatPoint> Items; int Totals; }` (`Totals` = total clicks;
exact numeric type confirmed from the Phase 0 capture).

**Link identification (C5).** Three explicit methods (no enum, discoverable):
`GetByIdAsync(int)`, `GetByCodeAsync(string)`, `GetByShortLinkAsync(string)`.
`Link/Update` keyed by `Id`.

**Capture harness (C7).** `tools/Mobizon.Net.ApiCapture` console (`IsPackable=false`,
not in the solution's package output). Reads the local `appsettings.Development.json`,
calls **read-safe/cheap** endpoints, writes raw JSON to `artifacts/api-captures/` (gitignored).
Safety tiers:
- Tier 1 (always): read-only — `user/getOwnBalance`, every `*/list`, `link/get`,
  `campaign/get`+`getInfo`, `taskqueue/getStatus`, `alphaname/list`, `message/getSMSStatus`.
- Tier 2 (default on, free): `link` create→get→getStats→update→delete cycle.
- Tier 3 (**off by default**, flag-gated): `campaign/create`+`delete` (never `send`).
- **Never** sends SMS or a campaign.
Captured JSON is sanitized (phones, names, balance) before becoming committed fixtures in
`tests/.../Payloads/`.

**DI lifetime.** Move to typed-client registration so `IHttpClientFactory` owns handler
lifetime/rotation; the SDK stops mutating a caller-owned `HttpClient.Timeout`.

**Adopt `BracketNotationSerializer` (C6).** Services build request params by serializing the
typed request/criteria objects through it, replacing manual `criteria[i]`/`pagination[...]`
string building. Its existing tests stay; new round-trip tests are added.

## 7. Phases (execution order)

**Phase 0 — Capture & baseline (no SDK changes).**
Build `tools/Mobizon.Net.ApiCapture`; user runs it with their key; record real JSON for all
endpoints; resolve open shapes (`CampaignData` vs `CampaignInfo` for List, `link/getStats`
`type` values + item shape, `alphaname` fields); sanitize → commit fixtures. Prerequisite:
resolve the local `dotnet build` MSB3491 (`obj/...AssemblyInfoInputs.cache` access-denied)
file-lock issue.

**Phase 1 — Correctness (breaking).** Generic envelope + Campaign/Link list fixes; Link
contracts (3 Get methods, Update by Id, `clickCnt`+missing fields, `LinkStatPoint` rename);
list-item model audit. Fill empty `CampaignServiceTests`, add Link tests, all from fixtures.

**Phase 2 — Coverage.** `recipientsFile` (multipart) + XOR validation; `shortenLinks`;
`Link/List` criteria; `alphaname` service+model. Tests from fixtures.

**Phase 3 — DX / infra.** README rewrite (verified examples, correct code table, 8 services,
ContactCards LINQ, webhooks); CI branches + MinVer; typed-client DI + timeout fix; webhook
endpoint (body cap, problem responses, enqueue docs); sample fixes; pre-flight validation.

**Phase 4 — Structure / naming.** Adopt `BracketNotationSerializer` in all services;
consolidate enum↔code mapping; unify module-name casing; `MobizonResponse.Data` invariant +
guard; `ContactCardQuery.Where`/`Skip` semantics.

## 8. Testing & verification strategy

- Fixtures captured in Phase 0 are the ground truth for all new tests.
- Per endpoint: MockHttp returns the fixture → assert (a) deserialization into the corrected
  model and (b) outgoing request encoding (params/bracket-notation/multipart).
- Each phase ends with green `dotnet test` (currently 150 core + 49 webhook tests).
- Optional opt-in live smoke tests may reuse the capture harness paths (manual / CI secret).

## 9. Risks & mitigations

- **Real shapes differ from docs** → Phase 0 capture is authoritative (C3).
- **PII in captures** → sanitize before committing fixtures; raw captures gitignored.
- **Cost** → capture is read-only + free link CRUD; SMS/campaign-send never invoked.
- **Local build lock (MSB3491)** → resolved as Phase 0 prerequisite.
- **Breaking changes** → acceptable pre-1.0 (C1); summarized in CHANGELOG.

## 10. Open items (resolved during Phase 0)

- `Campaign/List` item model: `CampaignData` vs `CampaignInfo`.
- `Link/GetStats`: exact `type` parameter values and per-item field shape.
- `alphaname/list`: item fields (e.g. `alphanameId`, `name`, `globalStatus`, `partnerStatus`, `description`).
