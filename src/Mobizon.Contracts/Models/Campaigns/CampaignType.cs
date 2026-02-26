namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Defines the type of an SMS campaign.
    /// </summary>
    public enum CampaignType
    {
        /// <summary>
        /// A single message sent to one recipient (<c>data[type]=1</c>).
        /// </summary>
        Single = 1,

        /// <summary>
        /// A bulk broadcast sent to multiple recipients (<c>data[type]=2</c>).
        /// This is the default campaign type.
        /// </summary>
        Bulk = 2,

        /// <summary>
        /// A template campaign where the message text may contain placeholders
        /// that are replaced with recipient-specific values (<c>data[type]=3</c>).
        /// </summary>
        Template = 3
    }
}

