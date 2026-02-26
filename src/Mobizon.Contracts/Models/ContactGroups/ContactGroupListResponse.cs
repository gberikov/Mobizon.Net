using System.Collections.Generic;

namespace Mobizon.Contracts.Models.ContactGroups
{
    /// <summary>
    /// Represents the paginated response returned by <c>contactgroup/list</c>.
    /// </summary>
    public class ContactGroupListResponse
    {
        /// <summary>Gets or sets the groups on the current page.</summary>
        public IReadOnlyList<ContactGroupData> Items { get; set; } = new List<ContactGroupData>();

        /// <summary>Gets or sets the total number of groups across all pages.</summary>
        public int TotalItemCount { get; set; }
    }
}
