using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;

namespace Mobizon.Net
{
    /// <summary>
    /// Provides EF Core-style CRUD and query operations for contact cards.
    /// Accessible via <c>client.ContactCards</c>.
    /// </summary>
    public sealed class ContactCardSet : IContactCardSet
    {
        private readonly ContactCardService _service;

        internal ContactCardSet(ContactCardService service)
        {
            _service = service;
        }

        // ── Query entry points ────────────────────────────────────────────────

        /// <summary>Begins a filtered query.</summary>
        public IContactCardQuery Where(Expression<Func<ContactCardFilterSpec, bool>> predicate)
            => new ContactCardQuery(_service).Where(predicate);

        /// <summary>Begins a query and sets the maximum number of items to return.</summary>
        public IContactCardQuery Take(int count)
            => new ContactCardQuery(_service).Take(count);

        /// <summary>Begins a query and skips the first <paramref name="count"/> items.</summary>
        public IContactCardQuery Skip(int count)
            => new ContactCardQuery(_service).Skip(count);

        /// <summary>Begins a query sorted by the specified field ascending.</summary>
        public IContactCardQuery OrderBy<TKey>(
            Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
            => new ContactCardQuery(_service).OrderBy(keySelector);

        /// <summary>Begins a query sorted by the specified field descending.</summary>
        public IContactCardQuery OrderByDescending<TKey>(
            Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
            => new ContactCardQuery(_service).OrderByDescending(keySelector);

        // ── Single-item access ────────────────────────────────────────────────

        /// <summary>
        /// Returns the full data of a single contact card by ID,
        /// or <see langword="null"/> if not found.
        /// </summary>
        public async Task<ContactCard?> FindAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            var response = await _service.GetAsync(id.ToString(), cancellationToken).ConfigureAwait(false);
            return response != null ? ContactCardMapper.ToEntity(response) : null;
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
                ContactCardMapper.ToCreateRequest(entity), cancellationToken).ConfigureAwait(false);

            entity.Id = long.TryParse(response, out var id) ? id : (long?)null;
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

        /// <summary>Deletes the contact card with the specified ID.</summary>
        public Task RemoveAsync(long id, CancellationToken cancellationToken = default)
            => _service.RemoveAsync(id.ToString(), cancellationToken);

        // ── Groups ────────────────────────────────────────────────────────────

        /// <summary>
        /// Replaces the group membership of a contact card.
        /// </summary>
        public Task SetGroupsAsync(
            long id,
            IReadOnlyList<string> groupIds,
            CancellationToken cancellationToken = default)
            => _service.SetGroupsAsync(id.ToString(), groupIds, cancellationToken);

        /// <summary>Returns the groups the specified contact card belongs to.</summary>
        public async Task<IReadOnlyList<ContactGroupRef>> GetGroupsAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            var response = await _service.GetGroupsAsync(id.ToString(), cancellationToken).ConfigureAwait(false);
            return response ?? Array.Empty<ContactGroupRef>();
        }
    }
}
