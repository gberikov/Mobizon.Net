using System.IO;

namespace Mobizon.Contracts.Models.ContactCard
{
    /// <summary>
    /// Request parameters for creating a new contact card via <c>contactcard/create</c>.
    /// </summary>
    public class CreateContactCardRequest
    {
        /// <summary>Gets or sets the title/salutation.</summary>
        public string? Title { get; set; }

        /// <summary>Gets or sets the first name.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the last name.</summary>
        public string? Surname { get; set; }

        /// <summary>Gets or sets the mobile phone number in international format.</summary>
        public string? MobileValue { get; set; }

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

        /// <summary>
        /// Gets or sets the photo stream to upload.
        /// When <see langword="null"/>, no photo is attached.
        /// </summary>
        public Stream? Photo { get; set; }

        /// <summary>
        /// Gets or sets the file name for the photo (e.g. <c>"photo.jpg"</c>).
        /// Used only when <see cref="Photo"/> is set.
        /// </summary>
        public string? PhotoFileName { get; set; }
    }
}
