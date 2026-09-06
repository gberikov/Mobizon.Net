using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
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
        public async Task SendAsync_BuildsUrl_AndSendsApiKeyInBodyNotQuery()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/message/sendsmsmessage")
                .WithQueryString("output", "json")
                .WithQueryString("api", "v1")
                .WithFormData("apiKey", "test-api-key")
                .With(req => !req.RequestUri!.Query.Contains("apiKey"))
                .Respond("application/json",
                    @"{""code"":0,""data"":{},""message"":""""}");

            var client = CreateClient(mockHttp);
            await client.SendAsync<object>("message", "sendsmsmessage", null);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendAsync_SetsSdkUserAgent()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/message/sendsmsmessage")
                .With(req => req.Headers.UserAgent.Any(p => p.Product?.Name == "Mobizon.Net" && !string.IsNullOrEmpty(p.Product.Version)))
                .Respond("application/json", @"{""code"":0,""data"":{},""message"":""""}");

            await CreateClient(mockHttp).SendAsync<object>("message", "sendsmsmessage", null);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData("getSMSStatus", true)]
        [InlineData("get", true)]
        [InlineData("getInfo", true)]
        [InlineData("getlinks", true)]
        [InlineData("getstats", true)]
        [InlineData("list", true)]
        [InlineData("getownbalance", true)]
        [InlineData("getstatus", true)]
        [InlineData("getgroups", true)]
        [InlineData("getcardscount", true)]
        [InlineData("sendsmsmessage", false)]
        [InlineData("create", false)]
        [InlineData("delete", false)]
        [InlineData("send", false)]
        [InlineData("addrecipients", false)]
        [InlineData("update", false)]
        [InlineData("setgroups", false)]
        public void IsReadMethod_ClassifiesEveryApiMethod(string method, bool expected)
        {
            Assert.Equal(expected, MobizonApiClient.IsReadMethod(method));
        }

        [Fact]
        public async Task SendAsync_ReadMethod_IsMarkedIdempotent_WriteIsNot()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/user/getownbalance")
                .With(req => RequestMarkers.IsIdempotent(req))
                .Respond("application/json", @"{""code"":0,""data"":{},""message"":""""}");
            mockHttp.Expect(HttpMethod.Post, "https://api.mobizon.kz/service/message/sendsmsmessage")
                .With(req => !RequestMarkers.IsIdempotent(req))
                .Respond("application/json", @"{""code"":0,""data"":{},""message"":""""}");

            var client = CreateClient(mockHttp);
            await client.SendAsync<object>("user", "getownbalance", null);
            await client.SendAsync<object>("message", "sendsmsmessage", null);

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
                "message", "sendsmsmessage", parameters);

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(123, result.Data.MessageId);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendAsync_GetOwnBalance_IsPostWithApiKeyInBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    "https://api.mobizon.kz/service/user/getownbalance")
                .WithFormData("apiKey", "test-api-key")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""balance"":""100.50"",""currency"":""KZT""},""message"":""""}");

            var client = CreateClient(mockHttp);
            var result = await client.SendAsync<TestBalanceResult>("user", "getownbalance", null);

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
                    "message", "sendsmsmessage", null));

            Assert.Equal(MobizonResponseCode.NotFound, ex.Code);
            Assert.Equal(2, ex.RawCode);
            Assert.Equal("Invalid API key", ex.ApiMessage);
        }

        [Fact]
        public async Task SendAsync_BackgroundTask_ReturnsNormally_WhenOperationAcceptsIt()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond("application/json",
                    @"{""code"":100,""data"":{""taskId"":42},""message"":""""}");

            var client = CreateClient(mockHttp);
            var result = await client.SendAsync<TestTaskResult>(
                "campaign", "send", null, acceptedCodes: new[] { 100 });

            Assert.Equal(MobizonResponseCode.BackgroundTask, result.Code);
            Assert.Equal(42, result.Data.TaskId);
        }

        [Fact]
        public async Task SendAsync_BackgroundTask_IsAnApiError_ForOperationsThatDoNotOptIn()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond("application/json", @"{""code"":100,""data"":42,""message"":""""}");

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() =>
                CreateClient(mockHttp).SendAsync<long>("campaign", "create", null));

            Assert.Equal(100, ex.RawCode);
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
                    "message", "sendsmsmessage", null));

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
                    "message", "sendsmsmessage", null, cts.Token));
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
                    "message", "sendsmsmessage", null));

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
                "message", "list", null);

            Assert.Equal(0.05f, result.Data.SegmentCost, 4);
        }

        private class TestSegResult
        {
            [JsonPropertyName("segUserBuy")]
            public float SegmentCost { get; set; }
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

        [Fact]
        public async Task SendAsync_Non2xxWithHtmlBody_ThrowsMobizonExceptionWithStatus_WithoutQuotingBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond(HttpStatusCode.BadGateway, "text/html", "<html><body>502 Bad Gateway</body></html>");

            var ex = await Assert.ThrowsAsync<MobizonException>(() =>
                CreateClient(mockHttp).SendAsync<object>("message", "sendsmsmessage", null));

            Assert.IsNotType<MobizonApiException>(ex);
            Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
            Assert.Contains("HTTP 502", ex.Message);
            Assert.Contains("message/sendsmsmessage", ex.Message);
            Assert.Contains("text/html", ex.Message);
            Assert.DoesNotContain("<html>", ex.ToString());
        }

        [Fact]
        public async Task SendAsync_2xxWithGarbageBody_ThrowsMobizonExceptionWithStatus_WithoutQuotingBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond("text/plain", "not json at all");

            var ex = await Assert.ThrowsAsync<MobizonException>(() =>
                CreateClient(mockHttp).SendAsync<object>("message", "sendsmsmessage", null));

            Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
            Assert.Contains("not valid JSON", ex.Message);
            Assert.DoesNotContain("not json at all", ex.ToString());
        }

        [Fact]
        public async Task SendAsync_ApiError_CarriesHttpStatus()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, "https://api.mobizon.kz/service/*")
                .Respond(HttpStatusCode.OK, "application/json", @"{""code"":8,""data"":null,""message"":""Login error""}");

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() =>
                CreateClient(mockHttp).SendAsync<object>("message", "sendsmsmessage", null));

            Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
            Assert.Equal(MobizonResponseCode.LoginError, ex.Code);
        }

        [Fact]
        public async Task SendAsync_HttpClientTimeout_ThrowsMobizonException()
        {
            var httpClient = new HttpClient(new HangingHandler()) { Timeout = TimeSpan.FromMilliseconds(200) };
            var client = new MobizonApiClient(httpClient, _options);

            var ex = await Assert.ThrowsAsync<MobizonException>(() =>
                client.SendAsync<object>("message", "sendsmsmessage", null));

            Assert.Contains("timed out", ex.Message);
            Assert.Null(ex.StatusCode);
            Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
        }

        [Fact]
        public async Task SendAsync_CallerCancellation_IsNotWrapped()
        {
            var httpClient = new HttpClient(new HangingHandler());
            var client = new MobizonApiClient(httpClient, _options);
            using var cts = new CancellationTokenSource(50);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                client.SendAsync<object>("message", "sendsmsmessage", null, cts.Token));
        }

        /// <summary>Never answers; completes only when the request's token is cancelled.</summary>
        private sealed class HangingHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}
