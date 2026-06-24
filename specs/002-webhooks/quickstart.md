# Quickstart: Mobizon Webhooks

## Install

```bash
# Framework-agnostic core (verify + parse)
dotnet add package Mobizon.Net.Webhooks

# Optional: ASP.NET Core integration
dotnet add package Mobizon.Net.Webhooks.AspNetCore
```

The core targets `netstandard2.0` and pulls in only `System.Text.Json`. The ASP.NET package targets `net8.0`.

## Option A — ASP.NET Core (recommended)

```csharp
using Mobizon.Net.Webhooks.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMobizonWebhooks(o =>
    // single shared secret: ignore the event arg.
    // Per-webhook secrets: switch on evt.WebhookId here.
    o.SecretKeyResolver = (sp, evt) => builder.Configuration["Mobizon:WebhookSecret"]!);

var app = builder.Build();

app.MapMobizonWebhook("/webhooks/mobizon", async (evt, ct) =>
{
    // Verified & parsed. Acknowledge fast; do heavy work asynchronously.
    switch (evt)
    {
        case SmsDeliveryReportEvent sms:
            // sms.Data.MessageId, sms.Data.Status (SmsStatus), sms.Data.To
            await queue.EnqueueAsync(sms.Data.MessageId, sms.Data.Status, ct);
            break;
        case FormSubmissionEvent form:
            // form.Data.FormId, form.Data.Items[]
            break;
        case UnknownWebhookEvent unknown:
            logger.LogInformation("Unhandled webhook type {Type}", unknown.EventTypeRaw);
            break;
    }
});
// 200 = handled, 403 = bad signature, 400 = malformed — returned automatically.

app.Run();
```

## Option B — Any HTTP stack (core primitives)

```csharp
using Mobizon.Net.Webhooks;            // WebhookProcessor
using Mobizon.Contracts.Models.Webhooks; // WebhookProcessResult, WebhookProcessStatus, event types

var processor = new WebhookProcessor(); // default parser + verifier

// 'body' = raw request body string; 'secret' = your webhook secret key
WebhookProcessResult result = processor.Process(body, secret);
// Per-webhook secrets: processor.Process(body, evt => LookupSecret(evt.WebhookId));

return result.Status switch
{
    WebhookProcessStatus.Ok                => Handle(result.Event!),  // 200
    WebhookProcessStatus.SignatureMismatch => StatusCode(403),
    WebhookProcessStatus.ParseError        => StatusCode(400),
    _                                      => StatusCode(400),
};
```

### Verify only (e.g. custom pipeline)

```csharp
var verifier = new WebhookSignatureVerifier();
bool ok = verifier.Verify(eventId, attempt, eventCreateTsRaw, sign, secret);
```

## Idempotency

Mobizon retries up to 10 times. Deduplicate on `evt.EventId` (stable across retries); `evt.Attempt` tells you which retry this is.

```csharp
if (!await seen.TryAddAsync(evt.EventId)) return Results.Ok(); // already processed
```

## Acknowledge fast

Mobizon treats a webhook as failed if no `2xx` arrives within 5 seconds. Verify → parse → persist/enqueue → return `200`, then process asynchronously. Do not run long work inside the handler.

## Validation checklist (maps to spec Success Criteria)

- [ ] Official `sms-delivery-report` sample parses with all fields (SC-002)
- [ ] Tampered signature → 403 / `SignatureMismatch` (SC-003)
- [ ] Correct signature → 200 / `Ok` (SC-003)
- [ ] Unknown `eventType` → `UnknownWebhookEvent`, no throw (SC-004)
- [ ] `confirmationTs: ""` → `null` (FR-020)
- [ ] No web-framework dependency required for Option B (SC-006)
