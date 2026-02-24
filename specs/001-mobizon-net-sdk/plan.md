# Implementation Plan: Mobizon.Net SDK

**Branch**: `001-mobizon-net-sdk` | **Date**: 2026-02-24 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-mobizon-net-sdk/spec.md`

## Summary

Build a complete .NET SDK for the Mobizon SMS gateway REST API.
The SDK consists of 4 NuGet packages: `Mobizon.Contracts` (DTOs,
interfaces), `Mobizon.Net` (core implementation), and two extension
packages (DI, Polly). Strategy: inside-out — core HTTP client first,
then services built on top, then extensions. TDD enforced at every
phase. Parallel subagent execution for independent service modules
after the foundation is proven.

## Technical Context

**Language/Version**: C# 8.0+ / .NET Standard 2.0
**Primary Dependencies**: System.Text.Json (NuGet), System.Net.Http
**Storage**: N/A (stateless HTTP client library)
**Testing**: xUnit 2.x, Moq, RichardSzalay.MockHttp
**Target Platform**: netstandard2.0 (.NET Framework 4.6.1+ / .NET Core 2.0+ / .NET 5+)
**Project Type**: Library (NuGet package)
**Performance Goals**: N/A (thin HTTP wrapper, perf determined by API)
**Constraints**: Zero third-party HTTP deps, netstandard2.0 only
**Scale/Scope**: 4 NuGet packages, ~20 public types, 5 API modules, ~20 API methods

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Evidence |
|---|-----------|--------|----------|
| I | Minimal Dependencies | PASS | Core depends only on System.Text.Json + HttpClient |
| II | Contract Separation | PASS | Mobizon.Contracts is a standalone package |
| III | Test-First | PASS | TDD enforced per-phase, MockHttpMessageHandler for HTTP |
| IV | Async-First API Design | PASS | All methods return Task\<T\>, accept CancellationToken |
| V | Complete API Coverage | PASS | All 5 modules specified: Message, Campaign, Link, User, TaskQueue |
| VI | Regional Flexibility | PASS | Configurable baseUrl in MobizonClientOptions, no default |
| VII | IHttpClientFactory | PASS | DI extension registers named HttpClient via factory |

All gates pass. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/001-mobizon-net-sdk/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── IMobizonClient.cs
│   ├── IMessageService.cs
│   ├── ICampaignService.cs
│   ├── ILinkService.cs
│   ├── IUserService.cs
│   └── ITaskQueueService.cs
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── Mobizon.Contracts/
│   ├── Mobizon.Contracts.csproj
│   ├── IMobizonClient.cs
│   ├── Services/
│   │   ├── IMessageService.cs
│   │   ├── ICampaignService.cs
│   │   ├── ILinkService.cs
│   │   ├── IUserService.cs
│   │   └── ITaskQueueService.cs
│   ├── Models/
│   │   ├── MobizonClientOptions.cs
│   │   ├── MobizonResponse.cs
│   │   ├── MobizonResponseCode.cs
│   │   ├── PaginationRequest.cs
│   │   ├── SortRequest.cs
│   │   ├── PaginatedResponse.cs
│   │   ├── Message/
│   │   │   ├── SendSmsMessageRequest.cs
│   │   │   ├── SendSmsResult.cs
│   │   │   ├── SmsStatusResult.cs
│   │   │   ├── MessageListRequest.cs
│   │   │   └── MessageInfo.cs
│   │   ├── Campaign/
│   │   │   ├── CreateCampaignRequest.cs
│   │   │   ├── CreateCampaignResult.cs
│   │   │   ├── CampaignData.cs
│   │   │   ├── CampaignInfo.cs
│   │   │   ├── CampaignSendResult.cs
│   │   │   ├── CampaignListRequest.cs
│   │   │   └── AddRecipientsRequest.cs
│   │   ├── Link/
│   │   │   ├── CreateLinkRequest.cs
│   │   │   ├── UpdateLinkRequest.cs
│   │   │   ├── LinkData.cs
│   │   │   ├── GetLinkStatsRequest.cs
│   │   │   ├── LinkStatsResult.cs
│   │   │   └── LinkListRequest.cs
│   │   ├── User/
│   │   │   └── BalanceResult.cs
│   │   └── TaskQueue/
│   │       └── TaskQueueStatus.cs
│   └── Exceptions/
│       ├── MobizonException.cs
│       └── MobizonApiException.cs
├── Mobizon.Net/
│   ├── Mobizon.Net.csproj
│   ├── MobizonClient.cs
│   ├── Internal/
│   │   ├── MobizonApiClient.cs
│   │   └── BracketNotationSerializer.cs
│   └── Services/
│       ├── MessageService.cs
│       ├── CampaignService.cs
│       ├── LinkService.cs
│       ├── UserService.cs
│       └── TaskQueueService.cs
├── Mobizon.Net.Extensions.DependencyInjection/
│   ├── Mobizon.Net.Extensions.DependencyInjection.csproj
│   └── ServiceCollectionExtensions.cs
└── Mobizon.Net.Extensions.Polly/
    ├── Mobizon.Net.Extensions.Polly.csproj
    ├── MobizonHttpClientBuilderExtensions.cs
    └── MobizonResilienceOptions.cs

tests/
├── Mobizon.Net.Tests/
│   ├── Mobizon.Net.Tests.csproj
│   ├── Internal/
│   │   ├── MobizonApiClientTests.cs
│   │   └── BracketNotationSerializerTests.cs
│   ├── Services/
│   │   ├── MessageServiceTests.cs
│   │   ├── CampaignServiceTests.cs
│   │   ├── LinkServiceTests.cs
│   │   ├── UserServiceTests.cs
│   │   └── TaskQueueServiceTests.cs
│   ├── MobizonClientTests.cs
│   └── Extensions/
│       ├── DependencyInjectionTests.cs
│       └── PollyExtensionsTests.cs
└── Mobizon.Net.IntegrationTests/
    ├── Mobizon.Net.IntegrationTests.csproj
    └── SmokeTests.cs

samples/
└── Mobizon.Net.ConsoleSample/
    ├── Mobizon.Net.ConsoleSample.csproj
    └── Program.cs

Directory.Build.props          (shared version, nullable, etc.)
Mobizon.Net.sln
```

