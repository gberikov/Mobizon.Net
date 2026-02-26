using System.Collections.Generic;

namespace Mobizon.Contracts.Models.StopLists
{
    /// <summary>
    /// Represents the paginated response returned by <c>numberstoplist/list</c>.
    /// </summary>
    public class StopListListResponse
    {
        /// <summary>Gets or sets the stop-list entries on the current page.</summary>
        public IReadOnlyList<StopListEntry> Items { get; set; } = new List<StopListEntry>();

        /// <summary>Gets or sets the total number of entries across all pages.</summary>
        public int TotalItemCount { get; set; }
    }
}
