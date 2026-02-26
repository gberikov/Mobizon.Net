using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.ContactCards;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.ContactCards
{
    /// <summary>
    /// Provides EF Core-style CRUD and query operations for contact cards.
    /// Accessible via <c>client.ContactCards</c>.
    /// </summary>
    public sealed class ContactCardSet
    {
        private readonly IContactCardService _service;

        internal ContactCardSet(IContactCardService service)
        {
            _service = service;
        }

        // ── Query entry points ────────────────────────────────────────────────

        /// <summary>Begins a filtered query.</summary>
        public ContactCardQuery Where(Expression<Func<ContactCardFilterSpec, bool>> predicate)
            => new ContactCardQuery(_service).Where(predicate);

        /// <summary>Begins a query and sets the maximum number of items to return.</summary>
        public ContactCardQuery Take(int count)
            => new ContactCardQuery(_service).Take(count);

        /// <summary>Begins a query and skips the first <paramref name="count"/> items.</summary>
        public ContactCardQuery Skip(int count)
            => new ContactCardQuery(_service).Skip(count);

        /// <summary>Begins a query sorted by the specified field ascending.</summary>
        public ContactCardQuery OrderBy<TKey>(
            Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
            => new ContactCardQuery(_service).OrderBy(keySelector);

        /// <summary>Begins a query sorted by the specified field descending.</summary>
        public ContactCardQuery OrderByDescending<TKey>(
            Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
            => new ContactCardQuery(_service).OrderByDescending(keySelector);

        // ── Single-item access ────────────────────────────────────────────────

        /// <summary>
        /// Returns the full data of a single contact card by ID,
        /// or <see langword="null"/> if not found.
        /// </summary>
        public async Task<ContactCard?> FindAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var response = await _service.GetAsync(id.ToString(), cancellationToken);
            return response.Data != null ? ContactCardMapper.ToEntity(response.Data) : null;
        }

        // ── CRUD ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new contact card. Sets <see cref="ContactCard.Id"/> on the entity after creation.
        /// </summary>
        public async Task AddAsync(
            ContactCard entity,
            CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var response = await _service.CreateAsync(
                ContactCardMapper.ToCreateRequest(entity), cancellationToken);

            entity.Id = int.TryParse(response.Data, out var id) ? id : (int?)null;
        }

        /// <summary>Updates an existing contact card. <see cref="ContactCard.Id"/> must be set.</summary>
        /// <exception cref="InvalidOperationException"><see cref="ContactCard.Id"/> is not set.</exception>
        public Task UpdateAsync(
            ContactCard entity,
            CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (!entity.Id.HasValue)
                throw new InvalidOperationException(
                    "ContactCard.Id must be set before calling UpdateAsync.");

            return _service.UpdateAsync(
                ContactCardMapper.ToUpdateRequest(entity), cancellationToken);
        }

        // ── Groups ────────────────────────────────────────────────────────────

        /// <summary>
        /// Replaces the group membership of a contact card.
        /// </summary>
        public Task SetGroupsAsync(
            int id,
            IReadOnlyList<string> groupIds,
            CancellationToken cancellationToken = default)
            => _service.SetGroupsAsync(id.ToString(), groupIds, cancellationToken);

        /// <summary>Returns the groups the specified contact card belongs to.</summary>
        public async Task<IReadOnlyList<ContactGroupRef>> GetGroupsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var response = await _service.GetGroupsAsync(id.ToString(), cancellationToken);
            return response.Data;
        }
    }
}
