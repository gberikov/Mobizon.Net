namespace Mobizon.Contracts.Models.Webhooks
{
    /// <summary>
    /// The type of a received Mobizon webhook event, mapped from the raw <c>eventType</c> string.
    /// </summary>
    public enum WebhookEventType
    {
        /// <summary>An <c>eventType</c> not recognised by this SDK version. See <see cref="UnknownWebhookEvent"/>.</summary>
        Unknown = 0,

        /// <summary>Final SMS delivery status (<c>sms-delivery-report</c>).</summary>
        SmsDeliveryReport,

        /// <summary>A form was submitted (<c>form-submission</c>).</summary>
        FormSubmission,

        /// <summary>A form contact (phone/email) was confirmed (<c>form-contact-confirmation</c>).</summary>
        FormContactConfirmation,

        /// <summary>A contact unsubscribed via a form (<c>form-contact-unsubscribe</c>).</summary>
        FormContactUnsubscribe
    }
}
