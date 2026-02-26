using System.Collections.Generic;

namespace Mobizon.Contracts.Models.ContactCards
{
    /// <summary>
    /// Represents a contact card entity for use with the EF Core-style query API.
    /// </summary>
    public class ContactCard
    {
        // ── Fields set from API responses ────────────────────────────────────

        /// <summary>Gets or sets the unique ID. Populated by <c>AddAsync</c> after creation.</summary>
        public int? Id { get; set; }

        /// <summary>Gets or sets the owner user ID. Set by the API on read.</summary>
        public int? UserId { get; set; }

        /// <summary>Gets or sets whether the card has been deleted.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>Gets or sets whether the contact is available for sending.</summary>
        public bool IsAvailable { get; set; }

        /// <summary>Gets or sets the groups this contact belongs to. Set by the API on read.</summary>
        public IReadOnlyList<ContactGroupRef>? Groups { get; set; }

        // ── Editable contact fields ───────────────────────────────────────────

        /// <summary>Gets or sets the title/salutation.</summary>
        public string? Title { get; set; }

        /// <summary>Gets or sets the first name.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the last name.</summary>
        public string? Surname { get; set; }

        /// <summary>Gets or sets the mobile phone number in international format.</summary>
        public string? Mobile { get; set; }

        /// <summary>Gets or sets the mobile phone type (e.g. HOME, JOB, MAIN).</summary>
        public string? MobileType { get; set; }

        /// <summary>Gets or sets the email address.</summary>
        public string? Email { get; set; }

        /// <summary>Gets or sets the Viber number.</summary>
        public string? Viber { get; set; }

        /// <summary>Gets or sets the WhatsApp number.</summary>
        public string? Whatsapp { get; set; }

        /// <summary>Gets or sets the landline phone number.</summary>
        public string? Landline { get; set; }

        /// <summary>Gets or sets the Skype handle.</summary>
        public string? Skype { get; set; }

        /// <summary>Gets or sets the Telegram handle.</summary>
        public string? Telegram { get; set; }

        /// <summary>Gets or sets the date of birth. Format: <c>YYYY-MM-DD</c>.</summary>
        public string? BirthDate { get; set; }

        /// <summary>Gets or sets the gender.</summary>
        public string? Gender { get; set; }

        /// <summary>Gets or sets the company name.</summary>
        public string? CompanyName { get; set; }

        /// <summary>Gets or sets the company website URL.</summary>
        public string? CompanyUrl { get; set; }

        /// <summary>Gets or sets free-form notes about the contact.</summary>
        public string? Info { get; set; }
    }
}
