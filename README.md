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
  NumberStopList, and Alphanames. Every method on the public documentation pages is covered; which fields and
  shapes are verified against official docs, live captures, or neither is tracked per endpoint in
  [`docs/coverage-matrix.md`](docs/coverage-matrix.md)
- **ContactCards LINQ-style query API** via `IContactCardQuery` for composable server-side filtering
- **Inbound webhooks** — verify signatures and parse delivery-report and form events, with optional ASP.NET Core helpers
- **Strongly-typed requests and responses** — service methods return domain types directly; errors throw `MobizonApiException`
- **ASP.NET Core DI integration** — register with a single `AddMobizon()` call
- **Polly resilience** — retry and circuit breaker policies out of the box
- **`netstandard2.0` + `net8.0` targets** — .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ (ASP.NET Core webhooks: `net8.0` / `net10.0`)
- **API key never in the URL** — sent in the POST body, so it stays out of proxy and `HttpClient` logs
- **Actionable, safe errors** — `MobizonApiException` (API code, message, per-field `FieldErrors`) vs `MobizonException`
  (transport/timeout/protocol) with the HTTP `StatusCode` and JSON path attached — and never a quote of the response body,
  so message texts, phone numbers and one-time codes stay out of your logs
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

**Check delivery status** (1–100 message IDs per call; larger sets must be split by the caller):

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

// 2. Add recipients (phone numbers or contact cards are processed synchronously, in batches of
//    up to 500 — the SDK splits larger lists for you). Outcome aggregates the batches:
//    AllAdded, PartiallyAdded (any batch accepted + any rejected) or NoneAdded (every batch rejected).
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

foreach (var entry in addResult.Entries!.Where(e => e.Code != 0))
    Console.WriteLine($"Rejected {entry.Number}: code {entry.Code}");

// 3. Send — returns CampaignSendResult; IsQueued is true when the API accepted
//    the send as a background task (response code 100).
var sendResult = await client.Campaigns.SendAsync(campaignId);

// 4. If the API queued a background task, poll for completion (at most once per second)
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

`ListAsync` items are `CampaignInfo` objects, as the API documents: cast one to read its statistics instead of
calling `GetInfoAsync` for every campaign.

```csharp
var page = await client.Campaigns.ListAsync();
foreach (var campaign in page.Items)
    if (campaign is CampaignInfo info && info.Counters is { } counters)
        Console.WriteLine($"{info.Id}: {counters.TotalDelivrdMsgNum}/{counters.TotalMsgNum} delivered");
```

Delivery settings are readable through `Validity`, `MessageClass` and `TrackShortLinkRecipients` whichever way
the server sent them: the documentation puts them at the top level, real responses nest them in `extra`
(also exposed as `CampaignExtra`). `Groups` arrives as a comma-separated string and is parsed into a list.

**Recipients from a file or contact groups are queued, not added.** The API answers with a background task;
`IsQueued` is `true` and `Outcome` is `AllAdded` only in the sense of "accepted". Poll the task until it completes
(or is rejected) before sending the campaign:

```csharp
using var csv = File.OpenRead("recipients.csv");            // you own the stream; the SDK never closes it
var queued = await client.Campaigns.AddRecipientsAsync(new AddRecipientsRequest
{
    CampaignId = campaignId, RecipientsFile = csv, RecipientsFileName = "recipients.csv"
});

var status = await client.TaskQueue.GetStatusAsync(queued.TaskId!.Value);
while (status.Status == BackgroundTaskStatus.Pending || status.Status == BackgroundTaskStatus.InProgress)
{
    await Task.Delay(TimeSpan.FromSeconds(1));               // Mobizon: poll no more than once per second
    status = await client.TaskQueue.GetStatusAsync(queued.TaskId.Value);
}
if (status.Status != BackgroundTaskStatus.Completed)
    throw new InvalidOperationException($"Recipient import failed: {status.Status}");

await client.Campaigns.SendAsync(campaignId);
```

**Partial execution of a large list.** When a later batch of a multi-batch send fails — API error, transport error,
timeout or your own cancellation — the earlier batches are already applied on the server. The thrown exception
(any type, including `OperationCanceledException`) carries an `AddRecipientsProgress`: the confirmed entries,
how many recipients they cover, and the size of the batch whose outcome is **unknown** (its request may have been
applied even though no response arrived). Never resend the whole list after a timeout; check the campaign
(`GetInfoAsync` / `Messages.ListAsync`) or resume from `ConfirmedCount + PendingCount` and reconcile the pending batch.

```csharp
try
{
    await client.Campaigns.AddRecipientsAsync(bigRequest);
}
catch (Exception ex) when (AddRecipientsProgress.FromException(ex) is { } progress)
{
    Console.WriteLine($"{progress.ConfirmedCount} confirmed, {progress.PendingCount} unknown, " +
                      $"{progress.Confirmed.Entries!.Count(e => e.Code == 0)} accepted so far");
    throw;
}
```

`Replace = true` is honoured by the first batch only, so later batches never wipe what earlier ones added.

