using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts.Models.Common;
using Mobizon.Contracts.Models.ContactCards;
using Mobizon.Contracts.Services;
using Mobizon.Net.Internal;

namespace Mobizon.Net.ContactCards
{
    /// <summary>
    /// A composable query builder for <c>contactcard/list</c>.
    /// Obtain an instance from <see cref="ContactCardSet"/>:
    /// <code>client.ContactCards.Where(x => x.GroupId == 100604)</code>
    /// </summary>
    public sealed class ContactCardQuery
    {
        private readonly IContactCardService _service;

        private Expression<Func<ContactCardFilterSpec, bool>>? _predicate;
        private int? _take;
        private int? _skip;
        private string? _sortField;
        private SortDirection _sortDirection = SortDirection.ASC;

        internal ContactCardQuery(IContactCardService service)
        {
            _service = service;
        }

        /// <summary>Adds a filter predicate.</summary>
        public ContactCardQuery Where(Expression<Func<ContactCardFilterSpec, bool>> predicate)
        {
            _predicate = predicate;
            return this;
        }

        /// <summary>Sets the maximum number of items to return (page size).</summary>
        public ContactCardQuery Take(int count)
        {
            _take = count;
            return this;
        }

        /// <summary>
        /// Skips the first <paramref name="count"/> items.
        /// Translated to <c>currentPage = count / pageSize</c>.
        /// Requires <see cref="Take"/> to be set for accurate results.
        /// </summary>
        public ContactCardQuery Skip(int count)
        {
            _skip = count;
            return this;
        }

        /// <summary>Sorts results by the specified field ascending.</summary>
        public ContactCardQuery OrderBy<TKey>(
            Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
        {
            _sortField     = ExtractFieldName(keySelector);
            _sortDirection = SortDirection.ASC;
            return this;
        }

        /// <summary>Sorts results by the specified field descending.</summary>
        public ContactCardQuery OrderByDescending<TKey>(
            Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
        {
            _sortField     = ExtractFieldName(keySelector);
            _sortDirection = SortDirection.DESC;
            return this;
        }

        // ── Terminal operations ───────────────────────────────────────────────

        /// <summary>Executes the query and returns all matching contact cards on the current page.</summary>
        public async Task<IReadOnlyList<ContactCard>> ToListAsync(
            CancellationToken cancellationToken = default)
        {
            var response = await _service.ListAsync(BuildRequest(), cancellationToken);
            return Map(response.Data.Items);
        }

        /// <summary>
        /// Returns the first contact card matching the query, or <see langword="null"/> if none found.
        /// </summary>
        public async Task<ContactCard?> FirstOrDefaultAsync(
            CancellationToken cancellationToken = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 1), cancellationToken);
            return response.Data.Items.Count > 0
                ? ContactCardMapper.ToEntity(response.Data.Items[0])
                : null;
        }

        /// <summary>Returns the first contact card matching the query.</summary>
        /// <exception cref="InvalidOperationException">No elements found.</exception>
        public async Task<ContactCard> FirstAsync(CancellationToken cancellationToken = default)
        {
            var result = await FirstOrDefaultAsync(cancellationToken);
            return result ?? throw new InvalidOperationException("Sequence contains no elements.");
        }

        /// <summary>
        /// Returns the only contact card matching the query, or <see langword="null"/> if none found.
        /// </summary>
        /// <exception cref="InvalidOperationException">More than one element found.</exception>
        public async Task<ContactCard?> SingleOrDefaultAsync(
            CancellationToken cancellationToken = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 2), cancellationToken);
            if (response.Data.Items.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element.");
            return response.Data.Items.Count == 1
                ? ContactCardMapper.ToEntity(response.Data.Items[0])
                : null;
        }

        /// <summary>Returns the only contact card matching the query.</summary>
        /// <exception cref="InvalidOperationException">No elements or more than one element found.</exception>
        public async Task<ContactCard> SingleAsync(CancellationToken cancellationToken = default)
        {
            var result = await SingleOrDefaultAsync(cancellationToken);
            return result ?? throw new InvalidOperationException("Sequence contains no elements.");
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private ContactCardListRequest BuildRequest(int? takeOverride = null)
        {
            PaginationRequest? pagination = null;
            var pageSize = takeOverride ?? _take;
            if (pageSize.HasValue || _skip.HasValue)
            {
                var size = pageSize ?? 25;
                var page = _skip.HasValue ? _skip.Value / size : 0;
                pagination = new PaginationRequest { CurrentPage = page, PageSize = size };
            }

            return new ContactCardListRequest
            {
                Criteria   = _predicate != null
                                 ? ContactCardExpressionParser.Parse(_predicate)
                                 : null,
                Pagination = pagination,
                Sort       = _sortField != null
                                 ? new SortRequest { Field = _sortField, Direction = _sortDirection }
                                 : null
            };
        }

        private static IReadOnlyList<ContactCard> Map(IReadOnlyList<ContactCardData> items)
        {
            var result = new ContactCard[items.Count];
            for (var i = 0; i < items.Count; i++)
                result[i] = ContactCardMapper.ToEntity(items[i]);
            return result;
        }

        private static string ExtractFieldName<TKey>(
            Expression<Func<ContactCardFilterSpec, TKey>> expr)
        {
            if (expr.Body is MemberExpression member)
                return ContactCardExpressionParser.GetApiFieldName(member.Member.Name);

            throw new ArgumentException(
                "Selector must be a simple property access, e.g. x => x.Surname.",
                nameof(expr));
        }
    }
}
