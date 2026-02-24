# Feature Specification: Mobizon.Net SDK

**Feature Branch**: `001-mobizon-net-sdk`
**Created**: 2026-02-24
**Status**: Draft
**Input**: Full .NET SDK for Mobizon SMS gateway REST API (v1)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Send a Single SMS (Priority: P1)

A .NET developer integrates Mobizon.Net into their application
to send a single SMS message to a phone number and check
delivery status. This is the most common use case — transactional
SMS (OTP, notifications, alerts).

**Why this priority**: Sending SMS is the primary reason to use
a Mobizon SDK. Without this, the library has no value.

**Independent Test**: Can be fully tested by creating a
`MobizonClient`, calling `Messages.SendSmsMessageAsync()`,
then calling `Messages.GetSmsStatusAsync()` with the returned
message ID. Delivers immediate value for any SMS use case.

**Acceptance Scenarios**:

1. **Given** a configured `IMobizonClient` with valid API key
   and regional URL, **When** the developer calls
   `SendSmsMessageAsync` with a recipient and text,
   **Then** the SDK returns a `MobizonResponse<SendSmsResult>`
   with `Code = Success`, containing `CampaignId`, `MessageId`,
   and `Status`.

2. **Given** a sent message ID, **When** the developer calls
   `GetSmsStatusAsync` with that ID, **Then** the SDK returns
   a `MobizonResponse<IReadOnlyList<SmsStatusResult>>` with
   delivery status information.

3. **Given** an invalid API key, **When** the developer calls
   any method, **Then** the SDK throws a `MobizonApiException`
   with the error code and human-readable message from the API.

4. **Given** a network timeout, **When** the developer calls
   any method, **Then** the SDK throws a
   `MobizonException` wrapping the `HttpRequestException`.

---

### User Story 2 - Manage Campaigns (Priority: P2)

A .NET developer creates and manages bulk SMS campaigns:
creating a campaign, adding recipients, sending it, and
monitoring delivery statistics.

**Why this priority**: Campaigns are the second-most-used
feature after single SMS. Businesses sending marketing or
bulk notifications rely on this workflow.

**Independent Test**: Can be tested by creating a campaign,
adding recipients, triggering send, and checking campaign info.
Delivers value for marketing/bulk SMS use cases.

**Acceptance Scenarios**:

1. **Given** a configured client, **When** the developer calls
   `Campaigns.CreateAsync` with type, sender name, and text,
   **Then** the SDK returns a campaign ID.

2. **Given** a campaign ID, **When** the developer calls
   `Campaigns.AddRecipientsAsync`, **Then** recipients are
   added and the SDK confirms the operation.

3. **Given** a campaign with recipients, **When** the developer
   calls `Campaigns.SendAsync`, **Then** the campaign starts
   sending (may return `BackgroundTask` code with task ID).

4. **Given** a campaign ID, **When** the developer calls
   `Campaigns.GetInfoAsync`, **Then** the SDK returns campaign
   statistics (sent, delivered, failed counts).

---

### User Story 3 - Check Balance (Priority: P3)

A .NET developer checks their Mobizon account balance
programmatically to monitor spending or prevent sending
when funds are low.

**Why this priority**: Balance checking is simple but
essential for operational monitoring. It is also the simplest
API call, useful for validating SDK connectivity.

**Independent Test**: Can be tested by calling
`User.GetOwnBalanceAsync()` and verifying the returned
balance and currency values.

**Acceptance Scenarios**:

1. **Given** a configured client, **When** the developer
   calls `User.GetOwnBalanceAsync()`, **Then** the SDK
   returns a `MobizonResponse<BalanceResult>` with
   `Balance` (decimal) and `Currency` (string).

---

### User Story 4 - Manage Short Links (Priority: P4)

A .NET developer creates, updates, and tracks short links
used in SMS messages for click tracking and analytics.

**Why this priority**: Link tracking is a value-add feature
used by marketing teams. Lower priority than core SMS.

**Independent Test**: Can be tested by creating a short link,
retrieving it, checking stats, and updating/deleting it.

**Acceptance Scenarios**:

