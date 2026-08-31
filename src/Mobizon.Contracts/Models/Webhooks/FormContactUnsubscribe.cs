using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Webhooks
{
    /// <summary>Payload of a <c>form-contact-unsubscribe</c> webhook.</summary>
    public sealed class FormContactUnsubscribe
    {
        /// <summary>Identifier of the form.</summary>
        [JsonPropertyName("formId")]
        public long FormId { get; set; }

        /// <summary>Time of the unsubscribe action, parsed for convenience.</summary>
        [JsonPropertyName("unsubscribeTs")]
        public DateTimeOffset? UnsubscribeTs { get; set; }

        /// <summary>The affected field values.</summary>
        [JsonPropertyName("items")]
        public IReadOnlyList<WebhookFieldItem> Items { get; set; } = new List<WebhookFieldItem>();
    }
}
