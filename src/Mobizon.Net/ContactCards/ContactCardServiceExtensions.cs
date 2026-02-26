using System;
using System.Linq.Expressions;
using Mobizon.Contracts.Models.ContactCards;
using Mobizon.Contracts.Services;

namespace Mobizon.Net.ContactCards
{
    /// <summary>
    /// LINQ-style query extensions for <see cref="IContactCardService"/>.
    /// These are useful when working with a raw <see cref="IContactCardService"/> reference.
    /// When using <see cref="MobizonClient"/> directly, prefer <c>client.ContactCards</c>
    /// which exposes <see cref="ContactCardSet"/>.
    /// </summary>
    public static class ContactCardServiceExtensions
    {
        /// <summary>Begins a query with the specified filter predicate.</summary>
        public static ContactCardQuery Where(
            this IContactCardService service,
            Expression<Func<ContactCardFilterSpec, bool>> predicate)
            => new ContactCardQuery(service).Where(predicate);

        /// <summary>Begins a query and sets the maximum number of items to return.</summary>
        public static ContactCardQuery Take(this IContactCardService service, int count)
            => new ContactCardQuery(service).Take(count);

        /// <summary>Begins a query and skips the first <paramref name="count"/> items.</summary>
        public static ContactCardQuery Skip(this IContactCardService service, int count)
            => new ContactCardQuery(service).Skip(count);

        /// <summary>Begins a query sorted by the specified field ascending.</summary>
        public static ContactCardQuery OrderBy<TKey>(
            this IContactCardService service,
            Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
            => new ContactCardQuery(service).OrderBy(keySelector);

        /// <summary>Begins a query sorted by the specified field descending.</summary>
        public static ContactCardQuery OrderByDescending<TKey>(
            this IContactCardService service,
            Expression<Func<ContactCardFilterSpec, TKey>> keySelector)
            => new ContactCardQuery(service).OrderByDescending(keySelector);
    }
}
