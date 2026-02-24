using System.Text.RegularExpressions;

namespace Mobizon.Contracts.Models.Message
{
    /// <summary>
    /// Represents the parameters required to send a single SMS message via the Mobizon API.
    /// </summary>
    public class SendSmsMessageRequest
    {
        private string _recipient = string.Empty;

        /// <summary>
        /// Gets or sets the recipient phone number in international format (e.g. <c>79991234567</c>).
        /// Only digits are retained; any non-digit characters (including a leading <c>+</c>) are stripped automatically.
        /// </summary>
        public string Recipient
        {
            get => _recipient;
            set => _recipient = Regex.Replace(value ?? string.Empty, @"\D", string.Empty);
        }

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
        /// Gets or sets optional additional parameters (<c>params[…]</c>) such as campaign name,
        /// scheduled send time, message class, and validity period.
        /// When <see langword="null"/>, platform defaults are used.
        /// </summary>
        public SmsMessageParameters? Parameters { get; set; }
    }
}


