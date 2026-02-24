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
        /// Gets or sets the sender name or number displayed to the recipient.
        /// </summary>
        public string From { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current delivery status code of the message.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the body text of the SMS message.
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }
}
