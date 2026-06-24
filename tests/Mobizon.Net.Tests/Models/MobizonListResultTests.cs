using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.Messages;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Models
{
    public class MobizonListResultTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey  = "test-key",
            ApiUrl  = "https://api.mobizon.kz"
        };

        private MessageService CreateService(MockHttpMessageHandler mockHttp)
        {
            var apiClient = new MobizonApiClient(mockHttp.ToHttpClient(), _options);
            return new MessageService(apiClient);
        }

        [Fact]
        public async Task ListAsync_DeserializesStringTotalItemCount()
        {
            // The real API returns totalItemCount as a JSON STRING (e.g. "0").
            // The globally-registered StringToIntConverter must handle that transparently.
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/Message/List")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result  = await service.ListAsync();

            Assert.NotNull(result.Data);
            Assert.NotNull(result.Data.Items);
            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.TotalItemCount);

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
