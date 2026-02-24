namespace Mobizon.Contracts.Models.ContactGroup
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
