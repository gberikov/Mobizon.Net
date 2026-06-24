# Phase 1 Contracts: Public API Surface

This library feature exposes its contract as public .NET types. Signatures below are the binding contract; tests assert against them. DTOs are covered in `data-model.md`; this file covers the **interfaces, entry points, and the ASP.NET surface**.

## Mobizon.Contracts — Service interfaces

```csharp
namespace Mobizon.Contracts.Services
{
    using System;
    using Mobizon.Contracts.Models.Webhooks;

    /// <summary>Parses a raw Mobizon webhook JSON body into a typed event.</summary>
    public interface IWebhookParser
    {
        /// <summary>Parses the body. Throws <see cref="Mobizon.Contracts.Exceptions.WebhookParseException"/>
        /// on malformed JSON or a missing required envelope field.</summary>
        MobizonWebhookEvent Parse(string jsonBody);

        /// <summary>Non-throwing parse. Returns false on malformed input.</summary>
        bool TryParse(string jsonBody, out MobizonWebhookEvent? @event);
    }

    /// <summary>Verifies webhook authenticity via the Mobizon SHA1 signature scheme.</summary>
    public interface IWebhookSignatureVerifier
    {
        /// <summary>Verifies an already-parsed event against the secret. Constant-time, fail-closed.</summary>
        bool Verify(MobizonWebhookEvent @event, string secretKey);

        /// <summary>Verifies from raw envelope values (no parsing required).
        /// Signature string = eventId|attempt|eventCreateTs|secretKey, SHA1, lowercase hex.</summary>
        bool Verify(long eventId, int attempt, string eventCreateTsRaw, string sign, string secretKey);
    }

    /// <summary>Combined verify + parse entry point (recommended).</summary>
    public interface IWebhookProcessor
    {
        /// <summary>Single-secret case.</summary>
        Mobizon.Contracts.Models.Webhooks.WebhookProcessResult Process(string jsonBody, string secretKey);

        /// <summary>Per-webhook-secret case: parses the envelope, lets the caller pick the secret
        /// from the parsed event (typically by <c>WebhookId</c>), then verifies. Parsing the
        /// envelope confers no trust — the selected secret still gates authenticity (FR-004a).</summary>
        Mobizon.Contracts.Models.Webhooks.WebhookProcessResult Process(
            string jsonBody, Func<MobizonWebhookEvent, string> secretSelector);
    }
}
```

> `WebhookProcessResult` and `WebhookProcessStatus` live in `Mobizon.Contracts.Models.Webhooks` (result DTOs) so this interface stays in Contracts per Principle II. The `WebhookProcessor` *implementation* lives in `Mobizon.Net.Webhooks`.

### Contract guarantees (asserted by tests)

| ID | Guarantee |
|----|-----------|
| C-01 | `Parse` populates 100% of documented fields for each known event type (SC-002). |
| C-02 | `Verify` returns true iff `sign` equals `SHA1(eventId\|attempt\|eventCreateTsRaw\|secretKey)` (lowercase hex), compared in constant time (SC-003, FR-001/002). |
| C-03 | `Verify` returns false (never throws) when any of `eventId`/`attempt`/`eventCreateTsRaw`/`sign`/`secretKey` is missing or empty — fail closed (FR-003). |
| C-04 | An unrecognised `eventType` yields `UnknownWebhookEvent`; envelope fields populated; `Verify` still works (SC-004, FR-011). |
| C-05 | Extra/unknown JSON fields never cause `Parse` to fail (FR-012). |
| C-06 | Empty-string timestamps (e.g. `confirmationTs:""`) parse to `null`, not an error (FR-020). |
| C-07 | `EventCreateTsRaw` is byte-identical to the received value and is what feeds the signature (FR-020). |
| C-08 | `Process` returns `Ok` only when parse succeeds AND signature matches; `SignatureMismatch` when parsed but signature wrong; `ParseError` when body unparseable (FR-016, D6). `Event` is populated for both `Ok` and `SignatureMismatch`, null only on `ParseError`. |
| C-14 | `Process(body, secretSelector)` parses the envelope, invokes the selector with the parsed event to obtain the secret, then verifies — supporting per-`WebhookId` secrets (FR-004a). |

## Mobizon.Net.Webhooks — Implementations & result types

```csharp
// Result types (in Mobizon.Contracts.Models.Webhooks):
//   public enum WebhookProcessStatus { Ok, SignatureMismatch, ParseError }
//   public sealed class WebhookProcessResult {
//       public WebhookProcessStatus Status { get; }
//       public MobizonWebhookEvent? Event { get; }
//       public bool IsAuthentic => Status == WebhookProcessStatus.Ok;
//   }

namespace Mobizon.Net.Webhooks
{
    using Mobizon.Contracts.Models.Webhooks;
    using Mobizon.Contracts.Services;

    public sealed class WebhookParser : IWebhookParser { /* ... */ }
    public sealed class WebhookSignatureVerifier : IWebhookSignatureVerifier { /* ... */ }
    public sealed class WebhookProcessor : IWebhookProcessor
    {
        public WebhookProcessor(IWebhookParser parser, IWebhookSignatureVerifier verifier);
        // Parameterless convenience ctor wiring the default parser/verifier.
        public WebhookProcessor();
    }
}
```

## Mobizon.Net.Webhooks.AspNetCore — Integration surface

```csharp
namespace Mobizon.Net.Webhooks.AspNetCore
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using Mobizon.Contracts.Models.Webhooks;

    public sealed class MobizonWebhookOptions
    {
        /// <summary>Resolves the secret key for a received event. Receives the DI container and the
        /// parsed envelope so the secret can be chosen per <c>WebhookId</c>. Required.
        /// For a single shared secret, ignore the event argument and return the configured value.</summary>
        public Func<IServiceProvider, MobizonWebhookEvent, string> SecretKeyResolver { get; set; }
    }

    public static class WebhookServiceCollectionExtensions
    {
        public static IServiceCollection AddMobizonWebhooks(
            this IServiceCollection services, Action<MobizonWebhookOptions> configure);
    }

    public static class WebhookEndpointRouteBuilderExtensions
    {
        /// <summary>Maps a POST endpoint that reads the body, verifies, parses, and invokes
        /// <paramref name="handler"/> only on success. Returns 200 (Ok), 403 (SignatureMismatch),
        /// or 400 (ParseError) automatically.</summary>
        public static IEndpointConventionBuilder MapMobizonWebhook(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Func<MobizonWebhookEvent, CancellationToken, Task> handler);
    }
}
```

### ASP.NET contract guarantees (asserted by tests)

| ID | Guarantee |
|----|-----------|
| C-09 | `MapMobizonWebhook` returns HTTP 200 and invokes the handler for a valid, correctly-signed POST. |
| C-10 | Returns HTTP 403 and does NOT invoke the handler for a signature mismatch. |
| C-11 | Returns HTTP 400 for an unparseable body. |
| C-12 | Reads the request body asynchronously; the handler receives the request's `CancellationToken`. |
| C-13 | The endpoint responds before the handler's heavy work would block acknowledgement (handler awaited but expected to be fast / queue async work) — enables ack within Mobizon's 5s window (SC-005, FR-018a). |
| C-15 | `MapMobizonWebhook` resolves the secret via `MobizonWebhookOptions.SecretKeyResolver`, passing the parsed event so a per-`WebhookId` secret can be selected (FR-004a). |
