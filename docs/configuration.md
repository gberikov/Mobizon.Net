# Configuration

[← Back to the README](https://github.com/gberikov/Mobizon.Net/blob/master/README.md)

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

