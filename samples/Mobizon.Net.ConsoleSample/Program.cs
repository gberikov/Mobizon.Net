using System;
using System.Threading.Tasks;
using Mobizon.Contracts.Exceptions;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Message;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var apiKey = Environment.GetEnvironmentVariable("MOBIZON_API_KEY");
            var apiUrl = Environment.GetEnvironmentVariable("MOBIZON_API_URL")
                         ?? "https://api.mobizon.kz";

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Set MOBIZON_API_KEY environment variable to run this sample.");
                Console.WriteLine("Optionally set MOBIZON_API_URL (default: https://api.mobizon.kz)");
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
                    Recipient = "77001234567",
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
