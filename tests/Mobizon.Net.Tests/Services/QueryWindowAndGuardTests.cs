using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    /// <summary>Regression tests for R4 (query window), R5 (culture-invariant dates), R10 (DTO/converter hardening) and R11 (guards).</summary>
    public class QueryWindowAndGuardTests
    {
        private const string Base = "https://api.mobizon.kz";
        private const string ListUrl = Base + "/service/contactcard/list";

        private static MobizonClient Client(MockHttpMessageHandler mockHttp) =>
            new MobizonClient(mockHttp.ToHttpClient(), new MobizonClientOptions { ApiKey = "k", ApiUrl = Base });

        private static string Page(int total, params long[] ids)
        {
            var items = string.Join(",", Array.ConvertAll(ids, id =>
                $@"{{""id"":""{id}"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{{""name"":""n{id}""}},""groups"":[]}}"));
            return $@"{{""code"":0,""data"":{{""items"":[{items}],""totalItemCount"":""{total}""}},""message"":""""}}";
        }

        // ── R4: First/Single/Count respect the selected window ────────────────

        [Fact]
        public async Task FirstOrDefault_WithExplicitPage_FetchesThatPage_NotOffsetOne()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "2")
                .WithFormData("pagination[pageSize]", "50")
                .Respond("application/json", Page(120, 101, 102));

            var first = await Client(mockHttp).ContactCards.Take(50).Page(2).FirstOrDefaultAsync();

            Assert.Equal(101, first!.Id);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task FirstOrDefault_WithPageZeroAndDefaultSize_MatchesHeadOfToList()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]", "25")
                .Respond("application/json", Page(3, 7, 8, 9));

            var query = Client(mockHttp).ContactCards.Page(0);
            var list = await query.ToListAsync();
            var first = await query.FirstOrDefaultAsync();

            Assert.Equal(list[0].Id, first!.Id);
        }

        [Fact]
        public async Task FirstOrDefault_WithoutPage_AsksForTheFirstPage_AtADocumentedPageSize()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]", "25")
                .Respond("application/json", Page(3, 7));

            var first = await Client(mockHttp).ContactCards.Take(50).FirstOrDefaultAsync();

            Assert.Equal(7, first!.Id);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task FirstOrDefault_OnEmptyLastPage_ReturnsNull()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "9")
                .Respond("application/json", Page(3));

            Assert.Null(await Client(mockHttp).ContactCards.Take(10).Page(9).FirstOrDefaultAsync());
        }

        [Fact]
        public async Task Count_IgnoresSelectedPage()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]", "25")
                .Respond("application/json", Page(42, 1));

            Assert.Equal(42, await Client(mockHttp).ContactCards.Take(10).Page(7).CountAsync());
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData(0, new long[0], false, false)]
        [InlineData(1, new long[] { 5 }, true, false)]
        [InlineData(2, new long[] { 5, 6 }, false, true)]
        [InlineData(2, new long[] { 5 }, false, true)] // server total says two even though one arrived
        public async Task Single_ChecksWholeQuery_IgnoringPage(int total, long[] ids, bool found, bool throws)
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]", "25")
                .Respond("application/json", Page(total, ids));

            var query = Client(mockHttp).ContactCards.Take(10).Page(3).Where(x => x.GroupId == 1);
            if (throws)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => query.SingleOrDefaultAsync());
            }
            else
            {
                var card = await query.SingleOrDefaultAsync();
                Assert.Equal(found, card != null);
            }
            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── R5: dates on the wire do not depend on CurrentCulture ─────────────

        [Theory]
        [InlineData("tr-TR")]
        [InlineData("ru-RU")]
        [InlineData("en-US")]
        [InlineData("ar-SA")]
        public async Task DateFilters_AreCultureInvariant(string culture)
        {
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            try
            {
                var birth = new DateTime(1990, 12, 31);
                var created = new DateTime(2026, 1, 2, 3, 4, 5);
                var mockHttp = new MockHttpMessageHandler();
                mockHttp.Expect(HttpMethod.Post, ListUrl)
                    .WithFormData("criteria[0][field]", "birth_date")
                    .WithFormData("criteria[0][value]", "1990-12-31")
                    .WithFormData("criteria[1][field]", "createTs")
                    .WithFormData("criteria[1][value]", "2026-01-02 03:04:05")
                    .Respond("application/json", Page(0));

                await Client(mockHttp).ContactCards
                    .Where(x => x.BirthDate >= birth && x.CreatedAt <= created)
                    .ToListAsync();

                mockHttp.VerifyNoOutstandingExpectation();
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public async Task CapturedVariable_IsReadAtEachExecution_NotCached()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl).WithFormData("criteria[0][value]", "1").Respond("application/json", Page(0));
            mockHttp.Expect(HttpMethod.Post, ListUrl).WithFormData("criteria[0][value]", "2").Respond("application/json", Page(0));

            long groupId = 1;
            var query = Client(mockHttp).ContactCards.Where(x => x.GroupId == groupId);
            await query.ToListAsync();
            groupId = 2;
            await query.ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("ru-RU")]
        public async Task NumericCast_IsEvaluatedBeforeFormatting_AtEachExecution(string culture)
        {
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            try
            {
                using var mockHttp = new MockHttpMessageHandler();
                mockHttp.Expect(HttpMethod.Post, ListUrl).WithFormData("criteria[0][value]", "1").Respond("application/json", Page(0));
                mockHttp.Expect(HttpMethod.Post, ListUrl).WithFormData("criteria[0][value]", "2").Respond("application/json", Page(0));
                using var client = Client(mockHttp);
                double value = 1.9;
                var query = client.ContactCards.Where(x => x.GroupId == (long)value);

                await query.ToListAsync();
                value = 2.9;
                await query.ToListAsync();

                mockHttp.VerifyNoOutstandingExpectation();
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public async Task NarrowingCast_PreservesUncheckedTruncation()
        {
            using var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl).WithFormData("criteria[0][value]", "1").Respond("application/json", Page(0));
            using var client = Client(mockHttp);
            long value = 4294967297L;

            await client.ContactCards.Where(x => x.GroupId == unchecked((int)value)).ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task UserDefinedCast_ExecutesConversionOperator()
        {
            using var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl).WithFormData("criteria[0][value]", "42").Respond("application/json", Page(0));
            using var client = Client(mockHttp);
            var value = new GroupKey(41);

            await client.ContactCards.Where(x => x.GroupId == (long)value).ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        private readonly struct GroupKey
        {
            private readonly long _value;
            public GroupKey(long value) => _value = value;
            public static explicit operator long(GroupKey value) => value._value + 1;
        }

        // ── R10: DTO completeness and converter strictness ────────────────────

        [Fact]
        public async Task Alphanames_ExposeStatusUpdateCommentsAndDocuments()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, Base + "/service/alphaname/list")
                .Respond("application/json", Fixtures.Load("alphaname.list.json"));

            var item = (await Client(mockHttp).Alphanames.ListAsync()).Items[0];

            Assert.Equal(new DateTime(2025, 2, 4, 9, 26, 57), item.StatusUpdated);
            Assert.Equal(string.Empty, item.GlobalComment);
            Assert.Equal(string.Empty, item.PartnerComment);
            Assert.Equal(3, item.Documents!.Count);
            Assert.Equal(4404, item.Documents[0].UserDocId);
            Assert.Equal(88296, item.Documents[0].UserId);
            Assert.Equal(58356, item.Documents[0].AlphanameId);
        }

        [Fact]
        public async Task LinkStats_EmptyPhpArray_IsEmptyResult()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, Base + "/service/link/getstats")
                .Respond("application/json", @"{""code"":0,""data"":[],""message"":""""}");

            var stats = await Client(mockHttp).Links.GetStatsAsync(new GetLinkStatsRequest { Ids = new[] { 1L }, Type = LinkStatsType.Daily });

            Assert.NotNull(stats);
            Assert.True(stats.Links == null || stats.Links.Count == 0);
        }

        [Theory]
        [InlineData(@"{""items"":[{""param"":""2026-01"",""clicks0"":""lots""}],""totals"":{}}")]
        [InlineData(@"{""items"":[{""param"":""2026-01"",""clicks0"":99999999999}],""totals"":{}}")]
        [InlineData(@"""just a string""")]
        public async Task LinkStats_CorruptCounter_IsProtocolError_NotZero(string data)
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, Base + "/service/link/getstats")
                .Respond("application/json", @"{""code"":0,""data"":" + data + @",""message"":""""}");

            var ex = await Assert.ThrowsAsync<MobizonException>(() =>
                Client(mockHttp).Links.GetStatsAsync(new GetLinkStatsRequest { Ids = new[] { 1L }, Type = LinkStatsType.Daily }));

            Assert.IsNotType<MobizonApiException>(ex);
            Assert.DoesNotContain("lots", ex.ToString());
        }

        [Fact]
        public async Task LinkStats_NullAndEmptyCounters_CountAsZero()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, Base + "/service/link/getstats")
                .Respond("application/json", @"{""code"":0,""data"":{""items"":[{""param"":""2026-01"",""clicks0"":null,""redirects0"":""""}],""totals"":{""totalClicks0"":""2""}},""message"":""""}");

            var stats = await Client(mockHttp).Links.GetStatsAsync(new GetLinkStatsRequest { Ids = new[] { 1L }, Type = LinkStatsType.Daily });

            Assert.Equal(0, stats.Links![0].Points![0].Clicks);
            Assert.Equal(2, stats.Links[0].TotalClicks);
        }

        // ── R11: guards fire before any HTTP call ─────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public async Task GetSmsStatus_RejectsOutOfRangeIdCount(int count)
        {
            var mockHttp = new MockHttpMessageHandler(); // no handlers: any request would fail differently
            var ids = new long[count];

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => Client(mockHttp).Messages.GetSmsStatusAsync(ids));
            Assert.Equal("ids", ex.ParamName);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public async Task GetSmsStatus_AcceptsBoundaryIdCounts(int count)
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Base + "/service/Message/GetSMSStatus")
                .WithFormData($"ids[{count - 1}]", (count - 1).ToString())
                .Respond("application/json", @"{""code"":0,""data"":[],""message"":""""}");
            var ids = new long[count];
            for (var i = 0; i < count; i++) ids[i] = i;

            await Client(mockHttp).Messages.GetSmsStatusAsync(ids);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetSmsStatus_NullIds_ThrowsArgumentNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Client(new MockHttpMessageHandler()).Messages.GetSmsStatusAsync((long[])null!));
        }

        [Fact]
        public async Task PublicMethods_RejectMissingRequiredInput_BeforeSending()
        {
            var client = Client(new MockHttpMessageHandler());

            await Assert.ThrowsAsync<ArgumentException>(() => client.Messages.QuickSendAsync("", "text"));
            await Assert.ThrowsAsync<ArgumentException>(() => client.Messages.QuickSendAsync("77001234567", ""));
            await Assert.ThrowsAsync<ArgumentNullException>(() => client.Links.CreateAsync(null!));
            await Assert.ThrowsAsync<ArgumentException>(() => client.Links.CreateAsync(new CreateLinkRequest()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => client.Links.DeleteAsync(null!));
            await Assert.ThrowsAsync<ArgumentException>(() => client.Links.DeleteAsync(new long[0]));
            await Assert.ThrowsAsync<ArgumentNullException>(() => client.Links.UpdateAsync(null!));
            await Assert.ThrowsAsync<ArgumentException>(() => client.ContactGroups.CreateAsync(" "));
            await Assert.ThrowsAsync<ArgumentException>(() => client.NumberStopList.AddNumberAsync(""));
            await Assert.ThrowsAsync<ArgumentException>(() => client.NumberStopList.AddNumberRangeAsync("", "77001"));
            await Assert.ThrowsAsync<ArgumentNullException>(() => client.ContactCards.SetGroupsAsync(1, null!));
        }

        [Fact]
        public async Task ContactCard_EmptyAddress_ClearsEveryAddressField()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, Base + "/service/contactcard/update")
                .With(req =>
                {
                    var body = req.Content!.ReadAsStringAsync().Result;
                    return body.Contains("name=\"data[address][city]\"") && body.Contains("name=\"data[address][street]\"");
                })
                .Respond("application/json", @"{""code"":0,""data"":true,""message"":""""}");

            await Client(mockHttp).ContactCards.UpdateAsync(new ContactCard { Id = 5, Name = "n", Address = new AddressFieldInfo() });

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
