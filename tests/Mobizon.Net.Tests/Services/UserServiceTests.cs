using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
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
        public async Task GetOwnBalanceAsync_UsesGetMethod()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get,
                    "https://api.mobizon.kz/service/user/getownbalance")
                .WithQueryString("output", "json")
                .WithQueryString("api", "v1")
                .WithQueryString("apiKey", "test-key")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""balance"":""4043.0656"",""currency"":""KZT""},""message"":""""}");

            var apiClient = new MobizonApiClient(mockHttp.ToHttpClient(), _options);
            var service = new UserService(apiClient);
            var result = await service.GetOwnBalanceAsync();

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal("4043.0656", result.Data.Balance);
            Assert.Equal("KZT", result.Data.Currency);
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
