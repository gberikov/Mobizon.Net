# Phase 1 Data Model: Mobizon Webhooks

All types below live in `Mobizon.Contracts` (namespace `Mobizon.Contracts.Models.Webhooks`) unless noted. Property names are the public C# names; the JSON field they bind to is shown in parentheses. All timestamp properties are `DateTimeOffset?` parsed from `yyyy-MM-dd HH:mm:ss`, with `null` for empty/missing.

## Enum: WebhookEventType

Maps the `eventType` string to a known value; unrecognised types become `Unknown`.

| Member | JSON `eventType` |
|--------|------------------|
| `SmsDeliveryReport` | `sms-delivery-report` |
| `FormSubmission` | `form-submission` |
| `FormContactConfirmation` | `form-contact-confirmation` |
| `FormContactUnsubscribe` | `form-contact-unsubscribe` |
| `Unknown` | (any other value) |

## Abstract base: MobizonWebhookEvent (envelope)

Common to every callback (FR-005). Abstract; concrete subclass chosen by `EventType`.

| Property | JSON | Type | Notes |
|----------|------|------|-------|
| `EventId` | `eventId` | `long` | Stable across retries — idempotency key (FR-017). |
| `EventType` | `eventType` | `WebhookEventType` | Parsed known type. |
| `EventTypeRaw` | `eventType` | `string` | Original string (fidelity for `Unknown`). |
| `EventCreateTs` | `eventCreateTs` | `DateTimeOffset?` | Parsed convenience value (FR-019). |
| `EventCreateTsRaw` | `eventCreateTs` | `string` | **Verbatim** — used for signature (FR-020). |
| `WebhookId` | `webhookId` | `long` | Source webhook (disambiguates multiple webhooks). |
| `Attempt` | `attempt` | `int` | Delivery attempt number (FR-017). |
| `Sign` | `sign` | `string` | Signature to verify. |

**Validation**: `EventId`, `Attempt`, `EventCreateTsRaw`, `Sign` are required for verification; absence ⇒ verification fails closed (FR-003).

## SmsDeliveryReportEvent : MobizonWebhookEvent

Adds `Data` of type **SmsDeliveryReport** (bound from `data`). Produced when `EventType == SmsDeliveryReport` (FR-006).

### SmsDeliveryReport

| Property | JSON | Type | Notes |
|----------|------|------|-------|
| `CampaignId` | `campaignId` | `long` | |
| `MessageId` | `messageId` | `long` | Correlates with `SendSmsResult` / `GetSMSStatus`. |
| `SegNum` | `segNum` | `int` | Segment count (one webhook per message, not per segment). |
| `StatusUpdateTs` | `statusUpdateTs` | `DateTimeOffset?` | |
| `Status` | `status` | `SmsStatus?` | Reuses `Mobizon.Contracts.Models.Messages.SmsStatus` (FR-010); `null` when the raw status is unrecognised — non-throwing map (research D3). |
| `StatusRaw` | `status` | `string` | Original string; preserves unknown future statuses. |
| `To` | `to` | `string` | Recipient MSISDN. |

## FormSubmissionEvent : MobizonWebhookEvent

Adds `Data` of type **FormSubmission** (FR-007).

### FormSubmission

| Property | JSON | Type | Notes |
|----------|------|------|-------|
| `FormId` | `formId` | `long` | |
| `SubmissionId` | `submissionId` | `long` | |
| `Items` | `items` | `IReadOnlyList<WebhookFieldItem>` | Submitted field values. |

## FormContactConfirmationEvent : MobizonWebhookEvent

Adds `Data` of type **FormContactConfirmation** (FR-008).

### FormContactConfirmation

| Property | JSON | Type | Notes |
|----------|------|------|-------|
| `FormId` | `formId` | `long` | |
| `SubmissionId` | `submissionId` | `long` | |
| `Item` | `item` | `WebhookFieldItem` | The confirmed field; `ConfirmationTs` populated. |

## FormContactUnsubscribeEvent : MobizonWebhookEvent

Adds `Data` of type **FormContactUnsubscribe** (FR-009).

### FormContactUnsubscribe

| Property | JSON | Type | Notes |
|----------|------|------|-------|
| `FormId` | `formId` | `long` | |
| `UnsubscribeTs` | `unsubscribeTs` | `DateTimeOffset?` | |
| `Items` | `items` | `IReadOnlyList<WebhookFieldItem>` | Affected fields. |

## WebhookFieldItem (shared by form events)

| Property | JSON | Type | Notes |
|----------|------|------|-------|
| `SubmissionId` | `submissionId` | `long?` | Present in unsubscribe items; null elsewhere. |
| `SubmissionDataId` | `submissionDataId` | `long` | |
| `FieldId` | `fieldId` | `long` | |
| `FieldType` | `fieldType` | `string` | e.g. `TEXT_STRING`, `EMAIL`, `MOBILE` — kept as string for forward-compat. |
| `FieldName` | `fieldName` | `string` | |
| `Value` | `value` | `string` | User-entered value. |
| `ConfirmationRequired` | `confirmationRequired` | `bool?` | `1`⇒true (submission items). |
| `ConfirmationTs` | `confirmationTs` | `DateTimeOffset?` | Empty string ⇒ null. |

## UnknownWebhookEvent : MobizonWebhookEvent

Produced when `EventType == Unknown` (FR-011, US3).

| Property | JSON | Type | Notes |
|----------|------|------|-------|
| `RawData` | `data` | `JsonElement` | Unparsed event data; envelope still populated and verifiable. |

## Result types (in Mobizon.Contracts.Models.Webhooks)

> Placed in Contracts (not the Webhooks impl package) so `IWebhookProcessor` in `Mobizon.Contracts.Services` can reference them without inverting the dependency direction.

### WebhookProcessStatus (enum)
`Ok` · `SignatureMismatch` · `ParseError`

### WebhookProcessResult
| Property | Type | Notes |
|----------|------|-------|
| `Status` | `WebhookProcessStatus` | Drives HTTP response mapping. |
| `Event` | `MobizonWebhookEvent?` | The fully parsed event whenever the body parsed successfully — i.e. non-null for **both** `Ok` and `SignatureMismatch`; null **only** for `ParseError`. (Consumers must check `Status`/`IsAuthentic` before trusting it.) |
| `IsAuthentic` | `bool` | `Status == Ok`. |

## Exception (in Mobizon.Contracts.Exceptions)

### WebhookParseException : Exception
Thrown by `IWebhookParser.Parse` on malformed JSON or a missing required envelope field. Distinct from signature mismatch (which is a status, not an exception). Carries the offending field/reason where known.

## Relationships

```
MobizonWebhookEvent (abstract)
├── SmsDeliveryReportEvent      ──has──> SmsDeliveryReport ──uses──> SmsStatus (Messages)
├── FormSubmissionEvent         ──has──> FormSubmission        ──has many──> WebhookFieldItem
├── FormContactConfirmationEvent──has──> FormContactConfirmation ──has──> WebhookFieldItem
├── FormContactUnsubscribeEvent ──has──> FormContactUnsubscribe ──has many──> WebhookFieldItem
└── UnknownWebhookEvent         ──has──> JsonElement (raw)
```
