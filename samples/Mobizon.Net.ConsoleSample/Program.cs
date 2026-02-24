using System;
using System.Threading.Tasks;
using Mobizon.Contracts.Exceptions;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Message;
using Mobizon.Net;
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

            var apiKey = configuration["Mobizon:ApiKey"];
            var apiUrl = configuration["Mobizon:ApiUrl"] ?? "https://api.mobizon.kz";

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API key is not configured. Please set it in one of:");
                Console.WriteLine($"  - appsettings.{environment}.json -> Mobizon:ApiKey");
                Console.WriteLine("  - Environment variable: MOBIZON__ApiKey");
                return;
            }

            var options = new MobizonClientOptions
            {
                ApiKey = apiKey,
                ApiUrl = apiUrl
            };

            using var client = new MobizonClient(options);

            try
            {
                // Check balance
                Console.WriteLine("Checking balance...");
                var balance = await client.User.GetOwnBalanceAsync();
                Console.WriteLine($"Balance: {balance.Data.Balance} {balance.Data.Currency}");

                // Send SMS
                Console.WriteLine("\nSending SMS...");
                var smsResult = await client.Messages.SendSmsMessageAsync(new SendSmsMessageRequest
                {
                    Recipient = "77017221502",
                    Text = "Hello from Mobizon.Net SDK!"
                });
                Console.WriteLine($"Message sent. ID: {smsResult.Data.MessageId}, " +
                                  $"Campaign: {smsResult.Data.CampaignId}");

                // Check status
                Console.WriteLine("\nChecking message status...");
                var status = await client.Messages.GetSmsStatusAsync(
                    new[] { smsResult.Data.MessageId });
                foreach (var s in status.Data)
                {
                    Console.WriteLine($"  Message {s.Id}: status={s.Status}, segments={s.SegNum}");
                }
            }
            catch (MobizonApiException ex)
            {
                Console.WriteLine($"API Error {ex.RawCode}: {ex.ApiMessage}");
            }
            catch (MobizonException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
