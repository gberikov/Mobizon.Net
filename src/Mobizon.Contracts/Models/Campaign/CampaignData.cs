namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Represents the full data of an SMS campaign as returned by the Mobizon API.
    /// </summary>
    public class CampaignData
    {
        /// <summary>
        /// Gets or sets the unique ID of the campaign.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the campaign type code.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the sender name or number used in the campaign.
        /// </summary>
        public string From { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the body text of the campaign message.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current status code of the campaign.
        /// </summary>
        public int Status { get; set; }
    }
}
