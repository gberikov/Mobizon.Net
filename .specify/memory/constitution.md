<!--
## Sync Impact Report
- **Version change**: 1.1.0 → 1.1.1
- **Bump rationale**: PATCH — clarified parallelism strategy to
  exempt reference implementation (Message service) from parallel
  requirement. No principle changes.
- **Modified principles**: None (all 7 principles unchanged)
- **Modified sections**:
  - AI Agent Workflow → Parallelism Strategy: clarified that Message
    service is implemented first as sequential reference, remaining 4
    services parallelized.
- **Removed sections**: None
- **Templates requiring updates**:
  - `.specify/templates/plan-template.md` — ✅ no update needed
  - `.specify/templates/spec-template.md` — ✅ no update needed
  - `.specify/templates/tasks-template.md` — ✅ no update needed
  - `.specify/templates/agent-file-template.md` — ✅ no update needed
- **Follow-up TODOs**: None
-->

# Mobizon.Net Constitution

## Core Principles

### I. Minimal Dependencies

The core SDK (`Mobizon.Net`) MUST depend only on
`System.Text.Json` and `System.Net.Http.HttpClient`.
No third-party HTTP libraries (Refit, RestSharp, Flurl, etc.)
are permitted. Additional functionality (DI, resilience) MUST
be delivered as separate opt-in extension packages.

**Rationale**: Minimizing the dependency footprint reduces
version conflicts, keeps the package lightweight, and gives
consumers full control over their HTTP pipeline.

### II. Contract Separation

All Request/Response DTOs, enums, and service interfaces MUST
reside in a dedicated `Mobizon.Contracts` package. Consumers
MUST be able to depend on contracts without pulling in the
SDK implementation.

**Rationale**: Enables mock-based testing, clean architectural
boundaries, and allows downstream libraries to reference
Mobizon types without a transitive dependency on HttpClient.

### III. Test-First (NON-NEGOTIABLE)

TDD is mandatory. Tests MUST be written BEFORE implementation.
The Red-Green-Refactor cycle MUST be strictly enforced.
Core-logic test coverage MUST be ≥90%. All HTTP interactions
MUST be tested via `MockHttpMessageHandler` — no real HTTP
calls in unit tests.

**Rationale**: The SDK wraps a third-party API with no OpenAPI
schema. TDD ensures every contract assumption is documented
as an executable test before code is written.

### IV. Async-First API Design

Every public SDK method MUST return `Task<T>` and accept a
`CancellationToken` parameter. Synchronous wrappers MUST NOT
be provided. Every API method MUST have dedicated strongly-typed
request and response models. All responses MUST be wrapped in
`MobizonResponse<T>` with `Code`, `Data`, and `Message` fields.

**Rationale**: Modern .NET consumers expect async APIs.
Strict typing prevents stringly-typed errors and enables
IntelliSense-driven discovery of the full API surface.

### V. Complete API Coverage

The SDK MUST implement all five Mobizon API v1 modules:
1. **Message** — SendSmsMessage, GetSMSStatus, List
2. **Campaign** — Create, Delete, Get, GetInfo, List, Send,
   AddRecipients
3. **Link** — Create, Delete, Get, GetLinks, GetStats, List,
   Update
4. **User** — GetOwnBalance
5. **TaskQueue** — GetStatus

No module may be deferred or marked optional.

**Rationale**: A partial SDK forces consumers to fall back to
raw HTTP calls, defeating the purpose of a typed client.

### VI. Regional Flexibility

The SDK MUST support a configurable `baseUrl` to target
regional Mobizon API domains (e.g., `api.mobizon.kz`,
`api.mobizon.com`, `api.mobizon.uz`). The default domain
MUST be explicitly set by the consumer — no hard-coded default.

**Rationale**: Mobizon operates across multiple countries with
separate API endpoints. Consumers MUST be able to target
the correct regional instance.

### VII. IHttpClientFactory Compatibility

The SDK MUST be designed for use with `IHttpClientFactory`.
The DI extension package MUST provide an `AddMobizon()`
method on `IServiceCollection`. The resilience extension
package MUST integrate via `Microsoft.Extensions.Http.Polly`
for retry, circuit breaker, and timeout policies.

**Rationale**: Enterprise .NET applications rely on
`IHttpClientFactory` for proper `HttpClient` lifetime
management, DNS rotation, and policy injection.

## Architecture Constraints

