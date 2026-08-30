using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Specifies pagination parameters for list API requests.
    /// </summary>
    public class PaginationRequest
    {
        /// <summary>
        /// Gets or sets the zero-based page number to retrieve. <c>0</c> is the first page,
        /// matching the Mobizon API's own page numbering.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Items per page. API default is 25; maximum 100.
        /// </summary>
        public int PageSize { get; set; } = 25;
    }
}
