using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Alphanames;
using Mobizon.Contracts.Models.Common;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class AlphanameServiceTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        { ApiKey = "test-key", ApiUrl = "https://api.mobizon.kz" };

        private AlphanameService CreateService(MockHttpMessageHandler m)
            => new AlphanameService(new MobizonApiClient(m.ToHttpClient(), _options));

        [Fact]
        public async Task ListAsync_Parses_Fixture()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/alphaname/list")
                .Respond("application/json", Fixtures.Load("alphaname.list.json"));

            var result = await CreateService(mockHttp).ListAsync();

            Assert.Equal(1, result.TotalItemCount);
            Assert.Single(result.Items);
            var item = result.Items[0];
            Assert.Equal(7719L, item.Id);
            Assert.Equal(58356L, item.AlphanameId);
            Assert.Equal(58356L, item.Alphaname!.Id);
            Assert.Equal("Profit", item.Alphaname!.Name);
            Assert.Equal(1, item.GlobalStatus);
            Assert.True(item.IsDefault);
            Assert.Equal(new System.DateTime(2026, 1, 2, 3, 4, 5), item.Created);
            Assert.Equal(new System.DateTime(2026, 1, 2, 3, 4, 5), item.Alphaname!.Created);
        }
    }
}
