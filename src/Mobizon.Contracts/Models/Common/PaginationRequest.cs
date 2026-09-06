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
        /// Items per page. The documented list endpoints accept 25, 50 or 100 and default to 25; other values
        /// are not documented and may be rejected with response code 12.
        /// </summary>
        public int PageSize { get; set; } = 25;
    }
}