1. **Given** a configured client, **When** the developer
   calls `Links.CreateAsync` with a full URL, **Then** the
   SDK returns the created link with its short code.

2. **Given** link IDs, **When** the developer calls
   `Links.GetStatsAsync` with a date range, **Then** the
   SDK returns click statistics.

---

### User Story 5 - DI and Resilience Integration (Priority: P5)

A .NET developer registers Mobizon.Net in their ASP.NET Core
DI container with resilience policies (retry, circuit breaker)
using a single `AddMobizon()` call.

**Why this priority**: DI integration is critical for
enterprise adoption but depends on the core SDK being
complete first.

**Independent Test**: Can be tested by configuring DI with
`AddMobizon()`, resolving `IMobizonClient`, and verifying
that resilience policies are applied via mock HTTP handler.

**Acceptance Scenarios**:

1. **Given** an ASP.NET Core application, **When** the
   developer calls `services.AddMobizon(options => ...)`
   in `ConfigureServices`, **Then** `IMobizonClient` is
   resolvable from the service provider with correct options.

2. **Given** DI registration with Polly extension, **When**
   a transient HTTP failure occurs, **Then** the SDK
   automatically retries up to 3 times with exponential backoff.

---

### User Story 6 - Monitor Background Tasks (Priority: P6)

A .NET developer polls the status of a long-running task
(e.g., campaign send) using the TaskQueue module.

**Why this priority**: Background tasks are a supporting
feature needed only for campaign sending workflows.

**Independent Test**: Can be tested by calling
`TaskQueue.GetStatusAsync(taskId)` and verifying progress.

**Acceptance Scenarios**:

1. **Given** a task ID returned from a campaign send,
   **When** the developer calls `TaskQueue.GetStatusAsync`,
   **Then** the SDK returns task progress information.

---

### Edge Cases

- What happens when the API returns an unknown response code
  not mapped in the enum? SDK MUST preserve the raw integer
  code and include it in the exception message.
- What happens when the API returns an empty `data` field
  on success? SDK MUST handle `null`/empty data gracefully
  and return `default(T)` or an empty collection.
- What happens when the consumer passes `CancellationToken`
  that is already cancelled? SDK MUST throw
  `OperationCanceledException` immediately without making
  an HTTP call.
- What happens when the API URL is misconfigured (wrong
  domain)? SDK MUST propagate the `HttpRequestException`
  wrapped in `MobizonException`.
- What happens when pagination parameters are out of range?
  SDK MUST pass them through to the API and return whatever
  the API responds (empty list or error).
- What happens when `ApiKey` is null or empty during client
  construction? SDK MUST throw `ArgumentException`
  immediately at configuration time.

## Requirements *(mandatory)*

### Functional Requirements

#### Core Infrastructure

- **FR-001**: SDK MUST provide `MobizonResponse<T>` as the
  universal response wrapper with `Code` (enum), `Data` (T),
  and `Message` (string) properties.
- **FR-002**: SDK MUST map API response codes to
  `MobizonResponseCode` enum: `Success = 0`,
  `BackgroundTask = 100`, and error codes. Unknown integer
  codes not mapped in the enum MUST be preserved as the raw
  integer value in `MobizonApiException` and its message.
- **FR-003**: SDK MUST throw `MobizonApiException` (with
  `Code` and `Message`) when the API returns a non-success
  response code.
- **FR-004**: SDK MUST throw `MobizonException` (base class)
  for transport-level errors (network, timeout, serialization).
- **FR-005**: SDK MUST serialize nested parameters using
  bracket notation (`data[field]=value`) as
  `application/x-www-form-urlencoded` POST body.
- **FR-006**: SDK MUST append `output=json&api=v1&apiKey={key}`
  as query string parameters to every request URL.
- **FR-007**: SDK MUST construct request URLs as
  `{baseUrl}/service/{module}/{method}`.
- **FR-008**: SDK MUST accept `CancellationToken` on every
  public async method and forward it to `HttpClient`.
- **FR-009**: SDK MUST validate `MobizonClientOptions` at
  construction time: `ApiKey` MUST NOT be null/empty,
  `ApiUrl` MUST NOT be null/empty.

#### Message Module

