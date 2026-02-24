using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class TaskQueueServiceTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-key",
            ApiUrl = "https://api.mobizon.kz"
        };

        [Fact]
        public async Task GetStatusAsync_SendsIdParameter()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/taskqueue/getstatus")
                .WithFormData("id", "42")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""id"":42,""status"":2,""progress"":100},""message"":""""}");

            var apiClient = new MobizonApiClient(mockHttp.ToHttpClient(), _options);
            var service = new TaskQueueService(apiClient);
            var result = await service.GetStatusAsync(42);

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(42, result.Data.Id);
            Assert.Equal(2, result.Data.Status);
            Assert.Equal(100, result.Data.Progress);
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
