using Mobizon.Contracts.Models.Common;

namespace Mobizon.Contracts.Models.ContactGroups
{
    /// <summary>
    /// Request parameters for <c>contactgroup/list</c>.
    /// </summary>
    public class ContactGroupListRequest
    {
        /// <summary>Gets or sets pagination parameters.</summary>
        public PaginationRequest? Pagination { get; set; }

        /// <summary>Gets or sets sort parameters.</summary>
        public SortRequest? Sort { get; set; }
    }
}