- **FR-010**: `IMessageService.SendSmsMessageAsync` MUST accept
  `SendSmsMessageRequest` (recipient, text, from, validity)
  and return `MobizonResponse<SendSmsResult>`.
- **FR-011**: `IMessageService.GetSmsStatusAsync` MUST accept
  an array of message IDs and return
  `MobizonResponse<IReadOnlyList<SmsStatusResult>>`.
- **FR-012**: `IMessageService.ListAsync` MUST accept
  `MessageListRequest` (criteria, pagination, sort) and return
  `MobizonResponse<PaginatedResponse<MessageInfo>>`.

#### Campaign Module

- **FR-013**: `ICampaignService.CreateAsync` MUST accept
  `CreateCampaignRequest` and return campaign ID.
- **FR-014**: `ICampaignService.DeleteAsync` MUST accept
  campaign ID and return success confirmation.
- **FR-015**: `ICampaignService.GetAsync` MUST accept
  campaign ID and return campaign data.
- **FR-016**: `ICampaignService.GetInfoAsync` MUST accept
  campaign ID and return campaign statistics.
- **FR-017**: `ICampaignService.ListAsync` MUST accept
  criteria/pagination/sort and return paginated list.
- **FR-018**: `ICampaignService.SendAsync` MUST accept
  campaign ID and trigger sending. MUST handle
  `BackgroundTask` response code by returning the task ID.
- **FR-019**: `ICampaignService.AddRecipientsAsync` MUST
  accept campaign ID, recipient type, and data array.

#### Link Module

- **FR-020**: `ILinkService.CreateAsync` MUST accept
  `CreateLinkRequest` and return the created link.
- **FR-021**: `ILinkService.DeleteAsync` MUST accept an
  array of link IDs.
- **FR-022**: `ILinkService.GetAsync` MUST accept a short
  code and return link data.
- **FR-023**: `ILinkService.GetLinksAsync` MUST accept a
  campaign ID and return associated links.
- **FR-024**: `ILinkService.GetStatsAsync` MUST accept
  link IDs, stat type (daily/monthly), and date range.
- **FR-025**: `ILinkService.ListAsync` MUST accept
  criteria/pagination/sort and return paginated list.
- **FR-026**: `ILinkService.UpdateAsync` MUST accept a
  short code and updated link data.

#### User Module

- **FR-027**: `IUserService.GetOwnBalanceAsync` MUST return
  `MobizonResponse<BalanceResult>` with balance (decimal)
  and currency (string). This MUST use HTTP GET (not POST).
  The API returns balance as a JSON string (e.g., `"4043.0656"`);
  the SDK MUST parse it to `decimal` using invariant culture.

#### TaskQueue Module

- **FR-028**: `ITaskQueueService.GetStatusAsync` MUST accept
  a task ID and return task progress information.

#### DI Package

- **FR-029**: DI package MUST provide `AddMobizon` extension
  method on `IServiceCollection` accepting
  `Action<MobizonClientOptions>`.
- **FR-030**: DI package MUST provide `AddMobizon` overload
  accepting `IConfiguration` for binding from config sections.
- **FR-031**: DI package MUST register `HttpClient` via
  `IHttpClientFactory` with a named client.
- **FR-032**: DI package MUST register `IMobizonClient` as
  a scoped service. `HttpClient` lifetime is managed by
  `IHttpClientFactory`; the client itself MUST NOT be
  singleton to avoid captured `HttpClient` staleness.

#### Polly Package

- **FR-033**: Polly package MUST provide
  `AddMobizonResilience` extension on `IHttpClientBuilder`.
- **FR-034**: Default retry policy: 3 attempts, exponential
  backoff (2s, 4s, 8s), triggered on transient HTTP errors.
- **FR-035**: Default circuit breaker: opens after 5
  consecutive failures, breaks for 30 seconds.
- **FR-036**: Default timeout: 30 seconds per request.
- **FR-037**: All default policies MUST be overridable via
  configuration parameters.

> **Timeout interaction**: `MobizonClientOptions.Timeout` sets
> `HttpClient.Timeout` (applies always). When Polly extension is
> used, the Polly timeout policy wraps the HTTP call and takes
> precedence. Consumers should configure timeout in one place —
> either `MobizonClientOptions` (without Polly) or
> `MobizonResilienceOptions` (with Polly).

