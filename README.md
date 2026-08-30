# Mobizon.Net

An **unofficial** .NET SDK for the [Mobizon](https://mobizon.kz) SMS gateway REST API (v1). Not
affiliated with or endorsed by Mobizon. Provides a strongly-typed,
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
- **Strongly-typed requests and responses** — service methods return domain types directly; errors throw `MobizonApiException`
- **ASP.NET Core DI integration** — register with a single `AddMobizon()` call
- **Polly resilience** — retry and circuit breaker policies out of the box
- **`netstandard2.0` + `net8.0` targets** — .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ (ASP.NET Core webhooks: `net8.0` / `net10.0`)
- **API key never in the URL** — sent in the POST body, so it stays out of proxy and `HttpClient` logs
- **Actionable errors** — `MobizonApiException` (API code) vs `MobizonException` (transport/timeout/non-JSON) with the HTTP `StatusCode` attached
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

Send an SMS in three lines (excluding configuration):

```csharp
using Mobizon.Contracts;
using Mobizon.Net;

using var client = new MobizonClient(new MobizonClientOptions
{
    ApiKey = "your-api-key-here",
    ApiUrl = "https://api.mobizon.kz"
});

var result = await client.Messages.QuickSendAsync("77001234567", "Hello from Mobizon.Net!");
Console.WriteLine($"Message ID: {result.MessageId}");
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
├── .ContactCards    — IContactCardSet  (query entry point; returns IContactCardQuery)
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
            Validity     = TimeSpan.FromMinutes(60),   // optional delivery window
            ShortenLinks = true,   // shorten URLs in Text via Mobizon's link shortener
        }
    });

Console.WriteLine($"Campaign: {result.CampaignId}");
Console.WriteLine($"Message:  {result.MessageId}");
Console.WriteLine($"Status:   {result.Status}");
```

**Check delivery status:**

```csharp
var status = await client.Messages.GetSmsStatusAsync(
    new[] { result.MessageId });

foreach (var item in status)
    Console.WriteLine($"ID={item.Id}  Status={item.Status}  Segments={item.Segments}");
```

**List messages with pagination:**

```csharp
var messages = await client.Messages.ListAsync(
    new MessageListRequest
    {
        Criteria   = new MessageListCriteria { Status = SmsStatus.Delivered },
        Pagination = new PaginationRequest   { CurrentPage = 0, PageSize = 50 },
        Sort       = new SortRequest         { Field = "id", Direction = SortDirection.Descending }
    });

Console.WriteLine($"Total: {messages.TotalItemCount}");
foreach (var msg in messages.Items)
    Console.WriteLine($"[{msg.Id}] {msg.Text}");
```

---

### Campaigns

Create and send bulk SMS campaigns, then retrieve delivery statistics.

```csharp
// 1. Create campaign — returns the new campaign ID directly
long campaignId = await client.Campaigns.CreateAsync(
    new CreateCampaignRequest
    {
        Type = CampaignType.Bulk,
        From = "MyBrand",
        Text = "Flash sale — 50% off today!"
    });

// 2. Add recipients — Outcome reports AllAdded / PartiallyAdded / NoneAdded
var addResult = await client.Campaigns.AddRecipientsAsync(
    new AddRecipientsRequest
    {
        CampaignId = campaignId,
        Recipients = new[]
        {
            new RecipientEntry { Recipient = "77001111111" },
            new RecipientEntry { Recipient = "77002222222" }
        }
    });

if (addResult.Outcome != AddRecipientsOutcome.AllAdded)
    Console.WriteLine($"Some recipients were rejected: {addResult.Outcome}");

// 3. Send — returns CampaignSendResult; IsQueued is true when the API accepted
//    the send as a background task (former code 100).
var sendResult = await client.Campaigns.SendAsync(campaignId);

// 4. If the API queued a background task, poll for completion
if (sendResult.IsQueued)
{
    var taskStatus = await client.TaskQueue.GetStatusAsync(sendResult.Id);
    Console.WriteLine($"Progress: {taskStatus.Progress}%");
}

// 5. Get delivery statistics
var info = await client.Campaigns.GetInfoAsync(campaignId);
Console.WriteLine($"Delivered: {info.Counters?.TotalDelivrdMsgNum}");

// 6. Short links used by the campaign (same as client.Links.GetLinksAsync)
var links = await client.Campaigns.GetLinksAsync(campaignId);
```

Other available methods: `GetAsync`, `ListAsync`, `DeleteAsync`, `GetLinksAsync`.

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
        ExpirationDate = new DateTime(2026, 12, 31)
    });

