#nullable enable

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Services;
using Mobizon.Net.Extensions.DependencyInjection;
using Mobizon.Net.Extensions.Polly;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Extensions
{
    public class IntegrationTests
    {
        [Fact]
        public void AddMobizon_WithAddMobizonResilience_ResolvesIMobizonClient()
        {
            var services = new ServiceCollection();

            services
                .AddMobizon(o =>
                {
                    o.ApiKey = "test-key";
                    o.ApiUrl = "https://api.mobizon.kz";
                })
                .AddMobizonResilience();

            var provider = services.BuildServiceProvider();
            var client = provider.GetService<IMobizonClient>();

            Assert.NotNull(client);
        }

        [Fact]
        public async Task AddMobizonResilience_RetryPolicy_RetriesOn503AndSucceeds()
        {
            const string successJson =
                @"{""code"":0,""data"":{""balance"":""100.50"",""currency"":""KZT""},""message"":""""}";

            var mockHttp = new MockHttpMessageHandler();

            // First call returns 503 (transient failure), second call returns 200 with success JSON.
            mockHttp
                .Expect(HttpMethod.Get, "https://api.mobizon.kz/service/user/getownbalance")
                .Respond(HttpStatusCode.ServiceUnavailable);

            mockHttp
                .Expect(HttpMethod.Get, "https://api.mobizon.kz/service/user/getownbalance")
                .Respond("application/json", successJson);

            var services = new ServiceCollection();

            services
                .AddMobizon(o =>
                {
                    o.ApiKey = "test-key";
                    o.ApiUrl = "https://api.mobizon.kz";
                })
                .AddMobizonResilience(o =>
                {
                    // Use a single retry with no delay so the test runs fast.
                    o.RetryCount = 1;
                    o.RetryBaseDelay = System.TimeSpan.Zero;
                    // Raise the circuit-breaker threshold so it never trips during this test.
                    o.CircuitBreakerFailureThreshold = 10;
                })
                .ConfigurePrimaryHttpMessageHandler(() => mockHttp);

            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMobizonClient>();

            Assert.NotNull(client);

            var result = await client.User.GetOwnBalanceAsync();

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal("100.50", result.Data.Balance);
            Assert.Equal("KZT", result.Data.Currency);

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
