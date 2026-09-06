# Webhooks

[← Back to the README](https://github.com/gberikov/Mobizon.Net/blob/master/README.md)

---

Mobizon can push events to your server in real time instead of you polling `GetSMSStatus`. Webhooks
are created and configured in the Mobizon **control panel** (not via the API); choose the **JSON**
data format when creating the webhook (this SDK does not parse the `raw`/`xml` formats); this SDK
provides the **receive** side: verifying the SHA1 signature and parsing the JSON body into a typed event.

Supported event types: `sms-delivery-report`, `form-submission`, `form-contact-confirmation`,
`form-contact-unsubscribe`. Unrecognised types are surfaced as `UnknownWebhookEvent` (forward-compatible).

## Framework-agnostic core

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

## ASP.NET Core

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