Console.WriteLine($"Short code: {link.Code}");
```

**Retrieve a link by short code, numeric ID, or full short URL:**

```csharp
var byCode      = await client.Links.GetByCodeAsync("abc123");
var byId        = await client.Links.GetByIdAsync(link.Id);
var byShortLink = await client.Links.GetByShortLinkAsync("https://mbzn.co/abc123");

Console.WriteLine($"URL: {byCode.FullLink}  Clicks: {byCode.Clicks}");
```

**Get click statistics:**

```csharp
var stats = await client.Links.GetStatsAsync(
    new GetLinkStatsRequest
    {
        Ids      = new[] { link.Id },
        Type     = LinkStatsType.Daily,   // up to 5 ids per request
        DateFrom = new DateTime(2026, 1, 1),
        DateTo   = new DateTime(2026, 1, 31, 23, 59, 59)
    });

// GetStatsAsync returns a LinkStatsResult with Links (one entry per requested link ID)
foreach (var s in stats.Links)
{
    Console.WriteLine($"LinkId={s.LinkId}  Total clicks={s.TotalClicks}");
    foreach (var entry in s.Points)
        Console.WriteLine($"  {entry.Param}: {entry.Clicks} clicks");
}
```

**Update a link:**

```csharp
await client.Links.UpdateAsync(
    new UpdateLinkRequest
    {
        Id      = link.Id,
        Comment = "Updated comment"
    });
```

**Delete links:**

```csharp
var deleted = await client.Links.DeleteAsync(new[] { link.Id });
if (!deleted.AllProcessed)
    Console.WriteLine($"Not deleted: {string.Join(", ", deleted.NotProcessed)}");
```

Other available methods: `GetLinksAsync` (links by campaign ID), `ListAsync`.

---

### User

Check your Mobizon account balance.

```csharp
var balance = await client.User.GetOwnBalanceAsync();
Console.WriteLine($"{balance.Balance:0.0000} {balance.Currency}"); // decimal
// Output: 4043.0656 KZT
```

All SDK calls are HTTP POST; the API key travels in the request body, never in the URL.

---

### TaskQueue

Poll the progress of a long-running background task (typically returned after `Campaigns.SendAsync`).

```csharp
var taskStatus = await client.TaskQueue.GetStatusAsync(taskId);
Console.WriteLine($"{taskStatus.Status}: {taskStatus.Progress}% complete");
```

`TaskQueueStatus` fields: `Status` (`BackgroundTaskStatus`: Pending / InProgress / Completed / Rejected) and `Progress` (0–100).

---

### Contact cards & groups

`client.ContactCards` is an immutable, LINQ-style query builder over `contactcard/list`; `client.ContactGroups` manages groups.

```csharp
long groupId = await client.ContactGroups.CreateAsync("VIP");

var card = new ContactCard
{
    Name = "Ivan", Surname = "Petrov",
    Mobile = new MobileFieldInfo { Value = "77001234567", Type = ContactType.Main },
    Gender = Gender.Male
};
await client.ContactCards.AddAsync(card);                 // sets card.Id
await client.ContactCards.SetGroupsAsync(card.Id!.Value, new[] { groupId });

// Filter, sort and page on the server. Take = page size (default 25), Page = zero-based page index.
var vip = client.ContactCards.Where(x => x.GroupId == groupId).OrderBy(x => x.Surname).Take(50);
var firstPage  = await vip.Page(0).ToListAsync();
var secondPage = await vip.Page(1).ToPageAsync();          // includes TotalCount
int total      = await vip.CountAsync();

// UpdateAsync is a full replace: load, modify, save. Null fields are cleared on the server.
var existing = await client.ContactCards.FindAsync(card.Id.Value);
existing!.Info = "Preferred customer";
await client.ContactCards.UpdateAsync(existing);
```

Supported filter operators: `==` (including `== null` for "empty"), `!=`, `>=`, `<=`, `.Contains()`, combined with `&&`.

---

### Alphanames

List the registered sender IDs available to the account.

```csharp
var alphanames = await client.Alphanames.ListAsync();
foreach (var a in alphanames.Items)
    Console.WriteLine($"{a.Alphaname?.Name}  (id={a.AlphanameId})");
