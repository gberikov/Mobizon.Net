# Feature Specification: Mobizon Webhooks (incoming event handling)

**Feature Branch**: `002-webhooks`  
**Created**: 2026-06-24  
**Status**: Draft  
**Input**: User description: "Реализуй недавно появившийся функционал вебхуков. Детали возьми из документации"

## Overview

Mobizon recently introduced **webhooks** — a mechanism that pushes notifications from Mobizon to a customer's system over HTTP(S) the moment an event actually happens, instead of the customer polling the API. The SDK currently has no support for the receive side of this mechanism, so consumers who want real-time SMS delivery statuses or form events have to poll `Message/GetSMSStatus` on a timer.

This feature adds **incoming webhook support** to the SDK: turning a raw HTTP POST body delivered by Mobizon into a strongly-typed, validated event that an application can act on, plus authenticity verification so forged or tampered requests are rejected. Webhooks are created and configured in the Mobizon control panel (not via API), so this feature is scoped to **receiving, authenticating, and parsing** webhook callbacks — not creating or managing webhook subscriptions. The feature ships in two layers: framework-agnostic core primitives, plus an **optional ASP.NET Core integration delivered as a separate assembly** so the core stays dependency-free.

## Clarifications

### Session 2026-06-24

- Q: How far should the SDK's webhook integration surface go in this feature? → A: Core primitives (verifier + parser) **plus** ASP.NET Core helpers shipped as a **separate assembly/package**, keeping the .NET Standard 2.0 core dependency-free.
- Q: Which inbound payload formats must the SDK parse in this first release? → A: **JSON only** (the panel `raw` default is treated as JSON); XML is out of scope for this release.
- Q: How should timestamp fields be exposed on the typed events? → A: Expose parsed **`DateTimeOffset`** for convenience **and** preserve the original `eventCreateTs` string verbatim so signature verification uses the exact bytes Mobizon signed.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Receive real-time SMS delivery statuses (Priority: P1)

A developer integrating the SDK wants their application to learn the final delivery status of each sent SMS the moment it is known, without polling. They configure a webhook of type "SMS statuses" in the Mobizon panel pointing at an endpoint in their app. When Mobizon delivers a callback, the developer hands the raw request (body + signature material) to the SDK and receives a typed delivery-report event (message id, campaign id, final status, recipient, segment count, status timestamp) that they can persist or act on.

**Why this priority**: This is the primary motivation for webhooks and the single highest-value gap versus the current polling-only experience. It is independently useful even if no other event type is supported.

**Independent Test**: Feed a sample `sms-delivery-report` payload (as published in the Mobizon docs) into the SDK and confirm it produces a typed event with every documented field populated correctly, and that a tampered payload is rejected.

**Acceptance Scenarios**:

1. **Given** a valid `sms-delivery-report` callback body and a correct signature, **When** the developer passes it to the SDK with their webhook secret key, **Then** the SDK confirms authenticity and returns a typed delivery-report event exposing `campaignId`, `messageId`, `segNum`, `statusUpdateTs`, `status`, and `to`.
2. **Given** a callback whose `sign` value does not match the recomputed signature, **When** the developer asks the SDK to verify it, **Then** the SDK reports the request as not authentic and the application can respond with a rejection (e.g. HTTP 403) without processing the event.
3. **Given** the same `eventId` arrives more than once (a retry), **When** the developer inspects the parsed event, **Then** the stable `eventId` and `attempt` number are available so the application can detect and skip duplicate processing.

---

### User Story 2 - Receive form events (Priority: P2)

A developer using Mobizon forms wants to be notified when a user submits a form, confirms a contact (phone/email), or unsubscribes. They configure a webhook of type "Form events" and route the callbacks through the SDK to get typed events describing the form, submission, and field values.

**Why this priority**: Forms are a distinct, valuable event family, but secondary to SMS delivery statuses for the typical SMS-gateway integration. Supporting it broadens the SDK to the full documented event set.

**Independent Test**: Feed sample `form-submission`, `form-contact-confirmation`, and `form-contact-unsubscribe` payloads into the SDK and confirm each is parsed into the correct typed event with its field collection intact.

**Acceptance Scenarios**:

1. **Given** a valid `form-submission` callback, **When** parsed, **Then** the SDK returns an event exposing `formId`, `submissionId`, and the collection of submitted field items (each with field id, type, name, value, and confirmation-required flag).
2. **Given** a valid `form-contact-confirmation` callback, **When** parsed, **Then** the SDK returns an event exposing the confirmed field item including its confirmation timestamp.
3. **Given** a valid `form-contact-unsubscribe` callback, **When** parsed, **Then** the SDK returns an event exposing `formId`, `unsubscribeTs`, and the affected field items.

---

### User Story 3 - Safely dispatch unknown or future event types (Priority: P3)

A developer wants their integration to keep working when Mobizon adds new event types or fields in the future, rather than crashing on something the SDK version does not recognise.

