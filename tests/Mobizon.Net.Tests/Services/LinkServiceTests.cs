using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.Links;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class LinkServiceTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-key",
            ApiUrl = "https://api.mobizon.kz"
        };

        private LinkService CreateService(MockHttpMessageHandler mockHttp)
        {
            var httpClient = mockHttp.ToHttpClient();
            var apiClient = new MobizonApiClient(httpClient, _options);
            return new LinkService(apiClient);
        }

        [Fact]
        public async Task CreateAsync_SendsCorrectParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/create")
                .WithFormData("data[fullLink]", "https://example.com")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""id"":1,""code"":""abc123"",""fullLink"":""https://example.com"",""status"":1,""clickCnt"":""0""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.CreateAsync(new CreateLinkRequest
            {
                FullLink = "https://example.com"
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(1, result.Data.Id);
            Assert.Equal("abc123", result.Data.Code);
            Assert.Equal("https://example.com", result.Data.FullLink);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CreateAsync_WithOptionalParams_SendsAll()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/create")
                .WithFormData("data[fullLink]", "https://example.com")
                .WithFormData("data[status]", "1")
                .WithFormData("data[expirationDate]", "2025-12-31")
                .WithFormData("data[comment]", "Test link")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""id"":1,""code"":""abc123"",""fullLink"":""https://example.com"",""status"":1,""expirationDate"":""2025-12-31"",""comment"":""Test link"",""clicks"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            await service.CreateAsync(new CreateLinkRequest
            {
                FullLink = "https://example.com",
                Status = 1,
                ExpirationDate = "2025-12-31",
                Comment = "Test link"
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task DeleteAsync_SendsIdsArray()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/delete")
                .WithFormData("ids[0]", "10")
                .WithFormData("ids[1]", "20")
                .Respond("application/json",
                    @"{""code"":0,""data"":{},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.DeleteAsync(new[] { 10, 20 });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetByCodeAsync_SendsCode()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/get")
                .WithFormData("code", "abc123")
                .Respond("application/json", @"{""code"":0,""data"":{""id"":""1"",""code"":""abc123"",""fullLink"":""https://e.com"",""status"":""1"",""clickCnt"":""5""},""message"":""""}");
            var result = await CreateService(mockHttp).GetByCodeAsync("abc123");
            Assert.Equal(5, result.Data.ClickCnt);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetByIdAsync_SendsId()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/get")
                .WithFormData("id", "42")
                .Respond("application/json", @"{""code"":0,""data"":{""id"":""42"",""code"":""x"",""fullLink"":""https://e.com"",""status"":""1"",""clickCnt"":""0""},""message"":""""}");
            var result = await CreateService(mockHttp).GetByIdAsync(42);
            Assert.Equal(42, result.Data.Id);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetByShortLinkAsync_SendsShortLink()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/get")
                .WithFormData("shortLink", "https://mbzn.co/x")
                .Respond("application/json", @"{""code"":0,""data"":{""id"":""1"",""code"":""x"",""fullLink"":""https://e.com"",""status"":""1"",""clickCnt"":""0""},""message"":""""}");
            var result = await CreateService(mockHttp).GetByShortLinkAsync("https://mbzn.co/x");
            Assert.Equal(MobizonResponseCode.Success, result.Code);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetLinksAsync_SendsCampaignId()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/getlinks")
                .WithFormData("campaignId", "42")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""id"":1,""code"":""abc"",""fullLink"":""https://example.com"",""status"":1,""clicks"":3},{""id"":2,""code"":""def"",""fullLink"":""https://example.org"",""status"":1,""clicks"":7}],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetLinksAsync(42);

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal("abc", result.Data[0].Code);
            Assert.Equal("def", result.Data[1].Code);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetStatsAsync_SendsCorrectParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/getstats")
                .WithFormData("ids[0]", "1")
                .WithFormData("ids[1]", "2")
                .WithFormData("type", "daily")
                .WithFormData("criteria[dateFrom]", "2025-01-01")
                .WithFormData("criteria[dateTo]", "2025-01-31")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""linkId"":1,""date"":""2025-01-01"",""clicks"":10},{""linkId"":2,""date"":""2025-01-01"",""clicks"":20}],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetStatsAsync(new GetLinkStatsRequest
            {
                Ids = new[] { 1, 2 },
                Type = LinkStatsType.Daily,
                DateFrom = "2025-01-01",
                DateTo = "2025-01-31"
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal(10, result.Data[0].Clicks);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetStatsAsync_MonthlyType_SendsMonthly()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/getstats")
                .WithFormData("ids[0]", "5")
                .WithFormData("type", "monthly")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""linkId"":5,""date"":""2025-01"",""clicks"":100}],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetStatsAsync(new GetLinkStatsRequest
            {
                Ids = new[] { 5 },
                Type = LinkStatsType.Monthly
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Single(result.Data);
            Assert.Equal(100, result.Data[0].Clicks);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithPaginationAndSort_SendsFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/list")
                .WithFormData("pagination[currentPage]", "1")
                .WithFormData("pagination[pageSize]", "10")
                .WithFormData("sort[id]", "DESC")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""1"",""code"":""abc"",""fullLink"":""https://example.com"",""status"":""1"",""clickCnt"":""0""}],""totalItemCount"":""1""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync(new LinkListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 1, PageSize = 10 },
                Sort = new SortRequest { Field = "id", Direction = SortDirection.DESC }
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Single(result.Data.Items);
            Assert.Equal(1, result.Data.TotalItemCount);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithoutRequest_SendsNoFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/list")
                .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Empty(result.Data.Items);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_Parses_Real_Fixture_Shape()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/link/list")
                .Respond("application/json", Fixtures.Load("link.list.json"));

            var service = CreateService(mockHttp);
            var result = await service.ListAsync(new LinkListRequest());

            Assert.Equal(2, result.Data.TotalItemCount);
            Assert.Equal(2, result.Data.Items.Count);
            Assert.Equal("tyz2", result.Data.Items[0].Code);
            Assert.Equal(1, result.Data.Items[1].ClickCnt); // second link has clickCnt "1"
        }

        [Fact]
        public async Task UpdateAsync_SendsIdAndFields_NoFullLink()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/link/update")
                .WithFormData("id", "42")
                .WithFormData("data[status]", "0")
                .WithFormData("data[comment]", "c")
                .Respond("application/json", @"{""code"":0,""data"":true,""message"":""""}");
            var result = await CreateService(mockHttp).UpdateAsync(new UpdateLinkRequest { Id = 42, Status = 0, Comment = "c" });
            Assert.Equal(MobizonResponseCode.Success, result.Code);
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
