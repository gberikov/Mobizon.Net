using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;
using System.Collections.Generic;

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
                    "https://api.mobizon.kz/service/Message/SendSmsMessage")
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

            Assert.Equal(42, result.MessageId);
            Assert.Equal(1, result.CampaignId);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task QuickSendAsync_SendsRecipientAndText()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/Message/SendSmsMessage")
                .WithFormData("recipient", "77001234567")
                .WithFormData("text", "Hello world")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""campaignId"":1,""messageId"":42,""status"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.QuickSendAsync("77001234567", "Hello world");

            Assert.Equal(42, result.MessageId);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task QuickSendAsync_StripsNonDigitsFromRecipient()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/Message/SendSmsMessage")
                .WithFormData("recipient", "77001234567")
                .WithFormData("text", "Hi")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""campaignId"":1,""messageId"":55,""status"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            await service.QuickSendAsync("+7 (700) 123-45-67", "Hi");

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendSmsMessageAsync_WithOptionalParams_SendsAll()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/Message/SendSmsMessage")
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
                Parameters = new SmsMessageParameters
                {
                    Validity = TimeSpan.FromMinutes(60)
                }
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendSmsMessageAsync_WithDeferredTo_SendsFormattedTimestamp()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/Message/SendSmsMessage")
                .WithFormData("recipient", "77001234567")
                .WithFormData("text", "Hi")
                .WithFormData("params[deferredToTs]", "2026-02-25 10:30:00")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""campaignId"":2,""messageId"":20,""status"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            await service.SendSmsMessageAsync(new SendSmsMessageRequest
            {
                Recipient = "77001234567",
                Text = "Hi",
                Parameters = new SmsMessageParameters
                {
                    DeferredTo = new DateTime(2026, 2, 25, 10, 30, 0)
                }
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData(MessageClass.Flash, "0")]
        [InlineData(MessageClass.Normal, "1")]
        public async Task SendSmsMessageAsync_WithMessageClass_SendsIntValue(MessageClass messageClass, string expected)
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/Message/SendSmsMessage")
                .WithFormData("recipient", "77001234567")
                .WithFormData("text", "Hi")
                .WithFormData("params[mclass]", expected)
                .Respond("application/json",
                    @"{""code"":0,""data"":{""campaignId"":3,""messageId"":30,""status"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            await service.SendSmsMessageAsync(new SendSmsMessageRequest
            {
                Recipient = "77001234567",
                Text = "Hi",
                Parameters = new SmsMessageParameters
                {
                    MessageClass = messageClass
                }
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetSmsStatusAsync_SendsIdsArray()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/Message/GetSMSStatus")
                .WithFormData("ids[0]", "100")
                .WithFormData("ids[1]", "200")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""id"":""100"",""status"":""DELIVRD"",""segNum"":""1"",""startSendTs"":""2024-01-01 12:00:00"",""statusUpdateTs"":""2024-01-01 12:05:00""},{""id"":""200"",""status"":""NEW"",""segNum"":""1"",""startSendTs"":"" "",""statusUpdateTs"":"" ""}],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetSmsStatusAsync(new[] { 100L, 200L });

            Assert.Equal(2, result.Count);
            Assert.Equal(100L, result[0].Id);
            Assert.Equal(SmsStatus.Delivered, result[0].Status);
            Assert.Equal(new DateTime(2024, 1, 1, 12, 5, 0), result[0].StatusUpdated);
            Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), result[0].SendStarted);
            Assert.Null(result[1].StatusUpdated);
            Assert.Null(result[1].SendStarted);
            Assert.Equal(SmsStatus.New, result[1].Status);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithCriteria_SendsFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/Message/List")
                .WithFormData("criteria[from]", "Alpha")
                .WithFormData("criteria[status]", "DELIVRD")
                .WithFormData("pagination[currentPage]", "1")
                .WithFormData("pagination[pageSize]", "10")
                .WithFormData("sort[campaignId]", "DESC")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":1,""campaignId"":5,""segNum"":1,""segUserBuy"":0.06,""from"":""Alpha"",""to"":""77001234567"",""status"":""DELIVRD"",""text"":""Hi""}],""totalItemCount"":1},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync(new MessageListRequest
            {
                Criteria = new MessageListCriteria { From = "Alpha", Status = SmsStatus.Delivered },
                Pagination = new PaginationRequest { CurrentPage = 1, PageSize = 10 },
                Sort = new SortRequest { Field = "campaignId", Direction = SortDirection.DESC }
            });

            Assert.Single(result.Items);
            Assert.Equal("Alpha", result.Items[0].From);
            Assert.Equal(1, result.TotalItemCount);
            Assert.Equal(0.06m, result.Items[0].SegmentCost);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithoutRequest_SendsNoFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/Message/List")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[],""totalItemCount"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalItemCount);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendSmsMessageAsync_NullRequest_Throws()
        {
            var service = CreateService(new MockHttpMessageHandler());
            await Assert.ThrowsAsync<System.ArgumentNullException>(() => service.SendSmsMessageAsync(null!));
        }

        [Fact]
        public async Task SendSmsMessageAsync_Parses_LargeIds()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/SendSmsMessage")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""campaignId"":""70000000001"",""messageId"":""70000000002"",""status"":0},""message"":""""}");
            var result = await CreateService(mockHttp).SendSmsMessageAsync(new SendSmsMessageRequest { Recipient = "7700", Text = "x" });
            Assert.Equal(70000000001L, result.CampaignId);
            Assert.Equal(70000000002L, result.MessageId);
        }

        [Fact]
        public async Task ListAsync_WithScheduledStatus_SendsSchedulCode()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/List")
                .WithFormData("criteria[status]", "SCHEDUL")
                .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

            var result = await CreateService(mockHttp).ListAsync(new MessageListRequest
            {
                Criteria = new MessageListCriteria { Status = SmsStatus.Scheduled }
            });

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalItemCount);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_DateCriteria_AreGregorianRegardlessOfCurrentCulture()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/List")
                .WithFormData("criteria[startSendTsFrom]", "2026-03-01 10:20:30")
                .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

            var thai = new CultureInfo("th-TH");
            thai.DateTimeFormat.Calendar = new ThaiBuddhistCalendar(); // year 2026 renders as 2569 under this calendar
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = thai;
            try
            {
                await CreateService(mockHttp).ListAsync(new MessageListRequest
                {
                    Criteria = new MessageListCriteria { SentFrom = new DateTime(2026, 3, 1, 10, 20, 30) }
                });
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithNumberInfo_SendsFlagAsOneOrZero()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/List")
                .WithFormData("withNumberInfo", "1")
                .Respond("application/json", @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

            await CreateService(mockHttp).ListAsync(new MessageListRequest { WithNumberInfo = true });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendSmsMessageAsync_ShortenLinks_SendsParam()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/Message/SendSmsMessage")
                .WithFormData("params[shortenLinks]", "1")
                .Respond("application/json", Fixtures.Load("message.sendSmsMessage.json"));

            await CreateService(mockHttp).SendSmsMessageAsync(new SendSmsMessageRequest
            {
                Recipient = "77001234567",
                Text = "https://example.com/very/long",
                Parameters = new SmsMessageParameters { ShortenLinks = true }
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
