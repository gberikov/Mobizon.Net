namespace Mobizon.Contracts.Models.Message
{
    /// <summary>
    /// Represents the parameters required to send a single SMS message via the Mobizon API.
    /// </summary>
    public class SendSmsMessageRequest
    {
        /// <summary>
        /// Gets or sets the recipient phone number in international format (e.g. <c>79991234567</c>).
        /// </summary>
        public string Recipient { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the body text of the SMS message.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender name or number displayed to the recipient.
        /// When <see langword="null"/>, the account's default alphanumeric name is used.
        /// </summary>
        public string? From { get; set; }

        /// <summary>
        /// Gets or sets the message validity period in minutes.
        /// When <see langword="null"/>, the platform default is used.
        /// </summary>
        public int? Validity { get; set; }
    }
}