**Why this priority**: Forward compatibility is a robustness concern, valuable but not required for the core happy path.

**Independent Test**: Feed a payload with an unrecognised `eventType` and confirm the SDK still verifies the signature and surfaces the common envelope fields without throwing.

**Acceptance Scenarios**:

1. **Given** a callback whose `eventType` is not known to the current SDK version, **When** it is processed, **Then** the SDK still verifies authenticity and exposes the common envelope (`eventId`, `eventType`, `eventCreateTs`, `webhookId`, `attempt`) plus the raw event data, without raising an error.
2. **Given** a known event type that has gained an extra undocumented field, **When** it is parsed, **Then** the additional field does not cause parsing to fail.

---

### Edge Cases

- **Malformed body**: A body that is not valid for its declared format produces a clear, catchable error distinguishing "could not parse" from "signature mismatch".
- **Missing signature material**: If `sign`, `eventId`, `attempt`, or `eventCreateTs` is absent, verification fails closed (treated as not authentic) rather than passing by default.
- **Wrong secret key**: Verifying with an incorrect secret key reports not-authentic; the application can then reject the request.
- **Multiple webhooks for one event type / multiple webhooks on one endpoint**: The same logical event may arrive via more than one configured webhook, each with its own secret key. `webhookId` is available to disambiguate and to resolve the correct secret for verification (FR-004a).
- **Retry storm**: Up to 10 retries with increasing intervals can arrive for one event; idempotency keys (`eventId`) let the application deduplicate.
- **Timing constraint**: Mobizon considers a delivery failed if no `2xx` is returned within 5 seconds; verification + parsing must be fast enough that the application can acknowledge well within that window and defer heavy work.

## Requirements *(mandatory)*

### Functional Requirements

#### Authenticity verification

- **FR-001**: The SDK MUST verify a webhook's authenticity by recomputing the signature from the documented inputs (`eventId`, `attempt`, `eventCreateTs`, and the webhook secret key, joined in that order with the `|` separator, hashed with SHA1) and comparing it against the request's `sign` value.
- **FR-002**: The signature comparison MUST be performed in a way that does not leak timing information (constant-time comparison).
- **FR-003**: Verification MUST fail closed: any missing input, format error, or mismatch results in "not authentic", never a default pass.
- **FR-004**: The SDK MUST allow the consumer to supply the webhook secret key at verification time (it is per-webhook and chosen by the user when the webhook is created).
- **FR-004a**: Because the secret key is per-webhook, the SDK MUST support resolving the secret based on the inbound event's `webhookId` (so multiple webhooks with different secrets can target one endpoint). The `webhookId` is read from the parsed envelope before verification; parsing the envelope confers no trust — the resolved secret is still used to verify the signature. A constant single secret remains supported as the simple case.

#### Parsing & event model

- **FR-005**: The SDK MUST parse the common webhook envelope fields: `eventId`, `eventType`, `eventCreateTs`, `webhookId`, `attempt`, `data`, `sign`.
- **FR-006**: The SDK MUST provide a typed representation for the `sms-delivery-report` event including `campaignId`, `messageId`, `segNum`, `statusUpdateTs`, `status`, and `to`.
- **FR-007**: The SDK MUST provide a typed representation for the `form-submission` event including `formId`, `submissionId`, and a collection of field items (`submissionDataId`, `fieldId`, `fieldType`, `fieldName`, `value`, `confirmationRequired`).
- **FR-008**: The SDK MUST provide a typed representation for the `form-contact-confirmation` event including `formId`, `submissionId`, and the confirmed item (with `confirmationTs`).
- **FR-009**: The SDK MUST provide a typed representation for the `form-contact-unsubscribe` event including `formId`, `unsubscribeTs`, and affected field items.
- **FR-010**: The SDK MUST expose the SMS delivery status value using the project's existing message-status representation so consumers handle webhook statuses the same way as polled statuses.
- **FR-011**: When the `eventType` is not recognised, the SDK MUST still expose the common envelope and the unparsed event data without throwing.
- **FR-012**: Parsing MUST tolerate unknown/extra fields in the payload without failing.

#### Data formats

- **FR-013**: The SDK MUST support parsing webhooks delivered in the `json` format (the project default). The panel `raw` default format is treated as `json`.
- **FR-014**: Parsing of the `xml` format is OUT OF SCOPE for this release. The design MUST NOT preclude adding XML later, but no XML parsing is delivered now.

#### Integration surface

