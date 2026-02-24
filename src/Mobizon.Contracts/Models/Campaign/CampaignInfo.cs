namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Represents delivery statistics for an SMS campaign.
    /// </summary>
    public class CampaignInfo
    {
        /// <summary>
        /// Gets or sets the unique ID of the campaign.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages in the campaign.
        /// </summary>
        public int TotalMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of messages that have been sent.
        /// </summary>
        public int Sent { get; set; }

        /// <summary>
        /// Gets or sets the number of messages that were successfully delivered to recipients.
        /// </summary>
        public int Delivered { get; set; }

        /// <summary>
        /// Gets or sets the number of messages that failed to deliver.
        /// </summary>
        public int Failed { get; set; }
    }
}
