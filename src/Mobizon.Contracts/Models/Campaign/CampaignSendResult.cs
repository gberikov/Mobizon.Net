namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Contains the result data returned after scheduling a campaign for sending.
    /// </summary>
    public class CampaignSendResult
    {
        /// <summary>
        /// Gets or sets the ID of the background task created to process the campaign send operation.
        /// This value is populated when the API response code is <see cref="Models.MobizonResponseCode.BackgroundTask"/>.
        /// </summary>
        public int? TaskId { get; set; }

        /// <summary>
        /// Gets or sets the resulting status code of the campaign after initiating the send.
        /// </summary>
        public int Status { get; set; }
    }
}
