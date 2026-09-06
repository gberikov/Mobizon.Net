using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    /// <summary>
    /// Regression tests for the review findings on envelope handling (R1, R2), safe diagnostics (R6),
    /// option validation (R7) and stream ownership (R9). All go through the public <see cref="MobizonClient"/>.
    /// </summary>
    public class EnvelopeHandlingTests
    {
        private const string Base = "https://api.mobizon.kz";

        private static MobizonClient Client(MockHttpMessageHandler mockHttp) =>
            new MobizonClient(mockHttp.ToHttpClient(), new MobizonClientOptions { ApiKey = "test-key", ApiUrl = Base });

        private static MockHttpMessageHandler Respond(string url, string json, HttpStatusCode status = HttpStatusCode.OK)
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, url).Respond(status, "application/json", json);
            return mockHttp;
        }

        private static readonly CreateCampaignRequest Campaign = new CreateCampaignRequest { Type = CampaignType.Bulk, Text = "hi" };

        // ── R1: API errors are classified before the payload is bound ─────────

        [Fact]
        public async Task ApiError_WithFieldErrorObject_ThrowsApiException_NotDeserializationError()
        {
            var mockHttp = Respond(Base + "/service/Campaign/Create",
                @"{""code"":1,""data"":{""text"":[""required""]},""message"":""Validation error""}");

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() => Client(mockHttp).Campaigns.CreateAsync(Campaign));

            Assert.Equal(1, ex.RawCode);
            Assert.Equal(MobizonResponseCode.ValidationError, ex.Code);
            Assert.Equal("Validation error", ex.ApiMessage);
            Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
            Assert.Equal(new[] { "required" }, ex.FieldErrors["text"]);
        }

        [Fact]
        public async Task ApiError_NestedFieldErrors_AreFlattenedWithDots()
        {
            var mockHttp = Respond(Base + "/service/contactcard/create",
                @"{""code"":1,""data"":{""mobile"":{""value"":""invalid""},""name"":""required""},""message"":""Validation error""}");

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() =>
                Client(mockHttp).ContactCards.AddAsync(new ContactCard { Name = "x" }));

            Assert.Equal(new[] { "invalid" }, ex.FieldErrors["mobile.value"]);
            Assert.Equal(new[] { "required" }, ex.FieldErrors["name"]);
        }

        [Theory]
        [InlineData(@"{""code"":2,""data"":null,""message"":""Not found""}")]
        [InlineData(@"{""code"":2,""data"":[],""message"":""Not found""}")]
        [InlineData(@"{""code"":2,""data"":""some text"",""message"":""Not found""}")]
        [InlineData(@"{""code"":2,""data"":{},""message"":""Not found""}")]
        [InlineData(@"{""code"":2,""message"":""Not found""}")]
        public async Task ApiError_AnyDataShape_PreservesCode_AndHasNoFieldErrors(string json)
        {
            var mockHttp = Respond(Base + "/service/Campaign/Create", json);

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() => Client(mockHttp).Campaigns.CreateAsync(Campaign));

            Assert.Equal(2, ex.RawCode);
            Assert.Equal("Not found", ex.ApiMessage);
            Assert.Empty(ex.FieldErrors);
        }

        [Fact]
        public async Task ApiError_UnknownCode_IsKeptAsNumber()
        {
            var mockHttp = Respond(Base + "/service/Campaign/Create", @"{""code"":777,""data"":null,""message"":""?""}");

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() => Client(mockHttp).Campaigns.CreateAsync(Campaign));

            Assert.Equal(777, ex.RawCode);
            Assert.Equal((MobizonResponseCode)777, ex.Code);
        }

        [Fact]
        public async Task ApiError_OnNon2xx_KeepsApiCodeAndHttpStatus()
        {
            var mockHttp = Respond(Base + "/service/Campaign/Create",
                @"{""code"":3,""data"":null,""message"":""boom""}", HttpStatusCode.InternalServerError);

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() => Client(mockHttp).Campaigns.CreateAsync(Campaign));

            Assert.Equal(3, ex.RawCode);
            Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        }

        // ── R2: no false success ───────────────────────────────────────────────

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, "{}")]
        [InlineData(HttpStatusCode.OK, "{}")]
        [InlineData(HttpStatusCode.OK, "null")]
        [InlineData(HttpStatusCode.OK, "[]")]
        [InlineData(HttpStatusCode.OK, @"{""code"":""abc"",""data"":5,""message"":""""}")]
        [InlineData(HttpStatusCode.OK, @"{""code"":null,""data"":5,""message"":""""}")]
        [InlineData(HttpStatusCode.OK, @"{""data"":5,""message"":""""}")]
        [InlineData(HttpStatusCode.InternalServerError, @"{""code"":0,""data"":""5"",""message"":""""}")]
        [InlineData(HttpStatusCode.OK, @"{""code"":0,""message"":""""}")]
        [InlineData(HttpStatusCode.OK, @"{""code"":0,""data"":null,""message"":""""}")]
        [InlineData(HttpStatusCode.OK, @"{""code"":0,""data"":""0"",""message"":""""}")]
        [InlineData(HttpStatusCode.OK, @"{""code"":0,""data"":0,""message"":""""}")]
        [InlineData(HttpStatusCode.OK, @"{""code"":0,""data"":-1,""message"":""""}")]
        public async Task Create_NeverSucceeds_OnInvalidEnvelopeOrMissingId(HttpStatusCode status, string json)
        {
            var mockHttp = Respond(Base + "/service/Campaign/Create", json, status);

            var ex = await Assert.ThrowsAsync<MobizonException>(() => Client(mockHttp).Campaigns.CreateAsync(Campaign));

            Assert.IsNotType<MobizonApiException>(ex);
            Assert.Equal(status, ex.StatusCode);
            Assert.Contains("Campaign/Create", ex.Message);
        }

        [Fact]
        public async Task Create_NumericStringCode_IsAccepted()
        {
            var mockHttp = Respond(Base + "/service/Campaign/Create", @"{""code"":""0"",""data"":""123"",""message"":""""}");

            Assert.Equal(123, await Client(mockHttp).Campaigns.CreateAsync(Campaign));
        }

        [Fact]
        public async Task Command_WithNullOrTrueData_Succeeds()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Base + "/service/Campaign/Delete").Respond("application/json", @"{""code"":0,""data"":true,""message"":""""}");
            mockHttp.Expect(HttpMethod.Post, Base + "/service/link/update").Respond("application/json", @"{""code"":0,""data"":null,""message"":""""}");

            var client = Client(mockHttp);
            await client.Campaigns.DeleteAsync(1);
            await client.Links.UpdateAsync(new UpdateLinkRequest { Id = 1, Comment = "c" });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task Command_ApiError_StillThrows()
        {
            var mockHttp = Respond(Base + "/service/Campaign/Delete", @"{""code"":2,""data"":null,""message"":""gone""}");

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() => Client(mockHttp).Campaigns.DeleteAsync(1));
            Assert.Equal(2, ex.RawCode);
        }

        [Fact]
        public async Task ContactCardList_NullData_IsStillAnEmptyPage()
        {
            var mockHttp = Respond(Base + "/service/contactcard/list", @"{""code"":0,""data"":null,""message"":""""}");

            Assert.Empty(await Client(mockHttp).ContactCards.Where(x => x.GroupId == 1).ToListAsync());
        }

        // ── R6: diagnostics never quote payload content ───────────────────────

        [Fact]
        public async Task DeserializationFailure_DoesNotLeakPayload_ButNamesOperationStatusAndPath()
        {
            const string secrets = "OTP-8421 +77001234567 someone@example.com SECRET-API-KEY";
            var mockHttp = Respond(Base + "/service/Message/SendSmsMessage",
                @"{""code"":0,""data"":{""campaignId"":""1"",""messageId"":""" + secrets + @""",""status"":1},""message"":""""}");

            var ex = await Assert.ThrowsAsync<MobizonException>(() =>
                Client(mockHttp).Messages.QuickSendAsync("77001234567", "hello"));

            var full = ex.ToString();
            Assert.IsNotType<MobizonApiException>(ex);
            Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
            Assert.Contains("Message/SendSmsMessage", ex.Message);
            Assert.Contains("messageId", ex.Message);
            foreach (var token in new[] { "OTP-8421", "77001234567", "someone@example.com", "SECRET-API-KEY" })
                Assert.DoesNotContain(token, full);
        }

        [Fact]
        public async Task NonJsonBody_DoesNotLeakBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, Base + "/service/*")
                .Respond(HttpStatusCode.BadGateway, "text/html", "<html>proxy says: OTP-8421 for +77001234567</html>");

            var ex = await Assert.ThrowsAsync<MobizonException>(() => Client(mockHttp).User.GetOwnBalanceAsync());

            Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
            Assert.Contains("user/getownbalance", ex.Message);
            Assert.DoesNotContain("OTP-8421", ex.ToString());
            Assert.DoesNotContain("77001234567", ex.ToString());
        }

        [Fact]
        public async Task UnknownEnumValue_DoesNotLeakValue_ButNamesField()
        {
            var mockHttp = Respond(Base + "/service/Message/GetSMSStatus",
                @"{""code"":0,""data"":[{""id"":""1"",""status"":""LEAKED-VALUE"",""segNum"":""1""}],""message"":""""}");

            var ex = await Assert.ThrowsAsync<MobizonException>(() => Client(mockHttp).Messages.GetSmsStatusAsync(1));

            Assert.DoesNotContain("LEAKED-VALUE", ex.ToString());
            Assert.Contains("status", ex.Message);
        }

        // ── R7: option validation ──────────────────────────────────────────────

        [Theory]
        [InlineData("api.mobizon.kz")]
        [InlineData("/service")]
        [InlineData("http://api.mobizon.kz")]
        [InlineData("ftp://api.mobizon.kz")]
        [InlineData("https://api.mobizon.kz/?x=1")]
        [InlineData("https://api.mobizon.kz/#frag")]
        [InlineData("https://user:pw@api.mobizon.kz")]
        public void Validate_RejectsUnsafeOrMalformedUrls(string url)
        {
            var options = new MobizonClientOptions { ApiKey = "k", ApiUrl = url };
            var ex = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Equal("ApiUrl", ex.ParamName);
        }

        [Theory]
        [InlineData("https://api.mobizon.kz")]
        [InlineData("https://api.mobizon.uz/")]
        [InlineData("https://api.mobizon.com")]
        [InlineData("https://gateway.example.com/mobizon-proxy")]
        [InlineData("https://127.0.0.1:8443")]
        public void Validate_AcceptsRegionalAndProxiedHttpsUrls(string url)
        {
            new MobizonClientOptions { ApiKey = "k", ApiUrl = url }.Validate();
        }

        [Fact]
        public void Validate_AllowsHttp_OnlyWithExplicitOptIn()
        {
            var options = new MobizonClientOptions { ApiKey = "k", ApiUrl = "http://localhost:5000", AllowInsecureHttp = true };
            options.Validate();
        }

        [Theory]
        [InlineData("")]
        [InlineData("v1/../x")]
        [InlineData("v 1")]
        public void Validate_RejectsInvalidApiVersion(string version)
        {
            var options = new MobizonClientOptions { ApiKey = "k", ApiUrl = "https://api.mobizon.kz", ApiVersion = version };
            var ex = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Equal("ApiVersion", ex.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Validate_RejectsNonPositiveTimeout(int seconds)
        {
            var options = new MobizonClientOptions { ApiKey = "k", ApiUrl = "https://api.mobizon.kz", Timeout = TimeSpan.FromSeconds(seconds) };
            var ex = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Equal("Timeout", ex.ParamName);
        }

        [Fact]
        public void Validate_AllowsInfiniteTimeout_AndOwnedClientHonoursIt()
        {
            var options = new MobizonClientOptions { ApiKey = "k", ApiUrl = "https://api.mobizon.kz", Timeout = System.Threading.Timeout.InfiniteTimeSpan };
            options.Validate();
            using var client = new MobizonClient(options);
        }

        [Fact]
        public async Task PathPrefix_IsKeptInRequestUrl()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "https://gw.example.com/mobizon/service/user/getownbalance")
                .WithQueryString("api", "v1")
                .Respond("application/json", @"{""code"":0,""data"":{""balance"":""1"",""currency"":""KZT""},""message"":""""}");

            var client = new MobizonClient(mockHttp.ToHttpClient(),
                new MobizonClientOptions { ApiKey = "k", ApiUrl = "https://gw.example.com/mobizon/" });
            await client.User.GetOwnBalanceAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public void Client_NullOptions_ThrowsArgumentNullException_NotNullReference()
        {
            Assert.Throws<ArgumentNullException>(() => new MobizonClient(new HttpClient(), null!));
            Assert.Throws<ArgumentNullException>(() => new MobizonClient(null!));
        }

        [Fact]
        public void Client_HttpUrlWithoutOptIn_IsRejectedBeforeAnyRequest()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new MobizonClient(new HttpClient(), new MobizonClientOptions { ApiKey = "k", ApiUrl = "http://api.mobizon.kz" }));
            Assert.Equal("ApiUrl", ex.ParamName);
        }

        [Fact]
        public void OwnedClient_AppliesTimeoutAndResponseCap()
        {
            var options = new MobizonClientOptions { ApiKey = "k", ApiUrl = "https://api.mobizon.kz", Timeout = TimeSpan.FromSeconds(7) };
            using var client = new MobizonClient(options);
            var http = (HttpClient)typeof(MobizonClient)
                .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(client)!;

            Assert.Equal(TimeSpan.FromSeconds(7), http.Timeout);
            Assert.Equal(MobizonClient.MaxResponseContentBufferSize, http.MaxResponseContentBufferSize);
        }

        // ── R9: caller owns the uploaded stream ───────────────────────────────

        [Fact]
        public async Task PhotoUpload_LeavesCallerStreamOpen()
        {
            var mockHttp = Respond(Base + "/service/contactcard/create", @"{""code"":0,""data"":""55"",""message"":""""}");
            using var photo = new MemoryStream(new byte[] { 1, 2, 3 });
            var card = new ContactCard { Name = "n", Photo = photo, PhotoFileName = "p.jpg" };

            await Client(mockHttp).ContactCards.AddAsync(card);

            Assert.Equal(55, card.Id);
            Assert.True(photo.CanRead);
            Assert.Null(card.Photo);
        }

        [Fact]
        public async Task RecipientsFileUpload_LeavesCallerStreamOpen_EvenOnApiError()
        {
            var mockHttp = Respond(Base + "/service/Campaign/AddRecipients", @"{""code"":1,""data"":null,""message"":""bad file""}");
            using var file = new MemoryStream(new byte[] { 1, 2, 3 });

            await Assert.ThrowsAsync<MobizonApiException>(() => Client(mockHttp).Campaigns.AddRecipientsAsync(
                new AddRecipientsRequest { CampaignId = 1, RecipientsFile = file }));

            Assert.True(file.CanRead);
        }
    }
}
