using Mobizon.Contracts.Models.Common;

namespace Mobizon.Contracts.Models.Campaigns
{
    /// <summary>
    /// Represents the top-level response code returned by the <c>campaign/addRecipients</c> endpoint.
    /// Codes 98 (<see cref="MobizonResponseCode.BulkPartialSuccess"/>) and
    /// 99 (<see cref="MobizonResponseCode.BulkCompleteFailure"/>) are treated as non-errors for this
    /// endpoint — they describe how many recipients were processed, not a request failure.
    /// </summary>
    public enum AddRecipientsResponseCode
    {
        /// <summary>All recipients were successfully added to the campaign.</summary>
        AllAdded = 0,

        /// <summary>Some recipients were added, but others failed validation or were rejected.</summary>
        PartiallyAdded = 98,

        /// <summary>No recipients were added; all entries failed validation or were rejected.</summary>
        NoneAdded = 99
    }
}
