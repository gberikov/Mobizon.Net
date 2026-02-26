using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Exceptions;
using Mobizon.Contracts.Models.Common;
using Mobizon.Net.Internal;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    public class MobizonApiClientTests
    {
        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-api-key",
            ApiUrl = "https://api.mobizon.kz"
        };

        private MobizonApiClient CreateClient(MockHttpMessageHandler mockHttp)
        {
            var httpClient = mockHttp.ToHttpClient();
            return new MobizonApiClient(httpClient, _options);
        }

        [Fact]
        public async Task SendAsync_Post_BuildsCorrectUrl()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/message/sendsmsmessage")
                .WithQueryString("output", "json")
                .WithQueryString("api", "v1")
                .WithQueryString("apiKey", "test-api-key")
                .Respond("application/json",
                    @"{""code"":0,""data"":{},""message"":""""}");

            var client = CreateClient(mockHttp);
            await client.SendAsync<object>(
                HttpMethod.Post, "message", "sendsmsmessage", null);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendAsync_Post_SendsFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/message/sendsmsmessage")
                .WithFormData("recipient", "77001234567")
                .WithFormData("text", "Hello")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""messageId"":123},""message"":""""}");

            var client = CreateClient(mockHttp);
            var parameters = new Dictionary<string, string>
            {
                ["recipient"] = "77001234567",
                ["text"] = "Hello"
            };

            var result = await client.SendAsync<TestSendResult>(
                HttpMethod.Post, "message", "sendsmsmessage", parameters);

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(123, result.Data.MessageId);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendAsync_Get_DoesNotSendBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get,
                    "https://api.mobizon.kz/service/user/getownbalance")
                .WithQueryString("output", "json")
                .WithQueryString("api", "v1")
                .WithQueryString("apiKey", "test-api-key")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""balance"":""100.50"",""currency"":""KZT""},""message"":""""}");

            var client = CreateClient(mockHttp);
            var result = await client.SendAsync<TestBalanceResult>(
                HttpMethod.Get, "user", "getownbalance", null);

            Assert.Equal("100.50", result.Data.Balance);
            Assert.Equal("KZT", result.Data.Currency);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendAsync_ApiError_ThrowsMobizonApiException()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond("application/json",
                    @"{""code"":2,""data"":null,""message"":""Invalid API key""}");

            var client = CreateClient(mockHttp);

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() =>
                client.SendAsync<object>(
                    HttpMethod.Post, "message", "sendsmsmessage", null));

            Assert.Equal(MobizonResponseCode.NotFound, ex.Code);
            Assert.Equal(2, ex.RawCode);
            Assert.Equal("Invalid API key", ex.ApiMessage);
        }

        [Fact]
        public async Task SendAsync_BackgroundTask_ReturnsNormally()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond("application/json",
                    @"{""code"":100,""data"":{""taskId"":42},""message"":""""}");

            var client = CreateClient(mockHttp);
            var result = await client.SendAsync<TestTaskResult>(
                HttpMethod.Post, "campaign", "send", null);

            Assert.Equal(MobizonResponseCode.BackgroundTask, result.Code);
            Assert.Equal(42, result.Data.TaskId);
        }

        [Fact]
        public async Task SendAsync_NetworkError_ThrowsMobizonException()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("*")
                .Throw(new HttpRequestException("Connection refused"));

            var client = CreateClient(mockHttp);

            var ex = await Assert.ThrowsAsync<MobizonException>(() =>
                client.SendAsync<object>(
                    HttpMethod.Post, "message", "sendsmsmessage", null));

            Assert.IsNotType<MobizonApiException>(ex);
            Assert.IsType<HttpRequestException>(ex.InnerException);
        }

        [Fact]
        public async Task SendAsync_CancellationToken_IsPropagated()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("*")
                .Respond("application/json",
                    @"{""code"":0,""data"":{},""message"":""""}");

            var client = CreateClient(mockHttp);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                client.SendAsync<object>(
                    HttpMethod.Post, "message", "sendsmsmessage", null, cts.Token));
        }

        [Fact]
        public async Task SendAsync_UnknownErrorCode_ThrowsWithRawCode()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond("application/json",
                    @"{""code"":999,""data"":null,""message"":""Unknown error""}");

            var client = CreateClient(mockHttp);

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() =>
                client.SendAsync<object>(
                    HttpMethod.Post, "message", "sendsmsmessage", null));

            Assert.Equal(999, ex.RawCode);
        }

        [Fact]
        public void Constructor_InvalidOptions_ThrowsArgumentException()
        {
            var options = new MobizonClientOptions();
            var httpClient = new HttpClient();

            Assert.Throws<ArgumentException>(() =>
                new MobizonApiClient(httpClient, options));
        }

        [Fact]
        public async Task SendAsync_FloatFieldAsString_IsDeserialized()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""segUserBuy"":""0.05""},""message"":""""}");

            var client = CreateClient(mockHttp);
            var result = await client.SendAsync<TestSegResult>(
                HttpMethod.Post, "message", "list", null);

            Assert.Equal(0.05f, result.Data.SegUserBuy, 4);
        }

        private class TestSegResult
        {
            public float SegUserBuy { get; set; }
        }

        private class TestSendResult
        {
            public int MessageId { get; set; }
        }

        private class TestBalanceResult
        {
            public string Balance { get; set; } = "";
            public string Currency { get; set; } = "";
        }

        private class TestTaskResult
        {
            public int TaskId { get; set; }
        }
    }
}
