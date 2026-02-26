namespace Mobizon.Contracts.Models.Messages
{
    /// <summary>
    /// Contains the result data returned after successfully submitting an SMS message.
    /// </summary>
    public class SendSmsResult
    {
        /// <summary>
        /// Gets or sets the ID of the campaign that was automatically created or used for this message.
        /// </summary>
        public int CampaignId { get; set; }

        /// <summary>
        /// Gets or sets the unique ID of the submitted message, which can be used to query delivery status.
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// Gets or sets the dispatch status of the SMS campaign.
        /// <list type="bullet">
        /// <item><description><see cref="CampaignStatus.PendingModeration"/> – the campaign is awaiting moderation.</description></item>
        /// <item><description><see cref="CampaignStatus.SentWithoutModeration"/> – the campaign was sent without moderation.</description></item>
        /// </list>
        /// </summary>
        public CampaignStatus Status { get; set; }
    }
}
