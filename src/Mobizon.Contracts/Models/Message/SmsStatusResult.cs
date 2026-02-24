using System;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.Message
{
    /// <summary>
    /// Represents the delivery status of a single SMS message returned by a status query.
    /// </summary>
    public class SmsStatusResult
    {
        /// <summary>
        /// Gets or sets the unique message ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the current delivery status of the message.
        /// </summary>
        public SmsStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the number of SMS segments the message was split into.
        /// </summary>
        [JsonPropertyName("segNum")]
        public int Segments { get; set; }

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
    }
}
