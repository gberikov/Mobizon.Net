using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mobizon.Contracts;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    /// <summary>
    /// Regression tests for the second review: the documented <c>groups</c> string, the nested <c>extra</c>
    /// object, list statistics, placeholder safety and the missing add-recipients payload.
    /// </summary>
    public class CampaignContractTests
    {
        private const string Base = "https://api.mobizon.kz";

        private static MobizonClient Client(MockHttpMessageHandler mockHttp) =>
            new MobizonClient(mockHttp.ToHttpClient(), new MobizonClientOptions { ApiKey = "k", ApiUrl = Base });

        private static MockHttpMessageHandler Respond(string url, string json)
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, url).Respond("application/json", json);
            return mockHttp;
        }

        // ── groups: documented as a comma-separated string ────────────────────

        [Theory]
        [InlineData("\"12,34\"", new long[] { 12, 34 })]
        [InlineData("\"12\"", new long[] { 12 })]
        [InlineData("\"\"", new long[0])]
        [InlineData("[\"12\",\"34\"]", new long[] { 12, 34 })]
        [InlineData("[12,34]", new long[] { 12, 34 })]
        [InlineData("[]", new long[0])]
        public async Task Get_ParsesGroups_InEitherDocumentedShape(string groupsJson, long[] expected)
        {
            var mockHttp = Respond(Base + "/service/Campaign/Get",
                @"{""code"":0,""data"":{""id"":1,""groups"":" + groupsJson + @"},""message"":""""}");

            var campaign = await Client(mockHttp).Campaigns.GetAsync(1);

            Assert.Equal(expected, campaign.Groups!);
        }

        [Fact]
        public async Task Get_NullGroups_StaysNull_AndRestOfCampaignIsReadable()
        {
            var mockHttp = Respond(Base + "/service/Campaign/Get",
                @"{""code"":0,""data"":{""id"":7,""groups"":null,""from"":""MyBrand""},""message"":""""}");

            var campaign = await Client(mockHttp).Campaigns.GetAsync(7);

            Assert.Null(campaign.Groups);
            Assert.Equal(7, campaign.Id);
            Assert.Equal("MyBrand", campaign.From);
        }

        [Fact]
        public async Task Get_MalformedGroups_IsProtocolError_NotSilentlyDropped()
        {
            var mockHttp = Respond(Base + "/service/Campaign/Get",
                @"{""code"":0,""data"":{""id"":1,""groups"":""12,not-a-number""},""message"":""""}");

            var ex = await Assert.ThrowsAsync<MobizonException>(() => Client(mockHttp).Campaigns.GetAsync(1));
            Assert.IsNotType<MobizonApiException>(ex);
        }

        [Fact]
        public async Task Delete_StillParsesArrayIdentifiers()
        {
            var mockHttp = Respond(Base + "/service/link/delete",
                @"{""code"":0,""data"":{""processed"":[""1"",""2""],""notProcessed"":[]},""message"":""""}");

            var result = await Client(mockHttp).Links.DeleteAsync(new[] { 1L, 2L });

            Assert.Equal(new long[] { 1, 2 }, result.Processed);
            Assert.Empty(result.NotProcessed);
        }

        // ── extra: the real capture nests what the documentation puts on top ──

        [Fact]
        public async Task GetInfo_ReadsSettingsNestedInExtra_FromTheRealCapture()
        {
            var mockHttp = Respond(Base + "/service/Campaign/GetInfo",
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Payloads", "campaign.getInfo.json")));

            var info = await Client(mockHttp).Campaigns.GetInfoAsync(1);

            Assert.Equal(TimeSpan.FromHours(24), info.Validity);
            Assert.Equal(MessageClass.Normal, info.MessageClass);
            Assert.False(info.TrackShortLinkRecipients);
            Assert.True(info.Extra!.IsTestAlphanameUsed);
            Assert.Equal("UTF-8", info.Extra.Charset);
            Assert.Equal(0, info.Extra.Coding);
        }

        [Fact]
        public async Task GetInfo_ReadsSettingsFromTheTopLevel_AsDocumented()
        {
            var mockHttp = Respond(Base + "/service/Campaign/GetInfo",
                @"{""code"":0,""data"":{""id"":1,""validity"":""60"",""mclass"":""0"",""trackShortLinkRecipients"":""1""},""message"":""""}");

            var info = await Client(mockHttp).Campaigns.GetInfoAsync(1);

            Assert.Equal(TimeSpan.FromHours(1), info.Validity);
            Assert.Equal(MessageClass.Flash, info.MessageClass);
            Assert.True(info.TrackShortLinkRecipients);
            Assert.Null(info.Extra);
        }

        [Fact]
        public async Task GetInfo_TopLevelWins_WhenBothPlacementsArePresent()
        {
            var mockHttp = Respond(Base + "/service/Campaign/GetInfo",
                @"{""code"":0,""data"":{""id"":1,""validity"":""60"",""extra"":{""validity"":""1440""}},""message"":""""}");

            var info = await Client(mockHttp).Campaigns.GetInfoAsync(1);

            Assert.Equal(TimeSpan.FromHours(1), info.Validity);
        }

        // ── list: documented to return getInfo items ──────────────────────────

        [Fact]
        public async Task List_KeepsCounters_SoNoExtraGetInfoCallIsNeeded()
        {
            var mockHttp = Respond(Base + "/service/Campaign/List",
                @"{""code"":0,""data"":{""items"":[{""id"":1,""creationWay"":""1"",""counters"":{""totalMsgNum"":""7"",""totalCost"":""16.2000""}}],""totalItemCount"":""1""},""message"":""""}");

            var page = await Client(mockHttp).Campaigns.ListAsync();

            var info = Assert.IsAssignableFrom<CampaignInfo>(page.Items[0]);
            Assert.Equal(7, info.Counters!.TotalMsgNum);
            Assert.Equal(16.2000m, info.Counters.TotalCost);
            Assert.Equal(1, info.CreationWay);
        }

        [Fact]
        public async Task List_WithoutCounters_StillReadsTheCampaign()
        {
            var mockHttp = Respond(Base + "/service/Campaign/List",
                @"{""code"":0,""data"":{""items"":[{""id"":""123"",""text"":""hi""}],""totalItemCount"":""1""},""message"":""""}");

            var page = await Client(mockHttp).Campaigns.ListAsync();

            Assert.Equal(123, page.Items[0].Id);
            Assert.Null(Assert.IsAssignableFrom<CampaignInfo>(page.Items[0]).Counters);
        }

        // ── placeholders must not rewrite the destination number ──────────────

        [Theory]
        [InlineData("recipient")]
        [InlineData("Recipient")]
        [InlineData("RECIPIENT")]
        public async Task AddRecipients_RejectsReservedPlaceholderName_BeforeSending(string name)
        {
            var mockHttp = new MockHttpMessageHandler(); // any request would be an unmatched-request failure

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                Client(mockHttp).Campaigns.AddRecipientsAsync(new AddRecipientsRequest
                {
                    CampaignId = 1,
                    Recipients = new[]
                    {
                        new RecipientEntry
                        {
                            Recipient = "77001111111",
                            Placeholders = new Dictionary<string, string> { [name] = "77002222222" }
                        }
                    }
                }));

            Assert.Contains("recipient", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("name[0]")]
        public async Task AddRecipients_RejectsPlaceholderNameThatWouldCorruptTheRequest(string name)
        {
            var mockHttp = new MockHttpMessageHandler();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                Client(mockHttp).Campaigns.AddRecipientsAsync(new AddRecipientsRequest
                {
                    CampaignId = 1,
                    Recipients = new[]
                    {
                        new RecipientEntry { Recipient = "77001111111", Placeholders = new Dictionary<string, string> { [name] = "x" } }
                    }
                }));
        }

        [Fact]
        public async Task AddRecipients_ValidatesEveryEntry_BeforeTheFirstBatch()
        {
            var mockHttp = new MockHttpMessageHandler();
            var recipients = Enumerable.Range(0, 600)
                .Select(i => new RecipientEntry { Recipient = (77000000000L + i).ToString() })
                .ToArray();
            recipients[599].Placeholders = new Dictionary<string, string> { ["recipient"] = "77009999999" };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                Client(mockHttp).Campaigns.AddRecipientsAsync(new AddRecipientsRequest { CampaignId = 1, Recipients = recipients }));

            // Nothing was sent: the bad name in the second batch stopped the first one too.
            Assert.Equal(0, mockHttp.GetMatchCount(mockHttp.When("*")));
        }

        [Fact]
        public async Task AddRecipients_AcceptsOrdinaryPlaceholders()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Base + "/service/Campaign/AddRecipients")
                .WithFormData("recipients[0][recipient]", "77001111111")
                .WithFormData("recipients[0][name]", "Ivan")
                .Respond("application/json", @"{""code"":0,""data"":[{""recipient"":""77001111111"",""code"":0}],""message"":""""}");

            await Client(mockHttp).Campaigns.AddRecipientsAsync(new AddRecipientsRequest
            {
                CampaignId = 1,
                Recipients = new[]
                {
                    new RecipientEntry
                    {
                        Recipient = "77001111111",
                        Placeholders = new Dictionary<string, string> { ["name"] = "Ivan" }
                    }
                }
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── a successful synchronous batch must describe its recipients ───────

        [Theory]
        [InlineData(@"{""code"":0,""data"":null,""message"":""""}")]
        [InlineData(@"{""code"":0,""message"":""""}")]
        public async Task AddRecipients_SuccessWithoutResults_IsProtocolError_NotAllAdded(string json)
        {
            var mockHttp = Respond(Base + "/service/Campaign/AddRecipients", json);

            var ex = await Assert.ThrowsAsync<MobizonException>(() =>
                Client(mockHttp).Campaigns.AddRecipientsAsync(new AddRecipientsRequest
                {
                    CampaignId = 1,
                    Recipients = new[] { new RecipientEntry { Recipient = "77001111111" } }
                }));

            Assert.IsNotType<MobizonApiException>(ex);
            Assert.Contains("Campaign/AddRecipients", ex.Message);
        }

        [Fact]
        public async Task AddRecipients_EmptyResultArray_IsStillAccepted()
        {
            var mockHttp = Respond(Base + "/service/Campaign/AddRecipients", @"{""code"":0,""data"":[],""message"":""""}");

            var result = await Client(mockHttp).Campaigns.AddRecipientsAsync(new AddRecipientsRequest
            {
                CampaignId = 1,
                Recipients = new[] { new RecipientEntry { Recipient = "77001111111" } }
            });

            Assert.Empty(result.Entries!);
            Assert.Equal(AddRecipientsOutcome.AllAdded, result.Outcome);
        }

        [Fact]
        public async Task AddRecipients_MissingPayloadInLaterBatch_KeepsConfirmedProgress()
        {
            var entries = string.Join(",", Enumerable.Range(0, 500).Select(i =>
                $@"{{""recipient"":""{77000000000L + i}"",""code"":0}}"));
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Base + "/service/Campaign/AddRecipients")
                .Respond("application/json", $@"{{""code"":0,""data"":[{entries}],""message"":""""}}");
            mockHttp.Expect(HttpMethod.Post, Base + "/service/Campaign/AddRecipients")
                .Respond("application/json", @"{""code"":0,""data"":null,""message"":""""}");

            var request = new AddRecipientsRequest
            {
                CampaignId = 1,
                Recipients = Enumerable.Range(0, 501)
                    .Select(i => new RecipientEntry { Recipient = (77000000000L + i).ToString() }).ToArray()
            };

            var ex = await Assert.ThrowsAsync<MobizonException>(() => Client(mockHttp).Campaigns.AddRecipientsAsync(request));

            var progress = AddRecipientsProgress.FromException(ex)!;
            Assert.Equal(500, progress.ConfirmedCount);
            Assert.Equal(500, progress.Confirmed.Entries!.Count);
        }
    }
}
