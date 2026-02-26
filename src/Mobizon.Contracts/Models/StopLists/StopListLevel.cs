namespace Mobizon.Contracts.Models.StopLists
{
    /// <summary>
    /// Represents the scope level of a number stop-list block.
    /// </summary>
    public enum StopListLevel
    {
        /// <summary>Block applies only to the current user.</summary>
        User = 0,

        /// <summary>Block applies to all users under the same partner.</summary>
        Partner = 1,

        /// <summary>Block applies globally across the entire platform.</summary>
        Global = 2
    }
}
