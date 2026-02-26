namespace Mobizon.Contracts.Models.Messages
{
    /// <summary>
    /// Represents the campaign dispatch status returned after an SMS message is submitted.
    /// </summary>
    public enum CampaignStatus
    {
        /// <summary>
        /// The campaign is awaiting moderation before it is sent.
        /// </summary>
        PendingModeration = 1,

        /// <summary>
        /// The campaign was dispatched immediately without moderation.
        /// </summary>
        SentWithoutModeration = 2
    }
}

