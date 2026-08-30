using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;

namespace Mobizon.Net
{
    /// <summary>
    /// A composable query builder for <c>contactcard/list</c>.
    /// Obtain an instance from <see cref="ContactCardSet"/>:
    /// <code>client.ContactCards.Where(x => x.GroupId == 100604)</code>
    /// </summary>
    public sealed class ContactCardQuery : IContactCardQuery
    {
        private readonly ContactCardService _service;

        private Expression<Func<ContactCardFilterSpec, bool>>? _predicate;
        private int? _take;
        private int? _skip;
        private string? _sortField;
        private SortDirection _sortDirection = SortDirection.ASC;

        internal ContactCardQuery(ContactCardService service)
        {
            _service = service;
        }

        /// <summary>
        /// Adds a filter predicate. Each successive call is combined with the previous one
        /// using a logical AND, so <c>.Where(a).Where(b)</c> filters by <c>a AND b</c>.
        /// </summary>
        public IContactCardQuery Where(Expression<Func<ContactCardFilterSpec, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            _predicate = _predicate == null ? predicate : CombineAnd(_predicate, predicate);
            return this;
        }

        private static Expression<Func<ContactCardFilterSpec, bool>> CombineAnd(
            Expression<Func<ContactCardFilterSpec, bool>> left,
            Expression<Func<ContactCardFilterSpec, bool>> right)
        {
            var param = Expression.Parameter(typeof(ContactCardFilterSpec), "x");
            var body = Expression.AndAlso(
                new ReplaceParam(left.Parameters[0], param).Visit(left.Body),
                new ReplaceParam(right.Parameters[0], param).Visit(right.Body));
            return Expression.Lambda<Func<ContactCardFilterSpec, bool>>(body, param);
        }

        private sealed class ReplaceParam : ExpressionVisitor
        {
            private readonly ParameterExpression _from, _to;
            public ReplaceParam(ParameterExpression from, ParameterExpression to) { _from = from; _to = to; }
            protected override Expression VisitParameter(ParameterExpression node) => node == _from ? _to : base.VisitParameter(node);
        }

        /// <summary>Sets the maximum number of items to return (page size).</summary>
        public IContactCardQuery Take(int count)
        {
            _take = count;
            return this;
        }

        /// <summary>
        /// Skips the first <paramref name="count"/> items.
        /// This value is page-based: it is translated to <c>currentPage = Skip / pageSize</c>,
        /// so skip is accurate only at exact multiples of the page size.
        /// Always pair with <see cref="Take"/> to set the page size; otherwise a default of 25 is used.
        /// </summary>
        public IContactCardQuery Skip(int count)
        {
            _skip = count;
            return this;
        }

        /// <summary>Sorts results by the specified field ascending.</summary>
        public IContactCardQuery OrderBy<TKey>(
            Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
        {
            _sortField     = ExtractFieldName(keySelector);
            _sortDirection = SortDirection.ASC;
            return this;
        }

        /// <summary>Sorts results by the specified field descending.</summary>
        public IContactCardQuery OrderByDescending<TKey>(
            Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
        {
            _sortField     = ExtractFieldName(keySelector);
            _sortDirection = SortDirection.DESC;
            return this;
        }

        // ── Terminal operations ───────────────────────────────────────────────

        /// <summary>Executes the query and returns all matching contact cards on the current page.</summary>
        public async Task<IReadOnlyList<ContactCard>> ToListAsync(
            CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(), ct).ConfigureAwait(false);
            return Map(ItemsOf(response));
        }

        /// <summary>Executes the query and returns the page with pagination metadata.</summary>
        public async Task<PaginatedResponse<ContactCard>> ToPageAsync(
            CancellationToken ct = default)
        {
            var request  = BuildRequest();
            var response = await _service.ListAsync(request, ct).ConfigureAwait(false);
            return new PaginatedResponse<ContactCard>
            {
                Items       = Map(ItemsOf(response)),
                TotalCount  = response?.TotalItemCount ?? 0,
                CurrentPage = request.Pagination?.CurrentPage ?? 0,
                PageSize    = request.Pagination?.PageSize    ?? 0
            };
        }

        /// <summary>Returns the total number of matching items without fetching their data.</summary>
        public async Task<int> CountAsync(CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 1), ct).ConfigureAwait(false);
            return response?.TotalItemCount ?? 0;
        }

        /// <summary>
        /// Returns the first contact card matching the query, or <see langword="null"/> if none found.
        /// </summary>
        public async Task<ContactCard?> FirstOrDefaultAsync(
            CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 1), ct).ConfigureAwait(false);
            var items = ItemsOf(response);
            return items.Count > 0
                ? ContactCardMapper.ToEntity(items[0])
                : null;
        }

        /// <summary>Returns the first contact card matching the query.</summary>
        /// <exception cref="InvalidOperationException">No elements found.</exception>
        public async Task<ContactCard> FirstAsync(CancellationToken ct = default)
        {
            var result = await FirstOrDefaultAsync(ct).ConfigureAwait(false);
            return result ?? throw new InvalidOperationException("Sequence contains no elements.");
        }

        /// <summary>
        /// Returns the only contact card matching the query, or <see langword="null"/> if none found.
        /// </summary>
        /// <exception cref="InvalidOperationException">More than one element found.</exception>
        public async Task<ContactCard?> SingleOrDefaultAsync(
            CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 2), ct).ConfigureAwait(false);
            var items = ItemsOf(response);
            if (items.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element.");
            return items.Count == 1
                ? ContactCardMapper.ToEntity(items[0])
                : null;
        }

        /// <summary>Returns the only contact card matching the query.</summary>
        /// <exception cref="InvalidOperationException">No elements or more than one element found.</exception>
        public async Task<ContactCard> SingleAsync(CancellationToken ct = default)
        {
            var result = await SingleOrDefaultAsync(ct).ConfigureAwait(false);
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
            => items.Select(ContactCardMapper.ToEntity).ToArray();

        // contactcard/list may return success with a null data payload; treat it as an empty page
        // rather than dereferencing null (mirrors the Data null-check in FindAsync/ContactCardSet).
        private static IReadOnlyList<ContactCardData> ItemsOf(ContactCardListResult response)
            => response?.Items ?? Array.Empty<ContactCardData>();

        private static string ExtractFieldName<TKey>(
            Expression<Func<ContactCardFilterSpec, TKey>> expr)
        {
            // Unwrap Convert nodes that appear for nullable value types
            Expression body = expr.Body is UnaryExpression { NodeType: ExpressionType.Convert } u
                ? u.Operand : expr.Body;

            if (body is MemberExpression)
            {
                var path = new System.Collections.Generic.List<string>();
                Expression current = body;
                while (current is MemberExpression m)
                {
                    path.Insert(0, m.Member.Name);
                    current = m.Expression!;
                }
                return ContactCardExpressionParser.GetApiFieldName(path.ToArray());
            }

            throw new ArgumentException(
                "Selector must be a property access, e.g. x => x.Surname or x => x.Mobile.Value.",
                nameof(expr));
        }
    }
}
