using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Webhooks
{
    /// <summary>Payload of a <c>form-contact-confirmation</c> webhook.</summary>
    public sealed class FormContactConfirmation
    {
        /// <summary>Identifier of the form.</summary>
        [JsonPropertyName("formId")]
        public long FormId { get; set; }

        /// <summary>Identifier of the submission.</summary>
        [JsonPropertyName("submissionId")]
        public long SubmissionId { get; set; }

        /// <summary>The confirmed field, including its confirmation timestamp.</summary>
        [JsonPropertyName("item")]
        public WebhookFieldItem Item { get; set; } = new WebhookFieldItem();
    }
}
