using Mobizon.Contracts.Models.Messages;
using Mobizon.Contracts.Models.Webhooks;
using Mobizon.Net.Webhooks;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class SmsDeliveryReportParseTests
    {
        private readonly WebhookParser _parser = new WebhookParser();

        [Fact]
        public void Parse_SmsDeliveryReport_AllFields()
        {
            var evt = Assert.IsType<SmsDeliveryReportEvent>(_parser.Parse(Payloads.Load(Payloads.SmsDeliveryReport)));
            var data = evt.Data;

            Assert.Equal(245455096, data.CampaignId);
            Assert.Equal(169275418, data.MessageId);
            Assert.Equal(3, data.Segments);
            Assert.NotNull(data.StatusUpdateTs);
            Assert.Equal(SmsStatus.Delivered, data.Status);
            Assert.Equal("DELIVRD", data.StatusRaw);
            Assert.Equal("380737893456", data.To);
        }
    }
}
