using Mobizon.Contracts.Webhooks;
using Mobizon.Net.Webhooks;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class ForwardCompatibilityTests
    {
        private readonly WebhookParser _parser = new WebhookParser();
        private readonly WebhookSignatureVerifier _verifier = new WebhookSignatureVerifier();

        [Fact]
        public void UnknownEventType_StillVerifiable()
        {
            // SHA1("99|2|2026-02-01 00:00:00|topsecret") = a80090cf6edc623e7f4f68a36aa30de403e7766a
            const string json = @"{
                ""eventId"": 99,
                ""eventType"": ""brand-new-event"",
                ""eventCreateTs"": ""2026-02-01 00:00:00"",
                ""webhookId"": 3,
                ""attempt"": 2,
                ""data"": { ""foo"": ""bar"" },
                ""sign"": ""a80090cf6edc623e7f4f68a36aa30de403e7766a""
            }";

            var evt = _parser.Parse(json);

            Assert.IsType<UnknownWebhookEvent>(evt);
            Assert.Equal(WebhookEventType.Unknown, evt.EventType);
            Assert.True(_verifier.Verify(evt, "topsecret"));
        }

        [Fact]
        public void KnownEvent_WithExtraUnknownFields_ParsesWithoutError()
        {
            const string json = @"{
                ""eventId"": 26,
                ""eventType"": ""sms-delivery-report"",
                ""eventCreateTs"": ""2026-01-15 11:42:28"",
                ""webhookId"": 1,
                ""attempt"": 1,
                ""futureEnvelopeField"": ""ignored"",
                ""data"": {
                    ""campaignId"": 1, ""messageId"": 2, ""segNum"": 1,
                    ""status"": ""DELIVRD"", ""to"": ""123"",
                    ""futureDataField"": { ""nested"": [1, 2, 3] }
                },
                ""sign"": ""x""
            }";

            var evt = Assert.IsType<SmsDeliveryReportEvent>(_parser.Parse(json));
            Assert.Equal(2, evt.Data.MessageId);
        }
    }
}
