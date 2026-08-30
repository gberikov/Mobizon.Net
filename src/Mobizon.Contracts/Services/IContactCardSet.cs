using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// EF Core-style set for managing contact cards via the Mobizon API.
    /// </summary>
    public interface IContactCardSet
    {
        // ── Query entry points ────────────────────────────────────────────────

        /// <summary>
        /// Begins a filtered query. Each successive <c>Where</c> call on the returned
        /// <see cref="IContactCardQuery"/> is combined with the previous one using a logical AND.
        /// </summary>
        IContactCardQuery Where(Expression<Func<ContactCardFilterSpec, bool>> predicate);

        /// <summary>Begins a query and sets the maximum number of items to return.</summary>
        IContactCardQuery Take(int count);

        /// <summary>
        /// Selects the zero-based page to return. Pair with <see cref="Take"/> to set the page size
        /// (default 25). Unlike LINQ's <c>Skip</c>, this maps 1:1 onto the API's <c>pagination[currentPage]</c>.
        /// </summary>
        IContactCardQuery Page(int pageIndex);

        /// <summary>Begins a query sorted by the specified field ascending.</summary>
        IContactCardQuery OrderBy<TKey>(Expression<Func<ContactCardFilterSpec, TKey>> keySelector);

        /// <summary>Begins a query sorted by the specified field descending.</summary>
        IContactCardQuery OrderByDescending<TKey>(Expression<Func<ContactCardFilterSpec, TKey>> keySelector);

        // ── Single-item access ────────────────────────────────────────────────

        /// <summary>
        /// Returns the full data of a single contact card by ID,
        /// or <see langword="null"/> if not found.
        /// </summary>
        Task<ContactCard?> FindAsync(long id, CancellationToken cancellationToken = default);

        // ── CRUD ──────────────────────────────────────────────────────────────

        /// <summary>Creates a new contact card. Sets <see cref="ContactCard.Id"/> after creation.</summary>
        Task AddAsync(ContactCard entity, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing contact card. <see cref="ContactCard.Id"/> must be set.</summary>
        Task UpdateAsync(ContactCard entity, CancellationToken cancellationToken = default);

        /// <summary>Deletes the contact card with the specified ID.</summary>
        Task RemoveAsync(long id, CancellationToken cancellationToken = default);

        // ── Groups ────────────────────────────────────────────────────────────

        /// <summary>Replaces the group membership of a contact card.</summary>
        Task SetGroupsAsync(long id, IReadOnlyList<long> groupIds, CancellationToken cancellationToken = default);

        /// <summary>Returns the groups the specified contact card belongs to.</summary>
        Task<IReadOnlyList<ContactGroupRef>> GetGroupsAsync(long id, CancellationToken cancellationToken = default);
    }
}
