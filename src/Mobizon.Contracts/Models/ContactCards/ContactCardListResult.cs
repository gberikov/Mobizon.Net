using Mobizon.Contracts;

namespace Mobizon.Contracts
{
    /// <summary>
    /// List result for <c>contactcard/list</c>. Extends <see cref="MobizonListResult{T}"/>
    /// with <see cref="FullListItemCount"/>, which is only returned by this endpoint.
    /// </summary>
    public class ContactCardListResult : MobizonListResult<ContactCardData>
    {
        /// <summary>
        /// Total number of items in the unfiltered list (before any criteria are applied).
        /// Only returned by <c>contactcard/list</c>.
        /// </summary>
        public int FullListItemCount { get; set; }
    }
}
