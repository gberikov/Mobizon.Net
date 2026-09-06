using System;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Webhooks
{
    /// <summary>
    /// A single form field value carried by form webhook events.
    /// </summary>
    public sealed class WebhookFieldItem
    {
        /// <summary>The submission this item belongs to. Present on unsubscribe items; <see langword="null"/> otherwise.</summary>
        [JsonPropertyName("submissionId")]
        public long? SubmissionId { get; set; }

        /// <summary>Identifier of this field value.</summary>
        [JsonPropertyName("submissionDataId")]
        public long SubmissionDataId { get; set; }

        /// <summary>Identifier of the form field.</summary>
        [JsonPropertyName("fieldId")]
        public long FieldId { get; set; }

        /// <summary>Raw field type (e.g. <c>TEXT_STRING</c>, <c>EMAIL</c>, <c>MOBILE</c>). Kept as a string for forward compatibility; see <see cref="FieldTypeKind"/> for the typed view.</summary>
        [JsonPropertyName("fieldType")]
        public string FieldType { get; set; } = string.Empty;

        /// <summary>
        /// Typed view of <see cref="FieldType"/>, or <see langword="null"/> when the value is not one of the
        /// documented <see cref="WebhookFieldType"/> values.
        /// </summary>
        [JsonIgnore]
        public WebhookFieldType? FieldTypeKind
        {
            get
            {
                switch (FieldType)
                {
                    case "TEXT_STRING": return WebhookFieldType.TextString;
                    case "EMAIL":       return WebhookFieldType.Email;
                    case "MOBILE":      return WebhookFieldType.Mobile;
                    default:            return null;
                }
            }
        }

        /// <summary>Field name as configured in the form.</summary>
        [JsonPropertyName("fieldName")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>The value entered by the user.</summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>Whether confirmation of this contact was required (<c>1</c> ⇒ <see langword="true"/>).</summary>
        [JsonPropertyName("confirmationRequired")]
        public bool? ConfirmationRequired { get; set; }

        /// <summary>Time the contact was confirmed, if applicable. Empty/absent ⇒ <see langword="null"/>.</summary>
        [JsonPropertyName("confirmationTs")]
        public DateTimeOffset? ConfirmationTs { get; set; }
    }
}
