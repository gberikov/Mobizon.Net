using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Mobizon.Contracts;
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

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalItemCount);
            Assert.Equal(0, result.FullListItemCount);
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

            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalItemCount);
            Assert.Equal(308, result.FullListItemCount);

            var card = result.Items[0];
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
                    @"{""code"":0,""data"":{""items"":[{""id"":""1"",""userId"":""88296"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{""title"":""Mr"",""name"":""John"",""surname"":""Doe"",""mobile"":{""value"":""77001234567"",""type"":""MAIN"",""countryA2"":""KZ"",""countryName"":""Казахстан"",""operatorId"":""304"",""operator"":""Altel"",""isMNP"":""0""},""email"":{""value"":""john@example.com"",""type"":""ADDITIONAL""},""birth_date"":""1990-01-15"",""gender"":""male"",""company_name"":""Acme"",""company_url"":""https://acme.com"",""info"":""VIP""},""groups"":[]}],""totalItemCount"":1,""fullListItemCount"":1},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            var f = result.Items[0].Fields!;
            Assert.Equal("Mr", f.Title);
            Assert.Equal("John", f.Name);
            Assert.Equal("Doe", f.Surname);
            Assert.Equal("77001234567", f.Mobile!.Value);
            Assert.Equal(ContactType.Main, f.Mobile.Type);
            Assert.Equal("KZ", f.Mobile.CountryA2);
            Assert.Equal("304", f.Mobile.OperatorId);
            Assert.Equal("Altel", f.Mobile.Operator);
            Assert.Equal(false, f.Mobile.IsMNP);
            Assert.Equal("john@example.com", f.Email?.Value);
            Assert.Equal(ContactType.Additional, f.Email?.Type);
            Assert.Equal("1990-01-15", f.BirthDate);
            Assert.Equal("male", f.Gender);
            Assert.Equal("Acme", f.CompanyName);
            Assert.Equal("https://acme.com", f.CompanyUrl);
            Assert.Equal("VIP", f.Info);
        }

        [Fact]
        public async Task ListAsync_DeserializesAddress()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""1"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{""name"":""John"",""address"":{""countryA2"":""KZ"",""country"":""Казахстан"",""regionId"":""5"",""region"":""Алматинская"",""cityId"":""77"",""city"":""Алматы"",""postalcode"":""050000"",""street"":""Абая"",""building"":""10"",""other"":""офис 3""}},""groups"":[]}],""totalItemCount"":1,""fullListItemCount"":1},""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.ListAsync();

            var a = result.Items[0].Fields!.Address!;
            Assert.Equal("KZ", a.CountryA2);
            Assert.Equal("Казахстан", a.Country);
            Assert.Equal("5", a.RegionId);
            Assert.Equal("Алматинская", a.Region);
            Assert.Equal("77", a.CityId);
            Assert.Equal("Алматы", a.City);
            Assert.Equal("050000", a.PostalCode);
            Assert.Equal("Абая", a.Street);
            Assert.Equal("10", a.Building);
            Assert.Equal("офис 3", a.Other);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        // ── Empty / sparse field tolerance (PHP loose typing) ────────────────

        [Fact]
        public async Task ListAsync_EmptyFieldsAsArrays_DeserializeToNull()
        {
            // The PHP API serialises unset object fields as an empty array `[]`.
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, $"{BaseUrl}/service/contactcard/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""1"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{""name"":""John"",""mobile"":[],""email"":[],""address"":[]},""groups"":[]}],""totalItemCount"":1,""fullListItemCount"":1},""message"":""""}");

            var service = CreateService(mockHttp);
            var f = (await service.ListAsync()).Items[0].Fields!;

            Assert.Equal("John", f.Name);
            Assert.Null(f.Mobile);
            Assert.Null(f.Email);
            Assert.Null(f.Address);
        }

        [Fact]
        public async Task ListAsync_EmptyFieldsAsStrings_DeserializeToNull()
        {
            // The PHP API may also serialise an unset field as an empty string `""`.
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, $"{BaseUrl}/service/contactcard/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""1"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{""name"":""John"",""mobile"":"""",""email"":""""},""groups"":[]}],""totalItemCount"":1,""fullListItemCount"":1},""message"":""""}");

            var service = CreateService(mockHttp);
            var f = (await service.ListAsync()).Items[0].Fields!;

            Assert.Null(f.Mobile);
            Assert.Null(f.Email);
        }

        [Fact]
        public async Task ListAsync_UnknownContactType_DegradesToNullType()
        {
            // An unknown/future type string must not fail the whole read — the field value survives,
            // only the unrecognised Type degrades to null.
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, $"{BaseUrl}/service/contactcard/list")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""items"":[{""id"":""1"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{""mobile"":{""value"":""77001234567"",""type"":""WORK""}},""groups"":[]}],""totalItemCount"":1,""fullListItemCount"":1},""message"":""""}");

            var service = CreateService(mockHttp);
            var f = (await service.ListAsync()).Items[0].Fields!;

            Assert.Equal("77001234567", f.Mobile!.Value);
            Assert.Null(f.Mobile.Type);
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

            Assert.Equal(77885666, result.Id);
            Assert.Equal("Yкiлi", result.Fields!.Name);
            Assert.Equal("77006537475", result.Fields.Mobile!.Value);
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

            Assert.Equal("78045032", result);
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

            Assert.Equal("78045033", result);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CreateAsync_WithAddress_SendsAddressFields()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/create")
                .With(req =>
                {
                    var content = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                    return content.Contains("data[address][street]")
                        && content.Contains("Абая")
                        && content.Contains("data[address][city]")
                        && content.Contains("Алматы")
                        && content.Contains("data[address][postalcode]")
                        && content.Contains("050000")
                        && content.Contains("apiKey")
                        && content.Contains("test-key");
                })
                .Respond("application/json",
                    @"{""code"":0,""data"":""78045040"",""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.CreateAsync(new CreateContactCardRequest
            {
                Name = "John",
                Address = new AddressFieldInfo
                {
                    City = "Алматы",
                    Street = "Абая",
                    PostalCode = "050000"
                }
            });

            Assert.Equal("78045040", result);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CreateAsync_WithoutAddress_OmitsAddressFields()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post,
                    $"{BaseUrl}/service/contactcard/create")
                .With(req =>
                {
                    var content = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                    return !content.Contains("data[address]");
                })
                .Respond("application/json",
                    @"{""code"":0,""data"":""78045041"",""message"":""""}");

            var service = CreateService(mockHttp);
            var result = await service.CreateAsync(new CreateContactCardRequest { Name = "John" });

            Assert.Equal("78045041", result);
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
            await service.UpdateAsync(new UpdateContactCardRequest
            {
                Id = "77885666",
                Name = "Yкiлi Updated",
                MobileValue = "77006537475"
            });

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
            await service.SetGroupsAsync("78045032", new[] { "100820" });

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

            Assert.Equal(2, result.Count);
            Assert.Equal("100604", result[0].Id);
            Assert.Equal("Freedom Broker", result[0].Name);
            Assert.Equal("100820", result[1].Id);
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

            Assert.Empty(result);
        }

        // ── RemoveAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task RemoveAsync_SendsDeleteRequest()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, $"{BaseUrl}/service/contactcard/delete")
                .WithFormData("id", "77885666")
                .Respond("application/json", @"{""code"":0,""data"":true,""message"":""""}");

            await CreateService(mockHttp).RemoveAsync("77885666");

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
