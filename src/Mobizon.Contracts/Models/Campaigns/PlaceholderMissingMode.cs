namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Behaviour when a template campaign is missing values for some placeholders
    /// (the <c>params[placeholdersFlag]</c> AddRecipients option).
    /// </summary>
    public enum PlaceholderMissingMode
    {
        /// <summary>Keep the placeholders in the text unchanged (API value 1, default).</summary>
        KeepAsIs = 1,

        /// <summary>Remove the unfilled placeholders from the text (API value 2).</summary>
        Remove = 2,

        /// <summary>Reject the message with an error (API value 3).</summary>
        Reject = 3
    }
}
