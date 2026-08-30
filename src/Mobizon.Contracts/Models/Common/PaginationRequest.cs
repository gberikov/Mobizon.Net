using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Specifies pagination parameters for list API requests.
    /// </summary>
    public class PaginationRequest
    {
        /// <summary>
        /// Gets or sets the one-based page number to retrieve.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Items per page. API default is 25; maximum 100.
        /// </summary>
        public int PageSize { get; set; } = 25;
    }
}
