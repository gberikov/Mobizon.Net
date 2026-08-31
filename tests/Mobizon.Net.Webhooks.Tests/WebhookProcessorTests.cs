using Mobizon.Contracts.Webhooks;
using Mobizon.Net.Webhooks;
using Xunit;

namespace Mobizon.Net.Webhooks.Tests
{
    public class WebhookProcessorTests
    {
        private readonly WebhookProcessor _processor = new WebhookProcessor();

        [Fact]
        public void Process_ValidAndSigned_ReturnsOk()
        {
            var result = _processor.Process(Payloads.Load(Payloads.SmsDeliveryReport), Payloads.Secret);

            Assert.Equal(WebhookProcessStatus.Ok, result.Status);
            Assert.True(result.IsAuthentic);
            Assert.NotNull(result.Event);
        }

        [Fact]
        public void Process_WrongSecret_ReturnsSignatureMismatch_WithEvent()
        {
            var result = _processor.Process(Payloads.Load(Payloads.SmsDeliveryReport), "wrong-secret");

            Assert.Equal(WebhookProcessStatus.SignatureMismatch, result.Status);
            Assert.False(result.IsAuthentic);
            Assert.NotNull(result.Event); // populated even on mismatch
        }

        [Fact]
        public void Process_Malformed_ReturnsParseError_WithNullEvent()
        {
            var result = _processor.Process("{ broken", Payloads.Secret);

            Assert.Equal(WebhookProcessStatus.ParseError, result.Status);
            Assert.Null(result.Event);
        }

        [Fact]
        public void Process_SecretSelector_PicksByWebhookId()
        {
            var result = _processor.Process(
                Payloads.Load(Payloads.SmsDeliveryReport),
                evt => evt.WebhookId == 1 ? Payloads.Secret : "wrong");

            Assert.Equal(WebhookProcessStatus.Ok, result.Status);
        }
    }
}
