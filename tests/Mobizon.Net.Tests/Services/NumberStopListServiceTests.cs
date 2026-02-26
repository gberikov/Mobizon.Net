using System;
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.StopLists;
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

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.TotalItemCount);
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
            await service.ListAsync(new Mobizon.Contracts.Models.StopLists.StopListListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 25 },
                Sort = new SortRequest { Field = "createTs", Direction = SortDirection.DESC }
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
                    @"{""code"":0,""data"":{""items"":[{""id"":""83486"",""userId"":""88296"",""partnerId"":""2"",""number"":""77007782006"",""ignoreSingle"":""1"",""level"":""0"",""createdByUserId"":""88296"",""createdByUserName"":null,""createdByUserSurname"":null,""createTs"":""2026-02-24 16:19:33"",""comment"":""\u041f\u0440\u043e\u0432\u0435\u0440\u043a\u0430"",""isSystem"":""0"",""countryA2"":""KZ"",""operatorName"":""Altel""}],""totalItemCount"":""1""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            Assert.Equal(1, result.Data.TotalItemCount);
            var e = result.Data.Items[0];
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
                .WithFormData("comment", "Проверка")
                .Respond("application/json",
                    @"{""code"":0,""data"":""83486"",""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.AddNumberAsync("77007782006", "Проверка");

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(83486, result.Data);
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

            Assert.Equal(99001, result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── AddNumberRangeAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task AddNumberRangeAsync_SendsFormData_ReturnsTrue()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/numberstoplist/create")
                .WithFormData("id", "")
                .WithFormData("numberFrom", "77470944000")
                .WithFormData("numberTo", "77470944099")
                .WithFormData("comment", "Тест диапазона")
                .Respond("application/json",
                    @"{""code"":0,""data"":true,""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.AddNumberRangeAsync("77470944000", "77470944099", "Тест диапазона");

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.True(result.Data);
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
            var result = await service.AddNumberRangeAsync("77000000000", "77000000099");

            Assert.True(result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── DeleteAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_SendsFormData_ReturnsTrue()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/numberstoplist/delete")
                .WithFormData("id", "83486")
                .Respond("application/json",
                    @"{""code"":0,""data"":true,""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.DeleteAsync(83486);

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.True(result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
