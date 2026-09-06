using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// A composable, mockable query builder for <c>contactcard/list</c>.
    /// Every builder method returns a new query and leaves the receiver untouched.
    /// </summary>
    public interface IContactCardQuery
    {
        /// <summary>
        /// Adds a filter predicate. Each successive call is combined with the previous one
        /// using a logical AND, so <c>.Where(a).Where(b)</c> filters by <c>a AND b</c>.
        /// </summary>
        IContactCardQuery Where(Expression<Func<ContactCardFilterSpec, bool>> predicate);

        /// <summary>Sets the page size (items per request). Must be positive; default 25.</summary>
        IContactCardQuery Take(int count);

        /// <summary>
        /// Selects the zero-based page to return. Pair with <see cref="Take"/> to set the page size
        /// (default 25). Unlike LINQ's <c>Skip</c>, this maps 1:1 onto the API's <c>pagination[currentPage]</c>.
        /// </summary>
        IContactCardQuery Page(int pageIndex);

        /// <summary>Sorts results by the specified field ascending.</summary>
        IContactCardQuery OrderBy<TKey>(Expression<Func<ContactCardFilterSpec, TKey>> keySelector);

        /// <summary>Sorts results by the specified field descending.</summary>
        IContactCardQuery OrderByDescending<TKey>(Expression<Func<ContactCardFilterSpec, TKey>> keySelector);

        /// <summary>Executes the query and returns all matching items on the current page.</summary>
        Task<IReadOnlyList<ContactCard>> ToListAsync(CancellationToken ct = default);

        /// <summary>Executes the query and returns the page with pagination metadata.</summary>
        Task<PaginatedResponse<ContactCard>> ToPageAsync(CancellationToken ct = default);

        /// <summary>
        /// Returns the total number of items matching the filter across all pages. <see cref="Page"/> and
        /// <see cref="Take"/> do not affect the result; a single-item request is made because the API has no
        /// count-only call.
        /// </summary>
        Task<int> CountAsync(CancellationToken ct = default);

        /// <summary>
        /// Returns the first item of the selected window, or <see langword="null"/> if it is empty. With an explicit
        /// <see cref="Page"/> the configured page (of <see cref="Take"/> items) is fetched and its head returned;
        /// without one, the first item of the whole query is fetched on its own.
        /// </summary>
        Task<ContactCard?> FirstOrDefaultAsync(CancellationToken ct = default);

        /// <summary>Same window rules as <see cref="FirstOrDefaultAsync"/>, throwing when it is empty.</summary>
        /// <exception cref="InvalidOperationException">No elements found.</exception>
        Task<ContactCard> FirstAsync(CancellationToken ct = default);

        /// <summary>
        /// Returns the only item matching the filter across the whole query, or <see langword="null"/> if none.
        /// <see cref="Page"/> and <see cref="Take"/> are ignored: uniqueness is checked against the server-side
        /// total, not one page.
        /// </summary>
        /// <exception cref="InvalidOperationException">More than one element matches.</exception>
        Task<ContactCard?> SingleOrDefaultAsync(CancellationToken ct = default);

        /// <summary>Same rules as <see cref="SingleOrDefaultAsync"/>, throwing when nothing matches.</summary>
        /// <exception cref="InvalidOperationException">No elements or more than one element found.</exception>
        Task<ContactCard> SingleAsync(CancellationToken ct = default);
    }
}
