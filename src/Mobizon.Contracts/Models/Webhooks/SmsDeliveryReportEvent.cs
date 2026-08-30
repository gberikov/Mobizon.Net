namespace Mobizon.Contracts.Webhooks
{
    /// <summary>An <c>sms-delivery-report</c> webhook event.</summary>
    public sealed class SmsDeliveryReportEvent : MobizonWebhookEvent
    {
        /// <summary>The delivery-report payload.</summary>
        public SmsDeliveryReport Data { get; set; } = new SmsDeliveryReport();
    }
}
