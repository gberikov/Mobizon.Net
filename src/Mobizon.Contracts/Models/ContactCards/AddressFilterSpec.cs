namespace Mobizon.Contracts.Models.ContactCards
{
    /// <summary>
    /// Describes the filterable sub-fields of a contact's address.
    /// Used in LINQ-style expressions:
    /// <code>client.ContactCards.Where(x => x.Address.City == "Almaty")</code>
    /// </summary>
    public sealed class AddressFilterSpec
    {
        internal static readonly AddressFilterSpec Instance = new AddressFilterSpec();

        /// <summary>ISO 3166-1 alpha-2 country code (e.g. <c>"KZ"</c>).</summary>
        public string CountryA2 { get; } = string.Empty;

        /// <summary>Country name.</summary>
        public string Country { get; } = string.Empty;

        /// <summary>Region / province ID.</summary>
        public int? RegionId { get; }

        /// <summary>Region / province name.</summary>
        public string Region { get; } = string.Empty;

        /// <summary>City ID.</summary>
        public int? CityId { get; }

        /// <summary>City name.</summary>
        public string City { get; } = string.Empty;

        /// <summary>Postal / ZIP code.</summary>
        public string PostalCode { get; } = string.Empty;

        /// <summary>Street name.</summary>
        public string Street { get; } = string.Empty;

        /// <summary>Building number.</summary>
        public string Building { get; } = string.Empty;

        /// <summary>Additional address details.</summary>
        public string Other { get; } = string.Empty;
    }
}