```

---

## Webhooks

Mobizon can push events to your server in real time instead of you polling `GetSMSStatus`. Webhooks
are created and configured in the Mobizon **control panel** (not via the API); choose the **JSON**
data format when creating the webhook (this SDK does not parse the `raw`/`xml` formats); this SDK
provides the **receive** side: verifying the SHA1 signature and parsing the JSON body into a typed event.

Supported event types: `sms-delivery-report`, `form-submission`, `form-contact-confirmation`,
`form-contact-unsubscribe`. Unrecognised types are surfaced as `UnknownWebhookEvent` (forward-compatible).

### Framework-agnostic core

```csharp
using Mobizon.Contracts.Webhooks;
using Mobizon.Net.Webhooks;

var processor = new WebhookProcessor(); // default parser + verifier

// body = raw request body; secret = the key you set when creating the webhook
WebhookProcessResult result = processor.Process(body, secret);

switch (result.Status)
{
    case WebhookProcessStatus.Ok when result.Event is SmsDeliveryReportEvent sms:
        // sms.Data.MessageId, sms.Data.Status (SmsStatus?), sms.Data.StatusRaw, sms.Data.To
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
using Mobizon.Contracts.Webhooks;
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

**Webhooks without a secret key**: Mobizon allows creating a webhook without a secret; such requests
carry no signature and `WebhookProcessor` (and `MapMobizonWebhook`) always rejects them with
`SignatureMismatch` — it fails closed. If you deliberately run unsigned, parse with
`new WebhookParser().Parse(body)` and protect the endpoint by other means (IP allow-list, private URL).

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
    options.ApiKey = configuration["Mobizon:ApiKey"]!;
    options.ApiUrl = configuration["Mobizon:ApiUrl"]!;
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
`IMobizonClient` is registered as a **transient** service backed by `IHttpClientFactory`. It is not
`IDisposable` — never wrap an injected client in `using`; the factory owns the `HttpClient`.

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

Chain `AddMobizonResilience()` after `AddMobizon()`. By default the **retry** policy applies only to
read-only calls (`get*` / `list`): retrying `message/sendSmsMessage` or `campaign/send` after a lost
response could send an SMS twice. Set `RetryNonIdempotentRequests = true` to retry everything. The
**circuit breaker** applies to every call. There is no separate timeout policy — use
`MobizonClientOptions.Timeout`.

### Default policies

| Policy | Default |
|--------|---------|
| Retry | 3 attempts, exponential backoff: 1 s, 2 s, 4 s |
| Circuit breaker | Opens after 5 consecutive failures, breaks for 30 s |

```csharp
services.AddMobizon(options =>
{
    options.ApiKey = configuration["Mobizon:ApiKey"]!;
    options.ApiUrl = configuration["Mobizon:ApiUrl"]!;
})
.AddMobizonResilience(); // apply default retry + circuit breaker
```

### Customise resilience options

```csharp
services.AddMobizon(configuration.GetSection("Mobizon"))
    .AddMobizonResilience(resilience =>
    {
        resilience.RetryNonIdempotentRequests       = false; // default
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
| `MobizonException` | Transport-level failure: network error, `HttpClient.Timeout` expiry, non-JSON response (proxy/5xx HTML page), or a JSON parse problem. `StatusCode` (`HttpStatusCode?`) carries the HTTP status when a response was received; `null` when it was not (network error, timeout). `MobizonApiException` derives from this type. |

```csharp
try
{
    var result = await client.Messages.SendSmsMessageAsync(request);
    Console.WriteLine($"Message ID: {result.MessageId}");
}
catch (MobizonApiException ex)
{
    // API-level error — invalid parameters, bad API key, quota exceeded, etc.
    Console.WriteLine($"API Error [{ex.Code}]: {ex.ApiMessage}");
}
catch (MobizonException ex)
{
    // Transport error — network failure, timeout, response parse error
    Console.WriteLine($"SDK Error [{ex.StatusCode?.ToString() ?? "no response"}]: {ex.Message}");
}
```

Cancellation via your own `CancellationToken` still surfaces as `OperationCanceledException`; only the
client's own timeout is wrapped.

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
| `BackgroundTask` | 100 | Operation queued as a background task |
| `ServiceError` | 999 | General service error |

Codes 98 (`BulkPartialSuccess`), 99 (`BulkCompleteFailure`), and 100 (`BackgroundTask`) are not
surfaced as exceptions. Instead they are folded into structured result types: code 100 becomes
`CampaignSendResult.IsQueued = true` (with `Id` holding the task ID); codes 98/99 become
`AddRecipientsResult.Outcome` (`PartiallyAdded` / `NoneAdded`). `MobizonApiException` is thrown
only for codes that represent actual failures.

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