**Placeholder names share the wire namespace with the phone number.** A placeholder called `recipient` would
replace the destination number, so it is rejected, as are empty names and names containing `[` or `]`. The whole
list is checked before the first batch is sent, so a bad name never applies to part of it.

```csharp
Recipients = new[]
{
    new RecipientEntry
    {
        Recipient    = "77001111111",
        Placeholders = new Dictionary<string, string> { ["name"] = "Ivan" }   // "recipient" would throw
    }
}
```

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

**Updating a link does not merge with what is stored.** The SDK sends only the properties you set, and the API
documents an omitted `data[expirationDate]` as making the link valid indefinitely — a null `ExpirationDate` does
not preserve the current one. Read the link first and pass the value back when you mean to keep it. This follows
the published contract and has not been confirmed against a live account. `FullLink` is not among the documented
update parameters; it is sent when set, but the server may ignore it.

```csharp
var link = await client.Links.GetByIdAsync(id);
await client.Links.UpdateAsync(new UpdateLinkRequest
{
    Id             = id,
    Comment        = "Q3 campaign",
    ExpirationDate = link.ExpirationDate   // omit this and the link may become permanent
});
```

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
int total      = await vip.CountAsync();                   // whole query; Page/Take do not matter
var head       = await vip.Page(1).FirstOrDefaultAsync();  // first item *of page 1* (same as secondPage.Items[0])
var only       = await client.ContactCards.Where(x => x.Mobile.Value == "77001234567").SingleAsync();
// Single checks uniqueness across the whole query (server-side total), ignoring Page/Take.

// UpdateAsync is a full replace: load, modify, save. Null fields are cleared on the server —
// except Address: a null Address keeps the server's address; an empty AddressFieldInfo clears it.
var existing = await client.ContactCards.FindAsync(card.Id.Value);
existing!.Info    = "Preferred customer";
existing.Address  = new AddressFieldInfo();                 // clear the address
await client.ContactCards.UpdateAsync(existing);
```

Contact fields arrive in more than one shape. An empty array or empty string means "not set" and reads as `null`;
a bare string (the shape the SDK itself writes) is mapped onto `Value`; anything else raises `MobizonException`
rather than silently reading as `null` and being cleared by the next update. A `type` or `gender` value the SDK
does not know stays in `Mobile.TypeRaw` / `GenderRaw`, and an update sends it back unchanged.

`CountAsync`, `FirstOrDefaultAsync` and `SingleOrDefaultAsync` request 25 items, the smallest page size the
documented list endpoints accept (25, 50 or 100).

Supported filter operators: `==` (including `== null` for "empty"), `!=`, `>=`, `<=`, `.Contains()`, combined with `&&`.
The filtered member must be on the left-hand side (`x.GroupId == 33`, not `33 == x.GroupId`), and the value must be a
constant or a captured variable. The API has no "not empty" operator, so `!= null` and `!= Gender.Undefined` are
rejected with `NotSupportedException` rather than sent as something that means the opposite.

---

### Number stop-list

Numbers (or ranges) that must never receive your messages. `campaign/addRecipients` reports such recipients
with per-entry code 5.

```csharp
long entryId = await client.NumberStopList.AddNumberAsync("77001234567", "Opted out via support");
await client.NumberStopList.AddNumberRangeAsync("77001000000", "77001999999", "Test range");

var page = await client.NumberStopList.ListAsync(new StopListListRequest
{
    Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 50 }
});
foreach (var e in page.Items)
    Console.WriteLine($"{e.Id}: {e.Number} ({e.Level}) — {e.Comment}");

await client.NumberStopList.DeleteAsync(entryId);
```

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

**Signature**: `SHA1(eventId|attempt|eventCreateTs|secretKey)` — a plain SHA1 over a pipe-joined string, **not**
an HMAC — compared in constant time and failing closed. The original `eventCreateTs` string is preserved verbatim
for verification. Note what the signature does *not* cover: `data`, `eventType` and `webhookId` are not part of
the signed string, so a valid signature proves the event id/attempt/timestamp came from someone holding the secret,
not that the payload body is untouched. This is a property of the Mobizon protocol; the SDK cannot strengthen it
without breaking compatibility. Serve the endpoint over HTTPS so the body cannot be altered in transit, and treat
`data` as input to validate, not as authenticated fact. The SDK deliberately adds no signature age check: Mobizon
retries a failed delivery up to 10 times with a growing interval (about an hour in total), and the retried event
keeps its original `eventCreateTs`, so a short TTL would reject legitimate deliveries.

**Webhooks without a secret key**: Mobizon allows creating a webhook without a secret; such requests
carry no signature and `WebhookProcessor` (and `MapMobizonWebhook`) always rejects them with
`SignatureMismatch` — it fails closed. If you deliberately run unsigned, parse with
`new WebhookParser().Parse(body)` and protect the endpoint by other means (IP allow-list, private URL).

**Timestamps have no time zone.** `eventCreateTs`, `statusUpdateTs` and the confirmation timestamps arrive as
`yyyy-MM-dd HH:mm:ss` with no offset, and the webhook documentation does not state a zone. The SDK parses them as
UTC. Treat that as an assumption: confirm it against your own account before using these values for ordering or
SLA measurement, and deduplicate on `EventId` rather than on time.

**Acknowledge fast**: Mobizon treats a webhook as failed if no `2xx` arrives within 5 seconds and
retries up to 10 times. Verify → persist/enqueue → return `200`, then process asynchronously.
Deduplicate on `EventId` (stable across retries) — persisting seen ids is your application's job.

**Unknown event vs unknown value**: a new *event type* arrives as `UnknownWebhookEvent` (with `EventTypeRaw` and the
raw `RawData` element); a known event whose *field* carries a new value stays typed — e.g. `SmsDeliveryReport.Status`
is `null` with `StatusRaw` set, and `WebhookFieldItem.FieldTypeKind` is `null` with `FieldType` set:

```csharp
case FormSubmissionEvent form:
    foreach (var item in form.Data.Items)
        switch (item.FieldTypeKind)
        {
            case WebhookFieldType.Mobile: /* item.Value is a phone number */ break;
            case WebhookFieldType.Email:  /* ... */ break;
            case null: logger.LogInformation("New field type {Type}", item.FieldType); break;
        }
    break;
