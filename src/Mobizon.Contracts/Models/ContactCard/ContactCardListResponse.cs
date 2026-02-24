using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.ContactCard
{
    /// <summary>
    /// Represents the paginated response returned by <c>contactcard/list</c>.
    /// </summary>
    public class ContactCardListResponse
    {
        /// <summary>Gets or sets the contact cards on the current page.</summary>
        public IReadOnlyList<ContactCardData> Items { get; set; } = new List<ContactCardData>();

        /// <summary>Gets or sets the total number of cards matching the current filter.</summary>
        public int TotalItemCount { get; set; }

        /// <summary>Gets or sets the total number of cards in the unfiltered list.</summary>
        [JsonPropertyName("fullListItemCount")]
        public int FullListItemCount { get; set; }
    }
}
