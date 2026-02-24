using System;
using System.Text.Json.Serialization;
using Mobizon.Contracts.Models.Campaign;

namespace Mobizon.Contracts.Models.Message
{
    /// <summary>
    /// Represents summary information about an SMS message returned by a list query.
    /// </summary>
    public class MessageInfo
    {
        /// <summary>
        /// Gets or sets the unique ID of the message.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the campaign this message belongs to.
        /// </summary>
        public int CampaignId { get; set; }

        /// <summary>
        /// Gets or sets the number of SMS segments the message was split into.
        /// </summary>
        [JsonPropertyName("segNum")]
        public int Segments { get; set; }

        /// <summary>
        /// Gets or sets the segment cost in user currency.
        /// </summary>
        public float SegUserBuy { get; set; }

        /// <summary>
        /// Gets or sets the sender name or number displayed to the recipient.
        /// </summary>
        public string From { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recipient phone number.
        /// </summary>
        public string To { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the body text of the SMS message.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current delivery status of the message.
        /// </summary>
        public SmsStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the contact group IDs associated with this message.
        /// </summary>
        public string? Groups { get; set; }

        /// <summary>
        /// Gets or sets the internal message identifier (UUID).
        /// </summary>
        public string? Uuid { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the message was first submitted for sending.
        /// </summary>
        [JsonPropertyName("startSendTs")]
        public DateTime? SendStarted { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last delivery status update.
        /// </summary>
        [JsonPropertyName("statusUpdateTs")]
        public DateTime? StatusUpdated { get; set; }

        /// <summary>
        /// Gets or sets the ISO-3166 alpha-2 country code of the recipient.
        /// Only populated when <c>withNumberInfo</c> is set to 1.
        /// </summary>
        public string? CountryA2 { get; set; }

        /// <summary>
        /// Gets or sets the mobile operator name of the recipient.
        /// Only populated when <c>withNumberInfo</c> is set to 1.
        /// </summary>
        public string? OperatorName { get; set; }

        /// <summary>
        /// Gets or sets the campaign this message belongs to.
        /// Populated when the API returns the nested campaign object.
        /// </summary>
        public CampaignData? Campaign { get; set; }
    }
}