### Target Framework

- `netstandard2.0` — ensuring compatibility with
  .NET Framework 4.6.1+ and .NET Core 2.0+.

### Package Structure

```
Mobizon.Contracts                             — DTOs, Enums, Interfaces
Mobizon.Net                                   — Core SDK implementation
Mobizon.Net.Extensions.DependencyInjection    — IServiceCollection DI
Mobizon.Net.Extensions.Polly                  — Resilience policies
```

### Solution Layout

```
Mobizon.Net.sln
├── src/
│   ├── Mobizon.Contracts/
│   ├── Mobizon.Net/
│   ├── Mobizon.Net.Extensions.DependencyInjection/
│   └── Mobizon.Net.Extensions.Polly/
├── tests/
│   ├── Mobizon.Net.Tests/
│   └── Mobizon.Net.IntegrationTests/
├── samples/
│   └── Mobizon.Net.ConsoleSample/
```

### Explicit Non-Goals

The following are OUT OF SCOPE and MUST NOT be implemented:
- XML response format support (JSON only)
- Synchronous method wrappers
- Response caching
- Webhook/callback handling
- OpenAPI code generation (no schema exists)

## Quality & CI Standards

### Testing Stack

- **Framework**: xUnit
- **Mocking**: Moq or NSubstitute
- **HTTP mocking**: MockHttpMessageHandler
- **Coverage target**: ≥90% for core logic

### Documentation

- XML comments MUST be present on all public APIs.
- README MUST include usage examples for each API module.
- A `ConsoleSample` project MUST demonstrate real usage.

### CI Pipeline (GitHub Actions)

- Build all projects on every push/PR.
- Run all unit tests on every push/PR.
- Publish to NuGet on tagged releases.

### Licensing

- MIT license for all packages.
- Open-source on GitHub.

## AI Agent Workflow

This project is developed using Claude Code agent teams.
The following strategy MUST be followed for task delegation.

### Model Tier Assignment

| Tier | Model | Scope |
|------|-------|-------|
| Architecture | Opus | CONSTITUTION, SPEC, PLAN, core design decisions, code review |
| Implementation | Sonnet | Service implementations, unit tests, integration logic |
| Boilerplate | Haiku | DTOs, XML doc-comments, repetitive scaffolding code |

**Rationale**: Matching model capability to task complexity
optimizes cost and throughput without sacrificing quality
on architectural decisions.

### Parallelism Strategy

Once the core infrastructure (`MobizonApiClient`) is complete:

- **Service modules**: One service (Message) MUST be implemented
  first as a sequential reference implementation. The remaining
  4 services (Campaign, Link, User, TaskQueue) MUST then be
  delegated to parallel subagents using the reference as a
  pattern. Each service is independent and touches separate files.
- **Extension packages**: The DI package and Polly package
  MUST be implemented by parallel subagents. They depend
  on `Mobizon.Net` but not on each other.
- **DTO generation**: Request/Response DTOs for different
  modules MAY be generated in parallel by Haiku subagents.

**Rationale**: The modular architecture (one service per API
module, separate extension packages) is specifically designed
to enable safe parallel development with no file conflicts.

### Sequencing Constraints

1. `Mobizon.Contracts` interfaces and DTOs MUST be finalized
   before any service implementation begins.
2. `MobizonApiClient` (internal HTTP layer) MUST be complete
   and tested before service implementations start.
3. Service implementations MUST be complete before extension
   packages begin (they depend on the registered services).
4. `ConsoleSample` MUST be the last deliverable — it depends
   on all packages being functional.

## Governance

This constitution is the supreme authority for the
Mobizon.Net project. All design decisions, code reviews,
and pull requests MUST comply with its principles.

### Amendment Procedure

1. Propose amendment as a GitHub issue or PR.
2. Document rationale for the change.
3. Update this file with new version, date, and
   sync impact report.
4. Propagate changes to dependent templates and docs.

### Versioning Policy

- **MAJOR**: Principle removal or backward-incompatible
  redefinition.
- **MINOR**: New principle or materially expanded guidance.
- **PATCH**: Wording clarifications, typo fixes.

### Compliance Review

Every PR MUST be checked against active principles.
Violations MUST be justified in the PR description
and tracked in the Complexity Tracking section of
the implementation plan.

**Version**: 1.1.1 | **Ratified**: 2026-02-24 | **Last Amended**: 2026-02-24
