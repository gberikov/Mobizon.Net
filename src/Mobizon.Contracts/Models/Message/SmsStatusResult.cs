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
        /// Gets or sets the current delivery status code of the message.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the number of SMS segments the message was split into.
        /// </summary>
        public int SegNum { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the message was first submitted for sending,
        /// as a string in the format returned by the API (YYYY-MM-DD HH:MM:SS or null if not sent).
        /// </summary>
        public string? StartSendTs { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last delivery status update,
        /// as a string in the format returned by the API (YYYY-MM-DD HH:MM:SS or null if not sent).
        /// </summary>
        public string? StatusUpdateTs { get; set; }
    }
}
