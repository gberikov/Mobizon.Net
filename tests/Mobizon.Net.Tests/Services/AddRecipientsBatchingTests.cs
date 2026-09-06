using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    /// <summary>Regression tests for R3 (multi-batch outcome) and R8 (linear accumulation) via the public client.</summary>
    public class AddRecipientsBatchingTests
    {
        private const string Url = "https://api.mobizon.kz/service/Campaign/AddRecipients";

        private static MobizonClient Client(MockHttpMessageHandler mockHttp) =>
            new MobizonClient(mockHttp.ToHttpClient(), new MobizonClientOptions { ApiKey = "k", ApiUrl = "https://api.mobizon.kz" });

        private static AddRecipientsRequest Request(int count, AddRecipientsParameters? prm = null) => new AddRecipientsRequest
        {
            CampaignId = 1,
            Recipients = Enumerable.Range(0, count).Select(i => new RecipientEntry { Recipient = (77000000000L + i).ToString() }).ToArray(),
            Parameters = prm
        };

        /// <summary>A batch response describing <paramref name="count"/> recipients starting at <paramref name="from"/>, all with <paramref name="entryCode"/>.</summary>
        private static string Batch(int code, int from, int count, int entryCode = 0)
        {
            var entries = string.Join(",", Enumerable.Range(from, count).Select(i =>
                $@"{{""recipient"":""{77000000000L + i}"",""code"":{entryCode},""messageId"":""{100 + i}""}}"));
            return $@"{{""code"":{code},""data"":[{entries}],""message"":""""}}";
        }

        private static async Task<string> Body(HttpRequestMessage req) => await req.Content!.ReadAsStringAsync();

        [Theory]
        [InlineData(0, 99, AddRecipientsOutcome.PartiallyAdded)]
        [InlineData(99, 0, AddRecipientsOutcome.PartiallyAdded)]
        [InlineData(98, 99, AddRecipientsOutcome.PartiallyAdded)]
        [InlineData(98, 0, AddRecipientsOutcome.PartiallyAdded)]
        [InlineData(0, 98, AddRecipientsOutcome.PartiallyAdded)]
        [InlineData(0, 0, AddRecipientsOutcome.AllAdded)]
        [InlineData(99, 99, AddRecipientsOutcome.NoneAdded)]
        public async Task TwoBatches_OutcomeIsAggregated_NotMaxCode(int first, int second, AddRecipientsOutcome expected)
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", Batch(first, 0, 500, first == 99 ? 3 : 0));
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", Batch(second, 500, 1, second == 99 ? 3 : 0));

            var result = await Client(mockHttp).Campaigns.AddRecipientsAsync(Request(501));

            Assert.Equal(expected, result.Outcome);
            Assert.False(result.IsQueued);
            Assert.Equal(501, result.Entries!.Count);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData(500, 1)]
        [InlineData(501, 2)]
        [InlineData(1001, 3)]
        public async Task BatchBoundaries_EntriesPreserveOrder(int total, int batches)
        {
            var mockHttp = new MockHttpMessageHandler();
            var calls = new List<int>();
            for (var b = 0; b < batches; b++)
            {
                var from = b * 500;
                var count = Math.Min(500, total - from);
                mockHttp.Expect(HttpMethod.Post, Url)
                    .With(req => { calls.Add(Body(req).Result.Split(new[] { "%5Brecipient%5D=" }, StringSplitOptions.None).Length - 1); return true; })
                    .Respond("application/json", Batch(0, from, count));
            }

            var result = await Client(mockHttp).Campaigns.AddRecipientsAsync(Request(total));

            Assert.Equal(total, result.Entries!.Count);
            Assert.Equal(Enumerable.Range(0, total).Select(i => (77000000000L + i).ToString()), result.Entries.Select(e => e.Recipient));
            Assert.Equal(Enumerable.Range(0, batches).Select(b => Math.Min(500, total - b * 500)), calls);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task Replace_IsSentOnFirstBatchOnly()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Url).WithFormData("params[replace]", "1").Respond("application/json", Batch(0, 0, 500));
            mockHttp.Expect(HttpMethod.Post, Url).WithFormData("params[replace]", "0").Respond("application/json", Batch(0, 500, 1));

            await Client(mockHttp).Campaigns.AddRecipientsAsync(Request(501, new AddRecipientsParameters { Replace = true }));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SecondBatchApiError_ThrowsApiException_WithConfirmedProgress()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", Batch(0, 0, 500));
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", @"{""code"":12,""data"":null,""message"":""limit""}");

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() => Client(mockHttp).Campaigns.AddRecipientsAsync(Request(501)));

            var progress = AddRecipientsProgress.FromException(ex);
            Assert.NotNull(progress);
            Assert.Equal(500, progress!.ConfirmedCount);
            Assert.Equal(1, progress.PendingCount);
            Assert.Equal(500, progress.Confirmed.Entries!.Count);
            Assert.Equal(AddRecipientsOutcome.AllAdded, progress.Confirmed.Outcome);
        }

        [Fact]
        public async Task SecondBatchTransportError_MarksBatchAsUnknown()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", Batch(99, 0, 500, 3));
            mockHttp.Expect(HttpMethod.Post, Url).Throw(new HttpRequestException("connection reset"));

            var ex = await Assert.ThrowsAsync<MobizonException>(() => Client(mockHttp).Campaigns.AddRecipientsAsync(Request(1001)));

            Assert.IsNotType<MobizonApiException>(ex);
            var progress = AddRecipientsProgress.FromException(ex)!;
            Assert.Equal(500, progress.ConfirmedCount);
            Assert.Equal(500, progress.PendingCount); // second batch of 1001 is 500 wide; its fate is unknown
            Assert.Equal(AddRecipientsOutcome.NoneAdded, progress.Confirmed.Outcome);
        }

        [Fact]
        public async Task CancellationDuringSecondBatch_IsOperationCanceled_WithConfirmedFirstBatch_AndSecondUnknown()
        {
            using var cts = new CancellationTokenSource();
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", Batch(0, 0, 500));
            mockHttp.Expect(HttpMethod.Post, Url).Respond(_ =>
            {
                cts.Cancel(); // the request is on the wire when the caller gives up
                return new HttpResponseMessage { Content = new StringContent(Batch(0, 500, 1), System.Text.Encoding.UTF8, "application/json") };
            });

            var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                Client(mockHttp).Campaigns.AddRecipientsAsync(Request(501), cts.Token));

            var progress = AddRecipientsProgress.FromException(ex)!;
            Assert.Equal(500, progress.ConfirmedCount);
            Assert.Equal(1, progress.PendingCount);
            Assert.Equal(500, progress.Confirmed.Entries!.Count);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CancellationBeforeFirstBatch_IsOperationCanceled_WithNothingConfirmedOrInFlight()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var mockHttp = new MockHttpMessageHandler();

            var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                Client(mockHttp).Campaigns.AddRecipientsAsync(Request(501), cts.Token));

            var progress = AddRecipientsProgress.FromException(ex)!;
            Assert.Equal(0, progress.ConfirmedCount);
            Assert.Equal(0, progress.PendingCount);
            Assert.Empty(progress.Confirmed.Entries!);
        }

        [Fact]
        public async Task SingleBatchFailure_CarriesNoProgress()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, Url).Respond("application/json", @"{""code"":12,""data"":null,""message"":""limit""}");

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() => Client(mockHttp).Campaigns.AddRecipientsAsync(Request(3)));

            Assert.Null(AddRecipientsProgress.FromException(ex));
        }

        [Fact]
        public async Task GroupLoad_Queued_SetsIsQueuedAndTaskId()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, Url).Respond("application/json", @"{""code"":100,""data"":""777"",""message"":""""}");

            var result = await Client(mockHttp).Campaigns.AddRecipientsAsync(new AddRecipientsRequest { CampaignId = 1, RecipientGroups = new[] { 5L } });

            Assert.True(result.IsQueued);
            Assert.Equal(777, result.TaskId);
            Assert.Equal(AddRecipientsOutcome.AllAdded, result.Outcome);
        }

        public static IEnumerable<object[]> InvalidQueuedResponses()
        {
            var responses = new[]
            {
                "{\"code\":100}",
                "{\"code\":100,\"data\":null}",
                "{\"code\":100,\"data\":[]}",
                "{\"code\":100,\"data\":0}",
                "{\"code\":100,\"data\":-1}",
                "{\"code\":100,\"data\":\"0\"}",
                "{\"code\":100,\"data\":{}}",
                "{\"code\":0,\"data\":777}",
                "{\"code\":0,\"data\":null}"
            };
            foreach (var file in new[] { false, true })
                foreach (var json in responses)
                    yield return new object[] { file, json };
        }

        [Theory]
        [MemberData(nameof(InvalidQueuedResponses))]
        public async Task AsyncLoad_InvalidTaskResponse_ThrowsProtocolError(bool fileLoad, string json)
        {
            using var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", json);
            using var file = new MemoryStream(new byte[] { 1, 2, 3 });
            using var client = Client(mockHttp);
            var request = fileLoad
                ? new AddRecipientsRequest { CampaignId = 1, RecipientsFile = file }
                : new AddRecipientsRequest { CampaignId = 1, RecipientGroups = new[] { 5L } };

            var ex = await Assert.ThrowsAsync<MobizonException>(() => client.Campaigns.AddRecipientsAsync(request));

            Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
            Assert.Contains("Campaign/AddRecipients", ex.Message);
            Assert.True(file.CanRead);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData(false, "777")]
        [InlineData(false, "\"777\"")]
        [InlineData(true, "777")]
        [InlineData(true, "\"777\"")]
        public async Task AsyncLoad_PositiveTaskId_IsQueued(bool fileLoad, string data)
        {
            using var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", "{\"code\":100,\"data\":" + data + "}");
            using var file = new MemoryStream(new byte[] { 1, 2, 3 });
            using var client = Client(mockHttp);
            var request = fileLoad
                ? new AddRecipientsRequest { CampaignId = 1, RecipientsFile = file }
                : new AddRecipientsRequest { CampaignId = 1, RecipientGroups = new[] { 5L } };

            var result = await client.Campaigns.AddRecipientsAsync(request);

            Assert.True(result.IsQueued);
            Assert.Equal(777, result.TaskId);
            Assert.Null(result.Entries);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData(false, 100)]
        [InlineData(true, 100)]
        [InlineData(false, 0)]
        [InlineData(true, 0)]
        public async Task SyncBatch_TaskResponse_StopsWithConfirmedProgress(bool contactLoad, int code)
        {
            using var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", Batch(0, 0, 500));
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", "{\"code\":" + code + ",\"data\":777}");
            using var client = Client(mockHttp);
            var request = contactLoad
                ? new AddRecipientsRequest { CampaignId = 1, RecipientContacts = Enumerable.Range(1, 1001).Select(i => i.ToString()).ToArray() }
                : Request(1001);

            var ex = await Assert.ThrowsAnyAsync<MobizonException>(() => client.Campaigns.AddRecipientsAsync(request));

            if (code == 100)
                Assert.Equal(100, Assert.IsType<MobizonApiException>(ex).RawCode);
            var progress = AddRecipientsProgress.FromException(ex)!;
            Assert.NotNull(progress);
            Assert.Equal(500, progress.ConfirmedCount);
            Assert.Equal(500, progress.PendingCount);
            Assert.Equal(500, progress.Confirmed.Entries!.Count);
            Assert.False(progress.Confirmed.IsQueued);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SingleSyncBatch_QueuedResponse_IsRejected()
        {
            using var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Url).Respond("application/json", "{\"code\":100,\"data\":777}");
            using var client = Client(mockHttp);

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() => client.Campaigns.AddRecipientsAsync(Request(1)));

            Assert.Equal(100, ex.RawCode);
            Assert.Null(AddRecipientsProgress.FromException(ex));
        }

        [Fact]
        public async Task GroupLoad_BulkCodes_AreNotAcceptedForAsyncSources()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, Url).Respond("application/json", @"{""code"":99,""data"":[],""message"":""""}");

            var ex = await Assert.ThrowsAsync<MobizonApiException>(() =>
                Client(mockHttp).Campaigns.AddRecipientsAsync(new AddRecipientsRequest { CampaignId = 1, RecipientGroups = new[] { 5L } }));

            Assert.Equal(99, ex.RawCode);
        }

        [Fact]
        public async Task SyncLoad_UnexpectedDataShape_IsProtocolError_NotEmptySuccess()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, Url).Respond("application/json", @"{""code"":0,""data"":{""weird"":true},""message"":""""}");

            var ex = await Assert.ThrowsAsync<MobizonException>(() => Client(mockHttp).Campaigns.AddRecipientsAsync(Request(1)));

            Assert.IsNotType<MobizonApiException>(ex);
        }

        [Fact]
        public async Task NullRequest_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Client(new MockHttpMessageHandler()).Campaigns.AddRecipientsAsync(null!));
        }
    }
}
