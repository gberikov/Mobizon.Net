using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Specifies optional pagination and sort criteria for listing short links.
    /// </summary>
    public class LinkListRequest
    {
        /// <summary>
        /// Gets or sets optional filter criteria. When <see langword="null"/>, no filtering is applied.
        /// </summary>
        public LinkListCriteria? Criteria { get; set; }

        /// <summary>
        /// Gets or sets optional pagination settings. When <see langword="null"/>, the API default page size is used.
        /// </summary>
        public PaginationRequest? Pagination { get; set; }

        /// <summary>
        /// Gets or sets optional sort settings. When <see langword="null"/>, the API default sort order is used.
        /// </summary>
        public SortRequest? Sort { get; set; }
    }
}
