namespace Mobizon.Contracts.Webhooks
{
    /// <summary>A <c>form-submission</c> webhook event.</summary>
    public sealed class FormSubmissionEvent : MobizonWebhookEvent
    {
        /// <summary>The submission payload.</summary>
        public FormSubmission Data { get; set; } = new FormSubmission();
    }
}
