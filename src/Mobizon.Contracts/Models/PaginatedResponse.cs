using System.Collections.Generic;

namespace Mobizon.Contracts.Models
{
    /// <summary>
    /// Wraps a page of results returned by a paginated Mobizon API list endpoint.
    /// </summary>
    /// <typeparam name="T">The type of the individual items in the page.</typeparam>
    public class PaginatedResponse<T>
    {
        /// <summary>
        /// Gets or sets the items on the current page.
        /// </summary>
        public IReadOnlyList<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Gets or sets the total number of items across all pages.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the one-based index of the current page.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items per page.
        /// </summary>
        public int PageSize { get; set; }
    }
}
