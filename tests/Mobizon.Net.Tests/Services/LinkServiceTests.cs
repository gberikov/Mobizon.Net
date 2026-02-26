using System.Collections.Generic;
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
                    @"{""code"":0,""data"":{""id"":1,""code"":""abc123"",""fullLink"":""https://example.com"",""status"":1,""clicks"":0},""message"":""""}");

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
        public async Task GetAsync_SendsCodeParameter()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/get")
                .WithFormData("code", "abc123")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""id"":1,""code"":""abc123"",""fullLink"":""https://example.com"",""status"":1,""clicks"":5},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetAsync("abc123");

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal("abc123", result.Data.Code);
            Assert.Equal(5, result.Data.Clicks);
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
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/list")
                .WithFormData("pagination[currentPage]", "1")
                .WithFormData("pagination[pageSize]", "10")
                .WithFormData("sort[id]", "DESC")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""id"":1,""code"":""abc"",""fullLink"":""https://example.com"",""status"":1,""clicks"":0}],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync(new LinkListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 1, PageSize = 10 },
                Sort = new SortRequest { Field = "id", Direction = SortDirection.DESC }
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Single(result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithoutRequest_SendsNoFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":[],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Empty(result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task UpdateAsync_SendsCorrectParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/update")
                .WithFormData("code", "abc123")
                .WithFormData("data[fullLink]", "https://updated.com")
                .WithFormData("data[status]", "2")
                .WithFormData("data[comment]", "Updated comment")
                .Respond("application/json",
                    @"{""code"":0,""data"":{},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.UpdateAsync(new UpdateLinkRequest
            {
                Code = "abc123",
                FullLink = "https://updated.com",
                Status = 2,
                Comment = "Updated comment"
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task UpdateAsync_WithOnlyCode_SendsMinimalParams()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/link/update")
                .WithFormData("code", "xyz789")
                .Respond("application/json",
                    @"{""code"":0,""data"":{},""message"":""""}");

            var service = CreateService(mockHttp);
            await service.UpdateAsync(new UpdateLinkRequest
            {
                Code = "xyz789"
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
