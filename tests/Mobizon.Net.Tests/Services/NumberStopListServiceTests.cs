using System;
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class NumberStopListServiceTests
    {
        private const string BaseUrl = "https://api.mobizon.kz";

        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-key",
            ApiUrl = BaseUrl
        };

        private NumberStopListService CreateService(MockHttpMessageHandler mockHttp)
        {
            var apiClient = new MobizonApiClient(mockHttp.ToHttpClient(), _options);
            return new NumberStopListService(apiClient);
        }

        // ── ListAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task ListAsync_WithoutRequest_SendsNoBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/numberstoplist/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalItemCount);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithPaginationAndSort_SendsFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/numberstoplist/list")
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]", "25")
                .WithFormData("sort[createTs]", "DESC")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[],""totalItemCount"":""0""},""message"":""""}");

            var service = CreateService(mockHttp);
            await service.ListAsync(new Mobizon.Contracts.StopListListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 25 },
                Sort = new SortRequest { Field = "createTs", Direction = SortDirection.Descending }
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_DeserializesEntryFields()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/numberstoplist/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""83486"",""userId"":""88296"",""partnerId"":""2"",""number"":""77007782006"",""ignoreSingle"":""1"",""level"":""0"",""createdByUserId"":""88296"",""createdByUserName"":null,""createdByUserSurname"":null,""createTs"":""2026-02-24 16:19:33"",""comment"":""Manual check"",""isSystem"":""0"",""countryA2"":""KZ"",""operatorName"":""Altel""}],""totalItemCount"":""1""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            Assert.Equal(1, result.TotalItemCount);
            var e = result.Items[0];
            Assert.Equal(83486, e.Id);
            Assert.Equal(88296, e.UserId);
            Assert.Equal(2, e.PartnerId);
            Assert.Equal("77007782006", e.Number);
            Assert.True(e.IgnoreSingle);
            Assert.Equal(StopListLevel.User, e.Level);
            Assert.Equal(88296, e.CreatedByUserId);
            Assert.Equal(new DateTime(2026, 2, 24, 16, 19, 33), e.Created);
            Assert.False(e.IsSystem);
            Assert.Equal("KZ", e.CountryA2);
            Assert.Equal("Altel", e.OperatorName);
        }

        // ── AddNumberAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task AddNumberAsync_SendsFormData_ReturnsId()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/numberstoplist/create")
                .WithFormData("id", "")
                .WithFormData("number", "77007782006")
                .WithFormData("comment", "Manual check")
                .Respond("application/json",
                    @"{""code"":0,""data"":""83486"",""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.AddNumberAsync("77007782006", "Manual check");

            Assert.Equal(83486, result);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task AddNumberAsync_WithoutComment_SendsEmptyComment()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/numberstoplist/create")
                .WithFormData("id", "")
                .WithFormData("number", "77001234567")
                .WithFormData("comment", "")
                .Respond("application/json",
                    @"{""code"":0,""data"":""99001"",""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.AddNumberAsync("77001234567");

            Assert.Equal(99001, result);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── AddNumberRangeAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task AddNumberRangeAsync_SendsFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/numberstoplist/create")
                .WithFormData("id", "")
                .WithFormData("numberFrom", "77470944000")
                .WithFormData("numberTo", "77470944099")
                .WithFormData("comment", "Range test")
                .Respond("application/json",
                    @"{""code"":0,""data"":true,""message"":""""}");

            var service = CreateService(mockHttp);
            await service.AddNumberRangeAsync("77470944000", "77470944099", "Range test");

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task AddNumberRangeAsync_WithoutComment_SendsEmptyComment()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/numberstoplist/create")
                .WithFormData("id", "")
                .WithFormData("numberFrom", "77000000000")
                .WithFormData("numberTo", "77000000099")
                .WithFormData("comment", "")
                .Respond("application/json",
                    @"{""code"":0,""data"":true,""message"":""""}");

            var service = CreateService(mockHttp);
            await service.AddNumberRangeAsync("77000000000", "77000000099");

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── DeleteAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_SendsFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/numberstoplist/delete")
                .WithFormData("id", "83486")
                .Respond("application/json",
                    @"{""code"":0,""data"":true,""message"":""""}");

            var service = CreateService(mockHttp);
            await service.DeleteAsync(83486);

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
