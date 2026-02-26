namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Contains the result data returned after successfully creating a new campaign.
    /// </summary>
    public class CreateCampaignResult
    {
        /// <summary>
        /// Gets or sets the unique ID of the newly created campaign.
        /// </summary>
        public int CampaignId { get; set; }
    }
}
