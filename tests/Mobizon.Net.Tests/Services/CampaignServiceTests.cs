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
    }
}