```

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
response could send an SMS twice. Set `RetryNonIdempotentRequests = true` to retry everything except
multipart uploads (contact-card photos, recipient files), which are **never** retried — the stream has been read
to the end by the first attempt, and a resend would carry an empty file. The **circuit breaker** applies to every
call. There is no separate timeout policy — use `MobizonClientOptions.Timeout`.

What actually counts as a failure for both policies (Polly's `HandleTransientHttpError`): `HttpRequestException`,
HTTP 5xx and HTTP 408. Not covered: an API error inside an HTTP 200 envelope — including rate-limit code 30, which
surfaces as `MobizonApiException` with `Code == RateLimitExceeded` and is up to you to back off from — and HTTP 429.
Option values are validated when `AddMobizonResilience` runs, so a negative count or a back-off schedule that
overflows fails at start-up rather than during an outage.

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

`ApiUrl` must be an absolute **`https://`** URI with no query string, fragment or credentials; the SDK refuses
anything else before sending a request, because the API key travels in every request body. A path prefix is fine
(a trusted reverse proxy such as `https://gateway.example.com/mobizon`), and any host is accepted so regional and
self-hosted endpoints work. For a local test server set `AllowInsecureHttp = true` explicitly.

**Ownership and limits.** The SDK applies `Timeout` (covering the whole exchange, body included; `Timeout.InfiniteTimeSpan`
disables it) and a 16 MB response-size cap to the `HttpClient` it owns — the `MobizonClient(options)` constructor and the
DI registration. A client you pass in yourself keeps its own settings. Streams you pass in (`ContactCard.Photo`,
`AddRecipientsRequest.RecipientsFile`) are read to the end but never closed. Your `CancellationToken` always surfaces
as `OperationCanceledException`; only the client's own timeout is wrapped in `MobizonException`.

---

## Error Handling

The SDK uses a two-level exception hierarchy:

| Exception | When thrown |
|-----------|-------------|
| `MobizonApiException` | The API returned a non-success response code (auth failure, invalid data, not found, etc.). Exposes `Code` (`MobizonResponseCode`), `RawCode`, `ApiMessage` and `FieldErrors` — per-field validation messages when the API sent them (`ex.FieldErrors["text"]`, nested paths flattened as `mobile.value`). Thrown regardless of the HTTP status as long as the body is a valid envelope. |
| `MobizonException` | Transport or protocol failure: network error, `HttpClient.Timeout` expiry, a non-JSON body (proxy/5xx HTML page), an envelope without a numeric `code`, a non-2xx status with a "success" envelope, a missing payload, a zero id from a create call, or a payload that does not fit the expected type. `StatusCode` (`HttpStatusCode?`) carries the HTTP status when a response was received; `null` when it was not. `MobizonApiException` derives from this type. |

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

**Safe diagnostics.** Exception messages name the operation (`Campaign/Create`), the HTTP status and the JSON path of
the offending field (`$.messageId`) but never quote the response body or a field value — even in `InnerException`
and `ToString()` — so SMS texts, phone numbers, one-time codes and the API key cannot leak through ordinary
exception logging. The one exception is `ApiMessage`, the API's own error text, which is needed to act on the error.
This guarantee does not extend to anything *you* log: an `HttpClient` logging handler or a proxy that dumps request
bodies will see the API key and message texts in every request. Do not log Mobizon request bodies.

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

Codes 98 (`BulkPartialSuccess`) and 99 (`BulkCompleteFailure`) become `AddRecipientsResult.Outcome`
(`PartiallyAdded` / `NoneAdded`) for synchronous recipient loads. Code 100 (`BackgroundTask`) is accepted
by campaign sending and group/file recipient loads: `CampaignSendResult.IsQueued` or
`AddRecipientsResult.IsQueued` identifies the queued result. Group/file loads require a positive `TaskId`;
an invalid task response throws `MobizonException`. Other non-zero codes, including 100 on synchronous
recipient batches, throw `MobizonApiException`. If a later batch fails, confirmed progress remains available
through `AddRecipientsProgress.FromException`.

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
