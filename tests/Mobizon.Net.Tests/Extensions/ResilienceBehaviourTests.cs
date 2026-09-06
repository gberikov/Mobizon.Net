using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Contracts;
using Mobizon.Net.Extensions.DependencyInjection;
using Mobizon.Net.Extensions.Polly;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Extensions
{
    /// <summary>R9 (multipart is never retried) and R11 (resilience options are validated up front).</summary>
    public class ResilienceBehaviourTests
    {
        private static IMobizonClient Build(MockHttpMessageHandler mockHttp, Action<MobizonResilienceOptions> configure)
        {
            var services = new ServiceCollection();
            services.AddMobizon(o => { o.ApiKey = "k"; o.ApiUrl = "https://api.mobizon.kz"; })
                .ConfigurePrimaryHttpMessageHandler(() => mockHttp)
                .AddMobizonResilience(configure);
            return services.BuildServiceProvider().GetRequiredService<IMobizonClient>();
        }

        [Fact]
        public async Task MultipartUpload_IsNotRetried_EvenWhenNonIdempotentRetryIsOn()
        {
            var mockHttp = new MockHttpMessageHandler();
            var request = mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/contactcard/create")
                .Respond(HttpStatusCode.ServiceUnavailable);
            var client = Build(mockHttp, o => { o.RetryNonIdempotentRequests = true; o.RetryBaseDelay = TimeSpan.Zero; });
            using var photo = new MemoryStream(new byte[] { 1, 2, 3 });

            await Assert.ThrowsAsync<MobizonException>(() =>
                client.ContactCards.AddAsync(new ContactCard { Name = "n", Photo = photo, PhotoFileName = "p.jpg" }));

            Assert.Equal(1, mockHttp.GetMatchCount(request));
        }

        [Fact]
        public async Task FormWrite_IsRetried_OnlyWhenNonIdempotentRetryIsOn()
        {
            var mockHttp = new MockHttpMessageHandler();
            var request = mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/Delete")
                .Respond(HttpStatusCode.ServiceUnavailable);
            var client = Build(mockHttp, o => { o.RetryNonIdempotentRequests = true; o.RetryCount = 2; o.RetryBaseDelay = TimeSpan.Zero; });

            await Assert.ThrowsAsync<MobizonException>(() => client.Campaigns.DeleteAsync(1));

            Assert.Equal(3, mockHttp.GetMatchCount(request));
        }

        [Fact]
        public async Task ApiRateLimitInsideHttp200_IsNotRetried()
        {
            var mockHttp = new MockHttpMessageHandler();
            var request = mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/user/getownbalance")
                .Respond("application/json", @"{""code"":30,""data"":null,""message"":""Too many requests""}");
            var client = Build(mockHttp, o => o.RetryBaseDelay = TimeSpan.Zero);

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() => client.User.GetOwnBalanceAsync());

            Assert.Equal(MobizonResponseCode.RateLimitExceeded, ex.Code);
            Assert.Equal(1, mockHttp.GetMatchCount(request));
        }

        [Theory]
        [InlineData(-1, 1000, 5, 30)]
        [InlineData(3, -1, 5, 30)]
        [InlineData(3, 1000, 0, 30)]
        [InlineData(3, 1000, 5, 0)]
        [InlineData(200, 1000, 5, 30)] // 1s * 2^199 overflows TimeSpan
        public void Options_OutOfRange_FailAtRegistration(int retries, int baseMs, int threshold, int breakSeconds)
        {
            var services = new ServiceCollection();
            var builder = services.AddHttpClient("Mobizon");

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddMobizonResilience(o =>
            {
                o.RetryCount = retries;
                o.RetryBaseDelay = TimeSpan.FromMilliseconds(baseMs);
                o.CircuitBreakerFailureThreshold = threshold;
                o.CircuitBreakerDuration = TimeSpan.FromSeconds(breakSeconds);
            }));
        }

        [Fact]
        public void Options_ZeroRetriesAndZeroDelay_AreValid()
        {
            new MobizonResilienceOptions { RetryCount = 0, RetryBaseDelay = TimeSpan.Zero }.Validate();
        }
    }
}
