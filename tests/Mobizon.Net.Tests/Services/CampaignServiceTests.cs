using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Campaign;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class CampaignServiceTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-key",
            ApiUrl = "https://api.mobizon.kz"
        };

        private CampaignService CreateService(MockHttpMessageHandler mockHttp)
        {
            var httpClient = mockHttp.ToHttpClient();
            var apiClient = new MobizonApiClient(httpClient, _options);
            return new CampaignService(apiClient);
        }

        [Fact]
        public async Task CreateAsync_SendsCorrectParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/campaign/create")
                .WithFormData("data[type]", "1")
                .WithFormData("data[from]", "Alpha")
                .WithFormData("data[text]", "Hello")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""campaignId"":5},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.CreateAsync(new CreateCampaignRequest
            {
                Type = 1,
                From = "Alpha",
                Text = "Hello"
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(5, result.Data.CampaignId);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task DeleteAsync_SendsCorrectParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/campaign/delete")
                .WithFormData("id", "10")
                .Respond("application/json",
                    @"{""code"":0,""data"":{},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.DeleteAsync(10);

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetAsync_SendsCorrectParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/campaign/get")
                .WithFormData("id", "7")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""id"":7,""type"":1,""from"":""Alpha"",""text"":""Hello"",""status"":2},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetAsync(7);

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(7, result.Data.Id);
            Assert.Equal(1, result.Data.Type);
            Assert.Equal("Alpha", result.Data.From);
            Assert.Equal("Hello", result.Data.Text);
            Assert.Equal(2, result.Data.Status);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetInfoAsync_SendsCorrectParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/campaign/getinfo")
                .WithFormData("id", "7")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""id"":7,""totalMessages"":100,""sent"":90,""delivered"":85,""failed"":5},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetInfoAsync(7);

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(7, result.Data.Id);
            Assert.Equal(100, result.Data.TotalMessages);
            Assert.Equal(90, result.Data.Sent);
            Assert.Equal(85, result.Data.Delivered);
            Assert.Equal(5, result.Data.Failed);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithPaginationAndSort_SendsFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/campaign/list")
                .WithFormData("pagination[currentPage]", "1")
                .WithFormData("pagination[pageSize]", "10")
                .WithFormData("sort[id]", "DESC")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""id"":1,""type"":1,""from"":""Alpha"",""text"":""Hi"",""status"":2}],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync(new CampaignListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 1, PageSize = 10 },
                Sort = new SortRequest { Field = "id", Direction = SortDirection.DESC }
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Single(result.Data);
            Assert.Equal(1, result.Data[0].Id);
            Assert.Equal("Alpha", result.Data[0].From);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithoutRequest_SendsNoFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/campaign/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":[],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Empty(result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendAsync_SendsCorrectParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/campaign/send")
                .WithFormData("id", "5")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""taskId"":42,""status"":1},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.SendAsync(5);

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(42, result.Data.TaskId);
            Assert.Equal(1, result.Data.Status);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task AddRecipientsAsync_SendsCorrectParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/campaign/addrecipients")
                .WithFormData("campaignId", "5")
                .WithFormData("type", "1")
                .WithFormData("data[0]", "77001234567")
                .WithFormData("data[1]", "77009876543")
                .Respond("application/json",
                    @"{""code"":0,""data"":{},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.AddRecipientsAsync(new AddRecipientsRequest
            {
                CampaignId = 5,
                Type = 1,
                Data = new List<string> { "77001234567", "77009876543" }
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
