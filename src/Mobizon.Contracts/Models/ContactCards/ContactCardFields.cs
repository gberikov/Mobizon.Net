using System.Text.Json.Serialization;

namespace Mobizon.Contracts.Models.ContactCards
{
    /// <summary>
    /// Represents the field values of a contact card.
    /// </summary>
    public class ContactCardFields
    {
        /// <summary>Gets or sets the title/salutation.</summary>
        public string? Title { get; set; }

        /// <summary>Gets or sets the first name.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the last name.</summary>
        public string? Surname { get; set; }

        /// <summary>Gets or sets the mobile phone details.</summary>
        public MobileFieldInfo? Mobile { get; set; }

        /// <summary>Gets or sets the email address.</summary>
        public ContactFieldInfo? Email { get; set; }

        /// <summary>Gets or sets the Viber number.</summary>
        public ContactFieldInfo? Viber { get; set; }

        /// <summary>Gets or sets the WhatsApp number.</summary>
        public ContactFieldInfo? WhatsApp { get; set; }

        /// <summary>Gets or sets the landline phone number.</summary>
        public ContactFieldInfo? Landline { get; set; }

        /// <summary>Gets or sets the Skype handle.</summary>
        public ContactFieldInfo? Skype { get; set; }

        /// <summary>Gets or sets the Telegram handle.</summary>
        public ContactFieldInfo? Telegram { get; set; }

        /// <summary>Gets or sets the address.</summary>
        public AddressFieldInfo? Address { get; set; }

        /// <summary>Gets or sets the date of birth. Format: <c>YYYY-MM-DD</c>.</summary>
        [JsonPropertyName("birth_date")]
        public string? BirthDate { get; set; }

        /// <summary>Gets or sets the gender.</summary>
        public string? Gender { get; set; }

        /// <summary>Gets or sets the company name.</summary>
        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        /// <summary>Gets or sets the company website URL.</summary>
        [JsonPropertyName("company_url")]
        public string? CompanyUrl { get; set; }

        /// <summary>Gets or sets free-form notes about the contact.</summary>
        public string? Info { get; set; }
    }
}
