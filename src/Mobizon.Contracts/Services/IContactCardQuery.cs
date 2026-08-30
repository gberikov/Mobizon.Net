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

        /// <summary>Returns the total number of matching items without fetching their data.</summary>
        Task<int> CountAsync(CancellationToken ct = default);

        /// <summary>Returns the first matching item, or <see langword="null"/> if none found.</summary>
        Task<ContactCard?> FirstOrDefaultAsync(CancellationToken ct = default);

        /// <summary>Returns the first matching item.</summary>
        /// <exception cref="InvalidOperationException">No elements found.</exception>
        Task<ContactCard> FirstAsync(CancellationToken ct = default);

        /// <summary>Returns the only matching item, or <see langword="null"/> if none found.</summary>
        /// <exception cref="InvalidOperationException">More than one element found.</exception>
        Task<ContactCard?> SingleOrDefaultAsync(CancellationToken ct = default);

        /// <summary>Returns the only matching item.</summary>
        /// <exception cref="InvalidOperationException">No elements or more than one element found.</exception>
        Task<ContactCard> SingleAsync(CancellationToken ct = default);
    }
}
