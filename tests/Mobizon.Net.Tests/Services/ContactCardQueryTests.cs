using System;
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.ContactCards;
using Mobizon.Net.ContactCards;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class ContactCardQueryTests
    {
        private const string BaseUrl = "https://api.mobizon.kz";
        private const string ListUrl = BaseUrl + "/service/contactcard/list";

        private const string EmptyListJson =
            @"{""code"":0,""data"":{""items"":[],""totalItemCount"":0,""fullListItemCount"":0},""message"":""""}";

        private const string SingleItemJson =
            @"{""code"":0,""data"":{""items"":[{""id"":""100604"",""userId"":""88296"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{""name"":""John"",""surname"":""Doe"",""mobile"":{""value"":""77001234567""}},""groups"":[]}],""totalItemCount"":""1"",""fullListItemCount"":""1""},""message"":""""}";

        private const string TwoItemsJson =
            @"{""code"":0,""data"":{""items"":[{""id"":""1"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{},""groups"":[]},{""id"":""2"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{},""groups"":[]}],""totalItemCount"":""2"",""fullListItemCount"":""2""},""message"":""""}";

        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-key",
            ApiUrl = BaseUrl
        };

        private ContactCardService CreateService(MockHttpMessageHandler mockHttp)
            => new ContactCardService(new MobizonApiClient(mockHttp.ToHttpClient(), _options));

        // ── Where: equal ─────────────────────────────────────────────────────

        [Fact]
        public async Task Where_GroupIdEqual_SendsEqualCriteria()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "groupId")
                .WithFormData("criteria[0][operator]", "equal")
                .WithFormData("criteria[0][value]",    "100604")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp).Where(x => x.GroupId == 100604).ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Where: empty ─────────────────────────────────────────────────────

        [Fact]
        public async Task Where_GroupIdNull_SendsEmptyCriteria()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "groupId")
                .WithFormData("criteria[0][operator]", "empty")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp).Where(x => x.GroupId == null).ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Where: contain ───────────────────────────────────────────────────

        [Fact]
        public async Task Where_SurnameContains_SendsContainCriteria()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "surname")
                .WithFormData("criteria[0][operator]", "contain")
                .WithFormData("criteria[0][value]",    "Петр")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp).Where(x => x.Surname.Contains("Петр")).ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Where: from / to ─────────────────────────────────────────────────

        [Fact]
        public async Task Where_DateRange_SendsFromToCriteria()
        {
            var from = new DateTime(2026, 1, 1);
            var to   = new DateTime(2026, 3, 1);

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "createTs")
                .WithFormData("criteria[0][operator]", "from")
                .WithFormData("criteria[0][value]",    "2026-01-01 00:00:00")
                .WithFormData("criteria[1][field]",    "createTs")
                .WithFormData("criteria[1][operator]", "to")
                .WithFormData("criteria[1][value]",    "2026-03-01 00:00:00")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp)
                .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
                .ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Where: multiple conditions ────────────────────────────────────────

        [Fact]
        public async Task Where_MultipleConditions_SendsAllCriteria()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "groupId")
                .WithFormData("criteria[0][operator]", "equal")
                .WithFormData("criteria[0][value]",    "33")
                .WithFormData("criteria[1][field]",    "surname")
                .WithFormData("criteria[1][operator]", "contain")
                .WithFormData("criteria[1][value]",    "Петр")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp)
                .Where(x => x.GroupId == 33 && x.Surname.Contains("Петр"))
                .ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Where: closed-over variable ───────────────────────────────────────

        [Fact]
        public async Task Where_ClosedOverVariable_EvaluatesCorrectly()
        {
            var groupId = 100604;

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "groupId")
                .WithFormData("criteria[0][operator]", "equal")
                .WithFormData("criteria[0][value]",    "100604")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp).Where(x => x.GroupId == groupId).ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Take / Skip ───────────────────────────────────────────────────────

        [Fact]
        public async Task Take_SetsPaginationPageSize()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]",    "10")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp).Take(10).ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task Skip_WithTake_SetsCurrentPage()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "2")
                .WithFormData("pagination[pageSize]",    "25")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp).Take(25).Skip(50).ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── OrderBy / OrderByDescending ───────────────────────────────────────

        [Fact]
        public async Task OrderBy_SetsSortAscending()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("sort[surname]", "ASC")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp).OrderBy(x => x.Surname).ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task OrderByDescending_SetsSortDescending()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("sort[surname]", "DESC")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp).OrderByDescending(x => x.Surname).ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── ToListAsync mapping ───────────────────────────────────────────────

        [Fact]
        public async Task ToListAsync_MapsContactCardDataToEntity()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .Respond("application/json", SingleItemJson);

            var cards = await CreateService(mockHttp).Where(x => x.GroupId == 100604).ToListAsync();

            Assert.Single(cards);
            Assert.Equal(100604, cards[0].Id);
            Assert.Equal("John",        cards[0].Name);
            Assert.Equal("Doe",         cards[0].Surname);
            Assert.Equal("77001234567", cards[0].Mobile);
        }

        // ── First / Single ────────────────────────────────────────────────────

        [Fact]
        public async Task FirstOrDefaultAsync_WhenFound_ReturnsFirst()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]",    "1")
                .Respond("application/json", SingleItemJson);

            var card = await CreateService(mockHttp)
                .Where(x => x.GroupId == 100604)
                .FirstOrDefaultAsync();

            Assert.NotNull(card);
            Assert.Equal(100604, card!.Id);
        }

        [Fact]
        public async Task FirstOrDefaultAsync_WhenEmpty_ReturnsNull()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .Respond("application/json", EmptyListJson);

            var card = await CreateService(mockHttp).Where(x => x.GroupId == 0).FirstOrDefaultAsync();

            Assert.Null(card);
        }

        [Fact]
        public async Task FirstAsync_WhenEmpty_Throws()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .Respond("application/json", EmptyListJson);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService(mockHttp).Where(x => x.GroupId == 0).FirstAsync());
        }

        [Fact]
        public async Task SingleOrDefaultAsync_WhenMoreThanOne_Throws()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .Respond("application/json", TwoItemsJson);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService(mockHttp).Where(x => x.GroupId == 100604).SingleOrDefaultAsync());
        }

        [Fact]
        public async Task SingleOrDefaultAsync_WhenExactlyOne_ReturnsIt()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .Respond("application/json", SingleItemJson);

            var card = await CreateService(mockHttp)
                .Where(x => x.GroupId == 100604)
                .SingleOrDefaultAsync();

            Assert.NotNull(card);
            Assert.Equal(100604, card!.Id);
        }

        // ── Combined ──────────────────────────────────────────────────────────

        [Fact]
        public async Task FullQuery_SendsAllParameters()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",      "groupId")
                .WithFormData("criteria[0][operator]",   "equal")
                .WithFormData("criteria[0][value]",      "100604")
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]",    "25")
                .WithFormData("sort[surname]",           "ASC")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp)
                .Where(x => x.GroupId == 100604)
                .Take(25)
                .Skip(0)
                .OrderBy(x => x.Surname)
                .ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Where: Mobile.Type (nested property) ─────────────────────────────

        [Fact]
        public async Task Where_MobileTypeEqual_SendsNestedFieldCriteria()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "mobile.type")
                .WithFormData("criteria[0][operator]", "equal")
                .WithFormData("criteria[0][value]",    "MAIN")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp)
                .Where(x => x.Mobile.Type == ContactType.Main)
                .ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task Where_MobileTypeAdditional_SendsUpperCaseEnumValue()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "mobile.type")
                .WithFormData("criteria[0][operator]", "equal")
                .WithFormData("criteria[0][value]",    "ADDITIONAL")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp)
                .Where(x => x.Mobile.Type == ContactType.Additional)
                .ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Regression: enum zero-value must not be serialised as "0" ───────────
        //
        // ContactType.Main == 0. The C# compiler can fold enum constants to their
        // underlying integer inside expression trees, causing the parser to receive
        // an (int)0 instead of ContactType.Main and produce "0" instead of "MAIN".
        // The fix in ContactCardExpressionParser.Build recovers the enum from the
        // member's declared type before serialising.

        [Fact]
        public async Task Where_MobileTypeMain_SendsEnumNameNotNumericZero()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "mobile.type")
                .WithFormData("criteria[0][operator]", "equal")
                .WithFormData("criteria[0][value]",    "MAIN")   // regression: was "0"
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp)
                .Where(x => x.Mobile.Type == ContactType.Main)
                .ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task Where_MobileTypeMain_ClosedOverVariable_SendsEnumName()
        {
            // Same regression via a closed-over variable (different expression-tree shape).
            var type = ContactType.Main;

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "mobile.type")
                .WithFormData("criteria[0][operator]", "equal")
                .WithFormData("criteria[0][value]",    "MAIN")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp)
                .Where(x => x.Mobile.Type == type)
                .ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Where: Mobile.Value (nested property) ─────────────────────────────

        [Fact]
        public async Task Where_MobileValueEqual_SendsNestedFieldCriteria()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "mobile.value")
                .WithFormData("criteria[0][operator]", "equal")
                .WithFormData("criteria[0][value]",    "77001234567")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp)
                .Where(x => x.Mobile.Value == "77001234567")
                .ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task Where_MobileValueContains_SendsContainCriteria()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "mobile.value")
                .WithFormData("criteria[0][operator]", "contain")
                .WithFormData("criteria[0][value]",    "7700")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp)
                .Where(x => x.Mobile.Value.Contains("7700"))
                .ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Where: GroupId + Mobile.Type combined ─────────────────────────────

        [Fact]
        public async Task Where_GroupIdAndMobileType_SendsBothCriteria()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, ListUrl)
                .WithFormData("criteria[0][field]",    "groupId")
                .WithFormData("criteria[0][operator]", "equal")
                .WithFormData("criteria[0][value]",    "100604")
                .WithFormData("criteria[1][field]",    "mobile.type")
                .WithFormData("criteria[1][operator]", "equal")
                .WithFormData("criteria[1][value]",    "MAIN")
                .Respond("application/json", EmptyListJson);

            await CreateService(mockHttp)
                .Where(x => x.GroupId == 100604 && x.Mobile.Type == ContactType.Main)
                .ToListAsync();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Unsupported expression ────────────────────────────────────────────

        [Fact]
        public async Task Where_UnsupportedExpression_ThrowsNotSupportedException()
        {
            var service = CreateService(new MockHttpMessageHandler());

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                service.Where(x => x.GroupId != 33).ToListAsync());
        }
    }
}
