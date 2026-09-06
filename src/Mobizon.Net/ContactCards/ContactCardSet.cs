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
    internal sealed class ContactCardSet : IContactCardSet
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

        /// <summary>Begins a query positioned on the given zero-based page.</summary>
        public IContactCardQuery Page(int pageIndex)
            => new ContactCardQuery(_service).Page(pageIndex);

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
            var response = await _service.GetAsync(ApiFormat.Int(id), cancellationToken).ConfigureAwait(false);
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

            entity.Id = await _service.CreateAsync(
                ContactCardMapper.ToCreateRequest(entity), cancellationToken).ConfigureAwait(false);
            ForgetPhoto(entity);
        }

        /// <summary>Updates an existing contact card. <see cref="ContactCard.Id"/> must be set.</summary>
        /// <exception cref="InvalidOperationException"><see cref="ContactCard.Id"/> is not set.</exception>
        public async Task UpdateAsync(
            ContactCard entity,
            CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (!entity.Id.HasValue)
                throw new InvalidOperationException(
                    "ContactCard.Id must be set before calling UpdateAsync.");

            await _service.UpdateAsync(
                ContactCardMapper.ToUpdateRequest(entity), cancellationToken).ConfigureAwait(false);
            ForgetPhoto(entity);
        }

        // The photo stream has been read to EOF by the send (the SDK never closes it — the caller owns it),
        // so a later UpdateAsync on the same entity must not try to re-send it.
        private static void ForgetPhoto(ContactCard entity)
        {
            entity.Photo = null;
            entity.PhotoFileName = null;
        }

        /// <summary>Deletes the contact card with the specified ID.</summary>
        public Task RemoveAsync(long id, CancellationToken cancellationToken = default)
            => _service.RemoveAsync(ApiFormat.Int(id), cancellationToken);

        // ── Groups ────────────────────────────────────────────────────────────

        /// <summary>
        /// Replaces the group membership of a contact card.
        /// </summary>
        public Task SetGroupsAsync(
            long id,
            IReadOnlyList<long> groupIds,
            CancellationToken cancellationToken = default)
            => _service.SetGroupsAsync(ApiFormat.Int(id), groupIds, cancellationToken);

        /// <summary>Returns the groups the specified contact card belongs to.</summary>
        public async Task<IReadOnlyList<ContactGroupRef>> GetGroupsAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            var response = await _service.GetGroupsAsync(ApiFormat.Int(id), cancellationToken).ConfigureAwait(false);
            return response ?? Array.Empty<ContactGroupRef>();
        }
    }
}
