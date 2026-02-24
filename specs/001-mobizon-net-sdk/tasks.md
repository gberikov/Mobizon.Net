# Tasks: Mobizon.Net SDK

**Input**: Design documents from `/specs/001-mobizon-net-sdk/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: TDD is mandatory per Constitution Principle III. Tests are written BEFORE implementation in every group.

**Organization**: Tasks are grouped by phase (foundation → reference service → parallel services → extensions → release). Phases 3-6 within Phase III are parallelizable by subagents.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US6)
- **Model emoji**: 🔴 Opus / 🟡 Sonnet / 🟢 Haiku
- **[🤖 PARALLEL]**: Entire group can execute via parallel subagent
- Include exact file paths in descriptions

---

## Phase 1: Setup (Project Infrastructure)

**Purpose**: Solution structure, project files, build configuration
**Model**: 🟡 Sonnet

- [x] T-000 🟡 Create solution structure: `Mobizon.Net.sln`, all 7 `.csproj` files (4 src + 2 tests + 1 sample), `Directory.Build.props` with shared version `1.0.0`, `LangVersion=8.0`, `Nullable=enable`, `TreatWarningsAsErrors=true`
  - `Mobizon.Net.sln`
  - `Directory.Build.props`
  - `src/Mobizon.Contracts/Mobizon.Contracts.csproj` (netstandard2.0, no deps except System.Text.Json)
  - `src/Mobizon.Net/Mobizon.Net.csproj` (netstandard2.0, refs Contracts)
  - `src/Mobizon.Net.Extensions.DependencyInjection/Mobizon.Net.Extensions.DependencyInjection.csproj`
  - `src/Mobizon.Net.Extensions.Polly/Mobizon.Net.Extensions.Polly.csproj`
  - `tests/Mobizon.Net.Tests/Mobizon.Net.Tests.csproj` (xUnit, Moq, RichardSzalay.MockHttp)
  - `tests/Mobizon.Net.IntegrationTests/Mobizon.Net.IntegrationTests.csproj`
  - `samples/Mobizon.Net.ConsoleSample/Mobizon.Net.ConsoleSample.csproj`
  - `.editorconfig`
  - `.gitignore`

- [x] T-001 🟡 Configure NuGet package metadata in `Directory.Build.props` and per-project `.csproj`: PackageId, Authors, Description, PackageTags (`sms mobizon sdk dotnet`), PackageProjectUrl, RepositoryUrl, PackageLicenseExpression=MIT, PackageReadmeFile, Copyright
  - `Directory.Build.props`
  - `src/Mobizon.Contracts/Mobizon.Contracts.csproj`
  - `src/Mobizon.Net/Mobizon.Net.csproj`
  - `src/Mobizon.Net.Extensions.DependencyInjection/Mobizon.Net.Extensions.DependencyInjection.csproj`
  - `src/Mobizon.Net.Extensions.Polly/Mobizon.Net.Extensions.Polly.csproj`

**Checkpoint**: `dotnet build` succeeds for all projects, `dotnet test` runs (0 tests).

---

## Phase 2: Contracts Foundation (Blocking Prerequisites)

**Purpose**: Core types in `Mobizon.Contracts` that ALL services depend on.
**Model**: 🔴 Opus (architectural decisions, public API design)

**⚠️ CRITICAL**: No service implementation can begin until this phase is complete.

### Core Types

- [x] T-010 🔴 Implement `MobizonClientOptions` with validation: `ApiKey` (string, required), `ApiUrl` (string, required), `ApiVersion` (string, default "v1"), `Timeout` (TimeSpan, default 30s). Throw `ArgumentException` on null/empty ApiKey or ApiUrl.
  - `src/Mobizon.Contracts/Models/MobizonClientOptions.cs`

- [x] T-011 🔴 Implement `MobizonResponse<T>` (Code, Data, Message) and `MobizonResponseCode` enum (Success=0, BackgroundTask=100, InvalidData=1, AuthFailed=2, NotFound=3, AccessDenied=4, InternalError=5)
  - `src/Mobizon.Contracts/Models/MobizonResponse.cs`
  - `src/Mobizon.Contracts/Models/MobizonResponseCode.cs`

- [x] T-012 🔴 Implement exception hierarchy: `MobizonException` (base, wraps transport errors), `MobizonApiException` (derived, has `Code: MobizonResponseCode` + `ApiMessage: string`)
  - `src/Mobizon.Contracts/Exceptions/MobizonException.cs`
  - `src/Mobizon.Contracts/Exceptions/MobizonApiException.cs`

- [x] T-013 🔴 [P] Implement pagination/sorting models: `PaginationRequest` (CurrentPage, PageSize), `SortRequest` (Field, Direction), `SortDirection` enum (ASC, DESC), `PaginatedResponse<T>` (Items, TotalCount, CurrentPage, PageSize)
  - `src/Mobizon.Contracts/Models/PaginationRequest.cs`
  - `src/Mobizon.Contracts/Models/SortRequest.cs`
  - `src/Mobizon.Contracts/Models/SortDirection.cs`
  - `src/Mobizon.Contracts/Models/PaginatedResponse.cs`

- [x] T-014 🔴 [P] Implement `LinkStatsType` enum (Daily, Monthly)
  - `src/Mobizon.Contracts/Models/LinkStatsType.cs`

### Core Types Tests

- [x] T-015 🔴 Tests for T-010..T-014: `MobizonClientOptions` validation (null ApiKey throws, empty URL throws, defaults correct), `MobizonResponse<T>` serialization/deserialization, `MobizonResponseCode` enum values, exception constructors and properties, pagination model defaults. **Edge cases**: unknown response code preserved as raw int in exception, empty `data` field deserializes to `default(T)`, null/empty ApiKey at construction throws `ArgumentException` immediately.
  - `tests/Mobizon.Net.Tests/Models/MobizonClientOptionsTests.cs`
  - `tests/Mobizon.Net.Tests/Models/MobizonResponseTests.cs`
  - `tests/Mobizon.Net.Tests/Exceptions/MobizonExceptionTests.cs`

### Core HTTP Client

- [x] T-020 🔴 TDD: Write `BracketNotationSerializerTests` FIRST — test cases: flat params (`key=value`), nested object (`data[field]=value`), nested array (`ids[0]=1&ids[1]=2`), mixed nesting (`data[nested][field]=value`), null values skipped, empty string preserved
  - `tests/Mobizon.Net.Tests/Internal/BracketNotationSerializerTests.cs`

- [x] T-021 🔴 Implement `BracketNotationSerializer` (internal static class): `Serialize(object obj, string? prefix) → Dictionary<string, string>`. Recursively flattens object properties into bracket-notation key-value pairs for form-urlencoded encoding.
  - `src/Mobizon.Net/Internal/BracketNotationSerializer.cs`

- [x] T-022 🔴 TDD: Write `MobizonApiClientTests` FIRST — test cases: URL construction (`{baseUrl}/service/{module}/{method}?output=json&api=v1&apiKey={key}`), POST with form-data body, GET without body (User/GetOwnBalance), successful response deserialization, error code → `MobizonApiException`, network error → `MobizonException`, cancellation token respected. **Edge cases**: pre-cancelled `CancellationToken` throws `OperationCanceledException` without HTTP call, misconfigured URL (wrong domain) propagates `HttpRequestException` wrapped in `MobizonException`, pagination out-of-range params passed through to API as-is.
  - `tests/Mobizon.Net.Tests/Internal/MobizonApiClientTests.cs`

- [x] T-023 🔴 Implement `MobizonApiClient` (internal): constructor takes `HttpClient` + `MobizonClientOptions`. Method `SendAsync<T>(HttpMethod, string module, string method, IDictionary<string,string>? parameters, CancellationToken)` → `MobizonResponse<T>`. Builds URL, serializes form data via `BracketNotationSerializer`, sends request, deserializes JSON response, maps error codes to exceptions.
  - `src/Mobizon.Net/Internal/MobizonApiClient.cs`

**Checkpoint**: Foundation ready. `BracketNotationSerializer` handles all nesting patterns. `MobizonApiClient` can send/receive any Mobizon API call. All tests green.

---

## Phase 3: User Story 1 — Send a Single SMS (Priority: P1) 🎯 MVP

**Goal**: Fully working Message module — send SMS, check status, list messages.
**Model**: 🟡 Sonnet (reference implementation for all other services)

**Independent Test**: Call `SendSmsMessageAsync`, verify response contains MessageId. Call `GetSmsStatusAsync` with that ID.

### Message DTOs

- [x] T-030 🟡 [US1] Implement Message module DTOs in `Mobizon.Contracts`: `SendSmsMessageRequest` (Recipient, Text, From?, Validity?), `SendSmsResult` (CampaignId, MessageId, Status), `SmsStatusResult` (Id, Status, SegNum, StartSendTs), `MessageListRequest` (Criteria?, Pagination?, Sort?), `MessageListCriteria` (From?, Status?), `MessageInfo` (Id, CampaignId, From, Status, Text)
  - `src/Mobizon.Contracts/Models/Message/SendSmsMessageRequest.cs`
  - `src/Mobizon.Contracts/Models/Message/SendSmsResult.cs`
  - `src/Mobizon.Contracts/Models/Message/SmsStatusResult.cs`
  - `src/Mobizon.Contracts/Models/Message/MessageListRequest.cs`
  - `src/Mobizon.Contracts/Models/Message/MessageListCriteria.cs`
  - `src/Mobizon.Contracts/Models/Message/MessageInfo.cs`

- [x] T-031 🟡 [US1] Implement `IMessageService` interface with 3 methods: `SendSmsMessageAsync`, `GetSmsStatusAsync`, `ListAsync` (signatures per spec.md contracts)
  - `src/Mobizon.Contracts/Services/IMessageService.cs`

### Message Tests (TDD — write FIRST, must FAIL)

- [x] T-032 🟡 [US1] Write `MessageServiceTests` using MockHttpMessageHandler. Test cases per method:
  - `SendSmsMessageAsync`: success (code=0, returns messageId), API error (code=2, throws MobizonApiException), form-data contains correct params (recipient, text, from, params[validity])
  - `GetSmsStatusAsync`: success (returns list of statuses), empty array input
  - `ListAsync`: success with pagination, criteria serialized as `criteria[from]`, sort serialized as `sort[campaignId]`
  - All methods: CancellationToken forwarded, network error → MobizonException
  - `tests/Mobizon.Net.Tests/Services/MessageServiceTests.cs`

### Message Implementation

- [x] T-033 🟡 [US1] Implement `MessageService` (internal class, receives `MobizonApiClient`). Maps each method to correct module/method path. `SendSmsMessageAsync` → POST `message/sendsmsmessage`. `GetSmsStatusAsync` → POST `message/getsmsstatus`. `ListAsync` → POST `message/list`.
  - `src/Mobizon.Net/Services/MessageService.cs`

**Checkpoint**: SMS sending works end-to-end (via mocks). All 3 Message methods tested. This is the reference implementation for Phase 4 subagents.

**🏷️ Milestone: v0.1.0-alpha** — Message module operational.

---

## Phase 4: Remaining Services [🤖 PARALLEL subagents after Phase 3]

**Purpose**: Complete all 5 API modules. Groups 4, 5, 6 run in parallel.
**Orchestration**: Head agent (Opus) launches 3 subagents, each gets `spec.md` + `MessageService.cs` as reference.

---

### Phase 4A: User Story 2 — Campaign Service (Priority: P2) [🤖 PARALLEL]

**Goal**: Full campaign workflow: create, add recipients, send, monitor.
**Model**: 🟡 Sonnet (Subagent A)
**Context for subagent**: spec.md Campaign section + MessageService.cs as pattern

- [x] T-040 🟡 [P] [US2] Implement Campaign DTOs: `CreateCampaignRequest` (Type, From, Text), `CreateCampaignResult` (CampaignId), `CampaignData` (Id, Type, From, Text, Status), `CampaignInfo` (Id, TotalMessages, Sent, Delivered, Failed), `CampaignSendResult` (TaskId?, Status), `CampaignListRequest`, `AddRecipientsRequest` (CampaignId, Type, Data)
  - `src/Mobizon.Contracts/Models/Campaign/CreateCampaignRequest.cs`
  - `src/Mobizon.Contracts/Models/Campaign/CreateCampaignResult.cs`
  - `src/Mobizon.Contracts/Models/Campaign/CampaignData.cs`
  - `src/Mobizon.Contracts/Models/Campaign/CampaignInfo.cs`
  - `src/Mobizon.Contracts/Models/Campaign/CampaignSendResult.cs`
  - `src/Mobizon.Contracts/Models/Campaign/CampaignListRequest.cs`
  - `src/Mobizon.Contracts/Models/Campaign/AddRecipientsRequest.cs`

- [x] T-041 🟡 [P] [US2] Implement `ICampaignService` interface: Create, Delete, Get, GetInfo, List, Send, AddRecipients (7 methods)
  - `src/Mobizon.Contracts/Services/ICampaignService.cs`

- [x] T-042 🟡 [US2] TDD: Write `CampaignServiceTests` — all 7 methods, happy path + errors. Special: `SendAsync` must handle `BackgroundTask` response (code=100) returning TaskId. `AddRecipientsAsync` must serialize array data as `data[0]=...&data[1]=...`. `CreateAsync` must serialize as `data[type]=...&data[from]=...&data[text]=...`.
  - `tests/Mobizon.Net.Tests/Services/CampaignServiceTests.cs`

- [x] T-043 🟡 [US2] Implement `CampaignService`: all 7 methods mapping to `campaign/{method}` endpoints.
  - `src/Mobizon.Net/Services/CampaignService.cs`

---

### Phase 4B: User Story 4 — Link Service (Priority: P4) [🤖 PARALLEL]

**Goal**: Short link management and click analytics.
**Model**: 🟡 Sonnet (Subagent B)

- [x] T-050 🟡 [P] [US4] Implement Link DTOs: `CreateLinkRequest` (FullLink, Status?, ExpirationDate?, Comment?), `UpdateLinkRequest` (Code, FullLink?, Status?, ExpirationDate?, Comment?), `LinkData` (Id, Code, FullLink, Status, ExpirationDate?, Comment?, Clicks), `GetLinkStatsRequest` (Ids, Type, DateFrom?, DateTo?), `LinkStatsResult` (LinkId, Date, Clicks), `LinkListRequest`
  - `src/Mobizon.Contracts/Models/Link/CreateLinkRequest.cs`
  - `src/Mobizon.Contracts/Models/Link/UpdateLinkRequest.cs`
  - `src/Mobizon.Contracts/Models/Link/LinkData.cs`
  - `src/Mobizon.Contracts/Models/Link/GetLinkStatsRequest.cs`
  - `src/Mobizon.Contracts/Models/Link/LinkStatsResult.cs`
  - `src/Mobizon.Contracts/Models/Link/LinkListRequest.cs`

- [x] T-051 🟡 [P] [US4] Implement `ILinkService` interface: Create, Delete, Get, GetLinks, GetStats, List, Update (7 methods)
  - `src/Mobizon.Contracts/Services/ILinkService.cs`

- [x] T-052 🟡 [US4] TDD: Write `LinkServiceTests` — all 7 methods. Special: `GetStatsAsync` serializes `criteria[dateFrom]`, `criteria[dateTo]`. `DeleteAsync` accepts int array. `GetAsync`/`UpdateAsync` use `code` (string) not id. `CreateAsync` serializes as `data[fullLink]=...`.
  - `tests/Mobizon.Net.Tests/Services/LinkServiceTests.cs`

- [x] T-053 🟡 [US4] Implement `LinkService`: all 7 methods mapping to `link/{method}` endpoints.
  - `src/Mobizon.Net/Services/LinkService.cs`

---

### Phase 4C: User Stories 3 & 6 — User & TaskQueue (Priority: P3, P6) [🤖 PARALLEL]

**Goal**: Balance check + background task monitoring.
**Model**: 🟢 Haiku (simple modules, 1-2 methods each)

- [x] T-060 🟢 [P] [US3] Implement User + TaskQueue DTOs: `BalanceResult` (Balance: decimal, Currency: string — note: API returns balance as string, must parse to decimal), `TaskQueueStatus` (Id, Status, Progress)
  - `src/Mobizon.Contracts/Models/User/BalanceResult.cs`
  - `src/Mobizon.Contracts/Models/TaskQueue/TaskQueueStatus.cs`

- [x] T-061 🟢 [P] [US3] Implement `IUserService` (GetOwnBalanceAsync) and `ITaskQueueService` (GetStatusAsync) interfaces
  - `src/Mobizon.Contracts/Services/IUserService.cs`
  - `src/Mobizon.Contracts/Services/ITaskQueueService.cs`

- [x] T-062 🟢 [US3] TDD: Write `UserServiceTests` — `GetOwnBalanceAsync` uses HTTP GET (not POST!), parses string balance "4043.0656" to decimal. Write `TaskQueueServiceTests` — `GetStatusAsync` sends POST with `id` param.
  - `tests/Mobizon.Net.Tests/Services/UserServiceTests.cs`
  - `tests/Mobizon.Net.Tests/Services/TaskQueueServiceTests.cs`

- [x] T-063 🟢 [US3] Implement `UserService` (GET `user/getownbalance`) and `TaskQueueService` (POST `taskqueue/getstatus`).
  - `src/Mobizon.Net/Services/UserService.cs`
  - `src/Mobizon.Net/Services/TaskQueueService.cs`

---

### Phase 4D: Client Assembly (after parallel subagents complete)

**Purpose**: Aggregate all services into `IMobizonClient` / `MobizonClient`.
**Model**: 🟡 Sonnet (head agent, post-merge integration)

- [x] T-070 🟡 Implement `IMobizonClient` interface: `Messages`, `Campaigns`, `Links`, `User`, `TaskQueue` properties
  - `src/Mobizon.Contracts/IMobizonClient.cs`

- [x] T-071 🟡 Implement `MobizonClient`: constructor takes `HttpClient` + `MobizonClientOptions`, creates `MobizonApiClient` internally, instantiates all 5 service implementations, exposes via interface properties.
  - `src/Mobizon.Net/MobizonClient.cs`

- [x] T-072 🟡 Write `MobizonClientTests`: all 5 sub-service properties are non-null after construction, options validation propagates from `MobizonApiClient`, verify correct `HttpClient` is used.
  - `tests/Mobizon.Net.Tests/MobizonClientTests.cs`

- [x] T-073 🔴 Integration review: `dotnet build` entire solution, `dotnet test` all projects — 100% green, zero warnings. Review subagent code for consistency with MessageService patterns.

**Checkpoint**: All 5 API modules complete. `IMobizonClient` fully assembled.

**🏷️ Milestone: v0.5.0-beta** — all API modules operational.

---

## Phase 5: User Story 5 — DI and Resilience Integration (Priority: P5) [🤖 PARALLEL subagents]

**Purpose**: Extension packages for DI container and Polly resilience.
**Orchestration**: 2 parallel subagents.

---

### Phase 5A: DI Extension [🤖 PARALLEL]

**Model**: 🟡 Sonnet (Subagent D)

- [x] T-080 🟡 [P] [US5] Implement `ServiceCollectionExtensions.AddMobizon(this IServiceCollection, Action<MobizonClientOptions>)` → registers options, named HttpClient via `AddHttpClient`, `IMobizonClient` → `MobizonClient`. Returns `IHttpClientBuilder` for chaining.
  - `src/Mobizon.Net.Extensions.DependencyInjection/ServiceCollectionExtensions.cs`

- [x] T-081 🟡 [P] [US5] Implement `AddMobizon(this IServiceCollection, IConfiguration)` overload: binds `MobizonClientOptions` from config section.
  - `src/Mobizon.Net.Extensions.DependencyInjection/ServiceCollectionExtensions.cs`

- [x] T-082 🟡 [US5] TDD: Write DI tests — resolve `IMobizonClient` from provider, verify options are correctly bound from both `Action<>` and `IConfiguration` overloads, verify `HttpClient` is from factory.
  - `tests/Mobizon.Net.Tests/Extensions/DependencyInjectionTests.cs`

---

### Phase 5B: Polly Extension [🤖 PARALLEL]

**Model**: 🟡 Sonnet (Subagent E)

- [x] T-090 🟡 [P] [US5] Implement `MobizonResilienceOptions` (RetryCount=3, RetryBaseDelay=2s, CircuitBreakerCount=5, CircuitBreakerDuration=30s, Timeout=30s) and `AddMobizonResilience(this IHttpClientBuilder, Action<MobizonResilienceOptions>?)` extension.
  - `src/Mobizon.Net.Extensions.Polly/MobizonResilienceOptions.cs`
  - `src/Mobizon.Net.Extensions.Polly/MobizonHttpClientBuilderExtensions.cs`

- [x] T-091 🟡 [US5] Configure default Polly policies: WaitAndRetryAsync (exponential backoff 2s/4s/8s on transient HTTP errors), CircuitBreakerAsync (5 failures, 30s break), TimeoutAsync (30s).
  - `src/Mobizon.Net.Extensions.Polly/MobizonHttpClientBuilderExtensions.cs`

- [x] T-092 🟡 [US5] TDD: Write Polly tests — retry fires on 503, circuit breaker opens after threshold, timeout cancels long requests, custom options override defaults.
  - `tests/Mobizon.Net.Tests/Extensions/PollyExtensionsTests.cs`

---

### Phase 5C: Integration (after parallel subagents)

- [x] T-093 🟡 [US5] Integration test: `services.AddMobizon(o => ...).AddMobizonResilience()` → resolve client → mock transient failure → verify retry + circuit breaker.
  - `tests/Mobizon.Net.Tests/Extensions/IntegrationTests.cs`

**Checkpoint**: `AddMobizon().AddMobizonResilience()` fully working.

---

## Phase 6: Release Preparation [🤖 PARALLEL subagents]

**Purpose**: Documentation, samples, CI — production-ready v1.0.0.

- [x] T-100 🟢 [P] XML doc-comments on ALL public types and members in `Mobizon.Contracts` and `Mobizon.Net`: every class, interface, method, property, enum value. Include `<param>`, `<returns>`, `<exception>`, `<example>` tags where appropriate.
  - `src/Mobizon.Contracts/**/*.cs`
  - `src/Mobizon.Net/MobizonClient.cs`

- [x] T-101 🟡 [P] Write `README.md`: project description, installation (4 packages), quick start (send SMS in 5 lines), per-module examples (Message, Campaign, Link, User, TaskQueue), DI configuration, Polly configuration, regional URLs, error handling, contributing link.
  - `README.md`

- [x] T-102 🟢 [P] Implement `ConsoleSample/Program.cs`: working console app demonstrating send SMS, check status, check balance, and DI setup. Use environment variables for ApiKey/ApiUrl.
  - `samples/Mobizon.Net.ConsoleSample/Program.cs`

- [x] T-103 🟢 [P] Create `CHANGELOG.md` (v1.0.0 initial release notes)
  - `CHANGELOG.md`

- [x] T-105 🟡 GitHub Actions CI pipeline: `.github/workflows/ci.yml` — trigger on push/PR to main, steps: checkout, setup-dotnet, restore, build, test, pack. On tag `v*`: publish all 4 packages to NuGet.org using API key secret.
  - `.github/workflows/ci.yml`

- [x] T-104 🟡 Final validation: `dotnet build -c Release`, `dotnet test`, `dotnet pack` — verify 4 `.nupkg` files generated, zero warnings, all tests pass.

**Checkpoint**: All deliverables complete.

**🏷️ Milestone: v1.0.0** — production-ready.

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup ──────────────► Phase 2: Foundation ──────► Phase 3: Message (US1)
                                                                    │
                                                                    ▼
                                                     ┌──────────────┼──────────────┐
                                                     ▼              ▼              ▼
                                              Phase 4A:       Phase 4B:      Phase 4C:
                                              Campaign        Link           User+TQ
                                              (Sonnet)        (Sonnet)       (Haiku)
                                                     │              │              │
                                                     └──────────────┼──────────────┘
                                                                    ▼
                                                             Phase 4D:
                                                             Client Assembly
                                                                    │
                                                          ┌─────────┴─────────┐
                                                          ▼                   ▼
                                                   Phase 5A: DI        Phase 5B: Polly
                                                   (Sonnet)            (Sonnet)
                                                          │                   │
                                                          └─────────┬─────────┘
                                                                    ▼
                                                             Phase 5C:
                                                             DI+Polly Integration
                                                                    │
                                                                    ▼
                                                             Phase 6: Release
                                                             (Haiku + Sonnet)
```