- **FR-015**: The verification and parsing capabilities MUST be usable from any HTTP server stack the consumer chooses, operating on the raw request body and the values needed for signature checking, without requiring a specific web framework. These primitives live in the dependency-free core.
- **FR-016**: The SDK MUST offer a single combined operation that both verifies authenticity and returns the typed event, so the recommended "verify → parse → acknowledge → process" flow is easy to follow correctly.
- **FR-017**: The SDK MUST make idempotency feasible by exposing the stable `eventId` and `attempt` on every parsed event.
- **FR-018**: The SDK MUST provide ASP.NET Core integration helpers (e.g. binding the incoming request, verifying the signature, and producing the typed event for a handler) delivered as a **separate assembly/package** that depends on the core. The core assembly MUST NOT take a dependency on ASP.NET Core or any other web framework.
- **FR-018a**: The ASP.NET Core helper MUST verify and parse synchronously to the point of returning the HTTP status (200/403/400), so the consumer can acknowledge within Mobizon's 5-second window even when the consumer's handler defers heavy work. The verify+parse step MUST add negligible latency relative to that window (see SC-005).

#### Timestamps

- **FR-019**: The SDK MUST expose timestamp fields (`eventCreateTs`, `statusUpdateTs`, `confirmationTs`, `unsubscribeTs`) as parsed `DateTimeOffset` values for consumer convenience.
- **FR-020**: The SDK MUST preserve the original `eventCreateTs` string exactly as received and use that verbatim value (not a reformatted/round-tripped value) when recomputing the signature, so verification matches the bytes Mobizon signed. Empty/absent timestamp values (e.g. an unconfirmed `confirmationTs: ""`) MUST surface as null rather than causing a parse failure.

#### Documentation & examples

- **FR-021**: The SDK MUST include a usage example demonstrating receiving, verifying, and handling at least an `sms-delivery-report` webhook, consistent with the existing per-endpoint samples in the console sample project. The example SHOULD show the ASP.NET Core helper path.

### Key Entities *(include if feature involves data)*

- **Webhook Event (envelope)**: The common wrapper around every callback — unique event id, event type, creation timestamp (as `DateTimeOffset`, with the original string preserved for signature verification), originating webhook id, delivery attempt number, the event-specific data payload, and the signature.
- **SMS Delivery Report**: The data for an `sms-delivery-report` event — links a final delivery status to a specific message and campaign for a specific recipient.
- **Form Submission Event**: The data for a `form-submission` — a form, a submission, and the collection of field values the user entered.
- **Form Contact Confirmation Event**: The data for a confirmed phone/email contact within a form submission, with the moment of confirmation.
- **Form Contact Unsubscribe Event**: The data for an unsubscribe action against a form, with the moment of unsubscribe and affected fields.
- **Webhook Field Item**: A single form field value (id, type, name, value, confirmation metadata) appearing in form events.
- **Signature inputs**: The ordered values (`eventId`, `attempt`, `eventCreateTs`, secret key) from which the authenticity signature is derived.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can go from a raw Mobizon webhook request to a verified, typed event in a single SDK call.
- **SC-002**: Every event type documented by Mobizon today (`sms-delivery-report`, `form-submission`, `form-contact-confirmation`, `form-contact-unsubscribe`) parses with 100% of its documented fields populated when fed the official sample payloads.
- **SC-003**: A request with a tampered or absent signature is rejected 100% of the time; a genuine request signed with the correct secret key is accepted 100% of the time.
- **SC-004**: An unrecognised future event type never causes an unhandled error — the envelope is still exposed and signature verification still runs.
- **SC-005**: Verification plus parsing of a single event completes fast enough that an application can acknowledge well within Mobizon's 5-second success window, leaving heavy processing to be done asynchronously.
- **SC-006**: The core webhook capability has no web-framework dependency — the verifier/parser primitives work regardless of the consumer's HTTP stack — while an ASP.NET Core consumer can wire up a verified, typed handler with minimal code via the separate integration assembly.

## Assumptions

- **Scope is receive-side only.** Webhooks are created/edited/deleted in the Mobizon control panel, not via the public API, so this feature does not add outbound webhook-management API calls. If a management API surfaces later, it is a separate feature.
- **Secret key is supplied by the consumer.** The secret is chosen by the user at webhook creation in the panel; the SDK does not store or retrieve it.
- **Framework-agnostic core + separate ASP.NET Core assembly.** The core SDK targets .NET Standard 2.0 and must not take a hard dependency on any web framework; webhook verification/parsing is exposed as plain primitives. ASP.NET Core helpers ARE in scope for this feature but ship in a **separate assembly/package** that depends on the core, so the core stays dependency-free.
- **JSON only for this release.** Consumers are expected to configure webhooks in `json` (or the equivalent `raw` default, which is treated as JSON). `xml` inbound parsing is out of scope for this release and may be added later.
- **Idempotency is the consumer's responsibility.** The SDK exposes the data needed to deduplicate (`eventId`, `attempt`) but does not itself persist or deduplicate events.
- **Final statuses only.** Per Mobizon, SMS webhooks fire once per message (not per segment) and only on a final delivery status; the SDK does not synthesise intermediate statuses.

## Dependencies

- Mobizon webhook documentation (event types, payload structures, signature algorithm, delivery/retry semantics) as the source of truth for field names and the signature formula.
- The SDK's existing message-status representation (reused for the delivery-report status field).
- The SDK's existing JSON serialization conventions reused for payload parsing.