#### Packaging

- **FR-038**: All packages MUST target `netstandard2.0`.
- **FR-039**: `Mobizon.Contracts` MUST have zero dependencies
  on `Mobizon.Net` (contracts-only package).
- **FR-040**: `Mobizon.Net` MUST depend only on
  `Mobizon.Contracts` and `System.Text.Json`.
- **FR-041**: SDK MUST include XML documentation on all
  public types and members.

### Key Entities

- **MobizonClientOptions**: Configuration object holding
  `ApiKey`, `ApiUrl`, `ApiVersion`, and `Timeout`. Used to
  configure the client at construction time.
- **MobizonResponse\<T\>**: Universal API response wrapper.
  Holds `Code` (response code enum), `Data` (typed payload),
  and `Message` (API message string).
- **MobizonResponseCode**: Enum mapping API integer codes to
  named values (`Success`, `BackgroundTask`, error codes).
- **IMobizonClient**: Primary entry point for consumers.
  Exposes sub-services as properties: `Messages`, `Campaigns`,
  `Links`, `User`, `TaskQueue`.
- **SendSmsResult**: Result of sending a single SMS. Contains
  `CampaignId`, `MessageId`, `Status`.
- **SmsStatusResult**: Delivery status for a single message.
  Contains `Id`, `Status`, `SegNum`, `StartSendTs`.
- **BalanceResult**: Account balance. Contains `Balance`
  (decimal) and `Currency` (string, e.g., "KZT").
- **PaginatedResponse\<T\>**: Wrapper for list endpoints.
  Contains `Items` (collection of T) and pagination metadata.
- **PaginationRequest**: Input model for pagination:
  `CurrentPage`, `PageSize`.
- **SortRequest**: Input model for sorting: `Field`, `Direction`
  (ASC/DESC).

## Public API Reference

### Package: Mobizon.Contracts

#### Configuration

```
MobizonClientOptions
├── ApiKey       : string (required)
├── ApiUrl       : string (required, e.g., "https://api.mobizon.kz")
├── ApiVersion   : string (default: "v1")
└── Timeout      : TimeSpan (default: 30s)
```

#### Response Types

```
MobizonResponse<T>
├── Code    : MobizonResponseCode
├── Data    : T
└── Message : string

MobizonResponseCode : enum
├── Success         = 0
├── BackgroundTask  = 100
├── InvalidData     = 1
├── AuthFailed      = 2
├── NotFound        = 3
├── AccessDenied    = 4
├── InternalError   = 5
└── (additional codes as discovered)

PaginatedResponse<T>
├── Items       : IReadOnlyList<T>
├── TotalCount  : int
├── CurrentPage : int
└── PageSize    : int
```

#### Exception Hierarchy

```
MobizonException : Exception
└── MobizonApiException : MobizonException
    ├── Code    : MobizonResponseCode
    └── ApiMessage : string
```

#### Pagination & Sorting

```
PaginationRequest
├── CurrentPage : int (default: 0)
└── PageSize    : int (default: 20)

SortRequest
├── Field     : string
└── Direction : SortDirection (ASC | DESC)
```

#### IMobizonClient Interface

```
IMobizonClient
├── Messages  : IMessageService
├── Campaigns : ICampaignService
├── Links     : ILinkService
├── User      : IUserService
└── TaskQueue : ITaskQueueService
```

#### IMessageService

| SDK Method | HTTP Endpoint | Request DTO | Response DTO |
|------------|---------------|-------------|--------------|
| `SendSmsMessageAsync(SendSmsMessageRequest, CT)` | `POST /service/message/sendsmsmessage` | `SendSmsMessageRequest` | `MobizonResponse<SendSmsResult>` |
| `GetSmsStatusAsync(int[], CT)` | `POST /service/message/getsmsstatus` | `ids` (int[]) | `MobizonResponse<IReadOnlyList<SmsStatusResult>>` |
| `ListAsync(MessageListRequest, CT)` | `POST /service/message/list` | `MessageListRequest` | `MobizonResponse<PaginatedResponse<MessageInfo>>` |