### Parallel Opportunities

| Group | Tasks | Model | Can run with |
|-------|-------|-------|--------------|
| Phase 4A | T-040..T-043 | 🟡 Sonnet | Phase 4B, 4C |
| Phase 4B | T-050..T-053 | 🟡 Sonnet | Phase 4A, 4C |
| Phase 4C | T-060..T-063 | 🟢 Haiku | Phase 4A, 4B |
| Phase 5A | T-080..T-082 | 🟡 Sonnet | Phase 5B |
| Phase 5B | T-090..T-092 | 🟡 Sonnet | Phase 5A |
| Phase 6 | T-100..T-103 | 🟢/🟡 mixed | All in parallel |

### Within Each Service Module

1. DTOs first (T-X40/X50/X60)
2. Interface (T-X41/X51/X61)
3. Tests MUST be written and FAIL (T-X42/X52/X62)
4. Implementation makes tests pass (T-X43/X53/X63)

---

## Implementation Strategy

### MVP First (Phase 1-3 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundation (CRITICAL)
3. Complete Phase 3: Message Service
4. **STOP and VALIDATE**: Send SMS works via mock tests
5. Tag `v0.1.0-alpha`

### Parallel Service Build (Phase 4)

1. Head agent (Opus) launches 3 subagents
2. Each subagent receives: `spec.md` + `MessageService.cs` as reference
3. Each works in its own `Models/{Module}/` + `Services/{Module}Service.cs` — no conflicts
4. Head agent merges, builds `MobizonClient`, runs full test suite
5. Tag `v0.5.0-beta`

### Full Release (Phase 5-6)

1. 2 parallel subagents for DI + Polly
2. Integration test confirms they work together
3. Parallel: XML docs + README + Sample + CI
4. Final validation
5. Tag `v1.0.0`

---

## Summary

| Metric | Value |
|--------|-------|
| Total tasks | 45 |
| Phase 1 (Setup) | 2 tasks |
| Phase 2 (Foundation) | 10 tasks |
| Phase 3 (Message/US1) | 4 tasks |
| Phase 4 (Services) | 16 tasks (12 parallel + 4 assembly) |
| Phase 5 (Extensions) | 7 tasks (6 parallel + 1 integration) |
| Phase 6 (Release) | 6 tasks (all parallel) |
| Parallel subagent groups | 5 (3 in Phase 4 + 2 in Phase 5) |
| Model distribution | 🔴 Opus: 11 / 🟡 Sonnet: 27 / 🟢 Haiku: 7 |
