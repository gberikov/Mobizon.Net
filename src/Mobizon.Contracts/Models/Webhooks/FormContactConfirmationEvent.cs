namespace Mobizon.Contracts.Models.Webhooks
{
    /// <summary>A <c>form-contact-confirmation</c> webhook event.</summary>
    public sealed class FormContactConfirmationEvent : MobizonWebhookEvent
    {
        /// <summary>The confirmation payload.</summary>
        public FormContactConfirmation Data { get; set; } = new FormContactConfirmation();
    }
}
