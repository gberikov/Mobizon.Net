using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Contracts;
using Mobizon.Net;
using Mobizon.Net.ConsoleSample.Samples;
using Mobizon.Net.Extensions.DependencyInjection;
using Mobizon.Net.Extensions.Polly;

namespace Mobizon.Net.ConsoleSample
{
    /// <summary>
    /// Usage: dotnet run -- &lt;command&gt; [args]
    /// Configure Mobizon:ApiKey / Mobizon:ApiUrl in appsettings.Development.json or env vars Mobizon__ApiKey / Mobizon__ApiUrl.
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
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
                Console.WriteLine("API key is not configured. Set Mobizon:ApiKey in appsettings.Development.json or Mobizon__ApiKey env var.");
                return 2;
            }

            using var client = new MobizonClient(new MobizonClientOptions { ApiKey = apiKey, ApiUrl = apiUrl });

            var commands = new Dictionary<string, Func<string[], Task>>(StringComparer.OrdinalIgnoreCase)
            {
                ["balance"]            = _ => UserSamples.GetBalanceAsync(client),
                ["send"]               = a => MessageSamples.QuickSendAsync(client, a.ElementAtOrDefault(0) ?? testRecipient, a.ElementAtOrDefault(1) ?? testMessage),
                ["send-full"]          = a => MessageSamples.SendSmsMessageAsync(client, a.ElementAtOrDefault(0) ?? testRecipient, a.ElementAtOrDefault(1) ?? testMessage),
                ["sms-status"]         = _ => MessageSamples.GetStatusAsync(client),
                ["sms-list"]           = _ => MessageSamples.ListAsync(client),
                ["campaign-list"]      = _ => CampaignSamples.ListAsync(client),
                ["campaign-get"]       = _ => CampaignSamples.GetAsync(client),
                ["campaign-info"]      = _ => CampaignSamples.GetInfoAsync(client),
                ["campaign-flow"]      = _ => CampaignSamples.CreateSendDeleteAsync(client),
                ["campaign-recipients"]= _ => CampaignSamples.AddRecipientsAsync(client),
                ["link-list"]          = _ => LinkSamples.ListAsync(client),
                ["link-flow"]          = _ => LinkSamples.CreateGetUpdateDeleteAsync(client),
                ["link-stats"]         = _ => LinkSamples.GetStatsAsync(client),
                ["group-list"]         = _ => ContactGroupSamples.ListAsync(client),
                ["group-flow"]         = _ => ContactGroupSamples.CreateUpdateDeleteAsync(client),
                ["group-count"]        = _ => ContactGroupSamples.GetCardsCountAsync(client),
                ["card-list"]          = _ => ContactCardSamples.ListAsync(client),
                ["card-by-group"]      = _ => ContactCardSamples.ListByGroupAsync(client),
                ["card-get"]           = _ => ContactCardSamples.GetAsync(client),
                ["card-first"]         = _ => ContactCardSamples.FirstAndSingleAsync(client),
                ["card-page"]          = _ => ContactCardSamples.ToPageAsync(client),
                ["card-add-update"]    = _ => ContactCardSamples.AddAndUpdateAsync(client),
                ["card-groups"]        = _ => ContactCardSamples.GroupsAsync(client),
                ["card-remove"]        = _ => ContactCardSamples.RemoveAsync(client),
                ["stoplist"]           = _ => NumberStopListSamples.ListAsync(client),
                ["stoplist-add"]       = _ => NumberStopListSamples.AddNumberAsync(client),
                ["stoplist-add-range"] = _ => NumberStopListSamples.AddNumberRangeAsync(client),
                ["stoplist-delete"]    = _ => NumberStopListSamples.DeleteAsync(client),
                ["task"]               = a => TaskQueueSamples.GetStatusAsync(client, long.Parse(a.ElementAtOrDefault(0) ?? "0")),
                ["webhook"]            = a => { WebhookSamples.ProcessDeliveryReport(a.ElementAtOrDefault(0) ?? SampleWebhookBody, a.ElementAtOrDefault(1) ?? "your-webhook-secret"); return Task.CompletedTask; },
                ["di-balance"]         = _ => DiBalanceAsync(apiKey, apiUrl),
            };

            if (args.Length == 0 || !commands.TryGetValue(args[0], out var command))
            {
                Console.WriteLine("Usage: dotnet run -- <command> [args]");
                Console.WriteLine("Commands: " + string.Join(", ", commands.Keys.OrderBy(k => k)));
                return 1;
            }

            try
            {
                await command(args.Skip(1).ToArray());
                return 0;
            }
            catch (MobizonApiException ex)
            {
                Console.WriteLine($"[API Error {ex.RawCode}] {ex.ApiMessage}");
                return 3;
            }
            catch (MobizonException ex)
            {
                Console.WriteLine($"[Error{(ex.StatusCode.HasValue ? " HTTP " + (int)ex.StatusCode.Value : "")}] {ex.Message}");
                return 4;
            }
        }

        /// <summary>The DI route: AddMobizon + AddMobizonResilience, then resolve IMobizonClient (not disposable — the container owns it).</summary>
        private static async Task DiBalanceAsync(string apiKey, string apiUrl)
        {
            var services = new ServiceCollection();
            services.AddMobizon(o => { o.ApiKey = apiKey; o.ApiUrl = apiUrl; })
                    .AddMobizonResilience();

            await using var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMobizonClient>();

            var balance = await client.User.GetOwnBalanceAsync();
            Console.WriteLine($"=== DI: User.GetOwnBalance ===");
            Console.WriteLine($"Balance : {balance.Balance:0.0000} {balance.Currency}");
        }

        private const string SampleWebhookBody =
            "{\"eventId\":1,\"eventType\":\"sms-delivery-report\",\"eventCreateTs\":\"2026-01-15 11:42:28\",\"webhookId\":1,\"attempt\":1," +
            "\"data\":{\"campaignId\":1,\"messageId\":2,\"segNum\":1,\"status\":\"DELIVRD\",\"to\":\"77001234567\"},\"sign\":\"...\"}";
    }
}
