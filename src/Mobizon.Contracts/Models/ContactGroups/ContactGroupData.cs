using System;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Represents a contact group as returned by <c>contactgroup/list</c>.
    /// </summary>
    public class ContactGroupData
    {
        /// <summary>Gets or sets the unique ID of the group.</summary>
        public long? Id { get; set; }

        /// <summary>Gets or sets the owner user ID.</summary>
        public long? UserId { get; set; }

        /// <summary>Gets or sets the display name of the group.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the number of contact cards in the group.</summary>
        [JsonPropertyName("cardsCnt")]
        public int CardsCount { get; set; }

        /// <summary>Gets or sets whether the group is hidden in the UI.</summary>
        public bool IsHidden { get; set; }

        /// <summary>Gets or sets the creation date and time of this group.</summary>
        [JsonPropertyName("createTs")]
        public DateTime? Created { get; set; }
    }
}
