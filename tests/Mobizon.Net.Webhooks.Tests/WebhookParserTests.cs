using Mobizon.Contracts;
using Mobizon.Contracts.Webhooks;
using Mobizon.Net.Webhooks;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class WebhookParserTests
    {
        private readonly WebhookParser _parser = new WebhookParser();

        [Fact]
        public void Parse_PopulatesCommonEnvelope()
        {
            var evt = _parser.Parse(Payloads.Load(Payloads.SmsDeliveryReport));

            Assert.Equal(26, evt.EventId);
            Assert.Equal(WebhookEventType.SmsDeliveryReport, evt.EventType);
            Assert.Equal("sms-delivery-report", evt.EventTypeRaw);
            Assert.Equal(1, evt.WebhookId);
            Assert.Equal(1, evt.Attempt);
            Assert.Equal("2026-01-15 11:42:28", evt.EventCreateTsRaw);
            Assert.NotNull(evt.EventCreateTs);
            Assert.Equal("5cdebbb611d765bad163d765e8637830b6fd4060", evt.Sign);
        }

        [Fact]
        public void Parse_UnknownEventType_ReturnsUnknownEvent()
        {
            const string json = @"{
                ""eventId"": 7,
                ""eventType"": ""future-event-type"",
                ""eventCreateTs"": ""2026-03-01 10:00:00"",
                ""webhookId"": 5,
                ""attempt"": 1,
                ""data"": { ""anything"": 123 },
                ""sign"": ""deadbeef""
            }";

            var evt = _parser.Parse(json);

            var unknown = Assert.IsType<UnknownWebhookEvent>(evt);
            Assert.Equal(WebhookEventType.Unknown, unknown.EventType);
            Assert.Equal("future-event-type", unknown.EventTypeRaw);
            Assert.Equal(7, unknown.EventId);
            Assert.Equal(123, unknown.RawData.GetProperty("anything").GetInt32());
        }

        [Fact]
        public void Parse_MalformedJson_Throws()
        {
            Assert.Throws<WebhookParseException>(() => _parser.Parse("{ not json"));
        }

        [Fact]
        public void Parse_MissingRequiredField_Throws()
        {
            const string json = @"{ ""eventType"": ""sms-delivery-report"", ""attempt"": 1, ""eventCreateTs"": ""2026-01-01 00:00:00"" }";
            Assert.Throws<WebhookParseException>(() => _parser.Parse(json));
        }

        [Fact]
        public void TryParse_Malformed_ReturnsFalse()
        {
            Assert.False(_parser.TryParse("nonsense", out var evt));
            Assert.Null(evt);
        }

        [Fact]
        public void TryParse_Valid_ReturnsTrue()
        {
            Assert.True(_parser.TryParse(Payloads.Load(Payloads.SmsDeliveryReport), out var evt));
            Assert.NotNull(evt);
        }
    }
}
