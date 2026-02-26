using Mobizon.Contracts.Models.Common;

namespace Mobizon.Contracts.Models.StopLists
{
    /// <summary>
    /// Request parameters for <c>numberstoplist/list</c>.
    /// </summary>
    public class StopListListRequest
    {
        /// <summary>Gets or sets pagination parameters.</summary>
        public PaginationRequest? Pagination { get; set; }

        /// <summary>Gets or sets sort parameters.</summary>
        public SortRequest? Sort { get; set; }
    }
}
