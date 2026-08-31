using System;
using System.Collections.Generic;
using System.IO;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Represents a contact card entity for use with the EF Core-style query API.
    /// </summary>
    public class ContactCard
    {
        // ── Fields set from API responses ────────────────────────────────────

        /// <summary>Gets or sets the unique ID. Populated by <c>AddAsync</c> after creation.</summary>
        public long? Id { get; set; }

        /// <summary>Gets or sets the owner user ID. Set by the API on read.</summary>
        public long? UserId { get; set; }

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

        /// <summary>Gets or sets the mobile phone details (number, type, operator info).</summary>
        public MobileFieldInfo? Mobile { get; set; }

        /// <summary>Gets or sets the email address details (value and type).</summary>
        public ContactFieldInfo? Email { get; set; }

        /// <summary>Gets or sets the Viber number details (value and type).</summary>
        public ContactFieldInfo? Viber { get; set; }

        /// <summary>Gets or sets the WhatsApp number details (value and type).</summary>
        public ContactFieldInfo? WhatsApp { get; set; }

        /// <summary>Gets or sets the landline phone number details (value and type).</summary>
        public ContactFieldInfo? Landline { get; set; }

        /// <summary>Gets or sets the Skype handle details (value and type).</summary>
        public ContactFieldInfo? Skype { get; set; }

        /// <summary>Gets or sets the Telegram handle details (value and type).</summary>
        public ContactFieldInfo? Telegram { get; set; }

        /// <summary>Gets or sets the address.</summary>
        public AddressFieldInfo? Address { get; set; }

        /// <summary>Gets or sets the date of birth.</summary>
        public DateTime? BirthDate { get; set; }

        /// <summary>Gets or sets the gender.</summary>
        public Gender? Gender { get; set; }

        /// <summary>Gets or sets the company name.</summary>
        public string? CompanyName { get; set; }

        /// <summary>Gets or sets the company website URL.</summary>
        public string? CompanyUrl { get; set; }

        /// <summary>Gets or sets free-form notes about the contact.</summary>
        public string? Info { get; set; }

        /// <summary>
        /// Gets or sets the photo to upload with the next <c>AddAsync</c>/<c>UpdateAsync</c> call.
        /// One-shot: the call consumes the stream and resets this property (and
        /// <see cref="PhotoFileName"/>) to <see langword="null"/>; the caller still owns and disposes
        /// the stream. Write-only: the API does not return the photo, so this is always
        /// <see langword="null"/> on a card read back from the server.
        /// </summary>
        public Stream? Photo { get; set; }

        /// <summary>
        /// Gets or sets the file name sent with <see cref="Photo"/> (e.g. <c>"photo.jpg"</c>).
        /// Used only when <see cref="Photo"/> is set.
        /// </summary>
        public string? PhotoFileName { get; set; }
    }
}
