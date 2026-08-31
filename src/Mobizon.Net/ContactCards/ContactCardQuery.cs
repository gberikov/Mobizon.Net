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
    /// An immutable, composable query over <c>contactcard/list</c>. Every builder call returns a new
    /// query and never modifies the receiver, so a base query can be reused:
    /// <code>
    /// var kz = client.ContactCards.Where(x => x.Address.CountryA2 == "KZ").Take(50);
    /// var page0 = await kz.Page(0).ToListAsync();
    /// var page1 = await kz.Page(1).ToListAsync();
    /// </code>
    /// </summary>
    internal sealed class ContactCardQuery : IContactCardQuery
    {
        private const int DefaultPageSize = 25;

        private readonly ContactCardService _service;
        private readonly Expression<Func<ContactCardFilterSpec, bool>>? _predicate;
        private readonly int? _take;
        private readonly int? _page;
        private readonly string? _sortField;
        private readonly SortDirection _sortDirection;

        internal ContactCardQuery(ContactCardService service)
            : this(service, null, null, null, null, SortDirection.Ascending)
        {
        }

        private ContactCardQuery(
            ContactCardService service,
            Expression<Func<ContactCardFilterSpec, bool>>? predicate,
            int? take,
            int? page,
            string? sortField,
            SortDirection sortDirection)
        {
            _service = service;
            _predicate = predicate;
            _take = take;
            _page = page;
            _sortField = sortField;
            _sortDirection = sortDirection;
        }

        /// <inheritdoc />
        public IContactCardQuery Where(Expression<Func<ContactCardFilterSpec, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var combined = _predicate == null ? predicate : CombineAnd(_predicate, predicate);
            return new ContactCardQuery(_service, combined, _take, _page, _sortField, _sortDirection);
        }

        /// <inheritdoc />
        public IContactCardQuery Take(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Page size must be positive.");
            return new ContactCardQuery(_service, _predicate, count, _page, _sortField, _sortDirection);
        }

        /// <inheritdoc />
        public IContactCardQuery Page(int pageIndex)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index is zero-based and cannot be negative.");
            return new ContactCardQuery(_service, _predicate, _take, pageIndex, _sortField, _sortDirection);
        }

        /// <inheritdoc />
        public IContactCardQuery OrderBy<TKey>(Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
            => new ContactCardQuery(_service, _predicate, _take, _page, ExtractFieldName(keySelector), SortDirection.Ascending);

        /// <inheritdoc />
        public IContactCardQuery OrderByDescending<TKey>(Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
            => new ContactCardQuery(_service, _predicate, _take, _page, ExtractFieldName(keySelector), SortDirection.Descending);

        // ── Terminal operations ───────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<IReadOnlyList<ContactCard>> ToListAsync(CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(), ct).ConfigureAwait(false);
            return Map(ItemsOf(response));
        }

        /// <inheritdoc />
        public async Task<PaginatedResponse<ContactCard>> ToPageAsync(CancellationToken ct = default)
        {
            var request = BuildRequest();
            var response = await _service.ListAsync(request, ct).ConfigureAwait(false);
            return new PaginatedResponse<ContactCard>
            {
                Items       = Map(ItemsOf(response)),
                TotalCount  = response?.TotalItemCount ?? 0,
                CurrentPage = request.Pagination?.CurrentPage ?? 0,
                PageSize    = request.Pagination?.PageSize    ?? DefaultPageSize
            };
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 1), ct).ConfigureAwait(false);
            return response?.TotalItemCount ?? 0;
        }

        /// <inheritdoc />
        public async Task<ContactCard?> FirstOrDefaultAsync(CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 1), ct).ConfigureAwait(false);
            var items = ItemsOf(response);
            return items.Count > 0 ? ContactCardMapper.ToEntity(items[0]) : null;
        }

        /// <inheritdoc />
        public async Task<ContactCard> FirstAsync(CancellationToken ct = default)
        {
            var result = await FirstOrDefaultAsync(ct).ConfigureAwait(false);
            return result ?? throw new InvalidOperationException("Sequence contains no elements.");
        }

        /// <inheritdoc />
        public async Task<ContactCard?> SingleOrDefaultAsync(CancellationToken ct = default)
        {
            var response = await _service.ListAsync(BuildRequest(takeOverride: 2), ct).ConfigureAwait(false);
            var items = ItemsOf(response);
            if (items.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element.");
            return items.Count == 1 ? ContactCardMapper.ToEntity(items[0]) : null;
        }

        /// <inheritdoc />
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
            if (pageSize.HasValue || _page.HasValue)
                pagination = new PaginationRequest { CurrentPage = _page ?? 0, PageSize = pageSize ?? DefaultPageSize };

            return new ContactCardListRequest
            {
                Criteria   = _predicate != null ? ContactCardExpressionParser.Parse(_predicate) : null,
                Pagination = pagination,
                Sort       = _sortField != null
                                 ? new SortRequest { Field = _sortField, Direction = _sortDirection }
                                 : null
            };
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

        private static IReadOnlyList<ContactCard> Map(IReadOnlyList<ContactCardData> items)
            => items.Select(ContactCardMapper.ToEntity).ToArray();

        // contactcard/list may return success with a null data payload; treat it as an empty page.
        private static IReadOnlyList<ContactCardData> ItemsOf(MobizonListResult<ContactCardData> response)
            => response?.Items ?? Array.Empty<ContactCardData>();

        private static string ExtractFieldName<TKey>(Expression<Func<ContactCardFilterSpec, TKey>> expr)
        {
            Expression body = expr.Body is UnaryExpression { NodeType: ExpressionType.Convert } u
                ? u.Operand : expr.Body;

            if (body is MemberExpression)
            {
                var path = new List<string>();
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
