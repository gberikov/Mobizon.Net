# Implementation Plan: Mobizon Webhooks (incoming event handling)

**Branch**: `002-webhooks` | **Date**: 2026-06-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-webhooks/spec.md`

## Summary

Add inbound webhook support to the SDK: verify a Mobizon callback's authenticity (SHA1 over `eventId|attempt|eventCreateTs|secretKey`, constant-time, fail-closed) and parse the raw JSON body into a strongly-typed event (`sms-delivery-report`, `form-submission`, `form-contact-confirmation`, `form-contact-unsubscribe`, plus a forward-compatible "unknown" fallback). Delivered as two new opt-in packages so the core SDK stays dependency-free: `Mobizon.Net.Webhooks` (framework-agnostic verify/parse primitives) and `Mobizon.Net.Webhooks.AspNetCore` (request binding + handler glue). Event DTOs/enums and the new service interfaces live in `Mobizon.Contracts`. JSON only; outbound subscription management is out of scope (configured in the Mobizon panel).

## Technical Context

**Language/Version**: C# 8.0+ — core packages target `netstandard2.0`; the ASP.NET Core integration package and the console sample target `net8.0` (current LTS).
**Primary Dependencies**: `System.Text.Json` 8.0.5 (matches existing projects); BCL `System.Security.Cryptography` (`SHA1`) — no third-party dependencies in the core. The ASP.NET Core package uses the `Microsoft.AspNetCore.App` framework reference and references `Mobizon.Net.Webhooks`.
**Storage**: N/A (stateless parsing/verification; idempotency persistence is the consumer's responsibility).
**Testing**: xUnit (existing convention). Tests are driven by the official sample payloads from the Mobizon docs — no `MockHttpMessageHandler` needed because there is no outbound HTTP. ASP.NET Core handler tested with `Microsoft.AspNetCore.TestHost`/`WebApplicationFactory`.
**Target Platform**: `netstandard2.0` consumers (.NET Framework 4.6.1+, .NET Core 2.0+) for the core; ASP.NET Core (`net8.0`) hosts for the integration package.
**Project Type**: Class-library SDK + opt-in extension packages (matches existing repo layout).
**Performance Goals**: Verify + parse a single event in well under Mobizon's 5-second acknowledgement window — target sub-millisecond per event so the application can return `2xx` immediately and defer processing.
**Constraints**: Dependency-free core (`System.Text.Json` + BCL only); constant-time signature comparison; fail-closed verification; tolerate unknown event types and extra fields; preserve the raw `eventCreateTs` string for signature integrity.
**Scale/Scope**: One event per HTTP request; payloads are small (typically < 8 KB). Up to 10 delivery retries per event (consumer deduplicates via `eventId`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution version at planning time: **1.2.0** (amended 2026-06-24 to add Principle VIII "Inbound Webhook Handling" and register the two webhook packages; webhook handling removed from Explicit Non-Goals).

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Minimal Dependencies | ✅ PASS | Core webhook package depends only on `System.Text.Json` + BCL `SHA1`. ASP.NET helpers isolated in a separate opt-in package — the prescribed pattern. |
| II. Contract Separation | ✅ PASS | Webhook event DTOs, enums, and service interfaces (`IWebhookParser`, `IWebhookSignatureVerifier`, `IWebhookProcessor`) reside in `Mobizon.Contracts`. |
| III. Test-First (NON-NEGOTIABLE) | ✅ PASS | TDD: payload-driven tests written before implementation; ≥90% coverage of verify/parse logic. New `Mobizon.Net.Webhooks.Tests` project. |
| IV. Async-First API Design | ✅ PASS (scoped) | Principle IV governs outbound API calls wrapped in `MobizonResponse<T>`. Per Principle VIII, webhook events are their own typed models, not `MobizonResponse<T>`. Pure verify/parse over a `string` is synchronous CPU work; the only I/O (reading the ASP.NET request body) is async. |
| V. Complete API Coverage | ✅ PASS (N/A) | Webhooks are not one of the five outbound API modules; coverage of those modules is unchanged. |
| VI. Regional Flexibility | ✅ PASS (N/A) | No outbound URL involved in receiving webhooks. |
| VII. IHttpClientFactory Compatibility | ✅ PASS (N/A) | No outbound `HttpClient`. The ASP.NET package still offers an `AddMobizonWebhooks()` DI method for parity with the DI convention. |
| Explicit Non-Goals | ✅ PASS | JSON-only honored; XML and outbound webhook management remain out of scope. |

**Result**: PASS. No violations — Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/002-webhooks/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (public API surface)
│   └── webhook-contracts.md
├── checklists/
│   └── requirements.md  # from /speckit.specify
└── tasks.md             # /speckit.tasks output (NOT created here)
```