**Structure Decision**: Multi-project .NET solution following the
constitution's prescribed layout. Each NuGet package is a separate
project under `src/`. Contracts separated from implementation.
Tests mirror source structure.

## Phases

### Phase 1: Foundation (Contracts + Core)

**Goal**: Working `Mobizon.Contracts` + `MobizonApiClient` that
can serialize, send, and deserialize any Mobizon API call.

**Model**: Opus (sequential, single agent)
**Rationale**: This is the architectural foundation. Errors here
cascade to all downstream work. Requires careful API design for
`MobizonApiClient`, `BracketNotationSerializer`, and the response
deserialization pipeline.

**Tasks**:

| ID | Task | Files |
|----|------|-------|
| T-000 | Create solution, projects, Directory.Build.props | `*.sln`, `*.csproj`, `Directory.Build.props` |
| T-001 | Create test projects with xUnit + MockHttp deps | `tests/**/*.csproj` |
| T-010 | Implement `MobizonClientOptions` | `src/Mobizon.Contracts/Models/MobizonClientOptions.cs` |
| T-011 | Implement `MobizonResponse<T>`, `MobizonResponseCode` | `src/Mobizon.Contracts/Models/Mobizon*.cs` |
| T-012 | Implement exception hierarchy | `src/Mobizon.Contracts/Exceptions/*.cs` |
| T-013 | Implement `PaginationRequest`, `SortRequest`, `PaginatedResponse<T>` | `src/Mobizon.Contracts/Models/*.cs` |
| T-014 | Implement `SortDirection`, `LinkStatsType` enums | `src/Mobizon.Contracts/Models/*.cs` |
| T-020 | TDD: `BracketNotationSerializer` tests → implementation | `tests/**/BracketNotationSerializerTests.cs`, `src/**/BracketNotationSerializer.cs` |
| T-021 | TDD: `MobizonApiClient` tests → implementation | `tests/**/MobizonApiClientTests.cs`, `src/**/MobizonApiClient.cs` |
| T-022 | Options validation tests (null ApiKey, empty URL, etc.) | `tests/**/MobizonApiClientTests.cs` |

**Definition of Done**:
- `Mobizon.Contracts` compiles with all base types
- `BracketNotationSerializer` handles flat, nested, and array params
- `MobizonApiClient.SendAsync<T>()` builds URL, serializes form data, sends POST, deserializes `MobizonResponse<T>`
- Error codes map to `MobizonApiException`
- All tests green: `dotnet test`

**Dependencies**: None (first phase)

---

### Phase 2: Message Service (Reference Implementation)

**Goal**: Fully working Message module — the template that all
other service implementations will follow.

**Model**: Sonnet (sequential, single agent)
**Rationale**: Sonnet is the workhorse for service implementation.
This module becomes the reference for Phase 3 subagents. Must be
clean, well-tested, and exemplary.

**Tasks**:

