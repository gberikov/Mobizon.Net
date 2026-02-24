using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts.Models;
using Mobizon.Contracts.Models.ContactGroup;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class ContactGroupServiceTests
    {
        private const string BaseUrl = "https://api.mobizon.kz";

        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-key",
            ApiUrl = BaseUrl
        };

        private ContactGroupService CreateService(MockHttpMessageHandler mockHttp)
        {
            var apiClient = new MobizonApiClient(mockHttp.ToHttpClient(), _options);
            return new ContactGroupService(apiClient);
        }

        // ── ListAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task ListAsync_WithoutRequest_SendsNoBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactgroup/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[],""totalItemCount"":0},""message"":""""}");

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
                    $"{BaseUrl}/service/contactgroup/list")
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]", "25")
                .WithFormData("sort[name]", "ASC")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""100604"",""userId"":""88296"",""name"":""Test Group"",""cardsCnt"":""308"",""isHidden"":""0"",""createTs"":""2026-02-18 15:07:45""}],""totalItemCount"":1},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync(new ContactGroupListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 25 },
                Sort = new SortRequest { Field = "name", Direction = SortDirection.ASC }
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Single(result.Data.Items);
            Assert.Equal("100604", result.Data.Items[0].Id);
            Assert.Equal("Test Group", result.Data.Items[0].Name);
            Assert.Equal(308, result.Data.Items[0].CardsCnt);
            Assert.Equal(1, result.Data.TotalItemCount);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_DeserializesAllFields()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactgroup/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""1"",""userId"":""88296"",""name"":""Group A"",""cardsCnt"":""12"",""isHidden"":""1"",""createTs"":""2026-01-01 09:00:00""}],""totalItemCount"":""1""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            var item = result.Data.Items[0];
            Assert.Equal("1", item.Id);
            Assert.Equal("88296", item.UserId);
            Assert.Equal("Group A", item.Name);
            Assert.Equal(12, item.CardsCnt);
            Assert.Equal(1, item.IsHidden);
            Assert.Equal("2026-01-01 09:00:00", item.CreateTs);
        }

        // ── CreateAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_SendsJsonBody_ReturnsId()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactgroup/create")
                .WithContent(@"{""data"":{""name"":""New Group""}}")
                .Respond("application/json",
                    @"{""code"":0,""data"":""100820"",""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.CreateAsync("New Group");

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal("100820", result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── UpdateAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_SendsJsonBody_ReturnsTrue()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactgroup/update")
                .WithContent(@"{""id"":""100820"",""data"":{""name"":""Renamed Group""}}")
                .Respond("application/json",
                    @"{""code"":0,""data"":true,""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.UpdateAsync("100820", "Renamed Group");

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.True(result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── DeleteAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_SendsJsonBody_ReturnsProcessedIds()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactgroup/delete")
                .WithContent(@"{""id"":""100820""}")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""processed"":[""100820""],""notProcessed"":[]},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.DeleteAsync("100820");

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Single(result.Data.Processed);
            Assert.Equal("100820", result.Data.Processed[0]);
            Assert.Empty(result.Data.NotProcessed);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task DeleteAsync_WhenNotProcessed_ReturnsNotProcessedIds()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactgroup/delete")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""processed"":[],""notProcessed"":[""999""]},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.DeleteAsync("999");

            Assert.Empty(result.Data.Processed);
            Assert.Single(result.Data.NotProcessed);
            Assert.Equal("999", result.Data.NotProcessed[0]);
        }

        // ── GetCardsCountAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetCardsCountAsync_SendsJsonBody_ReturnsCount()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactgroup/getcardscount")
                .WithContent(@"{""id"":""100604""}")
                .Respond("application/json",
                    @"{""code"":0,""data"":""308"",""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetCardsCountAsync("100604");

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(308, result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetCardsCountAsync_NoGroupContacts_SendsMinusOne()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactgroup/getcardscount")
                .WithContent(@"{""id"":""-1""}")
                .Respond("application/json",
                    @"{""code"":0,""data"":""0"",""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetCardsCountAsync("-1");

            Assert.Equal(0, result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