### Source Code (repository root)

```text
src/
├── Mobizon.Contracts/
│   ├── Models/
│   │   └── Webhooks/                       # NEW — event DTOs + enum
│   │       ├── WebhookEventType.cs
│   │       ├── MobizonWebhookEvent.cs      # abstract envelope base
│   │       ├── SmsDeliveryReportEvent.cs
│   │       ├── SmsDeliveryReport.cs
│   │       ├── FormSubmissionEvent.cs
│   │       ├── FormSubmission.cs
│   │       ├── FormContactConfirmationEvent.cs
│   │       ├── FormContactConfirmation.cs
│   │       ├── FormContactUnsubscribeEvent.cs
│   │       ├── FormContactUnsubscribe.cs
│   │       ├── WebhookFieldItem.cs
│   │       ├── UnknownWebhookEvent.cs
│   │       └── WebhookProcessResult.cs     # + WebhookProcessStatus enum
│   ├── Services/
│   │   ├── IWebhookParser.cs               # NEW
│   │   ├── IWebhookSignatureVerifier.cs    # NEW
│   │   └── IWebhookProcessor.cs            # NEW
│   └── Exceptions/
│       └── WebhookParseException.cs        # NEW
│
├── Mobizon.Net.Webhooks/                   # NEW project (netstandard2.0)
│   ├── Mobizon.Net.Webhooks.csproj
│   ├── WebhookParser.cs                    # IWebhookParser impl
│   ├── WebhookSignatureVerifier.cs         # IWebhookSignatureVerifier impl
│   ├── WebhookProcessor.cs                 # IWebhookProcessor impl (result types live in Contracts)
│   └── Internal/
│       └── Converters/                     # self-contained JSON converters
│           ├── WebhookSmsStatusConverter.cs   # non-throwing variant
│           └── WebhookDateTimeOffsetConverter.cs
│
├── Mobizon.Net.Webhooks.AspNetCore/        # NEW project (net8.0)
│   ├── Mobizon.Net.Webhooks.AspNetCore.csproj
│   ├── WebhookEndpointRouteBuilderExtensions.cs   # MapMobizonWebhook(...)
│   ├── WebhookServiceCollectionExtensions.cs       # AddMobizonWebhooks()
│   └── MobizonWebhookOptions.cs
│
└── (existing projects unchanged)

tests/
├── Mobizon.Net.Webhooks.Tests/             # NEW (xUnit)
│   ├── WebhookSignatureVerifierTests.cs
│   ├── WebhookParserTests.cs
│   ├── WebhookProcessorTests.cs
│   ├── ForwardCompatibilityTests.cs
│   └── Payloads/                           # official sample JSON fixtures
└── (existing test projects unchanged)

samples/
└── Mobizon.Net.ConsoleSample/
    └── Samples/
        └── WebhookSamples.cs               # NEW — verify+parse a delivery report
```

**Structure Decision**: Follows the established repo layout (`src/`, `tests/`, `samples/`) and the constitution's Solution Layout (now including `Mobizon.Net.Webhooks` and `Mobizon.Net.Webhooks.AspNetCore`). Contracts hold all DTOs/enums/interfaces (Principle II); the dependency-free verify/parse core is a separate package (Principle I); ASP.NET Core glue is isolated in its own `net8.0` package so `netstandard2.0` consumers never pull in a web framework. All four projects are added to `Mobizon.Net.sln`; CI builds/tests the solution, so the new projects are picked up automatically once added to the `.sln` (no CI file change required — confirm the workflow targets `Mobizon.Net.sln` rather than enumerating projects).

## Complexity Tracking

> No constitution violations after the 1.2.0 amendment — this section intentionally left empty.
