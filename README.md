# Mobizon.Net

A .NET SDK for the [Mobizon](https://mobizon.kz) SMS gateway REST API (v1). Provides a strongly-typed,
async-first client for sending SMS messages, managing bulk campaigns, tracking short links, checking
account balance, monitoring background tasks, managing contact groups and cards, maintaining a
number stop-list, and querying registered sender IDs — all from a single `IMobizonClient` interface.

[![NuGet](https://img.shields.io/nuget/v/Mobizon.Net.svg)](https://www.nuget.org/packages/Mobizon.Net)
[![NuGet](https://img.shields.io/nuget/v/Mobizon.Net.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/Mobizon.Net.Extensions.DependencyInjection)
[![NuGet](https://img.shields.io/nuget/v/Mobizon.Net.Extensions.Polly.svg)](https://www.nuget.org/packages/Mobizon.Net.Extensions.Polly)
[![NuGet](https://img.shields.io/nuget/v/Mobizon.Contracts.svg)](https://www.nuget.org/packages/Mobizon.Contracts)

---

## Features

- **9 API modules** — Messages, Campaigns, Links, User, TaskQueue, ContactGroups, ContactCards,
  NumberStopList, and Alphanames — with full method coverage
- **ContactCards LINQ-style query API** via `IContactCardQuery` for composable server-side filtering
- **Inbound webhooks** — verify signatures and parse delivery-report and form events, with optional ASP.NET Core helpers
- **Strongly-typed requests and responses** via `MobizonResponse<T>` with typed exceptions
- **ASP.NET Core DI integration** — register with a single `AddMobizon()` call
- **Polly resilience** — retry and circuit breaker policies out of the box
- **`netstandard2.0` target** — compatible with .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5+
- **`System.Text.Json` serialisation** — no other runtime dependencies in the core packages
- **`CancellationToken` support** on every public async method

---

## Installation

Install only the packages you need.

### Core SDK (no DI, no resilience)

```bash
dotnet add package Mobizon.Net
```

The primary package. Provides `MobizonClient` and all API modules. Depends only on
`Mobizon.Contracts` and `System.Text.Json`.

### Contracts only (for library authors)

```bash
dotnet add package Mobizon.Contracts
```

Interfaces, request/response DTOs, and exception types. References `System.Text.Json`. Reference
this package when you want to accept `IMobizonClient` without depending on the implementation.

### DI integration (ASP.NET Core)

```bash
dotnet add package Mobizon.Net.Extensions.DependencyInjection
```

Adds `AddMobizon()` extension methods on `IServiceCollection` and wires up `IHttpClientFactory`.

### Polly resilience

```bash
dotnet add package Mobizon.Net.Extensions.Polly
```

Adds `AddMobizonResilience()` on `IHttpClientBuilder` with retry and circuit breaker policies.
Requires the DI package.

### Webhooks (incoming events)

```bash
dotnet add package Mobizon.Net.Webhooks            # framework-agnostic verify + parse
dotnet add package Mobizon.Net.Webhooks.AspNetCore # optional ASP.NET Core helpers
```

The core webhook package depends only on `System.Text.Json`; the ASP.NET Core package adds endpoint
and DI helpers. See the [Webhooks](#webhooks) section below.

---

## Quick Start

Send an SMS in five lines (excluding configuration):

```csharp
using Mobizon.Contracts;
using Mobizon.Net;

var options = new MobizonClientOptions
{
    ApiKey = "your-api-key-here",
    ApiUrl = "https://api.mobizon.kz"
};

using var client = new MobizonClient(new HttpClient(), options);

var result = await client.Messages.SendSmsMessageAsync(
    new SendSmsMessageRequest
    {
        Recipient = "77001234567",
        Text = "Hello from Mobizon.Net!"
    });

Console.WriteLine($"Message ID: {result.Data.MessageId}");
```

---

## API Modules

`IMobizonClient` exposes nine sub-services as properties:

```
IMobizonClient
├── .Messages        — IMessageService
├── .Campaigns       — ICampaignService
├── .Links           — ILinkService
├── .User            — IUserService
├── .TaskQueue       — ITaskQueueService
├── .ContactGroups   — IContactGroupService
├── .ContactCards    — IContactCardSet  (also implements IContactCardQuery)
├── .NumberStopList  — INumberStopListService
└── .Alphanames      — IAlphanameService
```

### Messages

Send individual SMS messages and query delivery status.

**Send an SMS with optional parameters:**

```csharp
var result = await client.Messages.SendSmsMessageAsync(
    new SendSmsMessageRequest
    {
        Recipient  = "77001234567",
        Text       = "Your verification code: 8421",
        From       = "MyBrand",   // optional sender name / alphaname
        Parameters = new SmsMessageParameters
        {
            Validity = TimeSpan.FromMinutes(60)   // optional delivery window
        }
    });

Console.WriteLine($"Campaign: {result.Data.CampaignId}");
Console.WriteLine($"Message:  {result.Data.MessageId}");
Console.WriteLine($"Status:   {result.Data.Status}");
```

**Check delivery status:**

```csharp
var status = await client.Messages.GetSmsStatusAsync(
    new[] { result.Data.MessageId });

foreach (var item in status.Data)
    Console.WriteLine($"ID={item.Id}  Status={item.Status}  Segments={item.Segments}");
```

**List messages with pagination:**

```csharp
var messages = await client.Messages.ListAsync(
    new MessageListRequest
    {
        Criteria   = new MessageListCriteria { Status = SmsStatus.Delivered },
        Pagination = new PaginationRequest   { CurrentPage = 0, PageSize = 50 },
        Sort       = new SortRequest         { Field = "id", Direction = SortDirection.DESC }
    });

Console.WriteLine($"Total: {messages.Data.TotalItemCount}");
foreach (var msg in messages.Data.Items)
    Console.WriteLine($"[{msg.Id}] {msg.Text}");
```

---

### Campaigns

Create and send bulk SMS campaigns, then retrieve delivery statistics.

```csharp
// 1. Create campaign
var campaign = await client.Campaigns.CreateAsync(
    new CreateCampaignRequest
    {
        Type = CampaignType.Bulk,
        From = "MyBrand",
        Text = "Flash sale — 50% off today!"
    });

int campaignId = campaign.Data;   // Data is the integer campaign ID

// 2. Add recipients
await client.Campaigns.AddRecipientsAsync(
    new AddRecipientsRequest
    {
        CampaignId = campaignId,
        Recipients = new[]
        {
            new RecipientEntry { Recipient = "77001111111" },
            new RecipientEntry { Recipient = "77002222222" }
        }
    });

// 3. Send
var sendResult = await client.Campaigns.SendAsync(campaignId);

// 4. If the API queued a background task, poll for completion
if (sendResult.Code == MobizonResponseCode.BackgroundTask)
{
    int taskId = sendResult.Data;   // Data is the integer task ID
    var taskStatus = await client.TaskQueue.GetStatusAsync(taskId);
    Console.WriteLine($"Progress: {taskStatus.Data.Progress}%");
}

// 5. Get delivery statistics
var info = await client.Campaigns.GetInfoAsync(campaignId);
Console.WriteLine($"Delivered: {info.Data.Counters?.TotalDelivrdMsgNum}");
```

Other available methods: `GetAsync`, `ListAsync`, `DeleteAsync`.

---

### Links

Create and manage short links for click tracking in SMS messages.

**Create a short link:**

```csharp
var link = await client.Links.CreateAsync(
    new CreateLinkRequest
    {
        FullLink       = "https://example.com/promo",
        Comment        = "Summer sale campaign",
        ExpirationDate = "2026-12-31"
    });

Console.WriteLine($"Short code: {link.Data.Code}");
```

**Retrieve a link by short code, numeric ID, or full short URL:**

```csharp
var byCode      = await client.Links.GetByCodeAsync("abc123");
var byId        = await client.Links.GetByIdAsync(link.Data.Id);
var byShortLink = await client.Links.GetByShortLinkAsync("https://mbzn.co/abc123");

Console.WriteLine($"URL: {byCode.Data.FullLink}  Clicks: {byCode.Data.ClickCnt}");
```

**Get click statistics:**

```csharp
var stats = await client.Links.GetStatsAsync(
    new GetLinkStatsRequest
    {
        Ids      = new[] { link.Data.Id },
        Type     = LinkStatsType.Daily,
        DateFrom = "2026-01-01",
        DateTo   = "2026-01-31"
    });

// stats.Data is a LinkStatsResult with Items (per-period points) and Totals (aggregate)
Console.WriteLine($"Total clicks: {stats.Data.Totals}");
foreach (var entry in stats.Data.Items)
    Console.WriteLine($"{entry.Date}: {entry.Clicks} clicks");
```

**Update a link:**

```csharp
await client.Links.UpdateAsync(
    new UpdateLinkRequest
    {
        Id      = link.Data.Id,
        Comment = "Updated comment"
    });
```

**Delete links:**

```csharp
await client.Links.DeleteAsync(new[] { link.Data.Id });
```

Other available methods: `GetLinksAsync` (links by campaign ID), `ListAsync`.

---

### User

Check your Mobizon account balance.

```csharp
var balance = await client.User.GetOwnBalanceAsync();
Console.WriteLine($"{balance.Data.Balance} {balance.Data.Currency}");
// Output: 4043.0656 KZT
```

`GetOwnBalanceAsync` is the only SDK method that uses HTTP GET. All other methods use POST.

---

### TaskQueue

Poll the progress of a long-running background task (typically returned after `Campaigns.SendAsync`).

```csharp
var taskStatus = await client.TaskQueue.GetStatusAsync(taskId);
Console.WriteLine($"Task {taskStatus.Data.Id}: {taskStatus.Data.Progress}% complete");
```

`TaskQueueStatus` fields: `Id`, `Status`, `Progress` (0–100).

---

### Alphanames

List the registered sender IDs available to the account.

```csharp
var alphanames = await client.Alphanames.ListAsync();
foreach (var a in alphanames.Data.Items)
    Console.WriteLine($"{a.Alphaname?.Name}  (id={a.AlphanameId})");
```

---

## Webhooks

Mobizon can push events to your server in real time instead of you polling `GetSMSStatus`. Webhooks
are created and configured in the Mobizon **control panel** (not via the API); this SDK provides the
**receive** side: verifying the SHA1 signature and parsing the JSON body into a typed event.

Supported event types: `sms-delivery-report`, `form-submission`, `form-contact-confirmation`,
`form-contact-unsubscribe`. Unrecognised types are surfaced as `UnknownWebhookEvent` (forward-compatible).

### Framework-agnostic core

```csharp
using Mobizon.Net.Webhooks;
using Mobizon.Contracts.Models.Webhooks;

var processor = new WebhookProcessor(); // default parser + verifier

// body = raw request body; secret = the key you set when creating the webhook
WebhookProcessResult result = processor.Process(body, secret);

switch (result.Status)
{
    case WebhookProcessStatus.Ok when result.Event is SmsDeliveryReportEvent sms:
        // sms.Data.MessageId, sms.Data.Status (SmsStatus), sms.Data.To
        break;
    case WebhookProcessStatus.SignatureMismatch: /* respond 403 */ break;
    case WebhookProcessStatus.ParseError:        /* respond 400 */ break;
}
```

Per-webhook secrets (multiple webhooks on one endpoint):

```csharp
var result = processor.Process(body, evt => LookupSecret(evt.WebhookId));
```

### ASP.NET Core

```csharp
using Mobizon.Net.Webhooks.AspNetCore;

builder.Services.AddMobizonWebhooks(o =>
    o.SecretKeyResolver = (sp, evt) => builder.Configuration["Mobizon:WebhookSecret"]!);

app.MapMobizonWebhook("/webhooks/mobizon", async (evt, ct) =>
{
    switch (evt)
    {
        case SmsDeliveryReportEvent sms:  /* enqueue sms.Data */ break;
        case FormSubmissionEvent form:    /* handle form.Data */ break;
        case UnknownWebhookEvent unknown: /* log unknown.EventTypeRaw */ break;
    }
});
// 200 = handled, 403 = bad signature, 400 = malformed — returned automatically.
```

**Signature**: `SHA1(eventId|attempt|eventCreateTs|secretKey)`, compared in constant time and failing
closed. The original `eventCreateTs` string is preserved verbatim for verification.

**Acknowledge fast**: Mobizon treats a webhook as failed if no `2xx` arrives within 5 seconds and
retries up to 10 times. Verify → persist/enqueue → return `200`, then process asynchronously.
Deduplicate on `EventId` (stable across retries).

---

## DI Integration

### Register with an options action

```csharp
// Program.cs / Startup.cs
using Mobizon.Net.Extensions.DependencyInjection;

services.AddMobizon(options =>
{
    options.ApiKey = configuration["Mobizon:ApiKey"];
    options.ApiUrl = configuration["Mobizon:ApiUrl"];
});
```

### Register from a configuration section

```json
// appsettings.json
{
  "Mobizon": {
    "ApiKey": "your-api-key-here",
    "ApiUrl": "https://api.mobizon.kz"
  }
}
```

```csharp
services.AddMobizon(configuration.GetSection("Mobizon"));
```

Both overloads return `IHttpClientBuilder` for chaining additional HTTP client configuration.
`IMobizonClient` is registered as a **transient** service backed by `IHttpClientFactory`.

### Inject and use in a service

```csharp
public class NotificationService
{
    private readonly IMobizonClient _mobizon;

    public NotificationService(IMobizonClient mobizon)
    {
        _mobizon = mobizon;
    }

    public async Task SendOtpAsync(string phone, string code, CancellationToken ct)
    {
        await _mobizon.Messages.SendSmsMessageAsync(
            new SendSmsMessageRequest
            {
                Recipient = phone,
                Text      = $"Your verification code: {code}"
            }, ct);
    }
}
```

---

## Polly Resilience

Chain `AddMobizonResilience()` after `AddMobizon()` to apply retry and circuit breaker policies to
all HTTP calls. Note: `AddMobizonResilience` adds retry and circuit breaker only — there is no
separate timeout policy; use `MobizonClientOptions.Timeout` to configure the `HttpClient` timeout.

### Default policies

| Policy | Default |
|--------|---------|
| Retry | 3 attempts, exponential backoff: 1 s, 2 s, 4 s |
| Circuit breaker | Opens after 5 consecutive failures, breaks for 30 s |

```csharp
services.AddMobizon(options =>
{
    options.ApiKey = configuration["Mobizon:ApiKey"];
    options.ApiUrl = configuration["Mobizon:ApiUrl"];
})
.AddMobizonResilience(); // apply default retry + circuit breaker
```

### Customise resilience options

```csharp
services.AddMobizon(configuration.GetSection("Mobizon"))
    .AddMobizonResilience(resilience =>
    {
        resilience.RetryCount                       = 5;
        resilience.RetryBaseDelay                   = TimeSpan.FromSeconds(1);
        resilience.CircuitBreakerFailureThreshold   = 10;
        resilience.CircuitBreakerDuration           = TimeSpan.FromSeconds(60);
    });
```

---

## Regional Configuration

Mobizon operates regional API endpoints. Set `ApiUrl` to match your account region:

| Region | API URL |
|--------|---------|
| Kazakhstan | `https://api.mobizon.kz` |
| Uzbekistan | `https://api.mobizon.uz` |
| International | `https://api.mobizon.com` |

```csharp
var options = new MobizonClientOptions
{
    ApiKey = "your-api-key-here",
    ApiUrl = "https://api.mobizon.uz"  // Uzbekistan
};
```

The client automatically appends `/service/` and the API version path; do not include them in `ApiUrl`.

---

## Error Handling

The SDK uses a two-level exception hierarchy:

| Exception | When thrown |
|-----------|-------------|
| `MobizonApiException` | The API returned a non-success response code (auth failure, invalid data, not found, etc.). Exposes `Code` (`MobizonResponseCode`) and `ApiMessage`. |
| `MobizonException` | Transport-level failure: network error, timeout, or serialization problem. `MobizonApiException` derives from this type. |

```csharp
try
{
    var result = await client.Messages.SendSmsMessageAsync(request);
    Console.WriteLine($"Message ID: {result.Data.MessageId}");
}
catch (MobizonApiException ex)
{
    // API-level error — invalid parameters, bad API key, quota exceeded, etc.
    Console.WriteLine($"API Error [{ex.Code}]: {ex.ApiMessage}");
}
catch (MobizonException ex)
{
    // Transport error — network failure, timeout, response parse error
    Console.WriteLine($"SDK Error: {ex.Message}");
}
```

### Response codes

| `MobizonResponseCode` | Value | Meaning |
|-----------------------|-------|---------|
| `Success` | 0 | Operation completed successfully |
| `ValidationError` | 1 | Transmitted data contains invalid values |
| `NotFound` | 2 | Record not found or access denied by ID |
| `UnknownError` | 3 | Unknown application error |
| `InvalidModule` | 4 | Invalid `module` parameter |
| `InvalidMethod` | 5 | Invalid `method` parameter |
| `InvalidFormat` | 6 | Invalid `format` parameter |
| `LoginError` | 8 | Incorrect credentials or expired session |
| `AccessDenied` | 9 | Access to this API method is denied |
| `SaveError` | 10 | Server data save error |
| `MissingParameters` | 11 | Required parameters missing from the request |
| `InvalidParameter` | 12 | An input parameter violates constraints |
| `WrongServer` | 13 | Wrong regional API server |
| `AccountBlocked` | 14 | User account is blocked or deleted |
| `OperationError` | 15 | Operation error unrelated to data update |
| `RateLimitExceeded` | 30 | Too many requests; decrease the request frequency |
| `BulkPartialSuccess` | 98 | Bulk operation partially completed |
| `BulkCompleteFailure` | 99 | Bulk operation completely failed |
| `BackgroundTask` | 100 | Operation queued; `Data` contains the task ID |
| `ServiceError` | 999 | General service error |

`BackgroundTask` (100) is not an error — the SDK returns the response normally. `MobizonApiException`
is thrown only for codes that represent actual failures.

---

## Contributing

Contributions are welcome. Please open an issue to discuss significant changes before submitting a
pull request.

1. Fork the repository and create a feature branch from `master`.
2. Add tests for any new behaviour. The test suite must pass without network access.
3. Follow the existing C# code style (standard .NET conventions, XML documentation on all public
   members).
4. Submit a pull request with a clear description of the change and its motivation.

---

## License

This project is licensed under the [MIT License](LICENSE).
