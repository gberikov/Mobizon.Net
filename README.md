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
  [`docs/coverage-matrix.md`](https://github.com/gberikov/Mobizon.Net/blob/master/docs/coverage-matrix.md)
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
and DI helpers. See the [Webhooks](https://github.com/gberikov/Mobizon.Net/blob/master/docs/webhooks.md) section below.

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

## Documentation

| Guide | Contents |
| --- | --- |
| [API modules](https://github.com/gberikov/Mobizon.Net/blob/master/docs/api-modules.md) | Every method on the nine services, with request and response examples |
| [Webhooks](https://github.com/gberikov/Mobizon.Net/blob/master/docs/webhooks.md) | Signature verification, typed events, ASP.NET Core endpoints |
| [Configuration](https://github.com/gberikov/Mobizon.Net/blob/master/docs/configuration.md) | DI registration, Polly resilience, regional API URLs |
| [Error handling](https://github.com/gberikov/Mobizon.Net/blob/master/docs/error-handling.md) | Exception hierarchy, field errors, response codes |
| [Coverage matrix](https://github.com/gberikov/Mobizon.Net/blob/master/docs/coverage-matrix.md) | Per-endpoint coverage and how each field was verified |
| [API shapes](https://github.com/gberikov/Mobizon.Net/blob/master/docs/api-shapes.md) | Response shapes as actually captured from the live API |

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

This project is licensed under the [MIT License](https://github.com/gberikov/Mobizon.Net/blob/master/LICENSE).