**Request/Response DTOs:**

```
SendSmsMessageRequest
├── Recipient : string (international format, e.g., "77001234567")
├── Text      : string (message body)
├── From      : string? (optional alphaname/sender ID)
└── Validity  : int? (optional, minutes)

API mapping:
  recipient          → recipient
  text               → text
  from               → from
  params[validity]   → Validity

SendSmsResult
├── CampaignId : int
├── MessageId  : int
└── Status     : int

SmsStatusResult
├── Id         : int
├── Status     : int
├── SegNum     : int
└── StartSendTs: string

MessageListRequest
├── Criteria   : MessageListCriteria?
├── Pagination : PaginationRequest?
└── Sort       : SortRequest?

MessageListCriteria
├── From   : string?
└── Status : int?

MessageInfo
├── Id         : int
├── CampaignId : int
├── From       : string
├── Status     : int
└── Text       : string
Note: Only listed fields are implemented. Unknown JSON
properties are ignored via [JsonExtensionData] or
JsonSerializerOptions.IgnoreUnknownProperties.
```

#### ICampaignService

| SDK Method | HTTP Endpoint | Request DTO | Response DTO |
|------------|---------------|-------------|--------------|
| `CreateAsync(CreateCampaignRequest, CT)` | `POST /service/campaign/create` | `CreateCampaignRequest` | `MobizonResponse<CreateCampaignResult>` |
| `DeleteAsync(int, CT)` | `POST /service/campaign/delete` | `id` (int) | `MobizonResponse<object>` |
| `GetAsync(int, CT)` | `POST /service/campaign/get` | `id` (int) | `MobizonResponse<CampaignData>` |
| `GetInfoAsync(int, CT)` | `POST /service/campaign/getinfo` | `id` (int) | `MobizonResponse<CampaignInfo>` |
| `ListAsync(CampaignListRequest, CT)` | `POST /service/campaign/list` | `CampaignListRequest` | `MobizonResponse<PaginatedResponse<CampaignData>>` |
| `SendAsync(int, CT)` | `POST /service/campaign/send` | `id` (int) | `MobizonResponse<CampaignSendResult>` |
| `AddRecipientsAsync(AddRecipientsRequest, CT)` | `POST /service/campaign/addrecipients` | `AddRecipientsRequest` | `MobizonResponse<object>` |

**Request/Response DTOs:**

```
CreateCampaignRequest
├── Type : int (campaign type)
├── From : string (sender name)
└── Text : string (message text)

API mapping:
  data[type] → Type
  data[from] → From
  data[text] → Text

CreateCampaignResult
└── CampaignId : int

CampaignData
├── Id     : int
├── Type   : int
├── From   : string
├── Text   : string
└── Status : int
Note: Only listed fields are implemented.

CampaignInfo
├── Id            : int
├── TotalMessages : int
├── Sent          : int
├── Delivered     : int
└── Failed        : int
Note: Only listed fields are implemented.

CampaignSendResult
├── TaskId : int? (when Code = BackgroundTask)
└── Status : int

CampaignListRequest
├── Criteria   : CampaignListCriteria?
├── Pagination : PaginationRequest?
└── Sort       : SortRequest?

AddRecipientsRequest
├── CampaignId : int
├── Type       : int
└── Data       : IReadOnlyList<string> (phone numbers)

API mapping:
  campaignId → CampaignId
  type       → Type
  data[]     → Data items
```

#### ILinkService

| SDK Method | HTTP Endpoint | Request DTO | Response DTO |
|------------|---------------|-------------|--------------|
| `CreateAsync(CreateLinkRequest, CT)` | `POST /service/link/create` | `CreateLinkRequest` | `MobizonResponse<LinkData>` |
| `DeleteAsync(int[], CT)` | `POST /service/link/delete` | `ids` (int[]) | `MobizonResponse<object>` |
| `GetAsync(string, CT)` | `POST /service/link/get` | `code` (string) | `MobizonResponse<LinkData>` |
| `GetLinksAsync(int, CT)` | `POST /service/link/getlinks` | `campaignId` (int) | `MobizonResponse<IReadOnlyList<LinkData>>` |
| `GetStatsAsync(GetLinkStatsRequest, CT)` | `POST /service/link/getstats` | `GetLinkStatsRequest` | `MobizonResponse<IReadOnlyList<LinkStatsResult>>` |
| `ListAsync(LinkListRequest, CT)` | `POST /service/link/list` | `LinkListRequest` | `MobizonResponse<PaginatedResponse<LinkData>>` |
| `UpdateAsync(UpdateLinkRequest, CT)` | `POST /service/link/update` | `UpdateLinkRequest` | `MobizonResponse<object>` |