| ID | Task | Files |
|----|------|-------|
| T-030 | Message DTOs: `SendSmsMessageRequest`, `SendSmsResult`, `SmsStatusResult`, `MessageListRequest`, `MessageListCriteria`, `MessageInfo` | `src/Mobizon.Contracts/Models/Message/*.cs` |
| T-031 | `IMessageService` interface | `src/Mobizon.Contracts/Services/IMessageService.cs` |
| T-032 | TDD: `MessageServiceTests` (happy path + errors for all 3 methods) | `tests/**/MessageServiceTests.cs` |
| T-033 | Implement `MessageService` | `src/Mobizon.Net/Services/MessageService.cs` |

**Definition of Done**:
- `SendSmsMessageAsync`, `GetSmsStatusAsync`, `ListAsync` fully working
- Tests cover: success response, API error response, network error, cancellation
- Pattern established: service receives `MobizonApiClient`, calls `SendAsync<T>` with correct module/method/params

**Dependencies**: Phase 1

**Milestone**: `v0.1.0-alpha` — SMS sending works

---

### Phase 3: Remaining Services (Parallel Agent Teams)

**Goal**: Complete all 5 API modules.

**Orchestration**: Head agent (Opus) launches 3 parallel subagents,
then integrates results.

| Subagent | Model | Scope | Files touched |
|----------|-------|-------|---------------|
| A | Sonnet | Campaign Service (7 methods) | `Models/Campaign/*.cs`, `ICampaignService.cs`, `CampaignService.cs`, `CampaignServiceTests.cs` |
| B | Sonnet | Link Service (7 methods) | `Models/Link/*.cs`, `ILinkService.cs`, `LinkService.cs`, `LinkServiceTests.cs` |
| C | Haiku | User + TaskQueue (2 methods total) | `Models/User/*.cs`, `Models/TaskQueue/*.cs`, `IUserService.cs`, `ITaskQueueService.cs`, `UserService.cs`, `TaskQueueService.cs`, `*Tests.cs` |

**Context for each subagent**:
- Full `spec.md` (API reference section for their module)
- `MessageService.cs` + `MessageServiceTests.cs` as reference implementation
- `MobizonApiClient` interface contract

**After parallel work** (Head agent, Opus):

| ID | Task |
|----|------|
| T-070 | `IMobizonClient` interface in Contracts |
| T-071 | `MobizonClient` implementation aggregating all services |
| T-072 | `MobizonClientTests` — verify all sub-services accessible |
| T-073 | Integration review: build entire solution, run all tests |

**Definition of Done**:
- All 5 service interfaces + implementations complete
- `IMobizonClient` exposes `.Messages`, `.Campaigns`, `.Links`, `.User`, `.TaskQueue`
- `dotnet test` — 100% green across all test projects
- No compilation warnings

**Dependencies**: Phase 2

**Milestone**: `v0.5.0-beta` — all API modules operational

---

### Phase 4: Extensions (Parallel Agent Teams)

**Goal**: DI and Polly extension packages.

**Orchestration**: Head agent launches 2 parallel subagents.

| Subagent | Model | Scope |
|----------|-------|-------|
| D | Sonnet | DI Extension: `AddMobizon()` overloads, `IHttpClientFactory` registration |
| E | Sonnet | Polly Extension: `AddMobizonResilience()`, default policies, customization |

**After parallel work** (Head agent, Sonnet):
- Integration test: DI + Polly together
- Verify `IMobizonClient` resolves correctly from DI container
- Verify resilience policies fire on transient failures

**Tasks**:

| ID | Task |
|----|------|
| T-080 | `ServiceCollectionExtensions` — `AddMobizon(Action<Options>)` |
| T-081 | `ServiceCollectionExtensions` — `AddMobizon(IConfiguration)` |
| T-082 | DI tests: resolve client, verify options binding |
| T-090 | `MobizonResilienceOptions` + `AddMobizonResilience()` |
| T-091 | Default policies: retry, circuit breaker, timeout |
| T-092 | Polly tests: verify retry on transient, circuit breaker opens |
| T-093 | Integration: DI + Polly combined test |

**Definition of Done**:
- `services.AddMobizon(o => ...).AddMobizonResilience()` compiles and works
- Retry fires on 5xx, circuit breaker opens after threshold
- All policies overridable via `MobizonResilienceOptions`

**Dependencies**: Phase 3

---

### Phase 5: Release Preparation (Parallel Agent Teams)

**Goal**: Production-ready v1.0.0 for NuGet.

**Orchestration**:

| Subagent | Model | Scope |
|----------|-------|-------|
| F | Haiku | XML doc-comments on all public types/members |
| G | Haiku | `ConsoleSample`, CHANGELOG.md |
| H | Sonnet | README.md with full usage examples |

**Head agent** (Sonnet): CI pipeline (GitHub Actions)

