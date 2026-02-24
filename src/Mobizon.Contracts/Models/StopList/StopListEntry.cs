namespace Mobizon.Contracts.Models.StopList
{
    /// <summary>
    /// Represents a single entry in the user's number stop-list,
    /// as returned by <c>numberstoplist/list</c>.
    /// </summary>
    public class StopListEntry
    {
        /// <summary>Gets or sets the unique ID of this stop-list record.</summary>
        public string? Id { get; set; }

        /// <summary>Gets or sets the owner user ID.</summary>
        public string? UserId { get; set; }

        /// <summary>Gets or sets the partner ID.</summary>
        public string? PartnerId { get; set; }

        /// <summary>Gets or sets the blocked phone number in international format.</summary>
        public string? Number { get; set; }

        /// <summary>
        /// Gets or sets whether single messages to this number are blocked
        /// in addition to campaign messages.
        /// <c>"1"</c> — blocked; <c>"0"</c> — campaign-only block.
        /// </summary>
        public string? IgnoreSingle { get; set; }

        /// <summary>
        /// Gets or sets the scope level of the block:
        /// <c>"0"</c> — user-level; <c>"1"</c> — partner-level; <c>"2"</c> — global.
        /// </summary>
        public string? Level { get; set; }

        /// <summary>Gets or sets the ID of the user who created this record.</summary>
        public string? CreatedByUserId { get; set; }

        /// <summary>Gets or sets the first name of the user who created this record.</summary>
        public string? CreatedByUserName { get; set; }

        /// <summary>Gets or sets the last name of the user who created this record.</summary>
        public string? CreatedByUserSurname { get; set; }

        /// <summary>Gets or sets the creation timestamp. Format: <c>YYYY-MM-DD HH:MM:SS</c>.</summary>
        public string? CreateTs { get; set; }

        /// <summary>Gets or sets the optional comment describing why the number was blocked.</summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets whether this is a system-level block.
        /// <c>"1"</c> — system; <c>"0"</c> — user-created.
        /// </summary>
        public string? IsSystem { get; set; }

        /// <summary>Gets or sets the ISO 3166-1 alpha-2 country code of the number.</summary>
        public string? CountryA2 { get; set; }

        /// <summary>Gets or sets the mobile operator name for the number.</summary>
        public string? OperatorName { get; set; }
    }
}