**Request/Response DTOs:**

```
CreateLinkRequest
├── FullLink       : string (target URL)
├── Status         : int?
├── ExpirationDate : string? (date string)
└── Comment        : string?

API mapping:
  data[fullLink]       → FullLink
  data[status]         → Status
  data[expirationDate] → ExpirationDate
  data[comment]        → Comment

UpdateLinkRequest
├── Code           : string (short link code)
├── FullLink       : string?
├── Status         : int?
├── ExpirationDate : string?
└── Comment        : string?

API mapping:
  code                 → Code
  data[fullLink]       → FullLink
  data[status]         → Status
  data[expirationDate] → ExpirationDate
  data[comment]        → Comment

LinkData
├── Id             : int
├── Code           : string (short code)
├── FullLink       : string
├── Status         : int
├── ExpirationDate : string?
├── Comment        : string?
└── Clicks         : int
Note: Only listed fields are implemented.

GetLinkStatsRequest
├── Ids      : int[]
├── Type     : LinkStatsType (Daily | Monthly)
├── DateFrom : string?
└── DateTo   : string?

API mapping:
  ids[]             → Ids
  type              → Type ("daily" | "monthly")
  criteria[dateFrom]→ DateFrom
  criteria[dateTo]  → DateTo

LinkStatsType : enum
├── Daily
└── Monthly

LinkStatsResult
├── LinkId : int
├── Date   : string
└── Clicks : int
Note: Only listed fields are implemented.

LinkListRequest
├── Criteria   : LinkListCriteria?
├── Pagination : PaginationRequest?
└── Sort       : SortRequest?
```

#### IUserService

| SDK Method | HTTP Endpoint | Request DTO | Response DTO |
|------------|---------------|-------------|--------------|
| `GetOwnBalanceAsync(CT)` | `GET /service/user/getownbalance` | (none) | `MobizonResponse<BalanceResult>` |

**Response DTO:**

```
BalanceResult
├── Balance  : decimal (e.g., 4043.0656)
└── Currency : string (e.g., "KZT")
```

Note: This is the only endpoint using HTTP GET. All others
use POST.

#### ITaskQueueService

| SDK Method | HTTP Endpoint | Request DTO | Response DTO |
|------------|---------------|-------------|--------------|
| `GetStatusAsync(int, CT)` | `POST /service/taskqueue/getstatus` | `id` (int) | `MobizonResponse<TaskQueueStatus>` |

**Response DTO:**

```
TaskQueueStatus
├── Id       : int
├── Status   : int
└── Progress : int (percentage 0-100)
Note: Only listed fields are implemented.
```

### Package: Mobizon.Net (Implementation)

#### Internal Architecture

```
MobizonClient : IMobizonClient
├── Messages  → MessageService
├── Campaigns → CampaignService
├── Links     → LinkService
├── User      → UserService
└── TaskQueue → TaskQueueService

MobizonApiClient (internal)
├── SendAsync<T>(HttpMethod, string module, string method,
│     IDictionary<string,string>? parameters, CT)
│     → MobizonResponse<T>
├── BuildRequestUrl(string module, string method) → Uri
├── SerializeFormData(object request)
│     → FormUrlEncodedContent (bracket notation)
└── DeserializeResponse<T>(HttpResponseMessage)
│     → MobizonResponse<T>

BracketNotationSerializer (internal)
└── Serialize(object obj, string? prefix)
      → IDictionary<string, string>
      Converts nested objects to flat key-value pairs:
        { Data: { From: "Alpha" } }
        → { "data[from]": "Alpha" }
```

