namespace Mobizon.Contracts.Webhooks
{
    /// <summary>A <c>form-contact-unsubscribe</c> webhook event.</summary>
    public sealed class FormContactUnsubscribeEvent : MobizonWebhookEvent
    {
        /// <summary>The unsubscribe payload.</summary>
        public FormContactUnsubscribe Data { get; set; } = new FormContactUnsubscribe();
    }
}
