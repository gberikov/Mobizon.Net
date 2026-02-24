namespace Mobizon.Contracts.Models.Campaign
{
    /// <summary>
    /// Represents the overall (common) status of an SMS campaign as returned by the Mobizon API
    /// in the <c>commonStatus</c>, <c>sendStatus</c>, and <c>moderationStatus</c> fields.
    /// </summary>
    public enum CampaignCommonStatus
    {
        /// <summary>Campaign is awaiting manual moderation.</summary>
        Moderation,

        /// <summary>Campaign was declined during moderation.</summary>
        Declined,

        /// <summary>Campaign has passed moderation and is ready to be sent.</summary>
        ReadyForSend,

        /// <summary>Campaign was automatically approved and is ready to be sent.</summary>
        AutoReadyForSend,

        /// <summary>Campaign is currently being sent.</summary>
        Running,

        /// <summary>All messages in the campaign have been dispatched to the operator.</summary>
        Sent,

        /// <summary>Campaign has completed — all messages processed (delivered or failed).</summary>
        Done
    }
}
