using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Campaigns;
using Mobizon.Contracts.Models.Common;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class CampaignServiceTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        { ApiKey = "test-key", ApiUrl = "https://api.mobizon.kz" };

        private CampaignService CreateService(MockHttpMessageHandler mockHttp)
            => new CampaignService(new MobizonApiClient(mockHttp.ToHttpClient(), _options));

        [Fact]
        public async Task ListAsync_Deserializes_Envelope()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/List")
                .WithFormData("criteria[type]", "2")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""123"",""type"":""2"",""text"":""hi""}],""totalItemCount"":""1""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync(new CampaignListRequest
            {
                Criteria = new CampaignCriteria { Type = 2 }
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Single(result.Data.Items);
            Assert.Equal(1, result.Data.TotalItemCount);
            Assert.Equal(123, result.Data.Items[0].Id);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CreateAsync_ReturnsCampaignId_FromScalar()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/Create")
                .WithFormData("data[type]", "2").WithFormData("data[text]", "hi")
                .Respond("application/json", @"{""code"":0,""data"":""123456"",""message"":""""}");
            var result = await CreateService(mockHttp).CreateAsync(new CreateCampaignRequest { Type = CampaignType.Bulk, Text = "hi" });
            Assert.Equal(123456, result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendAsync_ReturnsScalar()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/Send")
                .WithFormData("id", "5")
                .Respond("application/json", @"{""code"":0,""data"":2,""message"":""""}");
            var result = await CreateService(mockHttp).SendAsync(5);
            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(2, result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetInfoAsync_Parses_Counters_And_Currency()
        {
            const string json = @"{""code"":0,""data"":{""id"":""123"",""counters"":{""campaignId"":""123"",""totalSegNum"":""1"",""totalCost"":""16.2000"",""userCurrency"":""KZT""}},""message"":""""}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/GetInfo")
                .Respond("application/json", json);

            var result = await CreateService(mockHttp).GetInfoAsync(123);

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.NotNull(result.Data.Counters);
            Assert.Equal(1, result.Data.Counters!.TotalSegNum);
            Assert.Equal(16.2000m, result.Data.Counters.TotalCost);
            Assert.Equal("KZT", result.Data.Counters.UserCurrency);
            Assert.Equal(123, result.Data.Counters.CampaignId);
        }

        [Fact]
        public async Task AddRecipientsAsync_NoSource_Throws()
        {
            var service = CreateService(new MockHttpMessageHandler());
            await Assert.ThrowsAsync<System.ArgumentException>(() =>
                service.AddRecipientsAsync(new AddRecipientsRequest { CampaignId = 1 }));
        }

        [Fact]
        public async Task AddRecipientsAsync_MultipleSources_Throws()
        {
            var service = CreateService(new MockHttpMessageHandler());
            await Assert.ThrowsAsync<System.ArgumentException>(() =>
                service.AddRecipientsAsync(new AddRecipientsRequest
                {
                    CampaignId = 1,
                    Recipients = new[] { new RecipientEntry { Recipient = "77001112233" } },
                    RecipientGroups = new[] { "9" }
                }));
        }

        [Fact]
        public async Task AddRecipientsAsync_File_SendsMultipart()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Campaign/AddRecipients")
                .With(req => req.Content is System.Net.Http.MultipartFormDataContent)
                .Respond("application/json", @"{""code"":100,""data"":555,""message"":""""}");

            var service = CreateService(mockHttp);
            using var ms = new System.IO.MemoryStream(new byte[] { 1, 2, 3 });
            var result = await service.AddRecipientsAsync(new AddRecipientsRequest
            {
                CampaignId = 1,
                RecipientsFile = ms,
                RecipientsFileName = "recipients.csv"
            });

            Assert.Equal(555, result.Data.TaskId);
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
