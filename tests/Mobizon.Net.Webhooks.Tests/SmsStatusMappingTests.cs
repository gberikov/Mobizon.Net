using Mobizon.Contracts;
using Mobizon.Contracts.Webhooks;
using Mobizon.Net.Webhooks;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class SmsStatusMappingTests
    {
        private readonly WebhookParser _parser = new WebhookParser();

        private static string ReportWithStatus(string status) => $@"{{
            ""eventId"": 1,
            ""eventType"": ""sms-delivery-report"",
            ""eventCreateTs"": ""2026-01-15 11:42:28"",
            ""webhookId"": 1,
            ""attempt"": 1,
            ""data"": {{ ""campaignId"": 1, ""messageId"": 2, ""segNum"": 1, ""status"": ""{status}"", ""to"": ""123"" }},
            ""sign"": ""x""
        }}";

        [Fact]
        public void Known_DELIVRD_MapsToDelivered()
        {
            var evt = Assert.IsType<SmsDeliveryReportEvent>(_parser.Parse(ReportWithStatus("DELIVRD")));
            Assert.Equal(SmsStatus.Delivered, evt.Data.Status);
            Assert.Equal("DELIVRD", evt.Data.StatusRaw);
        }

        [Fact]
        public void Unknown_Status_NullEnum_RawPreserved_NoThrow()
        {
            var evt = Assert.IsType<SmsDeliveryReportEvent>(_parser.Parse(ReportWithStatus("FUTURE_STATUS")));
            Assert.Null(evt.Data.Status);
            Assert.Equal("FUTURE_STATUS", evt.Data.StatusRaw);
        }
    }
}
