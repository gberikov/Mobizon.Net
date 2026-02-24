namespace Mobizon.Contracts.Models.Message
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
        /// Gets or sets the initial delivery status code of the message as returned by the API.
        /// </summary>
        public int Status { get; set; }
    }
}
