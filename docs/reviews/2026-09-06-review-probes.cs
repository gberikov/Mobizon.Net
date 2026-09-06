// Review probes, intentionally outside the test project. Copy into Mobizon.Net.Tests to reproduce.
// Eight assertions expose current defects; the response-disposal control test passes.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Mobizon.Net.ApiCapture;
using Microsoft.Extensions.DependencyInjection;
using Mobizon.Contracts;
using Mobizon.Net.Extensions.DependencyInjection;
using Mobizon.Net.Extensions.Polly;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests
{
    public class ReviewProbeTests
    {
        [Fact]
        public void Sanitizer_RemovesLatinPersonalDataAndOtp()
        {
            var scrubbed = Sanitizer.Scrub("{\"email\":\"alice@example.com\",\"text\":\"Your OTP is 8421\"}");
            Assert.DoesNotContain("alice@example.com", scrubbed);
            Assert.DoesNotContain("8421", scrubbed);
        }

        [Fact]
        public void Sanitizer_PreservesValidNumericJson()
        {
            using var document = JsonDocument.Parse(Sanitizer.Scrub("{\"id\":12345678}"));
            Assert.Equal(JsonValueKind.Number, document.RootElement.GetProperty("id").ValueKind);
        }

        [Fact]
        public async Task ContactField_RejectsNonEmptyUnexpectedShape()
        {
            using var mock = new MockHttpMessageHandler();
            mock.When("https://api.mobizon.kz/service/contactcard/get")
                .Respond("application/json", "{\"code\":0,\"data\":{\"id\":1,\"fields\":{\"email\":\"alice@example.com\"}}}");
            await Assert.ThrowsAsync<MobizonException>(() => Client(mock).ContactCards.FindAsync(1));
        }

        private static MobizonClient Client(MockHttpMessageHandler mock) => new MobizonClient(
            mock.ToHttpClient(), new MobizonClientOptions { ApiKey = "test", ApiUrl = "https://api.mobizon.kz" });

        [Fact]
        public async Task CampaignGroups_AcceptsDocumentedCsv()
        {
            using var mock = new MockHttpMessageHandler();
            mock.When("https://api.mobizon.kz/service/Campaign/Get")
                .Respond("application/json", "{\"code\":0,\"data\":{\"id\":1,\"groups\":\"12,34\"}}");
            var result = await Client(mock).Campaigns.GetAsync(1);
            Assert.Equal(new long[] { 12, 34 }, result.Groups);
        }

        [Fact]
        public async Task CampaignInfo_ExposesExtraFromRealCapture()
        {
            using var mock = new MockHttpMessageHandler();
            mock.When("https://api.mobizon.kz/service/Campaign/GetInfo")
                .Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Payloads", "campaign.getInfo.json")));
            var result = await Client(mock).Campaigns.GetInfoAsync(1);
            Assert.Equal(TimeSpan.FromHours(24), result.Validity);
            Assert.Equal(MessageClass.Normal, result.MessageClass);
            Assert.Equal(false, result.TrackShortLinkRecipients);
        }

        [Fact]
        public async Task CampaignList_PreservesDocumentedCounters()
        {
            using var mock = new MockHttpMessageHandler();
            mock.When("https://api.mobizon.kz/service/Campaign/List")
                .Respond("application/json", "{\"code\":0,\"data\":{\"items\":[{\"id\":1,\"counters\":{\"totalMsgNum\":7}}],\"totalItemCount\":1}}");
            var result = await Client(mock).Campaigns.ListAsync();
            var info = Assert.IsAssignableFrom<CampaignInfo>(result.Items[0]);
            Assert.Equal(7, info.Counters!.TotalMsgNum);
        }

        [Fact]
        public async Task Placeholder_CannotOverrideRecipient()
        {
            using var mock = new MockHttpMessageHandler();
            mock.When("https://api.mobizon.kz/service/Campaign/AddRecipients")
                .WithFormData("recipients[0][recipient]", "77001111111")
                .Respond("application/json", "{\"code\":0,\"data\":[]}");
            await Client(mock).Campaigns.AddRecipientsAsync(new AddRecipientsRequest
            {
                CampaignId = 1,
                Recipients = new[] { new RecipientEntry
                {
                    Recipient = "77001111111",
                    Placeholders = new Dictionary<string, string> { ["recipient"] = "77002222222" }
                }}
            });
        }

        [Fact]
        public async Task AddRecipients_RejectsMissingSuccessfulPayload()
        {
            using var mock = new MockHttpMessageHandler();
            mock.When("https://api.mobizon.kz/service/Campaign/AddRecipients")
                .Respond("application/json", "{\"code\":0,\"data\":null}");
            await Assert.ThrowsAsync<MobizonException>(() => Client(mock).Campaigns.AddRecipientsAsync(new AddRecipientsRequest
            {
                CampaignId = 1, Recipients = new[] { new RecipientEntry { Recipient = "77001111111" } }
            }));
        }

        [Fact]
        public async Task Retry_DisposesRejectedResponses()
        {
            using var handler = new TrackingHandler();
            var services = new ServiceCollection();
            services.AddMobizon(o => { o.ApiKey = "test"; o.ApiUrl = "https://api.mobizon.kz"; })
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddMobizonResilience(o => { o.RetryCount = 2; o.RetryBaseDelay = TimeSpan.Zero; });
            using var provider = services.BuildServiceProvider();
            await provider.GetRequiredService<IMobizonClient>().User.GetOwnBalanceAsync();
            Assert.All(handler.FailedContents, content => Assert.True(content.Disposed));
        }

        private sealed class TrackingHandler : HttpMessageHandler
        {
            public List<TrackingContent> FailedContents { get; } = new List<TrackingContent>();
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                if (FailedContents.Count < 2)
                {
                    var content = new TrackingContent();
                    FailedContents.Add(content);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = content });
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("{\"code\":0,\"data\":{\"balance\":1,\"currency\":\"KZT\"}}") });
            }
        }

        private sealed class TrackingContent : StringContent
        {
            public bool Disposed { get; private set; }
            public TrackingContent() : base("temporary failure") { }
            protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        }
    }
}
