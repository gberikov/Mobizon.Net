using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Webhooks
{
    /// <summary>Payload of a <c>form-submission</c> webhook.</summary>
    public sealed class FormSubmission
    {
        /// <summary>Identifier of the form.</summary>
        [JsonPropertyName("formId")]
        public long FormId { get; set; }

        /// <summary>Identifier of the submission.</summary>
        [JsonPropertyName("submissionId")]
        public long SubmissionId { get; set; }

        /// <summary>The submitted field values.</summary>
        [JsonPropertyName("items")]
        public IReadOnlyList<WebhookFieldItem> Items { get; set; } = new List<WebhookFieldItem>();
    }
}
