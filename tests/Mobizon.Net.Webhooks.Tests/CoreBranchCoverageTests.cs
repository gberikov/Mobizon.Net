using System;
using System.Text.Json;
using Mobizon.Contracts.Models.Messages;
using Mobizon.Contracts.Models.Webhooks;
using Mobizon.Net.Webhooks;
using Mobizon.Net.Webhooks.Internal.Converters;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class CoreBranchCoverageTests
    {
        private readonly WebhookParser _parser = new WebhookParser();

        private static JsonSerializerOptions ConverterOptions()
        {
            var o = new JsonSerializerOptions();
            o.Converters.Add(new WebhookDateTimeOffsetConverter());
            o.Converters.Add(new FlexibleBoolConverter());
            o.Converters.Add(new SmsDeliveryReportConverter());
            return o;
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("1", true)]
        [InlineData("false", false)]
        [InlineData("0", false)]
        public void FlexibleBool_StringVariants(string raw, bool expected)
        {
            var json = $@"{{ ""confirmationRequired"": ""{raw}"" }}";
            var item = JsonSerializer.Deserialize<WebhookFieldItem>(json, ConverterOptions());
            Assert.Equal(expected, item!.ConfirmationRequired);
        }

        [Fact]
        public void FlexibleBool_BoolAndUnknownString()
        {
            var opts = ConverterOptions();
            Assert.True(JsonSerializer.Deserialize<WebhookFieldItem>(@"{ ""confirmationRequired"": true }", opts)!.ConfirmationRequired);
            Assert.False(JsonSerializer.Deserialize<WebhookFieldItem>(@"{ ""confirmationRequired"": false }", opts)!.ConfirmationRequired);
            Assert.Null(JsonSerializer.Deserialize<WebhookFieldItem>(@"{ ""confirmationRequired"": ""maybe"" }", opts)!.ConfirmationRequired);
            Assert.Null(JsonSerializer.Deserialize<WebhookFieldItem>(@"{ ""confirmationRequired"": null }", opts)!.ConfirmationRequired);
        }

        [Fact]
        public void Converters_Write_RoundTrips()
        {
            var opts = ConverterOptions();

            var item = new WebhookFieldItem
            {
                ConfirmationRequired = true,
                ConfirmationTs = new DateTimeOffset(2026, 1, 15, 16, 54, 14, TimeSpan.Zero),
                FieldName = "Celular",
                Value = "x"
            };
            var json = JsonSerializer.Serialize(item, opts);
            var back = JsonSerializer.Deserialize<WebhookFieldItem>(json, opts);
            Assert.True(back!.ConfirmationRequired);
            Assert.Equal(item.ConfirmationTs, back.ConfirmationTs);

            // null branches in Write
            var empty = JsonSerializer.Serialize(new WebhookFieldItem(), opts);
            Assert.Contains("confirmationRequired", empty);
        }

        [Fact]
        public void SmsDeliveryReportConverter_Write_EmitsFields()
        {
            var opts = ConverterOptions();
            var report = new SmsDeliveryReport
            {
                CampaignId = 1,
                MessageId = 2,
                SegNum = 1,
                Status = SmsStatus.Delivered,
                StatusRaw = "DELIVRD",
                StatusUpdateTs = new DateTimeOffset(2026, 1, 15, 11, 42, 8, TimeSpan.Zero),
                To = "123"
            };
            var json = JsonSerializer.Serialize(report, opts);
            Assert.Contains("\"messageId\":2", json);
            Assert.Contains("DELIVRD", json);
        }

        [Fact]
        public void Parse_NumericFieldsAsStrings_Accepted()
        {
            const string json = @"{
                ""eventId"": ""26"",
                ""eventType"": ""sms-delivery-report"",
                ""eventCreateTs"": ""2026-01-15 11:42:28"",
                ""webhookId"": ""1"",
                ""attempt"": ""1"",
                ""data"": { ""campaignId"": ""5"", ""messageId"": ""6"", ""segNum"": ""2"", ""status"": ""DELIVRD"", ""to"": ""1"" },
                ""sign"": ""x""
            }";
            var evt = Assert.IsType<SmsDeliveryReportEvent>(_parser.Parse(json));
            Assert.Equal(26, evt.EventId);
            Assert.Equal(1, evt.WebhookId);
            Assert.Equal(5, evt.Data.CampaignId);
            Assert.Equal(6, evt.Data.MessageId);
            Assert.Equal(2, evt.Data.SegNum);
        }

        [Fact]
        public void Parse_OptionalFieldsMissing_DefaultedNotThrown()
        {
            // no webhookId, no sign
            const string json = @"{
                ""eventId"": 1,
                ""eventType"": ""sms-delivery-report"",
                ""eventCreateTs"": ""2026-01-15 11:42:28"",
                ""attempt"": 1,
                ""data"": { ""campaignId"": 1, ""messageId"": 2, ""segNum"": 1, ""status"": ""DELIVRD"", ""to"": ""1"" }
            }";
            var evt = _parser.Parse(json);
            Assert.Equal(0, evt.WebhookId);
            Assert.Equal(string.Empty, evt.Sign);
        }

        [Fact]
        public void Parse_NonObjectBody_Throws()
        {
            Assert.Throws<Mobizon.Contracts.Exceptions.WebhookParseException>(() => _parser.Parse("[1,2,3]"));
        }

        [Fact]
        public void Parse_EmptyBody_Throws()
        {
            Assert.Throws<Mobizon.Contracts.Exceptions.WebhookParseException>(() => _parser.Parse("   "));
        }

        [Fact]
        public void Parse_NonStringEventType_Throws()
        {
            Assert.Throws<Mobizon.Contracts.Exceptions.WebhookParseException>(
                () => _parser.Parse(@"{ ""eventType"": 5, ""eventId"": 1, ""attempt"": 1, ""eventCreateTs"": ""2026-01-15 11:42:28"" }"));
        }

        [Fact]
        public void DateTimeConverter_UnparseableTimestamp_BecomesNull()
        {
            Assert.Null(WebhookDateTimeOffsetConverter.ParseOrNull("not-a-date"));
        }

        [Fact]
        public void Verify_DifferentLengthSignature_ReturnsFalse()
        {
            var verifier = new WebhookSignatureVerifier();
            Assert.False(verifier.Verify(99, 2, "2026-02-01 00:00:00", "short", "topsecret"));
        }
    }
}
