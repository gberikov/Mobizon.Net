namespace Mobizon.Contracts
{
    /// <summary>
    /// Top-level outcome of <c>campaign/addRecipients</c>, derived from the API response code
    /// (0 = all added, 98 = partially added, 99 = none added). Numeric values match the API codes.
    /// </summary>
    public enum AddRecipientsOutcome
    {
        /// <summary>
        /// Every synchronous batch was accepted (response code 0), <b>or</b> the load was accepted as a background
        /// task (code 100) — in that case <see cref="AddRecipientsResult.IsQueued"/> is <see langword="true"/> and
        /// the recipients are not added until the task completes.
        /// </summary>
        AllAdded = 0,

        /// <summary>
        /// Some recipients were added and others rejected: response code 98 for a single batch, or a mix of
        /// accepted and rejected batches in a multi-batch send.
        /// </summary>
        PartiallyAdded = 98,

        /// <summary>No recipients were added; every batch was rejected (response code 99).</summary>
        NoneAdded = 99
    }
}
