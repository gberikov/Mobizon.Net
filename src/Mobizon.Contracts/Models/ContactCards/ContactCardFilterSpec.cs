using System;

namespace Mobizon.Contracts.Models.ContactCards
{
    /// <summary>
    /// Describes the filterable and sortable fields of a contact card.
    /// Used as the parameter type in LINQ-style query expressions:
    /// <code>client.ContactCards.Where(x => x.GroupId == 100604)</code>
    /// </summary>
    public sealed class ContactCardFilterSpec
    {
        /// <summary>Group the contact belongs to. Use <c>null</c> to match contacts without a group.</summary>
        public int? GroupId => null;

        /// <summary>Contact's title / salutation.</summary>
        public string Title { get; } = string.Empty;

        /// <summary>Contact's first name.</summary>
        public string Name { get; } = string.Empty;

        /// <summary>Contact's last name.</summary>
        public string Surname { get; } = string.Empty;

        /// <summary>Mobile phone. Supports <c>.Type</c> and <c>.Value</c>.</summary>
        public ContactFieldFilterSpec Mobile => ContactFieldFilterSpec.Instance;

        /// <summary>Email address. Supports <c>.Type</c> and <c>.Value</c>.</summary>
        public ContactFieldFilterSpec Email => ContactFieldFilterSpec.Instance;

        /// <summary>Viber number. Supports <c>.Type</c> and <c>.Value</c>.</summary>
        public ContactFieldFilterSpec Viber => ContactFieldFilterSpec.Instance;

        /// <summary>WhatsApp number. Supports <c>.Type</c> and <c>.Value</c>.</summary>
        public ContactFieldFilterSpec WhatsApp => ContactFieldFilterSpec.Instance;

        /// <summary>Landline phone. Supports <c>.Type</c> and <c>.Value</c>.</summary>
        public ContactFieldFilterSpec Landline => ContactFieldFilterSpec.Instance;

        /// <summary>Skype handle. Supports <c>.Type</c> and <c>.Value</c>.</summary>
        public ContactFieldFilterSpec Skype => ContactFieldFilterSpec.Instance;

        /// <summary>Telegram handle. Supports <c>.Type</c> and <c>.Value</c>.</summary>
        public ContactFieldFilterSpec Telegram => ContactFieldFilterSpec.Instance;

        /// <summary>Address sub-fields. Supports <c>.CountryA2</c>, <c>.Country</c>, <c>.RegionId</c>,
        /// <c>.Region</c>, <c>.CityId</c>, <c>.City</c>, <c>.PostalCode</c>, <c>.Street</c>,
        /// <c>.Building</c>, <c>.Other</c>.</summary>
        public AddressFilterSpec Address => AddressFilterSpec.Instance;

        /// <summary>Date of birth. Supports <c>&gt;=</c> (from) and <c>&lt;=</c> (to) operators.</summary>
        public DateTime? BirthDate => null;

        /// <summary>Gender. Use <see cref="Gender.Undefined"/> to match contacts where gender is not set.</summary>
        public Gender? Gender => null;

        /// <summary>Company name.</summary>
        public string CompanyName { get; } = string.Empty;

        /// <summary>Company website URL.</summary>
        public string CompanyUrl { get; } = string.Empty;

        /// <summary>Free-form notes.</summary>
        public string Info { get; } = string.Empty;

        /// <summary>Creation date. Supports <c>&gt;=</c> (from) and <c>&lt;=</c> (to) operators.</summary>
        public DateTime? CreatedAt => null;
    }
}
