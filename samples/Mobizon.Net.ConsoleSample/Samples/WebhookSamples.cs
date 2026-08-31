using System;
using Mobizon.Contracts.Webhooks;
using Mobizon.Net.Webhooks;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class WebhookSamples
    {
        // Receive + verify + parse an sms-delivery-report webhook using the framework-agnostic core.
        // 'secret' is the secret key you set when creating the webhook in the Mobizon control panel.
        public static void ProcessDeliveryReport(string body, string secret)
        {
            Console.WriteLine("=== Webhooks.ProcessDeliveryReport (core primitives) ===");

            var processor = new WebhookProcessor(); // default parser + verifier
            WebhookProcessResult result = processor.Process(body, secret);

            switch (result.Status)
            {
                case WebhookProcessStatus.Ok:
                    if (result.Event is SmsDeliveryReportEvent sms)
                    {
                        Console.WriteLine($"Verified. EventId={sms.EventId} Attempt={sms.Attempt}");
                        Console.WriteLine($"  MessageId : {sms.Data.MessageId}");
                        Console.WriteLine($"  Status    : {sms.Data.Status} (raw: {sms.Data.StatusRaw})");
                        Console.WriteLine($"  To        : {sms.Data.To}");
                        // In a real app: enqueue/save here and return 200 quickly, then process asynchronously.
                    }
                    break;
                case WebhookProcessStatus.SignatureMismatch:
                    Console.WriteLine("Rejected: signature mismatch (respond HTTP 403).");
                    break;
                case WebhookProcessStatus.ParseError:
                    Console.WriteLine("Rejected: malformed body (respond HTTP 400).");
                    break;
            }

            // ── ASP.NET Core path (in a web app) ──────────────────────────────────
            // Install Mobizon.Net.Webhooks.AspNetCore, then:
            //
            //   builder.Services.AddMobizonWebhooks(o =>
            //       o.SecretKeyResolver = (sp, evt) => config["Mobizon:WebhookSecret"]!);
            //
            //   app.MapMobizonWebhook("/webhooks/mobizon", async (evt, ct) =>
            //   {
            //       if (evt is SmsDeliveryReportEvent r) { /* enqueue r.Data */ }
            //   });
            // 200 = handled, 403 = bad signature, 400 = malformed — returned automatically.
        }
    }
}
