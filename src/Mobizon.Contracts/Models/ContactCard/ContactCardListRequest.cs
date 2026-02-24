using System.Collections.Generic;

namespace Mobizon.Contracts.Models.ContactCard
{
    /// <summary>
    /// Request parameters for <c>contactcard/list</c>.
    /// </summary>
    public class ContactCardListRequest
    {
        /// <summary>Gets or sets filter criteria applied to the contact list.</summary>
        public IReadOnlyList<ContactCardCriteria>? Criteria { get; set; }

        /// <summary>Gets or sets pagination parameters.</summary>
        public PaginationRequest? Pagination { get; set; }

        /// <summary>Gets or sets sort parameters.</summary>
        public SortRequest? Sort { get; set; }
    }
}
