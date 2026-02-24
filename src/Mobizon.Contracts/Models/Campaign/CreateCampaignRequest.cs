namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Represents the parameters required to create a new SMS campaign.
    /// </summary>
    public class CreateCampaignRequest
    {
        /// <summary>
        /// Gets or sets the campaign type code as defined by the Mobizon API.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the sender name or number displayed to recipients.
        /// </summary>
        public string From { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the body text of the campaign message.
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }
}
