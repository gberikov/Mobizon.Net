using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class UserServiceTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-key",
            ApiUrl = "https://api.mobizon.kz"
        };

        [Fact]
        public async Task GetOwnBalanceAsync_PostsWithApiKeyInBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/user/getownbalance")
                .WithFormData("apiKey", "test-key")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""balance"":""4043.0656"",""currency"":""KZT""},""message"":""""}");

            var apiClient = new MobizonApiClient(mockHttp.ToHttpClient(), _options);
            var service = new UserService(apiClient);
            var result = await service.GetOwnBalanceAsync();

            Assert.Equal(4043.0656m, result.Balance);
            Assert.Equal("KZT", result.Currency);
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
