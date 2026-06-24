# Tasks: Mobizon Webhooks (incoming event handling)

**Input**: Design documents from `/specs/002-webhooks/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUDED — the project constitution mandates Test-First (Principle III, NON-NEGOTIABLE). Test tasks precede their implementation and MUST fail before the implementation task runs.

**Organization**: Tasks are grouped by user story. US1 (P1) is the MVP — a complete vertical slice from HTTP request to typed delivery-report event.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (setup, foundational, polish carry no story label)

## Path Conventions

New projects (per plan.md Structure Decision):
- `src/Mobizon.Contracts/` — webhook DTOs, enums, interfaces (existing project, new `Models/Webhooks/` folder)
- `src/Mobizon.Net.Webhooks/` — verify/parse core (NEW, netstandard2.0)
- `src/Mobizon.Net.Webhooks.AspNetCore/` — ASP.NET Core glue (NEW, net8.0)
- `tests/Mobizon.Net.Webhooks.Tests/` — xUnit tests (NEW, net8.0)
- `samples/Mobizon.Net.ConsoleSample/` — existing sample project

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the new projects and wire them into the solution.

- [X] T001 Create `src/Mobizon.Net.Webhooks/Mobizon.Net.Webhooks.csproj` targeting `netstandard2.0`, `PackageReference` System.Text.Json 8.0.5, `ProjectReference` to Mobizon.Contracts
- [X] T002 [P] Create `src/Mobizon.Net.Webhooks.AspNetCore/Mobizon.Net.Webhooks.AspNetCore.csproj` targeting `net8.0` with `FrameworkReference Microsoft.AspNetCore.App` and `ProjectReference` to Mobizon.Net.Webhooks
- [X] T003 [P] Create `tests/Mobizon.Net.Webhooks.Tests/Mobizon.Net.Webhooks.Tests.csproj` targeting `net8.0` (xUnit, Microsoft.AspNetCore.Mvc.Testing) with `ProjectReference`s to Mobizon.Net.Webhooks, Mobizon.Net.Webhooks.AspNetCore, Mobizon.Contracts
- [X] T004 Add the four new projects to `Mobizon.Net.sln` (depends on T001, T002, T003)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared contracts and core primitives every user story depends on (envelope, signature verifier, parser dispatch with Unknown fallback, processor).

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Contracts — DTOs, enums, interfaces (Principle II)

- [X] T005 [P] Create `WebhookEventType` enum (SmsDeliveryReport, FormSubmission, FormContactConfirmation, FormContactUnsubscribe, Unknown) in `src/Mobizon.Contracts/Models/Webhooks/WebhookEventType.cs`
- [X] T006 [P] Create abstract `MobizonWebhookEvent` envelope base (EventId, EventType, EventTypeRaw, EventCreateTs, EventCreateTsRaw, WebhookId, Attempt, Sign) in `src/Mobizon.Contracts/Models/Webhooks/MobizonWebhookEvent.cs`
- [X] T007 [P] Create `UnknownWebhookEvent : MobizonWebhookEvent` (exposes `JsonElement RawData`) in `src/Mobizon.Contracts/Models/Webhooks/UnknownWebhookEvent.cs`
- [X] T008 [P] Create `WebhookParseException : Exception` in `src/Mobizon.Contracts/Exceptions/WebhookParseException.cs`
- [X] T009 [P] Create `WebhookProcessStatus` enum and `WebhookProcessResult` (Status, Event, IsAuthentic) in `src/Mobizon.Contracts/Models/Webhooks/WebhookProcessResult.cs`
- [X] T010 [P] Create `IWebhookSignatureVerifier` interface in `src/Mobizon.Contracts/Services/IWebhookSignatureVerifier.cs`
- [X] T011 [P] Create `IWebhookParser` interface in `src/Mobizon.Contracts/Services/IWebhookParser.cs`
- [X] T012 [P] Create `IWebhookProcessor` interface with both overloads — `Process(string body, string secretKey)` and `Process(string body, Func<MobizonWebhookEvent,string> secretSelector)` for per-`WebhookId` secrets (FR-004a) — returning `WebhookProcessResult`, in `src/Mobizon.Contracts/Services/IWebhookProcessor.cs` (depends on T009)

### Core converters (Mobizon.Net.Webhooks, internal)

- [X] T013 [P] Create internal `WebhookDateTimeOffsetConverter` (`JsonConverter<DateTimeOffset?>`, format `yyyy-MM-dd HH:mm:ss`, empty/null ⇒ null) in `src/Mobizon.Net.Webhooks/Internal/Converters/WebhookDateTimeOffsetConverter.cs`
- [X] T014 [P] Create internal non-throwing `WebhookSmsStatusConverter` (maps DELIVRD/UNDELIV/REJECTD/EXPIRD/etc. to SmsStatus; unknown ⇒ leaves StatusRaw, no throw) in `src/Mobizon.Net.Webhooks/Internal/Converters/WebhookSmsStatusConverter.cs`

### Foundational tests (write FIRST — must fail)

- [X] T015 [P] Write `WebhookSignatureVerifierTests` (valid signature accepted; tampered rejected; missing eventId/attempt/eventCreateTs/sign/secret fails closed; constant-time path) in `tests/Mobizon.Net.Webhooks.Tests/WebhookSignatureVerifierTests.cs`
- [X] T016 [P] Write `WebhookParserTests` for envelope parsing + Unknown fallback (envelope fields populated, unknown eventType ⇒ UnknownWebhookEvent, EventCreateTsRaw preserved verbatim) in `tests/Mobizon.Net.Webhooks.Tests/WebhookParserTests.cs`
- [X] T017 [P] Write `WebhookProcessorTests` (Ok when parsed+signed; SignatureMismatch when signature wrong; ParseError when body unparseable; `Event` non-null for Ok+SignatureMismatch, null for ParseError; selector overload picks secret by `WebhookId`) in `tests/Mobizon.Net.Webhooks.Tests/WebhookProcessorTests.cs`

### Foundational implementation (make tests pass)

- [X] T018 Implement `WebhookSignatureVerifier : IWebhookSignatureVerifier` (SHA1 over `eventId|attempt|eventCreateTs|secret`, lowercase hex, constant-time compare, fail-closed) in `src/Mobizon.Net.Webhooks/WebhookSignatureVerifier.cs` (passes T015; depends on T006, T010)
- [X] T019 Implement `WebhookParser : IWebhookParser` — parse envelope, read `eventType`, dispatch via switch (Unknown fallback for now), tolerate unknown fields, throw `WebhookParseException` on malformed/missing-required — in `src/Mobizon.Net.Webhooks/WebhookParser.cs` (passes T016; depends on T005, T006, T007, T008, T011, T013)
- [X] T020 Implement `WebhookProcessor : IWebhookProcessor` — default ctor wiring parser+verifier; both `Process` overloads (single secret + selector); flow = parse → (select secret) → verify → map to Ok/SignatureMismatch/ParseError; `Event` populated for Ok and SignatureMismatch, null on ParseError — in `src/Mobizon.Net.Webhooks/WebhookProcessor.cs` (passes T017; depends on T009, T012, T018, T019)

**Checkpoint**: Foundation ready — signature verification, envelope parsing, Unknown fallback, and the combined processor all work. User stories can begin.

---

## Phase 3: User Story 1 - Receive real-time SMS delivery statuses (Priority: P1) 🎯 MVP

**Goal**: A correctly-signed `sms-delivery-report` callback is verified and parsed into a typed `SmsDeliveryReportEvent`, end-to-end including an ASP.NET Core endpoint.

**Independent Test**: POST the official `sms-delivery-report` sample to the mapped endpoint with a correct signature → 200 + handler receives all fields; tampered signature → 403; malformed body → 400.

### Tests for User Story 1 (write FIRST — must fail)

- [X] T021 [P] [US1] Parser test: `sms-delivery-report` sample → `SmsDeliveryReportEvent` with all fields (campaignId, messageId, segNum, statusUpdateTs, status, to) in `tests/Mobizon.Net.Webhooks.Tests/SmsDeliveryReportParseTests.cs`
- [X] T022 [P] [US1] Status-mapping test: `DELIVRD` ⇒ `SmsStatus.Delivered`; unknown status string ⇒ preserved in `StatusRaw`, no throw, in `tests/Mobizon.Net.Webhooks.Tests/SmsStatusMappingTests.cs`
- [X] T023 [P] [US1] ASP.NET endpoint test via `WebApplicationFactory`: 200 + handler invoked (valid), 403 + handler NOT invoked (tampered), 400 (malformed); secret resolved per `WebhookId` (FR-004a); response returned before a deliberately slow handler completes, proving ack-then-process (FR-018a, SC-005) in `tests/Mobizon.Net.Webhooks.Tests/AspNetCoreWebhookEndpointTests.cs`

### Implementation for User Story 1

- [X] T024 [P] [US1] Create `SmsDeliveryReport` data class (CampaignId, MessageId, SegNum, StatusUpdateTs, Status:SmsStatus, StatusRaw, To) in `src/Mobizon.Contracts/Models/Webhooks/SmsDeliveryReport.cs`
- [X] T025 [P] [US1] Create `SmsDeliveryReportEvent : MobizonWebhookEvent` (Data: SmsDeliveryReport) in `src/Mobizon.Contracts/Models/Webhooks/SmsDeliveryReportEvent.cs`
- [X] T026 [P] [US1] Add `Payloads/sms-delivery-report.json` fixture (official sample) in `tests/Mobizon.Net.Webhooks.Tests/Payloads/sms-delivery-report.json`
- [X] T027 [US1] Add `sms-delivery-report` case to `WebhookParser` dispatch (deserialize data using the status + datetime converters) in `src/Mobizon.Net.Webhooks/WebhookParser.cs` (passes T021, T022; depends on T019, T013, T014, T024, T025)
- [X] T028 [P] [US1] Create `MobizonWebhookOptions` with `SecretKeyResolver` of type `Func<IServiceProvider, MobizonWebhookEvent, string>` (per-`WebhookId` secret; single-secret = ignore event arg) in `src/Mobizon.Net.Webhooks.AspNetCore/MobizonWebhookOptions.cs`
- [X] T029 [US1] Implement `AddMobizonWebhooks()` registering IWebhookProcessor + options in `src/Mobizon.Net.Webhooks.AspNetCore/WebhookServiceCollectionExtensions.cs` (depends on T020, T028)
- [X] T030 [US1] Implement `MapMobizonWebhook(pattern, handler)` — async body read, parse, resolve secret from parsed event via `SecretKeyResolver` (per `WebhookId`), verify, return 200/403/400 before invoking handler, invoke handler only on Ok — in `src/Mobizon.Net.Webhooks.AspNetCore/WebhookEndpointRouteBuilderExtensions.cs` (passes T023; depends on T029)
- [X] T031 [US1] Add `WebhookSamples.cs` demonstrating verify+parse of a delivery report (core primitives + ASP.NET path) in `samples/Mobizon.Net.ConsoleSample/Samples/WebhookSamples.cs` (FR-021; depends on T027, T030)

**Checkpoint**: MVP complete — SMS delivery reports flow end-to-end and are independently testable.

---

## Phase 4: User Story 2 - Receive form events (Priority: P2)

**Goal**: `form-submission`, `form-contact-confirmation`, and `form-contact-unsubscribe` callbacks parse into typed events with their field collections.

**Independent Test**: Feed each official form sample → correct typed event with all field items; `confirmationTs: ""` ⇒ null.

### Tests for User Story 2 (write FIRST — must fail)

- [X] T032 [P] [US2] Parser test: `form-submission` sample → `FormSubmissionEvent` (formId, submissionId, items[] with all WebhookFieldItem fields) in `tests/Mobizon.Net.Webhooks.Tests/FormSubmissionParseTests.cs`
- [X] T033 [P] [US2] Parser test: `form-contact-confirmation` sample → `FormContactConfirmationEvent` (item with ConfirmationTs) in `tests/Mobizon.Net.Webhooks.Tests/FormContactConfirmationParseTests.cs`
- [X] T034 [P] [US2] Parser test: `form-contact-unsubscribe` sample → `FormContactUnsubscribeEvent` (unsubscribeTs, items[]); empty `confirmationTs` ⇒ null in `tests/Mobizon.Net.Webhooks.Tests/FormContactUnsubscribeParseTests.cs`

### Implementation for User Story 2

- [X] T035 [P] [US2] Create `WebhookFieldItem` (SubmissionId?, SubmissionDataId, FieldId, FieldType, FieldName, Value, ConfirmationRequired?, ConfirmationTs?) in `src/Mobizon.Contracts/Models/Webhooks/WebhookFieldItem.cs`
- [X] T036 [P] [US2] Create `FormSubmission` + `FormSubmissionEvent` in `src/Mobizon.Contracts/Models/Webhooks/FormSubmission.cs` and `FormSubmissionEvent.cs`
- [X] T037 [P] [US2] Create `FormContactConfirmation` + `FormContactConfirmationEvent` in `src/Mobizon.Contracts/Models/Webhooks/FormContactConfirmation.cs` and `FormContactConfirmationEvent.cs`
- [X] T038 [P] [US2] Create `FormContactUnsubscribe` + `FormContactUnsubscribeEvent` in `src/Mobizon.Contracts/Models/Webhooks/FormContactUnsubscribe.cs` and `FormContactUnsubscribeEvent.cs`
- [X] T039 [P] [US2] Add `Payloads/form-submission.json`, `Payloads/form-contact-confirmation.json`, `Payloads/form-contact-unsubscribe.json` fixtures in `tests/Mobizon.Net.Webhooks.Tests/Payloads/`
- [X] T040 [US2] Add the three form-event cases to `WebhookParser` dispatch in `src/Mobizon.Net.Webhooks/WebhookParser.cs` (passes T032–T034; depends on T019, T013, T035, T036, T037, T038)

**Checkpoint**: All documented event types parse. US1 and US2 both work independently.

---

## Phase 5: User Story 3 - Safely dispatch unknown or future event types (Priority: P3)

**Goal**: Unknown event types and extra/undocumented fields never crash the handler; the envelope is still exposed and the signature still verifiable.

**Independent Test**: Feed a payload with an unrecognised `eventType` (and a known event with extra fields) → `UnknownWebhookEvent` / successful parse, signature verifies, no exception.

### Tests for User Story 3 (write FIRST — must fail)

- [X] T041 [P] [US3] Test: unrecognised `eventType` ⇒ `UnknownWebhookEvent` with envelope populated and signature verifying; `RawData` accessible in `tests/Mobizon.Net.Webhooks.Tests/ForwardCompatibilityTests.cs`
- [X] T042 [P] [US3] Test: a known event with extra/unknown JSON fields parses without error in `tests/Mobizon.Net.Webhooks.Tests/UnknownFieldToleranceTests.cs`
- [X] T043 [P] [US3] Test: unrecognised SMS `status` string ⇒ surfaced via `StatusRaw`, no throw in `tests/Mobizon.Net.Webhooks.Tests/UnknownStatusToleranceTests.cs`

### Implementation for User Story 3

- [X] T044 [US3] Harden `WebhookParser` JsonSerializerOptions for unknown-field tolerance and confirm Unknown-type dispatch + StatusRaw fallback satisfy T041–T043 in `src/Mobizon.Net.Webhooks/WebhookParser.cs` (depends on T019, T027)

**Checkpoint**: Forward-compatibility guaranteed across all stories.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, packaging, and validation across all stories.

- [X] T045 [P] Add XML doc comments to all public webhook types/members in `src/Mobizon.Contracts/Models/Webhooks/`, `src/Mobizon.Net.Webhooks/`, and `src/Mobizon.Net.Webhooks.AspNetCore/` (constitution Documentation)
- [X] T046 [P] Add a "Webhooks" usage section to `README.md` covering both the core primitives and the ASP.NET Core package
- [X] T047 [P] Add NuGet metadata (PackageId, Description, MIT license) to `src/Mobizon.Net.Webhooks/Mobizon.Net.Webhooks.csproj` and `src/Mobizon.Net.Webhooks.AspNetCore/Mobizon.Net.Webhooks.AspNetCore.csproj`
- [X] T048 Verify ≥90% test coverage for `Mobizon.Net.Webhooks` core logic (constitution Principle III) and address gaps
- [X] T049 [P] Wire `WebhookSamples` into `samples/Mobizon.Net.ConsoleSample/Program.cs` menu (depends on T031)
- [X] T050 Run `quickstart.md` validation end-to-end (Options A and B) and confirm the Success Criteria checklist

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T004 depends on T001–T003.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories. Within it: contracts (T005–T012) and converters (T013–T014) → foundational tests (T015–T017) → foundational impl (T018–T020).
- **User Stories (Phase 3–5)**: All depend on Foundational completion.
  - US1 (P1) → MVP, do first.
  - US2 (P2) and US3 (P3) depend only on Foundational, BUT all three touch `WebhookParser.cs` dispatch (T027 / T040 / T044) — those specific edits are sequential (US1 → US2 → US3). Everything else per story is independent.
- **Polish (Phase 6)**: After all targeted stories complete.

### User Story Dependencies

- **US1 (P1)**: Foundational only. Adds SMS event types + ASP.NET endpoint. No dependency on US2/US3.
- **US2 (P2)**: Foundational only. Shares `WebhookParser.cs` with US1 (T040 after T027).
- **US3 (P3)**: Foundational only. Hardening edit (T044) after T027 (and ideally after T040) on `WebhookParser.cs`.

### Parallel Opportunities

- Setup: T002, T003 in parallel (after/with T001).
- Foundational: all contracts T005–T011 in parallel; converters T013–T014 in parallel; foundational tests T015–T017 in parallel.
- US1: tests T021–T023 in parallel; models T024, T025, fixture T026, options T028 in parallel. T027 (parser) and T029/T030 (ASP.NET) are sequential per their deps.
- US2: tests T032–T034 in parallel; models T035–T038 + fixtures T039 in parallel; T040 last (shared parser file).
- US3: tests T041–T043 in parallel; T044 last.
- Polish: T045–T047, T049 in parallel.

---

## Parallel Example: Foundational contracts

```bash
# After Setup, create all Contracts types together (different files):
Task: "T005 Create WebhookEventType enum"
Task: "T006 Create MobizonWebhookEvent base"
Task: "T007 Create UnknownWebhookEvent"
Task: "T008 Create WebhookParseException"
Task: "T009 Create WebhookProcessResult + WebhookProcessStatus"
Task: "T010 Create IWebhookSignatureVerifier"
Task: "T011 Create IWebhookParser"
```

## Parallel Example: User Story 1 tests

```bash
# Write all US1 tests first (must fail before implementation):
Task: "T021 Parser test for sms-delivery-report"
Task: "T022 Status-mapping test"
Task: "T023 ASP.NET endpoint test (WebApplicationFactory)"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup (4 tasks)
2. Phase 2: Foundational (16 tasks) — CRITICAL, blocks everything
3. Phase 3: US1 (11 tasks) — SMS delivery reports end-to-end
4. **STOP and VALIDATE**: official sample → 200 + typed event; tampered → 403
5. Ship MVP — consumers can replace `GetSMSStatus` polling immediately.

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. US1 → test → ship (MVP)
3. US2 → test → ship (form events)
4. US3 → test → ship (forward-compat hardening)
5. Polish → docs, packaging, coverage, quickstart validation

### TDD note (Principle III)

Within every phase, the listed test tasks are written first and MUST fail before their implementation task is started. Core verify/parse coverage target is ≥90%.

---

## Notes

- [P] = different files, no incomplete dependencies.
- `WebhookParser.cs` is the one shared file across stories (T019 → T027 → T040 → T044); those edits are intentionally sequential to avoid conflicts.
- Webhook events are typed models, NOT wrapped in `MobizonResponse<T>` (per constitution Principle VIII).
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.

## Task Count Summary

- **Total**: 50 tasks
- Setup: 4 · Foundational: 16 · US1 (MVP): 11 · US2: 9 · US3: 4 · Polish: 6