**Tasks**:

| ID | Task |
|----|------|
| T-100 | XML doc-comments on all public APIs in Contracts + Net |
| T-101 | README.md — installation, configuration, examples per module |
| T-102 | `ConsoleSample/Program.cs` — working example |
| T-103 | CHANGELOG.md |
| T-105 | GitHub Actions CI: build, test, NuGet publish |
| T-104 | NuGet package metadata (descriptions, tags, icons, license) |
| T-105 | Final `dotnet build`, `dotnet test`, `dotnet pack` validation |

**Definition of Done**:
- All public APIs have XML comments
- README has examples for: send SMS, check status, campaigns, balance, DI setup
- CI builds and tests on push/PR
- `dotnet pack` produces 4 `.nupkg` files
- ConsoleSample compiles and demonstrates core workflows

**Dependencies**: Phase 4

**Milestone**: `v1.0.0` — production-ready

---

## Phase Mapping (Plan → Tasks)

| Plan Phase | Tasks Phase | Description |
|------------|-------------|-------------|
| Phase 1 (Foundation) | Phase 1 (Setup) + Phase 2 (Foundation) | Tasks splits setup from contracts |
| Phase 2 (Message) | Phase 3 (Message/US1) | Reference implementation |
| Phase 3 (Remaining Services) | Phase 4 (4A/4B/4C/4D) | Parallel services + assembly |
| Phase 4 (Extensions) | Phase 5 (5A/5B/5C) | DI + Polly + integration |
| Phase 5 (Release) | Phase 6 (Release) | Docs, CI, samples |

## Phase Dependencies (Gantt-like)

```
Phase 1: Foundation          ████████████░░░░░░░░░░░░░░░░░░░░░░
Phase 2: Message Service     ░░░░░░░░░░░░████░░░░░░░░░░░░░░░░░░
Phase 3: Remaining Services  ░░░░░░░░░░░░░░░░████████░░░░░░░░░░
  ├─ Subagent A (Campaign)   ░░░░░░░░░░░░░░░░██████░░░░░░░░░░░░
  ├─ Subagent B (Link)       ░░░░░░░░░░░░░░░░██████░░░░░░░░░░░░
  ├─ Subagent C (User+TQ)    ░░░░░░░░░░░░░░░░███░░░░░░░░░░░░░░░
  └─ Integration (Opus)      ░░░░░░░░░░░░░░░░░░░░░░██░░░░░░░░░░
Phase 4: Extensions          ░░░░░░░░░░░░░░░░░░░░░░░░████░░░░░░
  ├─ Subagent D (DI)         ░░░░░░░░░░░░░░░░░░░░░░░░███░░░░░░░
  ├─ Subagent E (Polly)      ░░░░░░░░░░░░░░░░░░░░░░░░███░░░░░░░
  └─ Integration             ░░░░░░░░░░░░░░░░░░░░░░░░░░░█░░░░░░
Phase 5: Release             ░░░░░░░░░░░░░░░░░░░░░░░░░░░░██████
  ├─ Subagents F,G,H         ░░░░░░░░░░░░░░░░░░░░░░░░░░░░████░░
  └─ CI + final              ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░██
```

## Milestones

| Version | After Phase | Deliverable |
|---------|-------------|-------------|
| `v0.1.0-alpha` | Phase 2 | Message module works (send, status, list) |
| `v0.5.0-beta` | Phase 3 | All 5 modules complete, MobizonClient assembled |
| `v1.0.0` | Phase 5 | Production-ready, docs, CI, NuGet packages |

## Risks & Mitigation

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | No OpenAPI schema — all typing based on docs + empirical testing | API changes break SDK silently | Integration tests with real API (manual), XML comments with doc links, version pinning to `api=v1` |
| 2 | Bracket notation serialization edge cases (nested arrays, null values) | Incorrect API calls, silent failures | Dedicated `BracketNotationSerializerTests` suite with exhaustive cases: flat, nested object, arrays, mixed, empty, null |
| 3 | netstandard2.0 limitations (no built-in System.Text.Json, no NRT) | Compilation issues, missing APIs | `System.Text.Json` NuGet package, `#nullable enable` via `Directory.Build.props`, `LangVersion` set to `8.0` |
| 4 | Four NuGet packages — versioning complexity | Version mismatch between packages | Single version in `Directory.Build.props`, all packages published atomically in CI |
| 5 | Phase 3 parallel subagent conflicts | Merge conflicts in shared files (`.sln`, `.csproj`) | Each subagent works only in its module folder; head agent handles `.sln`, `.csproj`, and integration files post-merge |

## Complexity Tracking

> No Constitution Check violations found. No complexity justifications needed.
