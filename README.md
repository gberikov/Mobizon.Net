# Mobizon.Net

A .NET SDK for the [Mobizon](https://mobizon.kz) SMS gateway REST API (v1). Provides a strongly-typed,
async-first client for sending SMS messages, managing bulk campaigns, tracking short links, checking
account balance, and monitoring background tasks — all from a single `IMobizonClient` interface.

[![NuGet](https://img.shields.io/nuget/v/Mobizon.Net.svg)](https://www.nuget.org/packages/Mobizon.Net)
[![NuGet](https://img.shields.io/nuget/v/Mobizon.Net.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/Mobizon.Net.Extensions.DependencyInjection)
[![NuGet](https://img.shields.io/nuget/v/Mobizon.Net.Extensions.Polly.svg)](https://www.nuget.org/packages/Mobizon.Net.Extensions.Polly)
[![NuGet](https://img.shields.io/nuget/v/Mobizon.Contracts.svg)](https://www.nuget.org/packages/Mobizon.Contracts)

---

## Features

- **5 API modules** — Messages, Campaigns, Links, User, and TaskQueue — with full method coverage
- **Strongly-typed requests and responses** via `MobizonResponse<T>` with typed exceptions
- **ASP.NET Core DI integration** — register with a single `AddMobizon()` call
- **Polly resilience** — retry, circuit breaker, and timeout policies out of the box
- **`netstandard2.0` target** — compatible with .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5+
- **Zero third-party dependencies** in the core package (only `System.Text.Json`)
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

Interfaces, request/response DTOs, and exception types. Zero dependencies. Reference this package
when you want to accept `IMobizonClient` without depending on the implementation.

### DI integration (ASP.NET Core)

```bash
dotnet add package Mobizon.Net.Extensions.DependencyInjection
```

Adds `AddMobizon()` extension methods on `IServiceCollection` and wires up `IHttpClientFactory`.

### Polly resilience

```bash
dotnet add package Mobizon.Net.Extensions.Polly
```

Adds `AddMobizonResilience()` on `IHttpClientBuilder` with retry, circuit breaker, and timeout
policies. Requires the DI package.

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

`IMobizonClient` exposes five sub-services as properties:

```
IMobizonClient
├── .Messages   — IMessageService
├── .Campaigns  — ICampaignService
├── .Links      — ILinkService
├── .User       — IUserService
└── .TaskQueue  — ITaskQueueService
```

### Messages

Send individual SMS messages and query delivery status.

**Send an SMS:**

```csharp
var result = await client.Messages.SendSmsMessageAsync(
    new SendSmsMessageRequest
    {
        Recipient = "77001234567",
        Text     = "Your verification code: 8421",
        From     = "MyBrand",   // optional sender name / alphaname
        Validity = 60           // optional, minutes
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
    Console.WriteLine($"ID={item.Id}  Status={item.Status}  Segments={item.SegNum}");
```

**List messages with pagination:**

```csharp
var messages = await client.Messages.ListAsync(
    new MessageListRequest
    {
        Criteria   = new MessageListCriteria { Status = 3 },
        Pagination = new PaginationRequest   { CurrentPage = 0, PageSize = 50 },
        Sort       = new SortRequest         { Field = "id", Direction = SortDirection.DESC }
    });

Console.WriteLine($"Total: {messages.Data.TotalCount}");
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
        Type = 1,
        From = "MyBrand",
        Text = "Flash sale — 50% off today!"
    });

// 2. Add recipients
await client.Campaigns.AddRecipientsAsync(
    new AddRecipientsRequest
    {
        CampaignId = campaign.Data.CampaignId,
        Type       = 1,
        Data       = new[] { "77001111111", "77002222222" }
    });

// 3. Send
var sendResult = await client.Campaigns.SendAsync(campaign.Data.CampaignId);

// 4. If the API queued a background task, poll for completion
if (sendResult.Code == MobizonResponseCode.BackgroundTask)
{
    var taskStatus = await client.TaskQueue.GetStatusAsync(
        sendResult.Data.TaskId.Value);
    Console.WriteLine($"Progress: {taskStatus.Data.Progress}%");
}

// 5. Get delivery statistics
var info = await client.Campaigns.GetInfoAsync(campaign.Data.CampaignId);
Console.WriteLine($"Sent: {info.Data.Sent}  Delivered: {info.Data.Delivered}  Failed: {info.Data.Failed}");
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

**Retrieve a link by short code:**

```csharp
var link = await client.Links.GetAsync("abc123");
Console.WriteLine($"URL: {link.Data.FullLink}  Clicks: {link.Data.Clicks}");
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

foreach (var entry in stats.Data)
    Console.WriteLine($"{entry.Date}: {entry.Clicks} clicks");
```

**Update a link:**

```csharp
await client.Links.UpdateAsync(
    new UpdateLinkRequest
    {
        Code     = "abc123",
        FullLink = "https://example.com/promo-v2",
        Comment  = "Updated URL"
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
`IMobizonClient` is registered as a **scoped** service backed by `IHttpClientFactory`.

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

Chain `AddMobizonResilience()` after `AddMobizon()` to apply retry, circuit breaker, and timeout
policies to all HTTP calls.

### Default policies

| Policy | Default |
|--------|---------|
| Retry | 3 attempts, exponential backoff: 2 s, 4 s, 8 s |
| Circuit breaker | Opens after 5 consecutive failures, breaks for 30 s |
| Timeout | 30 s per request |

```csharp
services.AddMobizon(options =>
{
    options.ApiKey = configuration["Mobizon:ApiKey"];
    options.ApiUrl = configuration["Mobizon:ApiUrl"];
})
.AddMobizonResilience(); // apply default retry + circuit breaker + timeout
```

### Customise resilience options

```csharp
services.AddMobizon(configuration.GetSection("Mobizon"))
    .AddMobizonResilience(resilience =>
    {
        resilience.RetryCount                = 5;
        resilience.RetryBaseDelay            = TimeSpan.FromSeconds(1);
        resilience.CircuitBreakerCount       = 10;
        resilience.CircuitBreakerDuration    = TimeSpan.FromSeconds(60);
        resilience.Timeout                   = TimeSpan.FromSeconds(15);
    });
```

> **Timeout note**: `MobizonClientOptions.Timeout` sets `HttpClient.Timeout` and applies when
> Polly is not used. When `AddMobizonResilience()` is active, configure timeout exclusively via
> `MobizonResilienceOptions.Timeout` to avoid conflicting policies.

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
| `BackgroundTask` | 100 | Task queued; `Data` contains the task ID |
| `InvalidData` | 1 | Invalid request parameters |
| `AuthFailed` | 2 | Authentication failed |
| `NotFound` | 3 | Resource not found |
| `AccessDenied` | 4 | Insufficient permissions |
| `InternalError` | 5 | Server-side error |

`BackgroundTask` (100) is not an error — the SDK returns the response normally. `MobizonApiException`
is thrown only for codes that represent actual failures.

---

## Contributing

Contributions are welcome. Please open an issue to discuss significant changes before submitting a
pull request.

1. Fork the repository and create a feature branch from `main`.
2. Add tests for any new behaviour. The test suite must pass without network access.
3. Follow the existing C# code style (standard .NET conventions, XML documentation on all public
   members).
4. Submit a pull request with a clear description of the change and its motivation.

---

## License

This project is licensed under the [MIT License](LICENSE).
