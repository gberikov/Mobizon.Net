using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.Message;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class MessageServiceTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-key",
            ApiUrl = "https://api.mobizon.kz"
        };

        private MessageService CreateService(MockHttpMessageHandler mockHttp)
        {
            var httpClient = mockHttp.ToHttpClient();
            var apiClient = new MobizonApiClient(httpClient, _options);
            return new MessageService(apiClient);
        }

        [Fact]
        public async Task SendSmsMessageAsync_SendsCorrectParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/message/sendsmsmessage")
                .WithFormData("recipient", "77001234567")
                .WithFormData("text", "Hello world")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""campaignId"":1,""messageId"":42,""status"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.SendSmsMessageAsync(new SendSmsMessageRequest
            {
                Recipient = "77001234567",
                Text = "Hello world"
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(42, result.Data.MessageId);
            Assert.Equal(1, result.Data.CampaignId);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendSmsMessageAsync_WithOptionalParams_SendsAll()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/message/sendsmsmessage")
                .WithFormData("recipient", "77001234567")
                .WithFormData("text", "Hello")
                .WithFormData("from", "Alpha")
                .WithFormData("params[validity]", "60")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""campaignId"":1,""messageId"":10,""status"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            await service.SendSmsMessageAsync(new SendSmsMessageRequest
            {
                Recipient = "77001234567",
                Text = "Hello",
                From = "Alpha",
                Validity = 60
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetSmsStatusAsync_SendsIdsArray()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/message/getsmsstatus")
                .WithFormData("ids[0]", "100")
                .WithFormData("ids[1]", "200")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""id"":100,""status"":2,""segNum"":1,""startSendTs"":""2024-01-01 12:00:00""},{""id"":200,""status"":1,""segNum"":1,""startSendTs"":""2024-01-01 12:01:00""}],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetSmsStatusAsync(new[] { 100, 200 });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal(100, result.Data[0].Id);
            Assert.Equal(2, result.Data[0].Status);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithCriteria_SendsFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/message/list")
                .WithFormData("criteria[from]", "Alpha")
                .WithFormData("criteria[status]", "2")
                .WithFormData("pagination[currentPage]", "1")
                .WithFormData("pagination[pageSize]", "10")
                .WithFormData("sort[campaignId]", "DESC")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""id"":1,""campaignId"":5,""from"":""Alpha"",""status"":2,""text"":""Hi""}],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync(new MessageListRequest
            {
                From = "Alpha",
                Status = 2,
                Pagination = new PaginationRequest { CurrentPage = 1, PageSize = 10 },
                Sort = new SortRequest { Field = "campaignId", Direction = SortDirection.DESC }
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Single(result.Data);
            Assert.Equal("Alpha", result.Data[0].From);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithoutRequest_SendsNoFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/message/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":[],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Empty(result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
