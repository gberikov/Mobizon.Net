using System;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net;
using Mobizon.Net.ConsoleSample.Samples;
using Microsoft.Extensions.Configuration;

namespace Mobizon.Net.ConsoleSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                              ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var apiKey        = configuration["Mobizon:ApiKey"];
            var apiUrl        = configuration["Mobizon:ApiUrl"] ?? "https://api.mobizon.kz";
            var testRecipient = configuration["Mobizon:TestRecipient"] ?? "";
            var testMessage   = configuration["Mobizon:TestMessage"]   ?? "Hello from Mobizon.Net SDK!";

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API key is not configured. Set it in:");
                Console.WriteLine($"  appsettings.{environment}.json  ->  Mobizon:ApiKey");
                Console.WriteLine("  or environment variable: Mobizon__ApiKey");
                return;
            }

            using var client = new MobizonClient(new MobizonClientOptions
            {
                ApiKey = apiKey,
                ApiUrl = apiUrl
            });

            try
            {
                // ── User ──────────────────────────────────────────────────────────
                // await UserSamples.GetBalanceAsync(client);

                // ── Message ───────────────────────────────────────────────────────
                // await MessageSamples.QuickSendAsync(client, testRecipient, testMessage);
                // await MessageSamples.SendSmsMessageAsync(client, testRecipient, testMessage);
                // await MessageSamples.GetStatusAsync(client);
                // await MessageSamples.ListAsync(client);

                // ── Campaign ──────────────────────────────────────────────────────
                // await CampaignSamples.ListAsync(client);
                // await CampaignSamples.GetAsync(client);
                // await CampaignSamples.GetInfoAsync(client);
                // await CampaignSamples.CreateSendDeleteAsync(client);
                // await CampaignSamples.AddRecipientsAsync(client);

                // ── Link ──────────────────────────────────────────────────────────
                // await LinkSamples.ListAsync(client);
                // await LinkSamples.CreateGetUpdateDeleteAsync(client);
                // await LinkSamples.GetStatsAsync(client);

                // ── Contact Groups ────────────────────────────────────────────────
                // await ContactGroupSamples.ListAsync(client);
                // await ContactGroupSamples.CreateUpdateDeleteAsync(client);
                // await ContactGroupSamples.GetCardsCountAsync(client);

                // ── Contact Cards ─────────────────────────────────────────────────
                // await ContactCardSamples.ListAsync(client);
                // await ContactCardSamples.ListByGroupAsync(client);
                // await ContactCardSamples.GetAsync(client);
                // await ContactCardSamples.CreateAndSetGroupAsync(client);
                // await ContactCardSamples.UpdateAsync(client);
                // await ContactCardSamples.GetGroupsAsync(client);

                // ── Number Stop List ──────────────────────────────────────────────
                // await NumberStopListSamples.ListAsync(client);
                // await NumberStopListSamples.AddNumberAsync(client);
                // await NumberStopListSamples.AddNumberRangeAsync(client);
                // await NumberStopListSamples.DeleteAsync(client);

                // ── Task Queue ────────────────────────────────────────────────────
                // await TaskQueueSamples.GetStatusAsync(client, taskId: 0);

                // ── Webhooks (incoming; no API call) ──────────────────────────────
                // var sampleBody = "{\"eventId\":1,\"eventType\":\"sms-delivery-report\",\"eventCreateTs\":\"2026-01-15 11:42:28\",\"webhookId\":1,\"attempt\":1,\"data\":{\"campaignId\":1,\"messageId\":2,\"segNum\":1,\"status\":\"DELIVRD\",\"to\":\"77001234567\"},\"sign\":\"...\"}";
                // WebhookSamples.ProcessDeliveryReport(sampleBody, "your-webhook-secret");

                Console.WriteLine("Uncomment a block in Program.cs to run a sample.");
            }
            catch (MobizonApiException ex)
            {
                Console.WriteLine($"[API Error {ex.RawCode}] {ex.ApiMessage}");
            }
            catch (MobizonException ex)
            {
                Console.WriteLine($"[Error] {ex.Message}");
            }
        }
    }
}
