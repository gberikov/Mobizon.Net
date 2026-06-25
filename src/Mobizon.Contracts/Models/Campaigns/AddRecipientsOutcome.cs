namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Top-level outcome of <c>campaign/addRecipients</c>, derived from the API response code
    /// (0 = all added, 98 = partially added, 99 = none added).
    /// </summary>
    public enum AddRecipientsOutcome
    {
        /// <summary>All recipients were added (response code 0), or the load was accepted as a background task.</summary>
        AllAdded = 0,

        /// <summary>Some recipients were added; others were rejected (response code 98).</summary>
        PartiallyAdded = 98,

        /// <summary>No recipients were added; all entries were rejected (response code 99).</summary>
        NoneAdded = 99
    }
}
