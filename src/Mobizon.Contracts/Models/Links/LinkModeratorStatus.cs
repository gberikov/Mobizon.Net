namespace Mobizon.Contracts
{
    /// <summary>Administrator moderation status of a short link.</summary>
    public enum LinkModeratorStatus
    {
        /// <summary>Blocked by the administrator (API value 0).</summary>
        Blocked = 0,

        /// <summary>Approved by the administrator (API value 1).</summary>
        Approved = 1
    }
}
