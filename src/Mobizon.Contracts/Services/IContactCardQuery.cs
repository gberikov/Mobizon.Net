using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.ContactCards;

namespace Mobizon.Contracts.Services
{
    /// <summary>
    /// A composable, mockable query builder for <c>contactcard/list</c>.
    /// </summary>
    public interface IContactCardQuery
    {
        /// <summary>Adds a filter predicate.</summary>
        IContactCardQuery Where(Expression<Func<ContactCardFilterSpec, bool>> predicate);

        /// <summary>Sets the maximum number of items to return (page size).</summary>
        IContactCardQuery Take(int count);

        /// <summary>Skips the first <paramref name="count"/> items.</summary>
        IContactCardQuery Skip(int count);

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
