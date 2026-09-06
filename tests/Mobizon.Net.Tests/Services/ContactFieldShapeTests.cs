using System;
using System.Net.Http;
using System.Threading.Tasks;
using Mobizon.Contracts;
using RichardSzalay.MockHttp;
using Xunit;

namespace Mobizon.Net.Tests.Services
{
    /// <summary>
    /// Regression tests for the second review's finding that the "tolerant" contact-card converters turned
    /// unexpected but non-empty data into <see langword="null"/>, which the next update then wrote back as an
    /// empty value.
    /// </summary>
    public class ContactFieldShapeTests
    {
        private const string Base = "https://api.mobizon.kz";

        private static MobizonClient Client(MockHttpMessageHandler mockHttp) =>
            new MobizonClient(mockHttp.ToHttpClient(), new MobizonClientOptions { ApiKey = "k", ApiUrl = Base });

        private static MockHttpMessageHandler Card(string fields)
        {
            var mockHttp = new MockHttpMessageHandler();
            // Expect, not When: a later Expect for the update must queue behind this one.
            mockHttp.Expect(HttpMethod.Post, Base + "/service/contactcard/get")
                .Respond("application/json",
                    @"{""code"":0,""data"":{""id"":""1"",""isDeleted"":""0"",""isAvailable"":""1"",""fields"":{" + fields + @"},""groups"":[]},""message"":""""}");
            return mockHttp;
        }

        // ── Empty forms stay tolerated ────────────────────────────────────────

        [Theory]
        [InlineData("[]")]
        [InlineData("\"\"")]
        [InlineData("null")]
        public async Task UnsetField_IsNull(string json)
        {
            var card = await Client(Card(@"""email"":" + json)).ContactCards.FindAsync(1);

            Assert.Null(card!.Email);
        }

        // ── Non-empty scalars are data, not absence ───────────────────────────

        [Fact]
        public async Task ScalarEmail_IsMappedToValue_NotDropped()
        {
            var card = await Client(Card(@"""email"":""alice@example.com""")).ContactCards.FindAsync(1);

            Assert.Equal("alice@example.com", card!.Email!.Value);
        }

        [Fact]
        public async Task ScalarMobile_IsMappedToValue_NotDropped()
        {
            var card = await Client(Card(@"""mobile"":""77001234567""")).ContactCards.FindAsync(1);

            Assert.Equal("77001234567", card!.Mobile!.Value);
        }

        [Theory]
        [InlineData(@"""address"":""Almaty, Abay 1""")]
        [InlineData(@"""email"":[{""value"":""alice@example.com""}]")]
        [InlineData(@"""email"":42")]
        [InlineData(@"""email"":true")]
        public async Task IncompatibleShape_IsProtocolError_NotSilentNull(string fields)
        {
            var ex = await Assert.ThrowsAsync<MobizonException>(() => Client(Card(fields)).ContactCards.FindAsync(1));

            Assert.IsNotType<MobizonApiException>(ex);
            Assert.DoesNotContain("alice@example.com", ex.ToString());
        }

        // ── Unknown enum values survive a round trip ──────────────────────────

        [Fact]
        public async Task UnknownContactType_KeepsRawValue_AndIsEchoedBackOnUpdate()
        {
            var mockHttp = Card(@"""mobile"":{""value"":""77001234567"",""type"":""WORK""}");
            mockHttp.Expect(HttpMethod.Post, Base + "/service/contactcard/update")
                .With(req => req.Content!.ReadAsStringAsync().Result.Contains("WORK"))
                .Respond("application/json", @"{""code"":0,""data"":true,""message"":""""}");

            var client = Client(mockHttp);
            var card = await client.ContactCards.FindAsync(1);

            Assert.Null(card!.Mobile!.Type);           // not a value this SDK knows
            Assert.Equal("WORK", card.Mobile.TypeRaw); // but not lost either

            await client.ContactCards.UpdateAsync(card);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task KnownContactType_IsWrittenInCanonicalCase()
        {
            var mockHttp = Card(@"""mobile"":{""value"":""77001234567"",""type"":""main""}");
            mockHttp.Expect(HttpMethod.Post, Base + "/service/contactcard/update")
                .With(req => req.Content!.ReadAsStringAsync().Result.Contains("MAIN"))
                .Respond("application/json", @"{""code"":0,""data"":true,""message"":""""}");

            var client = Client(mockHttp);
            var card = await client.ContactCards.FindAsync(1);

            Assert.Equal(ContactType.Main, card!.Mobile!.Type);

            await client.ContactCards.UpdateAsync(card);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task UnknownGender_KeepsRawValue_AndIsEchoedBackOnUpdate()
        {
            var mockHttp = Card(@"""gender"":""nonbinary""");
            mockHttp.Expect(HttpMethod.Post, Base + "/service/contactcard/update")
                .With(req => req.Content!.ReadAsStringAsync().Result.Contains("nonbinary"))
                .Respond("application/json", @"{""code"":0,""data"":true,""message"":""""}");

            var client = Client(mockHttp);
            var card = await client.ContactCards.FindAsync(1);

            Assert.Null(card!.Gender);
            Assert.Equal("nonbinary", card.GenderRaw);

            await client.ContactCards.UpdateAsync(card);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData("\"male\"", Gender.Male)]
        [InlineData("\"MALE\"", Gender.Male)]
        [InlineData("\"female\"", Gender.Female)]
        [InlineData("[]", null)]
        [InlineData("\"\"", null)]
        public async Task Gender_IsParsedFromEveryObservedForm(string json, Gender? expected)
        {
            var card = await Client(Card(@"""gender"":" + json)).ContactCards.FindAsync(1);

            Assert.Equal(expected, card!.Gender);
        }

        [Fact]
        public void SettingGender_FormatsTheWireValueInvariantly()
        {
            var card = new ContactCard { Gender = Gender.Female };
            Assert.Equal("female", card.GenderRaw);

            card.Gender = null;
            Assert.Null(card.GenderRaw);

            card.Mobile = new MobileFieldInfo { Type = ContactType.Additional };
            Assert.Equal("ADDITIONAL", card.Mobile.TypeRaw);
        }
    }
}
