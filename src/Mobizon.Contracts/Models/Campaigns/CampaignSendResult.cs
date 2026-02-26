namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Contains the result data returned after scheduling a campaign for sending.
    /// </summary>
    public class CampaignSendResult
    {
        /// <summary>
        /// Gets or sets the resulting status code of the campaign after initiating the send.
        /// </summary>
        public int Status { get; set; }
    }
}