Each service (e.g., `MessageService`) receives
`MobizonApiClient` via constructor injection and delegates
HTTP communication to it.

### Package: Mobizon.Net.Extensions.DependencyInjection

```
ServiceCollectionExtensions
├── AddMobizon(this IServiceCollection,
│     Action<MobizonClientOptions>) → IHttpClientBuilder
└── AddMobizon(this IServiceCollection,
      IConfiguration) → IHttpClientBuilder

Registration behavior:
  - Binds MobizonClientOptions from configuration/action
  - Registers named HttpClient via AddHttpClient
  - Registers IMobizonClient → MobizonClient (scoped, per FR-032)
  - Returns IHttpClientBuilder for chaining (Polly, etc.)
```

### Package: Mobizon.Net.Extensions.Polly

```
MobizonHttpClientBuilderExtensions
└── AddMobizonResilience(this IHttpClientBuilder,
      Action<MobizonResilienceOptions>? configure = null)
      → IHttpClientBuilder

MobizonResilienceOptions
├── RetryCount          : int (default: 3)
├── RetryBaseDelay      : TimeSpan (default: 2s)
├── CircuitBreakerCount : int (default: 5)
├── CircuitBreakerDuration : TimeSpan (default: 30s)
└── Timeout             : TimeSpan (default: 30s)

Default policies applied:
  1. Retry: exponential backoff (2s, 4s, 8s) on transient errors
  2. Circuit Breaker: opens after 5 failures, 30s break
  3. Timeout: 30s per request
```

## Usage Examples

### Send SMS (minimal)

```csharp
var options = new MobizonClientOptions
{
    ApiKey = "your-api-key",
    ApiUrl = "https://api.mobizon.kz"
};

using var client = new MobizonClient(options);

var result = await client.Messages.SendSmsMessageAsync(
    new SendSmsMessageRequest
    {
        Recipient = "77001234567",
        Text = "Hello from Mobizon.Net!"
    });

Console.WriteLine($"Message ID: {result.Data.MessageId}");
```

### Check SMS Status

```csharp
var status = await client.Messages.GetSmsStatusAsync(
    new[] { messageId });

foreach (var item in status.Data)
    Console.WriteLine($"ID={item.Id}, Status={item.Status}");
```

### Campaign Workflow

```csharp
var campaign = await client.Campaigns.CreateAsync(
    new CreateCampaignRequest
    {
        Type = 1,
        From = "MyBrand",
        Text = "Sale starts today!"
    });

await client.Campaigns.AddRecipientsAsync(
    new AddRecipientsRequest
    {
        CampaignId = campaign.Data.CampaignId,
        Type = 1,
        Data = new[] { "77001111111", "77002222222" }
    });

var sendResult = await client.Campaigns.SendAsync(
    campaign.Data.CampaignId);
```

### Check Balance

```csharp
var balance = await client.User.GetOwnBalanceAsync();
Console.WriteLine($"{balance.Data.Balance} {balance.Data.Currency}");
```

### DI + Polly Integration

```csharp
services.AddMobizon(options =>
{
    options.ApiKey = configuration["Mobizon:ApiKey"];
    options.ApiUrl = configuration["Mobizon:ApiUrl"];
})
.AddMobizonResilience();
```

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can send an SMS and receive a typed
  response in 5 or fewer lines of code (excluding
  configuration).
- **SC-002**: All 5 API modules (Message, Campaign, Link,
  User, TaskQueue) are fully operational with 100% method
  coverage — no API method left unimplemented.
- **SC-003**: 90% or more of the core SDK logic is covered
  by automated tests that run without network access.
- **SC-004**: A new developer can integrate the SDK into an
  existing project and send their first SMS within 10 minutes
  using only the README.
- **SC-005**: The SDK handles all API error codes gracefully,
  providing typed exceptions that include the error code and
  message — zero unhandled error scenarios.
- **SC-006**: The SDK works correctly on both legacy
  (.NET Framework 4.6.1+) and modern (.NET 6/8+) runtimes
  without conditional compilation or separate packages.
- **SC-007**: The NuGet package has zero third-party
  dependencies beyond `System.Text.Json` (core package).
