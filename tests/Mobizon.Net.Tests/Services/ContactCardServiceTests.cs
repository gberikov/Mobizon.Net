using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.ContactCards;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    public class ContactCardServiceTests
    {
        private const string BaseUrl = "https://api.mobizon.kz";

        private readonly MobizonClientOptions _options = new MobizonClientOptions
        {
            ApiKey = "test-key",
            ApiUrl = BaseUrl
        };

        private ContactCardService CreateService(MockHttpMessageHandler mockHttp)
        {
            var apiClient = new MobizonApiClient(mockHttp.ToHttpClient(), _options);
            return new ContactCardService(apiClient);
        }

        // ── ListAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task ListAsync_WithoutRequest_SendsNoBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[],""totalItemCount"":0,""fullListItemCount"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.TotalItemCount);
            Assert.Equal(0, result.Data.FullListItemCount);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithGroupIdCriteria_SendsFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/list")
                .WithFormData("criteria[0][field]", "groupId")
                .WithFormData("criteria[0][operator]", "equal")
                .WithFormData("criteria[0][value]", "100604")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""77885834"",""userId"":""88296"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{""name"":""Gabin"",""mobile"":{""value"":""77029932233"",""countryA2"":""KZ"",""operator"":""Теле2""}},""groups"":[{""id"":""100604"",""name"":""Test Group""}]}],""totalItemCount"":""1"",""fullListItemCount"":""308""},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync(new ContactCardListRequest
            {
                Criteria = new[]
                {
                    new ContactCardCriteria { Field = "groupId", Operator = "equal", Value = "100604" }
                }
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Single(result.Data.Items);
            Assert.Equal(1, result.Data.TotalItemCount);
            Assert.Equal(308, result.Data.FullListItemCount);

            var card = result.Data.Items[0];
            Assert.Equal(77885834, card.Id);
            Assert.False(card.IsDeleted);
            Assert.True(card.IsAvailable);
            Assert.Equal("Gabin", card.Fields!.Name);
            Assert.Equal("77029932233", card.Fields.Mobile!.Value);
            Assert.Equal("KZ", card.Fields.Mobile.CountryA2);
            Assert.Single(card.Groups!);
            Assert.Equal("100604", card.Groups![0].Id);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithNoGroupCriteria_SendsEmptyOperator()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/list")
                .WithFormData("criteria[0][field]", "groupId")
                .WithFormData("criteria[0][operator]", "empty")
                .WithFormData("criteria[0][value]", "-1")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[],""totalItemCount"":0,""fullListItemCount"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            await service.ListAsync(new ContactCardListRequest
            {
                Criteria = new[]
                {
                    new ContactCardCriteria { Field = "groupId", Operator = "empty", Value = "-1" }
                }
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_WithPaginationAndSort_SendsFormData()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/list")
                .WithFormData("pagination[currentPage]", "0")
                .WithFormData("pagination[pageSize]", "25")
                .WithFormData("sort[fullName]", "ASC")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[],""totalItemCount"":0,""fullListItemCount"":0},""message"":""""}");

            var service = CreateService(mockHttp);
            await service.ListAsync(new ContactCardListRequest
            {
                Pagination = new PaginationRequest { CurrentPage = 0, PageSize = 25 },
                Sort = new SortRequest { Field = "fullName", Direction = SortDirection.ASC }
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ListAsync_DeserializesContactFieldsFully()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""1"",""userId"":""88296"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{""title"":""Mr"",""name"":""John"",""surname"":""Doe"",""mobile"":{""value"":""77001234567"",""type"":""MAIN"",""countryA2"":""KZ"",""countryName"":""Казахстан"",""operatorId"":""304"",""operator"":""Altel"",""isMNP"":""0""},""email"":""john@example.com"",""birth_date"":""1990-01-15"",""gender"":""male"",""company_name"":""Acme"",""company_url"":""https://acme.com"",""info"":""VIP""},""groups"":[]}],""totalItemCount"":1,""fullListItemCount"":1},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            var f = result.Data.Items[0].Fields!;
            Assert.Equal("Mr", f.Title);
            Assert.Equal("John", f.Name);
            Assert.Equal("Doe", f.Surname);
            Assert.Equal("77001234567", f.Mobile!.Value);
            Assert.Equal("MAIN", f.Mobile.Type);
            Assert.Equal("KZ", f.Mobile.CountryA2);
            Assert.Equal("304", f.Mobile.OperatorId);
            Assert.Equal("Altel", f.Mobile.Operator);
            Assert.Equal("0", f.Mobile.IsMNP);
            Assert.Equal("john@example.com", f.Email);
            Assert.Equal("1990-01-15", f.BirthDate);
            Assert.Equal("male", f.Gender);
            Assert.Equal("Acme", f.CompanyName);
            Assert.Equal("https://acme.com", f.CompanyUrl);
            Assert.Equal("VIP", f.Info);
        }

        // ── GetAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAsync_SendsFormData_ReturnsCard()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/get")
                .WithFormData("id", "77885666")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""id"":""77885666"",""userId"":""88296"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{""name"":""Yкiлi"",""mobile"":{""value"":""77006537475"",""countryA2"":""KZ"",""operator"":""Altel""}},""groups"":[]},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetAsync("77885666");

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(77885666, result.Data.Id);
            Assert.Equal("Yкiлi", result.Data.Fields!.Name);
            Assert.Equal("77006537475", result.Data.Fields.Mobile!.Value);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── CreateAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_SendsMultipartRequest_ReturnsId()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/create")
                .Respond("application/json",
                    @"{""code"":0,""data"":""78045032"",""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.CreateAsync(new CreateContactCardRequest
            {
                Name = "Гани",
                MobileValue = "77017221502"
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal("78045032", result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CreateAsync_WithPhoto_SendsMultipartWithStream()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/create")
                .Respond("application/json",
                    @"{""code"":0,""data"":""78045033"",""message"":""""}");

            using var photoStream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image-bytes"));

            var service = CreateService(mockHttp);
            var result = await service.CreateAsync(new CreateContactCardRequest
            {
                Name = "Test",
                Photo = photoStream,
                PhotoFileName = "avatar.jpg"
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal("78045033", result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── UpdateAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_SendsMultipartRequest_ReturnsTrue()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/update")
                .Respond("application/json",
                    @"{""code"":0,""data"":true,""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.UpdateAsync(new UpdateContactCardRequest
            {
                Id = "77885666",
                Name = "Yкiлi Updated",
                MobileValue = "77006537475"
            });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.True(result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── SetGroupsAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task SetGroupsAsync_SendsFormData_ReturnsTrue()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/setgroups")
                .WithFormData("id", "78045032")
                .WithFormData("groupIds[0]", "100820")
                .Respond("application/json",
                    @"{""code"":0,""data"":true,""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.SetGroupsAsync("78045032", new[] { "100820" });

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.True(result.Data);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SetGroupsAsync_MultipleGroups_SendsAllIds()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/setgroups")
                .WithFormData("id", "100")
                .WithFormData("groupIds[0]", "1")
                .WithFormData("groupIds[1]", "2")
                .WithFormData("groupIds[2]", "3")
                .Respond("application/json",
                    @"{""code"":0,""data"":true,""message"":""""}");

            var service = CreateService(mockHttp);
            await service.SetGroupsAsync("100", new List<string> { "1", "2", "3" });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── GetGroupsAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetGroupsAsync_SendsFormData_ReturnsGroups()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/getgroups")
                .WithFormData("id", "77885666")
                .Respond("application/json",
                    @"{""code"":0,""data"":[{""id"":""100604"",""name"":""Freedom Broker""},{""id"":""100820"",""name"":""VIP""}],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetGroupsAsync("77885666");

            Assert.Equal(MobizonResponseCode.Success, result.Code);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal("100604", result.Data[0].Id);
            Assert.Equal("Freedom Broker", result.Data[0].Name);
            Assert.Equal("100820", result.Data[1].Id);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetGroupsAsync_WhenNoGroups_ReturnsEmptyList()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/getgroups")
                .Respond("application/json",
                    @"{""code"":0,""data"":[],""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.GetGroupsAsync("77885666");

            Assert.Empty(result.Data);
        }
    }
}
