using System.Text.Json.Serialization;

namespace Mobizon.Contracts
{
    /// <summary>
    /// Represents the address field of a contact card as returned by the API.
    /// </summary>
    public class AddressFieldInfo
    {
        /// <summary>Gets or sets the ISO 3166-1 alpha-2 country code.</summary>
        public string? CountryA2 { get; set; }

        /// <summary>Gets or sets the country name.</summary>
        public string? Country { get; set; }

        /// <summary>Gets or sets the region ID.</summary>
        public string? RegionId { get; set; }

        /// <summary>Gets or sets the region name.</summary>
        public string? Region { get; set; }

        /// <summary>Gets or sets the city ID.</summary>
        public string? CityId { get; set; }

        /// <summary>Gets or sets the city name.</summary>
        public string? City { get; set; }

        /// <summary>Gets or sets the postal code.</summary>
        [JsonPropertyName("postalcode")]
        public string? PostalCode { get; set; }

        /// <summary>Gets or sets the street name.</summary>
        public string? Street { get; set; }

        /// <summary>Gets or sets the building number.</summary>
        public string? Building { get; set; }

        /// <summary>Gets or sets additional address details.</summary>
        public string? Other { get; set; }
    }
}
