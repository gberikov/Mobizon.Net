using System;
using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.StopLists
{
    /// <summary>
    /// Represents a single entry in the user's number stop-list,
    /// as returned by <c>numberstoplist/list</c>.
    /// </summary>
    public class StopListEntry
    {
        /// <summary>Gets or sets the unique ID of this stop-list record.</summary>
        public int? Id { get; set; }

        /// <summary>Gets or sets the owner user ID.</summary>
        public int? UserId { get; set; }

        /// <summary>Gets or sets the partner ID.</summary>
        public int? PartnerId { get; set; }

        /// <summary>Gets or sets the blocked phone number in international format.</summary>
        public string? Number { get; set; }

        /// <summary>
        /// Gets or sets whether single messages to this number are blocked
        /// in addition to campaign messages.
        /// </summary>
        public bool? IgnoreSingle { get; set; }

        /// <summary>Gets or sets the scope level of the block.</summary>
        public StopListLevel? Level { get; set; }

        /// <summary>Gets or sets the ID of the user who created this record.</summary>
        public int? CreatedByUserId { get; set; }

        /// <summary>Gets or sets the first name of the user who created this record.</summary>
        public string? CreatedByUserName { get; set; }

        /// <summary>Gets or sets the last name of the user who created this record.</summary>
        public string? CreatedByUserSurname { get; set; }

        /// <summary>Gets or sets the creation date and time of this record.</summary>
        [JsonPropertyName("createTs")]
        public DateTime? Created { get; set; }

        /// <summary>Gets or sets the optional comment describing why the number was blocked.</summary>
        public string? Comment { get; set; }

        /// <summary>Gets or sets whether the comment is a template-generated comment.</summary>
        public bool? IsTemplateComment { get; set; }

        /// <summary>Gets or sets whether this is a system-level block.</summary>
        public bool? IsSystem { get; set; }

        /// <summary>Gets or sets the ISO 3166-1 alpha-2 country code of the number.</summary>
        public string? CountryA2 { get; set; }

        /// <summary>Gets or sets the mobile operator name for the number.</summary>
        public string? OperatorName { get; set; }
    }
}
