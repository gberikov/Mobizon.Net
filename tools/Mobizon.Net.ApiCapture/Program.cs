using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Mobizon.Net.ApiCapture
{
    internal static class Program
    {
        internal static async Task Main(string[] args)
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
            var testGroupId   = configuration["Mobizon:TestGroupId"] ?? "";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("API key is not configured. Set it in:");
                Console.WriteLine($"  appsettings.{environment}.json  ->  Mobizon:ApiKey");
                Console.WriteLine("  or environment variable: Mobizon__ApiKey");
                return;
            }

            // Parse --send flag
            bool send = false;
            foreach (var arg in args)
            {
                if (arg.Equals("--send", StringComparison.OrdinalIgnoreCase))
                {
                    send = true;
                    break;
                }
            }

            if (send && string.IsNullOrWhiteSpace(testRecipient))
            {
                Console.WriteLine("[capture] --send specified but Mobizon:TestRecipient is not configured — Tier 3 will be skipped");
            }

            var outDir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "api-captures");

            using var http = new HttpClient();
            var api = new RawMobizonApi(http, apiUrl, apiKey!);
            var runner = new CaptureRunner(api, outDir, testRecipient, testGroupId);

            Console.WriteLine($"[capture] Starting API capture. Output: {outDir}");
            Console.WriteLine($"[capture] ApiUrl: {apiUrl}");
            Console.WriteLine($"[capture] Send tier: {send}");

            await runner.RunAsync(send);

            Console.WriteLine("[capture] Done.");
        }
    }
}
