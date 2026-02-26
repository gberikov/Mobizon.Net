using Mobizon.Contracts.Models.Common;

namespace Mobizon.Contracts.Models.Common
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
        /// Gets or sets the number of items to return per page. Defaults to <c>20</c>.
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}
